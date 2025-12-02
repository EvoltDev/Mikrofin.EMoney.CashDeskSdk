using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Mikrofin.EMoney.CashDeskSdk;
using Mikrofin.EMoney.CashDeskSdk.Messaging;
using Microsoft.Extensions.Configuration;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cts.Cancel();
};

var configuration = BuildConfiguration();
var endpoint = ResolveEndpoint(configuration);

await using var client = new CashDeskClient(
    new CashDeskClientOptions(endpoint)
    {
        DiagnosticLogger = message => Console.WriteLine($"[SDK] {message}")
    });

Console.WriteLine($"Using endpoint {endpoint}");

client.CashierLoginSucceeded += (_, payload) =>
{
    Console.WriteLine($"Logged in as {payload.Cashier.UserName}");
    DumpPayload("cashier.login.success", payload);
};

client.CashierLoginFailed += (_, payload) =>
{
    Console.WriteLine($"Login failed: {payload.Code} - {payload.Message}");
    DumpPayload("cashier.login.error", payload);
};

client.PaymentCreated += (_, payload) =>
{
    Console.WriteLine($"Payment created: {payload.Payment.Id}");
    DumpPayload("payment.created", payload);
};

client.PaymentCompleted += (_, payload) =>
{
    Console.WriteLine($"Payment {payload.Payment.Id} completed");
    DumpPayload("payment.completed", payload);
};

client.PaymentCreateFailed += (_, payload) =>
{
    Console.WriteLine($"Payment creation failed: {payload.Code} - {payload.Message}");
    DumpPayload("payment.create.error", payload);
};

client.GeneralErrorReceived += (_, payload) =>
{
    Console.WriteLine($"General error: {payload.Code} - {payload.Message}");
    DumpPayload("cashdesk.error", payload);
};

client.ConnectionClosed += (_, args) =>
{
    Console.WriteLine($"Connection closed. Status={args.Status}, Description={args.Description}");
};

try
{
    await client.ConnectAsync(cts.Token);
    await LoginFromConsoleAsync(client, cts.Token);

    Console.WriteLine("Commands: 'login', 'create', 'cancel <id>', or Ctrl+C to quit.");
    while (!cts.Token.IsCancellationRequested)
    {
        var line = await ReadLineAsync(cts.Token).ConfigureAwait(false);
        if (line == null)
        {
            break;
        }

        if (line.Equals("login", StringComparison.OrdinalIgnoreCase))
        {
            await LoginFromConsoleAsync(client, cts.Token);
            continue;
        }

        if (line.Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            await CreatePaymentFromConsoleAsync(client, cts.Token);
            continue;
        }

        if (line.StartsWith("cancel", StringComparison.OrdinalIgnoreCase))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? idInput = parts.Length > 1 ? parts[1] : null;
            if (idInput == null)
            {
                Console.Write("Enter payment ID to cancel: ");
                idInput = Console.ReadLine();
            }

            if (Guid.TryParse(idInput, out var paymentId))
            {
                await client.CancelPaymentAsync(paymentId, cts.Token);
                Console.WriteLine($"Cancel request sent for {paymentId}");
            }
            else
            {
                Console.WriteLine("Invalid GUID.");
            }

            continue;
        }

        Console.WriteLine("Unknown command.");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Shutting down...");
}
catch (Exception ex)
{
    Console.WriteLine($"Unhandled exception: {ex}");
}
finally
{
    await client.DisposeAsync();
    cts.Dispose();
}

static void DumpPayload(string label, object payload)
{
    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
    {
        WriteIndented = true
    });
    Console.WriteLine($"{label} payload:\n{json}");
}

