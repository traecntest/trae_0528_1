using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Services.TestData
{
    public class TestDataGenerator
    {
        private readonly Random _random = new();

        public List<HardnessTestData> GenerateTestData(
            string sampleId,
            HardnessType hardnessType,
            int pointCount = 100,
            double minX = 0,
            double maxX = 100,
            double minY = 0,
            double maxY = 100,
            double baseHardness = 250,
            double hardnessVariation = 50,
            bool addGradient = true,
            bool addOutliers = true,
            double outlierPercentage = 5)
        {
            var data = new List<HardnessTestData>();

            for (int i = 0; i < pointCount; i++)
            {
                var x = minX + _random.NextDouble() * (maxX - minX);
                var y = minY + _random.NextDouble() * (maxY - minY);

                var hardness = baseHardness;

                if (addGradient)
                {
                    hardness += (x / (maxX - minX) - 0.5) * hardnessVariation * 0.5;
                    hardness += Math.Sin(y / (maxY - minY) * Math.PI * 2) * hardnessVariation * 0.3;
                    hardness += Math.Cos(x / (maxX - minX) * Math.PI * 3) * hardnessVariation * 0.2;
                }

                hardness += (_random.NextDouble() - 0.5) * hardnessVariation * 0.3;

                if (addOutliers && _random.NextDouble() * 100 < outlierPercentage)
                {
                    hardness += (_random.NextDouble() - 0.5) * hardnessVariation * 3;
                }

                data.Add(new HardnessTestData
                {
                    SampleId = sampleId,
                    HardnessType = hardnessType,
                    X = Math.Round(x, 2),
                    Y = Math.Round(y, 2),
                    Z = 0,
                    HardnessValue = Math.Round(hardness, 2),
                    Load = GetTypicalLoad(hardnessType),
                    IndentationDiagonal = Math.Round(_random.NextDouble() * 0.5 + 0.1, 3),
                    TestTime = DateTime.Now.AddMinutes(-_random.Next(60 * 24)),
                    OperatorName = "AutoTest",
                    Remarks = $"Test point {i + 1}",
                    IsValid = true
                });
            }

            return data.OrderBy(d => d.X).ThenBy(d => d.Y).ToList();
        }

        public List<HardnessTestData> GenerateGridData(
            string sampleId,
            HardnessType hardnessType,
            int gridX = 10,
            int gridY = 10,
            double minX = 0,
            double maxX = 100,
            double minY = 0,
            double maxY = 100,
            double baseHardness = 250)
        {
            var data = new List<HardnessTestData>();
            var stepX = (maxX - minX) / (gridX - 1);
            var stepY = (maxY - minY) / (gridY - 1);

            for (int i = 0; i < gridY; i++)
            {
                for (int j = 0; j < gridX; j++)
                {
                    var x = minX + j * stepX;
                    var y = minY + i * stepY;

                    var centerX = (minX + maxX) / 2;
                    var centerY = (minY + maxY) / 2;
                    var distance = Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    var maxDistance = Math.Sqrt(Math.Pow(maxX - minX, 2) + Math.Pow(maxY - minY, 2)) / 2;

                    var hardness = baseHardness + Math.Sin(distance / maxDistance * Math.PI) * 50;
                    hardness += (_random.NextDouble() - 0.5) * 10;

                    data.Add(new HardnessTestData
                    {
                        SampleId = sampleId,
                        HardnessType = hardnessType,
                        X = Math.Round(x, 2),
                        Y = Math.Round(y, 2),
                        Z = 0,
                        HardnessValue = Math.Round(hardness, 2),
                        Load = GetTypicalLoad(hardnessType),
                        IndentationDiagonal = Math.Round(_random.NextDouble() * 0.5 + 0.1, 3),
                        TestTime = DateTime.Now,
                        OperatorName = "AutoTest",
                        Remarks = $"Grid ({j}, {i})",
                        IsValid = true
                    });
                }
            }

            return data;
        }

        private double? GetTypicalLoad(HardnessType hardnessType)
        {
            return hardnessType switch
            {
                HardnessType.Vickers => 1.0,
                HardnessType.Brinell => 3000.0,
                HardnessType.Rockwell_C => 150.0,
                HardnessType.Rockwell_B => 100.0,
                _ => null
            };
        }

        public void SaveToCsv(List<HardnessTestData> data, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("SampleId,HardnessType,X,Y,Z,HardnessValue,Load,IndentationDiagonal,TestTime,Operator,Remarks");

            foreach (var point in data)
            {
                writer.WriteLine($"{point.SampleId},{point.HardnessType},{point.X},{point.Y},{point.Z}," +
                                 $"{point.HardnessValue},{point.Load},{point.IndentationDiagonal}," +
                                 $"{point.TestTime:yyyy-MM-dd HH:mm:ss},{point.OperatorName},{point.Remarks}");
            }
        }

        public void SaveToXml(List<HardnessTestData> data, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            writer.WriteLine("<HardnessTestData>");
            writer.WriteLine($"  <SampleId>{data.FirstOrDefault()?.SampleId ?? "Unknown"}</SampleId>");
            writer.WriteLine("  <TestPoints>");

            foreach (var point in data)
            {
                writer.WriteLine("    <TestPoint>");
                writer.WriteLine($"      <X>{point.X}</X>");
                writer.WriteLine($"      <Y>{point.Y}</Y>");
                writer.WriteLine($"      <Z>{point.Z}</Z>");
                writer.WriteLine($"      <Hardness>{point.HardnessValue}</Hardness>");
                if (point.Load.HasValue)
                    writer.WriteLine($"      <Load>{point.Load.Value}</Load>");
                if (point.IndentationDiagonal.HasValue)
                    writer.WriteLine($"      <Diagonal>{point.IndentationDiagonal.Value}</Diagonal>");
                writer.WriteLine($"      <Time>{point.TestTime:yyyy-MM-dd HH:mm:ss}</Time>");
                writer.WriteLine("    </TestPoint>");
            }

            writer.WriteLine("  </TestPoints>");
            writer.WriteLine("</HardnessTestData>");
        }
    }
}
