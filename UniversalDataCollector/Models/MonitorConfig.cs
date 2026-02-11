using System;

namespace UniversalDataCollector.Models
{
    public enum MonitorMode { File = 0, Folder = 1 }

    public class MonitorConfig
    {
        public MonitorMode Mode { get; set; } = MonitorMode.File;
        public string TargetFilePath { get; set; } = "";
        public string TargetFolderPath { get; set; } = "";
        public string FileNamePattern { get; set; } = "*.xlsx";
        public int IntervalSeconds { get; set; } = 3;

        // 起始采集行 (1代表第一行)
        public int StartRowIndex { get; set; } = 1;

        public static MonitorConfig GetDefault()
        {
            return new MonitorConfig();
        }
    }
}