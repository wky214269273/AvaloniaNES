# AvaloniaNES

[English](./README.md)

一款基于.NET8 以及 AvaloniaUI 实现的跨平台 NES 模拟器，该项目主要用于模拟器技术的研究与学习，并不具备过多的商业化功能实现（例如金手指、插件等）

## 模块实现

### CPU

完整实现 olc6502 的所有指令集和寻址模式（包括不支持的指令集以及一些寻址 Bug），完整匹配每个指令集的调度周期。

### PPU

95%实现 PPU 的功能，包括 nametable、palette、sprite、scanline、rendering 等，未实现的部分集中于硬件的一些原生 Bug，有少部分游戏会利用硬件 Bug 进行渲染，由于未主动实现这些 Bug 模拟，因此部分游戏的特定场景渲染可能会不正常。

### 卡带

完整实现卡带的所有功能，部分实现 Mapper，由于 Mapper 的数量过于庞大，可能需要长期维护。当前仅实现 Mapper 000、001、002、003、004、066，基本上可以覆盖 8 成左右的游戏

### 像素显示器

完整实现高性能的像素图像绘制

### APU

暂未实现任何 APU 功能，音频涉及许多专业知识，后续随 Mapper 更新慢慢补充

### 按键映射

完整实现 Player 1 与 Player 2 的按键映射

### 模拟器

支持导入卡带、移除卡带和重置卡带功能，支持 Debug 模式，可以运行过程中随时查看当前的 CPU 及 PPU 状态

## 怎么使用

### 编译及打包

使用 Microsoft Visual Studio 或 Rider 等 IDE 打开 AvaloniaNES.sln，编译并打包项目即可

### 运行

自行编译后运行 AvaloniaNES.exe 或者使用已发布的可执行程序执行。
注意我只会打包发布 Windows x64 版本以及 Linux x64 版本，其他平台请自行编译。

## 授权

遵循 MIT 协议，请自由使用。
