namespace GurtBot;

public class ProcessedMessage
{
    public required string AuthorUsername { get; set; }
    public required string AuthorGlobalName { get; set; }
    public required string MessageContent { get; set; }
}
