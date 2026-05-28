using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.DataImport
{
    public class CsvImporter
    {
        public async Task<List<HardnessTestData>> ImportAsync(string filePath, string sampleId, HardnessType hardnessType)
        {
            var results = new List<HardnessTestData>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = DetectDelimiter(filePath),
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, config);

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var record = new HardnessTestData
                {
                    SampleId = sampleId,
                    HardnessType = hardnessType,
                    X = GetFieldOrDefault(csv, "X", "x", "PositionX", "PosX"),
                    Y = GetFieldOrDefault(csv, "Y", "y", "PositionY", "PosY"),
                    Z = GetFieldOrDefault(csv, "Z", "z", "PositionZ", "PosZ"),
                    HardnessValue = GetFieldOrDefault(csv, "Hardness", "hardness", "Value", "HV", "HRC", "HRB", "HB"),
                    Load = GetNullableFieldOrDefault(csv, "Load", "load", "Force"),
                    IndentationDiagonal = GetNullableFieldOrDefault(csv, "Diagonal", "diagonal", "Indentation"),
                    TestTime = GetDateTimeFieldOrDefault(csv, "Time", "time", "TestTime", "Date"),
                    OperatorName = GetStringFieldOrDefault(csv, "Operator", "operator", "User"),
                    Remarks = GetStringFieldOrDefault(csv, "Remarks", "remarks", "Note", "Comment"),
                    IsValid = true
                };
                results.Add(record);
            }

            return results;
        }

        private string DetectDelimiter(string filePath)
        {
            var firstLine = File.ReadLines(filePath).FirstOrDefault();
            if (string.IsNullOrEmpty(firstLine))
                return ",";

            var possibleDelimiters = new[] { ",", ";", "\t", "|" };
            return possibleDelimiters
                .OrderByDescending(d => firstLine.Split(d).Length)
                .First();
        }

        private double GetFieldOrDefault(CsvReader csv, params string[] fieldNames)
        {
            foreach (var name in fieldNames)
            {
                if (csv.TryGetField(name, out double value))
                    return value;
            }
            return 0;
        }

        private double? GetNullableFieldOrDefault(CsvReader csv, params string[] fieldNames)
        {
            foreach (var name in fieldNames)
            {
                if (csv.TryGetField(name, out double value))
                    return value;
            }
            return null;
        }

        private DateTime GetDateTimeFieldOrDefault(CsvReader csv, params string[] fieldNames)
        {
            foreach (var name in fieldNames)
            {
                if (csv.TryGetField(name, out DateTime value))
                    return value;
            }
            return DateTime.Now;
        }

        private string GetStringFieldOrDefault(CsvReader csv, params string[] fieldNames)
        {
            foreach (var name in fieldNames)
            {
                if (csv.TryGetField(name, out string? value) && !string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }
    }
}
