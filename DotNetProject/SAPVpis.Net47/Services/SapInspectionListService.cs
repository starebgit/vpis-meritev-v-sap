using System;
using System.Collections.Generic;
using SAP.Middleware.Connector;

namespace SAPVpis.Net47.Services
{
    internal sealed class SapInspectionListService
    {
        internal sealed class InspectionLotEntry
        {
            public string LotNumber { get; set; }
            public string ShortText { get; set; }
            public string Material { get; set; }
            public string TxtMat { get; set; }   // TXT_MAT — material name
        }

        // Delphi: Button1Click nc=1 path — search by material + plant + date
        public List<InspectionLotEntry> GetByMaterial(RfcDestination destination, string material, string plant, DateTime createdFrom)
        {
            var f = destination.Repository.CreateFunction("BAPI_INSPLOT_GETLIST");
            f.SetValue("MATERIAL", FormatMaterial(material));
            f.SetValue("PLANT", plant);
            f.SetValue("CREAT_DAT", createdFrom.Date);
            f.SetValue("STATUS_UD", string.Empty);
            f.Invoke(destination);
            return ReadTable(f);
        }

        // Delphi: Button1Click nc=2 path — full plant list; caller filters by short-text prefix
        public List<InspectionLotEntry> GetByPlant(RfcDestination destination, string plant, DateTime createdFrom, int maxRows = 19000)
        {
            var f = destination.Repository.CreateFunction("BAPI_INSPLOT_GETLIST");
            f.SetValue("PLANT", plant);
            f.SetValue("CREAT_DAT", createdFrom.Date);
            f.SetValue("MAX_ROWS", maxRows);
            f.SetValue("STATUS_UD", string.Empty);
            f.Invoke(destination);
            return ReadTable(f);
        }

        private static List<InspectionLotEntry> ReadTable(IRfcFunction f)
        {
            var table = f.GetTable("INSPLOT_LIST");
            var result = new List<InspectionLotEntry>();
            for (var i = 0; i < table.RowCount; i++)
            {
                table.CurrentIndex = i;
                result.Add(new InspectionLotEntry
                {
                    LotNumber = GetStringSafe(table, "INSPLOT"),
                    Material = GetStringSafe(table, "MATERIAL"),
                    ShortText = GetStringSafe(table, "TXT_LOT"),
                    TxtMat = GetStringSafe(table, "TXT_MAT")
                });
            }
            return result;
        }

        // Numeric materials are zero-padded to 18; alpha materials are space-padded right
        private static string FormatMaterial(string raw)
        {
            var v = (raw ?? string.Empty).Trim();
            if (v.Length == 0) return v;
            long n;
            return long.TryParse(v, out n) ? v.PadLeft(18, '0') : v.PadRight(18);
        }

        private static string GetStringSafe(IRfcTable table, string field)
        {
            try { return (table.GetString(field) ?? string.Empty).Trim(); }
            catch { return string.Empty; }
        }
    }
}
