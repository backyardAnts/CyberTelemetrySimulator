using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Devices;

using CyberTelemetrySimulator.Models;

public class DeviceProfile
{
    public DeviceType DeviceType { get; init; }

    // Normal ranges (baseline behavior)
    public (int Min, int Max) PacketRateRange { get; init; } //tupples used to store the range of each metric
    public (int Min, int Max) FailedLoginsRange { get; init; }
    public (int Min, int Max) UniquePortsRange { get; init; }
    public (int Min, int Max) CpuUsageRange { get; init; }

    public static DeviceProfile For(DeviceType type) => type switch // there is a switch so it can choose what type of device profile to create based on the DeviceType passed in. Each case creates a new DeviceProfile with specific ranges for the metrics.
    {
        DeviceType.Workstation => new DeviceProfile
        {
            DeviceType = type,
            PacketRateRange = (50, 180),
            FailedLoginsRange = (0, 3),
            UniquePortsRange = (1, 6),
            CpuUsageRange = (5, 35)
        },

        DeviceType.WebServer => new DeviceProfile
        {
            DeviceType = type,
            PacketRateRange = (300, 900),
            FailedLoginsRange = (0, 2),
            UniquePortsRange = (2, 10),
            CpuUsageRange = (15, 60)
        },

        DeviceType.DatabaseServer => new DeviceProfile
        {
            DeviceType = type,
            PacketRateRange = (120, 500),
            FailedLoginsRange = (0, 2),
            UniquePortsRange = (1, 4),
            CpuUsageRange = (25, 75)
        },

        DeviceType.IoTDevice => new DeviceProfile
        {
            DeviceType = type,
            PacketRateRange = (10, 60),
            FailedLoginsRange = (0, 1),
            UniquePortsRange = (1, 3),
            CpuUsageRange = (1, 20)
        },

        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown DeviceType")
    };
}
