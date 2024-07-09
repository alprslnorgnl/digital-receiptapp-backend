using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]")]
[ApiController]
public class BaseUserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILoggingService _loggingService;
    private readonly IOtpService _otpService;

    public BaseUserController(ApplicationDbContext context, ILoggingService loggingService, IOtpService otpService)
    {
        _context = context;
        _loggingService = loggingService;
        _otpService = otpService;
    }

    // Get user profile +
    [HttpGet("get")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            //Gelen tokene ait kullanıcı olup olmadığı kontrol edilir
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var baseUser = await _context.BaseUsers.FirstOrDefaultAsync(b => b.UserId == user.UserId);
            
            var userProfile = new UserProfileDto
            {
                Name = baseUser?.Name ?? string.Empty,
                Surname = baseUser?.Surname ?? string.Empty,
                Email = baseUser?.Email ?? string.Empty,
                Gender = baseUser?.Gender ?? string.Empty,
                BirthDate = baseUser?.BirthDate ?? DateTime.MinValue,
                ProfileImage = baseUser?.ProfileImage ?? string.Empty // Base64 string
            };

            _loggingService.LogInfo("Kullanıcı profili başarıyla getirildi.");
            Console.WriteLine(userProfile);
            return Ok(userProfile);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Kullanıcı profili getirilirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Update user profile +
    [HttpPut("update")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileDto userProfileDto)
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

            var baseUser = await _context.BaseUsers.FirstOrDefaultAsync(b => b.UserId == user.UserId);
            if (baseUser == null)
            {
                baseUser = new BaseUser
                {
                    UserId = user.UserId,
                    Name = userProfileDto.Name,
                    Surname = userProfileDto.Surname,
                    Email = userProfileDto.Email,
                    Gender = userProfileDto.Gender,
                    BirthDate = userProfileDto.BirthDate,
                    ProfileImage = userProfileDto.ProfileImage
                };
                _context.BaseUsers.Add(baseUser);
            }
            else
            {
                baseUser.Name = userProfileDto.Name;
                baseUser.Surname = userProfileDto.Surname;
                baseUser.Email = userProfileDto.Email;
                baseUser.Gender = userProfileDto.Gender;
                baseUser.BirthDate = userProfileDto.BirthDate;
                baseUser.ProfileImage = userProfileDto.ProfileImage; // Base64 string olarak resim
                _context.BaseUsers.Update(baseUser);
            }

            user.Email = userProfileDto.Email;
            _context.Users.Update(user);

            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcı profili başarıyla güncellendi.");
            return Ok(new { message = "Kullanıcı profili başarıyla güncellendi" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Kullanıcı profili güncellenirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Change user password +
    [HttpPost("changePassword")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
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

            if (user.Password == null)
            {
                _loggingService.LogInfo("Bu kullanıcı şifre kullanarak giriş yapmamıştır.");
                return BadRequest(new { message = "Bu kullanıcı şifre kullanarak giriş yapmamıştır." });
            }

            if (changePasswordDto.NewPassword != changePasswordDto.NewPasswordConfirm)
            {
                return BadRequest(new { message = "Yeni şifre ve onay şifresi eşleşmiyor." });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcı şifresi başarıyla güncellendi.");
            return Ok(new { message = "Kullanıcı şifresi başarıyla güncellendi." });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Kullanıcı şifresi güncellenirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    // Change user phone number
    [HttpPost("changePhone")]
    [Authorize]
    public async Task<IActionResult> ChangePhone([FromBody] ChangePhoneDto changePhoneDto)
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

            if (user.PhoneNumber == null)
            {
                _loggingService.LogInfo("Bu kullanıcı telefon kullanarak giriş yapmamıştır.");
                return BadRequest(new { message = "Bu kullanıcı telefon kullanarak giriş yapmamıştır." });
            }

            //otp kodu gönder
            var result = await _otpService.SendOtpSmsAsync(changePhoneDto.NewPhoneNumber!);

            if (!result)
            {
                _loggingService.LogError("OTP gönderilemedi.", new Exception("OTP gönderilemedi."));
                return StatusCode(500, new { message = "OTP gönderilemedi" });
            }

            _loggingService.LogInfo("Telefon numarası değişikliği için OTP gönderildi.");
            return Ok();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Telefon numarası güncellenirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    [HttpPost("changePhoneOtpV")]
    [Authorize]
    public async Task<IActionResult> ChangePhoneOtpV([FromBody] ChangePhoneOtpVDto otpDto)
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

            // OTP kodunu doğrula
            var otpIsValid = await _otpService.ValidateOtpSmsCode(otpDto.NewPhoneNumber!, otpDto.OtpCode!);

            if (!otpIsValid)
            {
                return BadRequest(new { message = "Geçersiz OTP kodu" });
            }

            // Mevcut kullanıcıyı eski telefon numarasıyla bul
            var user2 = await _context.Users.SingleOrDefaultAsync(u => u.PhoneNumber == otpDto.OldPhoneNumber);

            if (user2 == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            // Kullanıcının telefon numarasını güncelle
            user2.PhoneNumber = otpDto.NewPhoneNumber;
            user2.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user2);
            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcının telefon numarası güncellendi.");
            return Ok();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Change phone OTP verification işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }


    [HttpDelete("deleteAccount")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var user = await _context.Users.Include(u => u.Receipts!).ThenInclude(r => r.Products).FirstOrDefaultAsync(u => u.Token == token);

            if (user == null)
            {
                _loggingService.LogInfo("Geçersiz token.");
                return Unauthorized(new { message = "Geçersiz token" });
            }

            _context.Products.RemoveRange(user.Receipts!.SelectMany(r => r.Products!));
            _context.Receipts.RemoveRange(user.Receipts!);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcı ve ilişkili veriler başarıyla silindi.");
            return Ok(new { message = "Hesap başarıyla silindi" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Hesap silinirken hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }
}