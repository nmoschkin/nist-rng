# NistRNG

`NistRNG` is a drop-in replacement for `System.Random` that periodically reseeds itself using live entropy from the [NIST Randomness Beacon](https://beacon.nist.gov). It provides higher-quality pseudorandomness by sourcing unpredictable seed material from an external, verifiable source.

This library supports event-driven updates, asynchronous initialization, and custom entropy decoding strategies via a pluggable factory interface.

---

## Features

- Reseeds periodically from **true entropy** published by the U.S. National Institute of Standards and Technology (NIST)
- Functions just like `System.Random`
- Easy to integrate into existing applications
- Thread-safe and event-driven
- Extendable via `IRandomFactory`

---

## NistRandom Test Results (Latest Sample: 100,000 integers)

| Test Name             | Result         | Notes                                              |
|----------------------|----------------|----------------------------------------------------|
| Shannon Entropy      | 10.888 bits    | Very high entropy (max possible: 11.0 bits)       |
| Chi-Square Test      | Passed         | Uniform distribution across 256 bins               |
| Digit Uniformity     | Uniform        | No significant bias in leading digits              |
| Monte Carlo π Estimate | 3.15776       | Close approximation to π (ideal: ≈ 3.14159)        |


## Getting Started

### Install

This is a source-only library. You can add the `.cs` files to your project or publish it as a NuGet package yourself.

---

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using NistRandomLib;

class Program
{
    static async Task Main()
    {
        var rng = await NistRandom.CreateAsync();

        int value = rng.Next(1, 101);
        Console.WriteLine($"Random number (1–100): {value}");

        rng.BeaconPulse += (sender, e) =>
        {
            Console.WriteLine($"New entropy pulse received at {e.Pulse.TimeStamp}.");
        };
    }
}
