namespace Acme.Web.Services;

/// <summary>Talks to the mailing-list provider. Registered in DI by MailingListComposer.</summary>
public interface IMailingListClient
{
    Task SubscribeAsync(string email, string? firstName, CancellationToken ct);
}
