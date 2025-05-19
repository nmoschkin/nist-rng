using Newtonsoft.Json;
using NistRNG.Model;

namespace NistRNG
{

    /// <summary>
    /// Encapsulates the functionality to create a new <see cref="Random"/> seeded with a NIST randomness pulse string.
    /// </summary>
    public interface IRandomFactory
    {
        /// <summary>
        /// Create a new <see cref="System.Random"/> instance seeded from the specified entropy pulse string.
        /// </summary>
        /// <param name="pulse">Entry pulse string.</param>
        /// <param name="intermediateValues">Receives intermediate randomized values.</param>
        /// <returns>A new <see cref="System.Random"/> instance.</returns>
        Random CreateRandomFromPulse(string pulse, out List<int> intermediateValues);
    }


    /// <summary>
    /// Methods for interfacing with the NIST Randomness Beacon
    /// </summary>
    public class NistUtil : IRandomFactory
    {
        /// <summary>
        /// Gets the default instance for this class
        /// </summary>
        public static NistUtil Instance { get; } = new NistUtil();
        
        /// <summary>
        /// The default version is not creatable. Derive a class, instead.
        /// </summary>
        private NistUtil()
        {
        }

        /// <summary>
        /// Poll the NIST Randomness Beacon (internet required)
        /// </summary>
        /// <returns>The full NIST Randomness Beacon payload</returns>
        public static async Task<NistBeaconPayload> GetBeacon()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync("https://beacon.nist.gov/beacon/2.0/chain/last/pulse/last?type=json");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<NistBeaconPayload>(json);
                }
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual Random CreateRandomFromPulse(string pulse, out List<int> intermediateValues)
        {
            // Only use 28 bits because System.Random discards the sign, which ends up as lost information, so we'll put that 8th nibble to some other purpose.
            var seed1txt = pulse.Substring(0, 7);
            var seed1 = int.Parse(seed1txt, System.Globalization.NumberStyles.HexNumber);
            var rng = new System.Random(seed1);

            var cycletxt = pulse.Substring(8);
            var cycles = new List<int>();

            var c = cycletxt.Length;
            byte prev = 0;

            for (var i = 0; i < c; i += 2)
            {
                var byteval = byte.Parse($"{cycletxt[i]}{cycletxt[i + 1]}", System.Globalization.NumberStyles.HexNumber);
                if (byteval < prev)
                {
                    prev = (byte)rng.Next(byteval, prev);
                }
                else
                {
                    prev = (byte)rng.Next(prev, byteval);
                }
                var seedval = ((byte)(byteval ^ prev));
                cycles.Add(seedval);
            }

            var dtbytes = BitConverter.GetBytes(DateTime.Now.ToBinary());

            c = cycles.Count;

            for (var i = 0; i < c; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    cycles[i] ^= dtbytes[j];
                }
            }

            int finalseed = unchecked((int)0xffffffff);

            List<int> refparts = new();

            foreach (var jostle in cycles)
            {
                finalseed ^= jostle;
                var bitsource = jostle;
                for (var i = 0; i < 8; i++)
                {
                    var jbit = (bitsource & 0x7);
                    if (jbit == 0) break;
                    finalseed = (finalseed << jbit) ^ jostle;

                    var high = jostle | 0x80;

                    bitsource >>= 1;

                    if ((bitsource & 0x3) != 0 && (bitsource & 0x3) != 3)
                    {
                        bitsource = ((~bitsource) << (bitsource & 0x3)) ^ (bitsource ^ ((~bitsource) >> (bitsource & 0x3)));
                    }
                    else if (bitsource > high)
                    {
                        bitsource = ((~bitsource) << (bitsource & 0xd)) ^ (bitsource ^ ((~bitsource) >> (bitsource & 0x5))) + (jostle + i);
                    }
                }
                if (finalseed < 0)
                {
                    while (finalseed < 0)
                    {
                        finalseed = (finalseed << 1) ^ (jostle & ~1);
                    }
                }
                foreach (var part in refparts)
                {
                    finalseed ^= part + (DateTime.Now.Nanosecond ^ (~DateTime.Now.Nanosecond | 0xa3));
                }
                refparts.Add(finalseed);
            }

            intermediateValues = refparts;
            return new System.Random(finalseed);
        }
    }
}
