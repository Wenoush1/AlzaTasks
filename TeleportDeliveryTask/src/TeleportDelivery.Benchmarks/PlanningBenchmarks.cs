using BenchmarkDotNet.Attributes;
using TeleportDelivery.Data;
using TeleportDelivery.Model;
using TeleportDelivery.Planning;
using TeleportDelivery.Pricing;

namespace TeleportDelivery.Benchmarks;

/// <summary>
/// Planning one trip of the full fleet on a busy day. <see cref="Pricing"/> is the LP step alone,
/// so <see cref="Plan"/> minus <see cref="Pricing"/> is roughly what ranking and loading cost.
/// </summary>
[MemoryDiagnoser]
public class PlanningBenchmarks
{
    private readonly Fleet _fleet = Fleet.Default;
    private LoadPlanner _planner = null!;
    private Package[] _packages = null!;

    [Params(ScenarioType.Typical, ScenarioType.Heavy, ScenarioType.Mixed)]
    public ScenarioType ScenarioType { get; set; }

    [Params(300_000, 1_000_000)]
    public int PackageCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _planner = new LoadPlanner(_fleet);
        _packages = PackageGenerator.Generate(PackageCount, ScenarioType, seed: 1);
    }

    [Benchmark]
    public LoadPlan Plan() => _planner.Plan(_packages);

    [Benchmark]
    public CapacityPrices Pricing() => CapacityPricing.Solve(_packages, _fleet.TotalWeightKg, _fleet.TotalVolumeM3);
}
