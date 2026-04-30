using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapLoginRepository
    {
        internal sealed class SapLoginRow
        {
            public string User { get; set; }
            public string System { get; set; }
            public string Client { get; set; }
            public string ApplicationServer { get; set; }
            public string SystemNumber { get; set; }
            public string Password { get; set; }
            public string Language { get; set; }
        }

        public static SapLoginRow GetDefaultLogin()
        {
            return QuerySingleLogin("select top 1 uporab, sistem, client, streznik, sysnnum, pass, jezik from prijava where glavni = 'X'", null);
        }

        public static SapLoginRow GetLoginBySystem(string system)
        {
            return QuerySingleLogin("select top 1 uporab, sistem, client, streznik, sysnnum, pass, jezik from prijava where sistem = :SYSTEM", system);
        }

        private static SapLoginRow QuerySingleLogin(string sql, string system)
        {
            var configuredConnectionString = ReadRequired("sap.login.db.connection_string");
            var connectionString = NormalizeToSqlClientConnectionString(configuredConnectionString);

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(system))
                {
                    cmd.CommandText = cmd.CommandText.Replace(":SYSTEM", "@SYSTEM");
                    cmd.Parameters.AddWithValue("@SYSTEM", system.Trim());
                }
                conn.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (!reader.Read())
                    {
                        throw new ConfigurationErrorsException("Table 'prijava' has no row with glavni='X'.");
                    }

                    return new SapLoginRow
                    {
                        User = ReadColumn(reader, "uporab"),
                        System = ReadColumn(reader, "sistem"),
                        Client = ReadColumn(reader, "client"),
                        ApplicationServer = ReadColumn(reader, "streznik"),
                        SystemNumber = ReadColumn(reader, "sysnnum"),
                        Password = ReadColumn(reader, "pass"),
                        Language = ReadColumn(reader, "jezik")
                    };
                }
            }
        }

        public static string ResolvePlant(string sapUser)
        {
            var normalized = (sapUser ?? string.Empty).Trim();
            return string.Equals(normalized, "rsg_rfc_1", StringComparison.OrdinalIgnoreCase) ? "0401" : "1061";
        }

        private static string ReadColumn(SqlDataReader reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value).Trim();
        }

        private static string ReadRequired(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(string.Format("Missing required appSetting '{0}'.", key));
            }

            return value.Trim();
        }

        internal static string NormalizeToSqlClientConnectionStringForSharedUsage(string raw)
        {
            return NormalizeToSqlClientConnectionString(raw);
        }

        private static string NormalizeToSqlClientConnectionString(string raw)
        {
            var source = new DbConnectionStringBuilder { ConnectionString = raw };
            var sql = new SqlConnectionStringBuilder();

            sql.DataSource = ReadFirst(source, "Data Source", "Server", "Address", "Addr", "Network Address");
            sql.InitialCatalog = ReadFirst(source, "Initial Catalog", "Database");
            sql.UserID = ReadFirst(source, "User ID", "UID", "User");
            sql.Password = ReadFirst(source, "Password", "PWD");

            if (TryRead(source, out var integrated, "Integrated Security", "Trusted_Connection"))
            {
                sql.IntegratedSecurity = IsTruthy(integrated);
            }

            return sql.ConnectionString;
        }

        private static string ReadFirst(DbConnectionStringBuilder builder, params string[] keys)
        {
            if (TryRead(builder, out var value, keys))
            {
                return value;
            }

            throw new ConfigurationErrorsException(
                string.Format("Missing required connection string key. Expected one of: {0}", string.Join(", ", keys)));
        }

        private static bool TryRead(DbConnectionStringBuilder builder, out string value, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (builder.TryGetValue(key, out var obj) && obj != null)
                {
                    value = Convert.ToString(obj).Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        private static bool IsTruthy(string value)
        {
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "sspi", StringComparison.OrdinalIgnoreCase)
                   || value == "1";
        }
    }
}
