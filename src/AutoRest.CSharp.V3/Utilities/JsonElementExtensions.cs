﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AutoRest.CSharp.V3.Utilities
{
    internal static class JsonElementExtensions
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var value) ? value : (JsonElement?)null;
        public static JsonProperty? GetPropertyOrNull(this IEnumerable<JsonProperty?> properties, string propertyName) =>
            properties.FirstOrDefault(p => p?.Name == propertyName);

        public static TValue ToObject<TValue>(this JsonElement element, JsonSerializerOptions? options = null) =>
#pragma warning disable CS8603 // Possible null reference return.
            JsonSerializer.Deserialize<TValue>(element.GetRawText(), options);
#pragma warning restore CS8603 // Possible null reference return.

        public static JsonElement? Parse(this string jsonText)
        {
            try { return JsonDocument.Parse(jsonText).RootElement; }
            catch { return null; }
        }

        public static JsonElement[] Unwrap(this JsonElement element) =>
            element.ValueKind == JsonValueKind.Array ? element.EnumerateArray().ToArray() : new[] { element };

        public static string[]? ToStringArray(this JsonElement? element) =>
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            element?.ValueKind == JsonValueKind.Array ? element.Value.EnumerateArray().Select(e => e.GetString()).ToArray() : null;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        public static string? ToStringValue(this JsonElement? element) =>
            element?.ValueKind == JsonValueKind.String ? element.Value.GetString() : null;
        public static int? ToNumber(this JsonElement? element) =>
            element?.ValueKind == JsonValueKind.Number ? element.Value.GetInt32() : (int?)null;
        public static bool? ToBoolean(this JsonElement? element) =>
            element?.ValueKind == JsonValueKind.True || element?.ValueKind == JsonValueKind.False ? element.Value.GetBoolean() : (bool?)null;

        public static bool IsNull(this JsonElement? element) => element?.ValueKind == JsonValueKind.Null;
        public static bool IsObject(this JsonElement? element) => element?.ValueKind == JsonValueKind.Object;

        public static T ToType<T>(this JsonElement? element) =>
            typeof(T) switch
            {
                var t when t == typeof(string) => (T)(object?)element.ToStringValue()!,
                var t when t == typeof(string[]) => (T)(object?)element.ToStringArray()!,
                var t when t == typeof(int?) => (T)(object?)element.ToNumber()!,
                var t when t == typeof(bool?) => (T)(object?)element.ToBoolean()!,
                var t when t == typeof(JsonElement?) => (T)(object?)element!,
                _ => throw new NotSupportedException($"Type {typeof(T)} is not a supported response type.")
            };
    }
}
