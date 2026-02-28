using System;
using System.Collections.ObjectModel;

namespace UniversalDataCollector.Models
{
    public class AppConfig
    {
        public string MesApiUrl { get; set; } = "http://localhost:5000/api/upload";
        public ObservableCollection<FieldMapping> Mappings { get; set; } = new ObservableCollection<FieldMapping>();
    }

    public class FieldMapping
    {
        // MES 字段名，支持点号表示嵌套，例如 "dcr.list1" 会生成 {"dcr": {"list1": ...}}
        public string MesFieldName { get; set; }

        // Excel/CSV 列索引
        public int ColumnIndex { get; set; }

        // 数据类型：Int, Double, String, DateTime, Bool, DoubleArray (新支持)
        public string DataType { get; set; }

        // ★ 新增：正则表达式提取规则 ★
        // 如果为空，则取整格数据；如果有值，则提取正则 Group[1] 的内容
        // 例如：Cell_DCR1=\((.*?)\)
        public string ExtractionRule { get; set; }

        public string DefaultValue { get; set; }
    }
}