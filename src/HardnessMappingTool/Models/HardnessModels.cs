using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HardnessMappingTool.Models
{
    public class HardnessTestData : INotifyPropertyChanged
    {
        private int _id;
        private string _sampleId = string.Empty;
        private HardnessType _hardnessType;
        private double _x;
        private double _y;
        private double _z;
        private double _hardnessValue;
        private double? _load;
        private double? _indentationDiagonal;
        private DateTime _testTime;
        private string _operatorName = string.Empty;
        private string _remarks = string.Empty;
        private bool _isValid = true;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string SampleId
        {
            get => _sampleId;
            set { _sampleId = value; OnPropertyChanged(); }
        }

        public HardnessType HardnessType
        {
            get => _hardnessType;
            set { _hardnessType = value; OnPropertyChanged(); }
        }

        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }

        public double Z
        {
            get => _z;
            set { _z = value; OnPropertyChanged(); }
        }

        public double HardnessValue
        {
            get => _hardnessValue;
            set { _hardnessValue = value; OnPropertyChanged(); }
        }

        public double? Load
        {
            get => _load;
            set { _load = value; OnPropertyChanged(); }
        }

        public double? IndentationDiagonal
        {
            get => _indentationDiagonal;
            set { _indentationDiagonal = value; OnPropertyChanged(); }
        }

        public DateTime TestTime
        {
            get => _testTime;
            set { _testTime = value; OnPropertyChanged(); }
        }

        public string OperatorName
        {
            get => _operatorName;
            set { _operatorName = value; OnPropertyChanged(); }
        }

        public string Remarks
        {
            get => _remarks;
            set { _remarks = value; OnPropertyChanged(); }
        }

        public bool IsValid
        {
            get => _isValid;
            set { _isValid = value; OnPropertyChanged(); }
        }

        public HardnessTestData Clone()
        {
            return (HardnessTestData)MemberwiseClone();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum HardnessType
    {
        Rockwell_B,
        Rockwell_C,
        Brinell,
        Vickers,
        Knoop,
        Shore
    }

    public class MaterialSample : INotifyPropertyChanged
    {
        private string _sampleId = string.Empty;
        private string _materialName = string.Empty;
        private string _materialType = string.Empty;
        private string _batchNumber = string.Empty;
        private double _thickness;
        private DateTime _receiveDate;
        private string _description = string.Empty;

        public string SampleId
        {
            get => _sampleId;
            set { _sampleId = value; OnPropertyChanged(); }
        }

        public string MaterialName
        {
            get => _materialName;
            set { _materialName = value; OnPropertyChanged(); }
        }

        public string MaterialType
        {
            get => _materialType;
            set { _materialType = value; OnPropertyChanged(); }
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set { _batchNumber = value; OnPropertyChanged(); }
        }

        public double Thickness
        {
            get => _thickness;
            set { _thickness = value; OnPropertyChanged(); }
        }

        public DateTime ReceiveDate
        {
            get => _receiveDate;
            set { _receiveDate = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatisticsResult
    {
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Median { get; set; }
        public double Variance { get; set; }
        public int Count { get; set; }
        public int ValidCount { get; set; }
        public double Range => Maximum - Minimum;
    }

    public class DistributionMatrix
    {
        public double[,] Values { get; set; } = new double[0, 0];
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public double StepX { get; set; }
        public double StepY { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
    }
}
