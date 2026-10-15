namespace Acme.Umbraco.Media;

/// <summary>Turns a media path into the public URL the site renders. Acme's views and tag helpers call it.</summary>
public interface IAcmeMediaUrlResolver
{
    string Resolve(string mediaPath, int? width = null, int? height = null);
}
