// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using AutoRest.CSharp.V3.Input;
using Azure.Core;

namespace AutoRest.CSharp.V3.Utilities
{
    internal static class AzureCoreExtensions
    {
        public static string ToRequestMethodName(this RequestMethod method) => method.ToString() switch
        {
            "GET" => nameof(RequestMethod.Get),
            "POST" => nameof(RequestMethod.Post),
            "PUT" => nameof(RequestMethod.Put),
            "PATCH" => nameof(RequestMethod.Patch),
            "DELETE" => nameof(RequestMethod.Delete),
            "HEAD" => nameof(RequestMethod.Head),
            "OPTIONS" => nameof(RequestMethod.Options),
            "TRACE" => nameof(RequestMethod.Trace),
            _ => String.Empty
        };

        public static RequestMethod? ToCoreRequestMethod(this Input.HttpMethod method) => method switch
        {
            Input.HttpMethod.Delete => RequestMethod.Delete,
            Input.HttpMethod.Get => RequestMethod.Get,
            Input.HttpMethod.Head => RequestMethod.Head,
            Input.HttpMethod.Options => RequestMethod.Options,
            Input.HttpMethod.Patch => RequestMethod.Patch,
            Input.HttpMethod.Post => RequestMethod.Post,
            Input.HttpMethod.Put => RequestMethod.Put,
            Input.HttpMethod.Trace => RequestMethod.Trace,
            _ => (RequestMethod?)null
        };
    }
}
