# Mikrofin EMoney CashDesk SDK Sample

This console app demonstrates how to consume the `Mikrofin.EMoney.CashDeskSdk`. Configure
the WebSocket endpoint via the appsettings files:

1. Edit `Mikrofin.EMoney.CashDeskSdk.Sample/appsettings.json` for local development.
2. Create overrides like `appsettings.Development.json` and set
   `DOTNET_ENVIRONMENT=Development` when running to pick that file up.

Run the sample, then follow the prompts for the account ID, username, and password:

```bash
dotnet run --project Mikrofin.EMoney.CashDeskSdk.Sample
```

After logging in, watch the console for events (`payment.created`,
`payment.completed`, errors, etc.).
