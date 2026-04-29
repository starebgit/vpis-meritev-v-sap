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
            var function = destination.Repository.CreateFunction("BAPI_INSPLOT_GETDETAIL");
            function.SetValue("NUMBER", lotNumber);
            function.SetValue("LANGUAGE", language);
            function.Invoke(destination);

            var returnStruct = function.GetStructure("RETURN");
            var returnType = GetString(returnStruct, "TYPE");
            var returnCode = GetStringSafe(returnStruct, "NUMBER", "CODE");
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
                    Message = string.Format("BAPI_INSPLOT_GETDETAIL returned {0}/{1}: {2}", returnType, returnCode, returnMessage),
                    StatusCodes = Array.Empty<string>()
                };
            }

            var table = function.GetTable("SYSTEM_STATUS");
            var statusCodes = new List<string>();
            var score = 0;
            for (var row = 0; row < table.RowCount; row++)
            {
                table.CurrentIndex = row;
                var status = GetString(table, "SYS_ST");
                statusCodes.Add(status);

                if (string.Equals(status, "DOPS", StringComparison.OrdinalIgnoreCase)) score++;
                if (string.Equals(status, "LANS", StringComparison.OrdinalIgnoreCase)) score++;
                if (string.Equals(status, "KAKK", StringComparison.OrdinalIgnoreCase)) score++;
                if (string.Equals(status, "SERS", StringComparison.OrdinalIgnoreCase)) score--;
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
                    lotNumber,
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

        private static string GetStringSafe(IRfcStructure structure, params string[] fields)
        {
            foreach (var field in fields)
            {
                try
                {
                    var value = GetString(structure, field);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
                catch
                {
                }
            }

            return string.Empty;
        }

        private static string GetString(IRfcTable table, string field)
        {
            return (table.GetString(field) ?? string.Empty).Trim();
        }
    }
}
