using System.Text.Json;
using CyberTelemetrySimulator.Config;
using CyberTelemetrySimulator.Campaigns;
using CyberTelemetrySimulator.Devices;
using CyberTelemetrySimulator.Models;
using CyberTelemetrySimulator.Publishers;

var settingsPath = Path.Combine(AppContext.BaseDirectory, "Config", "simulatorSettings.json");
var settingsJson = File.ReadAllText(settingsPath);
var settings = JsonSerializer.Deserialize<SimulatorSettings>(settingsJson) ?? new SimulatorSettings();

var campaigns = new CampaignManager(
    attackChancePerTick: settings.AttackChancePerTick,
    minDurationSec: settings.MinDurationSec,
    maxDurationSec: settings.MaxDurationSec
);

var devices = new List<DeviceSimulator>
{
    new("WS-01", DeviceType.Workstation),
    new("WS-02", DeviceType.Workstation),
    new("WEB-01", DeviceType.WebServer),
    new("DB-01", DeviceType.DatabaseServer),
    new("IOT-01", DeviceType.IoTDevice),
    new("IOT-02", DeviceType.IoTDevice),
    new("WEB-02", DeviceType.WebServer),
    new("WS-03", DeviceType.Workstation),
    new("DB-02", DeviceType.DatabaseServer),
    new("WS-04", DeviceType.Workstation),
};

ITelemetryPublisher consolePub = new ConsolePublisher();
ITelemetryPublisher filePub = new FileJsonlPublisher(settings.OutputPath);

while (true)
{
    foreach (var d in devices)
    {
        var evnt = d.GenerateTelemetry(campaigns);
        await consolePub.PublishAsync(evnt);
        await filePub.PublishAsync(evnt);
    }

    await Task.Delay(settings.TickMs);
}