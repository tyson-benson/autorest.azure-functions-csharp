# Azure Functions C# .NET 5 isolated process code generator for AutoRest V3

This is a code generation plugin for [autorest](https://aka.ms/autorest) - Microsoft's REST API code generation toolset.
This plugin was based on [@autorest/azure-functions-csharp](https://github.com/Azure/autorest.azure-functions-csharp), and updated to generate
Azure Function apps for the new .NET 5 isolated runtime.

In addition to generating .net 5 isolated function apps, a new option has been added (`api-group-by`) to provide additional folder structures
to arrange the generated API operations. See [the table below](#api-&-operation-file-organisation) for further details.

## Usage

```
autorest `
    --use=azure-functions-csharp-net5-isolated `
    --input-file=.\path\to\api-spec.yaml `
    --output-folder=.\path\to\output-directory `
    --namespace=Contoso.Namespace `
    --api-group-by=operation `
    --clear-output-folder
```

## Required parameters

-   input-file
-   output-folder
-   namespace
-   api-group-by

### API & Operation file organisation

The code generator can be configured to group operations into files based on a few available conventions.

| --api-group-by     | Description                                                                                                  |
| ------------------ | ------------------------------------------------------------------------------------------------------------ |
| operation          | Each operation gets its own file, and the files are nested in folders based on the operation path            |
| operation-flat     | Each operation gets its own file, and the files are in the root of the project                               |
| operation-group    | Operations are grouped into files named after the operationId prefix (e.g. operationId: groupName_getThing)  |
| first-path-segment | Operations are grouped into files named after the first segment in the operation path                        |
| last-path-segment  | Operations are grouped into files named after the last segment in the operation path prior to any parameters |

## Configuration

```yaml
use-extension:
    "@autorest/modelerfour": "4.15.414"
modelerfour:
    always-create-content-type-parameter: true
    flatten-models: true
    flatten-payloads: true
    group-parameters: true
pipeline:
    azure-functions-csharp-net5-isolated:
        input: modelerfour/identity
    azure-functions-csharp-net5-isolated/emitter:
        input: azure-functions-csharp-net5-isolated
        scope: output-scope
output-scope:
    output-artifact: source-file-csharp
```
