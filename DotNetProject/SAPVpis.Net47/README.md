# SAPVpis.Net47

Strict Delphi → .NET port of the QM measurement-entry app. Operators select an inspection lot + operation, enter measured values + A/R evaluation per characteristic, then submit them to SAP QM. Each submission is also staged in a local SQL Server database so unsubmitted entries can be retried if SAP is unavailable.

- **UI:** WinForms, .NET Framework 4.7
- **SAP:** SAP .NET Connector 3 (NCo) — `sapnco.dll` + `sapnco_utils.dll` resolved from `..\dll\` (or `..\dll\32\` fallback)
- **DB:** SQL Server via `System.Data.SqlClient`; `Provider=SQLOLEDB.1;...` connection strings are accepted and auto-normalized

---

## 1. Where the app connects

### 1.1 SAP system
- Destination is built at runtime from credentials read out of SQL Server (table `prijava`). There is **no separate SAP login dialog** — login is implicit on first SAP call.
- Plant code follows the Delphi rule: SAP user `rsg_rfc_1` → plant `0401`, anyone else → plant `1061`.
- Destination name (default `SAP_DEFAULT`) and pool tuning come from `App.config`.

### 1.2 SQL Server
- One connection string is used for **everything**: `App.config` key `sap.login.db.connection_string`.
- Default in source: `Data Source=172.20.1.14`, `Initial Catalog=SAPkontrola`, `User ID=sap`, `Password=sap`.
- Both legacy OLEDB-style strings (`Provider=SQLOLEDB.1;Data Source=...`) and modern SqlClient strings are accepted; the OLEDB form is normalized to SqlClient before each query.

---

## 2. SAP read/write surface (BAPIs and RFCs)

| BAPI / RFC                       | Direction | When                                                                                                | Notes |
| -------------------------------- | --------- | --------------------------------------------------------------------------------------------------- | ----- |
| `RFC_SYSTEM_INFO`                | read      | Optional smoke test (`Run SAP Trial` button on `MainForm`)                                          | Configurable via `sap.smoke.rfc` |
| `BAPI_INSPLOT_GETLIST`           | read      | "Naloži šarže" — searches inspection lots by material + plant + date, or by plant + short-text filter | Material is zero-padded to 18 if numeric, space-padded if alpha |
| `BAPI_INSPLOT_GETDETAIL`         | read      | "Naloži operacije" — pre-flight storno check on selected lot                                         | Status `I0224` blocks; `I0205`/`I0002`/`I0206` score "open"; `I0216` scores closed |
| `BAPI_INSPLOT_GETOPERATIONS`     | read      | "Naloži operacije" — list of operations for the selected lot                                         | Reads `INSPOPER`, `TXT_OPER` |
| `BAPI_INSPOPER_GETDETAIL`        | read      | "Naloži karakteristike" — list of characteristics for the selected operation                         | Uses `READ_CHAR_REQUIREMENTS=X` and `READ_INSPPOINTS=X` |
| `BAPI_INSPOPER_RECORDRESULTS`    | **write** | "Vpis" (in measurement-entry dialog) — one call posts all filled measurements at once               | Fills `INSPPOINTDATA`, `CHAR_RESULTS`, `SINGLE_RESULTS` (individual) or `SAMPLE_RESULTS` (sample) |
| `BAPI_TRANSACTION_COMMIT`        | **write** | Immediately after a successful `RECORDRESULTS`                                                       | `WAIT=X` (synchronous) |

### 2.1 INSPPOINTDATA fields written
Confirmed by metadata diagnostics against the live system:

| Field      | Value                                  |
| ---------- | -------------------------------------- |
| `INSPLOT`  | inspection lot number                  |
| `INSPOPER` | operation number                       |
| `USERC1`   | "kontrolna točka" / remark             |
| `USERD1`   | posting date  (`yyyyMMdd`, **required**) |
| `USERT1`   | posting time  (`HHmmss`,  **required**) |
| `CAT_TYPE` | `'3'`                                  |
| `PSEL_SET` | plant code                             |
| `SEL_SET`  | `'A/R'`                                |
| `CODE_GRP` | `'A/R'`                                |
| `CODE`     | overall valuation `A` or `R`           |

---

## 3. SQL Server read/write surface

Database: **`SAPkontrola`** on **`172.20.1.14`** (per default config).

| Table       | Read by                                                       | Written by                                                                | Purpose |
| ----------- | ------------------------------------------------------------- | ------------------------------------------------------------------------- | ------- |
| `prijava`   | `SapLoginRepository.GetDefaultLogin` (every SAP call indirectly) | _never_                                                                  | SAP credentials. App reads the row where `glavni='X'` |
| `KontSarze` | Department list (left panel), lot catalog by department, full grid in **Pregled → Kontrolne šarže** | `SqlSarzeForm` → "Briši" deletes one row by `SARZA + ODDELEK`            | Local catalog of inspection-lot batches per department |
| `SapVzr`    | **Pregled → Zgodovina zapisov** master grid                    | Inserted on every "Vpis" (header row); `PRENOS='X'` set after successful SAP post; deleted by "Briši vpis" | Local staging header per submission |
| `SapChr`    | **Pregled → Zgodovina zapisov** detail grid (filtered by selected `ZAP`) | Inserted on every "Vpis" (one row per filled measurement); cascade-deleted with header | Local staging detail per characteristic |

### 3.1 Table schemas (as used by the app)

**`prijava`** (login source — read-only)

| Column     | Type   | Used as                              |
| ---------- | ------ | ------------------------------------ |
| `glavni`   | char   | Filter: `WHERE glavni='X'` picks the row |
| `uporab`   | string | SAP user                             |
| `sistem`   | string | SAP system ID                        |
| `client`   | string | SAP client                           |
| `streznik` | string | Application server host              |
| `sysnnum`  | string | System number                        |
| `pass`     | string | SAP password                         |
| `jezik`    | string | SAP logon language                   |

**`KontSarze`** (lot catalog — read & delete)

| Column    | Description                              |
| --------- | ---------------------------------------- |
| `sarza`   | Inspection lot / batch (12-char NUMC-style) |
| `oddelek` | Department code (drives left-panel list) |
| `tekst`   | Free-text description                    |
| `ktocka`  | Default control point ("kontrolna točka") |

**`SapVzr`** (submission header — read, insert, update, delete)

| Column      | Description                                            |
| ----------- | ------------------------------------------------------ |
| `ZAP`       | Identity PK (returned by `SCOPE_IDENTITY()` after insert) |
| `INSPLOT`   | SAP inspection lot                                     |
| `INSPOPER`  | SAP operation number                                   |
| `NAZIVKK`   | Control point / remark (USERC1)                        |
| `DATUM`     | Posting timestamp                                      |
| `UPORAB`    | Local machine name (`Environment.MachineName`)         |
| `PRENOS`    | `''` until SAP post succeeds, then `'X'`               |

**`SapChr`** (submission detail — read, insert, delete)

| Column    | Description                                          |
| --------- | ---------------------------------------------------- |
| `ZPKAR`   | Identity PK                                          |
| `ZAP`     | FK to `SapVzr.ZAP`                                   |
| `STKAR`   | SAP `INSPCHAR` code                                  |
| `STMER`   | Sequential measurement number (kk)                   |
| `SKUPNI`  | `'X'` = individual result, `''` = sample result      |
| `TIP`     | `'01'` = numeric, `'02'` = qualitative               |
| `MERITEV` | Measured value                                       |
| `EVAL`    | `'A'` (accept) / `'R'` (reject)                      |
| `OPOMBA`  | Per-row remark                                       |

---

## 4. End-to-end workflow

```
Open app
  → SapKontSarzeRepository.GetDepartments()   ── SQL: KontSarze (DISTINCT ODDELEK)
  → User picks department
  → SapKontSarzeRepository.GetSarze(dept)     ── SQL: KontSarze WHERE ODDELEK=…
  
