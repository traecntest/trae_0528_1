using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using OxyPlot;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfVerticalAlignment = System.Windows.VerticalAlignment;
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
        private DistributionMatrix? _distributionMatrix;
        private DataTable? _distributionMatrixTable;
        private double[,,]? _distributionMatrix3D;

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

        public DistributionMatrix? DistributionMatrix
        {
            get => _distributionMatrix;
            set { _distributionMatrix = value; OnPropertyChanged(); }
        }

        public DataTable? DistributionMatrixTable
        {
            get => _distributionMatrixTable;
            set { _distributionMatrixTable = value; OnPropertyChanged(); }
        }

        public double[,,]? DistributionMatrix3D
        {
            get => _distributionMatrix3D;
            set { _distributionMatrix3D = value; OnPropertyChanged(); }
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
                var existingIds = await _dbContext.GetAllSampleIdsAsync();
                if (existingIds.Contains(SelectedSampleId ?? string.Empty))
                {
                    var result = MessageBox.Show(
                        $"样品 {SelectedSampleId} 已存在数据，是否覆盖现有数据？",
                        "确认覆盖",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await _dbContext.UpdateTestDataAsync(TestData.ToList());
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    await _dbContext.BulkInsertTestDataAsync(TestData);
                }
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

        public async Task SaveProcessedData()
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有数据可保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(SelectedSampleId))
            {
                MessageBox.Show("请先选择或设置样品编号", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusMessage = "正在保存处理后的数据...";
                await _dbContext.UpdateTestDataAsync(TestData.ToList());
                await LoadSampleIds();
                StatusMessage = "处理后的数据保存成功";
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
                $"确定要从数据库中删除样品 {SelectedSampleId} 的所有数据吗？此操作不可撤销！",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage = "正在删除数据...";
                    await _dbContext.DeleteSampleDataAsync(SelectedSampleId);
                    TestData.Clear();
                    DistributionMatrix = null;
                    DistributionMatrixTable = null;
                    DistributionMatrix3D = null;
                    CurrentPlot = null;
                    Statistics = null;
                    await LoadSampleIds();
                    SelectedSampleId = null;
                    StatusMessage = $"样品 {SelectedSampleId} 数据已删除";
                    MessageBox.Show("数据删除成功！", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"删除失败: {ex.Message}";
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void Calculate2DDistributionMatrix(int gridSizeX = 50, int gridSizeY = 50, InterpolationMethod method = InterpolationMethod.InverseDistanceWeighting)
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有可计算的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusMessage = "正在计算二维硬度分布矩阵...";
                DistributionMatrix = _distributionCalculator.Calculate2DDistribution(
                    TestData.ToList(), gridSizeX, gridSizeY, method);
                
                DistributionMatrixTable = ConvertMatrixToDataTable(DistributionMatrix);
                StatusMessage = $"二维分布矩阵计算完成 ({gridSizeX}x{gridSizeY})";
            }
            catch (Exception ex)
            {
                StatusMessage = $"计算失败: {ex.Message}";
                MessageBox.Show($"计算失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Calculate3DDistributionMatrix(int gridSizeX = 20, int gridSizeY = 20, int gridSizeZ = 10, InterpolationMethod method = InterpolationMethod.InverseDistanceWeighting)
        {
            if (!TestData.Any())
            {
                MessageBox.Show("没有可计算的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusMessage = "正在计算三维硬度分布矩阵...";
                DistributionMatrix3D = _distributionCalculator.Calculate3DDistribution(
                    TestData.ToList(), gridSizeX, gridSizeY, gridSizeZ, method);
                StatusMessage = $"三维分布矩阵计算完成 ({gridSizeX}x{gridSizeY}x{gridSizeZ})";
            }
            catch (Exception ex)
            {
                StatusMessage = $"计算失败: {ex.Message}";
                MessageBox.Show($"计算失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable? ConvertMatrixToDataTable(DistributionMatrix matrix)
        {
            if (matrix == null || matrix.Values == null) return null;

            var dataTable = new DataTable();
            
            dataTable.Columns.Add("Y\\X", typeof(string));
            for (int j = 0; j < matrix.Columns; j++)
            {
                var xValue = matrix.MinX + j * matrix.StepX;
                dataTable.Columns.Add(xValue.ToString("F1"), typeof(double));
            }

            for (int i = 0; i < matrix.Rows; i++)
            {
                var row = dataTable.NewRow();
                var yValue = matrix.MinY + i * matrix.StepY;
                row[0] = yValue.ToString("F1");
                for (int j = 0; j < matrix.Columns; j++)
                {
                    row[j + 1] = Math.Round(matrix.Values[i, j], 2);
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public void ShowDistributionMatrixView()
        {
            if (DistributionMatrixTable == null)
            {
                Calculate2DDistributionMatrix();
            }

            if (DistributionMatrixTable == null) return;

            var window = new Window
            {
                Title = $"{SelectedSampleId} - 硬度分布矩阵数据",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var dataGrid = new System.Windows.Controls.DataGrid
            {
                ItemsSource = DistributionMatrixTable.DefaultView,
                IsReadOnly = true,
                AutoGenerateColumns = true,
                CanUserAddRows = false,
                CanUserSortColumns = false,
                FontSize = 11,
                Margin = new Thickness(5)
            };

            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                HorizontalAlignment = WpfHorizontalAlignment.Right
            };

            var closeButton = new System.Windows.Controls.Button
            {
                Content = "关闭",
                Width = 75,
                Margin = new Thickness(5)
            };
            closeButton.Click += (s, e) => window.Close();

            infoPanel.Children.Add(closeButton);

            Grid.SetRow(dataGrid, 0);
            Grid.SetRow(infoPanel, 1);
            grid.Children.Add(dataGrid);
            grid.Children.Add(infoPanel);

            window.Content = grid;
            window.ShowDialog();
        }

        public void Show3DDistributionMatrixView()
        {
            if (DistributionMatrix3D == null)
            {
                Calculate3DDistributionMatrix();
            }

            if (DistributionMatrix3D == null) return;

            var window = new Window
            {
                Title = $"{SelectedSampleId} - 三维硬度分布矩阵",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
            };

            var grid = new Grid();
            var textBlock = new System.Windows.Controls.TextBlock
            {
                Text = $"三维矩阵已计算完成！\n\n尺寸: {DistributionMatrix3D.GetLength(2)} x {DistributionMatrix3D.GetLength(1)} x {DistributionMatrix3D.GetLength(0)}\n\n可通过热力图或等高线图查看各切面的硬度分布。",
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                VerticalAlignment = WpfVerticalAlignment.Center,
                HorizontalAlignment = WpfHorizontalAlignment.Center
            };

            grid.Children.Add(textBlock);
            window.Content = grid;
            window.ShowDialog();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
