using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synapse.Server.Extras;
using Synapse.Server.Services;

public interface IHttpApiService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

public class HttpApiService : IHttpApiService
{
    private readonly string _apiKey;
    private readonly ILogger<HttpApiService> _logger;
    private readonly HttpListener _listener = new();
    private readonly string[] _prefixes = { "http://localhost:5000/" }; // Podés parametrizar esto


    private readonly ILeaderboardService _leaderboardService;


    public HttpApiService(
        IConfiguration config,
        ILogger<HttpApiService> logger,
        ILeaderboardService leaderboardService)
    {
        _apiKey = config["ApiKey"] ?? throw new InvalidOperationException("ApiKey not configured.");
        _logger = logger;

        _leaderboardService = leaderboardService;

        foreach (var prefix in _prefixes)
        {
            _listener.Prefixes.Add(prefix);
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        _logger.LogInformation("HTTP API Service started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _ = Task.Run(() => HandleRequest(context));
        }

        _listener.Stop();
        _logger.LogInformation("HTTP API Service stopped.");
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string? providedKey = request.Headers["X-Api-Key"];
        if (providedKey != _apiKey)
        {
            response.StatusCode = 401;
            WriteResponse(response, "Unauthorized");
            return;
        }

        if (request.HttpMethod == "GET" && request.Url?.AbsolutePath == "/status")
        {
            var result = _leaderboardService.GetAllLeaderboardsScores(1, true);
            string content = JsonSerializer.Serialize(result, JsonUtils.Settings);

            response.StatusCode = 200;
            WriteResponse(response, content, contentType: "application/json");
        }
        else
        {
            response.StatusCode = 404;
            WriteResponse(response, "Not Found");
        }
    }

    private void WriteResponse(HttpListenerResponse response, string content, string contentType = "text/plain")
    {
        byte[] buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        response.ContentType = contentType;

        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }
}
