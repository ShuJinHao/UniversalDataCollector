using System.Collections.Generic;

namespace UniversalDataCollector.Models
{
    public enum MonitorType
    {
        SingleFile,  // 监控单个固定文件 (死磕模式)
        Folder       // 监控文件夹 (自动找最新文件)
    }

    public enum FileType
    {
        Csv,
        Excel        // 预留 Excel 支持
    }

    public class FieldMapping
    {
        public string MesFieldName { get; set; }
        public int ColumnIndex { get; set; } // 通用列索引
        public string DataType { get; set; } = "String";
        public string DefaultValue { get; set; }
    }

    public class AppConfig
    {
        // --- 核心监控配置 ---
        public MonitorType MonitorMode { get; set; } = MonitorType.SingleFile;

        public string TargetPath { get; set; } = ""; // 文件路径 或 文件夹路径
        public FileType FileType { get; set; } = FileType.Csv;
        public string FileNamePattern { get; set; } = "*.csv"; // 文件夹模式下的过滤规则

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
                    new FieldMapping { MesFieldName="packCode", ColumnIndex=0 },
                    new FieldMapping { MesFieldName="resistance", ColumnIndex=2, DataType="Number" },
                    new FieldMapping { MesFieldName="createTime", ColumnIndex=5, DataType="DateTime" }
                }
            };
        }
    }
}