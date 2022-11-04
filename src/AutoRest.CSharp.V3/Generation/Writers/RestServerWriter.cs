// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoRest.CSharp.V3.AutoRest.Plugins;
using AutoRest.CSharp.V3.Generation.Types;
using AutoRest.CSharp.V3.Input;
using AutoRest.CSharp.V3.Output.Models;
using AutoRest.CSharp.V3.Output.Models.Requests;
using AutoRest.CSharp.V3.Output.Models.Responses;
using AutoRest.CSharp.V3.Output.Models.Serialization;
using AutoRest.CSharp.V3.Output.Models.Serialization.Json;
using AutoRest.CSharp.V3.Output.Models.Serialization.Xml;
using AutoRest.CSharp.V3.Output.Models.Shared;
using AutoRest.CSharp.V3.Output.Models.Types;
using AutoRest.CSharp.V3.Utilities;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AutoRest.CSharp.V3.Generation.Writers
{
    internal class RestServerWriter
    {
        public void WriteServer(CodeWriter writer, IEnumerable<RestClientMethod> methods, CSharpType cs, Configuration configuration)
        {
            using (writer.Namespace(cs.Namespace))
            {
                using (writer.Scope($"public class {cs.Name}"))
                {
                    //The logger is obtained from the executionContext, rather than being injected by ctor
                    //WriteClientFields(writer, cs);
                    //WriteClientCtor(writer, cs);

                    foreach (var method in methods)
                    {
                        WriteOperation(writer, method, cs.Name, configuration);
                    }
                }
            }
        }

        private void WriteClientFields(CodeWriter writer, CSharpType cs)
        {
            var loggerParam = new Parameter("logger", "Class logger", new CSharpType(typeof(ILogger<>), cs), null, true);
            var parameters = new List<Parameter> { loggerParam };
            foreach (Parameter clientParameter in parameters)
            {
                writer.Line($"private {loggerParam.Type} _{loggerParam.Name};");
            }

            writer.Line();
        }

        private void WriteClientCtor(CodeWriter writer, CSharpType cs)
        {
            writer.WriteXmlDocumentationSummary($"Initializes a new instance of {cs.Name}");

            var loggerParam = new Parameter("logger", "Class logger", new CSharpType(typeof(ILogger<>), cs), null, true);
            var parameters = new List<Parameter> { loggerParam };

            foreach (Parameter parameter in parameters)
            {
                writer.WriteXmlDocumentationParameter(parameter.Name, parameter.Description);
            }

            writer.WriteXmlDocumentationRequiredParametersException(parameters);

            writer.Append($"public {cs.Name:D}(");

            foreach (Parameter clientParameter in parameters)
            {
                writer.WriteParameter(clientParameter);
            }

            writer.RemoveTrailingComma();
            writer.Line($")");
            using (writer.Scope())
            {
                writer.WriteParameterAssignmentsWithNullChecks(parameters);
            }
            writer.Line();
        }

        private string CreateMethodName(string name, bool async) => $"{name}{(async ? "Async" : string.Empty)}";

        private string CreateRequestMethodName(string name) => $"Create{name}Request";

        private void WriteRequestCreation(CodeWriter writer, RestClientMethod clientMethod)
        {
            using var methodScope = writer.AmbientScope();

            var methodName = CreateRequestMethodName(clientMethod.Name);
            writer.Append($"internal {typeof(HttpMessage)} {methodName}(");
            var parameters = clientMethod.Parameters;
            foreach (Parameter clientParameter in parameters)
            {
                writer.Append($"{clientParameter.Type} {clientParameter.Name:D},");
            }
            writer.RemoveTrailingComma();
            writer.Line($")");
            using (writer.Scope())
            {
                var message = new CodeWriterDeclaration("message");
                var request = new CodeWriterDeclaration("request");
                var uri = new CodeWriterDeclaration("uri");

                writer.Line($"var {request:D} = {message}.Request;");
                var method = clientMethod.Request.HttpMethod;
                writer.Line($"{request}.Method = {typeof(RequestMethod)}.{method.ToRequestMethodName()};");

                writer.Line($"var {uri:D} = new RawRequestUriBuilder();");
                foreach (var segment in clientMethod.Request.PathSegments)
                {
                    if (!segment.Value.IsConstant && segment.Value.Reference.Name == "nextLink")
                    {
                        if (segment.IsRaw)
                        {
                            // Artificial nextLink needs additional logic for relative versus absolute links
                            WritePathSegment(writer, uri, segment, "AppendRawNextLink");
                        }
                        else
                        {
                            // Natural nextLink parameters need to use a different method to parse path and query elements
                            WritePathSegment(writer, uri, segment, "AppendRaw");
                        }
                    }
                    else
                    {
                        WritePathSegment(writer, uri, segment);
                    }
                }

                //TODO: Duplicate code between query and header parameter processing logic
                foreach (var queryParameter in clientMethod.Request.Query)
                {
                    WriteQueryParameter(writer, uri, queryParameter);
                }

                writer.Line($"{request}.Uri = {uri};");

                foreach (var header in clientMethod.Request.Headers)
                {
                    WriteHeader(writer, request, header);
                }

                switch (clientMethod.Request.Body)
                {
                    case SchemaRequestBody body:
                        using (WriteValueNullCheck(writer, body.Value))
                        {
                            WriteSerializeContent(
                                writer,
                                request,
                                body.Serialization,
                                w => WriteConstantOrParameter(w, body.Value, ignoreNullability: true));
                        }

                        break;
                    case BinaryRequestBody binaryBody:
                        using (WriteValueNullCheck(writer, binaryBody.Value))
                        {
                            writer.Append($"{request}.Content = {typeof(RequestContent)}.Create(");
                            WriteConstantOrParameter(writer, binaryBody.Value);
                            writer.Line($");");
                        }
                        break;
                    case TextRequestBody textBody:
                        using (WriteValueNullCheck(writer, textBody.Value))
                        {
                            writer.Append($"{request}.Content = new {typeof(StringRequestContent)}(");
                            WriteConstantOrParameter(writer, textBody.Value);
                            writer.Line($");");
                        }
                        break;
                    case FlattenedSchemaRequestBody flattenedSchemaRequestBody:
                        var initializers = new List<PropertyInitializer>();
                        foreach (var initializer in flattenedSchemaRequestBody.Initializers)
                        {
                            initializers.Add(new PropertyInitializer(initializer.Property, w => w.WriteReferenceOrConstant(initializer.Value)));
                        }
                        var modelVariable = new CodeWriterDeclaration("model");
                        writer.WriteInitialization(
                                (w, v) => w.Line($"var {modelVariable:D} = {v};"),
                                flattenedSchemaRequestBody.ObjectType,
                                flattenedSchemaRequestBody.ObjectType.InitializationConstructor,
                                initializers);

                        WriteSerializeContent(
                            writer,
                            request,
                            flattenedSchemaRequestBody.Serialization,
                            w => w.Append(modelVariable));
                        break;
                    case null:
                        break;
                    default:
                        throw new NotImplementedException(clientMethod.Request.Body?.GetType().FullName);
                }

                writer.Line($"return {message};");
            }
            writer.Line();
        }

        private static void WriteSerializeContent(CodeWriter writer, CodeWriterDeclaration request, ObjectSerialization bodySerialization, CodeWriterDelegate valueDelegate)
        {
            switch (bodySerialization)
            {
                case JsonSerialization jsonSerialization:
                    {
                        var content = new CodeWriterDeclaration("content");

                        writer.Line($"var {content:D} = new {typeof(Utf8JsonRequestContent)}();");
                        writer.ToSerializeCall(
                            jsonSerialization,
                            valueDelegate,
                            writerName: w => w.Append($"{content}.{nameof(Utf8JsonRequestContent.JsonWriter)}"));
                        writer.Line($"{request}.Content = {content};");
                        break;
                    }
                case XmlElementSerialization xmlSerialization:
                    {
                        var content = new CodeWriterDeclaration("content");

                        writer.Line($"var {content:D} = new {typeof(XmlWriterContent)}();");
                        writer.ToSerializeCall(
                            xmlSerialization,
                            valueDelegate,
                            writerName: w => w.Append($"{content}.{nameof(XmlWriterContent.XmlWriter)}"));
                        writer.Line($"{request}.Content = {content};");
                        break;
                    }
                default:
                    throw new NotImplementedException(bodySerialization.ToString());
            }
        }

        private void WriteOperation(CodeWriter writer, RestClientMethod operation, string functionNamePrefix, Configuration configuration)
        {
            using var methodScope = writer.AmbientScope();

            CSharpType? bodyType = new CSharpType(typeof(Task<HttpResponseData>)); // operation.ReturnType;
            CSharpType? headerModelType = operation.HeaderModel?.Type;

            bool bodyIsTrigger = false;
            if (operation.Parameters.Any(p => p.Name == "body"))
            {
                bodyIsTrigger = true;
            }

            var httpRequestParameter = new Parameter("req", "A representation of the HTTP request sent by the host.", new CSharpType(typeof(HttpRequestData)), null, false);
            var functionContext = new Parameter("executionContext", "Encapsulates the information about a function execution.", new CSharpType(typeof(FunctionContext)), null, false);
            var parameters = operation.Parameters.ToList();

            // If we have parameters that are not in the route, then we can't have them in this list.
            var routeSegments = new HashSet<string>();
            foreach (var pathSegement in operation.Request.PathSegments)
            {
                if (!pathSegement.Value.IsConstant)
                {
                    routeSegments.Add(pathSegement.Value.Reference.Name);
                }
            }
            routeSegments.Add("body");
            parameters.RemoveAll(p => !routeSegments.Contains(p.Name));

            var indexOfFirstOptional = parameters.FindIndex(p => p.DefaultValue.HasValue);
            if (!bodyIsTrigger)
            {
                parameters.Insert(0, httpRequestParameter);
                parameters.Insert(1, functionContext);
            }
            else
            {
                // Insert req & context before optional params
                if (indexOfFirstOptional == -1)
                {
                    parameters.Add(httpRequestParameter);
                    parameters.Add(functionContext);
                }
                else
                {
                    parameters.Insert(indexOfFirstOptional, httpRequestParameter);
                }
            }

            //var logParameter = new Parameter("log", "function logger", new CSharpType(typeof(ILogger)), null, false);
            //// Insert logger before optional params
            //if (indexOfFirstOptional == -1)
            //{
            //    parameters.Add(logParameter);
            //}
            //else
            //{
            //    indexOfFirstOptional = parameters.FindIndex(p => p.DefaultValue.HasValue);
            //    parameters.Insert(indexOfFirstOptional, logParameter);
            //}

            writer.Line($"private readonly ILogger _logger;");
            writer.LineRaw($"");
            writer.Line($"public {functionNamePrefix}(ILoggerFactory loggerFactory)");
            writer.Line($"{{");
            writer.Line($"_logger = loggerFactory.CreateLogger<{functionNamePrefix}>();");
            writer.Line($"}}");
            writer.LineRaw($"");

            writer.WriteXmlDocumentationSummary(operation.Description);

            foreach (Parameter parameter in parameters)
            {
                writer.WriteXmlDocumentationParameter(parameter.Name, parameter.Description);
            }

            //https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide#differences-with-net-class-library-functions
            //writer.WriteXmlDocumentationParameter("cancellationToken", "The cancellation token provided on Function shutdown.");

            writer.WriteXmlDocumentationRequiredParametersException(parameters);

            //Use contentional method name 'Run' when grouping by operation
            var isSingleFuncPerFile = configuration.ApiGroupBy == ApiGroupBy.Operation || configuration.ApiGroupBy == ApiGroupBy.OperationFlat;
            var methodName = CreateMethodName(isSingleFuncPerFile ? "Run" : operation.Name, true);
            var functionName = CreateMethodName(operation.Name, true);
            var fullMethodName = $"{functionName}_{operation.Request.HttpMethod.Method.ToLowerInvariant()}";

            writer.Append($"[{new CSharpType(typeof(FunctionAttribute))}(\"{fullMethodName}\")]");
            writer.Append($"public async {new CSharpType(typeof(Task<>), new CSharpType(typeof(HttpResponseData)))} {methodName}(");

            string route = string.Empty;
            foreach (var pathSegement in operation.Request.PathSegments)
            {
                if (pathSegement.Value.IsConstant)
                {
                    route += pathSegement.Value.Constant.Value?.ToString();
                }
                else
                {
                    if (pathSegement.Value.Reference.Name != "endpoint")
                    {
                        route += $"{{{pathSegement.Value.Reference.Name}}}";
                    }
                }
            }
            route = route.TrimStart('/');

            foreach (Parameter parameter in parameters)
            {
                if ((bodyIsTrigger && parameter.Name == "body") || (!bodyIsTrigger && parameter.Name == httpRequestParameter.Name))
                {
                    writer.WriteFunctionParameter(parameter, operation.Request.HttpMethod.Method.ToLowerInvariant(), $"\"{route ?? "null"}\"");
                }
                else
                {
                    writer.WriteParameter(parameter);
                }
            }
            //writer.Line($"{typeof(CancellationToken)} cancellationToken = default)"); //Not supported in isolated runtime
            writer.RemoveTrailingComma();
            writer.Line($")");

            using (writer.Scope())
            {
                writer.UseNamespace(typeof(ILogger).Namespace!);
                writer.UseNamespace(typeof(HttpStatusCode).Namespace!);

                writer.Line($"_logger.LogInformation(\"HTTP trigger function processed a request.\");").Line();

                if (operation.Responses.Any())
                {
                    writer.Line($"// TODO: Handle Documented Responses.");
                }
                foreach (var response in operation.Responses)
                {
                    foreach (var statusCode in response.StatusCodes)
                    {
                        writer.Line($"// Spec Defines: HTTP {statusCode}");

                        if (statusCode == 200 && response.ResponseBody != null)
                        {
                            //Give an example of constructing the response object
                            writer.Line($"// Example:");
                            writer.Line($"// var response = req.CreateResponse({typeof(HttpStatusCode).Name}.OK);");
                            writer.Line($"// var model = {response.ResponseBody.Type.Name}...");
                            writer.Line($"// await response.WriteAsJsonAsync(model);");
                            writer.Line($"// return response;").Line();
                        }
                    }
                }

                writer.Line().Line($"throw new {typeof(NotImplementedException)}();");
            }
            writer.Line();
        }

        private void WriteConstantOrParameter(CodeWriter writer, ReferenceOrConstant constantOrReference, bool ignoreNullability = false, bool enumAsString = false)
        {
            if (constantOrReference.IsConstant)
            {
                writer.WriteConstant(constantOrReference.Constant);
            }
            else
            {
                writer.Identifier(constantOrReference.Reference.Name);
                if (!ignoreNullability)
                {
                    writer.AppendNullableValue(constantOrReference.Type);
                }
            }

            //TODO test if i need to comment this out or change it to support (query/path) parameter binding enum values
            if (enumAsString &&
                !constantOrReference.Type.IsFrameworkType &&
                constantOrReference.Type.Implementation is EnumType enumType)
            {
                writer.AppendEnumToString(enumType);
            }
        }

        private void WritePathSegment(CodeWriter writer, CodeWriterDeclaration uri, PathSegment segment, string? methodName = null)
        {
            if (segment.Value.Type.IsFrameworkType &&
                segment.Value.Type.FrameworkType == typeof(Uri))
            {
                writer.Append($"{uri}.Reset(");
                WriteConstantOrParameter(writer, segment.Value, enumAsString: !segment.IsRaw);
                writer.Line($");");
                return;
            }

            methodName ??= segment.IsRaw ? "AppendRaw" : "AppendPath";
            writer.Append($"{uri}.{methodName}(");
            WriteConstantOrParameter(writer, segment.Value, enumAsString: !segment.IsRaw);
            WriteSerializationFormat(writer, segment.Format);
            writer.Line($", {segment.Escape:L});");
        }

        private string? GetSerializationStyleDelimiter(RequestParameterSerializationStyle style) => style switch
        {
            RequestParameterSerializationStyle.PipeDelimited => "|",
            RequestParameterSerializationStyle.TabDelimited => "\t",
            RequestParameterSerializationStyle.SpaceDelimited => " ",
            RequestParameterSerializationStyle.CommaDelimited => ",",
            _ => null
        };

        private void WriteHeader(CodeWriter writer, CodeWriterDeclaration request, RequestHeader header)
        {
            string? delimiter = GetSerializationStyleDelimiter(header.SerializationStyle);
            string method = delimiter != null
                ? nameof(RequestHeaderExtensions.AddDelimited)
                : nameof(RequestHeaderExtensions.Add);

            using (WriteValueNullCheck(writer, header.Value))
            {
                writer.Append($"{request}.Headers.{method}({header.Name:L}, ");
                WriteConstantOrParameter(writer, header.Value, enumAsString: true);
                if (delimiter != null)
                {
                    writer.Append($", {delimiter:L}");
                }
                WriteSerializationFormat(writer, header.Format);
                writer.Line($");");
            }
        }

        private CodeWriter.CodeWriterScope? WriteValueNullCheck(CodeWriter writer, ReferenceOrConstant value)
        {
            if (value.IsConstant)
                return default;

            var type = value.Type;
            if (type.IsNullable)
            {
                // turn "object.Property" into "object?.Property"
                var parts = value.Reference.Name.Split(".");

                writer.Append($"if (");
                bool first = true;
                foreach (var part in parts)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        writer.AppendRaw("?.");
                    }
                    writer.Identifier(part);
                }

                return writer.Line($" != null)").Scope();
            }

            return default;
        }

        private void WriteSerializationFormat(CodeWriter writer, SerializationFormat format)
        {
            if (format == SerializationFormat.Bytes_Base64Url)
            {
                // base64url is the only options for paths ns queries
                return;
            }

            var formatSpecifier = format.ToFormatSpecifier();
            if (formatSpecifier != null)
            {
                writer.Append($", {formatSpecifier:L}");
            }
        }

        private void WriteQueryParameter(CodeWriter writer, CodeWriterDeclaration uri, QueryParameter queryParameter)
        {
            string? delimiter = GetSerializationStyleDelimiter(queryParameter.SerializationStyle);
            string method = delimiter != null
                ? nameof(RequestUriBuilderExtensions.AppendQueryDelimited)
                : nameof(RequestUriBuilderExtensions.AppendQuery);

            ReferenceOrConstant value = queryParameter.Value;
            using (WriteValueNullCheck(writer, value))
            {
                writer.Append($"{uri}.{method}({queryParameter.Name:L}, ");
                WriteConstantOrParameter(writer, value, enumAsString: true);
                if (delimiter != null)
                {
                    writer.Append($", {delimiter:L}");
                }
                WriteSerializationFormat(writer, queryParameter.SerializationFormat);
                writer.Line($", {queryParameter.Escape:L});");
            }
        }

        //TODO: Do multiple status codes
        private void WriteStatusCodeSwitch(CodeWriter writer, CodeWriterDeclaration message, RestClientMethod operation, bool async)
        {
            string responseVariable = $"{message.ActualName}.Response";

            var returnType = operation.ReturnType;
            var headersModelType = operation.HeaderModel?.Type;

            ReturnKind kind;

            if (returnType != null && headersModelType != null)
            {
                kind = ReturnKind.HeadersAndValue;
            }
            else if (headersModelType != null)
            {
                kind = ReturnKind.Headers;
            }
            else if (returnType != null)
            {
                kind = ReturnKind.Value;
            }
            else
            {
                kind = ReturnKind.Response;
            }

            if (headersModelType != null)
            {
                writer.Line($"var headers = new {headersModelType}({responseVariable});");
            }

            using (writer.Scope($"switch ({responseVariable}.Status)"))
            {
                foreach (var response in operation.Responses)
                {
                    var responseBody = response.ResponseBody;
                    var statusCodes = response.StatusCodes;

                    foreach (var statusCode in statusCodes)
                    {
                        writer.Line($"case {statusCode}:");
                    }

                    using (responseBody != null ? writer.Scope() : default)
                    {
                        ReferenceOrConstant value = default;

                        var valueVariable = new CodeWriterDeclaration("value");
                        if (responseBody is ObjectResponseBody objectResponseBody)
                        {
                            writer.Line($"{responseBody.Type} {valueVariable:D} = default;");
                            writer.WriteDeserializationForMethods(
                                objectResponseBody.Serialization,
                                async,
                                (w, v) => w.Line($"{valueVariable} = {v};"),
                                responseVariable);
                            value = new Reference(valueVariable.ActualName, responseBody.Type);
                        }
                        else if (responseBody is StreamResponseBody _)
                        {
                            writer.Line($"var {valueVariable:D} = {message}.ExtractResponseContent();");
                            value = new Reference(valueVariable.ActualName, responseBody.Type);
                        }
                        else if (returnType != null)
                        {
                            value = Constant.Default(returnType.WithNullable(true));
                        }

                        switch (kind)
                        {
                            case ReturnKind.Response:
                                writer.Append($"return {responseVariable};");
                                break;
                            case ReturnKind.Headers:
                                writer.Append($"return {typeof(ResponseWithHeaders)}.FromValue(headers, {responseVariable});");
                                break;
                            case ReturnKind.HeadersAndValue:
                                writer.Append($"return {typeof(ResponseWithHeaders)}.FromValue");
                                if (!Equals(responseBody?.Type, operation.ReturnType))
                                {
                                    writer.Append($"<{operation.ReturnType}, {headersModelType}>");
                                }
                                writer.Append($"(");
                                writer.WriteReferenceOrConstant(value);
                                writer.Append($", headers, {responseVariable});");
                                break;
                            case ReturnKind.Value:
                                writer.Append($"return {typeof(Azure.Response)}.FromValue");
                                if (!Equals(responseBody?.Type, operation.ReturnType))
                                {
                                    writer.Append($"<{operation.ReturnType}>");
                                }
                                writer.Append($"(");
                                writer.WriteReferenceOrConstant(value);
                                writer.Append($", {responseVariable});");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }

                writer.Line($"default:");
                //if (async)
                //{
                //    writer.Line($"throw await {ClientDiagnosticsField}.CreateRequestFailedExceptionAsync({responseVariable}).ConfigureAwait(false);");
                //}
                //else
                //{
                //    writer.Line($"throw {ClientDiagnosticsField}.CreateRequestFailedException({responseVariable});");
                //}
            }
        }

        private enum ReturnKind
        {
            Response,
            Headers,
            HeadersAndValue,
            Value
        }
    }
}
