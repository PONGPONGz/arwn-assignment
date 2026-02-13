namespace ClinicPos.Api.Services;

public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class => Task.CompletedTask;

    public Task InvalidateByPrefixAsync(string prefix) => Task.CompletedTask;
}
