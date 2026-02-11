using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UniversalDataCollector.Services
{
    public class CsvReaderService
    {
        public class ReadResult
        {
            public List<string[]> NewRows { get; set; } = new List<string[]>();
            public int MaxRowIndex { get; set; }
        }

        public ReadResult ReadNewRows(string filePath, int startRowIndex)
        {
            var result = new ReadResult { MaxRowIndex = startRowIndex };

            // 使用共享读写模式
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default)) // 注意编码
            {
                int currentLine = 0;
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    currentLine++;
                    result.MaxRowIndex = currentLine;

                    if (currentLine <= startRowIndex) continue; // 跳过旧行
                    if (currentLine == 1) continue;             // 跳过标题
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var cols = line.Split(',');
                    result.NewRows.Add(cols);
                }
            }
            return result;
        }
    }
}