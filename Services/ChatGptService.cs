using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Interfaces;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

public interface IChatGptService
{
    Task<string> AnalyzeReceipts(string prompt, List<Receipt> receipts);
}

public class ChatGptService : IChatGptService
{
    private readonly IOpenAIService _openAIService;

    public ChatGptService(IConfiguration configuration)
    {
        var apiKey = configuration["OpenAIServiceOptions:ApiKey"];
        _openAIService = new OpenAIService(new OpenAiOptions { ApiKey = apiKey! });
    }

    public async Task<string> AnalyzeReceipts(string prompt, List<Receipt> receipts)
    {
        var receiptData = receipts.Select(r => new
        {
            r.MarketName,
            r.MarketBranch,
            r.DateTime,
            r.TotalQuantity,
            Products = r.Products!.Select(p => new
            {
                p.ProductName,
                p.ProductPiece,
                p.KdvRate
            })
        }).ToList();

        var messages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are a financial assistant."),
            ChatMessage.FromUser($"Here is the data: {JsonSerializer.Serialize(receiptData)}"),
            ChatMessage.FromUser(prompt)
        };

        var completionRequest = new ChatCompletionCreateRequest
        {
            Messages = messages,
            Model = Models.Gpt_4o
        };

        var result = await _openAIService.ChatCompletion.CreateCompletion(completionRequest);

        if (result.Successful)
        {
            return result.Choices.First().Message.Content!;
        }

        throw new Exception(result.Error?.Message ?? "Unknown error");
    }
}
