namespace CompiaBackend.Models;
public class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string ProductId { get; set; } = "";
    public string ProductTitle { get; set; } = "";
    public string ProductType { get; set; } = "livro_fisico";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}