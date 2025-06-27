using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.RegularExpressions;
using VocabAutomation.Services.Interfaces;

namespace VocabAutomation.Services
{
    public class VocabStorageService : IVocabStorageService
    {
        private readonly string _connectionString;

        public VocabStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["POSTGRES_CONN"];
            if (string.IsNullOrEmpty(_connectionString))
            {
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

                // Create table if not exists
                cmd.CommandText = $@"
                    CREATE TABLE IF NOT EXISTS ""{tableName}"" (
                        id SERIAL PRIMARY KEY,
                        word TEXT UNIQUE NOT NULL,
                        meaning TEXT,
                        created_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        sent_date TIMESTAMP
                    );";
                await cmd.ExecuteNonQueryAsync();

                foreach (var (word, meaning) in entries)
                {
                    cmd.CommandText = $@"
                        INSERT INTO ""{tableName}"" (word, meaning)
                        VALUES (@word, @meaning)
                        ON CONFLICT (word) DO UPDATE SET meaning = EXCLUDED.meaning;";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("word", word);
                    cmd.Parameters.AddWithValue("meaning", meaning);
                    await cmd.ExecuteNonQueryAsync();
                }

                Console.WriteLine($"✅ Inserted {entries.Count} entries into table: {tableName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error inserting into DB: {ex.Message}");
            }
        }

        private string SanitizeTableName(string name)
        {
            return Regex.Replace(name, @"\W+", "_").Trim('_').ToLower();
        }
    }
}
