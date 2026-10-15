using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Skills.Examples.BackofficeLocalization.NetLocalization;

/// <summary>
/// This example's own harness, not part of the skill.
///
/// A real user creates config/lang/ at the project root and drops the .user.xml files in. Here the host
/// project is shared, so the same files are written from the embedded copies at start-up, before the
/// text service reads its file sources. config/lang is git-ignored in the host project.
///
/// The copy of en.user.xml also gets <see cref="TestOnlyCoreOverrides"/>, so the tests can prove how a
/// core key is overridden without the published asset carrying a core rename into every project.
/// </summary>
public class NetLocalizationExampleComposer : IComposer
{
    /// <summary>
    /// Server-only core keys, so nothing an editor sees in the reference backoffice changes. The exact
    /// alias replaces the reset-password email subject; the second is a near miss of the core alias
    /// resetCodeExpired and must leave the core text alone.
    /// </summary>
    public const string TestOnlyCoreOverrides = """
        <area alias="login">
            <key alias="resetPasswordEmailCopySubject">Skills: Reset your password</key>
            <key alias="resetcodeexpired">Skills: near miss</key>
        </area>
        """;

    public void Compose(IUmbracoBuilder builder)
    {
        // The host's content root is in configuration under "contentRoot"; the builder has no hosting environment.
        var contentRoot = builder.Config["contentRoot"]
            ?? throw new InvalidOperationException("The host configuration has no contentRoot.");
        var langFolder = Path.Combine(contentRoot, "config", "lang");
        Directory.CreateDirectory(langFolder);

        foreach (var name in new[] { "en.user.xml", "da.user.xml" })
        {
            using Stream stream = typeof(NetLocalizationExampleComposer).Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Embedded asset '{name}' is missing from the example.");
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            if (name == "en.user.xml")
            {
                content = content.Replace("</language>", TestOnlyCoreOverrides + "\n</language>", StringComparison.Ordinal);
            }

            var path = Path.Combine(langFolder, name);
            if (!File.Exists(path) || File.ReadAllText(path) != content)
            {
                File.WriteAllText(path, content);
            }
        }
    }
}
