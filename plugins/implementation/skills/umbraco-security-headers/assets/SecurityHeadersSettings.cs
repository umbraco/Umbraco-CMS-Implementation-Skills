namespace <Namespace>;

/// <summary>Bound from the "SecurityHeaders" section of appsettings.json.</summary>
public class SecurityHeadersSettings
{
    public const string SectionName = "SecurityHeaders";

    /// <summary>
    /// Send Content-Security-Policy-Report-Only instead of the enforcing header. Turn it on for
    /// the first rollout, read the violations in the browser console, then turn it off.
    /// </summary>
    public bool ReportOnly { get; set; }
}
