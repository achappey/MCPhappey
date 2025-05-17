namespace MCPhappey.Servers.SQL.Models;

public class PromptResourceTemplate
{
    public int PromptId { get; set; }
    public Prompt Prompt { get; set; } = null!;

    public int ResourceTemplateId { get; set; }
    public ResourceTemplate ResourceTemplate { get; set; } = null!;

    // Example optional metadata fields, remove or modify as needed:
    // public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

