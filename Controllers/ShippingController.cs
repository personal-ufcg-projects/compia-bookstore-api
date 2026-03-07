using Microsoft.AspNetCore.Mvc;
using CompiaBackend.DTOs;
using CompiaBackend.Services;

namespace CompiaBackend.Controllers;

[ApiController]
[Route("api/shipping")]
[Produces("application/json")]
public class ShippingController(ShippingService shippingService) : ControllerBase
{
    /// <summary>Retorna opções de frete para um CEP</summary>
    [HttpPost("quote")]
    [ProducesResponseType(typeof(ShippingQuoteResponse), 200)]
    public async Task<IActionResult> Quote([FromBody] ShippingQuoteRequest req)
    {
        var result = await shippingService.GetQuoteAsync(req);
        return Ok(result);
    }
}