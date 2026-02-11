using Newtonsoft.Json;
using System.IO;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    public class MonitorConfigService
    {
        // 独立的文件名，和 MES 配置隔离开
        private string _cfgPath = "MonitorConfig.json";

        public MonitorConfig Load()
        {
            if (!File.Exists(_cfgPath))
            {
                var def = MonitorConfig.GetDefault();
                Save(def);
                return def;
            }
            try
            {
                return JsonConvert.DeserializeObject<MonitorConfig>(File.ReadAllText(_cfgPath));
            }
            catch
            {
                return MonitorConfig.GetDefault();
            }
        }

        public void Save(MonitorConfig config)
        {
            // 格式化保存，方便您直接用记事本打开修改
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(_cfgPath, json);
        }
    }
}