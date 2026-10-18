using TeleportDelivery.Model;
using TeleportDelivery.Pricing;

namespace TeleportDelivery.Tests;

/// <summary>Shadow prices and the upper bound on small cases that can be solved by hand.</summary>
public class CapacityPricingTests
{
    [Fact]
    public void EverythingFits_CapacityIsFree()
    {
        Package[] packages =
        [
            new(Id: 0, WeightKg: 2, VolumeM3: 1, Profit: 60),
            new(Id: 1, WeightKg: 3, VolumeM3: 1, Profit: 30),
        ];

        var prices = CapacityPricing.Solve(packages, value: [60, 30], weightCapacity: 10, volumeCapacity: 10);

        Assert.Equal(0, prices.PerKg);
        Assert.Equal(0, prices.PerM3);
        Assert.Equal(90, prices.UpperBound, precision: 6);
    }

    /// <summary>
    /// Only weight is scarce. The LP loads package 0 whole and 4/6 of package 1: 60 + 20 = 80 CZK.
    /// The price of 1 kg is what the last, partly loaded package earns per kg: 30 / 6 = 5 CZK.
    /// </summary>
    [Fact]
    public void WeightIsScarce_PriceIsProfitPerKgOfTheLastPackage()
    {
        Package[] packages =
        [
            new(Id: 0, WeightKg: 6, VolumeM3: 1, Profit: 60),
            new(Id: 1, WeightKg: 6, VolumeM3: 1, Profit: 30),
        ];

        var prices = CapacityPricing.Solve(packages, value: [60, 30], weightCapacity: 10, volumeCapacity: 100);

        Assert.Equal(5, prices.PerKg, precision: 6);
        Assert.Equal(0, prices.PerM3);
        Assert.Equal(80, prices.UpperBound, precision: 6);
    }
}
