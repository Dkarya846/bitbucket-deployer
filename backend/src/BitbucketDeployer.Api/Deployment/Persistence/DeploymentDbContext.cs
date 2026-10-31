using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BitbucketDeployer.Api.Auth.Models;

namespace BitbucketDeployer.Api.Deployment.Persistence;

/// <summary>
/// SQLite-backed persistence for deployments and OAuth sessions. Deployment is the aggregate
/// root; DeploymentItem is stored as an owned collection (its own table, keyed by a shadow Id +
/// the owner's Id as FK) — there's no reason for items to be queried independently of their
/// parent deployment. OAuthSession is unrelated but shares the same DB file for simplicity.
/// </summary>
public class DeploymentDbContext(DbContextOptions<DeploymentDbContext> options) : DbContext(options)
{
    public DbSet<Models.Deployment> Deployments => Set<Models.Deployment>();
    public DbSet<OAuthSession> OAuthSessions => Set<OAuthSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OAuthSession>(session => session.HasKey(s => s.Id));

        modelBuilder.Entity<Models.Deployment>(deployment =>
        {
            deployment.HasKey(d => d.Id);
            deployment.Property(d => d.Status).HasConversion<string>();

            deployment.OwnsMany(d => d.Items, item =>
            {
                // DeploymentItem.Id is assigned in application code (see that class), not
                // DB-generated — SQLite doesn't auto-increment a composite (owner + item) key.
                item.HasKey(i => i.Id);
                item.Property(i => i.Status).HasConversion<string>();

                // Dictionary<string,string>? has no native SQLite column type — round-trip it as JSON.
                item.Property(i => i.Variables).HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null));
            });
        });
    }
}
