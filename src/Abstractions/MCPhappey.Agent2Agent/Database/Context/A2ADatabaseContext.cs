using MCPhappey.Agent2Agent.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Servers.SQL.Context;

public class A2ADatabaseContext(DbContextOptions<A2ADatabaseContext> options) : DbContext(options)
{
  public DbSet<Agent> Agents { get; set; } = null!;

  public DbSet<AgentCard> AgentCards { get; set; } = null!;

  public DbSet<Skill> Skills { get; set; } = null!;

  public DbSet<SkillTag> SkillTags { get; set; } = null!;

  public DbSet<McpServer> McpServers { get; set; } = null!;

  public DbSet<AgentServer> AgentServers { get; set; } = null!;

  public DbSet<AgentOwner> AgentOwners { get; set; } = null!;

  public DbSet<AppRegistration> AppRegistrations { get; set; } = null!;

  public DbSet<Tag> Tags { get; set; } = null!;

  public DbSet<SkillExample> SkillExamples { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);


    modelBuilder.Entity<Agent>(e =>
   {
     e.Navigation(x => x.Anthropic).IsRequired();   // 👈 required nav
     e.OwnsOne(x => x.Anthropic, nav =>
     {
       nav.ToJson();
       nav.OwnsOne(m => m.CodeExecution);
       nav.OwnsOne(m => m.Thinking);
       nav.OwnsOne(m => m.WebSearch);
     });
   });


    modelBuilder.Entity<Agent>(builder =>
        builder.OwnsOne(a => a.OpenAI, nav =>
            {
              nav.ToJson();                // <- key line

              // Optional: fine-tune nested owned objects (they’ll be nested JSON)
              nav.OwnsOne(m => m.CodeInterpreter);
              nav.OwnsOne(m => m.Reasoning);
              nav.OwnsOne(m => m.FileSearch);
            }));

    // === Agent ===
    modelBuilder.Entity<Agent>(builder =>
    {
      builder.HasKey(a => a.Id);

      // One-to-one relationship
      builder.HasOne(a => a.AgentCard)
            .WithOne(ac => ac.Agent)
            .HasForeignKey<AgentCard>(ac => ac.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

      builder.HasOne(a => a.AppRegistration)
        .WithOne(ar => ar.Agent)
        .HasForeignKey<AppRegistration>(ar => ar.AgentId)
        .OnDelete(DeleteBehavior.Cascade); // Or Restrict, depending on delete behavior
    });

    modelBuilder.Entity<AppRegistration>(builder =>
    {
      builder.HasKey(ar => ar.Id);

      // AgentId is also unique, since 1-to-1
      builder.HasIndex(ar => ar.AgentId).IsUnique();

      builder.Property(ar => ar.ClientId).IsRequired();
      builder.Property(ar => ar.ClientSecret).IsRequired();
      builder.Property(ar => ar.Audience).IsRequired();
      builder.Property(ar => ar.Scope).IsRequired();
    });

    modelBuilder.Entity<AgentServer>(builder =>
        {
          builder.HasKey(st => new { st.AgentId, st.McpServerId }); // Composite PK
          builder.HasOne(st => st.Agent)
                  .WithMany(s => s.Servers)
                  .HasForeignKey(st => st.McpServerId);

          builder.HasOne(st => st.McpServer)
                  .WithMany(t => t.AgentServers)
                  .HasForeignKey(st => st.AgentId);
        });

    // === AgentCard ===
    modelBuilder.Entity<AgentCard>(builder =>
    {
      builder.HasKey(ac => ac.Id);
      builder.Property(ac => ac.Name).IsRequired();
      builder.Property(ac => ac.Url).IsRequired();
      builder.Property(ac => ac.Description).IsRequired();

      builder.HasMany(ac => ac.Skills)
              .WithOne(s => s.AgentCard)
              .HasForeignKey(s => s.AgentCardId)
              .OnDelete(DeleteBehavior.Cascade);
    });

    // === Skill ===
    modelBuilder.Entity<Skill>(builder =>
    {
      builder.HasKey(s => s.Id);
      builder.Property(s => s.Name).IsRequired();
      builder.Property(s => s.Description).IsRequired();

      // One-to-many SkillExample
      builder.HasMany(s => s.Examples)
              .WithOne(e => e.Skill)
              .HasForeignKey(e => e.SkillId)
              .OnDelete(DeleteBehavior.Cascade);

      // Many-to-many with Tag via SkillTag
      builder.HasMany(s => s.SkillTags)
              .WithOne(st => st.Skill)
              .HasForeignKey(st => st.SkillId)
              .OnDelete(DeleteBehavior.Cascade);
    });

    // === Tag ===
    modelBuilder.Entity<Tag>(builder =>
    {
      builder.HasKey(t => t.Id);
      builder.Property(t => t.Value).IsRequired();

      builder.HasMany(t => t.SkillTags)
              .WithOne(st => st.Tag)
              .HasForeignKey(st => st.TagId)
              .OnDelete(DeleteBehavior.Cascade);
    });

    // === SkillTag (Join Table) ===
    modelBuilder.Entity<SkillTag>(builder =>
    {
      builder.HasKey(st => new { st.SkillId, st.TagId }); // Composite PK
      builder.HasOne(st => st.Skill)
              .WithMany(s => s.SkillTags)
              .HasForeignKey(st => st.SkillId);

      builder.HasOne(st => st.Tag)
              .WithMany(t => t.SkillTags)
              .HasForeignKey(st => st.TagId);
    });

    // === SkillExample ===
    modelBuilder.Entity<SkillExample>(builder =>
    {
      builder.HasKey(e => e.Id);
      builder.Property(e => e.Example).IsRequired();

      builder.HasOne(e => e.Skill)
              .WithMany(s => s.Examples)
              .HasForeignKey(e => e.SkillId)
              .OnDelete(DeleteBehavior.Cascade);
    });
  }

}
