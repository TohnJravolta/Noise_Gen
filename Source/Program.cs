using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace NoiseGen
{
    class Program
    {
        enum AppState { Mixing, ProfileMenu, NamingProfile }

        static AudioEngine _engine;
        static ConfigManager _config;
        static PerformanceCounter _cpuCounter;
        static Process _process;
        static bool _running = true;
        static AppState _state = AppState.Mixing;
        
        static List<string> _profileList = new List<string>();
        static int _profileIndex = 0;
        static string _inputName = "";
        static string _currentProfileName = "Default";

        static void Main(string[] args)
        {
            // Parse Args
            foreach (var arg in args)
            {
                if (arg == "--test")
                {
                    bool result = TestSuite.RunTests();
                    Environment.Exit(result ? 0 : 1);
                }
            }

            Console.CursorVisible = false;

            try
            {
                Setup();

                // Main Loop
                while (_running)
                {
                    if (_state == AppState.Mixing) InputMixing();
                    else if (_state == AppState.ProfileMenu) InputProfileMenu();
                    else if (_state == AppState.NamingProfile) InputNaming();

                    Update();
                    
                    // Console.Clear(); // Causes flickering, replaced with overwrite
                    Draw();
                    Thread.Sleep(50); // 20 FPS UI
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("crash.log", ex.ToString());
                Console.Clear();
                Console.WriteLine("CRASH DETECTED. See crash.log for details.");
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
            finally
            {
               Shutdown();
            }
        }

        static void Setup()
        {
            Console.Clear(); // Initial clear
            
            // Check for last session
            if (File.Exists("last_session.ini"))
            {
                _config = new ConfigManager("last_session.ini");
                _currentProfileName = "Last Session";
            }
            else
            {
                _config = new ConfigManager(); // Default profiles.ini
            }
            
            _config.Load();

            _engine = new AudioEngine();
            // Add Generators
            _engine.Generators.Add(new WhiteNoiseGenerator() { Enabled = _config.GetBool("gen0_enabled", false), Volume = _config.GetFloat("gen0_vol", 0.5f) });
            _engine.Generators.Add(new PinkNoiseGenerator() { Enabled = _config.GetBool("gen1_enabled", false), Volume = _config.GetFloat("gen1_vol", 0.5f) });
            _engine.Generators.Add(new BrownNoiseGenerator() { Enabled = _config.GetBool("gen2_enabled", false), Volume = _config.GetFloat("gen2_vol", 0.5f) });
            
            // Binaural
            _engine.Generators.Add(new BinauralGenerator("Focus (14Hz Beta)", 400, 14) { Enabled = _config.GetBool("gen3_enabled", false), Volume = _config.GetFloat("gen3_vol", 0.5f) });
            _engine.Generators.Add(new BinauralGenerator("Relax (7Hz Alpha)", 200, 7) { Enabled = _config.GetBool("gen4_enabled", false), Volume = _config.GetFloat("gen4_vol", 0.5f) });
            _engine.Generators.Add(new BinauralGenerator("Sleep (4Hz Theta)", 150, 4) { Enabled = _config.GetBool("gen5_enabled", false), Volume = _config.GetFloat("gen5_vol", 0.5f) });

            _engine.MasterVolume = _config.GetFloat("master_vol", 1.0f);

            // Perf
            try {
                _process = Process.GetCurrentProcess();
                _cpuCounter = new PerformanceCounter("Process", "% Processor Time", _process.ProcessName);
            } catch { /* Ignore if perf counters not avail */ }

            RefreshProfileList();
        }

        static void RefreshProfileList()
        {
             _profileList.Clear();
             var files = Directory.GetFiles(".", "*.ini");
             foreach(var f in files) 
             {
                 string name = Path.GetFileNameWithoutExtension(f);
                 if (name != "last_session") _profileList.Add(name);
             }
             if (_profileList.Count == 0) _profileList.Add("profiles");
        }

        static int _selectedIndex = 0;
        
        // P/Invoke for GetAsyncKeyState to detect actual key hold
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
        
        const int VK_LEFT = 0x25;
        const int VK_UP = 0x26;
        const int VK_RIGHT = 0x27;
        const int VK_DOWN = 0x28;
        
        static void InputMixing()
        {
            // Clear the keyboard buffer first to prevent ghost inputs
            while (Console.KeyAvailable) Console.ReadKey(true);
            
            // Check for actual key presses using Console for non-volume controls
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                
                if (key == ConsoleKey.Escape) _running = false;
                if (key == ConsoleKey.P) 
                {
                    _state = AppState.ProfileMenu;
                    RefreshProfileList();
                    _profileIndex = 0;
                    return;
                }
                if (key == ConsoleKey.S) 
                {
                    _state = AppState.NamingProfile;
                    _inputName = "";
                    return;
                }

                if (key == ConsoleKey.DownArrow) _selectedIndex = Math.Min(_selectedIndex + 1, _engine.Generators.Count);
                if (key == ConsoleKey.UpArrow) _selectedIndex = Math.Max(_selectedIndex - 1, 0);
                
                // Toggle
                if (key == ConsoleKey.Spacebar || key == ConsoleKey.Enter)
                {
                    if (_selectedIndex < _engine.Generators.Count)
                        _engine.Generators[_selectedIndex].Enabled = !_engine.Generators[_selectedIndex].Enabled;
                }
            }
            
            // Volume control using GetAsyncKeyState for real-time hold detection
            bool leftHeld = (GetAsyncKeyState(VK_LEFT) & 0x8000) != 0;
            bool rightHeld = (GetAsyncKeyState(VK_RIGHT) & 0x8000) != 0;
            
            if (leftHeld || rightHeld)
            {
                float step = 0.01f;
                
                if (rightHeld)
                {
                    if (_selectedIndex == _engine.Generators.Count) 
                        _engine.MasterVolume = Math.Min(_engine.MasterVolume + step, 1.0f);
                    else
                        _engine.Generators[_selectedIndex].Volume = Math.Min(_engine.Generators[_selectedIndex].Volume + step, 1.0f);
                }
                else if (leftHeld)
                {
                    if (_selectedIndex == _engine.Generators.Count) 
                        _engine.MasterVolume = Math.Max(_engine.MasterVolume - step, 0.0f);
                    else
                        _engine.Generators[_selectedIndex].Volume = Math.Max(_engine.Generators[_selectedIndex].Volume - step, 0.0f);
                }
            }
        }

        static void InputProfileMenu()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Escape) _state = AppState.Mixing;
                
                if (key == ConsoleKey.DownArrow) _profileIndex = Math.Min(_profileIndex + 1, _profileList.Count - 1);
                if (key == ConsoleKey.UpArrow) _profileIndex = Math.Max(_profileIndex - 1, 0);

                if (key == ConsoleKey.Enter)
                {
                   LoadProfile(_profileList[_profileIndex]);
                   _state = AppState.Mixing;
                }
            }
        }

        static void InputNaming()
        {
            if (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Escape) { _state = AppState.Mixing; return; }
                
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    if (!string.IsNullOrEmpty(_inputName)) SaveProfile(_inputName);
                    _state = AppState.Mixing;
                    return;
                }
                
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (_inputName.Length > 0) _inputName = _inputName.Substring(0, _inputName.Length - 1);
                }
                else if (char.IsLetterOrDigit(keyInfo.KeyChar) || keyInfo.KeyChar == '_' || keyInfo.KeyChar == '-')
                {
                    _inputName += keyInfo.KeyChar;
                }
            }
        }

        static void Update()
        {
            _engine.Update();
            // Auto-save logic removed to favor manual profile management, 
            // OR we can auto-save to 'last_session.ini'
        }

        static void LoadProfile(string name)
        {
             _currentProfileName = name;
             _config = new ConfigManager(name + ".ini");
             _config.Load();
             
             // Apply to engine
            for(int i=0; i<_engine.Generators.Count; i++)
            {
               _engine.Generators[i].Enabled = _config.GetBool(string.Format("gen{0}_enabled", i), false);
               _engine.Generators[i].Volume = _config.GetFloat(string.Format("gen{0}_vol", i), 0.5f);
            }
            _engine.MasterVolume = _config.GetFloat("master_vol", 1.0f);
        }

        static void SaveProfile(string name)
        {
            _currentProfileName = name;
            var cfg = new ConfigManager(name + ".ini");
            
            for(int i=0; i<_engine.Generators.Count; i++)
            {
                cfg.Set(string.Format("gen{0}_enabled", i), _engine.Generators[i].Enabled.ToString());
                cfg.Set(string.Format("gen{0}_vol", i), _engine.Generators[i].Volume.ToString());
            }
            cfg.Set("master_vol", _engine.MasterVolume.ToString());
            
            cfg.Save();
        }

        static void Draw()
        {
            Console.SetCursorPosition(0, 0);
            
            if (_state == AppState.ProfileMenu)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== LOAD PROFILE ===");
                Console.WriteLine("UP/DOWN to select, ENTER to load, ESC to cancel");
                Console.WriteLine("-----------------------------");
                for(int i=0; i<_profileList.Count; i++)
                {
                    if (i == _profileIndex) Console.BackgroundColor = ConsoleColor.White; Console.ForegroundColor = ConsoleColor.Black;
                    
                    Console.WriteLine(string.Format(" {0} ", _profileList[i]));
                    
                    Console.ResetColor();
                }
                return;
            }

            if (_state == AppState.NamingProfile)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=== SAVE PROFILE ===");
                Console.WriteLine("Type name and press ENTER. ESC to cancel.");
                Console.WriteLine("-----------------------------");
                Console.Write("Name: " + _inputName + "_");
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== LIGHTWEIGHT NOISE GEN ===");
            Console.ResetColor();
            
            float cpu = 0;
            if (_cpuCounter != null) try { cpu = _cpuCounter.NextValue() / Environment.ProcessorCount; } catch {}
            long mem = _process != null ? _process.PrivateMemorySize64 / 1024 / 1024 : 0;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(string.Format("RAM: {0} MB | CPU: {1:F1}%   ", mem, cpu).PadRight(79));
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(string.Format("Profile: {0}", _currentProfileName).PadRight(79));
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("-----------------------------".PadRight(79));
            Console.ResetColor();

            for (int i = 0; i < _engine.Generators.Count; i++)
            {
                DrawItem(i, _engine.Generators[i].Name, _engine.Generators[i].Enabled, _engine.Generators[i].Volume);
            }
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("-----------------------------".PadRight(79));
            Console.ResetColor();
            DrawItem(_engine.Generators.Count, "MASTER VOLUME", true, _engine.MasterVolume);
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("-----------------------------".PadRight(79));
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[UP/DOWN] Select | [SPACE] Toggle | [LEFT/RIGHT] Volume              ");
            Console.WriteLine("[P] Profiles | [S] Save | [ESC] Quit                                 ");
            Console.ResetColor();
        }

        static void DrawItem(int index, string name, bool enabled, float vol)
        {
            if (index == _selectedIndex) Console.BackgroundColor = ConsoleColor.DarkGray;
            else Console.BackgroundColor = ConsoleColor.Black;

            // Color code the status
            if (enabled)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[ON] ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[OFF]");
            }
            
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(string.Format(" {0,-20} ", name));
            
            // Draw Volume Bar with color gradient
            int bars = (int)(vol * 20);
            Console.Write("[");
            
            for (int b = 0; b < 20; b++)
            {
                if (b < bars)
                {
                    // Color gradient: Green -> Yellow -> Red
                    if (vol < 0.5f)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else if (vol < 0.75f)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("|");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(".");
                }
            }
            
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(string.Format("] {0:F2}   ", vol));
            
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ResetColor();
        }

        static void Shutdown()
        {
            SaveProfile("last_session");
            if (_engine != null) _engine.Dispose();
            Console.CursorVisible = true;
            Console.Clear();
        }
    }
}
