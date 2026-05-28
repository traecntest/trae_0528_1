using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using HardnessMappingTool.Models;
using HardnessMappingTool.Services.Distribution;

namespace HardnessMappingTool.Services.Visualization
{
    public class VisualizationService
    {
        public PlotModel CreateHeatMapPlot(DistributionMatrix matrix, string title = "硬度分布热力图")
        {
            var plotModel = new PlotModel { Title = title };

            var minValue = matrix.Values.Cast<double>().Min();
            var maxValue = matrix.Values.Cast<double>().Max();

            var colorAxis = new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Jet(256),
                Minimum = minValue,
                Maximum = maxValue
            };
            plotModel.Axes.Add(colorAxis);

            var heatMapSeries = new HeatMapSeries
            {
                X0 = matrix.MinX,
                X1 = matrix.MaxX,
                Y0 = matrix.MinY,
                Y1 = matrix.MaxY,
                Data = matrix.Values
            };
            plotModel.Series.Add(heatMapSeries);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "X 坐标",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Y 坐标",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.Legends.Add(new Legend
            {
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColors.White,
                LegendBorder = OxyColors.Black
            });

            return plotModel;
        }

        public PlotModel CreateContourPlot(DistributionMatrix matrix, int contourLevelCount = 10, string title = "硬度分布等高线图")
        {
            var plotModel = new PlotModel { Title = title };

            var minValue = matrix.Values.Cast<double>().Min();
            var maxValue = matrix.Values.Cast<double>().Max();

            var colorAxis = new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(256),
                Minimum = minValue,
                Maximum = maxValue
            };
            plotModel.Axes.Add(colorAxis);

            var columnCoordinates = GenerateCoordinates(matrix.MinX, matrix.MaxX, matrix.Columns);
            var rowCoordinates = GenerateCoordinates(matrix.MinY, matrix.MaxY, matrix.Rows);
            var contourLevels = GenerateContourLevels(matrix.Values, contourLevelCount);

            var heatMapSeries = new HeatMapSeries
            {
                X0 = matrix.MinX,
                X1 = matrix.MaxX,
                Y0 = matrix.MinY,
                Y1 = matrix.MaxY,
                Data = matrix.Values
            };
            plotModel.Series.Add(heatMapSeries);

            var contourSeries = new ContourSeries
            {
                ColumnCoordinates = columnCoordinates,
                RowCoordinates = rowCoordinates,
                Data = matrix.Values,
                ContourLevels = contourLevels,
                LineStyle = LineStyle.Solid,
                StrokeThickness = 1.5,
                Color = OxyColors.Black
            };
            plotModel.Series.Add(contourSeries);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "X 坐标",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Y 坐标",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            return plotModel;
        }

        public PlotModel CreateScatterPlot(List<HardnessTestData> data, string title = "测试点散点图")
        {
            var plotModel = new PlotModel { Title = title };

            var validData = data.Where(d => d.IsValid).ToList();

            var scatterSeries = new ScatterSeries
            {
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerStroke = OxyColors.Black,
                MarkerStrokeThickness = 1,
                Title = "测试点"
            };

            var minHardness = validData.Min(d => d.HardnessValue);
            var maxHardness = validData.Max(d => d.HardnessValue);
            var palette = OxyPalettes.Jet(256);

            foreach (var point in validData)
            {
                var normalizedValue = (point.HardnessValue - minHardness) / (maxHardness - minHardness);
                var colorIndex = (int)(normalizedValue * 255);
                colorIndex = Math.Clamp(colorIndex, 0, 255);

                scatterSeries.Points.Add(new ScatterPoint(
                    point.X,
                    point.Y,
                    6,
                    point.HardnessValue));
            }

            plotModel.Series.Add(scatterSeries);

            var colorAxis = new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = palette,
                Minimum = minHardness,
                Maximum = maxHardness,
                Title = "硬度值"
            };
            plotModel.Axes.Add(colorAxis);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "X 坐标",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Y 坐标",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            });

            return plotModel;
        }

        public PlotModel CreateSurfacePlot(DistributionMatrix matrix, string title = "3D 硬度分布图")
        {
            var plotModel = new PlotModel { Title = title };

            var columnCoordinates = GenerateCoordinates(matrix.MinX, matrix.MaxX, matrix.Columns);
            var rowCoordinates = GenerateCoordinates(matrix.MinY, matrix.MaxY, matrix.Rows);

            var contourSeries = new ContourSeries
            {
                ColumnCoordinates = columnCoordinates,
                RowCoordinates = rowCoordinates,
                Data = matrix.Values,
                ContourLevels = GenerateContourLevels(matrix.Values, 20),
                Color = OxyColors.SteelBlue
            };
            plotModel.Series.Add(contourSeries);

            var colorAxis = new LinearColorAxis
            {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Jet(256),
                Minimum = matrix.Values.Cast<double>().Min(),
                Maximum = matrix.Values.Cast<double>().Max()
            };
            plotModel.Axes.Add(colorAxis);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "X 坐标"
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Y 坐标"
            });

            return plotModel;
        }

        public PlotModel CreateStatisticsHistogram(List<HardnessTestData> data, int binCount = 20, string title = "硬度值分布直方图")
        {
            var plotModel = new PlotModel { Title = title };

            var validData = data.Where(d => d.IsValid).Select(d => d.HardnessValue).ToList();
            if (validData.Count == 0) return plotModel;

            var min = validData.Min();
            var max = validData.Max();
            var binWidth = (max - min) / binCount;

            var histogramSeries = new HistogramSeries
            {
                FillColor = OxyColors.SteelBlue,
                StrokeColor = OxyColors.Black,
                StrokeThickness = 1,
                Title = "频率分布"
            };

            for (int i = 0; i < binCount; i++)
            {
                var start = min + i * binWidth;
                var end = start + binWidth;
                var count = validData.Count(v => v >= start && v < end);
                histogramSeries.Items.Add(new HistogramItem(start, end, count, 1));
            }

            plotModel.Series.Add(histogramSeries);

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "硬度值",
                MajorGridlineStyle = LineStyle.Solid
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "频数",
                MajorGridlineStyle = LineStyle.Solid
            });

            return plotModel;
        }

        private double[] GenerateCoordinates(double min, double max, int count)
        {
            var coords = new double[count];
            var step = (max - min) / (count - 1);
            for (int i = 0; i < count; i++)
            {
                coords[i] = min + i * step;
            }
            return coords;
        }

        private double[] GenerateContourLevels(double[,] data, int levelCount)
        {
            var min = data.Cast<double>().Min();
            var max = data.Cast<double>().Max();
            var step = (max - min) / levelCount;
            var levels = new double[levelCount];
            for (int i = 0; i < levelCount; i++)
            {
                levels[i] = min + (i + 1) * step;
            }
            return levels;
        }

        public PlotModel CreateBoxPlot(List<List<HardnessTestData>> dataGroups, List<string> groupNames, string title = "硬度分布箱线图")
        {
            var plotModel = new PlotModel { Title = title };

            var boxPlotSeries = new BoxPlotSeries
            {
                Title = "硬度分布",
                Fill = OxyColors.LightBlue,
                Stroke = OxyColors.Black,
                StrokeThickness = 1
            };

            for (int i = 0; i < dataGroups.Count; i++)
            {
                var values = dataGroups[i].Where(d => d.IsValid).Select(d => d.HardnessValue).OrderBy(v => v).ToList();
                if (values.Count == 0) continue;

                var q1 = values[values.Count / 4];
                var q3 = values[values.Count * 3 / 4];
                var median = values.Count % 2 == 0 ? (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2 : values[values.Count / 2];
                var iqr = q3 - q1;
                var lowerWhisker = values.First(v => v >= q1 - 1.5 * iqr);
                var upperWhisker = values.Last(v => v <= q3 + 1.5 * iqr);

                boxPlotSeries.Items.Add(new BoxPlotItem(
                    i,
                    lowerWhisker,
                    q1,
                    median,
                    q3,
                    upperWhisker));
            }

            plotModel.Series.Add(boxPlotSeries);

            plotModel.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                ItemsSource = groupNames
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "硬度值",
                MajorGridlineStyle = LineStyle.Solid
            });

            return plotModel;
        }
    }
}
