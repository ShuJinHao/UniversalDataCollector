using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    public class RowData
    {
        public int LineIndex { get; set; }
        public string[] Columns { get; set; }
    }

    public class ReadResult
    {
        public List<RowData> NewRows { get; set; } = new List<RowData>();
        public string CurrentFileName { get; set; }
    }

    public class MonitorService
    {
        public List<string> GetMatchedFiles(MonitorConfig config)
        {
            if (config == null) return new List<string>();
            if (config.Mode == MonitorMode.File)
                return string.IsNullOrEmpty(config.TargetFilePath) ? new List<string>() : new List<string> { config.TargetFilePath };

            if (string.IsNullOrEmpty(config.TargetFolderPath) || !Directory.Exists(config.TargetFolderPath))
                return new List<string>();

            try
            {
                string pattern = string.IsNullOrEmpty(config.FileNamePattern) ? "*.*" : config.FileNamePattern;
                return Directory.GetFiles(config.TargetFolderPath, pattern)
                    .OrderBy(f => new FileInfo(f).CreationTime)
                    .ToList();
            }
            catch { return new List<string>(); }
        }

        public ReadResult ReadFileContent(MonitorConfig config, string filePath, int lastIdx)
        {
            var result = new ReadResult { CurrentFileName = filePath };
            if (!File.Exists(filePath)) return result;

            // 计算跳过阈值：取“上次进度”和“配置起始行”的最大值
            int skipThreshold = Math.Max(lastIdx, config.StartRowIndex - 1);
            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".xlsx" || ext == ".xls")
            {
                return ReadExcelBinary(filePath, skipThreshold);
            }
            return ReadCsvText(filePath, skipThreshold);
        }

        private ReadResult ReadExcelBinary(string filePath, int skipThreshold)
        {
            var result = new ReadResult { CurrentFileName = filePath };
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = false }
                    });

                    if (ds.Tables.Count > 0)
                    {
                        DataTable table = ds.Tables[0];
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            int currentLine = i + 1;
                            if (currentLine <= skipThreshold) continue;

                            DataRow row = table.Rows[i];
                            string[] cols = row.ItemArray.Select(x => x?.ToString() ?? "").ToArray();
                            result.NewRows.Add(new RowData { LineIndex = currentLine, Columns = cols });
                        }
                    }
                }
            }
            catch { }
            return result;
        }

        private ReadResult ReadCsvText(string filePath, int skipThreshold)
        {
            var result = new ReadResult { CurrentFileName = filePath };
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.GetEncoding("GB2312")))
                {
                    int currentLine = 0;
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        currentLine++;
                        if (currentLine <= skipThreshold) continue;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        result.NewRows.Add(new RowData { LineIndex = currentLine, Columns = line.Split(',') });
                    }
                }
            }
            catch { }
            return result;
        }
    }
}