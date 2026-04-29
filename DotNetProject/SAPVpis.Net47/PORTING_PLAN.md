# Delphi -> .NET Porting Plan (Defined)

## Goal
Port Delphi SAPVpis behavior to .NET in small, testable slices with clear completion criteria.

## Current status summary
- ✅ Completed: Steps 1-4
- ⏳ Remaining: Steps 5-9

---

## Step 1 - SAP connectivity baseline ✅
**Scope**
- Create .NET SAP destination registration and trial runner.
- Verify basic SAP reachability.

**Done when**
- App can create destination and call `Ping` successfully.
- Trial output shows pass/fail clearly.

**Status**
- ✅ Done.

## Step 2 - Delphi-style SAP login source ✅
**Scope**
- Read login values from SQL table `prijava` using default row `glavni='X'`.
- Map DB fields to SAP destination parameters.

**Done when**
- Destination no longer depends on per-field SAP appSettings.
- Login values are loaded from DB at runtime.

**Status**
- ✅ Done.

## Step 3 - Connection string compatibility ✅
**Scope**
- Accept Delphi/OLEDB-style DB connection strings (e.g., `Provider=SQLOLEDB.1;...`).

**Done when**
- DB login lookup works with legacy connection string formats.

**Status**
- ✅ Done.

## Step 4 - Plant rule parity and visibility ✅
**Scope**
- Port Delphi rule: `rsg_rfc_1 -> 0401`, otherwise `1061`.
- Show applied rule result in app status output.

**Done when**
- Status shows resolved user->plant mapping after trial.

**Status**
- ✅ Done.

## Step 5 - Read-only business RFC parity ⏳
**Scope**
- Port Delphi read path around `BAPI_INSPLOT_GETDETAIL`.

**Done when**
- Same input mapping as Delphi.
- Key returned fields parsed and shown/logged.
- Manual test with known sample passes.

**Status**
- ⏳ Not started.

## Step 6 - Write RFC parity (`RECORDRESULTS` + `COMMIT`) ⏳
**Scope**
- Port payload shaping for `BAPI_INSPOPER_RECORDRESULTS`.
- Port commit behavior via `BAPI_TRANSACTION_COMMIT`.

**Done when**
- Write request structure matches Delphi behavior.
- Commit only on successful return conditions.
- Manual end-to-end write test passes.

**Status**
- ⏳ Not started.

## Step 7 - Local data integration parity ⏳
**Scope**
- Port local SQL/table operations around SAP calls.

**Done when**
- Equivalent records are written/read in expected local tables.
- Error handling mirrors Delphi flow.

**Status**
- ⏳ Not started.

## Step 8 - UI workflow parity ⏳
**Scope**
- Port core user workflow screens used in daily operation.

**Done when**
- Main user flow can be executed in .NET without Delphi fallback.
- Operator-visible statuses/messages are clear.

**Status**
- ⏳ Not started.

## Step 9 - Hardening and rollout readiness ⏳
**Scope**
- Add regression checklist, operational logging, and runbook notes.

**Done when**
- Critical scenarios checklist exists and is validated.
- Deployment/rollback notes are documented.

**Status**
- ⏳ Not started.
