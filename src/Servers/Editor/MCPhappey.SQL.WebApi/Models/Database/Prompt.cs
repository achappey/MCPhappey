namespace MCPhappey.SQL.WebApi.Models.Database;

public class Prompt
{
    public int Id { get; set; }

    public int ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string PromptTemplate { get; set; } = null!;

    public ICollection<PromptArgument> Arguments { get; set; } = [];

    public ICollection<PromptResource> PromptResources { get; set; } = [];

    public ICollection<PromptResourceTemplate> PromptResourceTemplates { get; set; } = [];

}
