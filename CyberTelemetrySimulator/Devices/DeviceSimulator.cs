using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CyberTelemetrySimulator.Campaigns;
using CyberTelemetrySimulator.Attacks;
namespace CyberTelemetrySimulator.Devices;

using CyberTelemetrySimulator.Models;

public class DeviceSimulator
{
    private readonly Random _random = new();
    private readonly DeviceProfile _profile;

    public string DeviceId { get; }
    public DeviceType DeviceType { get; }

    public DeviceSimulator(string deviceId, DeviceType deviceType)
    {
        DeviceId = deviceId;
        DeviceType = deviceType;
        _profile = DeviceProfile.For(deviceType);
    }

    public TelemetryEvent GenerateNormalTelemetry()
    {
        var now = DateTime.UtcNow;

        var metrics = new Metrics
        {
            AveragePacketRate = NextInRange(_profile.PacketRateRange),
            TotalFailedLogins = NextInRange(_profile.FailedLoginsRange),
            UniquePortsAccessed = NextInRange(_profile.UniquePortsRange),
            AverageCpuUsage = NextInRange(_profile.CpuUsageRange),
            TimeOfDay = now.Hour
        };

        // Small noise to make it less “robotic”
        AddNoise(metrics);

        return new TelemetryEvent
        {
            Timestamp = now,
            DeviceId = DeviceId,
            DeviceType = DeviceType,
            Metrics = metrics,
            Label = AttackType.Normal,
            AttackId = null
        };
    }

    private int NextInRange((int Min, int Max) range)
        => _random.Next(range.Min, range.Max + 1);

    private void AddNoise(Metrics m)
    {
        // Tiny random wiggle
        m.AveragePacketRate = Math.Max(0, m.AveragePacketRate + _random.Next(-10, 11));
        m.AverageCpuUsage = Math.Clamp(m.AverageCpuUsage + _random.Next(-3, 4), 0, 100);
    }
    public TelemetryEvent GenerateTelemetry(CampaignManager campaigns)
    {
        var now = DateTime.UtcNow;

        // 1) Generate normal baseline
        var metrics = new Metrics
        {
            AveragePacketRate = NextInRange(_profile.PacketRateRange),
            TotalFailedLogins = NextInRange(_profile.FailedLoginsRange),
            UniquePortsAccessed = NextInRange(_profile.UniquePortsRange),
            AverageCpuUsage = NextInRange(_profile.CpuUsageRange),
            TimeOfDay = now.Hour
        };

        AddNoise(metrics);

        // 2) Check if there is an active attack episode, or start one
        var ep = campaigns.GetActiveEpisode(DeviceId, now) ?? campaigns.TryStartEpisode(DeviceId, now);

        // 3) Apply attack effects if active
        var label = AttackType.Normal;
        string? attackId = null;

        if (ep != null && ep.IsActive(now))
        {
            AttackApplier.Apply(metrics, ep);
            label = ep.AttackType;
            attackId = ep.AttackId;
        }

        // 4) Build event
        return new TelemetryEvent
        {
            Timestamp = now,
            DeviceId = DeviceId,
            DeviceType = DeviceType,
            Metrics = metrics,
            Label = label,
            AttackId = attackId
        };
    }
}
