using System;
using System.Collections.Generic;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapInspectionLotService
    {
        internal sealed class InspectionLotCheckResult
        {
            public bool IsOpen { get; set; }
            public string Message { get; set; }
            public string ReturnType { get; set; }
            public string ReturnCode { get; set; }
            public string ReturnText { get; set; }
            public IReadOnlyCollection<string> StatusCodes { get; set; }
        }

        public InspectionLotCheckResult CheckIsOpen(RfcDestination destination, string lotNumber, string language)
        {
            var normalizedLot = NormalizeInspectionLot(lotNumber);
            var function = destination.Repository.CreateFunction("BAPI_INSPLOT_GETDETAIL");
            function.SetValue("NUMBER", normalizedLot);
            function.SetValue("LANGUAGE", language);
            function.Invoke(destination);

            var returnStruct = function.GetStructure("RETURN");
            var returnType = GetString(returnStruct, "TYPE");
            var returnCode = GetString(returnStruct, "NUMBER");
            var returnMessage = GetString(returnStruct, "MESSAGE");

            if (string.Equals(returnType, "E", StringComparison.OrdinalIgnoreCase)
                || string.Equals(returnType, "A", StringComparison.OrdinalIgnoreCase))
            {
                return new InspectionLotCheckResult
                {
                    IsOpen = false,
                    ReturnType = returnType,
                    ReturnCode = returnCode,
                    ReturnText = returnMessage,
                    Message = string.Format("BAPI_INSPLOT_GETDETAIL({0}) returned {1}/{2}: {3}", normalizedLot, returnType, returnCode, returnMessage),
                    StatusCodes = Array.Empty<string>()
                };
            }

            var table = function.GetTable("SYSTEM_STATUS");
            var statusCodes = new List<string>();
            var score = 0;
            for (var row = 0; row < table.RowCount; row++)
            {
                table.CurrentIndex = row;
                var status = GetString(table, "SYS_STATUS");
                statusCodes.Add(status);

                if (string.Equals(status, "I0205", StringComparison.OrdinalIgnoreCase)) score++; // DOPS
                if (string.Equals(status, "I0002", StringComparison.OrdinalIgnoreCase)) score++; // LANS
                if (string.Equals(status, "I0206", StringComparison.OrdinalIgnoreCase)) score++; // KAKK
                if (string.Equals(status, "I0216", StringComparison.OrdinalIgnoreCase)) score--; // short-term close (SERS-equivalent)
            }

            var open = score == 3;
            return new InspectionLotCheckResult
            {
                IsOpen = open,
                ReturnType = returnType,
                ReturnCode = returnCode,
                ReturnText = returnMessage,
                StatusCodes = statusCodes,
                Message = string.Format("Lot {0}: {1} (score={2}, statuses=[{3}], return={4}/{5} {6})",
                    normalizedLot,
                    open ? "OPEN" : "NOT OPEN",
                    score,
                    string.Join(",", statusCodes),
                    returnType,
                    returnCode,
                    returnMessage)
            };
        }

        private static string GetString(IRfcStructure structure, string field)
        {
            return (structure.GetString(field) ?? string.Empty).Trim();
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
    }
}
