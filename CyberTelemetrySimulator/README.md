# CyberTelemetrySimulator

Generates labeled telemetry events for multiple device types and persists them as JSONL. The simulator models normal baselines plus attack-driven anomalies so you can prototype detection pipelines.

## Realism upgrades
- Persistent per-device baselines with slow drift instead of full re-randomization every tick.
- Poisson count modeling, log-normal byte volumes, and AR(1) smoothing for CPU/packet rates.
- Time-of-day and weekday/weekend activity multipliers by device type.
- Internal-consistency rules for traffic totals and derived ratios.
- Loud vs stealth attack modes with correlated metric changes.

## Run
```
dotnet run --project CyberTelemetrySimulator
```

## Self-check
```
dotnet run --project CyberTelemetrySimulator -- --self-check
```
