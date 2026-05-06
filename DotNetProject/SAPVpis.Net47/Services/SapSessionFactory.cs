using System.Configuration;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal static class SapSessionFactory
    {
        private static readonly object Lock = new object();
        private static RfcConfigParameters _params;
        private static string _plant;

        public static RfcDestination GetDestination()
        {
            return RfcDestinationManager.GetDestination(GetOrBuildParams());
        }

        public static string GetPlant()
        {
            GetOrBuildParams();
            return _plant;
        }

        private static RfcConfigParameters GetOrBuildParams()
        {
            if (_params != null) return _params;
            lock (Lock)
            {
                if (_params != null) return _params;
                var login = SapLoginRepository.GetDefaultLogin();
                _plant = SapLoginRepository.ResolvePlant(login.User);
                var p = new RfcConfigParameters();
                p[RfcConfigParameters.Name] = GetDestinationName();
                p[RfcConfigParameters.AppServerHost] = login.ApplicationServer;
                p[RfcConfigParameters.SystemNumber] = login.SystemNumber;
                p[RfcConfigParameters.Client] = login.Client;
                p[RfcConfigParameters.User] = login.User;
                p[RfcConfigParameters.Password] = login.Password;
                p[RfcConfigParameters.Language] = login.Language;
                if (!string.IsNullOrWhiteSpace(login.System))
                    p[RfcConfigParameters.SystemID] = login.System;
                _params = p;
                return p;
            }
        }

        private static string GetDestinationName()
        {
            var v = ConfigurationManager.AppSettings["sap.destination.name"];
            return string.IsNullOrWhiteSpace(v) ? "SAP_DEFAULT" : v.Trim();
        }
    }
}
