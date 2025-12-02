using System;
using System.Collections.Generic;

namespace CashDeskIntegrationSdk;

/// <summary>
///     Configuration values that control how the SDK connects to the cash desk WebSocket endpoint.
/// </summary>
public sealed class CashDeskClientOptions
{
    public CashDeskClientOptions(Uri endpoint)
    {
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    /// <summary>
    ///     The WebSocket endpoint exposed by the Core API (for example, <c>wss://api.example.com/ws/cashdesk</c>).
    /// </summary>
    public Uri Endpoint { get; }

    /// <summary>
    ///     Optional interval used to send WebSocket keep-alive pings. Defaults to 30 seconds.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Size of the receive buffer that is used when reading frames. Defaults to 32 KB.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 32 * 1024;

    /// <summary>
    ///     Optional custom headers that should be attached to the connect handshake.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Optional callback used for diagnostics. Receives short textual log messages about connection lifecycle events.
    /// </summary>
    public Action<string>? DiagnosticLogger { get; set; }
}
