using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SAPVpis.Net47.Services
{
    // Mirrors Delphi SqlTable.pas: ZapisVzr, VpisChr, prenos
    internal static class SapMeasurementSqlRepository
    {
        // Delphi: ZapisVzr — INSERT header row, return ZAP (identity)
        public static long ZapisVzr(string insplot, string inspoper, string rmk, DateTime datum)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO SapVzr (INSPLOT, INSPOPER, NAZIVKK, DATUM, UPORAB, PRENOS)
VALUES (@INSPLOT, @INSPOPER, @NAZIVKK, @DATUM, @UPORAB, '')
SELECT CAST(SCOPE_IDENTITY() AS bigint)";
                    cmd.Parameters.AddWithValue("@INSPLOT", (insplot ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@INSPOPER", (inspoper ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@NAZIVKK", (rmk ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@DATUM", datum);
                    cmd.Parameters.AddWithValue("@UPORAB", Environment.MachineName);
                    var result = cmd.ExecuteScalar();
                    return result == null || result == DBNull.Value ? -1L : Convert.ToInt64(result);
                }
            }
        }

        // Delphi: VpisChr — INSERT one detail row per filled characteristic
        public static void VpisChr(long izap, int stmer, string stkar, string skupni, string tip,
            string meritev, string eval, string opomba)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO SapChr (ZAP, STKAR, STMER, SKUPNI, TIP, MERITEV, EVAL, OPOMBA)
VALUES (@ZAP, @STKAR, @STMER, @SKUPNI, @TIP, @MERITEV, @EVAL, @OPOMBA)";
                    cmd.Parameters.AddWithValue("@ZAP", izap);
                    cmd.Parameters.AddWithValue("@STKAR", (stkar ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@STMER", stmer);
                    cmd.Parameters.AddWithValue("@SKUPNI", (skupni ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@TIP", (tip ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@MERITEV", (meritev ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@EVAL", (eval ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@OPOMBA", (opomba ?? string.Empty).Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Delphi: prenos — mark header as transferred to SAP
        public static void Prenos(long izap)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "UPDATE SapVzr SET PRENOS = 'X' WHERE ZAP = @ZAP";
                    cmd.Parameters.AddWithValue("@ZAP", izap);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Delphi: Prikaz — SELECT * FROM SapVzr (most recent first)
        public static DataTable LoadSapVzr()
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("SELECT * FROM SapVzr ORDER BY ZAP DESC", conn))
            {
                conn.Open();
                new SqlDataAdapter(cmd).Fill(dt);
            }
            return dt;
        }

        // Delphi: DrugiSql — SELECT * FROM SapChr WHERE zap = :ZAP
        public static DataTable LoadSapChr(long zap)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(GetConnectionString()))
            using (var cmd = new SqlCommand("SELECT * FROM SapChr WHERE ZAP = @ZAP ORDER BY STMER", conn))
            {
                cmd.Parameters.AddWithValue("@ZAP", zap);
                conn.Open();
                new SqlDataAdapter(cmd).Fill(dt);
            }
            return dt;
        }

        // Delphi: Button3 — delete all SapChr rows then SapVzr row
        public static void DeleteVzrWithDetails(long zap)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SapChr WHERE ZAP = @ZAP";
                    cmd.Parameters.AddWithValue("@ZAP", zap);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SapVzr WHERE ZAP = @ZAP";
                    cmd.Parameters.AddWithValue("@ZAP", zap);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Delphi: Button2 (SapChr case) — delete a single detail row
        public static void DeleteChrRow(long zpkar)
        {
            using (var conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM SapChr WHERE ZPKAR = @ZPKAR";
                    cmd.Parameters.AddWithValue("@ZPKAR", zpkar);
                    cmd.ExecuteNonQuery();
                }
            }
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
