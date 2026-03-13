# Smoke Music Player

Smoke Music Player is a dynamic 2D audio visualizer built in Unity. It uses real-time FFT (Fast Fourier Transform) analysis to drive a fluid simulation (smoke), creating an immersive and interactive music experience.

## Features

- **Real-time Audio Visualization**: Supports `.mp3` and `.wav` files.
- **Fluid Simulation**: Powered by GPU Compute Shaders for high performance (60+ FPS).
- **Interactive Smoke**: Influence the smoke density and velocity with mouse drag interactions.
- **Customizable Presets**: Adjust simulation parameters (viscosity, diffusion, color) and save/load your favorite configurations.
- **Cross-Platform Support**: Optimized for Windows Standalone with a CPU-based fallback for older hardware.

## Getting Started

### Prerequisites

- [Unity 2020.2.10f1](https://unity3d.com/get-unity/download/archive) or newer.
- A GPU with Compute Shader support is recommended for the best experience.

### Installation

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/RagnarokFate/SmokeMusicPlayer.git
   ```
2. **Open in Unity**:
   - Open Unity Hub.
   - Click **Add** and select the cloned folder.
   - Open the project with the correct Unity version.

3. **Build or Run**:
   - Open the main scene located in `Assets/Scenes/`.
   - Press **Play** in the Unity Editor or go to **File > Build Settings** to create a standalone executable.

## Usage Guide

1. **Load Music**: Click the "Load" button in the UI or drag and drop a `.wav`/`.mp3` file into the designated area (if supported by build).
2. **Playback Controls**: Use standard Play/Pause, Skip, and Volume controls.
   - **Speed Control**: Press `1` for 0.5x, `2` for 1.0x (normal), and `3` for 2.0x speed. This affects both music pitch and smoke animation speed.
3. **Interact**: Click and drag your mouse across the visualization window to disturb the smoke.
4. **Configure**: Use the settings panel to tweak the simulation:
   - **Viscosity**: Control how "thick" the smoke feels.
   - **Diffusion**: Control how fast the smoke spreads.
   - **Color Palette**: Choose different color schemes for the visualization.
5. **Save Presets**: Once you find a look you like, name and save your preset to access it later.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Developed by RagnarokFate.
Inspired by Jos Stam's Stable Fluids algorithm.
