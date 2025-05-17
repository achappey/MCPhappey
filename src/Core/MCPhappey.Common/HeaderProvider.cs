namespace MCPhappey.Common;

public class HeaderProvider
{
    public Dictionary<string, string>? Headers { get; set; }

    public string? Authorization =>
        Headers?.TryGetValue("Authorization", out var value) == true ? value : null;

    public string? Bearer =>
        Authorization?.Split(" ").LastOrDefault();

}