static async Task CreatePaymentFromConsoleAsync(CashDeskClient client, CancellationToken cancellationToken)
{
    Console.Write("Amount: ");
    var amountInput = Console.ReadLine();
    if (!decimal.TryParse(amountInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
    {
        Console.WriteLine("Invalid amount.");
        return;
    }

    Console.Write("Currency [BAM]: ");
    var currency = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(currency))
    {
        currency = "BAM";
    }

    var metadata = new List<CashDeskPaymentMetadata>();
    Console.Write("Add metadata entries? (y/N): ");
    var metadataAnswer = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(metadataAnswer) && metadataAnswer.StartsWith("y", StringComparison.OrdinalIgnoreCase))
    {
        while (true)
        {
            Console.Write("Metadata key (leave blank to finish): ");
            var key = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(key))
            {
                break;
            }

            Console.Write("Value: ");
            var value = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine("Value cannot be empty.");
                continue;
            }

            Console.Write("Display to user? (y/N): ");
            var displayAnswer = Console.ReadLine();
            var displayToUser = !string.IsNullOrWhiteSpace(displayAnswer) &&
                                displayAnswer.StartsWith("y", StringComparison.OrdinalIgnoreCase);

            metadata.Add(new CashDeskPaymentMetadata(key, value, displayToUser));
        }
    }

    var lineItems = new List<CashDeskPaymentLineItemRequest>();
    Console.Write("Add line items? (y/N): ");
    var answer = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(answer) && answer.StartsWith("y", StringComparison.OrdinalIgnoreCase))
    {
        while (true)
        {
            Console.Write("Item name (leave blank to finish): ");
            var itemName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                break;
            }

            Console.Write("Unit price: ");
            var priceInput = Console.ReadLine();
            if (!decimal.TryParse(priceInput, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice <= 0)
            {
                Console.WriteLine("Invalid price, skipping item.");
                continue;
            }

            Console.Write("Quantity [1]: ");
            var qtyInput = Console.ReadLine();
            if (!int.TryParse(qtyInput, out var quantity) || quantity <= 0)
            {
                quantity = 1;
            }

            lineItems.Add(new CashDeskPaymentLineItemRequest(itemName, unitPrice, quantity));
        }
    }

    await client.CreatePaymentAsync(
        new CashDeskPaymentCreateRequest(
            amount,
            currency,
            lineItems,
            metadata),
        cancellationToken);
}

static async Task LoginFromConsoleAsync(CashDeskClient client, CancellationToken cancellationToken)
{
    var accountId = ReadRequiredValue("Account ID");
    var userName = ReadRequiredValue("Username");
    var password = ReadRequiredPassword();

    await client.LoginAsync(new CashierLoginRequest(accountId, userName, password), cancellationToken);
}

static Uri ResolveEndpoint(IConfiguration configuration)
{
    var endpointFromConfig = configuration["CashDesk:Endpoint"];
    if (!string.IsNullOrWhiteSpace(endpointFromConfig))
    {
        if (Uri.TryCreate(endpointFromConfig, UriKind.Absolute, out var configUri))
        {
            return configUri;
        }

        Console.WriteLine($"Invalid CashDesk:Endpoint '{endpointFromConfig}', falling back to ws://localhost:5000/ws/cashdesk.");
    }

    return new Uri("ws://localhost:5000/ws/cashdesk");
}

static IConfiguration BuildConfiguration()
{
    var environment = GetEnvironmentName();
    return new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
        .Build();
}

static string GetEnvironmentName()
{
    return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
           ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
           ?? "Production";
}

static string ReadRequiredValue(string label)
{
    while (true)
    {
        Console.Write($"{label}: ");
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        Console.WriteLine($"{label} cannot be empty.");
    }
}

static string ReadRequiredPassword()
{
    while (true)
    {
        Console.Write("Password: ");
        var input = ReadPassword();
        if (!string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        Console.WriteLine("Password cannot be empty.");
    }
}

static async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
{
    var readTask = Task.Run(Console.ReadLine);
    var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
    var completedTask = await Task.WhenAny(readTask, cancelTask).ConfigureAwait(false);
    if (completedTask == readTask)
    {
        return await readTask.ConfigureAwait(false);
    }

    return null;
}

static string ReadPassword()
{
    var builder = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            break;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (builder.Length > 0)
            {
                builder.Length--;
                Console.Write("\b \b");
            }

            continue;
        }

        builder.Append(key.KeyChar);
        Console.Write('*');
    }

    return builder.ToString();
}
