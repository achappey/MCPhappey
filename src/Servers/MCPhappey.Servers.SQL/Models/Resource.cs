﻿namespace MCPhappey.Servers.SQL.Models;

public class Resource
{
    public int Id { get; set; }

    public int ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public string Uri { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public float? Priority { get; set; }

    public bool? UserAudience { get; set; }

    public bool? AssistantAudience { get; set; }

    public ICollection<PromptResource> PromptResources { get; set; } = [];
}
