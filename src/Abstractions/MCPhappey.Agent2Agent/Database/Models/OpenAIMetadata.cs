
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Agent2Agent.Database.Models;

public enum ReasoningEffort
{
    [EnumMember(Value = "minimal")]
    minimal,
    [EnumMember(Value = "low")]
    low,
    [EnumMember(Value = "medium")]
    medium,
    [EnumMember(Value = "high")]
    high
}


public enum ReasoningSummary
{
    [EnumMember(Value = "auto")]
    auto,
    [EnumMember(Value = "concise")]
    concise,
    [EnumMember(Value = "detailed")]
    detailed
}
public class Reasoning
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("effort")]
    public ReasoningEffort? Effort { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("summary")]
    public ReasoningSummary? Summary { get; set; }
}
[Owned]
public class CodeInterpreter
{
    [JsonPropertyName("container")]
    public AutoContainer Container { get; set; } = new();
}
[Owned]
public class AutoContainer
{
    [JsonPropertyName("type")]
    public string? Type { get; set; } = "auto";
}

[Owned]
public class FileSearch
{
    [JsonPropertyName("vector_store_ids")]
    public IEnumerable<string>? VectorStoreIds { get; set; }
}

[Owned]
public class WebSearchPreview
{
    [JsonPropertyName("search_context_size")]
    public ContextSize? SearchContextSize { get; set; } = ContextSize.medium;
}


public enum ContextSize
{
    [EnumMember(Value = "low")]
    low,
    [EnumMember(Value = "medium")]
    medium,
    [EnumMember(Value = "high")]
    high
}
