using System;
using System.Collections.ObjectModel; // 必须引用这个

namespace UniversalDataCollector.Models
{
    public class AppConfig
    {
        public string MesApiUrl { get; set; } = "http://localhost:5000/api/upload";

        // ★ 核心修改：类型必须是 ObservableCollection ★
        public ObservableCollection<FieldMapping> Mappings { get; set; } = new ObservableCollection<FieldMapping>();
    }

    public class FieldMapping
    {
        public string MesFieldName { get; set; }
        public int ColumnIndex { get; set; }
        public string DataType { get; set; } // Int, Double, DateTime, String
        public string DefaultValue { get; set; }
    }
}