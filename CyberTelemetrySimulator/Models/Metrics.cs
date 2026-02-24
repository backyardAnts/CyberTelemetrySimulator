using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberTelemetrySimulator.Models;
public class Metrics
{
    public double AveragePacketRate { get; set; }
    public int TotalFailedLogins { get; set; }
    public int UniquePortsAccessed { get; set; }
    public double AverageCpuUsage { get; set; }
    public int TimeOfDay { get; set; }
}
