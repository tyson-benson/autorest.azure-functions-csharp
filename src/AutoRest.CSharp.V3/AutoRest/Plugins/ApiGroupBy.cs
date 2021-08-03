// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace AutoRest.CSharp.V3.AutoRest.Plugins
{
    internal static class ApiGroupBy
    {
        /// <summary>
        /// One file per operation organised into a folder structure that follows the operation path
        /// </summary>
        /// <example>
        /// /my/api/operation
        ///   operationId: myOperation
        ///
        /// Results in /my/api/operation/MyOperationApi.cs
        /// </example>
        internal const string Operation = "operation";
        /// <summary>
        /// One file per operation flattened into the root of the project
        /// </summary>
        /// <example>operationId: myOperation, results in /MyOperationApi.cs</example>
        internal const string OperationFlat = "operation-flat";
        /// <summary>
        /// One file per operation group, denoted by operationId prefixes delimited by an underscore '_'
        /// </summary>
        /// <example>operationId: admin_doSomething, results in AdminApi.cs with a 'doSomething' function</example>
        internal const string OperationGroup = "operation-group";
        /// <summary>
        /// Default - group operations by the first segment of the path
        /// </summary>
        /// <example>/my/api/operation, results in MyApi.cs</example>
        internal const string FirstPathSegment = "first-path-segment";
        /// <summary>
        /// Group operations by the last segment of the path
        /// </summary>
        /// <example>/admin/secrets, results in SecretsApi.cs</example>
        internal const string LastPathSegment = "last-path-segment";
    }
}
