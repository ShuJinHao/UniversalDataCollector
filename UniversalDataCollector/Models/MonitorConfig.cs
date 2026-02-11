namespace UniversalDataCollector.Models
{
    // 监控模式枚举
    public enum MonitorMode
    {
        File = 0,   // 监控特定文件
        Folder = 1  // 监控文件夹
    }

    public class MonitorConfig
    {
        // 1. 核心开关：是监控文件还是文件夹？
        public MonitorMode Mode { get; set; } = MonitorMode.File;

        // 2. 如果选了文件，填这里：
        public string TargetFilePath { get; set; } = @"D:\Data\production.csv";

        // 3. 如果选了文件夹，填这里：
        public string TargetFolderPath { get; set; } = @"D:\Data\Logs\";

        // 4. 文件夹模式下的文件名过滤（比如 *.csv）
        public string FileNamePattern { get; set; } = "*.csv";

        // 5. 扫描频率（秒）
        public int IntervalSeconds { get; set; } = 3;

        // 预设默认值
        public static MonitorConfig GetDefault()
        {
            return new MonitorConfig();
        }
    }
}