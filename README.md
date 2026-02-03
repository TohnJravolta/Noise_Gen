# 🎵 NoiseGen - Lightweight Noise Generator

<div align="center">

**A minimal, efficient white noise generator with customizable sound profiles**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)]()
[![.NET Framework](https://img.shields.io/badge/.NET-4.0-512BD4.svg)]()

</div>

---

## 📖 Overview

NoiseGen is a **lightweight, terminal-based noise generator** designed for minimal system footprint and maximum customization. Perfect for focus, relaxation, or sleep, it generates true random noise in real-time rather than looping pre-recorded sounds.

### ✨ Key Features

- 🎚️ **Multiple Noise Types**: White, Pink, and Brown noise generators
- 🧠 **Binaural Beats**: Focus (14Hz Beta), Relax (7Hz Alpha), Sleep (4Hz Theta)
- 💾 **Profile Management**: Save and load custom sound configurations
- ⚡ **Ultra-Lightweight**: Minimal RAM and CPU usage
- 🎛️ **Real-time Control**: Adjust volumes and toggle channels on-the-fly
- 🖥️ **Terminal UI**: Clean, visual interface with performance monitoring
- 🔄 **True Random Generation**: No loops, seamless audio playback

---

## 🚀 Quick Start

### Prerequisites

- **Windows** with .NET Framework 4.0 or higher
- **C# Compiler** (included with .NET Framework)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/NOISE_GEN.git
   cd NOISE_GEN
   ```

2. **Build the application**
   ```bash
   build.bat
   ```
   
   This will compile all source files and create `NoiseGen.exe`

3. **Run the application**
   ```bash
   NoiseGen.exe
   ```

---

## 🎮 Usage

### Main Interface

When you launch NoiseGen, you'll see a clean terminal interface showing:

```
=== LIGHTWEIGHT NOISE GEN ===
RAM: 15 MB | CPU: 2.3%
Profile: Default | [P] Profiles | [S] Save | [ESC] Quit
-----------------------------
[[OFF] White Noise         [....................] 0.50
[[ON]  Pink Noise          [||||||||||..........] 0.50
[[OFF] Brown Noise         [....................] 0.50
[[OFF] Focus (14Hz Beta)   [....................] 0.50
[[OFF] Relax (7Hz Alpha)   [....................] 0.50
[[OFF] Sleep (4Hz Theta)   [....................] 0.50
-----------------------------
[[ON]  MASTER VOLUME       [||||||||||||||||||||] 1.00
```

### Controls

| Key | Action |
|-----|--------|
| <kbd>↑</kbd> / <kbd>↓</kbd> | Navigate between channels |
| <kbd>Space</kbd> / <kbd>Enter</kbd> | Toggle channel ON/OFF |
| <kbd>←</kbd> / <kbd>→</kbd> | Adjust volume (±1%) |
| <kbd>P</kbd> | Open profile menu |
| <kbd>S</kbd> | Save current configuration |
| <kbd>Esc</kbd> | Quit application |

### Profile Management

#### Saving a Profile

1. Press <kbd>S</kbd> to enter save mode
2. Type a name (letters, numbers, `_`, `-` allowed)
3. Press <kbd>Enter</kbd> to save
4. Your profile is saved as `[name].ini`

#### Loading a Profile

1. Press <kbd>P</kbd> to open the profile menu
2. Use <kbd>↑</kbd> / <kbd>↓</kbd> to select a profile
3. Press <kbd>Enter</kbd> to load
4. Press <kbd>Esc</kbd> to cancel

> **Note:** Your last session is automatically saved and restored on next launch!

---

## 🎨 Noise Types Explained

| Type | Description | Best For |
|------|-------------|----------|
| **White Noise** | Equal energy across all frequencies | Masking background sounds |
| **Pink Noise** | More energy in lower frequencies | Natural, soothing sound |
| **Brown Noise** | Even more bass-heavy | Deep relaxation, sleep |
| **Focus (14Hz Beta)** | Binaural beat for alertness | Concentration, studying |
| **Relax (7Hz Alpha)** | Binaural beat for calm | Meditation, stress relief |
| **Sleep (4Hz Theta)** | Binaural beat for deep rest | Falling asleep |

> 💡 **Tip:** Mix multiple noise types for your perfect sound environment!

---

## 🛠️ Technical Details

### Architecture

NoiseGen is built with a modular architecture:

- **`Program.cs`** - Main application loop, UI rendering, and input handling
- **`AudioEngine.cs`** - Audio playback system using **Low-Level WinMM API (winmm.dll)**
- **`Generators.cs`** - Noise generation algorithms (White, Pink, Brown, Binaural)
- **`ConfigManager.cs`** - INI-based configuration management
- **`TestSuite.cs`** - Automated testing framework

### Building from Source

The project uses a simple batch script for compilation:

```batch
build.bat
```

This compiles all `.cs` files in the `Source/` directory using the .NET Framework C# compiler.

### Running Tests

```bash
NoiseGen.exe --test
```

Runs the automated test suite to verify noise generation algorithms.

---

## 📁 Project Structure

```
NOISE_GEN/
├── Source/
│   ├── Program.cs          # Main application
│   ├── AudioEngine.cs      # Audio playback
│   ├── Generators.cs       # Noise algorithms
│   ├── ConfigManager.cs    # Configuration
│   └── TestSuite.cs        # Tests
├── build.bat               # Build script
├── test.bat                # Test runner
├── *.ini                   # Profile files
├── README.md               # This file
└── LICENSE                 # MIT License
```

---

## 🤝 Contributing

Contributions are welcome! Feel free to:

- 🐛 Report bugs
- 💡 Suggest new features
- 🔧 Submit pull requests
- 📖 Improve documentation

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

**TL;DR:** You can use, modify, and distribute this software freely, just keep the attribution! 🎉

---

## 🙏 Acknowledgments

- Built using **Low-Level WinMM API** for high-performance, lightweight audio playback
- Inspired by the need for a truly lightweight noise generator
- Thanks to the open-source community!

---

<div align="center">

**Made with ❤️ for focus, relaxation, and better sleep**

</div>
