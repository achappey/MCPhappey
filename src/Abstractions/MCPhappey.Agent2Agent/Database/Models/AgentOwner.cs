﻿using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Agent2Agent.Database.Models;

[PrimaryKey(nameof(Id), nameof(AgentId))]
public class AgentOwner
{
    [Column(Order = 1)]
    public string Id { get; set; } = null!;

    [Column(Order = 2)]
    public int AgentId { get; set; }

    [ForeignKey("AgentId")]
    public Agent Agent { get; set; } = null!;
}
