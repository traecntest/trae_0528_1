using System;
using System.Collections.Generic;
using System.Linq;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.Distribution
{
    public enum InterpolationMethod
    {
        NearestNeighbor,
        InverseDistanceWeighting,
        Kriging,
        Bilinear
    }

    public class HardnessDistributionCalculator
    {
        public DistributionMatrix Calculate2DDistribution(
            List<HardnessTestData> data,
            int gridSizeX = 50,
            int gridSizeY = 50,
            InterpolationMethod method = InterpolationMethod.InverseDistanceWeighting)
        {
            var validData = data.Where(d => d.IsValid).ToList();
            if (validData.Count == 0)
                return new DistributionMatrix();

            var minX = validData.Min(d => d.X);
            var maxX = validData.Max(d => d.X);
            var minY = validData.Min(d => d.Y);
            var maxY = validData.Max(d => d.Y);

            var stepX = (maxX - minX) / (gridSizeX - 1);
            var stepY = (maxY - minY) / (gridSizeY - 1);

            var matrix = new DistributionMatrix
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                StepX = stepX,
                StepY = stepY,
                Rows = gridSizeY,
                Columns = gridSizeX,
                Values = new double[gridSizeY, gridSizeX]
            };

            for (int i = 0; i < gridSizeY; i++)
            {
                for (int j = 0; j < gridSizeX; j++)
                {
                    var x = minX + j * stepX;
                    var y = minY + i * stepY;
                    matrix.Values[i, j] = Interpolate(x, y, validData, method);
                }
            }

            return matrix;
        }

        private double Interpolate(double x, double y, List<HardnessTestData> data, InterpolationMethod method)
        {
            return method switch
            {
                InterpolationMethod.NearestNeighbor => NearestNeighbor(x, y, data),
                InterpolationMethod.InverseDistanceWeighting => InverseDistanceWeighting(x, y, data),
                InterpolationMethod.Kriging => Kriging(x, y, data),
                InterpolationMethod.Bilinear => Bilinear(x, y, data),
                _ => InverseDistanceWeighting(x, y, data)
            };
        }

        private double NearestNeighbor(double x, double y, List<HardnessTestData> data)
        {
            var nearest = data.OrderBy(d =>
                Math.Sqrt(Math.Pow(d.X - x, 2) + Math.Pow(d.Y - y, 2))).First();
            return nearest.HardnessValue;
        }

        private double InverseDistanceWeighting(double x, double y, List<HardnessTestData> data, double power = 2)
        {
            var weightedSum = 0.0;
            var weightSum = 0.0;

            foreach (var point in data)
            {
                var distance = Math.Sqrt(Math.Pow(point.X - x, 2) + Math.Pow(point.Y - y, 2));

                if (distance < 1e-10)
                    return point.HardnessValue;

                var weight = 1.0 / Math.Pow(distance, power);
                weightedSum += point.HardnessValue * weight;
                weightSum += weight;
            }

            return weightSum > 0 ? weightedSum / weightSum : data.Average(d => d.HardnessValue);
        }

        private double Kriging(double x, double y, List<HardnessTestData> data)
        {
            var mean = data.Average(d => d.HardnessValue);
            var distances = data.Select(d => Math.Sqrt(Math.Pow(d.X - x, 2) + Math.Pow(d.Y - y, 2))).ToList();
            var weights = distances.Select(d => Math.Exp(-d)).ToList();
            var weightSum = weights.Sum();

            if (weightSum < 1e-10)
                return mean;

            var result = 0.0;
            for (int i = 0; i < data.Count; i++)
            {
                result += (data[i].HardnessValue - mean) * weights[i];
            }

            return mean + result / weightSum;
        }

        private double Bilinear(double x, double y, List<HardnessTestData> data)
        {
            var sortedByX = data.OrderBy(d => Math.Abs(d.X - x)).Take(4).ToList();
            var sortedByY = sortedByX.OrderBy(d => Math.Abs(d.Y - y)).ToList();

            if (sortedByY.Count < 4)
                return InverseDistanceWeighting(x, y, data);

            var q11 = sortedByY[0];
            var q12 = sortedByY[1];
            var q21 = sortedByY[2];
            var q22 = sortedByY[3];

            var x1 = Math.Min(q11.X, q21.X);
            var x2 = Math.Max(q11.X, q21.X);
            var y1 = Math.Min(q11.Y, q12.Y);
            var y2 = Math.Max(q11.Y, q12.Y);

            if (Math.Abs(x2 - x1) < 1e-10 || Math.Abs(y2 - y1) < 1e-10)
                return InverseDistanceWeighting(x, y, data);

            var r1 = ((x2 - x) / (x2 - x1) * q11.HardnessValue) + ((x - x1) / (x2 - x1) * q21.HardnessValue);
            var r2 = ((x2 - x) / (x2 - x1) * q12.HardnessValue) + ((x - x1) / (x2 - x1) * q22.HardnessValue);
            var p = ((y2 - y) / (y2 - y1) * r1) + ((y - y1) / (y2 - y1) * r2);

            return p;
        }

        public StatisticsResult CalculateStatistics(List<HardnessTestData> data)
        {
            var validData = data.Where(d => d.IsValid).Select(d => d.HardnessValue).ToList();

            if (validData.Count == 0)
                return new StatisticsResult();

            var sorted = validData.OrderBy(v => v).ToList();
            var mean = validData.Average();
            var variance = validData.Average(v => Math.Pow(v - mean, 2));

            return new StatisticsResult
            {
                Mean = mean,
                StandardDeviation = Math.Sqrt(variance),
                Variance = variance,
                Minimum = sorted.First(),
                Maximum = sorted.Last(),
                Median = sorted.Count % 2 == 0
                    ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2
                    : sorted[sorted.Count / 2],
                Count = data.Count,
                ValidCount = validData.Count
            };
        }

        public double[,,] Calculate3DDistribution(
            List<HardnessTestData> data,
            int gridSizeX = 20,
            int gridSizeY = 20,
            int gridSizeZ = 10,
            InterpolationMethod method = InterpolationMethod.InverseDistanceWeighting)
        {
            var validData = data.Where(d => d.IsValid).ToList();
            if (validData.Count == 0)
                return new double[0, 0, 0];

            var minX = validData.Min(d => d.X);
            var maxX = validData.Max(d => d.X);
            var minY = validData.Min(d => d.Y);
            var maxY = validData.Max(d => d.Y);
            var minZ = validData.Min(d => d.Z);
            var maxZ = validData.Max(d => d.Z);

            var stepX = (maxX - minX) / (gridSizeX - 1);
            var stepY = (maxY - minY) / (gridSizeY - 1);
            var stepZ = (maxZ - minZ) / (gridSizeZ - 1);

            var result = new double[gridSizeZ, gridSizeY, gridSizeX];

            for (int z = 0; z < gridSizeZ; z++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    for (int x = 0; x < gridSizeX; x++)
                    {
                        var px = minX + x * stepX;
                        var py = minY + y * stepY;
                        var pz = minZ + z * stepZ;
                        result[z, y, x] = Interpolate3D(px, py, pz, validData, method);
                    }
                }
            }

            return result;
        }

        private double Interpolate3D(double x, double y, double z, List<HardnessTestData> data, InterpolationMethod method)
        {
            if (method == InterpolationMethod.NearestNeighbor)
            {
                var nearest = data.OrderBy(d =>
                    Math.Sqrt(Math.Pow(d.X - x, 2) + Math.Pow(d.Y - y, 2) + Math.Pow(d.Z - z, 2))).First();
                return nearest.HardnessValue;
            }

            var weightedSum = 0.0;
            var weightSum = 0.0;

            foreach (var point in data)
            {
                var distance = Math.Sqrt(
                    Math.Pow(point.X - x, 2) +
                    Math.Pow(point.Y - y, 2) +
                    Math.Pow(point.Z - z, 2));

                if (distance < 1e-10)
                    return point.HardnessValue;

                var weight = 1.0 / Math.Pow(distance, 2);
                weightedSum += point.HardnessValue * weight;
                weightSum += weight;
            }

            return weightSum > 0 ? weightedSum / weightSum : data.Average(d => d.HardnessValue);
        }
    }
}
