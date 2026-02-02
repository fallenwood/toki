namespace Toki;

using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLinq;

internal static class PreviewServer {
  private static readonly List<WebSocket> sockets = new();
  private static readonly Lock @lock = new();

  internal static void TriggerReload() {
    lock (@lock) {
      var buffer = Encoding.UTF8.GetBytes("reload");
      foreach (var socket in sockets.AsValueEnumerable().Where(s => s.State == WebSocketState.Open).ToList()) {
        try {
          socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        } catch { /* Ignore */ }
      }
    }
  }

  internal static async Task RunPreviewServer(string publicDir, int port, ILoggerFactory loggerFactory, CancellationToken cancellationToken) {
    var builder = WebApplication.CreateBuilder();
    builder.WebHost.UseSetting("urls", $"http://127.0.0.1:{port}");
    builder.Logging.ClearProviders(); // We manage logging externally

    var app = builder.Build();
    var logger = loggerFactory.CreateLogger("toki.preview");

    var fullPath = Path.GetFullPath(publicDir);
    Directory.CreateDirectory(fullPath);

    app.UseWebSockets();

    app.UseDefaultFiles(new DefaultFilesOptions {
      FileProvider = new PhysicalFileProvider(fullPath)
    });

    app.Use(async (context, next) => {
      if (context.Request.Path == "/_reload") {
        if (context.WebSockets.IsWebSocketRequest) {
          using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
          lock (@lock) { sockets.Add(webSocket); }

          var buffer = new byte[1024 * 4];
          while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested) {
            // Keep connection open
            try { await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken); } catch { break; }
          }

          lock (@lock) { sockets.Remove(webSocket); }
        } else {
          context.Response.StatusCode = 400;
        }
        return;
      }

      // Inject reload script into HTML files
      var path = context.Request.Path.Value ?? "";
      if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) {
        var filePath = Path.Combine(fullPath, path.TrimStart('/'));
        if (File.Exists(filePath)) {
          context.Response.ContentType = "text/html";
          var content = await File.ReadAllTextAsync(filePath, cancellationToken);
          var script = @"
<script>
(function() {
    var protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    var url = protocol + '//' + window.location.host + '/_reload';
    var socket = new WebSocket(url);
    socket.onmessage = function(event) {
        if (event.data === 'reload') window.location.reload();
    };
    socket.onclose = function() { console.log('Reload socket closed'); };
})();
</script>";
          await context.Response.WriteAsync(content + script, cancellationToken);
          return;
        }
      }

      await next();
    });

    app.UseStaticFiles(new StaticFileOptions {
      FileProvider = new PhysicalFileProvider(fullPath)
    });

    logger.LogInformation("Preview server running at http://127.0.0.1:{Port}", port);
    await app.RunAsync(cancellationToken);
  }
}
