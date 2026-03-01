using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Attacks;

using CyberTelemetrySimulator.Campaigns;
using CyberTelemetrySimulator.Models;

public static class AttackApplier
{
    public static void Apply(Metrics m, AttackEpisode ep)
    {
        var p = ep.Progress01(DateTime.UtcNow); // 0 → 1
        // Use p to ramp effects smoothly
        switch (ep.AttackType)
        {
            case AttackType.PortScan:
                // Many ports over time + moderate packet rise
                m.UniquePortsAccessed = Math.Clamp(m.UniquePortsAccessed + (int)(20 + 80 * p), 0, 500);
                m.AveragePacketRate = Math.Max(0, m.AveragePacketRate + (int)(100 + 250 * p));
                m.ConnectionAttemptsPerSecond = Math.Max(0, m.ConnectionAttemptsPerSecond + (int)(30 + 120 * p));
                m.AverageConnectionDurationMs = Math.Clamp(m.AverageConnectionDurationMs - (int)(100 + 250 * p), 10, 10000);
                m.AverageCpuUsage = Math.Clamp(m.AverageCpuUsage + (int)(5 + 15 * p), 0, 100);
                break;

            case AttackType.BruteForce:
                // Failed logins spike + some CPU
                m.TotalFailedLogins = Math.Clamp(m.TotalFailedLogins + (int)(15 + 120 * p), 0, 10000);
                m.SuccessfulLogins = Math.Max(0, m.SuccessfulLogins - (int)(1 + 3 * p));
                m.UniqueSourceIps = Math.Clamp(m.UniqueSourceIps + (int)(1 + 6 * p), 1, 1000);
                m.AveragePacketRate = Math.Max(0, m.AveragePacketRate + (int)(50 + 120 * p));
                m.AverageCpuUsage = Math.Clamp(m.AverageCpuUsage + (int)(5 + 25 * p), 0, 100);
                break;

            case AttackType.DDoS:
                // Packet rate huge + CPU high
                m.AveragePacketRate = Math.Max(0, m.AveragePacketRate + (int)(700 + 2500 * p));
                m.NewConnectionsPerSecond = Math.Max(0, m.NewConnectionsPerSecond + (int)(200 + 600 * p));
                m.UniqueSourceIps = Math.Clamp(m.UniqueSourceIps + (int)(50 + 300 * p), 1, 5000);
                m.TrafficVolumeBytes = Math.Max(0, m.TrafficVolumeBytes + (int)(500000 + 1500000 * p));
                m.AverageCpuUsage = Math.Clamp(m.AverageCpuUsage + (int)(20 + 60 * p), 0, 100);
                break;

            case AttackType.Exfiltration:
                // Sustained moderate-high traffic (stealthier) + odd timing signal
                m.AveragePacketRate = Math.Max(0, m.AveragePacketRate + (int)(200 + 500 * p));
                m.OutgoingBytes = Math.Max(0, m.OutgoingBytes + (int)(200000 + 800000 * p));
                m.IncomingBytes = Math.Max(0, m.IncomingBytes - (int)(20000 + 50000 * p));
                m.TrafficVolumeBytes = Math.Max(0, m.TrafficVolumeBytes + (int)(300000 + 900000 * p));
                m.AfterHoursActivity = 1;
                m.AverageCpuUsage = Math.Clamp(m.AverageCpuUsage + (int)(5 + 15 * p), 0, 100);
                break;

            case AttackType.Normal:
            default:
                break;
        }
    }
}
