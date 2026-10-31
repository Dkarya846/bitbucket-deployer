using Microsoft.EntityFrameworkCore;
using BitbucketDeployer.Api.Deployment.Services;

namespace BitbucketDeployer.Api.Deployment.Persistence;

/// <summary>
/// Scoped, not singleton — unlike InMemoryDeploymentStore, persistence itself is what survives
/// across requests/restarts here, so there's no need for a shared in-process instance. Each
/// request gets its own DeploymentDbContext (also scoped), and Get/SaveAsync operate on the same
/// tracked entity graph within that request.
/// </summary>
public class SqliteDeploymentStore(DeploymentDbContext context) : IDeploymentStore
{
    public Models.Deployment Add(Models.Deployment deployment)
    {
        context.Deployments.Add(deployment);
        context.SaveChanges();
        return deployment;
    }

    public Models.Deployment? Get(string id) =>
        context.Deployments.FirstOrDefault(d => d.Id == id);

    public IReadOnlyList<Models.Deployment> GetAll() =>
        // EF's SQLite provider can't translate ORDER BY on DateTimeOffset server-side — sorting
        // client-side is fine at this app's scale (a small internal deployment history).
        context.Deployments.AsEnumerable().OrderByDescending(d => d.CreatedAt).ToList();

    public string GenerateNextId()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var prefix = $"DEP-{today}-";

        // Ids aren't a SQL-translatable numeric column, so pull today's ids and parse client-side.
        var maxSequence = context.Deployments
            .Select(d => d.Id)
            .AsEnumerable()
            .Where(id => id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(id => int.TryParse(id.AsSpan(prefix.Length), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{maxSequence + 1:D3}";
    }

    public Task SaveAsync(Models.Deployment deployment, CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
