// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AutoRest.CSharp.V3.AutoRest.Communication;
using AutoRest.CSharp.V3.Input;

namespace AutoRest.CSharp.V3.AutoRest.Plugins
{
    // ReSharper disable once StringLiteralTypo
    [PluginName("azure-functions-net5-csharpproj")]
    // ReSharper disable once IdentifierTypo
    internal class CSharpProj : IPlugin
    {
        private string _csProjContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <AzureFunctionsVersion>v3</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Azure.Functions.Worker.Extensions.Http"" Version=""3.0.12"" />
    <PackageReference Include=""Microsoft.Azure.Functions.Worker.Extensions.Storage"" Version=""4.0.4"" />
    <PackageReference Include=""Microsoft.Azure.Functions.Worker.Sdk"" Version=""1.0.3"" OutputItemType=""Analyzer"" />
    <PackageReference Include=""Microsoft.Azure.Functions.Worker"" Version=""1.1.0"" />
  </ItemGroup>
  <ItemGroup>
    <None Update=""host.json"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="".autorest_generated.json"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update=""local.settings.json"">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>Never</CopyToPublishDirectory>
    </None>
  </ItemGroup>
</Project>
";
        public async Task<bool> Execute(IPluginCommunication autoRest)
        {
            var ns = await autoRest.GetValue<string>("namespace");
            await autoRest.WriteFile($"{ns}.csproj", _csProjContent, "source-file-csharp");

            return true;
        }
    }
}
