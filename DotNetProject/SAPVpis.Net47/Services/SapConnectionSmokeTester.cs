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
            return RunTrialInternal(SapLoginRepository.GetDefaultLogin(), ReadDestinationName());
        }

        public static SapTrialResult RunTrialForSystem(string system)
        {
            var target = string.IsNullOrWhiteSpace(system) ? "E4Q" : system.Trim();
            var login = SapLoginRepository.GetLoginBySystem(target);
            return RunTrialInternal(login, ReadDestinationName() + "_" + target);
        }

        private static SapTrialResult RunTrialInternal(SapLoginRepository.SapLoginRow login, string destinationName)
        {
            var timestamp = DateTime.UtcNow;

            try
            {
                var destination = RfcDestinationManager.GetDestination(BuildParameters(login, destinationName));
                destination.Ping();

                var smokeFunctionName = ReadSmokeFunctionName();
                var plant = SapLoginRepository.ResolvePlant(login.User);
                var repository = destination.Repository;
                var function = repository.CreateFunction(smokeFunctionName);
                function.Invoke(destination);

                var lotNumber = ReadInspectionLot();
                var lotLanguage = ReadLotLanguage();
                var lotService = new SapInspectionLotService();
                var lotResult = lotService.CheckIsOpen(destination, lotNumber, lotLanguage);
                var operationService = new SapInspectionOperationService();
                var operationResult = operationService.ReadOperations(destination, lotNumber);
                var step6PostingEnabled = ReadStep6PostingEnabled();
                string step6InspCharSource;
                var step6InspChar = ResolveStep6InspChar(destination, lotNumber, "0010", out step6InspCharSource);
                var step6PostMessage = string.Format(
                    "Phase 1 preview (no posting). INSPCHAR={0} (source={1})",
                    string.IsNullOrWhiteSpace(step6InspChar) ? "<empty>" : step6InspChar,
                    step6InspCharSource);
                if (step6PostingEnabled)
                {
                    var resultValue = ReadStep6ResultValue();
                    var postService = new SapInspectionRecordPostService();
                    var postResult = postService.PostSingleResult(destination, lotNumber, "0010", step6InspChar, resultValue);
                    step6PostMessage = postResult.Success ? postResult.Message : "ERROR: " + postResult.Message;
                }

                return new SapTrialResult
                {
                    Success = true,
                    Message = string.Format(
                        "SAP trial passed. Destination '{0}' pinged and smoke RFC '{1}' executed.",
                        destinationName,
                        smokeFunctionName) + Environment.NewLine +
                        string.Format("Plant rule applied: user '{0}' -> plant '{1}'.", login.User, plant) + Environment.NewLine +
                        string.Format("Step 5 lot check (BAPI_INSPLOT_GETDETAIL, LANGUAGE={0}): {1}", lotLanguage, lotResult.Message) + Environment.NewLine +
                        string.Format("Step 5 operations (BAPI_INSPLOT_GETOPERATIONS): {0}", operationResult.Message) + Environment.NewLine +
                        string.Format("Step 6 mode: {0}", step6PostingEnabled ? "POST enabled (phase 2)" : "PREVIEW only - posting disabled (phase 1)") + Environment.NewLine +
                        string.Format("Step 6 char source: {0}", step6InspCharSource) + Environment.NewLine +
                        string.Format("Step 6 posting result: {0}", step6PostMessage),
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

        private static RfcConfigParameters BuildParameters(SapLoginRepository.SapLoginRow login, string destinationName)
        {
            var parameters = new RfcConfigParameters();
            parameters[RfcConfigParameters.Name] = destinationName;
            parameters[RfcConfigParameters.AppServerHost] = login.ApplicationServer;
            parameters[RfcConfigParameters.SystemNumber] = login.SystemNumber;
            parameters[RfcConfigParameters.Client] = login.Client;
            parameters[RfcConfigParameters.User] = login.User;
            parameters[RfcConfigParameters.Password] = login.Password;
            parameters[RfcConfigParameters.Language] = login.Language;
            if (!string.IsNullOrWhiteSpace(login.System))
            {
                parameters[RfcConfigParameters.SystemID] = login.System;
            }

            return parameters;
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

        private static string ReadInspectionLot()
        {
            var lot = ConfigurationManager.AppSettings["sap.step5.lot"];
            return string.IsNullOrWhiteSpace(lot) ? "30002292667" : lot.Trim();
        }

        private static string ReadLotLanguage()
        {
            var language = ConfigurationManager.AppSettings["sap.step5.language"];
            return string.IsNullOrWhiteSpace(language) ? "5" : language.Trim();
        }

        private static bool ReadStep6PostingEnabled()
        {
            var raw = ConfigurationManager.AppSettings["sap.step6.enable_post"];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return string.Equals(raw.Trim(), "true", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(raw.Trim(), "1", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(raw.Trim(), "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveStep6InspChar(RfcDestination destination, string lotNumber, string inspOper, out string source)
        {
            var configured = ReadStep6InspCharFallback();
            try
            {
                var normalizedLot = NormalizeInspectionLot(lotNumber);
                var op = (inspOper ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(op))
                {
                    source = "fallback:empty-op";
                    return configured;
                }

                var function = destination.Repository.CreateFunction("BAPI_INSPOPER_GETDETAIL");
                function.SetValue("INSPLOT", normalizedLot);
                function.SetValue("INSPOPER", op);
                function.SetValue("READ_CHAR_REQUIREMENTS", "X");
                function.Invoke(destination);

                var chars = function.GetTable("INSPCHAR");
                if (chars.RowCount == 0)
                {
                    source = "fallback:no-inspchar-rows";
                    return configured;
                }

                chars.CurrentIndex = 0;
                var inspChar = GetString(chars, "INSPCHAR");
                if (string.IsNullOrWhiteSpace(inspChar))
                {
                    source = "fallback:first-inspchar-empty";
                    return configured;
                }

                source = "sap:inspchar[0]";
                return inspChar;
            }
            catch
            {
                source = "fallback:exception";
                return configured;
            }
        }

        private static string ReadStep6InspCharFallback()
        {
            var raw = ConfigurationManager.AppSettings["sap.step6.inspchar"];
            return string.IsNullOrWhiteSpace(raw) ? "0001" : raw.Trim();
        }

        private static string ReadStep6ResultValue()
        {
            var raw = ConfigurationManager.AppSettings["sap.step6.result_value"];
            return string.IsNullOrWhiteSpace(raw) ? "0" : raw.Trim();
        }

        private static string GetString(IRfcTable table, string field)
        {
            return (table.GetString(field) ?? string.Empty).Trim();
        }

        private static string NormalizeInspectionLot(string lot)
        {
            var value = (lot ?? string.Empty).Trim();
            return value.Length >= 12 ? value : value.PadLeft(12, '0');
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
