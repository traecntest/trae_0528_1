using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HardnessMappingTool.Models;
using HardnessMappingTool.Services.Distribution;

namespace HardnessMappingTool.Services.Reporting
{
    public class ReportGenerator
    {
        private readonly HardnessDistributionCalculator _calculator = new();

        public string GenerateTextReport(
            List<HardnessTestData> data,
            string sampleId,
            MaterialSample? sampleInfo = null)
        {
            var stats = _calculator.CalculateStatistics(data);
            var builder = new StringBuilder();

            builder.AppendLine("=".PadRight(60, '='));
            builder.AppendLine("材料硬度分布分析报告");
            builder.AppendLine("=".PadRight(60, '='));
            builder.AppendLine();

            builder.AppendLine($"报告生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"样品编号: {sampleId}");
            builder.AppendLine();

            if (sampleInfo != null)
            {
                builder.AppendLine("样品信息:");
                builder.AppendLine($"  材料名称: {sampleInfo.MaterialName}");
                builder.AppendLine($"  材料类型: {sampleInfo.MaterialType}");
                builder.AppendLine($"  批次号: {sampleInfo.BatchNumber}");
                builder.AppendLine($"  厚度: {sampleInfo.Thickness} mm");
                builder.AppendLine($"  接收日期: {sampleInfo.ReceiveDate:yyyy-MM-dd}");
                if (!string.IsNullOrEmpty(sampleInfo.Description))
                {
                    builder.AppendLine($"  描述: {sampleInfo.Description}");
                }
                builder.AppendLine();
            }

            builder.AppendLine("统计指标:");
            builder.AppendLine("".PadRight(40, '-'));
            builder.AppendLine($"  测试点总数: {stats.Count}");
            builder.AppendLine($"  有效测试点数: {stats.ValidCount}");
            builder.AppendLine($"  平均值: {stats.Mean:F4}");
            builder.AppendLine($"  标准差: {stats.StandardDeviation:F4}");
            builder.AppendLine($"  方差: {stats.Variance:F4}");
            builder.AppendLine($"  最小值: {stats.Minimum:F4}");
            builder.AppendLine($"  最大值: {stats.Maximum:F4}");
            builder.AppendLine($"  极差: {stats.Range:F4}");
            builder.AppendLine($"  中位数: {stats.Median:F4}");
            builder.AppendLine();

            var hardnessType = data.FirstOrDefault()?.HardnessType ?? HardnessType.Vickers;
            builder.AppendLine($"硬度类型: {hardnessType}");
            builder.AppendLine();

            var invalidData = data.Where(d => !d.IsValid).ToList();
            if (invalidData.Any())
            {
                builder.AppendLine($"异常数据点 ({invalidData.Count} 个):");
                foreach (var point in invalidData.Take(10))
                {
                    builder.AppendLine($"  位置: ({point.X:F2}, {point.Y:F2}) - 硬度值: {point.HardnessValue:F2}");
                }
                if (invalidData.Count > 10)
                {
                    builder.AppendLine($"  ... 还有 {invalidData.Count - 10} 个异常点");
                }
                builder.AppendLine();
            }

            builder.AppendLine("测试点坐标范围:");
            builder.AppendLine($"  X: {data.Min(d => d.X):F2} ~ {data.Max(d => d.X):F2}");
            builder.AppendLine($"  Y: {data.Min(d => d.Y):F2} ~ {data.Max(d => d.Y):F2}");
            if (data.Any(d => d.Z != 0))
            {
                builder.AppendLine($"  Z: {data.Min(d => d.Z):F2} ~ {data.Max(d => d.Z):F2}");
            }
            builder.AppendLine();

            builder.AppendLine("=".PadRight(60, '='));
            builder.AppendLine("报告结束");
            builder.AppendLine("=".PadRight(60, '='));

            return builder.ToString();
        }

        public void SaveTextReport(string filePath, List<HardnessTestData> data, string sampleId, MaterialSample? sampleInfo = null)
        {
            var report = GenerateTextReport(data, sampleId, sampleInfo);
            File.WriteAllText(filePath, report, Encoding.UTF8);
        }

        public string GenerateCsvReport(List<HardnessTestData> data)
        {
            var builder = new StringBuilder();
            builder.AppendLine("SampleId,HardnessType,X,Y,Z,HardnessValue,Load,IndentationDiagonal,TestTime,Operator,Remarks,IsValid");

            foreach (var point in data)
            {
                builder.AppendLine($"{point.SampleId},{point.HardnessType},{point.X:F4},{point.Y:F4},{point.Z:F4}," +
                                   $"{point.HardnessValue:F4},{point.Load ?? 0:F4},{point.IndentationDiagonal ?? 0:F4}," +
                                   $"{point.TestTime:yyyy-MM-dd HH:mm:ss},{point.OperatorName},{point.Remarks},{point.IsValid}");
            }

            return builder.ToString();
        }

        public void SaveCsvReport(string filePath, List<HardnessTestData> data)
        {
            var report = GenerateCsvReport(data);
            File.WriteAllText(filePath, report, Encoding.UTF8);
        }

        public string GenerateHtmlReport(
            List<HardnessTestData> data,
            string sampleId,
            MaterialSample? sampleInfo = null)
        {
            var stats = _calculator.CalculateStatistics(data);
            var hardnessType = data.FirstOrDefault()?.HardnessType ?? HardnessType.Vickers;

            var html = $@"
<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <title>材料硬度分布分析报告 - {sampleId}</title>
    <style>
        body {{ font-family: 'Microsoft YaHei', Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1000px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }}
        h2 {{ color: #34495e; margin-top: 30px; }}
        .info-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 15px; margin: 20px 0; }}
        .info-item {{ background: #ecf0f1; padding: 15px; border-radius: 5px; }}
        .info-label {{ font-weight: bold; color: #7f8c8d; display: block; margin-bottom: 5px; }}
        .info-value {{ font-size: 1.1em; color: #2c3e50; }}
        .stats-table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        .stats-table th, .stats-table td {{ border: 1px solid #bdc3c7; padding: 12px; text-align: left; }}
        .stats-table th {{ background: #3498db; color: white; }}
        .stats-table tr:nth-child(even) {{ background: #f8f9fa; }}
        .summary {{ background: #e8f4f8; padding: 20px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; text-align: center; color: #7f8c8d; font-size: 0.9em; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>材料硬度分布分析报告</h1>
        
        <div class='summary'>
            <strong>报告生成时间:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br>
            <strong>样品编号:</strong> {sampleId}<br>
            <strong>硬度类型:</strong> {hardnessType}
        </div>

        <h2>统计指标</h2>
        <table class='stats-table'>
            <tr>
                <th>指标</th>
                <th>数值</th>
                <th>指标</th>
                <th>数值</th>
            </tr>
            <tr>
                <td>测试点总数</td>
                <td>{stats.Count}</td>
                <td>有效测试点数</td>
                <td>{stats.ValidCount}</td>
            </tr>
            <tr>
                <td>平均值</td>
                <td>{stats.Mean:F4}</td>
                <td>标准差</td>
                <td>{stats.StandardDeviation:F4}</td>
            </tr>
            <tr>
                <td>方差</td>
                <td>{stats.Variance:F4}</td>
                <td>极差</td>
                <td>{stats.Range:F4}</td>
            </tr>
            <tr>
                <td>最小值</td>
                <td>{stats.Minimum:F4}</td>
                <td>最大值</td>
                <td>{stats.Maximum:F4}</td>
            </tr>
            <tr>
                <td>中位数</td>
                <td>{stats.Median:F4}</td>
                <td>变异系数</td>
                <td>{(stats.Mean > 0 ? stats.StandardDeviation / stats.Mean * 100 : 0):F2}%</td>
            </tr>
        </table>

        <h2>坐标范围</h2>
        <div class='info-grid'>
            <div class='info-item'>
                <span class='info-label'>X 坐标范围</span>
                <span class='info-value'>{data.Min(d => d.X):F2} ~ {data.Max(d => d.X):F2}</span>
            </div>
            <div class='info-item'>
                <span class='info-label'>Y 坐标范围</span>
                <span class='info-value'>{data.Min(d => d.Y):F2} ~ {data.Max(d => d.Y):F2}</span>
            </div>
            <div class='info-item'>
                <span class='info-label'>硬度值范围</span>
                <span class='info-value'>{stats.Minimum:F2} ~ {stats.Maximum:F2}</span>
            </div>
            <div class='info-item'>
                <span class='info-label'>异常点数</span>
                <span class='info-value'>{data.Count(d => !d.IsValid)}</span>
            </div>
        </div>

        <div class='footer'>
            本报告由材料硬度分布映射工具自动生成
        </div>
    </div>
</body>
</html>";

            return html;
        }

        public void SaveHtmlReport(string filePath, List<HardnessTestData> data, string sampleId, MaterialSample? sampleInfo = null)
        {
            var report = GenerateHtmlReport(data, sampleId, sampleInfo);
            File.WriteAllText(filePath, report, Encoding.UTF8);
        }
    }
}
