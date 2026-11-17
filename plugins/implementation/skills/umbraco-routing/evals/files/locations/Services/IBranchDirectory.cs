namespace Acme.Web.Services;

public record Branch(string Name, string Address, string Phone);

/// <summary>Branch data from the company's branch API. Already registered in DI.</summary>
public interface IBranchDirectory
{
    /// <summary>Returns null for a city with no branches.</summary>
    Task<IReadOnlyList<Branch>?> GetBranchesAsync(string city, CancellationToken cancellationToken);
}
