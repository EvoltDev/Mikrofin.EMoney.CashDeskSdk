using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CashDeskIntegrationSdk.Messaging;

internal static class CashDeskMessageTypes
{
    public const string CashierLogin = "cashier.login";
    public const string CashierLoginSuccess = "cashier.login.success";
    public const string CashierLoginError = "cashier.login.error";
    public const string PaymentCreate = "payment.create";
    public const string PaymentCreated = "payment.created";
    public const string PaymentCreateError = "payment.create.error";
    public const string PaymentCancel = "payment.cancel";
    public const string PaymentCompleted = "payment.completed";
    public const string GeneralError = "cashdesk.error";
}

internal sealed class CashDeskOutgoingEnvelope<T>
{
    public CashDeskOutgoingEnvelope()
    {
    }

    public CashDeskOutgoingEnvelope(string type, T payload)
    {
        Type = type;
        Payload = payload;
    }

    public string Type { get; set; } = string.Empty;
    public T Payload { get; set; } = default!;
}

internal sealed class CashDeskIncomingMessage
{
    public string Type { get; set; } = string.Empty;
    public JsonElement Payload { get; set; }
}

public sealed class CashierLoginRequest
{
    public CashierLoginRequest(string accountId, string userName, string password)
    {
        AccountId = accountId;
        UserName = userName;
        Password = password;
    }

    public string AccountId { get; }
    public string UserName { get; }
    public string Password { get; }
}

public sealed class CashDeskPaymentLineItemRequest
{
    public CashDeskPaymentLineItemRequest(string name, decimal unitPrice, int quantity = 1)
    {
        Name = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public string Name { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; }
}

public sealed class CashDeskPaymentMetadata
{
    public CashDeskPaymentMetadata(string key, string value, bool displayToUser = false)
    {
        Key = key;
        Value = value;
        DisplayToUser = displayToUser;
    }

    public string Key { get; }
    public string Value { get; }
    public bool DisplayToUser { get; }
}

public sealed class CashDeskPaymentCreateRequest
{
    public CashDeskPaymentCreateRequest(
        decimal totalAmount,
        string currency,
        IReadOnlyList<CashDeskPaymentLineItemRequest> lineItems,
        IReadOnlyList<CashDeskPaymentMetadata> paymentMetadata)
    {
        TotalAmount = totalAmount;
        Currency = currency;
        LineItems = lineItems;
        PaymentMetadata = paymentMetadata;
    }

    public decimal TotalAmount { get; }
    public string Currency { get; }
    public IReadOnlyList<CashDeskPaymentLineItemRequest> LineItems { get; }
    public IReadOnlyList<CashDeskPaymentMetadata> PaymentMetadata { get; }
}

public sealed class CashDeskPaymentCancelRequest
{
    public CashDeskPaymentCancelRequest(Guid paymentId)
    {
        PaymentId = paymentId;
    }

    public Guid PaymentId { get; }
}

public sealed class CashierLoginSuccessPayload
{
    public CashDeskCashierInfo Cashier { get; set; } = new();
    public PaymentDetailsResponse? PendingPayment { get; set; }
    public string? PaymentQrCode { get; set; }
}

public sealed class CashDeskCashierInfo
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationAddress { get; set; } = string.Empty;
}

public sealed class PaymentCreatedPayload
{
    public PaymentDetailsResponse Payment { get; set; } = new();
    public string PaymentQrCode { get; set; } = string.Empty;
}

public sealed class PaymentCompletedPayload
{
    public PaymentDetailsResponse Payment { get; set; } = new();
    public Guid UserId { get; set; }
}

public class CashDeskErrorPayload
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class PaymentCreateErrorPayload : CashDeskErrorPayload
{
    public PaymentDetailsResponse? PendingPayment { get; set; }
}

public sealed class PaymentDetailsResponse
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }
    public PaymentLocationInfo Location { get; set; } = new();
    public JsonElement CreatedAt { get; set; }
    public IReadOnlyList<PaymentLineItemResponse> LineItems { get; set; } = Array.Empty<PaymentLineItemResponse>();
    public IReadOnlyList<PaymentMetadataResponse> Metadata { get; set; } = Array.Empty<PaymentMetadataResponse>();
}

public sealed class PaymentLocationInfo
{
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public sealed class PaymentLineItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public sealed class PaymentMetadataResponse
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool DisplayToUser { get; set; }
}

public enum PaymentStatus : byte
{
    Pending = 1,
    Successful = 2,
    Canceled = 3,
}
