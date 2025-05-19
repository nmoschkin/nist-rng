using NistRNG.Model;
using System.Runtime.CompilerServices;

namespace NistRNG
{
    /// <summary>
    /// Encapsulate pulse event data.
    /// </summary>
    public class BeaconPulseEventArgs : EventArgs
    {
        /// <summary>
        /// Current NIST Pulse object
        /// </summary>
        public Pulse Pulse { get; private set; }

        /// <summary>
        /// Current <see cref="NistRandom"/> instance.
        /// </summary>
        public NistRandom Random { get; private set; }

        public BeaconPulseEventArgs(Pulse pulse, NistRandom random)
        {
            Pulse = pulse;
            Random = random;
        }
    }

    /// <summary>
    /// A <see cref="System.Random"/>-derived class that uses the NIST Randomness Beacon to periodically reseed from true entropy.
    /// </summary>
    public sealed class NistRandom : Random, IDisposable
    {
        private readonly object _lock = new();
        private readonly object _modeLock = new();

        public event EventHandler<BeaconPulseEventArgs> BeaconPulse;

        private SynchronizationContext sync = null;
        private Random rng;
        private Thread beaconWatcher;
        private CancellationTokenSource tokenSource;
        private CancellationToken token;

        private Pulse currentPulse;
        private bool disposedValue;
        private IRandomFactory randomFactory;

        /// <summary>
        /// Asynchronously create and initialize a new instance of <see cref="NistRandom"/>.
        /// </summary>
        /// <param name="randomFactory">Optional alternative <see cref="IRandomFactory"/> implementation to use.</param>
        /// <param name="sync">Optional <see cref="SynchronizationContext"/>. If this is not null, then <see cref="BeaconPulse"/> events will be invoked from the <see cref="SynchronizationContext"/>.</param>
        /// <returns>A new instance of <see cref="NistRandom"/></returns>
        public static async Task<NistRandom> CreateAsync(IRandomFactory randomFactory = null, SynchronizationContext sync = null)
        {
            var inst = new NistRandom(sync)
            {
                randomFactory = randomFactory ?? NistUtil.Instance
            };

            await inst.StartBeaconSentinel();
            return inst;
        }

        /// <summary>
        /// This object cannot be publically created
        /// </summary>
        /// <param name="sync"></param>
        private NistRandom(SynchronizationContext sync)
        {
            this.sync = sync;
        }

