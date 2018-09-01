using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RPS
{
    public static class WebSocketExtenstion
    {
        public static async Task SendAsync(this WebSocket ws, string data)
        {
            if (ws.CloseStatus.HasValue) return;

            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data));
            await ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public static async Task SendAsync(this WebSocket ws, int data)
        {
            await ws.SendAsync(data.ToString());
        }
    }
}
