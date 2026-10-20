namespace Acme.Umbraco.Media;

internal sealed class AcmeMediaUrlResolver : IAcmeMediaUrlResolver
{
    public string Resolve(string mediaPath, int? width = null, int? height = null)
        => $"https://img.acme.example{mediaPath}?w={width}&h={height}";
}
