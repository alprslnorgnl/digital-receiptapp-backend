using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class ReceiptController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggingService _loggingService;
    private readonly IOcrService _ocrService;
    private readonly IChatGptService _chatGptService;

    public ReceiptController(ApplicationDbContext context, ILoggingService loggingService, IOcrService ocrService, IChatGptService chatGptService)
    {
        _context = context;
        _ocrService = ocrService;
        _loggingService = loggingService;
        _chatGptService = chatGptService;
    }

    // get receipt count of user +
    [HttpGet("count")]
    [Authorize]
    public async Task<IActionResult> GetReceiptCount()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.Include(u => u.Receipts).FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var receiptCount = user.Receipts?.Count ?? 0;
            _loggingService.LogInfo($"Kullanıcıya ait fiş sayısı: {receiptCount}");
            return Ok(new { receiptCount });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Fiş sayısı alınırken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    //get all receipts of user
    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetAllReceipts()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.Include(u => u.Receipts!).ThenInclude(r => r.Products!).FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var receipts = user.Receipts!.Select(r => new 
            {
                r.ReceiptId,
                r.DateTime,
                r.TotalQuantity,
                r.MarketName,
                r.MarketBranch,
                r.Favorite,
                Products = r.Products!.Select(p => new 
                {
                    p.ItemId,
                    p.ProductName,
                    p.ProductPiece,
                    p.KdvRate
                })
            }).ToList();

            _loggingService.LogInfo($"Kullanıcıya ait {receipts.Count} fiş bulundu.");
            return Ok(receipts);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Fiş bilgileri alınırken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    //its work +
    [HttpPost("ocr")]
    public async Task<IActionResult> ExtractReceiptData()
    {
        try
        {
            var formFile = Request.Form.Files.FirstOrDefault();
            if (formFile == null || formFile.Length == 0)
            {
                _loggingService.LogInfo("Resim dosyası bulunamadı.");
                return BadRequest(new { message = "Resim dosyası bulunamadı" });
            }

            using var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            var imageData = ms.ToArray();

            var receiptData = await _ocrService.ExtractReceiptData(imageData);

            return Ok(receiptData);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("OCR işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    //its work +
    [HttpPost("addReceipt")]
    [Authorize]
    public async Task<IActionResult> AddReceipt([FromBody] ReceiptDto receiptData)
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var receipt = new Receipt
            {
                UserId = user.UserId,
                MarketName = receiptData.MarketName,
                MarketBranch = receiptData.MarketBranch,
                DateTime = receiptData.DateTime,
                TotalQuantity = receiptData.TotalQuantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Products = receiptData.Products!.Select(p => new Product
                {
                    ProductName = p.ProductName,
                    ProductPiece = p.ProductPiece,
                    KdvRate = p.KdvRate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList()
            };

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Fiş başarıyla eklendi.");
            return Ok(new { message = "Fiş başarıyla eklendi" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Fiş eklenirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    [HttpPost("toggleFavorite/{id}")]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        try
        {
            var receipt = await _context.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return NotFound(new { message = "Fiş bulunamadı" });
            }

            receipt.Favorite = !receipt.Favorite;
            await _context.SaveChangesAsync();

            return Ok(new { favorite = receipt.Favorite });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Favorite durumu güncellenirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    // Delete receipt
    [HttpDelete("delete/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteReceipt(int id)
    {
        try
        {
            var receipt = await _context.Receipts.FindAsync(id);
            if (receipt == null)
            {
                return NotFound(new { message = "Fiş bulunamadı" });
            }

            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Fiş başarıyla silindi" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Fiş silinirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    [HttpPost("gpt")]
    [Authorize]
    public async Task<IActionResult> AnalyzeReceipts([FromBody] AnalysisRequestDto request)
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.Include(u => u.Receipts)!.ThenInclude(r => r.Products).FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Invalid token.");
                return Unauthorized(new { message = "Invalid token" });
            }

            var response = await _chatGptService.AnalyzeReceipts(request.Prompt, user.Receipts!.ToList());

            return Ok(new { analysis = response });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error during receipt analysis.", ex);
            return StatusCode(500, new { message = "Unexpected error occurred." });
        }
    }

    [HttpGet("favCount")]
    [Authorize]
    public async Task<IActionResult> GetFavoriteReceiptCount()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.Include(u => u.Receipts).FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var favoriteReceiptCount = user.Receipts?.Count(r => r.Favorite) ?? 0;
            _loggingService.LogInfo($"Kullanıcıya ait favori fiş sayısı: {favoriteReceiptCount}");
            return Ok(new { favoriteCount = favoriteReceiptCount });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Favori fiş sayısı alınırken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    [HttpGet("getAllFav")]
    [Authorize]
    public async Task<IActionResult> GetAllFavoriteReceipts()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.Include(u => u.Receipts!).ThenInclude(r => r.Products!).FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var favoriteReceipts = user.Receipts!.Where(r => r.Favorite).Select(r => new
            {
                r.ReceiptId,
                r.DateTime,
                r.TotalQuantity,
                r.MarketName,
                r.MarketBranch,
                r.Favorite,
                Products = r.Products!.Select(p => new
                {
                    p.ItemId,
                    p.ProductName,
                    p.ProductPiece,
                    p.KdvRate
                })
            }).ToList();

            _loggingService.LogInfo($"Kullanıcıya ait {favoriteReceipts.Count} favori fiş bulundu.");
            return Ok(favoriteReceipts);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Favori fiş bilgileri alınırken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }
}

public class AnalysisRequestDto
{
    public required string Prompt { get; set; }
}
