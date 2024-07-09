using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public interface IOtpService
{
    string GenerateOtpCode();
    Task<bool> SendOtpEmailAsync(string email, string otpCode);
    void StoreOtpCode(string key, string otpCode);
    bool ValidateStoredOtpCode(string key, string otpCode);
    Task<bool> SendOtpSmsAsync(string phoneNumber);
    Task<bool> ValidateOtpSmsCode(string key, string otpCode);
}

public class OtpService : IOtpService
{
    private readonly IEmailService _emailService;
    private readonly ITwilioSmsService _twilioSmsService;
    private static readonly ConcurrentDictionary<string, string> _otpStorage = new ConcurrentDictionary<string, string>();

    public OtpService(IEmailService emailService, ITwilioSmsService twilioSmsService)
    {
        _emailService = emailService;
        _twilioSmsService = twilioSmsService;
    }

    //EMAİL
    public string GenerateOtpCode()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    public async Task<bool> SendOtpEmailAsync(string email, string otpCode)
    {
        return await _emailService.SendOtpEmailAsync(email, otpCode);
    }

    public void StoreOtpCode(string key, string otpCode)
    {
        if (_otpStorage.ContainsKey(key))
        {
            _otpStorage[key] = otpCode;
        }
        else
        {
            _otpStorage.TryAdd(key, otpCode);
        }

        Console.WriteLine(key);
        Console.WriteLine(otpCode);
    }

    public bool ValidateStoredOtpCode(string key, string otpCode)
    {
    return _otpStorage.ContainsKey(key) && _otpStorage[key] == otpCode;
    }


    //PHONE
    public async Task<bool> SendOtpSmsAsync(string phoneNumber)
    {
        return await _twilioSmsService.SendOtpAsync(phoneNumber);
    }

    public async Task<bool> ValidateOtpSmsCode(string key, string otpCode)
    {
        return await _twilioSmsService.VerifyOtpAsync(key,otpCode);

    }

}
