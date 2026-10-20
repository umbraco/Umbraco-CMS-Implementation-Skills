namespace Acme.Site.Infrastructure;

public interface IRedirectCache
{
    Task LoadAllAsync();

    void Flush();

    bool TryGet(string path, out string? target);
}

public class RedirectCache : IRedirectCache
{
    private Dictionary<string, string> _redirects = new(StringComparer.OrdinalIgnoreCase);

    public Task LoadAllAsync()
    {
        // The real implementation reads the redirect table from the database.
        _redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        return Task.CompletedTask;
    }

    public void Flush() => _redirects.Clear();

    public bool TryGet(string path, out string? target) => _redirects.TryGetValue(path, out target);
}
