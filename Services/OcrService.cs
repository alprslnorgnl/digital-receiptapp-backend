using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOcrService
{
    Task<ReceiptDto> ExtractReceiptData(byte[] imageData);
}

public class OcrService : IOcrService
{
    public async Task<ReceiptDto> ExtractReceiptData(byte[] imageData)
    {
        // Bu kısımda OCR işlemi yapılmalı. Örneğin, Tesseract gibi bir kütüphane kullanılabilir.
        // Burada basit bir örnek olarak dummy data döndürüyoruz.

        await Task.Delay(1000); // OCR işlemi simülasyonu

        // Dummy data
        return new ReceiptDto
        {
            MarketName = "Bim",
            MarketBranch = "Bulvar Şubesi",
            DateTime = DateTime.Now,
            TotalQuantity = 101,
            Products = new List<ProductDto>
            {
                new ProductDto { ProductName = "Çikolata", ProductPiece = 1, KdvRate = 18 },
                new ProductDto { ProductName = "Yoğurt", ProductPiece = 1, KdvRate = 8 },
                new ProductDto { ProductName = "Makarna", ProductPiece = 4, KdvRate = 8 }
            }
        };
    }
}
