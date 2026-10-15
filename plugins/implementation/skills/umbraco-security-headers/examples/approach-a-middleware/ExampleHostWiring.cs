using Microsoft.AspNetCore.Mvc;

namespace Umbraco.Skills.Examples.SecurityHeaders;

// VALIDATION-ONLY harness — NOT generated from the skill's assets/ and NOT part of what the
// skill ships to users.
//
// The skill tells users to tag their own inline scripts with Umbraco's asp-csp-nonce tag helper
// (after registering it with @addTagHelper *, Umbraco.Web.Common). This endpoint renders a
// precompiled view that does exactly that, so the fixture can check the nonce the tag helper
// writes is the one the middleware put in the CSP header.
[ApiExplorerSettings(IgnoreApi = true)]
public class SecurityHeadersExampleController : Controller
{
    [HttpGet]
    [Route("example/security-headers/nonce-probe")]
    public IActionResult NonceProbe() => View("~/Views/SecurityHeadersExample/NonceProbe.cshtml");
}
