namespace CyberTelemetrySimulator.Publishers;

using CyberTelemetrySimulator.Models;

public interface ITelemetryPublisher
{
    Task PublishAsync(TelemetryEvent evnt);
}