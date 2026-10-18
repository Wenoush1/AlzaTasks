using TeleportDelivery.Model;

namespace TeleportDelivery.Pricing;

/// <summary>
/// Solves the LP relaxation of the problem with all trucks merged into one "virtual truck"
/// (W = total kg, V = total m³ of the fleet) and packages allowed to be loaded fractionally:
/// <code>
///   max Σ pᵢ·xᵢ   subject to   Σ wᵢ·xᵢ ≤ W,   Σ vᵢ·xᵢ ≤ V,   0 ≤ xᵢ ≤ 1
/// </code>
/// pᵢ is what loading package i is worth (its profit plus the cost of making it wait longer).
/// It is solved through its Lagrangian dual, which for an LP has the same optimal value:
/// <code>
///   min over α, β ≥ 0 of   L(α, β) = α·W + β·V + Σ max(0, pᵢ − α·wᵢ − β·vᵢ)
/// </code>
/// α (CZK/kg) and β (CZK/m³) are the shadow prices. Any α, β ≥ 0 give a valid upper bound on
/// the value of every real plan; the minimising pair gives the tightest one.
/// <para>
/// For a fixed α the best β is the critical ratio of a 1-D fractional knapsack over volume,
/// found by weighted quickselect in expected O(n). g(α) = min_β L(α, β) is convex, so a
/// golden-section search over α converges to the optimum. As the search interval shrinks,
/// packages whose decision is the same for every price pair still in play are fixed and dropped,
/// so later iterations touch only the few packages near the decision boundary.
/// </para>
/// </summary>
public static class CapacityPricing
{
    private const int MaxIterations = 100;
    private const double RelativeTolerance = 1e-9;
    private const double InvGoldenRatio = 0.6180339887498949;

    /// <param name="packages">Packages competing for the capacity.</param>
    /// <param name="value">What loading each package is worth (CZK); <c>value[i]</c> belongs to <c>packages[i]</c>.</param>
    /// <param name="weightCapacity">Total free weight of the fleet (kg).</param>
    /// <param name="volumeCapacity">Total free volume of the fleet (m³).</param>
    public static CapacityPrices Solve(Package[] packages, double[] value, double weightCapacity, double volumeCapacity)
    {
        var dual = new LagrangianDual(packages, value, Math.Max(0, weightCapacity), Math.Max(0, volumeCapacity));

        // Adding the volume limit can only make weight less scarce (volume competes for the same
        // packages), so the weight price with volume ignored bounds the search from above.
        var maxPerKg = dual.WeightPriceIgnoringVolume();

        Evaluation lower = dual.Evaluate(perKg: 0), best = lower;
        if (maxPerKg > 0)
        {
            // Golden-section search. The minimum of the convex g(α) stays within [lower, upper]; two probes
            // split that interval in the golden ratio, so every step drops one end and reuses one probe:
            //   lower ── lowerProbe ── upperProbe ── upper
            var upper = dual.Evaluate(maxPerKg);
            dual.FixDecidedPackages(lower, upper);
            var lowerProbe = dual.Evaluate(GoldenPoint(from: upper, to: lower));
            var upperProbe = dual.Evaluate(GoldenPoint(from: lower, to: upper));

            var tolerance = RelativeTolerance * maxPerKg;
            for (var i = 0; i < MaxIterations && upper.PerKg - lower.PerKg > tolerance; i++)
            {
                if (lowerProbe.Bound <= upperProbe.Bound)
                {
                    // The minimum is left of upperProbe: it becomes the new upper end.
                    (upper, upperProbe) = (upperProbe, lowerProbe);
                    lowerProbe = dual.Evaluate(GoldenPoint(from: upper, to: lower));
                }
                else
                {
                    // The minimum is right of lowerProbe: it becomes the new lower end.
                    (lower, lowerProbe) = (lowerProbe, upperProbe);
                    upperProbe = dual.Evaluate(GoldenPoint(from: lower, to: upper));
                }
                dual.FixDecidedPackages(lower, upper);
            }
            best = new[] { best, lower, lowerProbe, upperProbe, upper }.MinBy(e => e.Bound);
        }

        // The search ran on a shrinking subset; recompute the bound over all packages so that it
        // is valid no matter what (weak duality holds for any non-negative prices).
        return new CapacityPrices(best.PerKg, best.PerM3, dual.ExactBound(best.PerKg, best.PerM3));
    }

    /// <summary>The weight price that lies the golden-ratio share of the way from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static double GoldenPoint(Evaluation from, Evaluation to) => from.PerKg + InvGoldenRatio * (to.PerKg - from.PerKg);

    private readonly record struct Evaluation(double PerKg, double PerM3, double Bound);

