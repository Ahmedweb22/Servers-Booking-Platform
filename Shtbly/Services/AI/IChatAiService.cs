namespace Shtbly.Services.AI
{
    public interface IChatAiService
    {
        Task<string> AskAsync(string userMessage, List<ChatMessage> history);
    }

    public class ChatMessage
    {
        public string Role { get; set; }  // "user" or "model"
        public string Text { get; set; }
    }
}
