using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using VocabAutomation.Services.Interfaces;

namespace VocabAutomation.Services
{
    public class VocabService : IVocabService
    {
        private readonly string _connectionString;
        private readonly ILogger<VocabService> _logger;

        public VocabService(IConfiguration configuration, ILogger<VocabService> logger)
        {
            _connectionString = configuration["POSTGRES_CONN"];
            _logger = logger;

            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("❌ POSTGRES_CONN is missing in environment variables.");
                throw new ArgumentException("POSTGRES_CONN is missing in environment variables.");
            }
        }

        public async Task StoreVocabularyAsync(List<(string Word, string Meaning)> entries, string tableName)
        {
            try
            {
                tableName = SanitizeTableName(tableName);

                using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();

                cmd.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS \""{tableName}\ ""(
                        id SERIAL PRIMARY KEY,
                        word TEXT UNIQUE NOT NULL,
                        meaning TEXT,
                        created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        sent_date TIMESTAMP
                    );
                    ";
                await cmd.ExecuteNonQueryAsync();

                foreach (var (word, meaning) in entries)
                {
                    cmd.CommandText = $@"
                        INSERT INTO \""{tableName}\"" (word, meaning)
                        VALUES (@word, @meaning)
                        ON CONFLICT (word) DO UPDATE SET meaning = EXCLUDED.meaning;";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("word", word);
                    cmd.Parameters.AddWithValue("meaning", meaning);
                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation("✅ Inserted {Count} entries into table: {Table}", entries.Count, tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inserting into DB for table: {Table}", tableName);
            }
        }

public async Task<List<(int Id, string Word, string Meaning)>> GetVocabBatchAsync(string tableName)
{
    var result = new List<(int Id, string Word, string Meaning)>();

    try
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();

        // Step 1: Fetch words sent today
        cmd.CommandText = $@"
            SELECT id, word, meaning FROM ""{tableName}""
            WHERE sent_date::date = CURRENT_DATE
            ORDER BY id
            LIMIT 10;";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        reader.Close();

        if (result.Count > 0)
        {
            _logger.LogInformation($"✅ {result.Count} words already sent today from table {tableName}.");
            return result;
        }

        // Step 2: Fetch unsent words
        cmd.CommandText = $@"
            SELECT id, word, meaning FROM ""{tableName}""
            WHERE sent_date IS NULL
            ORDER BY id
            LIMIT 10;";
        
        var ids = new List<int>();
        using var reader2 = await cmd.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
        {
            int id = reader2.GetInt32(0);
            ids.Add(id);
            result.Add((id, reader2.GetString(1), reader2.GetString(2)));
        }
        reader2.Close();

        if (ids.Count > 0)
        {
            cmd.CommandText = $@"
                UPDATE ""{tableName}""
                SET sent_date = CURRENT_TIMESTAMP
                WHERE id = ANY(@ids);";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("ids", ids);
            await cmd.ExecuteNonQueryAsync();

            _logger.LogInformation($"✅ Marked {ids.Count} new words as sent from table {tableName}.");
        }
        else
        {
            _logger.LogInformation($"📭 No new or already-sent words found in table {tableName}.");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"❌ Error while fetching batch from {tableName}.");
    }

    return result;
}


        public async Task MarkWordsAsSentAsync(List<int> ids,string tableName)
        {
            if (ids.Count == 0) return;
            tableName = SanitizeTableName(tableName);

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();

                cmd.CommandText = $@"
                    UPDATE ""{tableName}""
                    SET sent_date = CURRENT_TIMESTAMP
                    WHERE id = ANY(@ids);";

                cmd.Parameters.AddWithValue("ids", ids);
                await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation("✅ Marked {Count} words as sent in table: {Table}", ids.Count, tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error marking words as sent for table: {Table}", tableName);
            }
        }

        private string SanitizeTableName(string name)
        {
            return Regex.Replace(name, @"\W+", "_").Trim('_').ToLower();
        }
    }
}