using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Tooark.Tests.Http.Support;

/// <summary>
/// Servidor HTTP mínimo no loopback, em uma porta livre, para exercitar a conexão real sem sair da máquina.
/// </summary>
/// <remarks>
/// Atende uma requisição por conexão e fecha: basta para GET sem corpo, que é o que os testes da guarda enviam.
/// </remarks>
internal sealed class LoopbackServer : IAsyncDisposable
{
  private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
  private readonly CancellationTokenSource _stop = new();
  private readonly Func<string, string> _respond;
  private readonly List<string> _targets = [];
  private readonly Task _loop;

  // Inicia o servidor; a função recebe o alvo da requisição (caminho e consulta) e devolve a resposta bruta
  public LoopbackServer(Func<string, string> respond)
  {
    _respond = respond;
    _listener.Start();
    Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
    _loop = AcceptAsync();
  }

  // Porta em que o servidor escuta
  public int Port { get; }

  // Alvos das requisições recebidas, na ordem de chegada
  public IReadOnlyList<string> Targets
  {
    get
    {
      lock (_targets)
      {
        return [.. _targets];
      }
    }
  }

  // Monta uma resposta 200 com corpo JSON
  public static string Json(string json) =>
    $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Encoding.UTF8.GetByteCount(json)}\r\nConnection: close\r\n\r\n{json}";

  // Monta um redirecionamento 302
  public static string Redirect(string location) =>
    $"HTTP/1.1 302 Found\r\nLocation: {location}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";

  public async ValueTask DisposeAsync()
  {
    await _stop.CancelAsync();
    _listener.Stop();

    try
    {
      await _loop;
    }
    catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException)
    {
      // O laço termina pelo cancelamento ou pelo fechamento do listener
    }

    _stop.Dispose();
  }

  private async Task AcceptAsync()
  {
    while (!_stop.IsCancellationRequested)
    {
      var client = await _listener.AcceptTcpClientAsync(_stop.Token);

      _ = HandleAsync(client);
    }
  }

  private async Task HandleAsync(TcpClient client)
  {
    try
    {
      using (client)
      {
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

        // Linha da requisição: "GET /alvo HTTP/1.1"
        var requestLine = await reader.ReadLineAsync(_stop.Token);

        if (string.IsNullOrEmpty(requestLine))
        {
          return;
        }

        // Consome os cabeçalhos até a linha em branco
        while (!string.IsNullOrEmpty(await reader.ReadLineAsync(_stop.Token)))
        {
        }

        var target = requestLine.Split(' ')[1];

        lock (_targets)
        {
          _targets.Add(target);
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes(_respond(target)), _stop.Token);
        await stream.FlushAsync(_stop.Token);
      }
    }
    catch (Exception exception) when (exception is IOException or OperationCanceledException or SocketException)
    {
      // Conexão encerrada pelo cliente ou pelo fim do teste
    }
  }
}
