using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    /// <summary>
    /// 模板增强服务：负责从 .tpld 文件中提取参数并合并到上传数据中
    /// </summary>
    public class TemplateEnricherService
    {
        private readonly ConfigService _configService = new ConfigService();
        private readonly TypeConversionService _converter = new TypeConversionService();

        private TemplateConfig _config;
        private FileSystemWatcher _watcher;

        // 缓存：Key=文件名(不含路径), Value=提取出的数据字典
        // 使用 ConcurrentDictionary 保证多线程安全
        private ConcurrentDictionary<string, Dictionary<string, object>> _cache
            = new ConcurrentDictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        public TemplateEnricherService()
        {
            // 构造时暂不加载配置，由 CollectorCoreService 统一调用 LoadConfig
        }

        /// <summary>
        /// 加载配置并启动文件监听
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                _config = _configService.Load<TemplateConfig>("TemplateConfig.json");

                if (_config != null && _config.EnableTemplateEnricher && !string.IsNullOrEmpty(_config.TemplateFolderPath))
                {
                    StartWatcher(_config.TemplateFolderPath);
                }
            }
            catch (Exception)
            {
                // 配置加载失败暂不中断主程序
            }
        }

        /// <summary>
        /// 核心方法：根据文件名获取模板参数
        /// </summary>
        /// <param name="fileName">模板文件名 (例如 "1P12-61.tpld")</param>
        /// <returns>提取出的参数字典</returns>
        public Dictionary<string, object> Enrich(string fileName)
        {
            var result = new Dictionary<string, object>();

            // 1. 基础校验
            if (_config == null || !_config.EnableTemplateEnricher) return result;
            if (string.IsNullOrWhiteSpace(fileName)) return result;

            fileName = fileName.Trim();

            // 2. 查缓存
            if (_cache.TryGetValue(fileName, out var cachedData))
            {
                return cachedData; // 命中缓存，直接返回
            }

            // 3. 读盘解析 (Cache Miss)
            var newData = ParseTemplateFile(fileName);

            // 4. 存入缓存
            _cache[fileName] = newData;

            return newData;
        }

        /// <summary>
        /// 实际去读文件并解析
        /// </summary>
        private Dictionary<string, object> ParseTemplateFile(string fileName)
        {
            var dic = new Dictionary<string, object>();

            if (string.IsNullOrEmpty(_config.TemplateFolderPath)) return dic;

            string fullPath = Path.Combine(_config.TemplateFolderPath, fileName);

            if (!File.Exists(fullPath)) return dic; // 文件不存在，返回空

            try
            {
                // 按行读取所有内容
                var lines = File.ReadAllLines(fullPath, Encoding.GetEncoding(_config.FileEncoding));

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // 遍历所有配置的正则规则
                    foreach (var rule in _config.ExtractionRules)
                    {
                        if (string.IsNullOrEmpty(rule.MatchPattern)) continue;

                        var match = Regex.Match(line, rule.MatchPattern);
                        if (match.Success && match.Groups.Count > 1)
                        {
                            string extractedValue = match.Groups[1].Value.Trim();

                            // 类型转换 (复用 TypeConversionService)
                            object finalValue = _converter.ConvertValue(extractedValue, rule.DataType);

                            // 存入结果
                            dic[rule.TargetKey] = finalValue;
                        }
                    }
                }
            }
            catch
            {
                // 解析出错暂不抛出，返回部分已解析的数据或空字典
            }

            return dic;
        }

        /// <summary>
        /// 启动文件夹监听，实现“热更新”
        /// </summary>
        private void StartWatcher(string path)
        {
            if (!Directory.Exists(path)) return;

            // 防止重复启动
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            try
            {
                _watcher = new FileSystemWatcher(path);
                _watcher.Filter = "*.tpld"; // 只监控 tpld 文件
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName; // 监控修改和重命名

                // 文件改变时，清空该文件的缓存
                _watcher.Changed += (s, e) => _cache.TryRemove(e.Name, out _);
                // 文件重命名时，把旧名字的缓存删掉
                _watcher.Renamed += (s, e) =>
                {
                    _cache.TryRemove(e.OldName, out _);
                    _cache.TryRemove(e.Name, out _);
                };

                _watcher.EnableRaisingEvents = true;
            }
            catch { }
        }
    }
}