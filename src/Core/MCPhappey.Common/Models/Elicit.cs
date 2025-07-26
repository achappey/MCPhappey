using System.Text.Json.Serialization;

namespace MCPhappey.Common.Models;

public interface IHasName
{
    string? Name { get; }
}

public class ElicitDefaultData<T>
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("defaultValues")]
    public T? DefaultValues { get; set; }
}