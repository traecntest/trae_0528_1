using System;
using System.Collections.Generic;
using System.Linq;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.DataProcessing
{
    public class DataValidationService
    {
        private readonly Dictionary<HardnessType, (double Min, double Max)> _validRanges = new()
        {
            { HardnessType.Rockwell_B, (0, 130) },
            { HardnessType.Rockwell_C, (20, 70) },
            { HardnessType.Brinell, (100, 600) },
            { HardnessType.Vickers, (50, 1500) },
            { HardnessType.Knoop, (50, 1500) },
            { HardnessType.Shore, (0, 100) }
        };

        public ValidationResult ValidateData(IEnumerable<HardnessTestData> dataList)
        {
            var result = new ValidationResult();
            var dataArray = dataList.ToArray();

            foreach (var data in dataArray)
            {
                var errors = new List<string>();

                if (string.IsNullOrWhiteSpace(data.SampleId))
                    errors.Add("样品编号不能为空");

                if (data.X < 0 || data.Y < 0)
                    errors.Add("坐标值不能为负数");

                if (_validRanges.TryGetValue(data.HardnessType, out var range))
                {
                    if (data.HardnessValue < range.Min || data.HardnessValue > range.Max)
                    {
                        errors.Add($"硬度值超出有效范围 ({range.Min} - {range.Max})");
                        data.IsValid = false;
                    }
                }

                if (errors.Any())
                {
                    result.InvalidRecords.Add(data);
                    result.ErrorMessages[data.Id] = errors;
                }
                else
                {
                    result.ValidRecords.Add(data);
                }
            }

            result.TotalCount = dataArray.Length;
            result.ValidCount = result.ValidRecords.Count;
            result.InvalidCount = result.InvalidRecords.Count;

            return result;
        }

        public bool ValidateSingleRecord(HardnessTestData data)
        {
            if (string.IsNullOrWhiteSpace(data.SampleId))
                return false;

            if (data.X < 0 || data.Y < 0)
                return false;

            if (_validRanges.TryGetValue(data.HardnessType, out var range))
            {
                if (data.HardnessValue < range.Min || data.HardnessValue > range.Max)
                    return false;
            }

            return true;
        }
    }

    public class ValidationResult
    {
        public List<HardnessTestData> ValidRecords { get; set; } = new();
        public List<HardnessTestData> InvalidRecords { get; set; } = new();
        public Dictionary<int, List<string>> ErrorMessages { get; set; } = new();
        public int TotalCount { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
    }
}
