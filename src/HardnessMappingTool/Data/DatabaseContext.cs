using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using HardnessMappingTool.Models;

namespace HardnessMappingTool.Data
{
    public class DatabaseContext
    {
        private readonly string _databasePath;

        public DatabaseContext()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HardnessMappingTool");
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _databasePath = Path.Combine(appDataPath, "hardness.db");
        }

        public string DatabasePath => _databasePath;

        public async Task InitializeDatabaseAsync()
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS HardnessTestData (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SampleId TEXT NOT NULL,
                    HardnessType INTEGER NOT NULL,
                    X REAL NOT NULL,
                    Y REAL NOT NULL,
                    Z REAL NOT NULL,
                    HardnessValue REAL NOT NULL,
                    Load REAL,
                    IndentationDiagonal REAL,
                    TestTime TEXT NOT NULL,
                    OperatorName TEXT,
                    Remarks TEXT,
                    IsValid INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS MaterialSamples (
                    SampleId TEXT PRIMARY KEY,
                    MaterialName TEXT NOT NULL,
                    MaterialType TEXT,
                    BatchNumber TEXT,
                    Thickness REAL,
                    ReceiveDate TEXT,
                    Description TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_hardness_sample ON HardnessTestData(SampleId);
                CREATE INDEX IF NOT EXISTS idx_hardness_coords ON HardnessTestData(X, Y, Z);
            ";

            await createTableCommand.ExecuteNonQueryAsync();
        }

        public async Task<List<HardnessTestData>> GetAllTestDataAsync(string? sampleId = null)
        {
            var results = new List<HardnessTestData>();
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            if (string.IsNullOrEmpty(sampleId))
            {
                command.CommandText = "SELECT * FROM HardnessTestData ORDER BY TestTime DESC";
            }
            else
            {
                command.CommandText = "SELECT * FROM HardnessTestData WHERE SampleId = @SampleId ORDER BY X, Y";
                command.Parameters.AddWithValue("@SampleId", sampleId);
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new HardnessTestData
                {
                    Id = reader.GetInt32(0),
                    SampleId = reader.GetString(1),
                    HardnessType = (HardnessType)reader.GetInt32(2),
                    X = reader.GetDouble(3),
                    Y = reader.GetDouble(4),
                    Z = reader.GetDouble(5),
                    HardnessValue = reader.GetDouble(6),
                    Load = reader.IsDBNull(7) ? null : reader.GetDouble(7),
                    IndentationDiagonal = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                    TestTime = DateTime.Parse(reader.GetString(9)),
                    OperatorName = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                    Remarks = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                    IsValid = reader.GetBoolean(12)
                });
            }

            return results;
        }

        public async Task<int> InsertTestDataAsync(HardnessTestData data)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO HardnessTestData 
                (SampleId, HardnessType, X, Y, Z, HardnessValue, Load, IndentationDiagonal, TestTime, OperatorName, Remarks, IsValid)
                VALUES (@SampleId, @HardnessType, @X, @Y, @Z, @HardnessValue, @Load, @IndentationDiagonal, @TestTime, @OperatorName, @Remarks, @IsValid);
                SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("@SampleId", data.SampleId);
            command.Parameters.AddWithValue("@HardnessType", (int)data.HardnessType);
            command.Parameters.AddWithValue("@X", data.X);
            command.Parameters.AddWithValue("@Y", data.Y);
            command.Parameters.AddWithValue("@Z", data.Z);
            command.Parameters.AddWithValue("@HardnessValue", data.HardnessValue);
            command.Parameters.AddWithValue("@Load", data.Load.HasValue ? data.Load.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IndentationDiagonal", data.IndentationDiagonal.HasValue ? data.IndentationDiagonal.Value : DBNull.Value);
            command.Parameters.AddWithValue("@TestTime", data.TestTime.ToString("o"));
            command.Parameters.AddWithValue("@OperatorName", string.IsNullOrEmpty(data.OperatorName) ? DBNull.Value : data.OperatorName);
            command.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(data.Remarks) ? DBNull.Value : data.Remarks);
            command.Parameters.AddWithValue("@IsValid", data.IsValid);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task BulkInsertTestDataAsync(IEnumerable<HardnessTestData> dataList)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            foreach (var data in dataList)
            {
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO HardnessTestData 
                    (SampleId, HardnessType, X, Y, Z, HardnessValue, Load, IndentationDiagonal, TestTime, OperatorName, Remarks, IsValid)
                    VALUES (@SampleId, @HardnessType, @X, @Y, @Z, @HardnessValue, @Load, @IndentationDiagonal, @TestTime, @OperatorName, @Remarks, @IsValid)
                ";

                command.Parameters.AddWithValue("@SampleId", data.SampleId);
                command.Parameters.AddWithValue("@HardnessType", (int)data.HardnessType);
                command.Parameters.AddWithValue("@X", data.X);
                command.Parameters.AddWithValue("@Y", data.Y);
                command.Parameters.AddWithValue("@Z", data.Z);
                command.Parameters.AddWithValue("@HardnessValue", data.HardnessValue);
                command.Parameters.AddWithValue("@Load", data.Load.HasValue ? data.Load.Value : DBNull.Value);
                command.Parameters.AddWithValue("@IndentationDiagonal", data.IndentationDiagonal.HasValue ? data.IndentationDiagonal.Value : DBNull.Value);
                command.Parameters.AddWithValue("@TestTime", data.TestTime.ToString("o"));
                command.Parameters.AddWithValue("@OperatorName", string.IsNullOrEmpty(data.OperatorName) ? DBNull.Value : data.OperatorName);
                command.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(data.Remarks) ? DBNull.Value : data.Remarks);
                command.Parameters.AddWithValue("@IsValid", data.IsValid);

