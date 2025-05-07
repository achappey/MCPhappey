namespace MCPhappey.Servers.SQL.Models;

public class PromptResource
{
    public int PromptId { get; set; }
    public Prompt Prompt { get; set; } = null!;

    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

}

