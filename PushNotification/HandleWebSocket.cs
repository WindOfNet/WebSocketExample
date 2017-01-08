using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

namespace PushNotification
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class HandleWebSocket
    {
        private readonly RequestDelegate _next;
        private readonly List<WebSocket> sockets;

        public HandleWebSocket(RequestDelegate next)
        {
            _next = next;
            sockets = new List<WebSocket>();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Monitor.Enter(sockets);

                        foreach (var ws in sockets)
                        {
                            //send server time every seconds
                            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString()));
                            await ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    finally { Monitor.Exit(sockets); }

                    Thread.Sleep(1000);
                }
            });
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (!httpContext.WebSockets.IsWebSocketRequest)
            {
                await _next(httpContext);
                return;
            }

            var ws = await httpContext.WebSockets.AcceptWebSocketAsync();

            sockets.Add(ws);

            while (!ws.CloseStatus.HasValue)
            {
                var buffer = new byte[1024 * 4];
                //handle websocket close (maybe page close, maybe user click close ...)
                await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            sockets.Remove(ws);

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class HandleWebSocketExtensions
    {
        public static IApplicationBuilder UseHandleWebSocket(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HandleWebSocket>();
        }
    }
}
