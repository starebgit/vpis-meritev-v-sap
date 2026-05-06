using System.Collections.Generic;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    // Wraps BAPI_INSPOPER_GETDETAIL READ_CHAR_REQUIREMENTS.
    // Mirrors Delphi Vnos.Button3Click + the karako record fields used in Button4Click.
    internal sealed class SapCharacteristicService
    {
        internal sealed class CharacteristicEntry
        {
            // Delphi CHAR_REQUIREMENTS column → confirmed SAP NCo field name:
            public string InspChar { get; set; }      // col 3  INSPCHAR
            public string Name { get; set; }           // col 5  CHAR_DESCR
            public string ControlMethod { get; set; } // col 6  CHAR_TYPE ('01'=numeric, '02'=qualitative)
            public string IsIndividual { get; set; }  // col 9  SINGLE_RES ('X'=individual result)
            public string SampleRes { get; set; }     // col 10 SAMPLE_RES ('X'=sample result)
            public int SampleSize { get; set; }       // col 14 SCOPE (used when SINGLE_RES='X')
            public string UpperTol { get; set; }      // col 44 UP_TOL_LMT
            public string LowerTol { get; set; }      // col 45 LW_TOL_LMT
            public int RowIndex { get; set; }         // 1-based (stkar in Delphi)

            // Number of measurement entries needed: Delphi Button4Click logic
            public int MeasurementCount
            {
                get { return IsIndividual == "X" ? SampleSize : 1; }
            }
        }

        public List<CharacteristicEntry> GetCharacteristics(RfcDestination destination, string lotNumber, string inspOper)
        {
            var lot = (lotNumber ?? string.Empty).Trim();
            if (lot.Length < 12) lot = lot.PadLeft(12, '0');
            var op = (inspOper ?? string.Empty).Trim();

            var f = destination.Repository.CreateFunction("BAPI_INSPOPER_GETDETAIL");
            f.SetValue("INSPLOT", lot);
            f.SetValue("INSPOPER", op);
            f.SetValue("READ_INSPPOINTS", "X");
            f.SetValue("READ_CHAR_REQUIREMENTS", "X");
            f.Invoke(destination);

            var table = f.GetTable("CHAR_REQUIREMENTS");
            var result = new List<CharacteristicEntry>();
            for (var i = 0; i < table.RowCount; i++)
            {
                table.CurrentIndex = i;
                var scopeStr = GetStringSafe(table, "SCOPE");
                int sampleSize;
                if (!int.TryParse(scopeStr, out sampleSize) || sampleSize == 0)
                    sampleSize = 1;

                result.Add(new CharacteristicEntry
                {
                    InspChar = GetStringSafe(table, "INSPCHAR"),
                    Name = GetStringSafe(table, "CHAR_DESCR"),
                    ControlMethod = GetStringSafe(table, "CHAR_TYPE"),
                    IsIndividual = GetStringSafe(table, "SINGLE_RES"),
                    SampleRes = GetStringSafe(table, "SAMPLE_RES"),
                    SampleSize = sampleSize,
                    UpperTol = GetStringSafe(table, "UP_TOL_LMT"),
                    LowerTol = GetStringSafe(table, "LW_TOL_LMT"),
                    RowIndex = i + 1
                });
            }
            return result;
        }

        private static string GetStringSafe(IRfcTable table, string field)
        {
            try { return (table.GetString(field) ?? string.Empty).Trim(); }
            catch { return string.Empty; }
        }
    }
}
