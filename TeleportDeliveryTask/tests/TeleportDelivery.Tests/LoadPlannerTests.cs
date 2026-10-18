using TeleportDelivery.Data;
using TeleportDelivery.Model;
using TeleportDelivery.Planning;

namespace TeleportDelivery.Tests;

/// <summary>The naive greedy planner: most profitable first, into the first truck that fits.</summary>
public class LoadPlannerTests
{
    [Fact]
    public void MostProfitablePackagesGoFirst()
    {
        var fleet = new Fleet([new TruckCapacity(MaxWeightKg: 10, MaxVolumeM3: 1)]);
        Package[] packages =
        [
            new(Id: 0, WeightKg: 6, VolumeM3: 0.1, Profit: 50),
            new(Id: 1, WeightKg: 6, VolumeM3: 0.1, Profit: 100),
            new(Id: 2, WeightKg: 4, VolumeM3: 0.1, Profit: 10),
        ];

        var plan = new LoadPlanner(fleet).Plan(packages);

        // Only one of the 6 kg packages fits: the more profitable one. The small one fills the gap.
        Assert.Equal([LoadPlan.NotLoaded, 0, 0], plan.TruckOfPackage);
        Assert.Equal(110, plan.TotalProfit);
    }

    [Fact]
    public void FullTruck_PackageGoesToTheNextOne()
    {
        var fleet = new Fleet([new TruckCapacity(10, 1), new TruckCapacity(10, 1)]);
        Package[] packages =
        [
            new(Id: 0, WeightKg: 8, VolumeM3: 0.1, Profit: 30),
            new(Id: 1, WeightKg: 8, VolumeM3: 0.1, Profit: 20),
            new(Id: 2, WeightKg: 8, VolumeM3: 0.1, Profit: 10),
        ];

        var plan = new LoadPlanner(fleet).Plan(packages);

        Assert.Equal([0, 1, LoadPlan.NotLoaded], plan.TruckOfPackage);
    }

    [Fact]
    public void VolumeIsTheLimit()
    {
        var fleet = new Fleet([new TruckCapacity(MaxWeightKg: 100, MaxVolumeM3: 1)]);
        Package[] packages =
        [
            new(Id: 0, WeightKg: 1, VolumeM3: 0.7, Profit: 20),
            new(Id: 1, WeightKg: 1, VolumeM3: 0.7, Profit: 10),
        ];

        var plan = new LoadPlanner(fleet).Plan(packages);

        Assert.Equal([0, LoadPlan.NotLoaded], plan.TruckOfPackage);
    }

    [Fact]
    public void LightDay_EverythingIsLoaded()
    {
        var packages = PackageGenerator.Generate(10_000, ScenarioType.Typical, seed: 1);

        var plan = new LoadPlanner(Fleet.Default).Plan(packages);

        Assert.Equal(packages.Length, plan.LoadedCount);
    }

    [Theory]
    [InlineData(ScenarioType.Typical)]
    [InlineData(ScenarioType.Heavy)]
    [InlineData(ScenarioType.Mixed)]
    public void BusyDay_NoTruckIsOverloaded(ScenarioType scenarioType)
    {
        var packages = PackageGenerator.Generate(300_000, scenarioType, seed: 1);

        var plan = new LoadPlanner(Fleet.Default).Plan(packages);

        Assert.InRange(plan.LoadedCount, 1, packages.Length - 1);   // busy: something stays behind
        foreach (var truck in plan.Trucks)
        {
            Assert.True(truck.WeightKg <= truck.MaxWeightKg, $"{truck.WeightKg} kg > {truck.MaxWeightKg} kg");
            Assert.True(truck.VolumeM3 <= truck.MaxVolumeM3, $"{truck.VolumeM3} m³ > {truck.MaxVolumeM3} m³");
        }

        // The trucks carry exactly the packages assigned to them.
        var loaded = Enumerable.Range(0, packages.Length).Where(i => plan.TruckOfPackage[i] != LoadPlan.NotLoaded).ToArray();
        Assert.Equal(loaded.Length, plan.LoadedCount);
        Assert.Equal(loaded.Sum(i => packages[i].Profit), plan.TotalProfit, precision: 3);
    }
}
