using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Web.Common.Filters;

namespace Contoso.Web.Controllers;

[ApiController]
[Route("api/partner/orders")]
[UmbracoMemberAuthorize("Partners", "", "")] // only the Partners member group
public class PartnerOrdersController : ControllerBase
{
    private readonly IMemberManager _memberManager;
    private readonly IOrderRepository _orders;

    public PartnerOrdersController(IMemberManager memberManager, IOrderRepository orders)
    {
        _memberManager = memberManager;
        _orders = orders;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string memberEmail)
    {
        // The front end passes the logged-in partner's email
        return Ok(await _orders.GetForMemberAsync(memberEmail));
    }
}
