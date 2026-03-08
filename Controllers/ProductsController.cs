using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CompiaBackend.DTOs;
using CompiaBackend.Services;

namespace CompiaBackend.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(ProductService productService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── Público ───────────────────────────────────────────────────

    /// <summary>Lista todos os produtos ativos (catálogo público)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProductResponse>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var products = await productService.GetAllAsync();
        return Ok(products);
    }

    /// <summary>Busca produto por ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    // ── Admin ─────────────────────────────────────────────────────

    /// <summary>Lista todos os produtos incluindo inativos (admin)</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllAdmin()
    {
        var products = await productService.GetAllAdminAsync();
        return Ok(products);
    }

    /// <summary>Cria novo produto (admin)</summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ProductResponse), 201)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var product = await productService.CreateAsync(req, UserId);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Atualiza produto (admin)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(typeof(ProductResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest req)
    {
        var product = await productService.UpdateAsync(id, req, UserId);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Remove produto (soft delete, admin)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await productService.DeleteAsync(id, UserId);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Upload de PDF para E-book ou Kit (admin)</summary>
    [HttpPost("{id:guid}/pdf")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UploadPdf(Guid id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Nenhum arquivo enviado." });

        if (!file.ContentType.Contains("pdf"))
            return BadRequest(new { message = "Apenas arquivos PDF são aceitos." });

        if (file.Length > 100 * 1024 * 1024) // 100 MB
            return BadRequest(new { message = "Arquivo muito grande. Máximo: 100 MB." });

        try
        {
            var path = await productService.SavePdfAsync(id, file);
            if (path is null) return NotFound();
            return Ok(new { pdfPath = path });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}