// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;

// public interface IOcrService
// {
//     Task<ReceiptDto> ExtractReceiptData(byte[] imageData);
// }

// public class OcrService : IOcrService
// {
//     public async Task<ReceiptDto> ExtractReceiptData(byte[] imageData)
//     {
//         // Bu kısımda OCR işlemi yapılmalı. Örneğin, Tesseract gibi bir kütüphane kullanılabilir.
//         // Burada basit bir örnek olarak dummy data döndürüyoruz.

//         await Task.Delay(1000); // OCR işlemi simülasyonu

//         // Dummy data
//         return new ReceiptDto
//         {
//             MarketName = "Bim",
//             MarketBranch = "Bulvar Şubesi",
//             DateTime = DateTime.Now,
//             TotalQuantity = 101,
//             Products = new List<ProductDto>
//             {
//                 new ProductDto { ProductName = "Çikolata", ProductPiece = 1, KdvRate = 18 },
//                 new ProductDto { ProductName = "Yoğurt", ProductPiece = 1, KdvRate = 8 },
//                 new ProductDto { ProductName = "Makarna", ProductPiece = 4, KdvRate = 8 }
//             }
//         };
//     }
// }

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IOcrService
{
    Task<ReceiptDto> ExtractReceiptData(byte[] imageData);
}

public class OcrService : IOcrService
{
    private static readonly Random _random = new Random();

    public async Task<ReceiptDto> ExtractReceiptData(byte[] imageData)
    {
        // OCR işlemi yapılmalı. Örneğin, Tesseract gibi bir kütüphane kullanılabilir.
        // Burada basit bir örnek olarak dummy data döndürüyoruz.

        await Task.Delay(1000); // OCR işlemi simülasyonu

        // Dummy data
        return new ReceiptDto
        {
            MarketName = GetRandomMarketName(),
            MarketBranch = GetRandomMarketBranch(),
            DateTime = DateTime.Now,
            TotalQuantity = _random.Next(1, 200),
            Products = GetRandomProducts()
        };
    }

    private string GetRandomMarketName()
    {
        var marketNames = new List<string> { "Bim", "A101", "Migros", "Carrefour" };
        return marketNames[_random.Next(marketNames.Count)];
    }

    private string GetRandomMarketBranch()
    {
        var marketBranches = new List<string> { "Bulvar Şubesi", "Merkez Şubesi", "Sahil Şubesi", "Çarşı Şubesi" };
        return marketBranches[_random.Next(marketBranches.Count)];
    }

    private List<ProductDto> GetRandomProducts()
    {
        var productNames = new List<string> { "Çikolata", "Yoğurt", "Makarna", "Ekmek", "Süt", "Peynir", "Zeytin", "Su" };
        var products = new List<ProductDto>();

        int productCount = _random.Next(1, 6);
        for (int i = 0; i < productCount; i++)
        {
            var productName = productNames[_random.Next(productNames.Count)];
            products.Add(new ProductDto
            {
                ProductName = productName,
                ProductPiece = _random.Next(1, 10),
                KdvRate = _random.Next(1, 20)
            });
        }

        return products;
    }
}