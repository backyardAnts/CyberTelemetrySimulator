namespace CyberTelemetrySimulator.Publishers;

using System.Text.Json;
using System.Text.Json.Serialization;
using CyberTelemetrySimulator.Models;

public class FileJsonlPublisher : ITelemetryPublisher //implement the publisher contract (it will include te async function)
{
    private readonly string _filePath; //define the file path where data is going to be stored
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