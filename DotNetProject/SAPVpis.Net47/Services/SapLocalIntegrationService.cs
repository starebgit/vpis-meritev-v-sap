using System;
using System.Configuration;
using System.Data.SqlClient;

namespace SAPVpis.Net47.Services
{
    internal static class SapLocalIntegrationService
    {
        public static void SaveTrialSnapshot(string system, bool success, string message, DateTime timestampUtc)
        {
            if (!ReadEnabled())
            {
                return;
            }

            var connectionString = ReadConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                EnsureTable(conn);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
insert into dbo.dotnet_sap_trial_log(system_name, is_success, message, timestamp_utc)
values(@system_name, @is_success, @message, @timestamp_utc)";
                    cmd.Parameters.AddWithValue("@system_name", (system ?? string.Empty).Trim());
                    cmd.Parameters.AddWithValue("@is_success", success ? 1 : 0);
                    cmd.Parameters.AddWithValue("@message", message ?? string.Empty);
                    cmd.Parameters.AddWithValue("@timestamp_utc", timestampUtc);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool ReadEnabled()
        {
            var raw = ConfigurationManager.AppSettings["sap.step7.enable_local_log"];
            return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadConnectionString()
        {
            var value = ConfigurationManager.AppSettings["sap.login.db.connection_string"];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException("Missing appSetting 'sap.login.db.connection_string'.");
            }

            return SapLoginRepository.NormalizeToSqlClientConnectionStringForSharedUsage(value.Trim());
        }

        private static void EnsureTable(SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
if object_id('dbo.dotnet_sap_trial_log', 'U') is null
begin
    create table dbo.dotnet_sap_trial_log
    (
        id int identity(1,1) primary key,
        system_name nvarchar(32) not null,
        is_success bit not null,
        message nvarchar(max) not null,
        timestamp_utc datetime2 not null
    )
end";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
