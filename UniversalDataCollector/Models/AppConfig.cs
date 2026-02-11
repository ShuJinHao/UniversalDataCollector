using System.Collections.Generic;

namespace UniversalDataCollector.Models
{
    public class FieldMapping
    {
        public string MesFieldName { get; set; }
        public int ColumnIndex { get; set; }

        // 默认类型改为 String，支持 Int, Double, DateTime
        public string DataType { get; set; } = "String";

        public string DefaultValue { get; set; }
    }

    public class AppConfig
    {
        public string MesApiUrl { get; set; } = "http://10.101.200.15:9999/api/Insulation/Upload";
        public List<FieldMapping> Mappings { get; set; } = new List<FieldMapping>();

        public static AppConfig GetDefault()
        {
            return new AppConfig
            {
                Mappings = new List<FieldMapping>
                {
                    // 示例配置：使用 Int 和 Double
                    new FieldMapping { MesFieldName="packCode", ColumnIndex=0, DataType="String" },
                    new FieldMapping { MesFieldName="resistance", ColumnIndex=2, DataType="Double" }, // 小数
                    new FieldMapping { MesFieldName="status", ColumnIndex=4, DataType="Int", DefaultValue="0" }, // 整数
                    new FieldMapping { MesFieldName="createTime", ColumnIndex=5, DataType="DateTime" }
                }
            };
        }
    }
}