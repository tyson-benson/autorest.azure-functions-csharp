# Azure Functions C# code generator

## Setup

-   [NodeJS](https://nodejs.org/en/) (16.x.x)
-   `npm install` (at root)
-   [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/6.0) (6.0.402)
-   [PowerShell Core](https://github.com/PowerShell/PowerShell/releases/latest)
-   [Azure Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)

## Build

-   `dotnet build` (at root)

## Usage

```powershell
npm install -g autorest
```

To use this Azure Function (.NET 6 - isolated process) generator, you use the `--azure-functions-csharp-net6-isolated` generator plugin. The `--input-file` parameter can also take in a URL as well.

```powershell
autorest `
    --use=azure-functions-csharp-net6-isolated `
    --input-file='.\path\to\api-spec.yaml' `
    --output-folder='.\path\to\output-directory' `
    --clear-output-folder `
    --namespace='Contoso.Namespace' `
    --api-group-by='operation'
```

If you have cloned this repository and wish to run autorest with the local build of the extension:

```powershell
autorest `
    --use='C:\GitHub\autorest.azure-functions-csharp\artifacts\bin\AutoRest.CSharp.V3\Debug\net6.0' `
    --input-file='C:\GitHub\Janison\platform\src\api\rest\v1\events-api\spec.yaml' `
    --output-folder='C:\GitHub\Janison\platform\src\api\rest\v1\events-api\gen2' `
    --clear-output-folder `
    --namespace='Janison.API' `
    --api-group-by='operation'
```

If you wish to debug this autorest plugin, you simply need to add the flag `--launch-debugger` to the above example.
Please remember to compile the solution to ensure that the debugger will attach with the correct symbols.

```powershell
autorest `
    --use='C:\GitHub\autorest.azure-functions-csharp\artifacts\bin\AutoRest.CSharp.V3\Debug\net6.0' `
    --input-file='C:\GitHub\Janison\platform\src\api\rest\v1\events-api\spec.yaml' `
    --output-folder='C:\GitHub\Janison\platform\src\api\rest\v1\events-api\gen2' `
    --clear-output-folder `
    --namespace='Janison.API' `
    --api-group-by='operation' `
    --launch-debugger
```
## Configuration

```yaml
# autorest-core version
version: 3.0.6289
shared-source-folder: $(this-folder)/src/assets
save-inputs: true
use: $(this-folder)/artifacts/bin/AutoRest.CSharp.V3/Debug/net6.0/
clear-output-folder: false
public-clients: true
pipeline:
    azure-functions-net6-csharpproj:
        input: modelerfour/identity
    azure-functions-net6-csharpproj/emitter:
        input: azure-functions-net6-csharpproj
        scope: output-scope
```

### API & Operation file organisation

The code generator can be configured to group operations into files based on a few available conventions.

| --api-group-by     | Description                                                                                                  |
| ------------------ | ------------------------------------------------------------------------------------------------------------ |
| operation          | Each operation gets its own file, and the files are nested in folders based on the operation path            |
| operation-flat     | Each operation gets its own file, and the files are in the root of the project                               |
| operation-group    | Operations are grouped into files named after the operationId prefix (e.g. operationId: groupName_getThing)  |
| first-path-segment | Operations are grouped into files named after the first segment in the operation path                        |
| last-path-segment  | Operations are grouped into files named after the last segment in the operation path prior to any parameters |

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit [https://cla.microsoft.com](https://cla.microsoft.com).

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
