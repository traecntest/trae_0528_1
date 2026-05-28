using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.DataImport
{
    public class ExcelImporter
    {
        public ExcelImporter()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<HardnessTestData> Import(string filePath, string sampleId, HardnessType hardnessType, int worksheetIndex = 0)
        {
            var results = new List<HardnessTestData>();

            var fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);

            var worksheet = package.Workbook.Worksheets[worksheetIndex];
            var rowCount = worksheet.Dimension?.Rows ?? 0;
            var colCount = worksheet.Dimension?.Columns ?? 0;

            if (rowCount < 2)
                return results;

            var columnMappings = DetectColumnMappings(worksheet, colCount);

            for (int row = 2; row <= rowCount; row++)
            {
                var record = new HardnessTestData
                {
                    SampleId = sampleId,
                    HardnessType = hardnessType,
                    X = GetDoubleValue(worksheet, row, columnMappings, "X"),
                    Y = GetDoubleValue(worksheet, row, columnMappings, "Y"),
                    Z = GetDoubleValue(worksheet, row, columnMappings, "Z"),
                    HardnessValue = GetDoubleValue(worksheet, row, columnMappings, "Hardness"),
                    Load = GetNullableDoubleValue(worksheet, row, columnMappings, "Load"),
                    IndentationDiagonal = GetNullableDoubleValue(worksheet, row, columnMappings, "Diagonal"),
                    TestTime = GetDateTimeValue(worksheet, row, columnMappings, "Time"),
                    OperatorName = GetStringValue(worksheet, row, columnMappings, "Operator"),
                    Remarks = GetStringValue(worksheet, row, columnMappings, "Remarks"),
                    IsValid = true
                };
                results.Add(record);
            }

            return results;
        }

        private Dictionary<string, int> DetectColumnMappings(ExcelWorksheet worksheet, int colCount)
        {
            var mappings = new Dictionary<string, int>();
            var keywords = new Dictionary<string, string[]>
            {
                { "X", new[] { "X", "x", "PositionX", "PosX", "横坐标" } },
                { "Y", new[] { "Y", "y", "PositionY", "PosY", "纵坐标" } },
                { "Z", new[] { "Z", "z", "PositionZ", "PosZ" } },
                { "Hardness", new[] { "Hardness", "Value", "HV", "HRC", "HRB", "HB", "硬度值" } },
                { "Load", new[] { "Load", "Force", "载荷" } },
                { "Diagonal", new[] { "Diagonal", "Indentation", "压痕" } },
                { "Time", new[] { "Time", "TestTime", "Date", "测试时间" } },
                { "Operator", new[] { "Operator", "User", "操作员" } },
                { "Remarks", new[] { "Remarks", "Note", "Comment", "备注" } }
            };

            for (int col = 1; col <= colCount; col++)
            {
                var headerValue = worksheet.Cells[1, col].Text;
                if (string.IsNullOrEmpty(headerValue))
                    continue;

                foreach (var keyword in keywords)
                {
                    if (keyword.Value.Any(k => headerValue.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        if (!mappings.ContainsKey(keyword.Key))
                        {
                            mappings[keyword.Key] = col;
                        }
                    }
                }
            }

            return mappings;
        }

        private double GetDoubleValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> mappings, string key)
        {
            if (mappings.TryGetValue(key, out int col))
            {
                var cellValue = worksheet.Cells[row, col].Value;
                if (cellValue != null && double.TryParse(cellValue.ToString(), out double result))
                    return result;
            }
            return 0;
        }

        private double? GetNullableDoubleValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> mappings, string key)
        {
            if (mappings.TryGetValue(key, out int col))
            {
                var cellValue = worksheet.Cells[row, col].Value;
                if (cellValue != null && double.TryParse(cellValue.ToString(), out double result))
                    return result;
            }
            return null;
        }

        private DateTime GetDateTimeValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> mappings, string key)
        {
            if (mappings.TryGetValue(key, out int col))
            {
                var cellValue = worksheet.Cells[row, col].Value;
                if (cellValue is DateTime dateTime)
                    return dateTime;
                if (cellValue != null && DateTime.TryParse(cellValue.ToString(), out var parsed))
                    return parsed;
            }
            return DateTime.Now;
        }

        private string GetStringValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> mappings, string key)
        {
            if (mappings.TryGetValue(key, out int col))
            {
                return worksheet.Cells[row, col].Text ?? string.Empty;
            }
            return string.Empty;
        }

        public List<string> GetWorksheetNames(string filePath)
        {
            var names = new List<string>();
            var fileInfo = new FileInfo(filePath);
            using var package = new ExcelPackage(fileInfo);

            foreach (var sheet in package.Workbook.Worksheets)
            {
                names.Add(sheet.Name);
            }
            return names;
        }
    }
}
