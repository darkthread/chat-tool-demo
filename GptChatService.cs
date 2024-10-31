using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace chat_tool_demo
{
    public class GptChatService
    {
        List<ChatMessage> history = new List<ChatMessage>();
        public void NewSession() => history.Clear();
        ChatClient client;
        public GptChatService(string apiUrl, string apiKey, string deployName)
        {
            client = new AzureOpenAIClient(
                new Uri(apiUrl),
                new System.ClientModel.ApiKeyCredential(apiKey))
                .GetChatClient(deployName);
        }

        public string SystemPrompt { get; set; } = "你是AI助理，負責解決使用者的問題";

        //https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.openai-readme?view=azure-dotnet#use-chat-tools
        public async Task<string> Complete(string msg, bool includeHistory = false)
        {
            var chatMessages = new List<ChatMessage>() { new SystemChatMessage(SystemPrompt) };
            if (includeHistory)
                chatMessages.AddRange(history);
            chatMessages.Add(new UserChatMessage(msg));
            // 加入工具
            ChatCompletionOptions options = new ChatCompletionOptions()
            {
                Tools = { RTERService.CreateChatTool() }
            };
            StringBuilder sb = new();
            while (true)
            {
                var completion = await client.CompleteChatAsync(chatMessages, options);
                if (completion.Value?.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // 加入 AssistantChatMessage 以包含 ToolCall 的回應
                    chatMessages.Add(new AssistantChatMessage(completion));
                    // 取得工具呼叫資訊
                    foreach (var toolCall in completion.Value.ToolCalls)
                    {
                        var toolCallOutput = await RTERService.HandleToolCallAsync(toolCall);
                        Console.WriteLine($"DEBUG: {toolCallOutput}");
                        chatMessages.Add(new ToolChatMessage(toolCall.Id, toolCallOutput));
                    }
                }
                else if (completion.Value?.FinishReason == ChatFinishReason.Stop)
                {
                    sb.Append(completion.Value.Content[0].Text);
                    break;
                }
            }
            var resp = sb.ToString();
            if (includeHistory)
            {
                history.Add(new UserChatMessage(msg));
                history.Add(new AssistantChatMessage(resp));
            }
            return resp;
        }
    }
}