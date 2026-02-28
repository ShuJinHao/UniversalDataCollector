using System;
using System.Collections.Generic;

namespace UniversalDataCollector.Models
{
    /// <summary>
    /// 模板增强功能的总配置
    /// </summary>
    public class TemplateConfig
    {
        // 总开关
        public bool EnableTemplateEnricher { get; set; } = true;

        // 模板文件存放的文件夹路径 (例如 D:\Data\Templates)
        public string TemplateFolderPath { get; set; }

        // CSV 中哪一列是模板文件名？(例如第 14 列)
        public int SourceColumnIndex { get; set; }

        // 模板文件的编码 (默认 UTF-8，如果是中文老系统可能是 GB2312)
        public string FileEncoding { get; set; } = "UTF-8";

        // 提取规则列表
        public List<TemplateExtractionRule> ExtractionRules { get; set; } = new List<TemplateExtractionRule>();
    }

    /// <summary>
    /// 单条提取规则：定义如何从文本行中提取数据
    /// </summary>
    public class TemplateExtractionRule
    {
        // 目标 MES 字段名 (支持 specs.max_v 这种嵌套写法)
        public string TargetKey { get; set; }

        // 正则匹配表达式 (核心！例如 "^v_min=(.*)")
        public string MatchPattern { get; set; }

        // 数据类型 (Double, Int, String, DoubleArray) - 复用之前的逻辑
        public string DataType { get; set; }
    }
}