# Mikrofin EMoney CashDesk SDK – Detaljna dokumentacija

## Pregled

`Mikrofin.EMoney.CashDeskSdk` je .NET Standard 2.0 biblioteka koja enkapsulira
WebSocket protokol `ws(s)://<host>/ws/cashdesk` iz Mikrofin EMoney Core API-ja.

SDK održava jednu WebSocket vezu prema API-ju, šalje poruke kroz tipizirane
metode (`LoginAsync`, `CreatePaymentAsync`, `CancelPaymentAsync`) i izlaže
dogadjaje (`PaymentCreated`, `PaymentCompleted`, itd.) koje integracija može
pretvoriti u UI obavijesti ili poslovnu logiku.

## Zahtjevi

- .NET 6.0+ ili .NET Standard kompatibilni runtime (biblioteka cilja netstandard2.0).
- Kreiran račun za kasira u EMoney Core-u.
- Pristup odgovarajućem WebSocket endpointu (`ws://localhost:5000/ws/cashdesk`
  za lokalno, `wss://…` za dev/test/prod).

## Instalacija (NuGet)

SDK se distribuira isključivo kao NuGet paket. Uključite ga u projekat pomoću:

```bash
dotnet add package Mikrofin.EMoney.CashDeskSdk
```

ili ručno kroz `PackageReference`:

```xml
<ItemGroup>
  <PackageReference Include="Mikrofin.EMoney.CashDeskSdk" Version="x.y.z" />
</ItemGroup>
```

## Konfiguracija klijenta

```csharp
var options = new CashDeskClientOptions(new Uri("wss://<vas-host>/ws/cashdesk"))
{
    KeepAliveInterval = TimeSpan.FromSeconds(30),
    ReceiveBufferSize = 32 * 1024,
    DiagnosticLogger = message => logger.LogInformation("[CashDeskSdk] {Message}", message)
};

await using var client = new CashDeskClient(options);
```

### Obavezni parametri
- `Endpoint` – puni `ws://` ili `wss://` URL koji vodi do CashDesk endpointa.

### Često korištene postavke
- `KeepAliveInterval` – period slanja pingova (default 30 sek).
- `ReceiveBufferSize` – veličina buffera za prijem frame-ova (default 32 KB).
- `Headers` – kolekcija custom zaglavlja (trenutno nije neophodna; koristite samo ako vam gateway naknadno zatraži dodatne header-e).
- `DiagnosticLogger` – callback za logovanje lifecycle događaja (spajanje, diskonekcija).

## Tipičan tok integracije

1. Instancirati `CashDeskClient` i pretplatiti se na događaje **prije** poziva
   `ConnectAsync`.
2. Pozvati `ConnectAsync(ct)` – uspostavlja WebSocket handshake.
3. Poslati `LoginAsync(new CashierLoginRequest(accountId, userName, password), ct)`.
4. Po potrebi pozivati:
   - `CreatePaymentAsync` sa linijama artikala, valutom i metapodacima.
   - `CancelPaymentAsync(paymentId)` za otkazivanje.
5. Rukovati događajima koje server šalje (npr. `PaymentCreated`, `PaymentCompleted`).
6. Na gašenje aplikacije ili gubitak konekcije pozvati `DisconnectAsync` i
   `DisposeAsync` (ili koristiti `await using` kao u primjerima).

## Kod primjer

```csharp
await using var client = new CashDeskClient(options);

client.CashierLoginSucceeded += (_, payload) =>
{
    Console.WriteLine($"Blagajnik {payload.Cashier.UserName} je prijavljen.");
    if (payload.PendingPayment != null)
    {
        Console.WriteLine($"Postoji neriješena uplata: {payload.PendingPayment.Id}");
    }
};

client.PaymentCreated += (_, payload) =>
{
    Console.WriteLine($"Nova uplata {payload.Payment.Id}, QR: {payload.PaymentDeepLink}");
};

client.PaymentCreateFailed += (_, payload) =>
{
    Console.WriteLine($"Greška {payload.Code}: {payload.Message}");
};

await client.ConnectAsync(ct);
await client.LoginAsync(new CashierLoginRequest(accountId, username, password), ct);

await client.CreatePaymentAsync(
    new CashDeskPaymentCreateRequest(
        totalAmount: 25.00m,
        currency: "BAM",
        lineItems: new[]
        {
            new CashDeskPaymentLineItemRequest("Brasno", 25.00m)
        },
        paymentMetadata: Array.Empty<CashDeskPaymentMetadata>()),
    ct);
```

## Događaji

