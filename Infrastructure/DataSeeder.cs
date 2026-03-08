using CompiaBackend.Data;
using CompiaBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace CompiaBackend.Infrastructure;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Só roda se não houver nenhum produto ainda
        if (await db.Products.AnyAsync()) return;

        var products = new List<Product>
        {
            // 1. E-book — já tem PDF (você faz upload pelo painel admin depois)
            new()
            {
                Title       = "Fundamentos de Inteligência Artificial",
                Author      = "Dr. Carlos Mendes",
                Description = "Uma introdução completa aos conceitos fundamentais de IA, "
                            + "cobrindo aprendizado de máquina, redes neurais e aplicações práticas.",
                Format      = "E-book",
                Category    = "Inteligência Artificial",
                Price       = 64.90m,
                OriginalPrice = 89.90m,
                Image       = "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=400&h=560&fit=crop",
                StockCount  = 0,      // E-books não usam estoque
                IsActive    = true,
                PdfPath     = null,   // faça upload pelo painel /admin/catalogo
            },

            // 2. Livro físico
            new()
            {
                Title       = "Cibersegurança Moderna",
                Author      = "Fernanda Costa",
                Description = "Técnicas avançadas de proteção digital para profissionais e entusiastas.",
                Format      = "Físico",
                Category    = "Cibersegurança",
                Price       = 79.90m,
                Image       = "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=400&h=560&fit=crop",
                StockCount  = 20,
                IsActive    = true,
                PdfPath     = null,
            },

            // 3. Kit — livro físico + e-book (tem estoque E tem PDF)
            new()
            {
                Title       = "Kit Machine Learning Completo",
                Author      = "COMPIA Editora",
                Description = "Livro físico + e-book + exercícios práticos. "
                            + "Tudo que você precisa para dominar Machine Learning.",
                Format      = "Kit",
                Category    = "Machine Learning",
                Price       = 149.90m,
                OriginalPrice = 199.90m,
                Image       = "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?w=400&h=560&fit=crop",
                StockCount  = 10,     // Kit tem estoque físico
                IsActive    = true,
                PdfPath     = null,   // faça upload pelo painel /admin/catalogo
            },
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}