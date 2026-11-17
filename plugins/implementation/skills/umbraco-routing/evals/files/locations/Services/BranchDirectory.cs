namespace Acme.Web.Services;

public class BranchDirectory : IBranchDirectory
{
    private static readonly Dictionary<string, IReadOnlyList<Branch>> Branches = new(StringComparer.OrdinalIgnoreCase)
    {
        ["leeds"] = [new Branch("Leeds Central", "1 Park Row", "0113 000 0000")],
        ["york"] = [new Branch("York", "2 Coney Street", "01904 000 000")],
    };

    public Task<IReadOnlyList<Branch>?> GetBranchesAsync(string city, CancellationToken cancellationToken)
        => Task.FromResult(Branches.TryGetValue(city, out IReadOnlyList<Branch>? branches) ? branches : null);
}
