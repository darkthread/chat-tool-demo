using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace chat_tool_demo
{
    public class SecureEnvVars
    {
        //REF: https://blog.darkthread.net/blog/secure-apikey-for-console-app/
        static byte[] additionalEntropy = { 2, 8, 8, 2, 5, 2, 5, 2 };

        public static string GetSecureEnvVar(string varName)
        {
            var val = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(val))
            {
                try
                {
                    val = Encoding.UTF8.GetString(
                        ProtectedData.Unprotect(Convert.FromBase64String(val), additionalEntropy, DataProtectionScope.CurrentUser));
                    return val;
                }
                catch
                {
                    Console.WriteLine("非有效加密格式，請重新輸入");
                }
            }
            Console.Write($"請設定[{varName}]：");
            val = Console.ReadLine() ?? string.Empty;
            //加密後存入環境變數
            var enc =
                Convert.ToBase64String(
                    ProtectedData.Protect(Encoding.UTF8.GetBytes(val), additionalEntropy, DataProtectionScope.CurrentUser));
            Environment.SetEnvironmentVariable(varName, enc, EnvironmentVariableTarget.User);
            return val;
        }

    }
}