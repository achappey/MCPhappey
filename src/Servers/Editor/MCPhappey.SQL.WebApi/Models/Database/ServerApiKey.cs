using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.SQL.WebApi.Models.Database;

[PrimaryKey(nameof(Id), nameof(ServerId))]
public class ServerApiKey
{
  [Column(Order = 1)]
  public string Id { get; set; } = null!;

  public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

  [Column(Order = 2)]
  public int ServerId { get; set; }

  public Server Server { get; set; } = null!;

}
