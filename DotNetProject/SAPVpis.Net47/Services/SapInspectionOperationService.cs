using System;
using System.Collections.Generic;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapInspectionOperationService
    {
        // Delphi: tabr.value[j,2]+'  '+tabr.value[j,4] — col2=INSPOPER, col4=LTXA1
        internal sealed class OperationEntry
        {
            public string InspOper { get; set; }
            public string ShortText { get; set; }

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(ShortText) ? InspOper : InspOper + "  " + ShortText;
            }
        }

        internal sealed class OperationReadResult
        {
            public IReadOnlyCollection<OperationEntry> Operations { get; set; }
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
            var operations = new List<OperationEntry>();
            for (var row = 0; row < table.RowCount; row++)
            {
                table.CurrentIndex = row;
                operations.Add(new OperationEntry
                {
                    InspOper = GetString(table, "INSPOPER"),
                    ShortText = GetStringSafe(table, "TXT_OPER")
                });
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

        private static string GetStringSafe(IRfcTable table, string field)
        {
            try { return (table.GetString(field) ?? string.Empty).Trim(); }
            catch { return string.Empty; }
        }

        private static string NormalizeInspectionLot(string lot)
        {
            var value = (lot ?? string.Empty).Trim();
            return value.Length >= 12 ? value : value.PadLeft(12, '0');
        }
    }
}
