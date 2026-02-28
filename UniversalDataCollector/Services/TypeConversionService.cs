using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalDataCollector.Services
{
    /// <summary>
    /// 职责：负责将原始字符串清洗并转换为强类型数据
    /// </summary>
    public class TypeConversionService
    {
        public object ConvertValue(string rawValue, string targetType)
        {
            // 1. 基础清洗 (去除首尾空白，去除 CSV 可能带的单引号等脏字符)
            string cleanValue = rawValue?.Trim().Trim('\'');

            if (string.IsNullOrWhiteSpace(cleanValue))
            {
                return GetDefaultValue(targetType);
            }

            try
            {
                switch (targetType)
                {
                    case "Int":
                        return int.TryParse(cleanValue, out int i) ? i : 0;

                    case "Double":
                        return double.TryParse(cleanValue, out double d) ? d : 0.0;

                    case "Bool":
                        return bool.TryParse(cleanValue, out bool b) ? b : false;

                    case "DateTime":
                        // 尝试解析日期，如果格式怪异可在后续增加 ParseExact
                        return DateTime.TryParse(cleanValue, out DateTime dt) ? dt : (DateTime?)null;

                    case "DoubleArray": // ★ 处理空格分隔的浮点数数组 ★
                        return ParseDoubleArray(cleanValue);

                    case "String":
                    default:
                        return cleanValue;
                }
            }
            catch
            {
                // 转换失败（如遇到 "NG" 字符强转 Double），为了“不丢弃行”，返回默认值
                return GetDefaultValue(targetType);
            }
        }

        private List<double> ParseDoubleArray(string input)
        {
            var list = new List<double>();
            if (string.IsNullOrWhiteSpace(input)) return list;

            // 应对 "3.4031 3.4016" 这种空格分隔
            // 也兼容 "1.2, 3.4" 逗号分隔
            var parts = input.Split(new[] { ' ', ',', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (double.TryParse(part, out double val))
                {
                    list.Add(val);
                }
                // 如果遇到解析不了的部分（如数组里夹杂了文字），选择跳过该项，保留其他有效数字
            }
            return list;
        }

        private object GetDefaultValue(string targetType)
        {
            switch (targetType)
            {
                case "Int": return 0;
                case "Double": return 0.0;
                case "Bool": return false;
                case "DoubleArray": return new List<double>(); // 返回空数组而不是 null
                default: return null; // String 或 DateTime 返回 null
            }
        }
    }
}