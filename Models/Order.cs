namespace CompiaBackend.Models;
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OrderNumber { get; set; } = "";

    // Cliente
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Endereço
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Estado { get; set; } = "";

    // Envio e pagamento
    public string ShippingMethod { get; set; } = "";
    public decimal ShippingPrice { get; set; }
    public string PaymentMethod { get; set; } = ""; // card | pix
    public string Status { get; set; } = "Processando";

    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}