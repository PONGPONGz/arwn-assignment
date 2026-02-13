namespace ClinicPos.Api.Services;

public class NoOpEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(string eventName, T payload) => Task.CompletedTask;
}
