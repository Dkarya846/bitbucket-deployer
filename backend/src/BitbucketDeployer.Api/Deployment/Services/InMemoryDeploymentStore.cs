using System.Collections.Concurrent;

namespace BitbucketDeployer.Api.Deployment.Services;

public class InMemoryDeploymentStore : IDeploymentStore
{
    private readonly ConcurrentDictionary<string, Models.Deployment> _deployments = new();
    private readonly object _sequenceLock = new();
    private string _sequenceDate = string.Empty;
    private int _sequence;

    public Models.Deployment Add(Models.Deployment deployment)
    {
        _deployments[deployment.Id] = deployment;
        return deployment;
    }

    public Models.Deployment? Get(string id) => _deployments.GetValueOrDefault(id);

    public IReadOnlyList<Models.Deployment> GetAll() =>
        _deployments.Values.OrderByDescending(d => d.CreatedAt).ToList();

    public string GenerateNextId()
    {
        lock (_sequenceLock)
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            if (today != _sequenceDate)
            {
                _sequenceDate = today;
                _sequence = 0;
            }
            _sequence++;
            return $"DEP-{today}-{_sequence:D3}";
        }
    }
}
