using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string email, string otpCode);
    bool VerifyOtp(string sentOtp, string receivedOtp);
    string GenerateOtpCode();
    void StoreOtpCode(string email, string otpCode);
    bool ValidateStoredOtpCode(string email, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private static readonly Dictionary<string, string> _otpStorage = new Dictionary<string, string>();

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> SendOtpEmailAsync(string email, string otpCode)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Digital Receipt App", _configuration["EmailSettings:From"]));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "OTP Code";

            message.Body = new TextPart("plain")
            {
                Text = $"OTP code: {otpCode}"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_configuration["EmailSettings:SmtpServer"], int.Parse(_configuration["EmailSettings:Port"]!), true);
            await client.AuthenticateAsync(_configuration["EmailSettings:Username"], _configuration["EmailSettings:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Hata:", ex);
            return false;
        }
    }

    public bool VerifyOtp(string sentOtp, string receivedOtp)
    {
        return sentOtp == receivedOtp;
    }

    public string GenerateOtpCode()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    public void StoreOtpCode(string email, string otpCode)
    {
        if (_otpStorage.ContainsKey(email))
        {
            _otpStorage.Remove(email);
        }
        _otpStorage[email] = otpCode;

        Console.WriteLine("Mevcut OTP Kodları:");
        foreach (var entry in _otpStorage)
        {
            Console.WriteLine($"Email: {entry.Key}, OTP: {entry.Value}");
        }
    }

    public bool ValidateStoredOtpCode(string email, string otpCode)
    {
        Console.WriteLine("Mevcut OTP Kodları:");
        foreach (var entry in _otpStorage)
        {
            Console.WriteLine($"Email: {entry.Key}, OTP: {entry.Value}");
        }

        return _otpStorage.ContainsKey(email) && _otpStorage[email] == otpCode;
    }
}
