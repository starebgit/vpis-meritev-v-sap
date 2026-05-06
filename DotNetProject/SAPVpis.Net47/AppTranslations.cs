using System;
using System.Collections.Generic;

namespace SAPVpis.Net47
{
    public enum AppLanguage { SLO = 0, EN = 1, DE = 2 }

    internal static class AppTranslations
    {
        private static AppLanguage _lang = AppLanguage.SLO;
        public static AppLanguage CurrentLanguage { get { return _lang; } }

        public static event EventHandler LanguageChanged;

        public static void SetLanguage(AppLanguage lang)
        {
            _lang = lang;
            var h = LanguageChanged;
            if (h != null) h(null, EventArgs.Empty);
        }

        public static string T(string key)
        {
            string[] arr;
            return _strings.TryGetValue(key, out arr) ? arr[(int)_lang] : key;
        }

        private static readonly Dictionary<string, string[]> _strings = new Dictionary<string, string[]>
        {
            // === VnosForm ===
            { "vnos_title",           new[] { "SAPVpis - Vpis meritev v SAP",       "SAPVpis - Record Measurements in SAP",      "SAPVpis - Messwerte in SAP erfassen" } },
            { "lbl_dept",             new[] { "Oddelek:",                             "Department:",                               "Abteilung:" } },
            { "lbl_code",             new[] { "Koda:",                                "Code:",                                     "Code:" } },
            { "lbl_date_from",        new[] { "Datum od:",                            "Date from:",                                "Datum ab:" } },
            { "lbl_shorttext",        new[] { "Kratki tekst:",                        "Short text:",                               "Kurztext:" } },
            { "btn_load_lots",        new[] { "Naloži šarže",                         "Load batches",                              "Chargen laden" } },
            { "lbl_item_name",        new[] { "Naziv izdelka:",                       "Material name:",                            "Materialname:" } },
            { "lbl_lots",             new[] { "Kontrolne šarže:",                     "Inspection lots:",                          "Prüflose:" } },
            { "btn_load_ops",         new[] { "Naloži operacije",                     "Load operations",                           "Vorgänge laden" } },
            { "lbl_ops",              new[] { "Operacije:",                           "Operations:",                               "Vorgänge:" } },
            { "btn_load_char",        new[] { "Naloži karakteristike",               "Load characteristics",                      "Merkmale laden" } },
            { "lbl_char",             new[] { "Karakteristike:",                      "Characteristics:",                          "Merkmale:" } },
            { "btn_enter_meas",       new[] { "Vnesi meritve",                        "Enter measurements",                        "Messwerte eingeben" } },
            { "btn_exit",             new[] { "Izhod",                               "Exit",                                      "Beenden" } },
            { "menu_view",            new[] { "Pregled",                              "View",                                      "Ansicht" } },
            { "menu_history",         new[] { "Zgodovina zapisov",                    "Record history",                            "Aufzeichnungsverlauf" } },
            { "menu_kontsarze",       new[] { "Kontrolne šarže",                      "Inspection lots",                           "Prüflose" } },
            { "menu_language",        new[] { "Jezik",                               "Language",                                  "Sprache" } },
            { "msg_no_lots",          new[] { "Nisem našel nobene kontrolne šarže",   "No inspection lots found",                  "Keine Prüflose gefunden" } },
            { "msg_no_lot_sel",       new[] { "Kontrolna šarža ni izbrana",           "Inspection lot not selected",               "Prüflos nicht ausgewählt" } },
            { "msg_lot_cancelled",    new[] { "Kontrolna šarža je stornirana",        "Inspection lot is cancelled",               "Prüflos ist storniert" } },
            { "msg_no_op_sel",        new[] { "Operacija ni izbrana",                 "Operation not selected",                    "Vorgang nicht ausgewählt" } },
            { "msg_no_char",          new[] { "Najprej naloži karakteristike",        "Load characteristics first",                "Merkmale zuerst laden" } },
            { "msg_no_meas",          new[] { "Ni izpolnjenih meritev za vpis.",      "No measurements to post.",                  "Keine Messwerte zum Erfassen." } },
            { "msg_sap_ok",           new[] { "SAP vpis uspešen:",                    "SAP post successful:",                      "SAP-Buchung erfolgreich:" } },
            { "msg_sap_err",          new[] { "SAP vpis NAPAKA:",                     "SAP post ERROR:",                           "SAP-Buchung FEHLER:" } },
            { "msg_post_disabled",    new[] {
                "Kontrolna točka: {0}\nIzpolnjenih vrstic: {1}/{2}\nSkupna ocena: {3}\n\n(sap.step10.enable_post=false — vpis v SAP ni aktiven)",
                "Control point: {0}\nFilled rows: {1}/{2}\nOverall score: {3}\n\n(sap.step10.enable_post=false — SAP post not active)",
                "Kontrollpunkt: {0}\nAusgefüllte Zeilen: {1}/{2}\nGesamtbewertung: {3}\n\n(sap.step10.enable_post=false — SAP-Buchung nicht aktiv)"
            } },
            { "msg_err",              new[] { "Napaka: ",                             "Error: ",                                   "Fehler: " } },
            { "msg_err_post",         new[] { "Napaka pri vpisu: ",                   "Error posting: ",                           "Fehler beim Erfassen: " } },
            { "msg_err_dept",         new[] { "Napaka pri nalaganju oddelkov: ",      "Error loading departments: ",               "Fehler beim Laden der Abteilungen: " } },
            { "msg_sql_vzr_err",      new[] { "SQL ZapisVzr napaka: ",               "SQL ZapisVzr error: ",                      "SQL ZapisVzr Fehler: " } },
            { "msg_sql_pren_err",     new[] { "SQL Prenos napaka: ",                 "SQL Prenos error: ",                        "SQL Prenos Fehler: " } },
            { "diag_select_lot",      new[] {
                "(Izberi šaržo in klikni Naloži operacije za ostale sekcije)",
                "(Select lot and click Load operations for other sections)",
                "(Los auswählen und Vorgänge laden klicken für weitere Abschnitte)"
            } },
            { "diag_select_op",       new[] {
                "(Izberi operacijo za diagnostiko CHAR_REQUIREMENTS)",
                "(Select operation for CHAR_REQUIREMENTS diagnostics)",
                "(Vorgang auswählen für CHAR_REQUIREMENTS-Diagnose)"
            } },

            // === VpisMerForm ===
            { "vpis_title",           new[] { "Vpis meritev",                         "Enter measurements",                        "Messwerte erfassen" } },
            { "lbl_ctrl_pt",          new[] { "Kontrolna točka:",                     "Control point:",                            "Kontrollpunkt:" } },
            { "btn_ok_vpis",          new[] { "Vpis",                                "Post",                                      "Erfassen" } },
            { "btn_cancel",           new[] { "Prekliči",                             "Cancel",                                    "Abbrechen" } },
            { "col_char",             new[] { "Karakteristika",                       "Characteristic",                            "Merkmal" } },
            { "col_meas_no",          new[] { "Št. mer.",                            "Meas. no.",                                 "Mess.-Nr." } },
            { "col_type",             new[] { "Meritev",                              "Type",                                      "Messung" } },
            { "col_individual",       new[] { "Posam.",                              "Indiv.",                                    "Einzel." } },
            { "col_result",           new[] { "Rezultat",                             "Result",                                    "Ergebnis" } },
            { "col_lower_tol",        new[] { "Sp. tol.",                            "Low. tol.",                                 "Unt. Tol." } },
            { "col_upper_tol",        new[] { "Zg. tol.",                            "Up. tol.",                                  "Ob. Tol." } },
            { "col_eval",             new[] { "A/R",                                 "A/R",                                       "A/R" } },
            { "col_comment",          new[] { "Opomba",                               "Comment",                                   "Bemerkung" } },
            { "msg_fill_ctrl_pt",     new[] { "Prosim vpiši kontrolno točko.",        "Please enter the control point.",            "Bitte Kontrollpunkt eingeben." } },

            // === SqlViewForm ===
            { "sql_title",            new[] { "SQL Pregled — SapVzr / SapChr",       "SQL View — SapVzr / SapChr",                "SQL-Ansicht — SapVzr / SapChr" } },
            { "lbl_vzr",              new[] { "SapVzr (vpisi meritev):",              "SapVzr (measurement entries):",             "SapVzr (Messeinträge):" } },
            { "lbl_chr_detail",       new[] { "SapChr (karakteristike za izbrano vrstico):", "SapChr (characteristics for selected row):", "SapChr (Merkmale für ausgewählte Zeile):" } },
            { "btn_refresh",          new[] { "Osveži",                               "Refresh",                                   "Aktualisieren" } },
            { "btn_send_sap",         new[] { "Pošlji v SAP",                         "Post to SAP",                               "An SAP senden" } },
            { "btn_delete_entry",     new[] { "Briši vpis",                           "Delete entry",                              "Eintrag löschen" } },
            { "btn_close",            new[] { "Zapri",                               "Close",                                     "Schließen" } },
            { "msg_sel_vzr_row",      new[] { "Izberi vrstico v SapVzr.",             "Select a row in SapVzr.",                   "Zeile in SapVzr auswählen." } },
            { "msg_already_sent",     new[] { "Ta vnos je že prenesen v SAP (PRENOS=X).", "This entry has already been sent to SAP (PRENOS=X).", "Dieser Eintrag wurde bereits an SAP übertragen (PRENOS=X)." } },
            { "msg_no_chr_rows",      new[] { "Ni vrstic v SapChr za ta vpis.",       "No rows in SapChr for this entry.",         "Keine Zeilen in SapChr für diesen Eintrag." } },
            { "msg_confirm_del",      new[] { "Ali zares želiš izbrisati?",           "Are you sure you want to delete?",          "Wirklich löschen?" } },
            { "msg_confirm_del_all",  new[] {
                "Ali zares želiš izbrisati VSE (SapVzr + SapChr) za ZAP=",
                "Are you sure you want to delete ALL (SapVzr + SapChr) for ZAP=",
                "Wirklich ALLES löschen (SapVzr + SapChr) für ZAP="
            } },
            { "confirm_title",        new[] { "Potrditev",                            "Confirm",                                   "Bestätigung" } },
            { "msg_send_ok",          new[] { "Uspešno poslano v SAP. ZAP=",         "Successfully sent to SAP. ZAP=",            "Erfolgreich an SAP gesendet. ZAP=" } },
            { "msg_sap_err_pfx",      new[] { "SAP napaka: ",                         "SAP error: ",                               "SAP-Fehler: " } },
            { "msg_select_row",       new[] { "Izberi vrstico.",                      "Select a row.",                             "Zeile auswählen." } },
            { "msg_read_vzr_err",     new[] { "Napaka pri branju SapVzr: ",          "Error reading SapVzr: ",                    "Fehler beim Lesen von SapVzr: " } },
            { "msg_read_chr_err",     new[] { "Napaka pri branju SapChr: ",          "Error reading SapChr: ",                    "Fehler beim Lesen von SapChr: " } },
            { "msg_del_err",          new[] { "Napaka brisanja: ",                    "Delete error: ",                            "Fehler beim Löschen: " } },
            { "msg_send_err_pfx",     new[] { "Napaka: ",                             "Error: ",                                   "Fehler: " } },

            // === SqlSarzeForm ===
            { "kontsarze_title",      new[] { "Kontrolne šarže — KontSarze",         "Inspection lots — KontSarze",                "Prüflose — KontSarze" } },
            { "btn_prepis",           new[] { "Prepis",                              "Export",                                    "Exportieren" } },
            { "btn_brisi",            new[] { "Briši",                                "Delete",                                    "Löschen" } },
            { "msg_no_data",          new[] { "Ni podatkov.",                         "No data.",                                  "Keine Daten." } },
            { "msg_select_to_delete", new[] { "Izberi vrstico za brisanje.",          "Select a row to delete.",                   "Zeile zum Löschen auswählen." } },
            { "msg_export_ok",        new[] { "Izvoz uspešen v: {0}",                 "Export successful to: {0}",                  "Export erfolgreich nach: {0}" } },
            { "msg_export_err",       new[] { "Napaka pri izvozu: ",                 "Export error: ",                            "Exportfehler: " } },
            { "msg_delete_ok",        new[] { "Izbrisanih vrstic: {0}",              "Rows deleted: {0}",                          "Gelöschte Zeilen: {0}" } },
            { "msg_kontsarze_load_err", new[] { "Napaka pri branju KontSarze: ",     "Error reading KontSarze: ",                  "Fehler beim Lesen von KontSarze: " } },

            // === Tooltips ===
            { "tip_load_lots",        new[] { "Poišči kontrolne šarže po kodi, datumu ali kratkem tekstu", "Find inspection lots by code, date, or short text", "Prüflose nach Code, Datum oder Kurztext suchen" } },
            { "tip_load_ops",         new[] { "Naloži operacije za izbrano kontrolno šaržo",               "Load operations for the selected inspection lot",   "Vorgänge für ausgewähltes Prüflos laden" } },
            { "tip_load_char",        new[] { "Naloži karakteristike za izbrano operacijo",                "Load characteristics for the selected operation",   "Merkmale für ausgewählten Vorgang laden" } },
            { "tip_enter_meas",       new[] { "Odpri okno za vnos meritev",                                "Open the measurement entry dialog",                 "Messwerteingabe öffnen" } },
            { "tip_exit",             new[] { "Zapri program",                                             "Close the application",                             "Anwendung schließen" } },
            { "tip_ok_vpis",          new[] { "Potrdi in pošlji meritve v SAP",                             "Confirm and post measurements to SAP",              "Bestätigen und Messwerte an SAP senden" } },
            { "tip_cancel",           new[] { "Prekliči brez shranjevanja",                                "Cancel without saving",                             "Abbrechen ohne Speichern" } },
            { "tip_refresh",          new[] { "Ponovno preberi podatke",                                   "Reload data",                                       "Daten neu laden" } },
            { "tip_send_sap",         new[] { "Ponovno pošlji izbran vpis v SAP",                          "Re-send the selected entry to SAP",                 "Ausgewählten Eintrag erneut an SAP senden" } },
            { "tip_delete_entry",     new[] { "Briši izbran vpis in vse pripadajoče karakteristike",       "Delete the selected entry and all its characteristics", "Ausgewählten Eintrag und alle Merkmale löschen" } },
            { "tip_close",            new[] { "Zapri okno",                                                "Close window",                                      "Fenster schließen" } },
            { "tip_prepis",           new[] { "Izvozi vse vrstice v sarze.txt v mapo aplikacije",          "Export all rows to sarze.txt in the app folder",    "Alle Zeilen in sarze.txt im Anwendungsordner exportieren" } },
            { "tip_brisi",            new[] { "Briši izbrano vrstico iz tabele KontSarze",                "Delete the selected row from the KontSarze table",  "Ausgewählte Zeile aus der Tabelle KontSarze löschen" } },
        };
    }
}
