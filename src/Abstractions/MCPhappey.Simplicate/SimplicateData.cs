using System.Text.Json.Serialization;

namespace MCPhappey.Simplicate;


public class SimplicateData<T>
{
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; } = default!;

    [JsonPropertyName("metadata")]
    public SimplicateMetadata? Metadata { get; set; }
}

public class SimplicateMetadata
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}