"Naloži šarže"
  → BAPI_INSPLOT_GETLIST                       ── SAP read

User picks lot, "Naloži operacije"
  → BAPI_INSPLOT_GETDETAIL  (storno check)     ── SAP read
  → BAPI_INSPLOT_GETOPERATIONS                 ── SAP read

User picks operation, "Naloži karakteristike"
  → BAPI_INSPOPER_GETDETAIL                    ── SAP read

"Vnesi meritve"  → opens VpisMerForm (modal)

User fills results, "Vpis"
  → SapMeasurementSqlRepository.ZapisVzr       ── SQL: INSERT SapVzr  (returns ZAP)
  → SapMeasurementSqlRepository.VpisChr × N    ── SQL: INSERT SapChr  (one per filled row)
  → BAPI_INSPOPER_RECORDRESULTS                ── SAP write
  → BAPI_TRANSACTION_COMMIT                    ── SAP write
  → SapMeasurementSqlRepository.Prenos         ── SQL: UPDATE SapVzr SET PRENOS='X'
                                                  (only if SAP post succeeded)
```

The Delphi-parity ordering is: **SQL header + detail are written first, SAP post second, PRENOS marker last.** If SAP fails, the SQL rows remain with `PRENOS=''` and can be retried later from **Pregled → Zgodovina zapisov → Pošlji v SAP**.

---

## 5. Auxiliary forms

| Form              | Opened from                                  | Reads                                  | Writes                                              |
| ----------------- | -------------------------------------------- | -------------------------------------- | --------------------------------------------------- |
| `MainForm`        | (not wired into Program.cs; trial harness)   | nothing                                | nothing                                             |
| `VnosForm`        | App startup (`Program.cs`)                   | KontSarze (left panel), SAP BAPIs      | nothing direct                                      |
| `VpisMerForm`     | `Vnesi meritve` on VnosForm                  | (in-memory rows passed in)             | nothing direct (returns rows to caller)             |
| `SqlViewForm`     | `Pregled → Zgodovina zapisov`                | `SapVzr`, `SapChr`                     | inserts via `Prenos`; deletes `SapVzr+SapChr` rows  |
| `SqlSarzeForm`    | `Pregled → Kontrolne šarže`                  | `KontSarze`                            | deletes `KontSarze` rows; writes `sarze.txt` (Prepis) |

---

## 6. Files written by the running app

| File                                 | Where                | When                                         | Format                                       |
| ------------------------------------ | -------------------- | -------------------------------------------- | -------------------------------------------- |
| `sarze.txt`                          | App startup folder   | "Prepis" button in **Kontrolne šarže** view  | One line per KontSarze row: `sarza,oddelek,tekst,ktocka` (UTF-8) |
| (none for SAP)                       | —                    | All SAP results posted directly via BAPI     | —                                            |

---

## 7. Configuration (`App.config`)

| Key                                | Required | Default          | Purpose |
| ---------------------------------- | -------- | ---------------- | ------- |
| `sap.login.db.connection_string`   | ✅       | _(SAPkontrola)_  | SQL connection for `prijava` and all local tables |
| `sap.destination.name`             | ❌       | `SAP_DEFAULT`    | Friendly name for the SAP NCo destination |
| `sap.smoke.rfc`                    | ❌       | `RFC_SYSTEM_INFO` | RFC executed by the **Run SAP Trial** harness |
| `sap.pool_size`                    | ❌       | `5`              | NCo connection pool size |
| `sap.peak_connections_limit`       | ❌       | `20`             | NCo peak connections |
| `sap.step10.enable_post`           | ❌       | `false`          | Master kill-switch for the live SAP post (set `true` to actually submit) |
| `sap.step11.enable_sql_write`      | ❌       | `false`          | Master kill-switch for SQL `SapVzr`/`SapChr` writes |
| `sap.step5/6/7.*`                  | ❌       | _(off)_          | Legacy per-step trial knobs from the porting phase |

---

## 8. Operational notes

- **First-time setup:** drop `sapnco.dll` + `sapnco_utils.dll` into `..\dll\` (or `..\dll\32\`); the build fails fast if either is missing.
- **Recovery flow:** if SAP is down, "Vpis" still writes the local `SapVzr` / `SapChr` rows (when `sap.step11.enable_sql_write=true`), with `PRENOS=''`. Re-open the app later, go to **Pregled → Zgodovina zapisov**, select the row, click **Pošlji v SAP** — the .NET service replays the post and flips `PRENOS='X'`.
- **Diagnostics:** the **BAPI Diagnostika** button on `VnosForm` is wired but hidden (`Visible = false`). Set it to `true` to dump live BAPI parameter/field metadata into a scrollable dialog.
- **Languages:** UI ships with SLO (default) / EN / DE; switch live from the **Jezik** menu.
