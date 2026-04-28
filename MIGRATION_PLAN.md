# Delphi -> C# Migration Plan (agreed)

Target:
- WinForms
- .NET Framework 4.7

## Step 0 (confirmed)
- [x] Confirm parity scope (all current forms/flows, SAP behavior, SQL behavior).
- [x] Confirm technical constraints (WinForms + .NET 4.7).
- [x] Confirm no implementation started before sign-off.

## Step 1 (SAP DLL handoff)
1. You provide SAP runtime DLLs.
2. You push DLLs into repo folder: `dll/`.
3. Expected filenames:
   - `sapnco.dll`
   - `sapnco_utils.dll`
4. After push, project references resolve from `dll/`.

## Step 2 (mandatory SAP trial run gate)
1. Implement SAP connection module first.
2. Run SAP trial:
   - create destination,
   - ping SAP,
   - run one smoke RFC/BAPI.
3. Record result (success/failure + message).
4. Continue only if trial succeeds.

## Step 3+ (function-by-function porting)
1. Inspection lot list.
2. Operation list.
3. Characteristics fetch.
4. Measurement entry + A/R logic.
5. SQL staging and resend.
6. SAP writeback + commit.
