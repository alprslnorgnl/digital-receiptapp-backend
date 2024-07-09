using System;

public interface ILoggingService
{
    void LogInfo(string message);
    void LogError(string message, Exception ex);
}

public class LoggingService : ILoggingService
{
    public void LogInfo(string message)
    {
        Console.WriteLine($"INFO: {message}");
    }

    public void LogError(string message, Exception ex)
    {
        Console.WriteLine($"ERROR: {message} - Exception: {ex.Message}");
    }
}
