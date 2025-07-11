﻿using Microsoft.EntityFrameworkCore;

namespace MCPhappey.SQL.WebApi.Models.Database;

[Index(nameof(Name), IsUnique = true)]
public class Server
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool Secured { get; set; }

    public ICollection<Prompt> Prompts { get; set; } = [];

    public ICollection<Resource> Resources { get; set; } = [];

    public ICollection<ResourceTemplate> ResourceTemplates { get; set; } = [];

    public ICollection<ServerOwner> Owners { get; set; } = [];

    public ICollection<ServerGroup> Groups { get; set; } = [];

    public ICollection<ServerTool> Tools { get; set; } = [];

    public ICollection<ServerApiKey> ApiKeys { get; set; } = [];

    public string? Instructions { get; set; }
}
