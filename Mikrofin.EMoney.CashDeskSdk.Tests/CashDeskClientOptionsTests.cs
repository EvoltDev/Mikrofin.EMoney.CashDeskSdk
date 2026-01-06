using System;
using Mikrofin.EMoney.CashDeskSdk;

namespace Mikrofin.EMoney.CashDeskSdk.Tests;

public class CashDeskClientOptionsTests
{
    [Fact]
    public void Defaults_AreInitialized()
    {
        var options = new CashDeskClientOptions(new Uri("wss://test-api-emoney.mfsoftware.com/ws/cashdesk"));

        Assert.Equal(TimeSpan.FromSeconds(30), options.KeepAliveInterval);
        Assert.Equal(32 * 1024, options.ReceiveBufferSize);
        Assert.NotNull(options.Headers);
        Assert.Empty(options.Headers);
        Assert.Null(options.DiagnosticLogger);
    }

    [Fact]
    public void Headers_DictionaryIsCaseInsensitive()
    {
        var options = new CashDeskClientOptions(new Uri("wss://test-api-emoney.mfsoftware.com/ws/cashdesk"));

        options.Headers["X-Test-Key"] = "value";

        Assert.True(options.Headers.TryGetValue("x-test-key", out var value));
        Assert.Equal("value", value);
    }
}
