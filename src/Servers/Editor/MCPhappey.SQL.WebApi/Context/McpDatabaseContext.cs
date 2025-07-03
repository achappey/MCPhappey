using MCPhappey.SQL.WebApi.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.SQL.WebApi.Context;

public class McpDatabaseContext(DbContextOptions<McpDatabaseContext> options) : DbContext(options)
{
  public DbSet<Resource> Resources { get; set; } = null!;

  public DbSet<Server> Servers { get; set; } = null!;

  public DbSet<Prompt> Prompts { get; set; } = null!;

  public DbSet<PromptArgument> PromptArguments { get; set; } = null!;

  public DbSet<ResourceTemplate> ResourceTemplates { get; set; } = null!;

  public DbSet<ServerTool> Tools { get; set; } = null!;

  public DbSet<ServerOwner> ServerOwners { get; set; } = null!;

  public DbSet<ServerGroup> ServerGroups { get; set; } = null!;

  public DbSet<ServerApiKey> ServerApiKeys { get; set; } = null!;

  public DbSet<PromptResource> PromptResources { get; set; } = null!;

  public DbSet<PromptResourceTemplate> PromptResourceTemplates { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<PromptResourceTemplate>(entity =>
    {
      entity.HasKey(e => new { e.PromptId, e.ResourceTemplateId });

      entity.HasOne(e => e.Prompt)
      .WithMany(p => p.PromptResourceTemplates)
      .HasForeignKey(e => e.PromptId)
      .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(e => e.ResourceTemplate)
      .WithMany(r => r.PromptResourceTemplates)
      .HasForeignKey(e => e.ResourceTemplateId)
      .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<PromptResource>(entity =>
    {
      entity.HasKey(e => new { e.PromptId, e.ResourceId });

      entity.HasOne(e => e.Prompt)
      .WithMany(p => p.PromptResources)
      .HasForeignKey(e => e.PromptId)
      .OnDelete(DeleteBehavior.Cascade);

      entity.HasOne(e => e.Resource)
      .WithMany(r => r.PromptResources)
      .HasForeignKey(e => e.ResourceId)
      .OnDelete(DeleteBehavior.Restrict);
    });

    base.OnModelCreating(modelBuilder);
  }

}
