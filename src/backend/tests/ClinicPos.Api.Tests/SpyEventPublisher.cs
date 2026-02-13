using ClinicPos.Api.Services;

namespace ClinicPos.Api.Tests;

public class SpyEventPublisher : IEventPublisher
{
    public List<(string EventName, object Payload)> PublishedEvents { get; } = [];

    public Task PublishAsync<T>(string eventName, T payload)
    {
        PublishedEvents.Add((eventName, payload!));
        return Task.CompletedTask;
    }
}
