using System.ComponentModel.DataAnnotations;

public class Product
{
    [Key]
    public int ItemId { get; set; }
    public int ReceiptId { get; set; }
    public string? ProductName { get; set; }
    public int ProductPiece { get; set; }
    public decimal KdvRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    //Bir Product nesnesi bir adet Receipt e sahiptir
    public Receipt? Receipt { get; set; }
}
