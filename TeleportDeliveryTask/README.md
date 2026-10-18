# Teleport Delivery: planning truck loads

Before each trip, this library decides which waiting packages go out and which truck carries each one. It aims for
the highest profit while making sure no package waits in the warehouse too long.

- **Fast:** 300,000 packages are planned in about 0.15 s on a single CPU core (1,000,000 in about 0.4 s).
- **Close to optimal:** each plan comes with an upper bound that no plan can beat. On the test data, plans are at
  most 0.007 % below it.
- **Fair:** no package waits more than 2 days.

## The idea

Each truck has two limits, **5,500 kg** and **7 m³**, and a package goes whole or not at all. This is a multiple
knapsack problem with two dimensions: too big to solve exactly, but packages are tiny compared to a truck, so a good
greedy order is almost optimal. The hard part is the order, because which limit is scarce changes from day to day.

1. **Price the capacity.** Treat the fleet as one giant truck and allow fractions of packages. This simplified
   problem (the LP relaxation) is solved exactly and fast. It gives the value of 1 kg and 1 m³ on this trip (shadow
   prices) and an upper bound on any real plan.
2. **Rank** packages by value / (price per kg × weight + price per m³ × volume). The prices change every trip, so
   the ranking adapts to whatever is scarce that day.
3. **Load** in that order, each package into the truck where weight and volume stay the most balanced.

**Waiting.** A package's value is its profit plus **50 CZK for every trip it has missed**, and a package that has
missed **4 trips (2 days) must go** next (`WaitingPolicy`). Without this, cheap bulky packages would keep being
pushed back by new, more profitable ones.

## Results

Synthetic data (`PackageGenerator`), 300,000 packages, 120 trucks:

| Day | Scarce limit | Below upper bound |
|---|---|---:|
| Typical goods | volume | 0.001 % |
| Heavy goods | weight | 0.001 % |
| Heavy and bulky mixed | both | 0.007 % |

For comparison, a fixed 1:1 weighting of kg and m³ is up to 2.9 % below the bound on mixed days.

A 4-week warehouse simulation (two trips a day, more arriving than fits on most days):

| Policy | Waited 2+ days | Longest wait |
|---|---:|---:|
| profit only | 1.89 % | 6.5 days |
| **delay cost + 4-trip limit** | **0.54 %** | **2 days** |

## Assumptions

- Any package can go into any truck; volumes simply add up (no 3D packing).
- Waiting is counted in missed trips.
- Weight, volume and profit are positive and known.

## Running it

Requires .NET 10 or newer.

```
dotnet test -c Release                                              # tests, a few seconds
dotnet run -c Release --project src/TeleportDelivery.Benchmarks     # benchmarks, a few minutes
```

| Project | Contents |
|---|---|
| `src/TeleportDelivery.Core` | the algorithm: `Model/`, `Pricing/`, `Planning/`, `Data/` (synthetic data) |
| `src/TeleportDelivery.Benchmarks` | speed and memory (BenchmarkDotNet) |
| `tests/TeleportDelivery.Tests` | correctness, optimality gap, warehouse simulation |
