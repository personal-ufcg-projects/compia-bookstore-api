namespace CompiaBackend.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    // "Físico" | "E-book" | "Kit"
    public string Format { get; set; } = "Físico";
    // "Inteligência Artificial" | "Machine Learning" | "Data Science" | "Blockchain" | "Cibersegurança"
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string Image { get; set; } = "";
    public int StockCount { get; set; } = 0;
    // Caminho do PDF (apenas para E-book e Kit)
    public string? PdfPath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
