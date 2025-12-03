# Mikrofin EMoney CashDesk SDK Sample

This console app demonstrates how to consume the `Mikrofin.EMoney.CashDeskSdk`. Configure
the WebSocket endpoint putem `appsettings*.json`:

1. Edit `Mikrofin.EMoney.CashDeskSdk.Sample/appsettings.json` for local development.
2. Create overrides like `appsettings.Development.json` and set
   `DOTNET_ENVIRONMENT=Development` when running to pick that file up.

Run the sample and unesite traženi `accountId`, `username` i `password` kada aplikacija
zatraži komandu `login`.

```bash
dotnet run --project Mikrofin.EMoney.CashDeskSdk.Sample
```

Sample prikazuje sve događaje (`payment.created`, `payment.completed`, greške, itd.).
Ako želite otkazati neku uplatu, iskoristite komandu `cancel <paymentId>`
(npr. `cancel e2d1...`).
