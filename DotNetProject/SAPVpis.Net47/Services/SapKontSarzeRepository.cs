using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SAPVpis.Net47.Services
{
    // Mirrors Delphi SqlSarze.pas: getodd() and getsarze() from KontSarze table
    internal static class SapKontSarzeRepository
    {
        internal sealed class SarzeEntry
        {
            public string Sarza { get; set; }
            public string Tekst { get; set; }
        }

        public static List<string> GetDepartments()
        {
            var result = new List<string>();
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("SELECT DISTINCT ODDELEK FROM KontSarze ORDER BY ODDELEK", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var v = reader["ODDELEK"];
                        result.Add(v == DBNull.Value ? string.Empty : v.ToString().Trim());
                    }
                }
            }
            return result;
        }

        public static List<SarzeEntry> GetSarze(string oddelek)
        {
            var result = new List<SarzeEntry>();
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("SELECT sarza, tekst FROM KontSarze WHERE ODDELEK = @ODD", conn))
            {
                cmd.Parameters.AddWithValue("@ODD", oddelek ?? string.Empty);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SarzeEntry
                        {
                            Sarza = ReadColumn(reader, "sarza"),
                            Tekst = ReadColumn(reader, "tekst")
                        });
                    }
                }
            }
            return result;
        }

        // Delphi: ADOQuery1.SQL := 'SELECT * FROM Kontsarze' (TFSqlSarze.Pokazi)
        public static DataTable LoadAll()
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("SELECT * FROM KontSarze ORDER BY ODDELEK, SARZA", conn))
            {
                conn.Open();
                new SqlDataAdapter(cmd).Fill(dt);
            }
            return dt;
        }

        // Delphi: TFSqlSarze.Button2Click — Adoquery1.Delete (deletes current row)
        public static int DeleteRow(string sarza, string oddelek)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand(
                "DELETE FROM KontSarze WHERE SARZA = @SARZA AND ODDELEK = @ODD", conn))
            {
                cmd.Parameters.AddWithValue("@SARZA", (sarza ?? string.Empty).Trim());
                cmd.Parameters.AddWithValue("@ODD", (oddelek ?? string.Empty).Trim());
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        private static string ReadColumn(SqlDataReader reader, string column)
        {
            var v = reader[column];
            return v == DBNull.Value ? string.Empty : v.ToString().Trim();
        }

        private static string GetConnectionString()
        {
            var raw = ConfigurationManager.AppSettings["sap.login.db.connection_string"];
            if (string.IsNullOrWhiteSpace(raw))
                throw new ConfigurationErrorsException("Missing appSetting 'sap.login.db.connection_string'.");
            return SapLoginRepository.NormalizeToSqlClientConnectionStringForSharedUsage(raw.Trim());
        }
    }
}
