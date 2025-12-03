using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Mikrofin.EMoney.CashDeskSdk;
using Mikrofin.EMoney.CashDeskSdk.Messaging;
using Xunit;

namespace Mikrofin.EMoney.CashDeskSdk.Tests;

public class CashDeskIntegrationTests
{
    private readonly Uri endpoint;
    private readonly CashierLoginRequest credentials;

    public CashDeskIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
            .Build();

        var endpointValue = configuration["CashDesk:Endpoint"];
        var accountId = configuration["CashDesk:AccountId"];
        var userName = configuration["CashDesk:UserName"];
        var password = configuration["CashDesk:Password"];

        Assert.False(string.IsNullOrWhiteSpace(endpointValue), "CashDesk:Endpoint is required for integration tests.");
        Assert.False(string.IsNullOrWhiteSpace(accountId), "CashDesk:AccountId is required for integration tests.");
        Assert.False(string.IsNullOrWhiteSpace(userName), "CashDesk:UserName is required for integration tests.");
        Assert.False(string.IsNullOrWhiteSpace(password), "CashDesk:Password is required for integration tests.");

        endpoint = new Uri(endpointValue!);
        credentials = new CashierLoginRequest(accountId!, userName!, password!);
    }

    [Fact]
    public async Task LoginAndCancelPendingAndCreatedPayments()
    {
        await using var client = new CashDeskClient(new CashDeskClientOptions(endpoint));
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        CashierLoginSuccessPayload? loginPayload = null;
        PaymentCreatedPayload? createdPayload = null;
        client.CashierLoginSucceeded += (_, payload) => loginPayload = payload;
        client.PaymentCreated += (_, payload) => createdPayload = payload;

        await client.ConnectAsync(cts.Token);
        await client.LoginAsync(credentials, cts.Token);
        await WaitUntilAsync(() => loginPayload != null, cts.Token);

        if (loginPayload?.PendingPayment != null)
        {
            await client.CancelPaymentAsync(loginPayload.PendingPayment.Id, cts.Token);
        }

        var request = new CashDeskPaymentCreateRequest(
            totalAmount: 1m,
            currency: "BAM",
            lineItems: new[] { new CashDeskPaymentLineItemRequest("Test Item", 1m, 1) },
            paymentMetadata: Array.Empty<CashDeskPaymentMetadata>());

        await client.CreatePaymentAsync(request, cts.Token);
        await WaitUntilAsync(() => createdPayload != null, cts.Token);

        if (createdPayload?.Payment != null)
        {
            await client.CancelPaymentAsync(createdPayload.Payment.Id, cts.Token);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, CancellationToken cancellationToken)
    {
        while (!predicate())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
        }
    }
}
