namespace MCPhappey.Servers.SQL.Models;

public class ResourceTemplate
{
    public int Id { get; set; }

    public int ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public string TemplateUri { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<PromptResourceTemplate> PromptResourceTemplates { get; set; } = [];
}
