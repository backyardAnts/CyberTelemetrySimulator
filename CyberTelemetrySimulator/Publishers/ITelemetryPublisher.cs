namespace CyberTelemetrySimulator.Publishers;

using CyberTelemetrySimulator.Models;

public interface ITelemetryPublisher
{
    //lol
    Task PublishAsync(TelemetryEvent evnt);
}