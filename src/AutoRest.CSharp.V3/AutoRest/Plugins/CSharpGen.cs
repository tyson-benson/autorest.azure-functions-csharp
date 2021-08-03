// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoRest.CSharp.V3.AutoRest.Communication;
using AutoRest.CSharp.V3.Generation.Types;
using AutoRest.CSharp.V3.Generation.Writers;
using AutoRest.CSharp.V3.Input;
using AutoRest.CSharp.V3.Input.Source;
using AutoRest.CSharp.V3.Output.Builders;
using AutoRest.CSharp.V3.Output.Models.Responses;
using AutoRest.CSharp.V3.Output.Models.Types;
using AutoRest.CSharp.V3.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;
using AutoRest.CSharp.V3.Output.Models;
using AutoRest.CSharp.V3.Output.Models.Requests;

namespace AutoRest.CSharp.V3.AutoRest.Plugins
{
    [PluginName("azure-functions-csharp-net5-isolated")]
    internal class CSharpGen : IPlugin
    {

        public async Task<GeneratedCodeWorkspace> ExecuteAsync(CodeModel codeModel, Configuration configuration, IPluginCommunication? autoRest)
        {
            var directory = Directory.CreateDirectory(configuration.OutputFolder);
            var project = GeneratedCodeWorkspace.Create(configuration.OutputFolder);
            var sourceInputModel = new SourceInputModel(await project.GetCompilationAsync());

            var context = new BuildContext(codeModel, configuration, sourceInputModel);

            var modelWriter = new ModelWriter();
            var clientWriter = new ClientWriter();
            var restClientWriter = new RestClientWriter();
            var restServerWriter = new RestServerWriter();
            var serializeWriter = new SerializationWriter();
            var headerModelModelWriter = new ResponseHeaderGroupWriter();
            var cSharpProj = new CSharpProj();

            // Generate the landing zone for the files.
            if (configuration.GenerateMetadata)
            {
                var GitIgnoreTemplateFile = File.ReadAllText(@"StaticResources/GitIgnoreTemplateFile.txt");
                var LocalSettingsJSONTemplate = File.ReadAllText(@"StaticResources/LocalSettingsJSONTemplate.json");
                var VSCodeExtensions = File.ReadAllText(@"StaticResources/VSCodeExtensions.json");
                var HostJSONTemplate = File.ReadAllText(@"StaticResources/HostJSONTemplate.json");
                var ProgramCsTemplate = File.ReadAllText(@"StaticResources/ProgramCsTemplate.txt");
                ProgramCsTemplate = ProgramCsTemplate.Replace("NAMESPACE_PLACEHOLDER", configuration.Namespace);

                project.AddGeneratedFile(".gitignore", GitIgnoreTemplateFile);
                project.AddGeneratedFile(".vscode/extensions.json", VSCodeExtensions);
                project.AddGeneratedFile("host.json", HostJSONTemplate);
                project.AddGeneratedFile("local.settings.json", LocalSettingsJSONTemplate);
                project.AddGeneratedFile("Program.cs", ProgramCsTemplate);
                if (autoRest != null)
                    { _ = await cSharpProj.Execute(autoRest); }
            }

            var AutorestGeneratedJSONTemplate = File.ReadAllText(@"StaticResources/AutorestGenerated.json");
            project.AddGeneratedFile(".autorest_generated.json", AutorestGeneratedJSONTemplate);

            foreach (TypeProvider? model in context.Library.Models)
            {
                var codeWriter = new CodeWriter();
                modelWriter.WriteModel(codeWriter, model);

                var name = model.Type.Name;
                project.AddGeneratedFile($"Models/{name}.cs", codeWriter.ToString());
            }

            var clientsAndOperations =
                from client in context.Library.RestClients
                from operation in client.Methods
                let path = operation.Request.PathSegments.FirstOrDefault(seg => seg.Value.IsConstant && !string.IsNullOrEmpty(seg.Value.Constant.Value?.ToString()))?.Value.Constant.Value?.ToString()?.Trim('/')
                select new ClientAndOperation(
                    client,
                    operation,
                    path
                );

            switch (configuration.ApiGroupBy)
            {
                case ApiGroupBy.Operation:
                case ApiGroupBy.OperationFlat:
                    {
                        var apiGroups = clientsAndOperations.GroupBy(co => co.Operation.Name);

                        foreach (var apiGroup in apiGroups)
                        {
                            var codeWriter = new CodeWriter();
                            var ns = apiGroup.First().Client.Type.Namespace;
                            var cs = new CSharpType(new SelfTypeProvider(context), ns, $"{apiGroup.Key.ToCleanName()}Api");
                            restServerWriter.WriteServer(codeWriter, apiGroup.Select(g => g.Operation).OrderBy(o => o.Name), cs, configuration);

                            var isFlat = configuration.ApiGroupBy == ApiGroupBy.OperationFlat;
                            //TODO consider adding a parameter to specify a desired maximum nesting depth
                            var folder = isFlat ? "" : $"{apiGroup.First().Path}/";
                            project.AddGeneratedFile($"{folder}{cs.Name}.cs", codeWriter.ToString());
                        }
                    }
                    break;

                case ApiGroupBy.OperationGroup:
                    {
                        var apiGroups = clientsAndOperations.GroupBy(co => co.Client.ClientPrefix);

                        foreach (var apiGroup in apiGroups)
                        {
                            var codeWriter = new CodeWriter();
                            var ns = apiGroup.First().Client.Type.Namespace;
                            var cs = new CSharpType(new SelfTypeProvider(context), ns, $"{apiGroup.Key.ToCleanName()}Api");
                            restServerWriter.WriteServer(codeWriter, apiGroup.Select(g => g.Operation).OrderBy(o => o.Name), cs, configuration);

                            project.AddGeneratedFile($"{cs.Name}.cs", codeWriter.ToString());
                        }
                    }
                    break;

                case ApiGroupBy.FirstPathSegment:
                case ApiGroupBy.LastPathSegment:
                    {
                        var apiGroups = clientsAndOperations.GroupBy(co =>
                        {
                            //Default grouping, first segment of operation path /admin/secrets -> AdminApi.cs
                            Output.Models.Requests.PathSegment? pathSegment = co.Operation.Request.PathSegments.First(s =>
                            {
                                var segementValue = s.Value.IsConstant ? s.Value.Constant.Value : null;
                                if (segementValue != null)
                                {
                                    return (segementValue.ToString() ?? string.Empty).StartsWith("/");
                                }
                                return false;
                            });

                            if (pathSegment != null)
                            {
                                var pathString = pathSegment.Value.Constant.Value?.ToString();

                                if (!string.IsNullOrWhiteSpace(pathString) && pathString.Contains('/'))
                                {
                                    var segments = pathString.Split('/', StringSplitOptions.RemoveEmptyEntries);
                                    if (configuration.ApiGroupBy == ApiGroupBy.FirstPathSegment)
                                    {
                                        return segments.First().ToLower();
                                    }
                                    if (configuration.ApiGroupBy == ApiGroupBy.LastPathSegment)
                                    {
                                        return segments.Last().ToLower();
                                    }
                                }
                            }

                            return string.Empty;
                        });

                        foreach (var apiGroup in apiGroups)
                        {
                            var codeWriter = new CodeWriter();
                            var ns = apiGroup.First().Client.Type.Namespace;
                            var cs = new CSharpType(new SelfTypeProvider(context), ns, $"{apiGroup.Key.ToCleanName()}Api");
                            restServerWriter.WriteServer(codeWriter, apiGroup.Select(g => g.Operation).OrderBy(o => o.Name), cs, configuration);

                            project.AddGeneratedFile($"{cs.Name}.cs", codeWriter.ToString());
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(configuration.ApiGroupBy), "Unknown api-grouping value: " + configuration.ApiGroupBy);
            }

            return project;
        }

        public async Task<bool> Execute(IPluginCommunication autoRest)
        {
            string? codeModelFileName = (await autoRest.ListInputs()).FirstOrDefault();
            if (string.IsNullOrEmpty(codeModelFileName)) throw new Exception("Generator did not receive the code model file.");

            var listInputs = await autoRest.ListInputs();

            var codeModelYaml = await autoRest.ReadFile(codeModelFileName);

            CodeModel codeModel = CodeModelSerialization.DeserializeCodeModel(codeModelYaml);

            var configuration = new Configuration(
                new Uri(GetRequiredOption(autoRest, "output-folder")).LocalPath,
                GetRequiredOption(autoRest, "namespace"),
                GetRequiredOption(autoRest, "api-group-by"),
                autoRest.GetValue<string?>("library-name").GetAwaiter().GetResult(),
                autoRest.GetValue<bool?>("save-inputs").GetAwaiter().GetResult() ?? false,
                autoRest.GetValue<bool?>("public-clients").GetAwaiter().GetResult() ?? false,
                autoRest.GetValue<bool?>("generate-metadata").GetAwaiter().GetResult() ?? true
            );

            if (configuration.SaveInputs)
            {
                await autoRest.WriteFile("Configuration.json", StandaloneGeneratorRunner.SaveConfiguration(configuration), "source-file-csharp");
                await autoRest.WriteFile("CodeModel.yaml", codeModelYaml, "source-file-csharp");
            }

            var project = await ExecuteAsync(codeModel, configuration, autoRest);
            await foreach (var file in project.GetGeneratedFilesAsync())
            {
                await autoRest.WriteFile(file.Name, file.Text, "source-file-csharp");
            }

            return true;
        }

        private string GetRequiredOption(IPluginCommunication autoRest, string name)
        {
            return autoRest.GetValue<string?>(name).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"{name} configuration parameter is required");
        }
    }

    internal record ClientAndOperation(RestClient Client, RestClientMethod Operation, string Path);
}
