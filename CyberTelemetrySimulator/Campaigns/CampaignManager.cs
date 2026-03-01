using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Campaigns;
//owns the list/queue of AttackEpisodes, decides what’s active right now, and tells AttackApplier what to apply.
using CyberTelemetrySimulator.Models;

public class CampaignManager
{
    private readonly Random _random = new();
    private readonly Dictionary<string, AttackEpisode> _active = new();

    // Config knobs (we’ll move to Config later)
    private readonly double _attackChancePerTick; // chance to start an attack each telemetry tick
    private readonly int _minDurationSec;
    private readonly int _maxDurationSec;

    public CampaignManager(double attackChancePerTick = 0.08, int minDurationSec = 20, int maxDurationSec = 90)
    {
        _attackChancePerTick = attackChancePerTick;
        _minDurationSec = minDurationSec;
        _maxDurationSec = maxDurationSec;
    }

    public AttackEpisode? GetActiveEpisode(string deviceId, DateTime nowUtc)
    {
        if (_active.TryGetValue(deviceId, out var ep))
        {
            if (ep.IsActive(nowUtc)) return ep;
            _active.Remove(deviceId); // expired
        }
        return null;
    }

    public AttackEpisode? TryStartEpisode(string deviceId, DateTime nowUtc)
    {
        // If one already active, do nothing
        if (GetActiveEpisode(deviceId, nowUtc) != null) return null;

        // Start a new one with some probability
        if (_random.NextDouble() > _attackChancePerTick) return null;

        var duration = _random.Next(_minDurationSec, _maxDurationSec + 1);
        var type = RandomAttackType();
        var mode = RandomAttackMode(type);
        var intensity = RandomIntensity(mode, type);
        var (clusters, clusterSize) = SourceIpClustering(type, mode);
        var forceAfterHours = type == AttackType.Exfiltration && _random.NextDouble() < (mode == AttackMode.Stealth ? 0.7 : 0.4);

        var ep = new AttackEpisode
        {
            AttackId = $"attack_{Guid.NewGuid():N}".Substring(0, 12),
            AttackType = type,
            Mode = mode,
            Intensity = intensity,
            SourceIpClusters = clusters,
            SourceIpsPerCluster = clusterSize,
            ForceAfterHours = forceAfterHours,
            StartUtc = nowUtc,
            EndUtc = nowUtc.AddSeconds(duration)
        };

        _active[deviceId] = ep;
        return ep;
    }

    private AttackType RandomAttackType()
    {
        // exclude Normal
        var options = new[] { AttackType.PortScan, AttackType.BruteForce, AttackType.DDoS, AttackType.Exfiltration };
        return options[_random.Next(options.Length)];
    }

    private AttackMode RandomAttackMode(AttackType type)
    {
        return type switch
        {
            AttackType.Exfiltration => _random.NextDouble() < 0.7 ? AttackMode.Stealth : AttackMode.Loud,
            AttackType.DDoS => _random.NextDouble() < 0.6 ? AttackMode.Loud : AttackMode.Stealth,
            AttackType.BruteForce => _random.NextDouble() < 0.55 ? AttackMode.Loud : AttackMode.Stealth,
            AttackType.PortScan => _random.NextDouble() < 0.5 ? AttackMode.Loud : AttackMode.Stealth,
            _ => AttackMode.Loud
        };
    }

    private double RandomIntensity(AttackMode mode, AttackType type)
    {
        var baseIntensity = mode == AttackMode.Loud ? 1.0 : 0.45;
        var variability = mode == AttackMode.Loud ? 0.5 : 0.25;
        if (type == AttackType.DDoS && mode == AttackMode.Loud)
        {
            baseIntensity = 1.2;
            variability = 0.6;
        }

        return baseIntensity + _random.NextDouble() * variability;
    }

    private (int Clusters, int ClusterSize) SourceIpClustering(AttackType type, AttackMode mode)
    {
        return type switch
        {
            AttackType.DDoS => mode == AttackMode.Loud
                ? (_random.Next(6, 14), _random.Next(60, 200))
                : (_random.Next(3, 8), _random.Next(20, 80)),
            AttackType.BruteForce => mode == AttackMode.Loud
                ? (_random.Next(2, 6), _random.Next(8, 25))
                : (_random.Next(4, 10), _random.Next(4, 12)),
            _ => (1, 1)
        };
    }
}
