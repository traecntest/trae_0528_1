using System;
using System.Collections.Generic;
using System.Linq;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.DataProcessing
{
    public class NoiseReductionService
    {
        public List<HardnessTestData> ApplyMovingAverageFilter(List<HardnessTestData> data, int windowSize = 3)
        {
            if (windowSize < 3) windowSize = 3;
            if (windowSize % 2 == 0) windowSize++;

            var sortedData = data.OrderBy(d => d.X).ThenBy(d => d.Y).ToList();
            var result = new List<HardnessTestData>();

            for (int i = 0; i < sortedData.Count; i++)
            {
                var neighbors = GetNeighbors(sortedData, i, windowSize);
                var averagedData = (HardnessTestData)sortedData[i].Clone();
                averagedData.HardnessValue = neighbors.Average(n => n.HardnessValue);
                result.Add(averagedData);
            }

            return result;
        }

        public List<HardnessTestData> ApplyGaussianFilter(List<HardnessTestData> data, double sigma = 1.0)
        {
            var result = new List<HardnessTestData>();
            var sortedData = data.OrderBy(d => d.X).ThenBy(d => d.Y).ToList();

            for (int i = 0; i < sortedData.Count; i++)
            {
                var weightedSum = 0.0;
                var weightTotal = 0.0;

                for (int j = 0; j < sortedData.Count; j++)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(sortedData[i].X - sortedData[j].X, 2) +
                        Math.Pow(sortedData[i].Y - sortedData[j].Y, 2));

                    var weight = Math.Exp(-distance * distance / (2 * sigma * sigma));
                    weightedSum += sortedData[j].HardnessValue * weight;
                    weightTotal += weight;
                }

                var filteredData = (HardnessTestData)sortedData[i].Clone();
                filteredData.HardnessValue = weightedSum / weightTotal;
                result.Add(filteredData);
            }

            return result;
        }

        private List<HardnessTestData> GetNeighbors(List<HardnessTestData> data, int index, int windowSize)
        {
            var halfWindow = windowSize / 2;
            var start = Math.Max(0, index - halfWindow);
            var end = Math.Min(data.Count - 1, index + halfWindow);
            return data.GetRange(start, end - start + 1);
        }
    }

    public class OutlierRemovalService
    {
        public List<HardnessTestData> RemoveOutliersByIQR(List<HardnessTestData> data, double threshold = 1.5)
        {
            var values = data.Select(d => d.HardnessValue).OrderBy(v => v).ToList();
            var q1 = values[values.Count / 4];
            var q3 = values[values.Count * 3 / 4];
            var iqr = q3 - q1;
            var lowerBound = q1 - threshold * iqr;
            var upperBound = q3 + threshold * iqr;

            return data.Where(d => d.HardnessValue >= lowerBound && d.HardnessValue <= upperBound).ToList();
        }

        public List<HardnessTestData> RemoveOutliersByZScore(List<HardnessTestData> data, double threshold = 3.0)
        {
            var mean = data.Average(d => d.HardnessValue);
            var stdDev = Math.Sqrt(data.Average(d => Math.Pow(d.HardnessValue - mean, 2)));

            return data.Where(d =>
            {
                var zScore = Math.Abs((d.HardnessValue - mean) / stdDev);
                return zScore <= threshold;
            }).ToList();
        }

        public List<HardnessTestData> RemoveOutliersByGrubbs(List<HardnessTestData> data, double confidenceLevel = 0.95)
        {
            var result = new List<HardnessTestData>(data);
            bool foundOutlier;

            do
            {
                foundOutlier = false;
                if (result.Count < 3) break;

                var mean = result.Average(d => d.HardnessValue);
                var stdDev = Math.Sqrt(result.Average(d => Math.Pow(d.HardnessValue - mean, 2)));

                if (stdDev == 0) break;

                var maxDeviation = result.Max(d => Math.Abs(d.HardnessValue - mean));
                var grubbsStatistic = maxDeviation / stdDev;

                var n = result.Count;
                var t = 1.96;
                var criticalValue = ((n - 1) / Math.Sqrt(n)) * Math.Sqrt(t * t / (n - 2 + t * t));

                if (grubbsStatistic > criticalValue)
                {
                    var outlier = result.OrderByDescending(d => Math.Abs(d.HardnessValue - mean)).First();
                    result.Remove(outlier);
                    foundOutlier = true;
                }

            } while (foundOutlier);

            return result;
        }
    }

    public class DataNormalizationService
    {
        public List<HardnessTestData> NormalizeData(List<HardnessTestData> data)
        {
            var minValue = data.Min(d => d.HardnessValue);
            var maxValue = data.Max(d => d.HardnessValue);
            var range = maxValue - minValue;

            if (range == 0) return data;

            return data.Select(d =>
            {
                var normalized = (HardnessTestData)d.Clone();
                normalized.HardnessValue = (d.HardnessValue - minValue) / range;
                return normalized;
            }).ToList();
        }

        public List<HardnessTestData> StandardizeData(List<HardnessTestData> data)
        {
            var mean = data.Average(d => d.HardnessValue);
            var stdDev = Math.Sqrt(data.Average(d => Math.Pow(d.HardnessValue - mean, 2)));

            if (stdDev == 0) return data;

            return data.Select(d =>
            {
                var standardized = (HardnessTestData)d.Clone();
                standardized.HardnessValue = (d.HardnessValue - mean) / stdDev;
                return standardized;
            }).ToList();
        }

        public List<HardnessTestData> MinMaxScale(List<HardnessTestData> data, double min = 0, double max = 1)
        {
            var originalMin = data.Min(d => d.HardnessValue);
            var originalMax = data.Max(d => d.HardnessValue);
            var originalRange = originalMax - originalMin;

            if (originalRange == 0) return data;

            var targetRange = max - min;

            return data.Select(d =>
            {
                var scaled = (HardnessTestData)d.Clone();
                scaled.HardnessValue = min + ((d.HardnessValue - originalMin) / originalRange) * targetRange;
                return scaled;
            }).ToList();
        }
    }
}
