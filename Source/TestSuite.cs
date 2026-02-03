using System;
using System.IO;

namespace NoiseGen
{
    public class TestSuite
    {
        public static bool RunTests()
        {
            Console.WriteLine("Running Self-Test Suite...");
            bool allPassed = true;

            allPassed &= TestGenerators();
            allPassed &= TestConfig();
            
            if (allPassed) 
                Console.WriteLine("[PASS] All Systems Operational.");
            else 
                Console.WriteLine("[FAIL] Some tests failed.");

            return allPassed;
        }

        static bool TestGenerators()
        {
            Console.Write("Testing Generators... ");
            try
            {
                float[] buffer = new float[100];
                var white = new WhiteNoiseGenerator();
                white.Volume = 1.0f;
                // Run
                white.FillBuffer(buffer, 0, 100, 44100);
                
                // Assert not silent
                bool hasSignal = false;
                foreach(var f in buffer) { if(f != 0) hasSignal = true; }
                
                if (!hasSignal) { Console.WriteLine("FAIL (Silent White Noise)"); return false; }

                // Test Binaural
                Array.Clear(buffer, 0, 100);
                var bin = new BinauralGenerator("TestBin", 400, 10);
                bin.Volume = 1.0f;
                bin.FillBuffer(buffer, 0, 100, 44100);
                 
                hasSignal = false;
                foreach(var f in buffer) { if(f != 0) hasSignal = true; }
                 
                if (!hasSignal) { Console.WriteLine("FAIL (Silent Binaural)"); return false; }
                
                Console.WriteLine("OK");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: " + ex.Message);
                return false;
            }
        }

        static bool TestConfig()
        {
            Console.Write("Testing Config Persistence... ");
            try
            {
                ConfigManager cfg = new ConfigManager();
                cfg.Set("test_key", "test_val");
                cfg.Save();
                
                ConfigManager cfg2 = new ConfigManager();
                cfg2.Load();
                if (cfg2.Get("test_key", "") == "test_val")
                {
                    Console.WriteLine("OK");
                    return true;
                }
                else
                {
                    Console.WriteLine("FAIL (Value mismatch)");
                    return false;
                }
            }
            catch (Exception ex)
            {
                 Console.WriteLine("FAIL: " + ex.Message);
                 return false;
            }
        }
    }
}