| Događaj                  | Opis                                                                 | Payload (ključna polja)                                                                                                                                                        |
|-------------------------|----------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `CashierLoginSucceeded` | Server je prihvatio `cashier.login`.                                | `Cashier` (AccountId, UserName, Location…), opcioni `PendingPayment` (ako integrator treba odmah preuzeti postojeću uplatu), `PaymentDeepLink` (ako postoji).                 |
| `CashierLoginFailed`    | Prijava odbijena.                                                    | `CashDeskErrorPayload` – `Code` (npr. `InvalidCredentials`, `UserLocked`) i `Message`. Nijedno polje nije `null`; koristi ih za prikaz operateru ili za audit log.             |
| `PaymentCreated`        | `payment.create` uspješan.                                           | `PaymentCreatedPayload` – `Payment` (`PaymentDetailsResponse`: Id, Amount, Currency, Status, Metadata, LineItems), obavezni `PaymentDeepLink`.                                |
| `PaymentCreateFailed`   | Kreiranje uplate odbijeno (`payment.create.error`).                  | `PaymentCreateErrorPayload` – nasljeđuje `CashDeskErrorPayload`. Opcioni `PendingPayment` (ako server zadržava prethodnu uplatu). `Code` npr. `MissingLineItems`. |
| `PaymentCompleted`      | Korisnik je završio plaćanje (`payment.completed`).                  | `PaymentCompletedPayload` – `Payment` (iste strukture kao iznad) i `UserId` koji je inicirao završetak.                                                                        |
| `GeneralErrorReceived`  | Šalje se `cashdesk.error` za sve ostale greške protokola.            | `CashDeskErrorPayload` – `Code`, `Message`. Koristite za prikaz korisniku ili logovanje; može značiti da je server odbio komandu zbog stanja uređaja.                          |
| `ConnectionClosed`      | Konekcija zatvorena sa bilo koje strane.                             | `ConnectionClosedEventArgs` – `Status` (npr. `NormalClosure`, `AbnormalClosure`), `Description` (poruka servera ili izuzetak). Korisno za prikaz i za retry logiku.            |

### Obavezna polja po zahtevima

- `CashierLoginRequest`: `AccountId`, `UserName`, `Password` su obavezni i nikada se ne šalju prazni.
- `CashDeskPaymentCreateRequest`: 
  - `TotalAmount` (decimal) koji je obavezan i `Currency` (default `BAM`).
  - `LineItems` je opcionalan. Svaki `CashDeskPaymentLineItemRequest` ima obavezna polja `Name`, `UnitPrice` i opcionalno `Quantity` (default 1).
  - `PaymentMetadata` može biti prazna lista. Svaki `CashDeskPaymentMetadata` ima `Key`, `Value`, i opcioni `DisplayToUser` (default `false`).
- `CashDeskPaymentCancelRequest`: zahtijeva validan `PaymentId` (Guid) prethodno vraćen od servera.

### Struktura tipova

- `PaymentDetailsResponse` (server response) sadrži:
  - `Id` (Guid), `Amount`, `Currency`, `Status` (`Pending`, `Successful`, `Canceled`).
  - `Location` (`PaymentLocationInfo` sa `Name`, `Address`).
  - `CreatedAt` (`JsonElement` – server šalje datum u ISO formatu), `LineItems` (lista `PaymentLineItemResponse`), `Metadata` (lista `PaymentMetadataResponse`).
- `PaymentLineItemResponse` uključuje `Id`, `Name`, `Quantity`, `UnitPrice`, `Amount`.
- `PaymentMetadataResponse` ima `Key`, `Value`, `DisplayToUser`.

Koristite ova polja direktno ili mapirajte na vlastite DTO klasse. SDK već koristi `System.Text.Json` sa `JsonNamingPolicy.CamelCase`, tako da su nazivi polja identični onome što vidite u JSON payload-ima.

## Rukovanje greškama

- Svaki poziv (`ConnectAsync`, `LoginAsync`, `CreatePaymentAsync`) prihvata
  `CancellationToken` – koristite ga za timeoute i graceful shutdown.
- U slučaju izuzetka u `ReceiveLoop` SDK poziva `ConnectionClosed` sa razlogom.
- Ako primite `CashierLoginFailed`, tipično treba ponovo pitati korisnika za
  kredencijale ili blokirati dalji rad.
- `PaymentCreateFailed` vraća i eventualnu `PendingPayment`, što vam omogućava
  prikaz korisniku šta je ostalo otvoreno.

## Testiranje i okruženja

- **Lokalno**: pokrenite sample API na `http://localhost:5000`, podesite
  `appsettings.json` na `ws://localhost:5000/ws/cashdesk`.
- **Development**: `DOTNET_ENVIRONMENT=Development` + `appsettings.Development.json`
  gdje definišete `wss://<vas-dev-host>/ws/cashdesk` ili drugi URL.
- **Production**: koristite `wss://` (TLS) URL koji obezbjeđuje vaš API gateway;
  provjerite da li treba dodatna autentifikacija na nivou zaglavlja.
- **Automatski testovi**: možete koristiti testove iz Mikrofin.EMoney.CashDeskSdk.Tests.

## Resursi

- `Mikrofin.EMoney.CashDeskSdk.Sample` – konzolna aplikacija koja demonstrira
  sve funkcionalnosti (prijava, kreiranje uplata, praćenje događaja).
