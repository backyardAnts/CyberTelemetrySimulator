namespace CyberTelemetrySimulator.Config;

public class SimulatorSettings
{
    public int TickMs { get; set; } = 2000;
    public double AttackChancePerTick { get; set; } = 0.10;
    public int MinDurationSec { get; set; } = 20;
    public int MaxDurationSec { get; set; } = 60;
    public string OutputPath { get; set; } = "data/raw-telemetry.jsonl";
}