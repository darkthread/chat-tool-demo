using OpenAI.Chat;
using System.Text.Json;

namespace chat_tool_demo
{
    public class RTERService
    {
        static Dictionary<(string From, string To), (decimal Exrate, string UpdTime)> _exchangeRates = null!;
        static DateTime ExpireTime = DateTime.MinValue;
        static HttpClient httpClient = new HttpClient();
        class Entry
        {
            public decimal Exrate { get; set; }
            public string UTC { get; set; } = "";
        }
        static Dictionary<(string From, string To), (decimal Exrate, string UpdTime)> ExchangeRates
        {
            get
            {
                if (DateTime.Now > ExpireTime)
                {
                    // 每分鐘更新一次匯率(非同步進行)
                    Task.Factory.StartNew(() =>
                    {
                        // 避免多執行緒同時執行
                        lock (httpClient)
                        {
                            // 第二次檢查，防止多執行緒重複更新
                            if (DateTime.Now < ExpireTime) return;
                            // 全球即時匯率API                            
                            httpClient.GetAsync("https://tw.rter.info/capi.php")
                                .ContinueWith(task =>
                                {
                                    var response = task.Result;
                                    if (response.IsSuccessStatusCode)
                                    {
                                        var rates = JsonSerializer.Deserialize<Dictionary<string, Entry>>(response.Content.ReadAsStringAsync().Result);
                                        _exchangeRates = rates!
                                            .Where(o => o.Key.Length == 6).ToDictionary(
                                            rate => (rate.Key.Substring(0, 3), rate.Key.Substring(3, 3)),
                                            rate => (rate.Value.Exrate, rate.Value.UTC));
                                    }
                                }).Wait();
                            ExpireTime = DateTime.Now.AddMinutes(1);
                        }
                    });
                }
                var timeOut = DateTime.Now.AddSeconds(30);
                while (_exchangeRates == null) // 第一次執行稍等結果，最多等30秒
                {
                    Task.Delay(100).Wait();
                    if (DateTime.Now > timeOut)
                        throw new TimeoutException("Timeout while waiting for exchange rates");
                }
                return _exchangeRates;
            }
        }

        public static Task<string> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            if (ExchangeRates.TryGetValue((fromCurrency, toCurrency), out var rate))
                return Task.FromResult($"Exchange rate from {fromCurrency} to {toCurrency} is {rate.Exrate} (updated at {rate.UpdTime} UTC)");
            else if (ExchangeRates.TryGetValue((toCurrency, fromCurrency), out rate))
                return Task.FromResult($"Exchange rate from {toCurrency} to {fromCurrency} is {1 / rate.Exrate:n4} (updated at {rate.UpdTime} UTC)");
            else
                return Task.FromResult("No date available");
        }

        public static ChatTool CreateChatTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: nameof(GetExchangeRate),
                functionDescription: "Get the current exchage rate for fromCurrency to toCurrency",
                functionParameters: BinaryData.FromString("""
                    {
                        "type": "object",
                        "properties": {
                            "fromCurrency": {
                                "type": "string",
                                "description": "The currency you currently hold or want to exchange. ex: USD, TWD"
                            },
                            "toCurrency": {
                                "type": "string",
                                "description": "The currency you want to acquire. ex: USD, JPY"
                            }
                        },
                        "required": [ "fromCurrency", "toCurrency" ]
                    }
                    """)
           );
        }

        public static async Task<string> HandleToolCallAsync(ChatToolCall toolCall)
        {
            if (toolCall.FunctionName == nameof(GetExchangeRate))
            {
                try
                {
                    using JsonDocument argumentsDocument = JsonDocument.Parse(toolCall.FunctionArguments);
                    if (
                        argumentsDocument.RootElement.TryGetProperty("fromCurrency", out JsonElement fromCurrencyElement) && !string.IsNullOrEmpty(fromCurrencyElement.GetString()) &&
                        argumentsDocument.RootElement.TryGetProperty("toCurrency", out JsonElement toCurrencyElement) && !string.IsNullOrEmpty(toCurrencyElement.GetString())
                    )
                    {
                        return await GetExchangeRate(fromCurrencyElement.GetString()!, toCurrencyElement.GetString()!);
                    }
                    return "Invalid or missing 'fromCurrency'/'toCurrency' argument.";
                }
                catch (JsonException ex)
                {
                    return $"Error parsing JSON: {ex.Message}";
                }
            }
            return "Unknown function call.";
        }

    }
}