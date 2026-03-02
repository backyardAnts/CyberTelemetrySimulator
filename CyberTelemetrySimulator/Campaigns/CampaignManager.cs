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
    private readonly Random _random = new(); //random instance for generating random numbers
    private readonly Dictionary<string, AttackEpisode> _active = new(); //dictionary to track active attack episodes by device ID
    private readonly bool _incidentChainEnabled;
    private readonly bool _demoMode;
    private string? _incidentId;
    private string? _incidentDeviceId;
    private int _incidentStageIndex;
    private bool _incidentCompleted;

    // Config knobs (we’ll move to Config later)
    private readonly double _attackChancePerTick; // chance to start an attack each telemetry tick
    private readonly int _minDurationSec;
    private readonly int _maxDurationSec;

    public CampaignManager(
        double attackChancePerTick = 0.08,
        int minDurationSec = 20,
        int maxDurationSec = 90,
        bool incidentChainEnabled = false,
        bool demoMode = false)
    {
        _attackChancePerTick = attackChancePerTick;
        _minDurationSec = minDurationSec;
        _maxDurationSec = maxDurationSec;
        _incidentChainEnabled = incidentChainEnabled;
        _demoMode = demoMode;
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

        if (_incidentChainEnabled)
        {
            if (_incidentCompleted)
            {
                return null;
            }

            if (_incidentId == null)
            {
                var shouldStart = _demoMode || _random.NextDouble() <= _attackChancePerTick;

                if (!shouldStart)
                {
                    return null;
                }

                _incidentId = $"inc_{Guid.NewGuid():N}".Substring(0, 10);
                _incidentDeviceId = deviceId;
                _incidentStageIndex = 0;
            }

            if (deviceId != _incidentDeviceId)
            {
                return null;
            }

            var stage = GetIncidentStage(_incidentStageIndex);
            if (stage == null)
            {
                _incidentCompleted = true;
                return null;
            }

            _incidentStageIndex++;
            var episode = BuildEpisode(stage.Value, nowUtc, incidentId: _incidentId);
            _active[deviceId] = episode;
            return episode;
        }

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

    private (AttackType Type, AttackMode Mode, int MinSec, int MaxSec)? GetIncidentStage(int index)
    {
        return index switch
        {
            0 => (AttackType.PortScan, AttackMode.Stealth, 30, 90),
            1 => (AttackType.BruteForce, AttackMode.Loud, 20, 60),
            2 => (AttackType.Exfiltration, AttackMode.Stealth, 60, 180),
            _ => null
        };
    }

    private AttackEpisode BuildEpisode(
        (AttackType Type, AttackMode Mode, int MinSec, int MaxSec) stage,
        DateTime nowUtc,
        string? incidentId)
    {
        var duration = _random.Next(stage.MinSec, stage.MaxSec + 1);
        var intensity = RandomIntensity(stage.Mode, stage.Type);
        var (clusters, clusterSize) = SourceIpClustering(stage.Type, stage.Mode);
        var forceAfterHours = stage.Type == AttackType.Exfiltration && _random.NextDouble() < 0.6;

        return new AttackEpisode
        {
            AttackId = $"attack_{Guid.NewGuid():N}".Substring(0, 12),
            AttackType = stage.Type,
            Mode = stage.Mode,
            Intensity = intensity,
            SourceIpClusters = clusters,
            SourceIpsPerCluster = clusterSize,
            ForceAfterHours = forceAfterHours,
            IncidentId = incidentId,
            StartUtc = nowUtc,
            EndUtc = nowUtc.AddSeconds(duration)
        };
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
