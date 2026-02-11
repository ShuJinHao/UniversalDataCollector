using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UniversalDataCollector.Services
{
    public class MesService
    {
        public async Task<bool> UploadDynamicAsync(string url, Dictionary<string, object> data, Action<string> logAction)
        {
            try
            {
                // 1. 兼容性补丁 (TLS 1.2)
                try
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
                    ServicePointManager.Expect100Continue = false;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                }
                catch { }

                // 2. 序列化动态字典
                string jsonContent = JsonConvert.SerializeObject(data);
                byte[] byteData = Encoding.UTF8.GetBytes(jsonContent);

                // 3. 发送请求
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = byteData.Length;
                request.Timeout = 10000;
                request.KeepAlive = false;

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    stream.Write(byteData, 0, byteData.Length);
                }

                using (WebResponse response = await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string responseStr = reader.ReadToEnd();
                    // 简单的成功判断，您可以根据实际 MES 返回结构修改
                    if (responseStr.Contains("\"code\":200") || responseStr.Contains("\"success\":true"))
                    {
                        return true;
                    }
                    else
                    {
                        logAction($"MES拒绝: {responseStr}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                logAction($"MES通讯错误: {ex.Message}");
                return false;
            }
        }
    }

    // 扩展方法：解决 .NET 4.5 某些版本没有 GetRequestStreamAsync 的问题
    public static class WebRequestExtensions
    {
        public static Task<Stream> GetRequestStreamAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null);
        }

        public static Task<WebResponse> GetResponseAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
        }
    }
}