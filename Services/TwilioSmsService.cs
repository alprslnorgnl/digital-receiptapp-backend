using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Verify.V2.Service;
using Twilio.Types;

public interface ITwilioSmsService
{
    Task<bool> SendOtpAsync(string phoneNumber);
    Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode);
}

public class TwilioSmsService : ITwilioSmsService
{
    private readonly IConfiguration _configuration;

    public TwilioSmsService(IConfiguration configuration)
    {
        _configuration = configuration;
        var accountSid = _configuration["Twilio:AccountSid"];
        var authToken = _configuration["Twilio:AuthToken"];
        TwilioClient.Init(accountSid, authToken);
    }

    public async Task<bool> SendOtpAsync(string phoneNumber)
    {
        try
        {
            var serviceSid = _configuration["Twilio:ServiceSid"];
            var verification = await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: "sms",
                pathServiceSid: serviceSid
            );

            return verification.Status == "pending";
        }
        catch (Exception ex)
        {
            // Hata durumunda loglama yapılabilir
            Console.WriteLine("SMS Gönderme hatası",ex);
            return false;
        }
    }

    public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
    {
        try
        {
            var serviceSid = _configuration["Twilio:ServiceSid"];
            var verificationCheck = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: otpCode,
                pathServiceSid: serviceSid
            );

            return verificationCheck.Status == "approved";
        }
        catch (Exception ex)
        {
            // Hata durumunda loglama yapılabilir
            Console.WriteLine("OTP doğrulama hatası",ex);
            return false;
        }
    }
}
