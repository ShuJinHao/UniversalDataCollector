using System.Collections.Generic;

namespace UniversalDataCollector.Models
{
    // 监控模式枚举
    public enum MonitorType
    {
        SingleFile,  // 监控单个固定文件
        Folder       // 监控文件夹
    }

    // 文件类型枚举
    public enum FileType
    {
        Csv,
        Excel
    }

    public class FieldMapping
    {
        public string MesFieldName { get; set; }

        // ★★★ 修正点：统一命名为 ColumnIndex，解决 "未包含定义" 的报错 ★★★
        public int ColumnIndex { get; set; }

        public string DataType { get; set; } = "String";
        public string DefaultValue { get; set; }
    }

    public class AppConfig
    {
        // --- 核心监控配置 ---
        public MonitorType MonitorMode { get; set; } = MonitorType.SingleFile;

        public string TargetPath { get; set; } = "";
        public FileType FileType { get; set; } = FileType.Csv;
        public string FileNamePattern { get; set; } = "*.csv";

        // --- MES 配置 ---
        public string MesApiUrl { get; set; } = "http://10.101.200.15:9999/api/Insulation/Upload";

        public int ScanIntervalSeconds { get; set; } = 3;

        // --- 字段映射 ---
        public List<FieldMapping> Mappings { get; set; } = new List<FieldMapping>();

        public static AppConfig GetDefault()
        {
            return new AppConfig
            {
                Mappings = new List<FieldMapping>
                {
                    // 这里也统一使用 ColumnIndex
                    new FieldMapping { MesFieldName="packCode", ColumnIndex=0 },
                    new FieldMapping { MesFieldName="resistance", ColumnIndex=2, DataType="Number" },
                    new FieldMapping { MesFieldName="createTime", ColumnIndex=5, DataType="DateTime" }
                }
            };
        }
    }
}