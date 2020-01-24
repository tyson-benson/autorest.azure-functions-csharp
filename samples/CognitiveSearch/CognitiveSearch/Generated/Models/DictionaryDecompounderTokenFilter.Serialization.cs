// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using Azure.Core;

namespace CognitiveSearch.Models
{
    public partial class DictionaryDecompounderTokenFilter : IUtf8JsonSerializable
    {
        void IUtf8JsonSerializable.Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("wordList");
            writer.WriteStartArray();
            foreach (var item in WordList)
            {
                writer.WriteStringValue(item);
            }
            writer.WriteEndArray();
            if (MinWordSize != null)
            {
                writer.WritePropertyName("minWordSize");
                writer.WriteNumberValue(MinWordSize.Value);
            }
            if (MinSubwordSize != null)
            {
                writer.WritePropertyName("minSubwordSize");
                writer.WriteNumberValue(MinSubwordSize.Value);
            }
            if (MaxSubwordSize != null)
            {
                writer.WritePropertyName("maxSubwordSize");
                writer.WriteNumberValue(MaxSubwordSize.Value);
            }
            if (OnlyLongestMatch != null)
            {
                writer.WritePropertyName("onlyLongestMatch");
                writer.WriteBooleanValue(OnlyLongestMatch.Value);
            }
            writer.WritePropertyName("@odata.type");
            writer.WriteStringValue(OdataType);
            writer.WritePropertyName("name");
            writer.WriteStringValue(Name);
            writer.WriteEndObject();
        }
        internal static DictionaryDecompounderTokenFilter DeserializeDictionaryDecompounderTokenFilter(JsonElement element)
        {
            DictionaryDecompounderTokenFilter result = new DictionaryDecompounderTokenFilter();
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("wordList"))
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        result.WordList.Add(item.GetString());
                    }
                    continue;
                }
                if (property.NameEquals("minWordSize"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }
                    result.MinWordSize = property.Value.GetInt32();
                    continue;
                }
                if (property.NameEquals("minSubwordSize"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }
                    result.MinSubwordSize = property.Value.GetInt32();
                    continue;
                }
                if (property.NameEquals("maxSubwordSize"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }
                    result.MaxSubwordSize = property.Value.GetInt32();
                    continue;
                }
                if (property.NameEquals("onlyLongestMatch"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }
                    result.OnlyLongestMatch = property.Value.GetBoolean();
                    continue;
                }
                if (property.NameEquals("@odata.type"))
                {
                    result.OdataType = property.Value.GetString();
                    continue;
                }
                if (property.NameEquals("name"))
                {
                    result.Name = property.Value.GetString();
                    continue;
                }
            }
            return result;
        }
    }
}