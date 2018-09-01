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

        public HandleWebSocket(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.WebSockets.IsWebSocketRequest &&
                httpContext.Request.Path.Value.Equals("/time"))
            {
                var ws = await httpContext.WebSockets.AcceptWebSocketAsync();

                Task.Run(async () =>
                {
                    while (!ws.CloseStatus.HasValue)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(DateTime.Now.ToString())), WebSocketMessageType.Text, true, CancellationToken.None);
                        Thread.Sleep(1000);
                    }
                }).GetAwaiter();
                await ws.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
            }
            else
                await _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class HandleWebSocketExtensions
    {
        public static IApplicationBuilder UseHandleWebSocket(this IApplicationBuilder builder)
        {
            builder.UseWebSockets();
            return builder.UseMiddleware<HandleWebSocket>();
        }
    }
}
