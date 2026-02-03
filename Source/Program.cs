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
        enum AppState { Mixing, ProfileMenu, NamingProfile, DeleteConfirm }

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
        static bool _colorBlindMode = false;

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
                    else if (_state == AppState.DeleteConfirm) InputDeleteConfirm();

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
            
            // Organization: Ensure Profiles folder exists
            if (!Directory.Exists("Profiles")) Directory.CreateDirectory("Profiles");
            
            // Migrate any legacy root INI files to Profiles folder
            try {
                foreach(var f in Directory.GetFiles(".", "*.ini"))
                {
                    string dest = Path.Combine("Profiles", Path.GetFileName(f));
                    if (!File.Exists(dest)) File.Move(f, dest);
                    else File.Delete(f); // Remove root copy if dest exists
                }
            } catch {}
            
            string lastSessionPath = Path.Combine("Profiles", "last_session.ini");

            // Check for last session
            if (File.Exists(lastSessionPath))
            {
                _config = new ConfigManager(lastSessionPath);
                _currentProfileName = "Last Session";
            }
            else
            {
                // Fallback to defaults if no last session
                _config = new ConfigManager(); 
            }
            
            // Ensure we save on exit (even X button)
            _consoleHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(_consoleHandler, true);
            
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            
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

            _engine.MasterVolume = _config.GetFloat("master_vol", 0.69f);
            
            // Load Color Blind Mode preference
            _colorBlindMode = _config.GetBool("color_blind_mode", false);

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
             if (Directory.Exists("Profiles"))
             {
                 var files = Directory.GetFiles("Profiles", "*.ini");
                 foreach(var f in files) 
                 {
                     string name = Path.GetFileNameWithoutExtension(f);
                     if (name != "last_session") _profileList.Add(name);
                 }
             }
             if (_profileList.Count == 0) _profileList.Add("DEFAULT");
        }

        static int _selectedIndex = 0;
        
        
        
        
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        
        static HandlerRoutine _consoleHandler;
        
        static void InputMixing()
        {
            // Process keys
            while (Console.KeyAvailable)
            {
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;
                
                if (key == ConsoleKey.Escape) _running = false;
                
                // Volume Control (Now handled via standard input to respect focus)
                if (key == ConsoleKey.LeftArrow || key == ConsoleKey.RightArrow)
                {
                    float step = 0.01f;
                    if (key == ConsoleKey.RightArrow)
                    {
                        if (_selectedIndex == _engine.Generators.Count) 
                            _engine.MasterVolume = Math.Min(_engine.MasterVolume + step, 1.0f);
                        else
                            _engine.Generators[_selectedIndex].Volume = Math.Min(_engine.Generators[_selectedIndex].Volume + step, 1.0f);
                    }
                    else
                    {
                        if (_selectedIndex == _engine.Generators.Count) 
                            _engine.MasterVolume = Math.Max(_engine.MasterVolume - step, 0.0f);
                        else
                            _engine.Generators[_selectedIndex].Volume = Math.Max(_engine.Generators[_selectedIndex].Volume - step, 0.0f);
                    }
                    continue;
                }

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
                
                // Color Blind Toggle
                if (key == ConsoleKey.C)
                {
                    _colorBlindMode = !_colorBlindMode;
                }

                if (key == ConsoleKey.Tab)
                {
                     if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
                        _selectedIndex = Math.Max(_selectedIndex - 1, 0);
                     else
                        _selectedIndex = Math.Min(_selectedIndex + 1, _engine.Generators.Count);
                }

                if (key == ConsoleKey.DownArrow) _selectedIndex = Math.Min(_selectedIndex + 1, _engine.Generators.Count);
                if (key == ConsoleKey.UpArrow) _selectedIndex = Math.Max(_selectedIndex - 1, 0);
                
                // Toggle
                if (key == ConsoleKey.Spacebar || key == ConsoleKey.Enter)
                {
                    if (_selectedIndex < _engine.Generators.Count)
                        _engine.Generators[_selectedIndex].Enabled = !_engine.Generators[_selectedIndex].Enabled;
                    else
                        _engine.MasterEnabled = !_engine.MasterEnabled;
                }

                // Kill/Mute
                if (key == ConsoleKey.M)
                {
                    _engine.MasterEnabled = !_engine.MasterEnabled;
                }

                // Reset Defaults
                if (key == ConsoleKey.R)
                {
                    ResetToDefaults();
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

                if (key == ConsoleKey.Delete)
                {
                    if (_profileList.Count > 0)
                    {
                        _state = AppState.DeleteConfirm;
                    }
                    return;
                }

                if (key == ConsoleKey.Enter)
                {
                   LoadProfile(_profileList[_profileIndex]);
                   _state = AppState.Mixing;
                }
            }
        }

        static void InputDeleteConfirm()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.Y)
                {
                    string target = Path.Combine("Profiles", _profileList[_profileIndex] + ".ini");
                    if (File.Exists(target))
                    {
                        try { File.Delete(target); } catch {}
                        RefreshProfileList();
                        _profileIndex = Math.Min(_profileIndex, Math.Max(0, _profileList.Count - 1));
                    }
                    _state = AppState.ProfileMenu;
                }
                else if (key == ConsoleKey.N || key == ConsoleKey.Escape)
                {
                    _state = AppState.ProfileMenu;
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
             _config = new ConfigManager(Path.Combine("Profiles", name + ".ini"));
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
            var cfg = new ConfigManager(Path.Combine("Profiles", name + ".ini"));
            
            for(int i=0; i<_engine.Generators.Count; i++)
            {
                cfg.Set(string.Format("gen{0}_enabled", i), _engine.Generators[i].Enabled.ToString());
                cfg.Set(string.Format("gen{0}_vol", i), _engine.Generators[i].Volume.ToString());
            }
            cfg.Set("master_vol", _engine.MasterVolume.ToString());
            cfg.Set("color_blind_mode", _colorBlindMode.ToString());
            
            cfg.Save();
        }

        static void Draw()
        {
            Console.SetCursorPosition(0, 0);
            
            if (_state == AppState.ProfileMenu)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                // Box Width 62 chars: 0..61
                // ┌ + 60 dashes + ┐
                string top    = "┌────────────────────────────────────────────────────────────┐";
                string mid    = "│                    === LOAD PROFILE ===                    │";
                string bot    = "└────────────────────────────────────────────────────────────┘";
                string header = "│ UP/DOWN: Select | ENTER: Load | DEL: Delete | ESC: Cancel  │";
                
                Console.WriteLine(top.PadRight(79));
                Console.WriteLine(mid.PadRight(79));
                Console.WriteLine(header.PadRight(79));
                Console.WriteLine("├────────────────────────────────────────────────────────────┤".PadRight(79));
                
                int maxItems = 10;
                for(int i=0; i < maxItems; i++)
                {
                    if (i < _profileList.Count)
                    {
                        string prefix = (i == _profileIndex) ? " > " : "   ";
                        ConsoleColor itemColor = (i == _profileIndex) ? ConsoleColor.Cyan : ConsoleColor.Gray;
                        
                        string name = _profileList[i];
                        if (name.Length > 35) name = name.Substring(0, 35) + "..."; // Truncate if too long
                        
                        string content = string.Format("{0}{1}", prefix, name); // length varies
                        
                        // Construct line: "│ " + content + padding + " │"
                        // Total inner width 60 chars.
                        // "│ " is 2 chars.
                        // End " │" is 2 chars.
                        // Content area = 58 chars.
                        
                        Console.Write("│ ");
                        Console.ForegroundColor = itemColor;
                        Console.Write(content.PadRight(58));
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(" │".PadRight(19)); // Pad to 79 total line length
                    }
                    else
                    {
                        Console.WriteLine(("│ ".PadRight(60) + " │").PadRight(79));
                    }
                }
                Console.WriteLine(bot.PadRight(79));
                
                // Clear any remaining lines below the menu to mask the background mix TUI
                for(int i=0; i<10; i++) Console.WriteLine("".PadRight(79));
                
                return;
            }

            if (_state == AppState.NamingProfile)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                string top = "┌────────────────────────────────────────────────────────────┐";
                string bot = "└────────────────────────────────────────────────────────────┘";
                
                Console.WriteLine(top.PadRight(79));
                Console.WriteLine("│                    === SAVE PROFILE ===                    │".PadRight(79));
                Console.WriteLine("│ Type name and press ENTER. ESC to cancel.                  │".PadRight(79));
                Console.WriteLine("├────────────────────────────────────────────────────────────┤".PadRight(79));
                
                Console.Write("│ Name: ");
                string input = _inputName + "_";
                if (input.Length > 45) input = input.Substring(0, 45); // Safety
                
                Console.Write(input.PadRight(52));
                Console.WriteLine(" │".PadRight(19));
                Console.WriteLine(bot.PadRight(79));
                
                for(int i=0; i<15; i++) Console.WriteLine("".PadRight(79));
                return;
            }

            if (_state == AppState.DeleteConfirm)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string top = "┌────────────────────────────────────────────────────────────┐";
                string bot = "└────────────────────────────────────────────────────────────┘";

                Console.WriteLine(top.PadRight(79));
                Console.WriteLine("│                  === DELETE PROFILE? ===                   │".PadRight(79));
                Console.WriteLine("│                                                            │".PadRight(79));
                
                string msg = " Delete '" + _profileList[_profileIndex] + "'?";
                if (msg.Length > 58) msg = msg.Substring(0, 58);
                
                Console.WriteLine(("│ " + msg.PadRight(58) + " │").PadRight(79));
                Console.WriteLine("│                                                            │".PadRight(79));
                Console.WriteLine("│        [Y] CONFIRM DELETE         [N] CANCEL               │".PadRight(79));
                Console.WriteLine(bot.PadRight(79));
                
                for(int i=0; i<15; i++) Console.WriteLine("".PadRight(79));
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("=== LIGHTWEIGHT NOISE GEN ===".PadRight(79)); // FIXED: Padding added to wipe artifacts
            Console.ResetColor();
            
            float cpu = 0;
            if (_cpuCounter != null) try { cpu = _cpuCounter.NextValue() / Environment.ProcessorCount; } catch {}
            long mem = _process != null ? _process.PrivateMemorySize64 / 1024 / 1024 : 0;

            // Stats: White for high contrast readability
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("RAM: {0} MB | CPU: {1:F1}%   ", mem, cpu).PadRight(79));
            Console.ResetColor();
            
            // Profile: Yellow (High Vis)
            Console.ForegroundColor = ConsoleColor.Yellow;
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
            DrawItem(_engine.Generators.Count, "MASTER VOLUME", _engine.MasterEnabled, _engine.MasterVolume);
            
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("-----------------------------".PadRight(79));
            Console.ResetColor();
            
            // Controls: Cyan
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("[UP/DOWN] Select | [SPACE] Toggle | [LEFT/RIGHT] Volume".PadRight(79));
            
            Console.Write("[P] Profiles | [S] Save | ");
            
            // Highlight Color Blind Toggle
            if (_colorBlindMode) Console.ForegroundColor = ConsoleColor.Yellow; 
            else Console.ForegroundColor = ConsoleColor.Magenta;
            
            Console.WriteLine("[C] Color Blind Mode".PadRight(40));
            
            // Row 3: Mute, Reset, Quit
            // Mute Logic
            if (!_engine.MasterEnabled)
            {
                if (_colorBlindMode) Console.ForegroundColor = ConsoleColor.White; // High contrast
                else Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[M] UNMUTE ");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("[M] Mute   ");
            }
            
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(" | [R] Reset | [ESC] Quit".PadRight(40));
            
            Console.ResetColor();
            
            // Cleanup: Ensure we wipe any leftover lines from menus that might be taller than the mix UI
            for(int k=0; k<10; k++) Console.WriteLine("".PadRight(79));
        }

        static void DrawItem(int index, string name, bool enabled, float vol)
        {
            if (index == _selectedIndex) Console.BackgroundColor = ConsoleColor.DarkGray;
            else Console.BackgroundColor = ConsoleColor.Black;

            if (_colorBlindMode)
            {
                // === COLOR BLIND PALETTE (White -> Cyan -> Yellow) ===
                if (enabled) { Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("[ON] "); }
                else { Console.ForegroundColor = ConsoleColor.Gray; Console.Write("[OFF]"); }
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(string.Format(" {0,-20} ", name));
                
                // Determine text color based on level
                ConsoleColor volColor = ConsoleColor.White;
                if (vol >= 0.35f) volColor = ConsoleColor.Cyan;
                if (vol >= 0.75f) volColor = ConsoleColor.Yellow;

                int bars = (int)(vol * 20);
                Console.Write("[");
                for (int b = 0; b < 20; b++)
                {
                    if (b < bars)
                    {
                        // Gradient logic for bars
                        if (b < 7) Console.ForegroundColor = ConsoleColor.White;      // < 35%
                        else if (b < 15) Console.ForegroundColor = ConsoleColor.Cyan; // < 75%
                        else Console.ForegroundColor = ConsoleColor.Yellow;           // > 75%
                        Console.Write("|");
                    }
                    else { Console.ForegroundColor = ConsoleColor.Gray; Console.Write("."); }
                }
                
                // Text Color for number
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("]");
                
                Console.ForegroundColor = volColor;
                string volStr = string.Format(" {0:F2}", vol);
                
                // Pad the rest of the line to erase any background artifacts (total width 79)
                // Current length approx: 5 (ON) + 22 (Name) + 1 ([) + 20 (Bar) + 1 (]) + length of volStr
                // We just print volStr and then padding
                Console.WriteLine(volStr.PadRight(79 - (5 + 22 + 1 + 20 + 1))); 
            }
            else
            {
                // === REGULAR PALETTE (Green/Red/Gradient) ===
                if (enabled) { Console.ForegroundColor = ConsoleColor.Green; Console.Write("[ON] "); }
                else { Console.ForegroundColor = ConsoleColor.Red; Console.Write("[OFF]"); } // Red for OFF
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(string.Format(" {0,-20} ", name));
                
                // Determine text color based on level
                ConsoleColor volColor = ConsoleColor.Green;
                if (vol >= 0.5f) volColor = ConsoleColor.Yellow;
                if (vol >= 0.75f) volColor = ConsoleColor.Red;
                
                int bars = (int)(vol * 20);
                Console.Write("[");
                for (int b = 0; b < 20; b++)
                {
                    if (b < bars)
                    {
                        if (b < 10) Console.ForegroundColor = ConsoleColor.Green;      // < 50%
                        else if (b < 15) Console.ForegroundColor = ConsoleColor.Yellow;// < 75%
                        else Console.ForegroundColor = ConsoleColor.Red;               // > 75%
                        Console.Write("|");
                    }
                    else { Console.ForegroundColor = ConsoleColor.Gray; Console.Write("."); }
                }
                
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("]");
                
                Console.ForegroundColor = volColor;
                string volStr = string.Format(" {0:F2}", vol);
                Console.WriteLine(volStr.PadRight(79 - (5 + 22 + 1 + 20 + 1)));
            }
            
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

        static void OnProcessExit(object sender, EventArgs e)
        {
            SaveProfile("last_session");
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    SaveProfile("last_session");
                    break;
            }
            return true;
        }

        static void ResetToDefaults()
        {
             // Disable all gens, reset volumes
             foreach(var gen in _engine.Generators)
             {
                 gen.Enabled = false;
                 gen.Volume = 0.5f;
             }
             _engine.MasterVolume = 0.69f;
             _engine.MasterEnabled = true;
             _colorBlindMode = false;
             _currentProfileName = "DEFAULT";
        }
    }
}
