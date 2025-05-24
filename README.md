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

## NistRNG Test Results (Latest Sample: 100,000 integers)

| Test Name             | Result             | Notes                                                  |
|----------------------|--------------------|--------------------------------------------------------|
| Shannon Entropy      | 7.9982 bits         | Extremely high entropy (max = 8.0 bits)                |
| Chi-Square Test      | Passed              | p-value = 0.8068 — excellent randomness                |
| Monte Carlo π Estimate | 3.14296           | Very precise approximation to π (ideal ≈ 3.14159)      |
| Digit Uniformity     | Uniform             | All digits 0–9 evenly distributed across 100,000 values |
| Mean Value           | 127.531             | Consistent with expected midpoint (ideal: 128)         |

---

### Example Usage

```csharp
using System;
using System.Threading.Tasks;
using NistRNG;

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
