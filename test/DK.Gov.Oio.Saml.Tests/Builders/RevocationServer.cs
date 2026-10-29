using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DK.Gov.Oio.Saml.Tests.Builders;

// Serves CA certificates, CRLs and OCSP responses over plain HTTP on loopback, the way a CA publishes them.
public sealed class RevocationServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly ConcurrentDictionary<string, Func<string, byte[], byte[]?>> _routes = new();
    private readonly CancellationTokenSource _stop = new();

    public RevocationServer()
    {
        _listener.Start();
        _ = Task.Run(Serve);
    }

    public ConcurrentQueue<string> Requests { get; } = new();

    public string Url(string name) => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/{name}";

    public void Publish(string name, Func<string, byte[], byte[]?> respond) => _routes[$"/{name}"] = respond;

    public void Withdraw(string name) => _routes.TryRemove($"/{name}", out _);

    private async Task Serve()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_stop.Token); }
            catch (Exception) { return; }
            _ = Task.Run(() => Respond(client));
        }
    }

    private async Task Respond(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            var received = new List<byte>();
            var buffer = new byte[8192];
            int headerEnd;
            while ((headerEnd = IndexOfHeaderEnd(received)) < 0)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0) return;
                received.AddRange(buffer.Take(read));
            }
            var head = Encoding.ASCII.GetString(received.Take(headerEnd).ToArray()).Split("\r\n");
            var contentLength = head.Skip(1).Select(h => h.Split(':', 2)).Where(h => h[0].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                .Select(h => int.Parse(h[1].Trim())).FirstOrDefault();
            while (received.Count < headerEnd + 4 + contentLength)
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0) return;
                received.AddRange(buffer.Take(read));
            }
            var method = head[0].Split(' ')[0];
            var path = head[0].Split(' ')[1];
            Requests.Enqueue($"{method} {path}");
            var body = _routes.TryGetValue(path, out var respond) ? respond(method, received.Skip(headerEnd + 4).Take(contentLength).ToArray()) : null;
            var status = body == null
                ? "HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"
                : $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(status));
            if (body != null) await stream.WriteAsync(body);
        }
    }

    private static int IndexOfHeaderEnd(List<byte> bytes)
    {
        for (var i = 0; i + 3 < bytes.Count; i++)
            if (bytes[i] == '\r' && bytes[i + 1] == '\n' && bytes[i + 2] == '\r' && bytes[i + 3] == '\n') return i;
        return -1;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
    }
}
