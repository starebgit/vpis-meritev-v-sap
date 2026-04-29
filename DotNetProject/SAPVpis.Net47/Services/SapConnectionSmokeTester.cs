using System;
using System.Configuration;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    public static class SapConnectionSmokeTester
    {
        private static readonly object RegistrationLock = new object();
        private static bool _destinationConfigRegistered;
        private static SapDestinationConfiguration _destinationConfiguration;

        public static SapTrialResult RunTrial()
        {
            var timestamp = DateTime.UtcNow;

            try
            {
                var destinationName = ReadDestinationName();
                EnsureDestinationConfigurationRegistered(destinationName);

                var destination = RfcDestinationManager.GetDestination(destinationName);
                destination.Ping();

                var smokeFunctionName = ReadSmokeFunctionName();
                var repository = destination.Repository;
                var function = repository.CreateFunction(smokeFunctionName);
                function.Invoke(destination);

                return new SapTrialResult
                {
                    Success = true,
                    Message = string.Format(
                        "SAP trial passed. Destination '{0}' pinged and smoke RFC '{1}' executed.",
                        destinationName,
                        smokeFunctionName),
                    TimestampUtc = timestamp
                };
            }
            catch (Exception ex)
            {
                return new SapTrialResult
                {
                    Success = false,
                    Message = ex.Message,
                    TimestampUtc = timestamp
                };
            }
        }

        private static string ReadDestinationName()
        {
            var destinationName = ConfigurationManager.AppSettings["sap.destination.name"];
            if (string.IsNullOrWhiteSpace(destinationName))
            {
                throw new ConfigurationErrorsException(
                    "Missing required appSetting 'sap.destination.name'.");
            }

            return destinationName.Trim();
        }

        private static string ReadSmokeFunctionName()
        {
            var functionName = ConfigurationManager.AppSettings["sap.smoke.rfc"];
            if (string.IsNullOrWhiteSpace(functionName))
            {
                return "RFC_SYSTEM_INFO";
            }

            return functionName.Trim();
        }

        private static void EnsureDestinationConfigurationRegistered(string destinationName)
        {
            if (_destinationConfigRegistered)
            {
                return;
            }

            lock (RegistrationLock)
            {
                if (_destinationConfigRegistered)
                {
                    return;
                }

                _destinationConfiguration = new SapDestinationConfiguration(destinationName);
                RfcDestinationManager.RegisterDestinationConfiguration(_destinationConfiguration);
                _destinationConfigRegistered = true;
            }
        }
    }

    public class SapTrialResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
