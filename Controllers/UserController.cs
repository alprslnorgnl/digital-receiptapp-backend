using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BCrypt.Net;
using Newtonsoft.Json.Linq;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IOtpService _otpService;
    private readonly ILoggingService _loggingService;
    private readonly HttpClient _httpClient;
    private readonly IEmailService _emailService;

    public UserController(ApplicationDbContext context, IConfiguration configuration, IJwtService jwtService, IEmailService emailService, IOtpService otpService, ILoggingService loggingService)
    {
        _context = context;
        _configuration = configuration;
        _jwtService = jwtService;
        _httpClient = new HttpClient();
        _emailService = emailService;
        _otpService = otpService;
        _loggingService = loggingService;
    }

    // Signup
    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupDto signupDto)
    {
        try
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == signupDto.PhoneNumber);
            if (existingUser != null)
            {
                _loggingService.LogInfo("Kullanıcı zaten kayıtlı.");
                return BadRequest(new { message = "Kullanıcı zaten kayıtlı" });
            }

            // OTP kodu gönder
            var result = await _otpService.SendOtpSmsAsync(signupDto.PhoneNumber!);

            if (!result)
            {
                _loggingService.LogError("OTP gönderilemedi.", new Exception("OTP gönderilemedi."));
                return StatusCode(500, new { message = "OTP gönderilemedi" });
            }

            _loggingService.LogInfo("Kullanıcı kaydı için OTP gönderildi.");
            return Ok();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Signup işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == loginDto.PhoneNumber);

            if (user == null)
            {
                _loggingService.LogInfo("Geçerli bir telefon numarası girilmedi.");
                return Unauthorized(new { message = "Lütfen geçerli bir telefon numarası giriniz" });
            }
            else if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            {
                _loggingService.LogInfo("Hatalı şifre girildi.");
                return Unauthorized(new { message = "Hatalı şifre girdiniz. Lütfen tekrar deneyiniz" });
            }

            var token = await _jwtService.GenerateJwtToken(user);
            _loggingService.LogInfo("Kullanıcı giriş yaptı.");
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Login işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Google Login
    [HttpPost("googleLogin")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
    {
        try
        {
            var googleApiUrl = $"https://www.googleapis.com/oauth2/v3/userinfo?access_token={request.AccessToken}";
            var response = await _httpClient.GetStringAsync(googleApiUrl);
            var userInfo = JObject.Parse(response);

            var email = userInfo["email"]?.ToString();
            if (email == null)
            {
                _loggingService.LogInfo("Geçersiz Google access token.");
                return BadRequest(new { message = "Invalid Google access token" });
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser == null)
            {
                var user = new User
                {
                    Email = email,
                    Guid = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var baseUser = new BaseUser
                {
                    UserId = user.UserId,
                    Name = "",
                    Surname = "",
                    Gender = "",
                    BirthDate = DateTime.MinValue,
                    ProfileImage = ""
                };

                _context.BaseUsers.Add(baseUser);
                await _context.SaveChangesAsync();

                var token = await _jwtService.GenerateJwtToken(user);
                _loggingService.LogInfo("Yeni Google kullanıcısı oluşturuldu ve giriş yaptı.");
                return Ok(new { token });
            }
            else
            {
                var token = await _jwtService.GenerateJwtToken(existingUser);
                _loggingService.LogInfo("Google kullanıcısı giriş yaptı.");
                return Ok(new { token });
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Google login işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Password Reset
    [HttpPost("passwordReset")]
    public async Task<IActionResult> PasswordReset([FromBody] PasswordResetDto resetDto)
    {
        try
        {
            if (string.IsNullOrEmpty(resetDto.Phone) && string.IsNullOrEmpty(resetDto.Email))
            {
                return BadRequest(new { message = "Telefon numarası veya e-posta adresi sağlanmalı" });
            }
            

            //EMAİL GİRİLMİŞ İSE
            if (!string.IsNullOrEmpty(resetDto.Phone))
            {
                var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Email == resetDto.Email);

                if(user1 == null)
                {
                    return BadRequest(new {message = "Bu mail adresine kayıtlı kullanıcı bulunmamaktadır"});
                }

                // OTP kodu oluştur ve gönder
                var otpCode = _otpService.GenerateOtpCode();
                var result = await _otpService.SendOtpEmailAsync(resetDto.Email!, otpCode);

                if (!result)
                {
                    _loggingService.LogError("OTP e-posta ile gönderilemedi.", new Exception("OTP gönderilemedi."));
                    return StatusCode(500, new { message = "OTP e-posta ile gönderilemedi" });
                }

                // OTP kodunu sakla
                _otpService.StoreOtpCode(resetDto.Email!, otpCode);

                _loggingService.LogInfo("OTP kodu e-posta adresine gönderildi.");
                return Ok(new { message = "OTP kodu e-posta adresine gönderildi" });
            }

            //PHONE GİRİLMİŞ İSE
            if (!string.IsNullOrEmpty(resetDto.Email))
            {
                var user1 = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == resetDto.Phone);

                if(user1 == null)
                {
                    return BadRequest(new {message = "Bu telefon numarasına kayıtlı kullanıcı bulunmamaktadır"});
                }

                // OTP kodu gönder
                var result = await _otpService.SendOtpSmsAsync(resetDto.Phone!);

                if (!result)
                {
                    _loggingService.LogError("OTP gönderilemedi.", new Exception("OTP gönderilemedi."));
                    return StatusCode(500, new { message = "OTP gönderilemedi" });
                }

                _loggingService.LogInfo("OTP kodu telefon numarasına gönderildi.");
                return Ok(new { message = "OTP kodu telefon numarasına gönderildi" });
            }

            return BadRequest(new { message = "Geçersiz istek" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Password reset işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Password Reset Last
    [HttpPost("passwordResetLast")]
    public async Task<IActionResult> PasswordResetLast([FromBody] PasswordResetLastDto resetLastDto)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == resetLastDto.EmailOrPhone || u.PhoneNumber == resetLastDto.EmailOrPhone);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            if (user.Email == resetLastDto.EmailOrPhone && user.Password == null)
            {
                return BadRequest(new { message = "Kullanıcının şifre alanı boş, şifre güncellenemedi" });
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(resetLastDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcının şifresi güncellendi.");
            return Ok();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Password reset last işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Change Phone
    [HttpPost("changePhone")]
    public async Task<IActionResult> ChangePhone([FromBody] ChangePhoneDto changePhoneDto)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == changePhoneDto.OldPhoneNumber);
            if (user == null)
            {
                return NotFound(new { message = "Telefon numarası bulunamadı" });
            }

            // OTP kodu gönder
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
            _loggingService.LogError("Change phone işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Create Account Otp Verification
    [HttpPost("createAccountOtpV")]
    public async Task<IActionResult> CreateAccountOtpV([FromBody] CreateAccountOtpVDto otpDto)
    {
        try
        {   
            Console.WriteLine("otp code: ", otpDto.OtpCode!);
            var otpIsValid = await _otpService.ValidateOtpSmsCode(otpDto.PhoneNumber!, otpDto.OtpCode!);

            if (!otpIsValid)
            {
                return BadRequest(new { message = "Geçersiz OTP kodu" });
            }

            var user = new User
            {
                PhoneNumber = otpDto.PhoneNumber,
                Password = BCrypt.Net.BCrypt.HashPassword(otpDto.Password),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var baseUser = new BaseUser
                {
                    UserId = user.UserId,
                    Name = "",
                    Surname = "",
                    Gender = "",
                    BirthDate = DateTime.MinValue,
                    ProfileImage = ""
                };

                _context.BaseUsers.Add(baseUser);
                await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcı oluşturuldu ve OTP doğrulandı.");
            return Ok();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Create account OTP verification işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    [HttpPost("passwordResetOtpV")]
    public async Task<IActionResult> PasswordResetOtpV([FromBody] PasswordResetOtpVDto otpVDto)
    {
        try
        {
            if (string.IsNullOrEmpty(otpVDto.Code) || (string.IsNullOrEmpty(otpVDto.Phone) && string.IsNullOrEmpty(otpVDto.Email)))
            {
                return BadRequest(new { message = "OTP kodu ve telefon veya e-posta bilgisi sağlanmalı" });
            }

            bool otpIsValid = false;

            //EMAİL DOĞRULAMA İŞLEMLERİ GERÇEKLEŞTİRİLİR
            if (!string.IsNullOrEmpty(otpVDto.Phone))
            {
                // OTP kodunu doğrulama işlemi
                otpIsValid = await Task.Run(() => _otpService.ValidateStoredOtpCode(otpVDto.Email!, otpVDto.Code));

                if (!otpIsValid)
                {
                    return BadRequest(new { message = "Geçersiz OTP kodu" });
                }

                return Ok(new { message = "OTP kodu doğrulandı" });
            }

            //PHONE DOĞRULAMA İŞLEMLERİ GERÇEKLEŞTİRİLİR
            if (!string.IsNullOrEmpty(otpVDto.Email))
            {
                // OTP kodunu doğrulama işlemi
                otpIsValid = _otpService.ValidateStoredOtpCode(otpVDto.Phone!, otpVDto.Code);

                if (!otpIsValid)
                {
                    return BadRequest(new { message = "Geçersiz OTP kodu" });
                }

                return Ok(new { message = "OTP kodu doğrulandı" });
            }

            return BadRequest(new { message = "Geçersiz istek" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Password reset OTP verification işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }

    // Change Phone Otp Verification
    [HttpPost("changePhoneOtpV")]
    public async Task<IActionResult> ChangePhoneOtpV([FromBody] ChangePhoneOtpVDto otpDto)
    {
        try
        {
            // OTP kodunu doğrula
            var otpIsValid = _otpService.ValidateStoredOtpCode(otpDto.NewPhoneNumber!, otpDto.OtpCode!);

            if (!otpIsValid)
            {
                return BadRequest(new { message = "Geçersiz OTP kodu" });
            }

            // Mevcut kullanıcıyı eski telefon numarasıyla bul
            var user = await _context.Users.SingleOrDefaultAsync(u => u.PhoneNumber == otpDto.OldPhoneNumber);

            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            // Kullanıcının telefon numarasını güncelle
            user.PhoneNumber = otpDto.NewPhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "Geçersiz token" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Token == token);
            if (user == null)
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            user.Token = null;
            await _context.SaveChangesAsync();

            _loggingService.LogInfo("Kullanıcı çıkış yaptı.");
            return Ok(new { message = "Çıkış yapıldı" });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Logout işlemi sırasında hata oluştu.", ex);
            return StatusCode(500, new { message = "Beklenmedik bir hata oluştu." });
        }
    }
}
