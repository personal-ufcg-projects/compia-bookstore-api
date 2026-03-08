using Microsoft.EntityFrameworkCore;
using CompiaBackend.Data;
using CompiaBackend.DTOs;
using CompiaBackend.Models;

namespace CompiaBackend.Services;

public class ProductService(AppDbContext db, ILogger<ProductService> logger)
{
    // ── Helpers ───────────────────────────────────────────────────
    private static ProductResponse ToResponse(Product p) => new(
        p.Id.ToString(),
        p.Title,
        p.Author,
        p.Description,
        p.Format,
        p.Category,
        p.Price,
        p.OriginalPrice,
        p.Image,
        p.StockCount,
        InStock: p.Format == "E-book" || p.StockCount > 0,
        HasPdf:  p.PdfPath != null,
        IsActive: p.IsActive
    );

    // ── Público ───────────────────────────────────────────────────
    public async Task<List<ProductResponse>> GetAllAsync()
    {
        return await db.Products
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductResponse(
                p.Id.ToString(), p.Title, p.Author, p.Description,
                p.Format, p.Category, p.Price, p.OriginalPrice,
                p.Image, p.StockCount, p.Format == "E-book" || p.StockCount > 0, p.PdfPath != null, p.IsActive
            ))
            .ToListAsync();
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        var p = await db.Products.FindAsync(id);
        return p is null || !p.IsActive ? null : ToResponse(p);
    }

    // ── Admin ─────────────────────────────────────────────────────
    public async Task<List<ProductResponse>> GetAllAdminAsync()
    {
        return await db.Products
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductResponse(
                p.Id.ToString(), p.Title, p.Author, p.Description,
                p.Format, p.Category, p.Price, p.OriginalPrice,
                p.Image, p.StockCount, p.Format == "E-book" || p.StockCount > 0, p.PdfPath != null, p.IsActive
            ))
            .ToListAsync();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest req, Guid adminId)
    {
        var product = new Product
        {
            Title         = req.Title,
            Author        = req.Author,
            Description   = req.Description,
            Format        = req.Format,
            Category      = req.Category,
            Price         = req.Price,
            OriginalPrice = req.OriginalPrice,
            Image         = req.Image,
            StockCount    = req.StockCount,
        };

        db.Products.Add(product);
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId     = adminId,
            Action     = "product_created",
            EntityType = "product",
            EntityId   = product.Id.ToString(),
            Details    = $"{{\"title\":\"{req.Title}\",\"format\":\"{req.Format}\"}}"
        });

        await db.SaveChangesAsync();
        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(Guid id, UpdateProductRequest req, Guid adminId)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return null;

        product.Title         = req.Title;
        product.Author        = req.Author;
        product.Description   = req.Description;
        product.Format        = req.Format;
        product.Category      = req.Category;
        product.Price         = req.Price;
        product.OriginalPrice = req.OriginalPrice;
        product.Image         = req.Image;
        product.StockCount    = req.StockCount;
        product.IsActive      = req.IsActive;
        product.UpdatedAt     = DateTime.UtcNow;

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId     = adminId,
            Action     = "product_updated",
            EntityType = "product",
            EntityId   = id.ToString(),
            Details    = $"{{\"title\":\"{req.Title}\",\"stock\":{req.StockCount}}}"
        });

        await db.SaveChangesAsync();
        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid adminId)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return false;

        // Soft delete — preserva histórico de pedidos
        product.IsActive  = false;
        product.UpdatedAt = DateTime.UtcNow;

        db.ActivityLogs.Add(new ActivityLog
        {
            UserId     = adminId,
            Action     = "product_deleted",
            EntityType = "product",
            EntityId   = id.ToString(),
        });

        await db.SaveChangesAsync();
        return true;
    }

    // ── Upload de PDF ─────────────────────────────────────────────
    public async Task<string?> SavePdfAsync(Guid productId, IFormFile file)
    {
        var product = await db.Products.FindAsync(productId);
        if (product is null) return null;

        // Só E-book e Kit têm PDF
        if (product.Format == "Físico")
            throw new InvalidOperationException("Livros físicos não têm PDF.");

        var uploadsDir = Path.Combine("wwwroot", "pdfs");
        Directory.CreateDirectory(uploadsDir);

        // Apaga PDF anterior se existir
        if (product.PdfPath != null)
        {
            var oldPath = Path.Combine("wwwroot", product.PdfPath.TrimStart('/'));
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        var fileName  = $"{productId}_{Guid.NewGuid():N}.pdf";
        var fullPath  = Path.Combine(uploadsDir, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        product.PdfPath   = $"/pdfs/{fileName}";
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("PDF salvo para produto {Id}: {Path}", productId, product.PdfPath);
        return product.PdfPath;
    }

    // ── Decrementar estoque (chamado pelo OrderService) ───────────
    public async Task DecrementStockAsync(string productId, int quantity)
    {
        if (!Guid.TryParse(productId, out var guid)) return;

        var product = await db.Products.FindAsync(guid);
        if (product is null) return;

        // E-books têm estoque "infinito" — não decrementa
        if (product.Format == "E-book") return;

        product.StockCount = Math.Max(0, product.StockCount - quantity);
        product.UpdatedAt  = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("Estoque do produto {Id} decrementado em {Qty}. Novo estoque: {Stock}",
            productId, quantity, product.StockCount);
    }
}