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
        public class RowData
        {
            public int LineIndex { get; set; } // 真实的物理行号
            public string[] Columns { get; set; }
        }

        public class ReadResult
        {
            public List<RowData> NewRows { get; set; } = new List<RowData>();
            public string CurrentFileName { get; set; }
        }

        public ReadResult ReadData(MonitorConfig config, int lastProcessedLine)
        {
            string actualFilePath = "";
            var result = new ReadResult();

            // 1. 确定路径
            if (config.Mode == MonitorMode.File)
                actualFilePath = config.TargetFilePath;
            else
            {
                if (!Directory.Exists(config.TargetFolderPath)) return result;
                var dir = new DirectoryInfo(config.TargetFolderPath);
                string pattern = string.IsNullOrEmpty(config.FileNamePattern) ? "*.*" : config.FileNamePattern;
                var file = dir.GetFiles(pattern).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (file == null) return result;
                actualFilePath = file.FullName;
            }

            if (string.IsNullOrEmpty(actualFilePath) || !File.Exists(actualFilePath)) return result;
            result.CurrentFileName = actualFilePath;

            // 2. 读取数据
            try
            {
                using (var fs = new FileStream(actualFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    int currentLineIdx = 0;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        currentLineIdx++;
                        // ★ 核心修复：必须跳过已经处理过的物理行号 ★
                        if (currentLineIdx <= lastProcessedLine) continue;
                        if (currentLineIdx == 1) continue; // 跳过标题
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        // 使用更稳妥的 CSV 分隔逻辑
                        result.NewRows.Add(new RowData
                        {
                            LineIndex = currentLineIdx,
                            Columns = line.Split(',')
                        });
                    }
                }
            }
            catch { }
            return result;
        }
    }
}