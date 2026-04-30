using System;
using System.Collections.Generic;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapInspectionOperationService
    {
        internal sealed class OperationReadResult
        {
            public IReadOnlyCollection<string> Operations { get; set; }
            public string ReturnType { get; set; }
            public string ReturnNumber { get; set; }
            public string ReturnMessage { get; set; }
            public string Message { get; set; }
        }

        public OperationReadResult ReadOperations(RfcDestination destination, string lotNumber)
        {
            var normalizedLot = NormalizeInspectionLot(lotNumber);
            var function = destination.Repository.CreateFunction("BAPI_INSPLOT_GETOPERATIONS");
            function.SetValue("NUMBER", normalizedLot);
            function.Invoke(destination);

            var ret = function.GetStructure("RETURN");
            var returnType = GetString(ret, "TYPE");
            var returnNumber = GetString(ret, "NUMBER");
            var returnMessage = GetString(ret, "MESSAGE");

            var table = function.GetTable("INSPOPER_LIST");
            var operations = new List<string>();
            for (var row = 0; row < table.RowCount; row++)
            {
                table.CurrentIndex = row;
                operations.Add(GetString(table, "INSPOPER"));
            }

            return new OperationReadResult
            {
                Operations = operations,
                ReturnType = returnType,
                ReturnNumber = returnNumber,
                ReturnMessage = returnMessage,
                Message = string.Format("Lot {0}: operations={1}, list=[{2}], return={3}/{4} {5}",
                    normalizedLot,
                    operations.Count,
                    string.Join(",", operations),
                    returnType,
                    returnNumber,
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