    /// <summary>Evaluates the dual function on the packages whose fate is still open.</summary>
    private sealed class LagrangianDual(
        Package[] packages,
        double[] value,
        double weightCapacity,
        double volumeCapacity)
    {
        private readonly Random _random = new(Seed: 2026);

        private readonly int[] _open = Enumerable.Range(0, packages.Length).ToArray();      // packages whose decision still depends on the prices
        private int _openCount = packages.Length;
        private double _fixedValue, _fixedWeight, _fixedVolume;   // sums over packages surely loaded

        private readonly Item[] _items = new Item[packages.Length];    // scratch buffer for the fractional knapsack

        private readonly record struct Item(double Ratio, double Size);

        /// <summary>Shadow price of weight if volume were unlimited (classic 1-D fractional knapsack).</summary>
        public double WeightPriceIgnoringVolume()
        {
            for (var i = 0; i < packages.Length; i++)
                _items[i] = new Item(value[i] / packages[i].WeightKg, packages[i].WeightKg);
            return CriticalRatio(_items.AsSpan(), weightCapacity);
        }

        /// <summary>For a weight price α finds the best volume price β and the bound g(α) = L(α, β).</summary>
        public Evaluation Evaluate(double perKg)
        {
            // What is left of each package's value after paying for its weight, per m³.
            var count = 0;
            for (var k = 0; k < _openCount; k++)
            {
                var i = _open[k];
                double valueAfterWeight = value[i] - perKg * packages[i].WeightKg;
                if (valueAfterWeight > 0)
                    _items[count++] = new Item(valueAfterWeight / packages[i].VolumeM3, packages[i].VolumeM3);
            }
            Span<Item> items = _items.AsSpan(0, count);
            double perM3 = CriticalRatio(items, Math.Max(0, volumeCapacity - _fixedVolume));

            double bound = perKg * weightCapacity + perM3 * volumeCapacity
                         + _fixedValue - perKg * _fixedWeight - perM3 * _fixedVolume;
            foreach (Item item in items)
                bound += Math.Max(0, (item.Ratio - perM3) * item.Size);   // = max(0, p − α·w − β·v)

            return new Evaluation(perKg, perM3, bound);
        }

        /// <summary>
        /// The optimum lies within α ∈ [lower, upper]; β falls as α rises, so β ∈ [β(upper), β(lower)].
        /// A package unprofitable even at the lowest prices, or profitable even at the highest,
        /// is decided: it is dropped from further evaluations (the profitable ones are summed up).
        /// </summary>
        public void FixDecidedPackages(Evaluation lower, Evaluation upper)
        {
            var kept = 0;
            for (var k = 0; k < _openCount; k++)
            {
                var i = _open[k];
                ref readonly Package p = ref packages[i];
                if (value[i] - lower.PerKg * p.WeightKg - upper.PerM3 * p.VolumeM3 <= 0) continue;   // never pays off
                if (value[i] - upper.PerKg * p.WeightKg - lower.PerM3 * p.VolumeM3 > 0)              // always pays off
                {
                    _fixedValue += value[i];
                    _fixedWeight += p.WeightKg;
                    _fixedVolume += p.VolumeM3;
                    continue;
                }
                _open[kept++] = i;
            }
            _openCount = kept;
        }

        /// <summary>L(α, β) over all packages.</summary>
        public double ExactBound(double perKg, double perM3)
        {
            var bound = perKg * weightCapacity + perM3 * volumeCapacity;
            for (var i = 0; i < packages.Length; i++)
                bound += Math.Max(0, value[i] - perKg * packages[i].WeightKg - perM3 * packages[i].VolumeM3);
            return bound;
        }

        /// <summary>
        /// Finds the ratio t at which items taken by decreasing ratio fill the capacity:
        /// Σ size(ratio &gt; t) ≤ capacity ≤ Σ size(ratio ≥ t). Returns 0 if everything fits.
        /// Weighted quickselect with three-way partitioning: expected O(n), no sorting.
        /// </summary>
        private double CriticalRatio(Span<Item> items, double capacity)
        {
            // The critical ratio lies among items[start..end). `remaining` is the capacity those items still
            // have to fill, after every item with a higher ratio has been taken.
            var start = 0;
            var end = items.Length;
            var remaining = capacity;

            while (start < end)
            {
                var pivot = items[_random.Next(start, end)].Ratio;

                // Partition [start, end) into three runs:
                //   [start, aboveEnd) ratio > pivot   [aboveEnd, belowStart) ratio == pivot   [belowStart, end) ratio < pivot
                int aboveEnd = start, next = start, belowStart = end;
                double sizeAbove = 0, sizeAtPivot = 0;
                while (next < belowStart)
                {
                    switch (items[next].Ratio.CompareTo(pivot))
                    {
                        case > 0:
                            sizeAbove += items[next].Size;
                            Swap(items, next++, aboveEnd++);
                            break;
                        case < 0:
                            Swap(items, next, --belowStart);
                            break;
                        default:
                            sizeAtPivot += items[next].Size;
                            next++;
                            break;
                    }
                }

                if (sizeAbove > remaining)
                {
                    end = aboveEnd;                           // capacity runs out above the pivot: search there
                }
                else if (sizeAbove + sizeAtPivot >= remaining)
                {
                    return pivot;                             // ... exactly at the pivot
                }
                else
                {
                    remaining -= sizeAbove + sizeAtPivot;     // ... below it: take everything down to the pivot
                    start = belowStart;
                }
            }
            return 0;
        }

        private static void Swap(Span<Item> items, int i, int j) => (items[i], items[j]) = (items[j], items[i]);
    }
}
