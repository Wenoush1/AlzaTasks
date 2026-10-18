using TeleportDelivery.Data;
using TeleportDelivery.Model;
using TeleportDelivery.Planning;

namespace TeleportDelivery.Tests;

/// <summary>The greedy planner: most profitable per capacity first, into the most balanced truck.</summary>
public class LoadPlannerTests
{
    [Fact]
    public void MostProfitablePerCapacityGoFirst()
    {
        var fleet = new Fleet([new TruckCapacity(MaxWeightKg: 10, MaxVolumeM3: 10)]);
        Package[] packages =
        [
            new(Id: 0, WeightKg: 8, VolumeM3: 0.1, Profit: 90),
            new(Id: 1, WeightKg: 5, VolumeM3: 1, Profit: 70),
            new(Id: 2, WeightKg: 5, VolumeM3: 1, Profit: 70),
        ];

        var plan = new LoadPlanner(fleet).Plan(packages);

        // The 8 kg package is the most profitable, but the two 5 kg ones earn more per kilogram used.
        Assert.Equal([LoadPlan.NotLoaded, 0, 0], plan.TruckOfPackage);
        Assert.Equal(140, plan.TotalProfit);
    }

    [Fact]
    public void BulkyPackageGoesWhereSpaceIsLeft()
    {
        var fleet = new Fleet([new TruckCapacity(10, 1), new TruckCapacity(10, 1)]);
        Package[] packages =
        [
            new(Id: 0, WeightKg: 8, VolumeM3: 0.1, Profit: 100),
            new(Id: 1, WeightKg: 1, VolumeM3: 0.8, Profit: 90),
        ];

        var plan = new LoadPlanner(fleet).Plan(packages);

        // Both fit in truck 0, but there it would be left with almost no room in either dimension.
        Assert.Equal([0, 1], plan.TruckOfPackage);
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
        Assert.True(plan.TotalProfit <= plan.UpperBound, $"profit {plan.TotalProfit:N0} above the bound {plan.UpperBound:N0}");
    }
    
    [Theory]
    [InlineData(ScenarioType.Typical)]
    [InlineData(ScenarioType.Heavy)]
    [InlineData(ScenarioType.Mixed)]
    public void BusyDay_NoSkippedPackageFitsAnywhere(ScenarioType scenarioType)
    {
        var packages = PackageGenerator.Generate(300_000, scenarioType, seed: 1);

        var plan = new LoadPlanner(Fleet.Default).Plan(packages);

        for (var i = 0; i < packages.Length; i++)
        {
            if (plan.TruckOfPackage[i] != LoadPlan.NotLoaded) continue;
            var package = packages[i];
            Assert.DoesNotContain(plan.Trucks, truck => truck.Fits(package));
        }
    }
}
