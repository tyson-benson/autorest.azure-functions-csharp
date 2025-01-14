﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace AutoRest.CSharp.V3.AutoRest.Plugins
{
    internal class GeneratedCodeWorkspace
    {
        private Project _project;

        private GeneratedCodeWorkspace(Project generatedCodeProject)
        {
            _project = generatedCodeProject;
        }

        public void AddGeneratedFile(string name, string text)
        {
            var ext = Path.GetExtension(name).ToLowerInvariant();
            if (ext == ".cs")
            {
                var document = _project.AddDocument(name, text);
                var root = document.GetSyntaxRootAsync().Result;
                Debug.Assert(root != null);

                root = root.WithAdditionalAnnotations(Simplifier.Annotation);
                document = document.WithSyntaxRoot(root);
                _project = document.Project;
            }
            else
            {
                //Non-c# files should not be parsed and formatted as c#
                var document = _project.AddAdditionalDocument(name, text);
                _project = document.Project;
            }
        }


        public async IAsyncEnumerable<(string Name, string Text)> GetGeneratedFilesAsync()
        {
            List<Task<Document>> documents = new List<Task<Document>>();
            foreach (Document document in _project.Documents)
            {
                // Skip writing shared files or originals
                if (!IsGeneratedDocument(document))
                {
                    continue;
                }

                documents.Add(Task.Run(() => ProcessDocument(document)));
            }

            foreach (var task in documents)
            {
                var processed = await task;
                var text = await processed.GetSyntaxTreeAsync();

                yield return (processed.Name, text!.ToString());
            }

            foreach (var document in _project.AdditionalDocuments)
            {
                var text = await document.GetTextAsync();
                yield return (document.Name, text!.ToString());
            }
        }

        private async Task<Document> ProcessDocument(Document document)
        {
            var compilation = await document.Project.GetCompilationAsync();
            Debug.Assert(compilation != null);

            var syntaxTree = await document.GetSyntaxTreeAsync();
            if (syntaxTree != null)
            {
                var rewriter = new MemberRemoverRewriter(_project, compilation.GetSemanticModel(syntaxTree));
                document = document.WithSyntaxRoot(rewriter.Visit(await syntaxTree.GetRootAsync()));
            }

            document = await Simplifier.ReduceAsync(document);
            document = await Formatter.FormatAsync(document);
            return document;
        }

        public static GeneratedCodeWorkspace Create(string projectDirectory)
        {
            var workspace = new AdhocWorkspace();
            // TODO: This is not the right way to construct the workspace but it works
            Project generatedCodeProject = workspace.AddProject("GeneratedCode", LanguageNames.CSharp);

            var corlibLocation = typeof(object).Assembly.Location;
            var references = new List<MetadataReference>();

            references.Add(MetadataReference.CreateFromFile(corlibLocation));

            var trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator);
            foreach (var tpl in trustedAssemblies)
            {
                references.Add(MetadataReference.CreateFromFile(tpl));
            }

            generatedCodeProject = generatedCodeProject
                .AddMetadataReferences(references)
                .WithCompilationOptions(new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

            var generatedCodeDirectory = Path.Combine(projectDirectory);

            foreach (string sourceFile in Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                // Ignore existing generated code
                if (sourceFile.StartsWith(generatedCodeDirectory))
                {
                    continue;
                }
                generatedCodeProject = generatedCodeProject.AddDocument(sourceFile, File.ReadAllText(sourceFile), Array.Empty<string>(), sourceFile).Project;
            }

            return new GeneratedCodeWorkspace(generatedCodeProject);
        }

        public async Task<CSharpCompilation> GetCompilationAsync()
        {
            var compilation = await _project.GetCompilationAsync() as CSharpCompilation;
            Debug.Assert(compilation != null);
            return compilation;
        }

        public static bool IsGeneratedDocument(Document document) => true;
    }
}
