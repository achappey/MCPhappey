namespace MCPhappey.SQL.WebApi.Models.Dto;

public class PromptArgument
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool? Required { get; set; }
}
