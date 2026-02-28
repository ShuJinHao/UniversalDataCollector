using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UniversalDataCollector.Models;

namespace UniversalDataCollector.Services
{
    public class DataMappingService
    {
        private readonly TypeConversionService _converter = new TypeConversionService();

        /// <summary>
        /// 第一步：提取数据 (返回扁平字典，Key = MesFieldName)
        /// 这个结果专门用于 UI 显示和本地 CSV 备份
        /// </summary>
        public Dictionary<string, object> MapRow(string[] rawCols, IEnumerable<FieldMapping> mappings)
        {
            var flatData = new Dictionary<string, object>();

            if (mappings == null) return flatData;

            foreach (var map in mappings)
            {
                // 1. 获取原始单元格
                string rawCell = (rawCols != null && map.ColumnIndex < rawCols.Length && map.ColumnIndex >= 0)
                                 ? rawCols[map.ColumnIndex]
                                 : "";

                // 2. 正则提取
                string extractedValue = rawCell;
                if (!string.IsNullOrWhiteSpace(map.ExtractionRule))
                {
                    try
                    {
                        var match = Regex.Match(rawCell, map.ExtractionRule);
                        extractedValue = (match.Success && match.Groups.Count > 1) ? match.Groups[1].Value : "";
                    }
                    catch { extractedValue = ""; }
                }

                // 3. 类型转换
                object finalValue = _converter.ConvertValue(extractedValue, map.DataType);

                // 4. 直接存入扁平字典 (Key 保持配置原本的样子，如 "dcr.list1")
                flatData[map.MesFieldName] = finalValue;
            }

            return flatData;
        }

        /// <summary>
        /// 第二步：构造嵌套结构
        /// 这个结果专门用于发送给 MES 接口
        /// </summary>
        public Dictionary<string, object> BuildNestedData(Dictionary<string, object> flatData)
        {
            var root = new Dictionary<string, object>();
            foreach (var kvp in flatData)
            {
                SetNestedValue(root, kvp.Key, kvp.Value);
            }
            return root;
        }

        // 递归生成 JSON 对象
        private void SetNestedValue(Dictionary<string, object> root, string keyPath, object value)
        {
            if (string.IsNullOrEmpty(keyPath)) return;

            var parts = keyPath.Split('.');
            var currentDict = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string key = parts[i];
                if (!currentDict.ContainsKey(key) || !(currentDict[key] is Dictionary<string, object>))
                {
                    currentDict[key] = new Dictionary<string, object>();
                }
                currentDict = (Dictionary<string, object>)currentDict[key];
            }
            currentDict[parts[parts.Length - 1]] = value;
        }
    }
}