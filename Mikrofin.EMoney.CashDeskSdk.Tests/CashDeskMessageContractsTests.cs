using System;
using System.Collections.Generic;
using Mikrofin.EMoney.CashDeskSdk.Messaging;

namespace Mikrofin.EMoney.CashDeskSdk.Tests;

public class CashDeskMessageContractsTests
{
    [Fact]
    public void CashDeskPaymentCreateRequest_PreservesValues()
    {
        var lineItems = new List<CashDeskPaymentLineItemRequest>
        {
            new("Item 1", 10m, 2)
        };
        var metadata = new List<CashDeskPaymentMetadata>
        {
            new("orderId", "12345", true)
        };

        var request = new CashDeskPaymentCreateRequest(
            totalAmount: 20m,
            currency: "BAM",
            lineItems: lineItems,
            paymentMetadata: metadata);

        Assert.Equal(20m, request.TotalAmount);
        Assert.Equal("BAM", request.Currency);
        Assert.Same(lineItems, request.LineItems);
        Assert.Same(metadata, request.PaymentMetadata);
    }

    [Fact]
    public void CashDeskPaymentLineItemRequest_AssignsProperties()
    {
        var lineItem = new CashDeskPaymentLineItemRequest("Test", 5m, 3);

        Assert.Equal("Test", lineItem.Name);
        Assert.Equal(5m, lineItem.UnitPrice);
        Assert.Equal(3, lineItem.Quantity);
    }
}
