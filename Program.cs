using chat_tool_demo;

Console.WriteLine(RTERService.GetExchangeRate("USD", "TWD"));

GptChatService chat = new(
    SecureEnvVars.GetSecureEnvVar("SK_EndPoint"),
    SecureEnvVars.GetSecureEnvVar("SK_ApiKey"), "gpt-4o")
{ SystemPrompt = "你是AI助理，負責回覆使用者詢問" };

Console.WriteLine("請輸入問題，輸入 /clear 可另起交談，輸入 /exit 離開");
Console.WriteLine("================================================");
while (true) {
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input == "/clear") chat.NewSession();
    else if (input == "/exit") break;
    else {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(await chat.Complete(input, true));
        Console.ResetColor();
    }
}

