using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    public class MonitorService
    {
        public class ReadResult
        {
            public List<string[]> NewRows { get; set; } = new List<string[]>();
            public string CurrentFileName { get; set; }
        }

        public ReadResult ReadData(MonitorConfig config, ref int lastRowIndex)
        {
            string actualFilePath = "";
            var result = new ReadResult();

            // 1. 确定文件路径
            if (config.Mode == MonitorMode.File)
            {
                actualFilePath = config.TargetFilePath;
            }
            else // 文件夹模式
            {
                if (!Directory.Exists(config.TargetFolderPath)) return result;

                try
                {
                    var dir = new DirectoryInfo(config.TargetFolderPath);
                    string pattern = string.IsNullOrEmpty(config.FileNamePattern) ? "*.*" : config.FileNamePattern;
                    // 找最新的文件
                    var file = dir.GetFiles(pattern).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                    if (file == null) return result;
                    actualFilePath = file.FullName;
                }
                catch { return result; }
            }

            if (string.IsNullOrEmpty(actualFilePath) || !File.Exists(actualFilePath)) return result;

            result.CurrentFileName = actualFilePath;

            // 2. 读取数据
            try
            {
                using (var fs = new FileStream(actualFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default)) // 如有乱码请改 Encoding.GetEncoding("GB2312")
                {
                    int lineIdx = 0;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineIdx++;
                        if (lineIdx <= lastRowIndex) continue; // 跳过旧行
                        if (lineIdx == 1) continue; // 跳过标题
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        result.NewRows.Add(line.Split(','));
                    }
                }
            }
            catch { }

            return result;
        }
    }
}