using System.Text.Json;
using CyberTelemetrySimulator.Config;
using CyberTelemetrySimulator.Campaigns;
using CyberTelemetrySimulator.Devices;
using CyberTelemetrySimulator.Detection;
using CyberTelemetrySimulator.Models;
using CyberTelemetrySimulator.Publishers;
using CyberTelemetrySimulator.Validation;

var settingsPath = Path.Combine(AppContext.BaseDirectory, "Config", "simulatorSettings.json"); //used to load conf settings from json file
var settingsJson = File.ReadAllText(settingsPath); //read the JSON file content into a string
var settings = JsonSerializer.Deserialize<SimulatorSettings>(settingsJson) ?? new SimulatorSettings(); //convert the JSON string into a SimulatorSettings object, if deserialization fails, create a new SimulatorSettings with default values

if (args.Contains("--self-check", StringComparer.OrdinalIgnoreCase))
{
    TelemetrySelfCheck.Run();
    return;
}

var socMode = args.Contains("--soc", StringComparer.OrdinalIgnoreCase);
var demoMode = args.Contains("--demo", StringComparer.OrdinalIgnoreCase);

var campaigns = new CampaignManager( //attack scheduling manager, it will decide when to start an attack and what type based on the settings
    attackChancePerTick: settings.AttackChancePerTick, //gets the info from the settings file 
    minDurationSec: settings.MinDurationSec,
    maxDurationSec: settings.MaxDurationSec,
    incidentChainEnabled: socMode || demoMode,
    demoMode: demoMode
);

var devices = new List<DeviceSimulator> //list of simulated devices, each with a unique ID and type. The DeviceSimulator class will use the DeviceProfile to generate telemetry data based on the device type and any active attack episodes.

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

ITelemetryPublisher consolePub = new ConsolePublisher(); //publisher that outputs telemetry events to the console
ITelemetryPublisher filePub = new FileJsonlPublisher(settings.OutputPath); //publisher that appends telemetry events as JSON lines to a specified file. The OutputPath is obtained from the settings, allowing for flexible configuration of where the telemetry data should be stored.
var alertPublisher = socMode ? new AlertJsonlPublisher("data/alerts.jsonl") : null;
var deviceStates = socMode ? new Dictionary<string, DeviceSecurityState>() : null;


while (true) //infinite loop that simulates the telemetry generation process. In each iteration, it goes through all the devices, generates a telemetry event for each device based on the current active attack episodes (if any), and publishes the event using both the console and file publishers.
{
    foreach (var d in devices)
    {
        var evnt = d.GenerateTelemetry(campaigns);
        await filePub.PublishAsync(evnt); //publish content on file, the file publisher will append the telemetry event as a JSON line

        if (!socMode)
        {
            await consolePub.PublishAsync(evnt); //publish content on console
            continue;
        }

        var detection = DetectionEngine.Evaluate(evnt);
        var state = MapState(detection.RiskScore);
        var emoji = state switch
        {
            DeviceSecurityState.Normal => "🟢",
            DeviceSecurityState.Suspicious => "🟠",
            DeviceSecurityState.UnderAttack => "🔴",
            _ => "🟢"
        };
        var reasons = detection.Reasons.Length == 0 ? "none" : string.Join("; ", detection.Reasons);
        var suspectedLabel = detection.SuspectedType?.ToString() ?? "None";
        Console.WriteLine($"[{emoji}] {evnt.DeviceId} {evnt.DeviceType} Risk={detection.RiskScore} Suspected={suspectedLabel} Reasons={reasons}");

        if (deviceStates != null)
        {
            deviceStates.TryGetValue(evnt.DeviceId, out var previousState);
            if (previousState != state)
            {
                deviceStates[evnt.DeviceId] = state;
            }

            if (previousState != state || detection.RiskScore >= 70)
            {
                var alert = new SecurityAlert
                {
                    AlertId = $"alert_{Guid.NewGuid():N}".Substring(0, 12),
                    Timestamp = evnt.Timestamp,
                    DeviceId = evnt.DeviceId,
                    DeviceType = evnt.DeviceType,
                    IncidentId = evnt.IncidentId,
                    RiskScore = detection.RiskScore,
                    Severity = detection.RiskScore >= 70 ? "Critical" : "Suspicious",
                    SuspectedType = detection.SuspectedType,
                    Reasons = detection.Reasons
                };
                if (alertPublisher != null)
                {
                    await alertPublisher.PublishAsync(alert);
                }
            }
        }
    }

    await Task.Delay(settings.TickMs); //wait for a specified amount of time 
}

static DeviceSecurityState MapState(int riskScore)
{
    if (riskScore >= 70) return DeviceSecurityState.UnderAttack;
    if (riskScore >= 40) return DeviceSecurityState.Suspicious;
    return DeviceSecurityState.Normal;
}
