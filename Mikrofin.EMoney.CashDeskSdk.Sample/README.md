# Mikrofin EMoney CashDesk SDK Sample

Ova konzolna aplikacija demonstrira korištenje `Mikrofin.EMoney.CashDeskSdk`. Konfigurišite 
WebSocket endpoint putem `appsettings*.json`:

1. Prilagodite `Mikrofin.EMoney.CashDeskSdk.Sample/appsettings.json` tako da `CashDesk:Endpoint`
   pokazuje na `wss://test-api-emoney.mfsoftware.com/ws/cashdesk` (ili drugi
   endpoint koji dobijete).
2. Po potrebi kreirajte datoteke kao `appsettings.Development.json` i
   postavite `DOTNET_ENVIRONMENT=Development` kako biste preuzeli drugi endpoint.

Pokrenite projekat i unesite traženi `accountId`, `username` i `password` kada aplikacija
zatraži komandu `login`.

```bash
dotnet run --project Mikrofin.EMoney.CashDeskSdk.Sample
```

Primjer projekta prikazuje sve događaje (`payment.created`, `payment.completed`, greške, itd.).
Ako želite otkazati neku uplatu, iskoristite komandu `cancel <paymentId>`
(npr. `cancel e2d1...`).
