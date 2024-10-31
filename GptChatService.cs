using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace chat_tool_demo
{
    public class GptChatService
    {
        List<ChatMessage> history = new List<ChatMessage>();
        ChatClient client;
        public GptChatService(string apiUrl, string apiKey, string deployName)
        {
            client = new AzureOpenAIClient(
                new Uri(apiUrl),
                new System.ClientModel.ApiKeyCredential(apiKey))
                .GetChatClient(deployName);
        }
        public async Task<string> Complete(string sysPropmt, string msg, bool includeHistory = false)
        {
            var chatMessages = new List<ChatMessage>() {
                new SystemChatMessage(sysPropmt)
            };
            if (includeHistory)
                chatMessages.AddRange(history);
            chatMessages.Add(new UserChatMessage(msg));
            var complete = await client.CompleteChatAsync(chatMessages);
            var resp = complete.Value.Content[0].Text;
            if (includeHistory)
            {
                history.Add(new UserChatMessage(msg));
                history.Add(new AssistantChatMessage(resp));
            }
            return resp;
        }
    }
}