using System;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapInspectionRecordPostService
    {
        internal sealed class PostResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        public PostResult PostSingleResult(RfcDestination destination, string lotNumber, string inspOper, string inspChar, string resultValue)
        {
            var lot = NormalizeInspectionLot(lotNumber);
            var op = (inspOper ?? string.Empty).Trim();
            var chr = (inspChar ?? string.Empty).Trim();
            var val = (resultValue ?? string.Empty).Trim();

            var f = destination.Repository.CreateFunction("BAPI_INSPOPER_RECORDRESULTS");
            f.SetValue("INSPLOT", lot);
            f.SetValue("INSPOPER", op);

            var handheld = TryResolveHandheldApplication(destination, lot, op);
            if (!string.IsNullOrWhiteSpace(handheld))
            {
                f.SetValue("HANDHELD_APPLICATION", handheld);
            }

            var charResults = f.GetTable("CHAR_RESULTS");
            charResults.Append();
            charResults.SetValue("INSPLOT", lot);
            charResults.SetValue("INSPOPER", op);
            charResults.SetValue("INSPCHAR", chr);
            charResults.SetValue("CLOSED", "X");
            charResults.SetValue("EVALUATED", "X");

            var single = f.GetTable("SINGLE_RESULTS");
            single.Append();
            single.SetValue("INSPLOT", lot);
            single.SetValue("INSPOPER", op);
            single.SetValue("INSPCHAR", chr);
            single.SetValue("INSPSAMPLE", "000001");
            single.SetValue("RES_NO", "0001");
            single.SetValue("RES_ATTR", "X");
            single.SetValue("RES_VALUE", val);

            f.Invoke(destination);

            var ret = f.GetTable("RETURNTABLE");
            var hasError = false;
            var firstMsg = "";
            for (var i = 0; i < ret.RowCount; i++)
            {
                ret.CurrentIndex = i;
                var type = GetString(ret, "TYPE");
                var num = GetString(ret, "NUMBER");
                var msg = GetString(ret, "MESSAGE");
                if (i == 0) firstMsg = type + "/" + num + " " + msg;
                if (type == "E" || type == "A") hasError = true;
            }

            if (hasError)
            {
                return new PostResult { Success = false, Message = "RecordResults failed: " + firstMsg + " | handheld=" + (handheld == string.Empty ? "<empty>" : handheld) };
            }

            var commit = destination.Repository.CreateFunction("BAPI_TRANSACTION_COMMIT");
            commit.SetValue("WAIT", "X");
            commit.Invoke(destination);

            return new PostResult { Success = true, Message = "RecordResults+Commit success. First return: " + firstMsg + " | handheld=" + (handheld == string.Empty ? "<empty>" : handheld) };
        }

        private static string TryResolveHandheldApplication(RfcDestination destination, string lot, string op)
        {
            try
            {
                var detail = destination.Repository.CreateFunction("BAPI_INSPOPER_GETDETAIL");
                detail.SetValue("INSPLOT", lot);
                detail.SetValue("INSPOPER", op);
                detail.SetValue("READ_INSPPOINTS", "X");
                detail.Invoke(destination);

                var points = detail.GetTable("INSPPOINTS");
                if (points.RowCount == 0)
                {
                    return string.Empty;
                }

                points.CurrentIndex = 0;
                return GetStringSafe(points,
                    "HANDHELD_APPLICATION",
                    "HANDHELD_APPL",
                    "HANDHELDAPP",
                    "APP_ID");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetString(IRfcTable table, string field)
        {
            return (table.GetString(field) ?? string.Empty).Trim();
        }

        private static string GetStringSafe(IRfcTable table, params string[] fields)
        {
            foreach (var field in fields)
            {
                try
                {
                    var value = GetString(table, field);
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

        private static string NormalizeInspectionLot(string lot)
        {
            var value = (lot ?? string.Empty).Trim();
            return value.Length >= 12 ? value : value.PadLeft(12, '0');
        }
    }
}
