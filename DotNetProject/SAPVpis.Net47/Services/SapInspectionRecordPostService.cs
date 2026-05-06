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

        // Delphi: pv (karak record) in Zapis karlist loop — one entry per filled measurement row
        public sealed class CharRow
        {
            public string InspChar { get; set; }
            public string IsIndividual { get; set; }   // 'X' = individual, '' = sample
            public string CharType { get; set; }        // '01' = numeric, '02' = qualitative
            public string ResultValue { get; set; }
            public string Valuation { get; set; }       // 'A' or 'R'
            public string Remark { get; set; }
            public int SequentialIndex { get; set; }    // 1-based kk in Delphi Zapis loop
        }

        // Delphi: Zapis — posts all filled measurement rows in one BAPI call
        public PostResult PostAllResults(
            RfcDestination destination,
            string lotNumber, string inspOper,
            string rmk, string plant,
            DateTime postingDateTime,
            string overallValuation,
            List<CharRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return new PostResult { Success = false, Message = "No rows to post." };

            var lot = NormalizeInspectionLot(lotNumber);
            var op = (inspOper ?? string.Empty).Trim();
            var odl = (overallValuation ?? "A").Trim();

            var f = destination.Repository.CreateFunction("BAPI_INSPOPER_RECORDRESULTS");
            f.SetValue("INSPLOT", lot);
            f.SetValue("INSPOPER", op);

            // INSPPOINTDATA field names confirmed via BAPI Diagnostika (2026-05-05):
            // pos1=INSPLOT, pos2=INSPOPER, pos12=USERC1 (rmk/kontrolna točka),
            // pos16=USERD1 (date, required), pos17=USERT1 (time, required),
            // pos18=CAT_TYPE ('3'), pos19=PSEL_SET (plant), pos20=SEL_SET,
            // pos21=CODE_GRP, pos22=CODE (overall A/R)
            TrySetStructure(f, "INSPPOINTDATA", "INSPLOT", lot);
            TrySetStructure(f, "INSPPOINTDATA", "INSPOPER", op);
            TrySetStructure(f, "INSPPOINTDATA", "USERC1", rmk);
            TrySetStructure(f, "INSPPOINTDATA", "USERD1", postingDateTime.ToString("yyyyMMdd"));
            TrySetStructure(f, "INSPPOINTDATA", "USERT1", postingDateTime.ToString("HHmmss"));
            TrySetStructure(f, "INSPPOINTDATA", "CAT_TYPE", "3");
            TrySetStructure(f, "INSPPOINTDATA", "PSEL_SET", plant);
            TrySetStructure(f, "INSPPOINTDATA", "SEL_SET", "A/R");
            TrySetStructure(f, "INSPPOINTDATA", "CODE_GRP", "A/R");
            TrySetStructure(f, "INSPPOINTDATA", "CODE", odl);

            foreach (var row in rows)
            {
                var kk = row.SequentialIndex;
                var inspChar = (row.InspChar ?? string.Empty).Trim();
                var isIndividual = (row.IsIndividual ?? string.Empty).Trim();
                var charType = (row.CharType ?? string.Empty).Trim();
                var resultValue = (row.ResultValue ?? string.Empty).Trim();
                var valuation = (row.Valuation ?? string.Empty).Trim();
                var remark = (row.Remark ?? string.Empty).Trim();

                var charResults = f.GetTable("CHAR_RESULTS");
                charResults.Append();
                charResults.SetValue("INSPLOT", lot);
                charResults.SetValue("INSPOPER", op);
                charResults.SetValue("INSPCHAR", inspChar);
                charResults.SetValue("CLOSED", "X");
                charResults.SetValue("EVALUATED", "X");
                charResults.SetValue("REMARK", remark);             // col 25 confirmed

                if (isIndividual == "X")
                {
                    var single = f.GetTable("SINGLE_RESULTS");
                    single.Append();
                    single.SetValue("INSPLOT", lot);
                    single.SetValue("INSPOPER", op);
                    single.SetValue("INSPCHAR", inspChar);
                    single.SetValue("INSPSAMPLE", "000001");        // Delphi pos4 = 1
                    single.SetValue("RES_NO", kk.ToString("D4"));   // Delphi pos5 = kk (sequential)
                    single.SetValue("RES_ATTR", "X");                // Delphi pos7 = 'X' (col 11, not 7 — confirmed LAST_RES vs RES_ATTR)
                    if (charType == "02")
                        single.SetValue("RES_VALUAT", valuation);   // col 13 = RES_VALUAT (qualitative)
                    else
                        single.SetValue("RES_VALUE", resultValue);  // col 10 = RES_VALUE (numeric)
                }
                else
                {
                    // Sample result: CHAR_RESULTS gets EVALUATION + MEAN_VALUE
                    charResults.SetValue("EVALUATION", valuation);   // col 8 = EVALUATION (not VALUATION)
                    charResults.SetValue("MEAN_VALUE", resultValue); // col 15 = MEAN_VALUE

                    var sample = f.GetTable("SAMPLE_RESULTS");
                    sample.Append();
                    sample.SetValue("INSPLOT", lot);
                    sample.SetValue("INSPOPER", op);
                    sample.SetValue("INSPCHAR", inspChar);
                    sample.SetValue("INSPSAMPLE", kk.ToString());   // Delphi pos4 = kk
                    sample.SetValue("CLOSED", "X");                  // col 6 = CLOSED
                    sample.SetValue("EVALUATED", "X");               // col 7 = EVALUATED
                    if (charType == "02")
                        sample.SetValue("EVALUATION", valuation);   // col 10 = EVALUATION
                    else
                        sample.SetValue("MEAN_VALUE", resultValue); // col 17 = MEAN_VALUE
                }
            }

            f.Invoke(destination);

            var msgs = new List<string>();
            var hasError = false;
            var retTable = f.GetTable("RETURNTABLE");
            for (var i = 0; i < retTable.RowCount; i++)
            {
                retTable.CurrentIndex = i;
                var t = GetString(retTable, "TYPE");
                var n = GetString(retTable, "NUMBER");
                var m = GetString(retTable, "MESSAGE");
                msgs.Add(t + "/" + n + " " + m);
                if (t == "E" || t == "A") hasError = true;
            }
            var returnSummary = msgs.Count == 0 ? "<none>" : string.Join(" || ", msgs.ToArray());

            if (hasError)
                return new PostResult { Success = false, Message = "RECORDRESULTS error: " + returnSummary };

            Commit(destination);
            return new PostResult { Success = true, Message = "Posted " + rows.Count + " rows. Returns: " + returnSummary };
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

        private static void TrySetStructure(IRfcFunction f, string structName, string field, string value)
        {
            try { f.GetStructure(structName).SetValue(field, value); }
            catch { }
        }

        private static void TrySet(IRfcTable table, string field, string value)
        {
            try { table.SetValue(field, value); }
            catch { }
        }

        private sealed class AttemptResult
        {
            public bool HasError { get; set; }
            public string FirstMessage { get; set; }
            public string Message { get; set; }
        }
    }
}
