namespace MCPhappey.SQL.WebApi.Models.Dto;

public class Server
{
    public string Name { get; set; } = null!;

    public bool Secured { get; set; }

    public object Prompts { get; set; } = new();

    public object Resources { get; set; } = new();

    public object Tools { get; set; } = new();

    public IEnumerable<string> Owners { get; set; } = [];

    public IEnumerable<string> Groups { get; set; } = [];

    public IEnumerable<string> ApiKeys { get; set; } = [];

    public string? Instructions { get; set; }
}
