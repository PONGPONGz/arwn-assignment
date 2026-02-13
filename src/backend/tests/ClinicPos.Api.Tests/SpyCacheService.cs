using ClinicPos.Api.Services;
using System.Collections.Concurrent;

namespace ClinicPos.Api.Tests;

public class SpyCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _store = new();
    private readonly ConcurrentBag<string> _invalidatedPrefixes = new();

    public IReadOnlyDictionary<string, object> Store => _store;
    public IReadOnlyCollection<string> InvalidatedPrefixes => _invalidatedPrefixes;

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_store.TryGetValue(key, out var value))
            return Task.FromResult(value as T);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task InvalidateByPrefixAsync(string prefix)
    {
        _invalidatedPrefixes.Add(prefix);

        var keysToRemove = _store.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        _store.Clear();
        while (_invalidatedPrefixes.TryTake(out _)) { }
    }
}
