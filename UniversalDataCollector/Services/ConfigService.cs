using Newtonsoft.Json;
using System.IO;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    public class ConfigService
    {
        private string _cfgPath = "AppConfig.json";

        public AppConfig Load()
        {
            if (!File.Exists(_cfgPath))
            {
                var def = AppConfig.GetDefault();
                Save(def);
                return def;
            }
            return JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(_cfgPath));
        }

        public void Save(AppConfig config)
        {
            // ★★★ 修复点：显式指定 Newtonsoft.Json.Formatting，解决和 System.Xml 的冲突 ★★★
            string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_cfgPath, json);
        }
    }
}