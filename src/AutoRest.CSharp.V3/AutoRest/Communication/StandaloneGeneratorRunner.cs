﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoRest.CSharp.V3.AutoRest.Plugins;
using AutoRest.CSharp.V3.Input;

namespace AutoRest.CSharp.V3.AutoRest.Communication
{
    internal class StandaloneGeneratorRunner
    {
        public static async Task RunAsync(string[] args)
        {
            var basePath = args.Single(a=> !a.StartsWith("--"));

            var configuration = LoadConfiguration(basePath, File.ReadAllText(Path.Combine(basePath, "Configuration.json")));
            var codeModel = CodeModelSerialization.DeserializeCodeModel(File.ReadAllText(Path.Combine(basePath, "CodeModel.yaml")));

            var workspace = await new CSharpGen().ExecuteAsync(codeModel, configuration, null);

            await foreach (var file in workspace.GetGeneratedFilesAsync())
            {
                if (string.IsNullOrEmpty(file.Text))
                {
                    continue;
                }
                var filename = Path.Combine(configuration.OutputFolder, file.Name);
                Console.WriteLine($"Writing {filename}");
#pragma warning disable CS8604 // Possible null reference argument.
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
#pragma warning restore CS8604 // Possible null reference argument.
                await File.WriteAllTextAsync(filename, file.Text);
            }
        }

        internal static string SaveConfiguration(Configuration configuration)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream))
                {
                    writer.WriteStartObject();
                    writer.WriteString(nameof(Configuration.OutputFolder), Path.GetRelativePath(configuration.OutputFolder, configuration.OutputFolder));
                    writer.WriteString(nameof(Configuration.Namespace), configuration.Namespace);
                    writer.WriteString(nameof(Configuration.LibraryName), configuration.LibraryName);
                    writer.WriteBoolean(nameof(Configuration.PublicClients), configuration.PublicClients);
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        private static Configuration LoadConfiguration(string basePath, string json)
        {
            JsonDocument document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new Configuration(
#pragma warning disable CS8604 // Possible null reference argument.
                Path.Combine(basePath, root.GetProperty(nameof(Configuration.OutputFolder)).GetString()),
                root.GetProperty(nameof(Configuration.Namespace)).GetString(),
                root.GetProperty(nameof(Configuration.ApiGroupBy)).GetString(),
#pragma warning restore CS8604 // Possible null reference argument.
                root.GetProperty(nameof(Configuration.LibraryName)).GetString(),
                saveInputs: false,
                root.GetProperty(nameof(Configuration.PublicClients)).GetBoolean(),
                root.GetProperty(nameof(Configuration.GenerateMetadata)).GetBoolean()
            );
        }
    }
}
