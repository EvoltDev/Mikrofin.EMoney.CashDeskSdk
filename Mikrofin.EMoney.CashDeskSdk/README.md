# Mikrofin EMoney CashDesk SDK

This lightweight .NET Standard library wraps the `ws://…/ws/cashdesk` WebSocket
protocol exposed by the Core API.  It gives integrators a typed client for logging
in cashiers, creating or cancelling payments, and reacting to payment events without
having to hand‐craft JSON frames.

## Installation

1. Reference the `Mikrofin.EMoney.CashDeskSdk` project (or package) from your solution.
2. Instantiate `CashDeskClient` with the WebSocket endpoint of your Core API deployment.

```csharp
var client = new CashDeskClient(
    new CashDeskClientOptions(new Uri("wss://api.example.com/ws/cashdesk"))
    {
        DiagnosticLogger = Console.WriteLine
    });
```

## Usage

```csharp
// Subscribe to events before connecting.
client.CashierLoginSucceeded += (_, payload) =>
{
    Console.WriteLine($"Welcome {payload.Cashier.UserName}");
    if (payload.PendingPayment != null)
    {
        Console.WriteLine($"Pending payment: {payload.PendingPayment.Id}");
    }
};

client.PaymentCreated += (_, payload) =>
{
    Console.WriteLine($"Payment {payload.Payment.Id} created with QR {payload.PaymentQrCode[..16]}…");
};

client.PaymentCreateFailed += (_, payload) =>
{
    Console.WriteLine($"Failed to create payment: {payload.Code} {payload.Message}");
};

await client.ConnectAsync(ct);
await client.LoginAsync(new CashierLoginRequest(accountId, userName, password), ct);

await client.CreatePaymentAsync(
    new CashDeskPaymentCreateRequest(
        totalAmount: 10.50m,
        currency: "BAM",
        LineItems: new[]
        {
            new CashDeskPaymentLineItemRequest("Item 1", 10.50m, 1),
        },
        PaymentMetadata: new[]
        {
            new CashDeskPaymentMetadata("externalUserId", "123", true),
        }),
    ct);
```

Remember to dispose the client (or call `DisconnectAsync`) when the integration
is shutting down.

```csharp
await client.DisconnectAsync();
await client.DisposeAsync();
```

## Events

| Event                        | Description                                                     |
|-----------------------------|-----------------------------------------------------------------|
| `CashierLoginSucceeded`     | Raised when `cashier.login.success` is received.                 |
| `CashierLoginFailed`        | Raised when the login payload is rejected.                       |
| `PaymentCreated`            | Raised after a successful `payment.create`.                      |
| `PaymentCreateFailed`       | Raised when `payment.create.error` arrives.                      |
| `PaymentCompleted`          | Raised once the user has completed the payment.                  |
| `GeneralErrorReceived`      | Raised for `cashdesk.error` envelopes.                           |
| `ConnectionClosed`          | Raised if the WebSocket closes (server or client initiated).     |

The SDK mirrors the server contracts (`PaymentDetailsResponse`, metadata, QR code string, etc.).
