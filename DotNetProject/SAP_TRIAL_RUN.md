# SAP Trial Run Gate (Step 2)

Scope: WinForms + .NET Framework 4.7 only.

## 1) DLL/reference check (Step 1.4)

The project resolves SAP NCo references from:

1. `dll/sapnco.dll` + `dll/sapnco_utils.dll` (primary), or
2. `dll/32/sapnco.dll` + `dll/32/sapnco_utils.dll` (fallback).

`SAPVpis.Net47.csproj` fails early at build time if both locations are missing.

## 2) Required SAP config

Edit `DotNetProject/SAPVpis.Net47/App.config` and set these values:

- `sap.ashost`
- `sap.sysnr`
- `sap.client`
- `sap.user`
- `sap.passwd`
- `sap.lang`

Optional:

- `sap.destination.name` (default `SAP_DEFAULT`)
- `sap.smoke.rfc` (default `RFC_SYSTEM_INFO`)
- pool settings (`sap.pool_size`, `sap.max_pool_size`, `sap.idle_timeout`, `sap.peak_connections_limit`)

> There is currently no SAP login popup form in this .NET skeleton. Login is config-driven from `App.config`.

## 3) Run instructions

1. Open `DotNetProject/SAPVpis.Net47/SAPVpis.Net47.csproj` in Visual Studio (Developer Pack for .NET Framework 4.7 installed).
2. Build in `Debug|AnyCPU`.
3. Start app and click **Run SAP Trial**.

The trial executes, in order:

1. create destination from `App.config`,
2. `destination.Ping()`,
3. invoke smoke RFC/BAPI (default `RFC_SYSTEM_INFO`).

## 4) Result capture

The status box prints:

- `Success: True/False`
- `Message: <result/error>`
- `TimestampUtc: <ISO-8601 UTC>`

Gate rule:

- Continue to Step 3+ only when `Success: True`.

## 5) Troubleshooting quick guide

- `Cannot get destination <NAME> -- no destination configuration registered`:
  rebuild and rerun with the fixed destination configuration registration (latest commit), then click **Run SAP Trial** again.
- `Missing required appSetting ...`:
  fill the missing SAP config keys in `App.config`.
- `Name or password is incorrect` or logon failure:
  verify `sap.user`, `sap.passwd`, `sap.client`, and authorization for the smoke RFC.
