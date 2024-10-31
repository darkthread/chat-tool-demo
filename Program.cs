using chat_tool_demo;

GptChatService chat = new GptChatService(
    SecureEnvVars.GetSecureEnvVar("SK_EndPoint"),
    SecureEnvVars.GetSecureEnvVar("SK_ApiKey"),
    "gpt-4o");
var sysPrompt = "你是AI英文翻譯，將使用者輸入內容翻成英文";
var resp = await chat.Complete(sysPrompt, "天助自助者");
Console.WriteLine(resp);
    
