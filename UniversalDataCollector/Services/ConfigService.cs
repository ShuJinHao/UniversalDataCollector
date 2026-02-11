using Newtonsoft.Json;
using System.IO;

namespace UniversalDataCollector.Services
{
    public class ConfigService
    {
        // ★★★ 泛型加载：传入什么类型(T)就返回什么类型 ★★★
        public T Load<T>(string path) where T : new()
        {
            if (!File.Exists(path))
            {
                var def = new T();
                Save(path, def); // 文件不存在则创建默认的
                return def;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch
            {
                return new T();
            }
        }

        // ★★★ 泛型保存：支持传入路径和配置对象 ★★★
        public void Save<T>(string path, T config)
        {
            // 使用 Formatting.Indented 保证保存出来的 JSON 是格式化好看的
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}