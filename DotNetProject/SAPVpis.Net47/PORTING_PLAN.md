# Delphi → .NET Porting Plan

## Goal
Strict Delphi parity port of SAPVpis to .NET Framework 4.7 WinForms. App posts QM measurement results into SAP via BAPIs and stages them locally first.

## Current status summary
- ✅ Steps 1–12 complete and verified live
- ✅ Multi-language UI (SLO / EN / DE) live-switchable
- ✅ Custom SAP-themed application icon

---

## Step 1 — SAP connectivity baseline ✅
Destination registration + trial runner. `destination.Ping()` + smoke RFC pass.

## Step 2 — Delphi-style SAP login source ✅
Login values read from SQL `prijava` (default row `glavni='X'`). No appSetting per-field credentials.

## Step 3 — Connection string compatibility ✅
Accepts Delphi/OLEDB-style strings (`Provider=SQLOLEDB.1;...`) and normalizes for SqlClient.

## Step 4 — Plant rule parity ✅
`rsg_rfc_1 → 0401`, otherwise `1061`. Resolved plant visible in trial output.

## Step 5 — Read-only business RFC parity ✅
- `BAPI_INSPLOT_GETDETAIL` (lot status / open-state scoring incl. I0224 storno)
- `BAPI_INSPLOT_GETOPERATIONS` (operation list)
- `BAPI_INSPOPER_GETDETAIL` (CHAR_REQUIREMENTS)

## Step 6 — Write RFC parity ✅
`BAPI_INSPOPER_RECORDRESULTS` + `BAPI_TRANSACTION_COMMIT`. INSPPOINTDATA fields confirmed via metadata diagnostics: `USERC1=rmk`, `USERD1=date`, `USERT1=time`, `CAT_TYPE='3'`, `PSEL_SET=plant`, `SEL_SET='A/R'`, `CODE_GRP='A/R'`, `CODE=overall`. CHAR_RESULTS / SINGLE_RESULTS / SAMPLE_RESULTS posted with Delphi-equivalent CHAR_CLOSE behavior.

## Step 7 — Local trial logging ✅
Optional `dotnet_sap_trial_log` table writer (gated by `sap.step7.enable_local_log`).

## Step 8 — Record posting service ✅
`SapInspectionRecordPostService` — single-result vs char-close-only modes, retry logic, handheld app resolution, `BAPI_TRANSACTION_COMMIT`.

## Step 9 — SQL schema & local logging infrastructure ✅
Auto-creates `dotnet_sap_trial_log` on first write.

## Step 10 — Live SAP posting from VpisMerForm ✅
OK button posts `BAPI_INSPOPER_RECORDRESULTS` + commit. Empty RETURNTABLE = clean commit. Gated by `sap.step10.enable_post`.

## Step 11 — Local SQL staging (SapVzr / SapChr) ✅
`SapMeasurementSqlRepository` — `ZapisVzr` (INSERT, returns ZAP), `VpisChr` (INSERT detail rows), `Prenos` (UPDATE `PRENOS='X'`). Delphi parity: SQL writes happen before SAP post; `Prenos` only after SAP success. Gated by `sap.step11.enable_sql_write`.

## Step 12 — UI parity for Delphi auxiliary forms ✅
- **Pregled → Zgodovina zapisov** (`SqlViewForm`): master-detail SapVzr / SapChr view. "Pošlji v SAP" re-sends unsent rows (PRENOS=''). "Briši vpis" deletes the SapVzr row + all its SapChr rows.
- **Pregled → Kontrolne šarže** (`SqlSarzeForm`): full KontSarze grid. **Prepis** exports all rows to `sarze.txt` (`sarza,oddelek,tekst,ktocka` per line — matches Delphi `Writeln` format). **Briši** deletes selected row by `SARZA + ODDELEK` after confirmation.

## Polish — UI language + branding ✅
- `AppTranslations` static class with SLO/EN/DE dictionary + `LanguageChanged` event
- **Jezik** menu (SLO / EN / DE) with checked-state radio behavior; live-switches all VnosForm labels/buttons/menu items
- Modal forms (VpisMerForm, SqlViewForm, SqlSarzeForm) read translations at construction
- Programmatic 32×32 SAP-themed icon: blue background, white "SAP" text, gold accent bar
- BAPI Diagnostika button hidden (kept in code as a developer tool — set `Visible = true` in code if needed)
