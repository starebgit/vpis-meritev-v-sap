# Phase 0 — Delphi→C# Parity Contract (WinForms, .NET Framework 4.7)

## Goal
Define exactly what “complete Delphi parity” means before any migration coding starts.

---

## Step 0 confirmation status
- [x] Confirm parity scope.
- [x] Confirm technical constraints (WinForms + .NET Framework 4.7).
- [x] Confirm no implementation before sign-off.

---

## 1) Scope boundary (in-scope)
The C# app must reproduce behavior of these Delphi forms/flows:

1. `Vnos` (main workflow)
2. `Vpiskar`
3. `single`
4. `vpisMer`
5. `odlocitev`
6. `KonTocke`
7. `vpisSkup`
8. `SqlTable`
9. `SqlSarze`
10. `PrijavaSAP` (external shared form)
11. `VpisPrijava` (external shared form)

**In-scope behavior**
- SAP login/session handling
- Inspection lot search and selection
- Operation selection
- Characteristic retrieval and entry
- Result evaluation A/R logic
- Local SQL staging + re-send
- Department/lot helper list and fallback text file

---

## 2) Technical constraints (locked)
- UI technology: **WinForms**
- Runtime: **.NET Framework 4.7**
- SAP runtime binaries: loaded from repo folder **`dll/`**
- Same SAP system credentials and plant-selection behavior as existing app
- Same DB schema semantics (`SapVzr`, `SapChr`, `Kontsarze`)

---

## 3) SAP parity surface (must match)
Minimum BAPI/RFC surface that must behave the same:

- `ZRFC_CONV_MATNR_GET`
- `BAPI_INSPLOT_GETLIST`
- `BAPI_INSPLOT_GETDETAIL`
- `BAPI_INSPLOT_GETOPERATIONS`
- `BAPI_INSPOPER_GETDETAIL`
- `BAPI_INSPOPER_RECORDRESULTS`
- `BAPI_TRANSACTION_COMMIT`

---

## 4) Data parity rules
- Persist local transaction header + line items equivalent to Delphi `SapVzr` and `SapChr` writes.
- Preserve transfer marker semantics (`prenos = 'X'`) after successful SAP post.
- Preserve fallback behavior for lot/department source (`sarze.txt`) when DB is unavailable.

---

## 5) UX parity rules
- Preserve modal workflow and button order where practical.
- Preserve validation/error messages intent and decision points.
- Preserve A/R auto-evaluation behavior for measured numeric values versus tolerance limits.

---

## 6) Acceptance criteria (Phase 0 exit)
Phase 0 is complete when the team signs off these artifacts:

1. **Parity checklist** (all features enumerated and mapped)
2. **Field mapping matrix** (Delphi field → C# model/property)
3. **SAP call mapping** (input/output table/field coverage)
4. **Test scenario list** (happy path + edge cases)
5. **Environment contract** (`dll/` policy, config policy, DB connection policy)

No Phase 1 coding starts until this document is accepted.

---

## 7) Out-of-scope for Phase 0
- No implementation code
- No UI rebuild yet
- No DB migration/refactor
- No architecture expansion beyond parity requirements
