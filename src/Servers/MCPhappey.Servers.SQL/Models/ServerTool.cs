﻿using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Servers.SQL.Models;

[PrimaryKey(nameof(Name), nameof(ServerId))]
public class ServerTool
{
    [Column(Order = 1)]
    public string Name { get; set; } = null!;

    [Column(Order = 2)]
    public int ServerId { get; set; }

    [ForeignKey("ServerId")]
    public Server Server { get; set; } = null!;

}
