using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using HardnessMappingTool.Models;
using HardnessMappingTool.ViewModels;

namespace HardnessMappingTool;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ImportCsv_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            Title = "选择要导入的 CSV 文件"
        };

        if (dialog.ShowDialog() == true)
        {
            var inputDialog = new ImportSettingsDialog();
            if (inputDialog.ShowDialog() == true)
            {
                _ = ViewModel.ImportCsvFile(
                    dialog.FileName,
                    inputDialog.SampleId,
                    inputDialog.HardnessType);
            }
        }
    }

    private void ImportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel 文件 (*.xlsx;*.xls)|*.xlsx;*.xls|所有文件 (*.*)|*.*",
            Title = "选择要导入的 Excel 文件"
        };

        if (dialog.ShowDialog() == true)
        {
            var inputDialog = new ImportSettingsDialog();
            if (inputDialog.ShowDialog() == true)
            {
                _ = ViewModel.ImportExcelFile(
                    dialog.FileName,
                    inputDialog.SampleId,
                    inputDialog.HardnessType);
            }
        }
    }

    private void ImportXml_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "XML 文件 (*.xml)|*.xml|所有文件 (*.*)|*.*",
            Title = "选择要导入的 XML 文件"
        };

        if (dialog.ShowDialog() == true)
        {
            var inputDialog = new ImportSettingsDialog();
            if (inputDialog.ShowDialog() == true)
            {
                _ = ViewModel.ImportXmlFile(
                    dialog.FileName,
                    inputDialog.SampleId,
                    inputDialog.HardnessType);
            }
        }
    }

    private void ExportData_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExportTestData();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RemoveOutliers_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RemoveOutliers();
    }

    private void NoiseReduction_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ApplyNoiseReduction();
    }

    private void ShowHeatMap_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowHeatMap();
    }

    private void ShowContourMap_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowContourMap();
    }

    private void ShowScatterPlot_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowScatterPlot();
    }

    private void ShowHistogram_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ShowHistogram();
    }

    private void GenerateTextReport_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateTextReport();
    }

    private void GenerateHtmlReport_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GenerateHtmlReport();
    }

    private void GenerateTestData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new TestDataSettingsDialog();
        if (dialog.ShowDialog() == true)
        {
            ViewModel.GenerateTestData(
                dialog.SampleId,
                dialog.HardnessType,
                dialog.PointCount);
        }
    }

    private async void SaveTestData_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveTestData();
    }

    private async void ClearData_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ClearData();
    }
}

public class ImportSettingsDialog : Window
{
    public string SampleId { get; private set; } = "Sample_001";
    public HardnessType HardnessType { get; private set; } = HardnessType.Vickers;

    public ImportSettingsDialog()
    {
        Title = "导入设置";
        Width = 300;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Owner = Application.Current.MainWindow;

        var stackPanel = new StackPanel { Margin = new Thickness(10) };

        var sampleIdLabel = new TextBlock { Text = "样品编号:" };
        var sampleIdTextBox = new TextBox { Name = "SampleIdTextBox", Text = SampleId, Margin = new Thickness(0, 0, 0, 10) };

        var typeLabel = new TextBlock { Text = "硬度类型:" };
        var typeComboBox = new ComboBox { Name = "TypeComboBox", Margin = new Thickness(0, 0, 0, 10) };
        typeComboBox.ItemsSource = Enum.GetValues<HardnessType>();
        typeComboBox.SelectedItem = HardnessType;

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new Button { Content = "确定", Width = 75, Margin = new Thickness(5) };
        var cancelButton = new Button { Content = "取消", Width = 75, Margin = new Thickness(5) };

        okButton.Click += (s, e) =>
        {
            SampleId = sampleIdTextBox.Text;
            HardnessType = (HardnessType)typeComboBox.SelectedItem;
            DialogResult = true;
            Close();
        };

        cancelButton.Click += (s, e) =>
        {
            DialogResult = false;
            Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(sampleIdLabel);
        stackPanel.Children.Add(sampleIdTextBox);
        stackPanel.Children.Add(typeLabel);
        stackPanel.Children.Add(typeComboBox);
        stackPanel.Children.Add(buttonPanel);

        Content = stackPanel;
    }
}

public class TestDataSettingsDialog : Window
{
    public string SampleId { get; private set; } = "Test_Sample_001";
    public HardnessType HardnessType { get; private set; } = HardnessType.Vickers;
    public int PointCount { get; private set; } = 100;

    public TestDataSettingsDialog()
    {
        Title = "测试数据设置";
        Width = 300;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Owner = Application.Current.MainWindow;

        var stackPanel = new StackPanel { Margin = new Thickness(10) };

        var sampleIdLabel = new TextBlock { Text = "样品编号:" };
        var sampleIdTextBox = new TextBox { Text = SampleId, Margin = new Thickness(0, 0, 0, 10) };

        var typeLabel = new TextBlock { Text = "硬度类型:" };
        var typeComboBox = new ComboBox { Margin = new Thickness(0, 0, 0, 10) };
        typeComboBox.ItemsSource = Enum.GetValues<HardnessType>();
        typeComboBox.SelectedItem = HardnessType;

        var countLabel = new TextBlock { Text = "数据点数量:" };
        var countSlider = new Slider 
        { 
            Minimum = 10, 
            Maximum = 500, 
            Value = PointCount, 
            IsSnapToTickEnabled = true,
            TickFrequency = 10,
            Margin = new Thickness(0, 0, 0, 5)
        };
        var countValueLabel = new TextBlock { Text = PointCount.ToString(), Margin = new Thickness(0, 0, 0, 10) };
        countSlider.ValueChanged += (s, e) => countValueLabel.Text = e.NewValue.ToString("F0");

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var okButton = new Button { Content = "确定", Width = 75, Margin = new Thickness(5) };
        var cancelButton = new Button { Content = "取消", Width = 75, Margin = new Thickness(5) };

        okButton.Click += (s, e) =>
        {
            SampleId = sampleIdTextBox.Text;
            HardnessType = (HardnessType)typeComboBox.SelectedItem;
            PointCount = (int)countSlider.Value;
            DialogResult = true;
            Close();
        };

        cancelButton.Click += (s, e) =>
        {
            DialogResult = false;
            Close();
        };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        stackPanel.Children.Add(sampleIdLabel);
        stackPanel.Children.Add(sampleIdTextBox);
        stackPanel.Children.Add(typeLabel);
        stackPanel.Children.Add(typeComboBox);
        stackPanel.Children.Add(countLabel);
        stackPanel.Children.Add(countSlider);
        stackPanel.Children.Add(countValueLabel);
        stackPanel.Children.Add(buttonPanel);

        Content = stackPanel;
    }
}
