# Fluent Scrcpy

A modern, Fluent Design-based GUI for [Scrcpy](https://github.com/Genymobile/scrcpy), built with Python and PyQt6.

![Screenshot](docs/screenshot.png)

## Features

*   **Modern UI**: Built with [PyQt-Fluent-Widgets](https://github.com/zhiyiYo/PyQt-Fluent-Widgets), offering a native Windows 11 look and feel with Mica/Acrylic effects.
*   **Real-time Control**: Toggle settings like "Always on Top", "Fullscreen", "Stay Awake" instantly without restarting the connection.
*   **Wireless Connection**: Easily connect to devices over TCP/IP with a dedicated input field and "Switch to Wireless" button.
*   **Aspect Ratio Lock**: Unique feature to lock the scrcpy window to your device's native aspect ratio (e.g., 16:9), preventing black bars or distortion during resizing.
*   **ADB Control Panel**: Quick access to essential Android keys: Home, Recents, Back, Volume Mute, and Power directly from the device card.
*   **Device Detection**: Auto-detects connected devices and displays real model names (e.g., "Pixel 6" instead of generic names).
*   **Customizable**: Supports Light/Dark themes and custom accent colors.
*   **Localization**: Fully localized in English and Chinese (简体中文).

## Requirements

*   Python 3.8+
*   [Scrcpy](https://github.com/Genymobile/scrcpy) (Must be installed and added to your system PATH)
*   ADB (Usually comes with Scrcpy)

## Download

You can download the latest standalone executable from the [Releases](https://github.com/gaoxing64/Fluent-scrcpy/releases) page.

1.  Download `FluentScrcpy-v1.0.zip`.
2.  Extract the zip file.
3.  Run `FluentScrcpy.exe`.

## Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/gaoxing64/Fluent-scrcpy.git
    cd Fluent-scrcpy
    ```

2.  Install dependencies:
    ```bash
    pip install -r requirements.txt
    ```

3.  Run the application:
    ```bash
    python main.py
    ```

## Usage

1.  Connect your Android device via USB and enable USB Debugging.
2.  Open **Fluent Scrcpy**.
3.  Your device should appear in the "Home" tab.
4.  Click **"Start Mirroring"** to launch scrcpy.
5.  Use the **"Configuration"** tab to tweak video bitrate, resolution, and control settings.
6.  Use the **"App Settings"** tab to change the theme or language.

## Building from Source

To build a standalone executable (EXE), you can use PyInstaller:

```bash
pip install pyinstaller
pyinstaller FluentScrcpy.spec
```

## Credits

*   **Scrcpy**: [Genymobile/scrcpy](https://github.com/Genymobile/scrcpy)
*   **UI Framework**: [zhiyiYo/PyQt-Fluent-Widgets](https://github.com/zhiyiYo/PyQt-Fluent-Widgets)

## License

MIT License

---

# Fluent Scrcpy (中文介绍)

一个基于 Fluent Design 设计风格的 Scrcpy 图形化控制工具，使用 Python 和 PyQt6 构建。

## 功能特性

*   **现代 UI**: 采用 [PyQt-Fluent-Widgets](https://github.com/zhiyiYo/PyQt-Fluent-Widgets) 构建，提供原生的 Windows 11 外观，支持云母 (Mica) 和亚克力 (Acrylic) 效果。
*   **实时控制**: 实时切换“窗口置顶”、“全屏模式”、“保持唤醒”等设置，无需重启连接。
*   **无线连接**: 方便的无线连接功能，支持一键将有线连接切换为无线模式。
*   **锁定长宽比**: 独特的窗口比例锁定功能，强制 Scrcpy 窗口保持设备原本的比例（如 16:9），调整大小时自动吸附，告别黑边。
*   **ADB 控制面板**: 在设备卡片上直接提供常用按键：主页、多任务、返回、静音、电源。
*   **设备检测**: 自动检测已连接设备并显示真实的设备型号（如显示 "Pixel 6" 而不是 "Android Device"）。
*   **个性化**: 支持浅色/深色主题切换，以及自定义主题强调色。
*   **多语言**: 完美支持简体中文和英文。

## 运行环境

*   Python 3.8+
*   [Scrcpy](https://github.com/Genymobile/scrcpy) (必须已安装并添加到系统 PATH 环境变量中)
*   ADB (通常随 Scrcpy 一起附带)

## 下载

您可以从 [Releases](https://github.com/gaoxing64/Fluent-scrcpy/releases) 页面下载最新的独立可执行文件。

1.  下载 `FluentScrcpy-v1.0.zip`。
2.  解压压缩包。
3.  运行 `FluentScrcpy.exe`。

## 安装步骤

1.  克隆仓库:
    ```bash
    git clone https://github.com/gaoxing64/Fluent-scrcpy.git
    cd Fluent-scrcpy
    ```

2.  安装依赖:
    ```bash
    pip install -r requirements.txt
    ```

3.  运行程序:
    ```bash
    python main.py
    ```

## 使用说明

1.  通过 USB 连接您的 Android 手机并开启 USB 调试模式。
2.  打开 **Fluent Scrcpy**。
3.  您的设备应该会显示在“主页”选项卡中。
4.  点击 **“开始投屏”** 即可启动 Scrcpy。
5.  在 **“配置”** 选项卡中可以调整视频比特率、分辨率和控制选项。
6.  在 **“应用设置”** 选项卡中可以更改主题或语言。

## 许可证

MIT License
