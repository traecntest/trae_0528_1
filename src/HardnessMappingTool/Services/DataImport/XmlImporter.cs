using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.DataImport
{
    public class XmlImporter
    {
        public async Task<List<HardnessTestData>> ImportAsync(string filePath, string sampleId, HardnessType hardnessType)
        {
            var results = new List<HardnessTestData>();

            var xmlContent = await File.ReadAllTextAsync(filePath);
            var document = XDocument.Parse(xmlContent);

            var testPoints = document.Descendants()
                .FirstOrDefault(e => e.Name.LocalName.Equals("TestPoints", StringComparison.OrdinalIgnoreCase) ||
                                    e.Name.LocalName.Equals("Points", StringComparison.OrdinalIgnoreCase) ||
                                    e.Name.LocalName.Equals("Records", StringComparison.OrdinalIgnoreCase));

            if (testPoints != null)
            {
                foreach (var point in testPoints.Elements())
                {
                    var record = ParseTestPoint(point, sampleId, hardnessType);
                    if (record != null)
                    {
                        results.Add(record);
                    }
                }
            }
            else
            {
                foreach (var point in document.Descendants()
                    .Where(e => e.Name.LocalName.Equals("Point", StringComparison.OrdinalIgnoreCase) ||
                               e.Name.LocalName.Equals("TestPoint", StringComparison.OrdinalIgnoreCase) ||
                               e.Name.LocalName.Equals("Record", StringComparison.OrdinalIgnoreCase)))
                {
                    var record = ParseTestPoint(point, sampleId, hardnessType);
                    if (record != null)
                    {
                        results.Add(record);
                    }
                }
            }

            return results;
        }

        private HardnessTestData? ParseTestPoint(XElement element, string sampleId, HardnessType hardnessType)
        {
            try
            {
                return new HardnessTestData
                {
                    SampleId = GetElementValue(element, "SampleId", sampleId),
                    HardnessType = hardnessType,
                    X = GetDoubleValue(element, "X", "x", "PositionX", "PosX"),
                    Y = GetDoubleValue(element, "Y", "y", "PositionY", "PosY"),
                    Z = GetDoubleValue(element, "Z", "z", "PositionZ", "PosZ"),
                    HardnessValue = GetDoubleValue(element, "Hardness", "Value", "HV", "HRC", "HRB", "HB", "硬度值"),
                    Load = GetNullableDoubleValue(element, "Load", "Force", "载荷"),
                    IndentationDiagonal = GetNullableDoubleValue(element, "Diagonal", "Indentation", "压痕"),
                    TestTime = GetDateTimeValue(element, "Time", "TestTime", "Date", "测试时间"),
                    OperatorName = GetElementValue(element, "Operator", "User", "操作员"),
                    Remarks = GetElementValue(element, "Remarks", "Note", "Comment", "备注"),
                    IsValid = true
                };
            }
            catch
            {
                return null;
            }
        }

        private double GetDoubleValue(XElement element, params string[] elementNames)
        {
            foreach (var name in elementNames)
            {
                var el = element.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (el != null && double.TryParse(el.Value, out double result))
                    return result;

                var attr = element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (attr != null && double.TryParse(attr.Value, out result))
                    return result;
            }
            return 0;
        }

        private double? GetNullableDoubleValue(XElement element, params string[] elementNames)
        {
            foreach (var name in elementNames)
            {
                var el = element.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (el != null && double.TryParse(el.Value, out double result))
                    return result;

                var attr = element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (attr != null && double.TryParse(attr.Value, out result))
                    return result;
            }
            return null;
        }

        private DateTime GetDateTimeValue(XElement element, params string[] elementNames)
        {
            foreach (var name in elementNames)
            {
                var el = element.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (el != null && DateTime.TryParse(el.Value, out DateTime result))
                    return result;

                var attr = element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (attr != null && DateTime.TryParse(attr.Value, out result))
                    return result;
            }
            return DateTime.Now;
        }

        private string GetElementValue(XElement element, params string[] elementNames)
        {
            foreach (var name in elementNames)
            {
                var el = element.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (el != null && !string.IsNullOrEmpty(el.Value))
                    return el.Value;

                var attr = element.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (attr != null && !string.IsNullOrEmpty(attr.Value))
                    return attr.Value;
            }
            return string.Empty;
        }
    }
}
