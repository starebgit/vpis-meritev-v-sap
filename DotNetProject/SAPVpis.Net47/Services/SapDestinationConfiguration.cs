using System;
using System.Configuration;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapDestinationConfiguration : IDestinationConfiguration
    {
        private readonly string _destinationName;

        public SapDestinationConfiguration(string destinationName)
        {
            _destinationName = destinationName;
        }

        public bool ChangeEventsSupported()
        {
            return false;
        }

        public event RfcDestinationManager.ConfigurationChangeHandler ConfigurationChanged;

        public RfcConfigParameters GetParameters(string destinationName)
        {
            if (!string.Equals(destinationName, _destinationName, StringComparison.Ordinal))
            {
                return null;
            }

            return BuildParameters(destinationName);
        }

        private static RfcConfigParameters BuildParameters(string destinationName)
        {
            var parameters = new RfcConfigParameters();

            parameters[RfcConfigParameters.Name] = destinationName;
            parameters[RfcConfigParameters.AppServerHost] = ReadRequired("sap.ashost");
            parameters[RfcConfigParameters.SystemNumber] = ReadRequired("sap.sysnr");
            parameters[RfcConfigParameters.Client] = ReadRequired("sap.client");
            parameters[RfcConfigParameters.User] = ReadRequired("sap.user");
            parameters[RfcConfigParameters.Password] = ReadRequired("sap.passwd");
            parameters[RfcConfigParameters.Language] = ReadRequired("sap.lang");


            return parameters;
        }

        private static string ReadRequired(string key)
        {
            var value = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ConfigurationErrorsException(
                    string.Format("Missing required appSetting '{0}' for SAP destination.", key));
            }

            return value.Trim();
        }

    }
}
