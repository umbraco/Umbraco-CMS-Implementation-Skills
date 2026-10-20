namespace Acme.Web.Services;

public class MailingListClient : IMailingListClient
{
    public Task SubscribeAsync(string email, string? firstName, CancellationToken ct) =>
        Task.Delay(50, ct); // stands in for the provider's API call
}
