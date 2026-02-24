using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Campaigns;

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

        var ep = new AttackEpisode
        {
            AttackId = $"attack_{Guid.NewGuid():N}".Substring(0, 12),
            AttackType = type,
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
}
