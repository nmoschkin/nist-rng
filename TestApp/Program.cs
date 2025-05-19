
using NistRNG;
using System.Security.Cryptography;

namespace TestApp
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await Tests();
        }

        private static async Task Tests()
        {
            var nist = await NistRandom.CreateAsync();
                        
            while (true)
            {
                if (Console.KeyAvailable) break;

                await Task.Delay(1000);
                Console.WriteLine(nist.Next());
            }
        }
    }
}