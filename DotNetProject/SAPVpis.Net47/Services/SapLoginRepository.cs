using System;
using System.Configuration;
using System.Data;
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
            var connectionString = ReadRequired("sap.login.db.connection_string");
            const string sql = @"select top 1 uporab, sistem, client, streznik, sysnnum, pass, jezik from prijava where glavni = 'X'";

            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
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
    }
}
