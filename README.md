# 🎵 NoiseGen - Lightweight Noise Generator

<div align="center">

**A minimal, efficient white noise generator with customizable sound profiles and accessibility focus**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)]()
[![.NET Framework](https://img.shields.io/badge/.NET-4.0-512BD4.svg)]()

</div>

---

## 📖 Overview

NoiseGen is a **lightweight, terminal-based noise generator** designed for minimal system footprint and maximum customization. Perfect for focus, relaxation, or sleep, it generates true random noise in real-time rather than looping pre-recorded sounds. 

Now featuring a dedicated **Color Blind Mode** for enhanced accessibility.

---

## 🌟 What Makes NoiseGen Unique?

While many noise generators exist, NoiseGen is built differently:

- 🎚️ **Live TUI Mixing Board**: Most CLI tools are "one-shot" commands. NoiseGen gives you a live, interactive dashboard to adjust volumes and toggle channels on the fly without restarting.
- 🔢 **Pure Algorithmic Synthesis**: We don't use audio loops. Every sound is generated in real-time using pure mathematics (White, Pink, Brown noise and Binaural Beats), ensuring zero "loop seams" or repeating patterns.
- 🪶 **Zero Distraction, Zero Bloat**: No heavy browser tabs or complex GUI frameworks. It runs in a tiny corner of your terminal with a memory footprint smaller than an empty Chrome tab.
- 🎨 **Accessibility First**: Unlike most terminal apps, NoiseGen includes a dedicated **Color Blind Mode** as a primary feature, ensuring the visual mixer is usable by everyone.

---

### ✨ Key Features

- 🖥️ **Terminal UI**: Clean, visual interface with performance monitoring
- 🎚️ **Multiple Noise Types**: White, Pink, and Brown noise generators
- 🧠 **Binaural Beats**: Focus (14Hz Beta), Relax (7Hz Alpha), Sleep (4Hz Theta)
- 💾 **Profile Management**: Save, load, and delete custom sound configurations
- 🎨 **Color Blind Mode**: High-contrast, color-blind friendly visual palette
- ⚡ **Ultra-Lightweight**: Minimal RAM core (~15MB) and CPU usage (<1%)
- 🎛️ **Precision Control**: Real-time incremental volume adjustments
- 🔄 **True Random Generation**: No loops, seamless audio playback via low-level WinMM API

---

## 🚀 Quick Start

### Prerequisites

- **Windows** (7/8/10/11) with .NET Framework 4.0 or higher
- **C# Compiler** (included with .NET Framework)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/TohnJravolta/NOISE_GEN.git
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

### Controls

| Key | Action |
|-----|--------|
| <kbd>↑</kbd> / <kbd>↓</kbd> | Navigate between channels |
| <kbd>←</kbd> / <kbd>→</kbd> | Adjust volume (±1% per step, hold for rapid adjustment) |
| <kbd>Space</kbd> / <kbd>Enter</kbd> | Toggle channel ON/OFF |
| <kbd>P</kbd> | Open Profile Menu (Load/Delete) |
| <kbd>S</kbd> | Save current configuration as a profile |
| <kbd>C</kbd> | Toggle **Color Blind Mode** |
| <kbd>Esc</kbd> | Quit application / Cancel menu |

### 🎨 Visual Modes

#### Regular Mode (Default)
- **ON Status**: Green 🟢
- **OFF Status**: Red 🔴 (High Visibility)
- **Gradient**: Green (Low) → Yellow (Med) → Red (Loud)

#### Color Blind Mode
- **Palette**: High-contrast scheme avoiding Red/Green combinations.
- **Gradient**: White (Low) → Cyan (Med) → Yellow (High Visibility)
- **Toggle**: Press <kbd>C</kbd> to switch modes. The setting is automatically saved!

### 💾 Profile Management

#### Saving a Profile
1. Press <kbd>S</kbd> to enter save mode.
2. Type a name (letters, numbers, `_`, `-` allowed).
3. Press <kbd>Enter</kbd> to save.

#### Loading a Profile
1. Press <kbd>P</kbd> to open the bridged profile menu.
2. Use <kbd>↑</kbd> / <kbd>↓</kbd> to select.
3. Press <kbd>Enter</kbd> to load.

#### Deleting a Profile
1. Open the profile menu (<kbd>P</kbd>).
2. Highlight the profile you wish to remove.
3. Press <kbd>Delete</kbd>.
4. Confirm with <kbd>Y</kbd> (Yes) or cancel with <kbd>N</kbd> (No).

> **Note:** Your last session is automatically saved and restored on next launch!

---

## 🎨 Noise Types explained

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

NoiseGen is built with a focus on stability and performance:

- **`Program.cs`** - State machine, input buffering (with async key-hold), and artifact-free UI drawing.
- **`AudioEngine.cs`** - Memory-safe audio playback using low-level WinMM buffers and manual pinning.
- **`Generators.cs`** - Math-based noise algorithms for seamless playback.
- **`ConfigManager.cs`** - C# 5 compatible INI management.

### Building & Testing

- **Build**: Run `build.bat` to compile `NoiseGen.exe`.
- **Test**: Run `test.bat` or `NoiseGen.exe --test` to verify generators and persistence.

---

## 📁 Project Structure

```
NOISE_GEN/
├── Source/
│   ├── Program.cs          # Main application & TUI
│   ├── AudioEngine.cs      # Audio processing & Drivers
│   ├── Generators.cs       # Noise & Binaural algorithms
│   ├── ConfigManager.cs    # Configuration persistence
│   └── TestSuite.cs        # Automated self-tests
├── build.bat               # Simple build script
├── test.bat                # Test execution script
└── README.md               # Documentation
```

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

**Made with ❤️ for focus, relaxation, and accessibility**

</div>
