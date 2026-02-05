using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GurtBot;

public class LoggingManager
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public const string Path = "../../../messages.json";
    
    public static readonly Random Random = new();
    
    public static async Task LogMessage(IMessage message)
    {
        ProcessedMessage processedMessage = CreateMessage(message);
        var messages = GetMessages(Path);
        messages.Add(processedMessage);
        await File.WriteAllTextAsync(Path, JsonSerializer.Serialize(messages, Options));
    }

    public static async Task LogMessages(IEnumerable<IMessage> messages)
    {
        List<ProcessedMessage> processedMessages = new();
        processedMessages.AddRange(CreateMessages(messages));
        var old = GetMessages(Path);
        old.AddRange(processedMessages);
        await File.WriteAllTextAsync(Path, JsonSerializer.Serialize(processedMessages, Options));
    }

    public static async Task SendRandomMessageCombination(SocketMessage socketMessage)
    {
        var result = GetMessages(Path);

        if (result.Count < 10) return;

        List<ProcessedMessage> messages = new();
        
        for (int i = 0; i < SettingsManager.GetMaxWords(); i++)
        {
            messages.Add(result[Random.Next(result.Count)]);
        }

        List<string> messageList = new();
        
        foreach (ProcessedMessage message in messages)
        {
            if (message.MessageContent.Length == 0) continue;
            if (message.MessageContent.StartsWith("https://"))
            {
                messageList.Clear();
                messageList.Add(message.MessageContent);
                break;
            }
            else
            {
                string content = Regex.Match(message.MessageContent, @"^([\w\-]+)").Value;
                messageList.Add(content);
            }
        }
        
        string finalMessage = string.Join(" ", messageList);
        await socketMessage.Channel.SendMessageAsync(finalMessage);
    }

    private static ProcessedMessage CreateMessage(IMessage message)
    {
        return new ProcessedMessage
        {
            AuthorGlobalName = message.Author.GlobalName,
            AuthorUsername = message.Author.Username,
            MessageContent = message.CleanContent,
        };
    }

    private static List<ProcessedMessage> CreateMessages(IEnumerable<IMessage> messages)
    {
        List<ProcessedMessage> finalMessages = new();
        foreach (IMessage message in messages)
        {
            finalMessages.Add(CreateMessage(message));
        }

        return finalMessages;
    }

    private static List<ProcessedMessage> GetMessages(string path)
    {
        List<ProcessedMessage> messages = new();
        if (IsFileLocked(path)) return messages;

        messages = File.Exists(path)
            ? JsonConvert.DeserializeObject<List<ProcessedMessage>>(File.ReadAllText(path))
              ?? new()
            : new();

        return messages;
    }

    public static bool IsFileLocked(string fileName)
    {
        try
        {
            FileStream fs = File.Open(fileName, FileMode.Open);
            fs.Dispose();
            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }
}
