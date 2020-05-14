// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using body_complex.Models;

namespace body_complex
{
    internal partial class PolymorphicrecursiveRestClient
    {
        private Uri endpoint;
        private ClientDiagnostics _clientDiagnostics;
        private HttpPipeline _pipeline;

        /// <summary> Initializes a new instance of PolymorphicrecursiveRestClient. </summary>
        public PolymorphicrecursiveRestClient(ClientDiagnostics clientDiagnostics, HttpPipeline pipeline, Uri endpoint = null)
        {
            endpoint ??= new Uri("http://localhost:3000");

            this.endpoint = endpoint;
            _clientDiagnostics = clientDiagnostics;
            _pipeline = pipeline;
        }

        internal HttpMessage CreateGetValidRequest()
        {
            var message = _pipeline.CreateMessage();
            var request = message.Request;
            request.Method = RequestMethod.Get;
            var uri = new RawRequestUriBuilder();
            uri.Reset(endpoint);
            uri.AppendPath("/complex/polymorphicrecursive/valid", false);
            request.Uri = uri;
            return message;
        }

        /// <summary> Get complex types that are polymorphic and have recursive references. </summary>
        /// <param name="cancellationToken"> The cancellation token to use. </param>
        public async ValueTask<Response<Fish>> GetValidAsync(CancellationToken cancellationToken = default)
        {
            using var message = CreateGetValidRequest();
            await _pipeline.SendAsync(message, cancellationToken).ConfigureAwait(false);
            switch (message.Response.Status)
            {
                case 200:
                    {
                        Fish value = default;
                        using var document = await JsonDocument.ParseAsync(message.Response.ContentStream, default, cancellationToken).ConfigureAwait(false);
                        if (document.RootElement.ValueKind == JsonValueKind.Null)
                        {
                            value = null;
                        }
                        else
                        {
                            value = Fish.DeserializeFish(document.RootElement);
                        }
                        return Response.FromValue(value, message.Response);
                    }
                default:
                    throw await _clientDiagnostics.CreateRequestFailedExceptionAsync(message.Response).ConfigureAwait(false);
            }
        }

        /// <summary> Get complex types that are polymorphic and have recursive references. </summary>
        /// <param name="cancellationToken"> The cancellation token to use. </param>
        public Response<Fish> GetValid(CancellationToken cancellationToken = default)
        {
            using var message = CreateGetValidRequest();
            _pipeline.Send(message, cancellationToken);
            switch (message.Response.Status)
            {
                case 200:
                    {
                        Fish value = default;
                        using var document = JsonDocument.Parse(message.Response.ContentStream);
                        if (document.RootElement.ValueKind == JsonValueKind.Null)
                        {
                            value = null;
                        }
                        else
                        {
                            value = Fish.DeserializeFish(document.RootElement);
                        }
                        return Response.FromValue(value, message.Response);
                    }
                default:
                    throw _clientDiagnostics.CreateRequestFailedException(message.Response);
            }
        }

        internal HttpMessage CreatePutValidRequest(Fish complexBody)
        {
            var message = _pipeline.CreateMessage();
            var request = message.Request;
            request.Method = RequestMethod.Put;
            var uri = new RawRequestUriBuilder();
            uri.Reset(endpoint);
            uri.AppendPath("/complex/polymorphicrecursive/valid", false);
            request.Uri = uri;
            request.Headers.Add("Content-Type", "application/json");
            using var content = new Utf8JsonRequestContent();
            content.JsonWriter.WriteObjectValue(complexBody);
            request.Content = content;
            return message;
        }