                command.Transaction = transaction;
                await command.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }

        public async Task<bool> DeleteTestDataAsync(int id)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM HardnessTestData WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteSampleDataAsync(string sampleId)
        {
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM HardnessTestData WHERE SampleId = @SampleId";
            command.Parameters.AddWithValue("@SampleId", sampleId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task UpdateTestDataAsync(List<HardnessTestData> dataList)
        {
            if (dataList.Count == 0) return;

            var sampleId = dataList[0].SampleId;

            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            var deleteCommand = connection.CreateCommand();
            deleteCommand.CommandText = "DELETE FROM HardnessTestData WHERE SampleId = @SampleId";
            deleteCommand.Parameters.AddWithValue("@SampleId", sampleId);
            deleteCommand.Transaction = transaction;
            await deleteCommand.ExecuteNonQueryAsync();

            foreach (var data in dataList)
            {
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = @"
                    INSERT INTO HardnessTestData 
                    (SampleId, HardnessType, X, Y, Z, HardnessValue, Load, IndentationDiagonal, TestTime, OperatorName, Remarks, IsValid)
                    VALUES (@SampleId, @HardnessType, @X, @Y, @Z, @HardnessValue, @Load, @IndentationDiagonal, @TestTime, @OperatorName, @Remarks, @IsValid)
                ";

                insertCommand.Parameters.AddWithValue("@SampleId", data.SampleId);
                insertCommand.Parameters.AddWithValue("@HardnessType", (int)data.HardnessType);
                insertCommand.Parameters.AddWithValue("@X", data.X);
                insertCommand.Parameters.AddWithValue("@Y", data.Y);
                insertCommand.Parameters.AddWithValue("@Z", data.Z);
                insertCommand.Parameters.AddWithValue("@HardnessValue", data.HardnessValue);
                insertCommand.Parameters.AddWithValue("@Load", data.Load.HasValue ? data.Load.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@IndentationDiagonal", data.IndentationDiagonal.HasValue ? data.IndentationDiagonal.Value : DBNull.Value);
                insertCommand.Parameters.AddWithValue("@TestTime", data.TestTime.ToString("o"));
                insertCommand.Parameters.AddWithValue("@OperatorName", string.IsNullOrEmpty(data.OperatorName) ? DBNull.Value : data.OperatorName);
                insertCommand.Parameters.AddWithValue("@Remarks", string.IsNullOrEmpty(data.Remarks) ? DBNull.Value : data.Remarks);
                insertCommand.Parameters.AddWithValue("@IsValid", data.IsValid);

                insertCommand.Transaction = transaction;
                await insertCommand.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }

        public async Task<List<string>> GetAllSampleIdsAsync()
        {
            var results = new List<string>();
            using var connection = new SqliteConnection($"Data Source={_databasePath}");
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT DISTINCT SampleId FROM HardnessTestData ORDER BY SampleId";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(reader.GetString(0));
            }

            return results;
        }
    }
}
