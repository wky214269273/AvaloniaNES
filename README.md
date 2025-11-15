# AvaloniaNES

[中文](./README_CN.md)

A cross-platform NES emulator implemented based on .NET10 and AvaloniaUI. This project is mainly used for the research and learning of emulator technology, and does not have many commercialized features (such as cheat codes, plugins, etc.).

## Module Implementation

### CPU

Fully implements all instruction sets and addressing modes of olc6502 (including unsupported instruction sets and some addressing bugs), and completely matches the scheduling cycles of each instruction set.

### PPU

95% of PPU functions are implemented, including nametable, palette, sprite, scanline, rendering, etc. The unimplemented parts are concentrated on some native bugs of the hardware. A small number of games will use hardware bugs for rendering. Since these bug simulations are not actively implemented, the rendering of specific scenes in some games may be abnormal.

### Cartridge

Fully implements all functions of the cartridge, and partially implements Mappers. Due to the large number of Mappers, long-term maintenance may be required. Currently, only Mapper 000, 001, 002, 003, 004, and 066 are implemented, which can basically cover about 80% of the games.

### Pixel Display

Fully implements high-performance pixel image rendering.

### APU

No APU functions have been implemented yet. Audio involves a lot of professional knowledge, which will be supplemented gradually with Mapper updates in the future.

### Key Mapping

Fully implements key mapping for Player 1 and Player 2.

### Emulator

Supports functions such as importing cartridges, removing cartridges, and resetting cartridges. It also supports Debug mode, allowing you to check the current status of CPU and PPU at any time during operation.

## How to Use

### Compilation and Packaging

Open AvaloniaNES.sln with IDEs such as Microsoft Visual Studio or Rider, compile and package the project.

### Running

Run AvaloniaNES.exe after compiling by yourself or use the released executable program.Note that I will only package and release the Windows x64 version and Linux x64 version. For other platforms, please compile by yourself.

## License

Apache License 2.0