        /// <summary> Put complex types that are polymorphic and have recursive references. </summary>
        /// <param name="complexBody">
        /// Please put a salmon that looks like this:
        /// {
        ///     &quot;fishtype&quot;: &quot;salmon&quot;,
        ///     &quot;species&quot;: &quot;king&quot;,
        ///     &quot;length&quot;: 1,
        ///     &quot;age&quot;: 1,
        ///     &quot;location&quot;: &quot;alaska&quot;,
        ///     &quot;iswild&quot;: true,
        ///     &quot;siblings&quot;: [
        ///         {
        ///             &quot;fishtype&quot;: &quot;shark&quot;,
        ///             &quot;species&quot;: &quot;predator&quot;,
        ///             &quot;length&quot;: 20,
        ///             &quot;age&quot;: 6,
        ///             &quot;siblings&quot;: [
        ///                 {
        ///                     &quot;fishtype&quot;: &quot;salmon&quot;,
        ///                     &quot;species&quot;: &quot;coho&quot;,
        ///                     &quot;length&quot;: 2,
        ///                     &quot;age&quot;: 2,
        ///                     &quot;location&quot;: &quot;atlantic&quot;,
        ///                     &quot;iswild&quot;: true,
        ///                     &quot;siblings&quot;: [
        ///                         {
        ///                             &quot;fishtype&quot;: &quot;shark&quot;,
        ///                             &quot;species&quot;: &quot;predator&quot;,
        ///                             &quot;length&quot;: 20,
        ///                             &quot;age&quot;: 6
        ///                         },
        ///                         {
        ///                             &quot;fishtype&quot;: &quot;sawshark&quot;,
        ///                             &quot;species&quot;: &quot;dangerous&quot;,
        ///                             &quot;length&quot;: 10,
        ///                             &quot;age&quot;: 105
        ///                         }
        ///                     ]
        ///                 },
        ///                 {
        ///                     &quot;fishtype&quot;: &quot;sawshark&quot;,
        ///                     &quot;species&quot;: &quot;dangerous&quot;,
        ///                     &quot;length&quot;: 10,
        ///                     &quot;age&quot;: 105
        ///                 }
        ///             ]
        ///         },
        ///         {
        ///             &quot;fishtype&quot;: &quot;sawshark&quot;,
        ///             &quot;species&quot;: &quot;dangerous&quot;,
        ///             &quot;length&quot;: 10,
        ///             &quot;age&quot;: 105
        ///         }
        ///     ]
        /// }.
        /// </param>
        /// <param name="cancellationToken"> The cancellation token to use. </param>
        public async ValueTask<Response> PutValidAsync(Fish complexBody, CancellationToken cancellationToken = default)
        {
            if (complexBody == null)
            {
                throw new ArgumentNullException(nameof(complexBody));
            }

            using var message = CreatePutValidRequest(complexBody);
            await _pipeline.SendAsync(message, cancellationToken).ConfigureAwait(false);
            switch (message.Response.Status)
            {
                case 200:
                    return message.Response;
                default:
                    throw await _clientDiagnostics.CreateRequestFailedExceptionAsync(message.Response).ConfigureAwait(false);
            }
        }

        /// <summary> Put complex types that are polymorphic and have recursive references. </summary>
        /// <param name="complexBody">
        /// Please put a salmon that looks like this:
        /// {
        ///     &quot;fishtype&quot;: &quot;salmon&quot;,
        ///     &quot;species&quot;: &quot;king&quot;,
        ///     &quot;length&quot;: 1,
        ///     &quot;age&quot;: 1,
        ///     &quot;location&quot;: &quot;alaska&quot;,
        ///     &quot;iswild&quot;: true,
        ///     &quot;siblings&quot;: [
        ///         {
        ///             &quot;fishtype&quot;: &quot;shark&quot;,
        ///             &quot;species&quot;: &quot;predator&quot;,
        ///             &quot;length&quot;: 20,
        ///             &quot;age&quot;: 6,
        ///             &quot;siblings&quot;: [
        ///                 {
        ///                     &quot;fishtype&quot;: &quot;salmon&quot;,
        ///                     &quot;species&quot;: &quot;coho&quot;,
        ///                     &quot;length&quot;: 2,
        ///                     &quot;age&quot;: 2,
        ///                     &quot;location&quot;: &quot;atlantic&quot;,
        ///                     &quot;iswild&quot;: true,
        ///                     &quot;siblings&quot;: [
        ///                         {
        ///                             &quot;fishtype&quot;: &quot;shark&quot;,
        ///                             &quot;species&quot;: &quot;predator&quot;,
        ///                             &quot;length&quot;: 20,
        ///                             &quot;age&quot;: 6
        ///                         },
        ///                         {
        ///                             &quot;fishtype&quot;: &quot;sawshark&quot;,
        ///                             &quot;species&quot;: &quot;dangerous&quot;,
        ///                             &quot;length&quot;: 10,
        ///                             &quot;age&quot;: 105
        ///                         }
        ///                     ]
        ///                 },
        ///                 {
        ///                     &quot;fishtype&quot;: &quot;sawshark&quot;,
        ///                     &quot;species&quot;: &quot;dangerous&quot;,
        ///                     &quot;length&quot;: 10,
        ///                     &quot;age&quot;: 105
        ///                 }
        ///             ]
        ///         },
        ///         {
        ///             &quot;fishtype&quot;: &quot;sawshark&quot;,
        ///             &quot;species&quot;: &quot;dangerous&quot;,
        ///             &quot;length&quot;: 10,
        ///             &quot;age&quot;: 105
        ///         }
        ///     ]
        /// }.
        /// </param>
        /// <param name="cancellationToken"> The cancellation token to use. </param>
        public Response PutValid(Fish complexBody, CancellationToken cancellationToken = default)
        {
            if (complexBody == null)
            {
                throw new ArgumentNullException(nameof(complexBody));
            }

            using var message = CreatePutValidRequest(complexBody);
            _pipeline.Send(message, cancellationToken);
            switch (message.Response.Status)
            {
                case 200:
                    return message.Response;
                default:
                    throw _clientDiagnostics.CreateRequestFailedException(message.Response);
            }
        }
    }
}
