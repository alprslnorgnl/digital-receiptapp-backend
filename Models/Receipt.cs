using System.ComponentModel.DataAnnotations;

public class Receipt
{
    [Key]
    public int ReceiptId { get; set; }
    public int UserId { get; set; }
    public DateTime DateTime { get; set; }
    public int TotalQuantity { get; set; }
    public string? MarketName { get; set; }
    public string? MarketBranch { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool Favorite { get; set; } = false;

    public User? User { get; set; }
    public ICollection<Product>? Products { get; set; }
}
