# Scrcpy GUI Launcher Development Plan (PyQt-Fluent-Widgets)

This plan outlines the development of a modern, Windows 11-style GUI for scrcpy, focusing on a robust configuration interface and ease of use.

## 1. Project Initialization & Structure

* **Dependencies**: Set up `requirements.txt` with `PyQt6`, `PyQt-Fluent-Widgets`, and `adbutils`.

* **Structure**: Organize code into `app/common` (config, settings), `app/view` (UI pages), and `app/components`.

* **Resources**: specific folders for icons and translation files (`i18n`).

## 2. UI Framework & Theming (WinUI 3 Style)

* **Main Window**: Use `FluentWindow` for the standard navigation pane layout.

* **Theme Support**: Integrate Light/Dark mode switching (System default + manual toggle).

* **Localization (i18n)**:

  * Setup `QTranslator` infrastructure.

  * Create initial translation files for **English** and **Chinese**.

  * Ensure dynamic language switching without restart.

## 3. Configuration Interface ("MPV Style")

* **Visual Style**: Use `SettingCardGroup` and specific card types (`SwitchSettingCard`, `ComboBoxSettingCard`, `SpinBoxSettingCard`) to mimic the clean, list-based layout shown in the reference image.

* **Detailing**: Display the corresponding scrcpy CLI flag (e.g., `--max-size`) as the description/subtitle for each setting item, for clarity.

* **Categories**:

  * **Video**: Resolution, Bitrate, FPS, Codec (H.264/H.265/AV1).

  * **Audio**: Enable/Disable, Audio Source, Buffer.

  * **Control**: Show Touches, Stay Awake, Turn Screen Off.

## 4. Presets & Profiles System

* **Profile Selector**: A prominent dropdown or segmented control for:

  * **Fast Mode**: Low latency, lower resolution (e.g., 800p), lower bitrate.

  * **High Quality**: Max resolution, high bitrate (e.g., 16Mbps+), H.265.

  * **Custom**: Fully editable fields.

* **Logic**: Implementing the logic to auto-fill settings when a preset is selected and enable/disable specific controls accordingly.

## 5. Scrcpy Integration & Execution

* **Device Management**: Auto-detect connected devices via ADB and display them in a list.

* **Command Builder**: A robust engine to generate the final scrcpy command string based on current UI state and selected profile.

* **Process Runner**: Asynchronous execution of scrcpy to keep the GUI responsive.

## 6. Verification

* Verify layout matches the "MPV Settings" visual reference.

* Test language switching (CN/EN).

* Test preset application (Fast vs High Quality).

* Successful launch of scrcpy with generated parameters.

