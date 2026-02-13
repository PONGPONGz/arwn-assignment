using System.Text.Json;
using StackExchange.Redis;

namespace ClinicPos.Api.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}, falling through to DB", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromSeconds(300));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    public async Task InvalidateByPrefixAsync(string prefix)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = new List<RedisKey>();

                await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
                {
                    keys.Add(key);
                }

                if (keys.Count > 0)
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(keys.ToArray());
                    _logger.LogInformation("Invalidated {Count} cache keys with prefix {Prefix}", keys.Count, prefix);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis invalidation failed for prefix {Prefix}", prefix);
        }
    }
}
