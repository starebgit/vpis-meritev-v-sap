# SAPVpis.Net47

## Where the app connects
- **SQL Server** (for login source): reads SAP login data from table `prijava` (default row `glavni='X'`).
- **SAP**: connects using the login read from SQL and configured destination name.

## What the app does
- Builds SAP destination settings from DB login data.
- Tests SAP connectivity (`Ping`).
- Runs a smoke RFC call (default: `RFC_SYSTEM_INFO`).
- Shows status/result in the form.
