namespace MCPhappey.SQL.WebApi.Models.Dto;

public class Prompt
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string PromptTemplate { get; set; } = null!;

    public ICollection<PromptArgument> Arguments { get; set; } = [];
}
