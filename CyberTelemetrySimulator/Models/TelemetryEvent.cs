using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Models;
public class TelemetryEvent
{
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; }
    public Metrics Metrics { get; set; } = new();
    public AttackType Label { get; set; } = AttackType.Normal;
    public string? AttackId { get; set; }
}