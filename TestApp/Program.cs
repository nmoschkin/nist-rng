
using NistRNG;
using System.Security.Cryptography;

namespace TestApp
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {            
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            var ints = Tests().ConfigureAwait(false).GetAwaiter().GetResult();

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Text files|*.txt|Any file|*.*";
            dlg.OverwritePrompt = true;
            var result = dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                var path = dlg.FileName;
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    foreach (var i in ints)
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(i.ToString() + "\r\n");
                        fs.Write(bytes, 0, bytes.Length);
                    }

                    fs.Flush();
                }
            }

        }

        private static async Task<IList<int>> Tests()
        {
            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            var nist = await NistRandom.CreateAsync();
            var ints = new List<int>();

            Console.WriteLine("Running 1,000,000 iterations...\r\n");
            const int count = 1_000_000;
            for (var i = 0; i < count; i++)
            {
                ints.Add(nist.Next());
                if (((i + 1) % 1000) == 0)
                {
                    Console.Write($"\r{i + 1:#,##0} of {count:#,##0}...   ");
                }
            }

            return ints;
        }
    }
}