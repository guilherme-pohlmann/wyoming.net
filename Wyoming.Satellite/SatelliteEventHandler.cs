using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using Wyoming.Net.Core.Events;
using Wyoming.Net.Core.Server;

namespace Wyoming.Net.Satellite;

public sealed class SatelliteEventHandler : AsyncEventHandler
{
    private readonly WyomingStreamWriter writer;
    private readonly string clientId;
    private readonly SatelliteBase satellite;
    private readonly Info wyomingInfo;

    public SatelliteEventHandler(
        TcpClient client,
        AsyncTcpServer server,
        ILoggerFactory loggerFactory,
        SatelliteBase satellite,
        Info wyomingInfo
    ) : base(client, server, loggerFactory)
    {
        writer = new WyomingStreamWriter(client.GetStream());
        clientId = Guid.NewGuid().ToString();
        this.satellite = satellite;
        this.wyomingInfo = wyomingInfo;
    }

    protected override async Task<bool> HandleEventAsync(Event ev, CancellationToken cancellationToken)
    {
        if (Describe.IsType(ev.Type))
        {
            await writer.WriteEventAsync(wyomingInfo.ToEvent());
            return true;
        }

        if (string.IsNullOrEmpty(satellite.ServerId))
        {
            await satellite.SetServerAsync(clientId, writer);
        }
        else if (!satellite.ServerId.Equals(clientId))
        {
            logger.LogInformation("Connection cancelled - ServerId: {serverId}", clientId);
            return false;
        }

        return await satellite.EventFromServerAsync(ev).WaitAsync(cancellationToken);
    }

    protected override ValueTask OnDisconnectAsync()
    {
        return satellite.ClearServerAsync();
    }
}
