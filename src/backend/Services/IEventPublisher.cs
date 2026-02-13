namespace ClinicPos.Api.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(string eventName, T payload);
}
