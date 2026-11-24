using ConvenienceStore.Web.Modules.Promotions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConvenienceStore.Web.Modules.Promotions.Controllers;

[ApiController,Route("api/promotions")]
[Authorize(Roles=PromotionRoles.Admin)]
public sealed class PromotionHistoryApiController(PromotionUsageStore store) : ControllerBase
{
    [HttpGet("{id:long}/usage")]
    public async Task<IActionResult> Usage(long id,CancellationToken ct)=>Ok(await store.HistoryAsync(id,ct));

    // Session XSRF is enforced by the shared AUTH API filter, as for the other API mutations.
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id,CancellationToken ct,[FromQuery] long? version = null)
    {
        await store.DeleteAsync(id,ct,version);return NoContent();
    }
}