        /// <summary>
        /// Gets a value indicating whether the NIST-seeded <see cref="Random"/> has been created.
        /// </summary>
        public bool IsReady
        {
            get
            {
                lock (_lock)
                {
                    return (rng != null);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the NIST beacon is being actively polled.
        /// </summary>
        public bool IsBeaconSentinelRunning
        {
            get
            {
                lock(_modeLock)
                {
                    return (beaconWatcher != null && beaconWatcher.ThreadState == ThreadState.Running);
                }
            }
        }

        /// <summary>
        /// Gets the latest pull data from the last call to the NIST Randomness Beacon endpoint.
        /// </summary>
        public Pulse LatestPulseData
        {
            get
            {
                lock (_lock)
                {
                    return currentPulse;
                }
            }
        }

        /// <summary>
        /// Gets the timestamp of the last random generation.
        /// </summary>
        public DateTime LastPulseTime
        {
            get
            {
                lock (_lock)
                {
                    return currentPulse.TimeStamp;
                }
            }
        }

        /// <inheritdoc/>
        public override int Next()
        {
            lock (_lock)
            {
                return rng?.Next() ?? base.Next();
            }
        }

        /// <inheritdoc/>
        public override int Next(int maxValue)
        {
            lock (_lock)
            {
                return rng?.Next(maxValue) ?? base.Next(maxValue);
            }
        }

        /// <inheritdoc/>
        public override int Next(int minValue, int maxValue)
        {
            lock (_lock)
            {
                return rng?.Next(minValue, maxValue) ?? base.Next(minValue, maxValue);
            }            
        }

        /// <inheritdoc/>
        public override void NextBytes(byte[] buffer)
        {
            lock (_lock)
            {
                if (rng != null)
                {
                    rng.NextBytes(buffer);
                }
                else
                {
                    base.NextBytes(buffer);
                }
            }
        }

        /// <inheritdoc/>
        public override void NextBytes(Span<byte> buffer)
        {
            lock (_lock)
            {
                if (rng != null)
                {
                    rng.NextBytes(buffer);
                }
                else
                {
                    base.NextBytes(buffer);
                }
            }
        }

        /// <inheritdoc/>
        public override double NextDouble()
        {    
            lock (_lock)
            {
                return rng?.NextDouble() ?? base.NextDouble();
            }            
        }

        /// <inheritdoc/>
        public override long NextInt64()
        {
            lock (_lock) 
            {
                return rng?.NextInt64() ?? base.NextInt64();
            }
        }

        /// <inheritdoc/>
        public override long NextInt64(long maxValue)
        {
            lock (_lock)
            { 
                return rng?.NextInt64(maxValue) ?? base.NextInt64(maxValue); 
            }
        }

        /// <inheritdoc/>
        public override long NextInt64(long minValue, long maxValue)
        {
            lock (_lock)
            {
                return rng?.NextInt64(minValue, maxValue) ?? base.NextInt64(minValue, maxValue);
            }
        }

        /// <inheritdoc/>
        public override float NextSingle()
        {
            lock(_lock) 
            {
                return rng?.NextSingle() ?? base.NextSingle();
            }
        }

        /// <summary>
        /// Stop the NIST Randomness Beacon sentinel.
        /// </summary>
        /// <returns>True if successful. False if there's nothing to stop.</returns>
        public Task<bool> StopBeaconSentinel()
        {
            var res = new TaskCompletionSource<bool>();
            lock (_modeLock)
            {
                if (beaconWatcher == null)
                {
                    res.SetResult(false);
                    return res.Task;
                }
                tokenSource.Cancel();                
                beaconWatcher = null;
                token = CancellationToken.None;
                tokenSource = null;
                res.SetResult(true);
                return res.Task;
            }            
        }

        /// <summary>
        /// Starts the NIST Randomness Beacon sentinel, which will poll the NIST Randomness beacon on a sechedule specified by the last reported beacon period.
        /// </summary>
        /// <returns>True if successful.</returns>
        public Task<bool> StartBeaconSentinel()
        {
            const int timeout = 60000;
            var res = new TaskCompletionSource<bool>();
            lock (_modeLock)
            {
                if (beaconWatcher != null)
                {
                    res.SetResult(false);
                    return res.Task;
                }
                tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;
                beaconWatcher = new Thread(async () => await ThreadMethod());
                beaconWatcher.IsBackground = true;
                beaconWatcher.Start();
                Task.Run(async () =>
                {
                    var acc = 0;
                    while (rng == null && acc < timeout)
                    {
                        await Task.Delay(1);
                        acc++;                        
                    }
                    res.SetResult(rng != null);
                });
                return res.Task;
            }
        }

        /// <summary>
        /// Thread method
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private async Task ThreadMethod()
        {
            const int wait = 100;
            var acc = 0;

            var payload = await NistUtil.GetBeacon();
            if (payload == null) throw new NullReferenceException(nameof(payload));

            var interval = payload.Pulse.Period;

            ProcessPayload(payload);

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(wait);
                acc += wait;
                if (token.IsCancellationRequested) return;

                if (acc >= interval)
                {
                    acc = 0;
                    payload = await NistUtil.GetBeacon();
                    if (payload == null) throw new NullReferenceException(nameof(payload));

                    if (token.IsCancellationRequested) return;
                    interval = payload.Pulse.Period;
                    ProcessPayload(payload);
                }
            }
        }

        /// <summary>
        /// Process the beacon payload and dispatch any events.
        /// </summary>
        /// <param name="payload"></param>
        private void ProcessPayload(NistBeaconPayload payload)
        {
            lock (_lock)
            {
                currentPulse = payload.Pulse;
                rng = randomFactory.CreateRandomFromPulse(currentPulse.OutputValue, out _);
            }

            PublishNextPulse();
        }

        private void PublishNextPulse()
        {
#if DEBUG
            Console.WriteLine($"Beacon Pulse:\r\n{currentPulse?.OutputValue}");
#endif
            if (sync != null)
            {
                try
                {
                    sync.Post((o) =>
                    {
                        BeaconPulse?.Invoke(this, new BeaconPulseEventArgs(currentPulse, this));
                    }, null);
                    return;
                }
                catch
                {
                    try
                    {
                        sync.Send((o) =>
                        {
                            BeaconPulse?.Invoke(this, new BeaconPulseEventArgs(currentPulse, this));
                        }, null);
                        return;
                    }
                    catch
                    {

                    }                    
                }
            }           

            BeaconPulse?.Invoke(this, new BeaconPulseEventArgs(currentPulse, this));
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (beaconWatcher != null && beaconWatcher.ThreadState == ThreadState.Running)
                {
                    tokenSource.Cancel();
                    beaconWatcher.Join();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~NistRandom()
        {
            Dispose(false);
        }

    }
}
