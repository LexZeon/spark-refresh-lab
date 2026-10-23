# Display redraw reproduction

**The affected user reports that visible Ball and Canvas animation markedly alleviate typing-triggered brightness pulses on the MSI / RTX Spark machine below.** This is a subjective result from one machine, with no established physical cause or universal-fix claim. Version 0.3 offers these two visible methods for continuous use; the earlier hidden test failed to resolve the symptom.

## Affected configuration

- MSI Prestige N16 Flip AI+ C1XMT, RTX Spark N1X 6144-core Blackwell GPU.
- Windows 11 Pro 26H1 ARM64, build 28000.3231.
- Samsung SDC4222 / ATNA60HC04-0 internal 3840 × 2400 Tandem OLED.
- Verified GPU/NPU driver during these tests: 617.42 / 32.0.16.1742. Previous 621.01 and MSI OEM 616.62 also reproduced the symptom; identical clean-install procedures were not established for all versions.
- Default GPU memory allocation restored to 64 GB. Windows output UI reported RGB, full range, 8 bpc, SDR; TrueColor Display P3 profile.

These details describe the tested Windows ARM64 system. New comparisons must record their own current settings.

## User-observed sequence

1. Stop visible animations. Leave the desktop mostly static for approximately 10 seconds.
2. Type individual English characters or Pinyin in Notepad or another input box.
3. Observe a brief perceived brightness increase, particularly near the screen corners and in dark-gray areas.
4. Enable the browser baseline's moving-dot rAF animation while it is actually visible. The user reports substantial relief.
5. Cover the animation with another window/page. The user reports that the benefit disappears. An open background tab is insufficient.

Active computer interaction or screen capture can mask the symptom. Judge a comparison with automation and capture stopped. Pure RGB 0 black looks normal; near-black gray has colored grain. Its relationship to the brightness pulse is unconfirmed.

## Visible multimethod comparison and v0.3

In the local v0.2 comparison, the user reported marked relief from **Ball** (rAF-driven CSS translation) and **Canvas** (rAF-driven Canvas 2D drawing). Native WinForms/GDI felt worse, and WPF Rendering/OnRender had no reported effect. Exact exposure durations and objective severity measurements were not supplied. See [the complete result note](results-2026-10-23-local-multimethod.md).

Version 0.3 retains the Ball and Canvas drawing content and 252 × 22 logical-pixel drawing area. A 50-pixel Exit button makes the complete topmost visible strip 302 × 22 logical pixels, scaled by Windows. Separate Chinese and English apps each expose both continuous methods, and only one language version can run at a time. Both controllers open stopped by default. The English `Start moving ball.vbs` / `Start canvas redraw.vbs` and Chinese `小球移动（持续）.vbs` / `画布重画（持续）.vbs` launch without a duration limit. Stop pauses the animation; Exit closes the application. There is no autostart or display-setting change.

A visible workaround must remain actually visible. This version does not establish that hidden rendering works, that the panel is refreshed continuously, or that a driver defect has been repaired. Its x64 host runs compatibly on the tested ARM64 machine and is not a native ARM64 build.

## Earlier hidden-mode result

In the first actual v0.1 hidden-mode trial, the user reported that flicker persisted while the process was verified active, the host HWND was genuinely hidden, and rAF callbacks continued at approximately 60/s. The [original result note](results-2026-10-23.md) is preserved. This does not establish that every hidden rendering method must fail.

WebView2/compositor visibility rules can suppress rendering even when a process or callback loop continues. A background workload cannot be assumed to reproduce the successful visible-animation case. The retained hidden and native CLI probes are historical diagnostics, not promoted workarounds.

## Historical unsuccessful system comparisons

Fixed Windows 60 / 120 Hz with DRR off, G-SYNC off, MSI display power-saver off, brightness changes, True Color presets, MSI Balanced mode, NVIDIA maximum-performance preference, temporary MPO override with reboot (then restored), HAGS and NVIDIA overlay disabled with reboot, default GPU memory allocation and reboot, and AC versus battery did not resolve the reported symptom.

These are historical trials, not a statement that all settings were disabled together or remain disabled. During a later read-only audit, G-SYNC was on for full-screen applications; DRR was observed off and later on; Windows VRR and windowed-game optimizations were on. Driver reinstalls and manual comparisons can change the baseline.

An ordinary reinstall plus user shutdown/power-on produced a possible slight subjective improvement, followed by continued flicker. Full kernel cold-start completion was not verified.

## Evidence limits and engineering questions

Callback counts and application draw counts do not measure DWM presentation or physical scanout. PSR/Panel Replay, internal dithering, PWM, and panel brightness have not been measured. There has been no external-display comparison. The tool neither records typed text nor captures the screen, and it does not upload its logs automatically.

Engineering feedback sought: a model-supported comparison of the idle-to-redraw path and minimal Windows ARM64 / RTX Spark-compatible display/ETW logging instructions. A phone video could compare static typing, visible animation, covered animation, and hidden mode; no video is bundled.
