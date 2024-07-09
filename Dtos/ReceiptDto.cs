public class ReceiptDto
{
    public string? MarketName { get; set; }
    public string? MarketBranch { get; set; }
    public DateTime DateTime { get; set; }
    public int TotalQuantity { get; set; }
    public bool Favorite { get; set; } = false;
    public List<ProductDto>? Products { get; set; }
}

public class ProductDto
{
    public string? ProductName { get; set; }
    public int ProductPiece { get; set; }
    public decimal KdvRate { get; set; }
}
