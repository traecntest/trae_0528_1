using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using OxyPlot;
using HardnessMappingTool.Data;
using HardnessMappingTool.Models;
using HardnessMappingTool.Services.DataImport;
using HardnessMappingTool.Services.DataProcessing;
using HardnessMappingTool.Services.Distribution;
using HardnessMappingTool.Services.Reporting;
using HardnessMappingTool.Services.TestData;
using HardnessMappingTool.Services.Visualization;

namespace HardnessMappingTool.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseContext _dbContext;
        private readonly CsvImporter _csvImporter;
        private readonly ExcelImporter _excelImporter;
        private readonly XmlImporter _xmlImporter;
        private readonly DataValidationService _validationService;
        private readonly NoiseReductionService _noiseReductionService;
        private readonly OutlierRemovalService _outlierRemovalService;
        private readonly HardnessDistributionCalculator _distributionCalculator;
        private readonly VisualizationService _visualizationService;
        private readonly ReportGenerator _reportGenerator;
        private readonly TestDataGenerator _testDataGenerator;

        private ObservableCollection<HardnessTestData> _testData = new();
        private ObservableCollection<string> _sampleIds = new();
        private string? _selectedSampleId;
        private PlotModel? _currentPlot;
        private string _statusMessage = "就绪";
        private StatisticsResult? _statistics;

        public MainViewModel()
        {
            _dbContext = new DatabaseContext();
            _csvImporter = new CsvImporter();
            _excelImporter = new ExcelImporter();
            _xmlImporter = new XmlImporter();
            _validationService = new DataValidationService();
            _noiseReductionService = new NoiseReductionService();
            _outlierRemovalService = new OutlierRemovalService();
            _distributionCalculator = new HardnessDistributionCalculator();
            _visualizationService = new VisualizationService();
            _reportGenerator = new ReportGenerator();
            _testDataGenerator = new TestDataGenerator();

            InitializeDatabase();
        }

        public ObservableCollection<HardnessTestData> TestData
        {
            get => _testData;
            set { _testData = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> SampleIds
        {
            get => _sampleIds;
            set { _sampleIds = value; OnPropertyChanged(); }
        }

        public string? SelectedSampleId
        {
            get => _selectedSampleId;
            set
            {
                _selectedSampleId = value;
                OnPropertyChanged();
                _ = LoadSampleData();
            }
        }

        public PlotModel? CurrentPlot
        {
            get => _currentPlot;
            set { _currentPlot = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public StatisticsResult? Statistics
        {
            get => _statistics;
            set { _statistics = value; OnPropertyChanged(); }
        }

        private async void InitializeDatabase()
        {
            try
            {
                await _dbContext.InitializeDatabaseAsync();
                await LoadSampleIds();
                StatusMessage = "数据库初始化完成";
            }
            catch (Exception ex)
            {
                StatusMessage = $"数据库初始化失败: {ex.Message}";
            }
        }

        private async Task LoadSampleIds()
        {
            var ids = await _dbContext.GetAllSampleIdsAsync();
            SampleIds = new ObservableCollection<string>(ids);
        }

        private async Task LoadSampleData()
        {
            if (string.IsNullOrEmpty(SelectedSampleId))
                return;

            var data = await _dbContext.GetAllTestDataAsync(SelectedSampleId);
            TestData = new ObservableCollection<HardnessTestData>(data);
            UpdateStatistics();
            StatusMessage = $"已加载 {data.Count} 条数据";
        }

        private void UpdateStatistics()
        {
            Statistics = _distributionCalculator.CalculateStatistics(TestData.ToList());
        }

        public async Task ImportCsvFile(string filePath, string sampleId, HardnessType hardnessType)
        {
            try
            {
                StatusMessage = "正在导入 CSV 文件...";
                var data = await _csvImporter.ImportAsync(filePath, sampleId, hardnessType);
                var validation = _validationService.ValidateData(data);
                await _dbContext.BulkInsertTestDataAsync(validation.ValidRecords);
                await LoadSampleIds();
                StatusMessage = $"导入完成: {validation.ValidCount} 条有效数据, {validation.InvalidCount} 条无效数据";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ImportExcelFile(string filePath, string sampleId, HardnessType hardnessType)
        {
            try
            {
                StatusMessage = "正在导入 Excel 文件...";
                var data = _excelImporter.Import(filePath, sampleId, hardnessType);
                var validation = _validationService.ValidateData(data);
                await _dbContext.BulkInsertTestDataAsync(validation.ValidRecords);
                await LoadSampleIds();
                StatusMessage = $"导入完成: {validation.ValidCount} 条有效数据, {validation.InvalidCount} 条无效数据";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task ImportXmlFile(string filePath, string sampleId, HardnessType hardnessType)
        {
            try
            {
                StatusMessage = "正在导入 XML 文件...";
                var data = await _xmlImporter.ImportAsync(filePath, sampleId, hardnessType);
                var validation = _validationService.ValidateData(data);
                await _dbContext.BulkInsertTestDataAsync(validation.ValidRecords);
                await LoadSampleIds();
                StatusMessage = $"导入完成: {validation.ValidCount} 条有效数据, {validation.InvalidCount} 条无效数据";
            }
            catch (Exception ex)
            {
                StatusMessage = $"导入失败: {ex.Message}";
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ShowHeatMap()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有可显示的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "正在生成热力图...";
            var matrix = _distributionCalculator.Calculate2DDistribution(
                TestData.ToList(), 100, 100, InterpolationMethod.InverseDistanceWeighting);
            CurrentPlot = _visualizationService.CreateHeatMapPlot(matrix, $"{SelectedSampleId} - 硬度分布热力图");
            StatusMessage = "热力图生成完成";
        }

        public void ShowContourMap()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有可显示的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "正在生成等高线图...";
            var matrix = _distributionCalculator.Calculate2DDistribution(
                TestData.ToList(), 100, 100, InterpolationMethod.InverseDistanceWeighting);
            CurrentPlot = _visualizationService.CreateContourPlot(matrix, 15, $"{SelectedSampleId} - 硬度分布等高线图");
            StatusMessage = "等高线图生成完成";
        }

        public void ShowScatterPlot()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有可显示的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "正在生成散点图...";
            CurrentPlot = _visualizationService.CreateScatterPlot(TestData.ToList(), $"{SelectedSampleId} - 测试点散点图");
            StatusMessage = "散点图生成完成";
        }

        public void ShowHistogram()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有可显示的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StatusMessage = "正在生成直方图...";
            CurrentPlot = _visualizationService.CreateStatisticsHistogram(TestData.ToList(), 20, $"{SelectedSampleId} - 硬度分布直方图");
            StatusMessage = "直方图生成完成";
        }

        public void RemoveOutliers()
        {
            if (!TestData.Any()) return;

            StatusMessage = "正在剔除异常值...";
            var cleaned = _outlierRemovalService.RemoveOutliersByIQR(TestData.ToList());
            TestData = new ObservableCollection<HardnessTestData>(cleaned);
            UpdateStatistics();
            StatusMessage = $"异常值剔除完成，剩余 {cleaned.Count} 条数据";
        }

        public void ApplyNoiseReduction()
        {
            if (!TestData.Any()) return;

            StatusMessage = "正在进行去噪处理...";
            var filtered = _noiseReductionService.ApplyMovingAverageFilter(TestData.ToList(), 5);
            TestData = new ObservableCollection<HardnessTestData>(filtered);
            UpdateStatistics();
            StatusMessage = "去噪处理完成";
        }

        public void GenerateTextReport()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有数据可生成报告", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "文本文件 (*.txt)|*.txt",
                FileName = $"{SelectedSampleId}_Report.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                _reportGenerator.SaveTextReport(dialog.FileName, TestData.ToList(), SelectedSampleId ?? "Unknown");
                StatusMessage = $"报告已保存至 {dialog.FileName}";
                MessageBox.Show("报告生成成功！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void GenerateHtmlReport()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有数据可生成报告", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "HTML 文件 (*.html)|*.html",
                FileName = $"{SelectedSampleId}_Report.html"
            };

            if (dialog.ShowDialog() == true)
            {
                _reportGenerator.SaveHtmlReport(dialog.FileName, TestData.ToList(), SelectedSampleId ?? "Unknown");
                StatusMessage = $"报告已保存至 {dialog.FileName}";
                MessageBox.Show("报告生成成功！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void GenerateTestData(string sampleId, HardnessType hardnessType, int pointCount)
        {
            try
            {
                StatusMessage = "正在生成测试数据...";
                var data = _testDataGenerator.GenerateTestData(sampleId, hardnessType, pointCount);
                TestData = new ObservableCollection<HardnessTestData>(data);
                UpdateStatistics();
                StatusMessage = $"生成了 {pointCount} 条测试数据";
            }
            catch (Exception ex)
            {
                StatusMessage = $"生成失败: {ex.Message}";
                MessageBox.Show($"生成失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task SaveTestData()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有数据可保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusMessage = "正在保存数据...";
                await _dbContext.BulkInsertTestDataAsync(TestData);
                await LoadSampleIds();
                StatusMessage = "数据保存成功";
                MessageBox.Show("数据保存成功！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败: {ex.Message}";
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportTestData()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv|XML 文件 (*.xml)|*.xml",
                FileName = $"{SelectedSampleId}_Export.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                if (Path.GetExtension(dialog.FileName).Equals(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    _testDataGenerator.SaveToXml(TestData.ToList(), dialog.FileName);
                }
                else
                {
                    _testDataGenerator.SaveToCsv(TestData.ToList(), dialog.FileName);
                }
                StatusMessage = $"数据已导出至 {dialog.FileName}";
                MessageBox.Show("导出成功！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public async Task ClearData()
        {
            if (string.IsNullOrEmpty(SelectedSampleId))
            {
                MessageBox.Show("请先选择一个样品", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除样品 {SelectedSampleId} 的所有数据吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                TestData.Clear();
                await LoadSampleIds();
                CurrentPlot = null;
                Statistics = null;
                StatusMessage = "数据已清除";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
