namespace CyberTelemetrySimulator.Publishers;

using System.Text.Json;
using System.Text.Json.Serialization;
using CyberTelemetrySimulator.Models;

public class FileJsonlPublisher : ITelemetryPublisher
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public FileJsonlPublisher(string filePath)
    {
        _filePath = filePath;

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }

    public async Task PublishAsync(TelemetryEvent evnt)
    {
        var line = JsonSerializer.Serialize(evnt, _options);
        await File.AppendAllTextAsync(_filePath, line + Environment.NewLine);
    }
}