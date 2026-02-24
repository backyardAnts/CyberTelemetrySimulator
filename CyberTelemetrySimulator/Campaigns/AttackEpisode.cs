using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Campaigns;

using CyberTelemetrySimulator.Models;

public class AttackEpisode
{
    public string AttackId { get; init; } = string.Empty;
    public AttackType AttackType { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }

    public bool IsActive(DateTime nowUtc) => nowUtc >= StartUtc && nowUtc < EndUtc;

    public double Progress01(DateTime nowUtc)
    {
        if (nowUtc <= StartUtc) return 0;
        if (nowUtc >= EndUtc) return 1;

        var total = (EndUtc - StartUtc).TotalSeconds;
        var done = (nowUtc - StartUtc).TotalSeconds;
        return total <= 0 ? 1 : done / total;
    }
}
