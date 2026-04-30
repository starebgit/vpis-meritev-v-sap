using System;
using System.Collections.Generic;
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

        public PostResult PostSingleResult(RfcDestination destination, string lotNumber, string inspOper, string inspChar, string resultValue, string mode)
        {
            var lot = NormalizeInspectionLot(lotNumber);
            var op = (inspOper ?? string.Empty).Trim();
            var chr = (inspChar ?? string.Empty).Trim();
            var val = (resultValue ?? string.Empty).Trim();
            var handheld = TryResolveHandheldApplication(destination, lot, op);
            var normalizedMode = (mode ?? string.Empty).Trim().ToUpperInvariant();

            var preferCharCloseOnly = normalizedMode == "DELPHI" || normalizedMode == "CHAR_CLOSE";
            var firstAttempt = ExecuteRecordResults(destination, lot, op, chr, val, handheld, includeSingleResult: !preferCharCloseOnly);
            if (!firstAttempt.HasError)
            {
                Commit(destination);
                return new PostResult
                {
                    Success = true, Message = "RecordResults+Commit success. requestedMode=" + EmptyAsToken(normalizedMode) + " | " + firstAttempt.Message
                };
            }

            if (ContainsResultsCannotBeEntered(firstAttempt.FirstMessage) || normalizedMode == "AUTO")
            {
                var retryAttempt = ExecuteRecordResults(destination, lot, op, chr, val, handheld, includeSingleResult: preferCharCloseOnly);
                if (!retryAttempt.HasError)
                {
                    Commit(destination);
                    return new PostResult
                    {
                        Success = true, Message = "RecordResults+Commit success (retry). requestedMode=" + EmptyAsToken(normalizedMode) + " | firstAttempt=" + firstAttempt.FirstMessage + " | retry=" + retryAttempt.Message
                    };
                }

                return new PostResult
                {
                    Success = false,
                    Message = "RecordResults failed in both modes. firstAttempt=" + firstAttempt.Message + " | retryAttempt=" + retryAttempt.Message
                };
            }

            return new PostResult { Success = false, Message = "RecordResults failed: " + firstAttempt.Message };
        }

        private static AttemptResult ExecuteRecordResults(RfcDestination destination, string lot, string op, string chr, string val, string handheld, bool includeSingleResult)
        {
            var f = destination.Repository.CreateFunction("BAPI_INSPOPER_RECORDRESULTS");
            f.SetValue("INSPLOT", lot);
            f.SetValue("INSPOPER", op);

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

            if (includeSingleResult)
            {
                var single = f.GetTable("SINGLE_RESULTS");
                single.Append();
                single.SetValue("INSPLOT", lot);
                single.SetValue("INSPOPER", op);
                single.SetValue("INSPCHAR", chr);
                single.SetValue("INSPSAMPLE", "000001");
                single.SetValue("RES_NO", "0001");
                single.SetValue("RES_ATTR", "X");
                single.SetValue("RES_VALUE", val);
            }

            f.Invoke(destination);

            var ret = f.GetTable("RETURNTABLE");
            var hasError = false;
            var firstMsg = "";
            var returnRows = new List<string>();
            for (var i = 0; i < ret.RowCount; i++)
            {
                ret.CurrentIndex = i;
                var type = GetString(ret, "TYPE");
                var num = GetString(ret, "NUMBER");
                var msg = GetString(ret, "MESSAGE");
                returnRows.Add(type + "/" + num + " " + msg);
                if (i == 0) firstMsg = type + "/" + num + " " + msg;
                if (type == "E" || type == "A") hasError = true;
            }

            var debugContext = string.Format(
                "lot={0}, op={1}, char={2}, value={3}, handheld={4}, returnRows={5}",
                lot,
                EmptyAsToken(op),
                EmptyAsToken(chr),
                EmptyAsToken(val),
                EmptyAsToken(handheld),
                returnRows.Count);
            var debugReturns = returnRows.Count == 0 ? "<none>" : string.Join(" || ", returnRows.ToArray());

            return new AttemptResult
            {
                HasError = hasError,
                FirstMessage = firstMsg,
                Message = string.Format("mode={0} | first={1} | {2} | returns={3}", includeSingleResult ? "single-result" : "char-close-only", firstMsg, debugContext, debugReturns)
            };
        }
        
        private static void Commit(RfcDestination destination)
        {
            var commit = destination.Repository.CreateFunction("BAPI_TRANSACTION_COMMIT");
            commit.SetValue("WAIT", "X");
            commit.Invoke(destination);
        }

        private static bool ContainsResultsCannotBeEntered(string message)
        {
            var m = (message ?? string.Empty).ToLowerInvariant();
            return m.Contains("rezultati ne morejo biti vnešeni")
                   || m.Contains("results cannot be entered");
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

        private static string EmptyAsToken(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<empty>" : value;
        }

        private sealed class AttemptResult
        {
            public bool HasError { get; set; }
            public string FirstMessage { get; set; }
            public string Message { get; set; }
        }
    }
}
