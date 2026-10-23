# Display redraw reproduction

## Affected configuration

- MSI Prestige N16 Flip AI+ C1XMT, RTX Spark N1X 6144-core Blackwell GPU.
- Windows 11 Pro 26H1 ARM64, build 28000.3231.
- Samsung SDC4222 / ATNA60HC04-0 internal 3840 × 2400 Tandem OLED.
- Current verified GPU/NPU driver: 617.42 / 32.0.16.1742. Previous 621.01 and MSI OEM 616.62 also reproduced the symptom; identical clean-install procedures were not established for all versions.
- Default GPU memory allocation restored to 64 GB. Windows output UI reports RGB, full range, 8 bpc, SDR; TrueColor Display P3 profile.

This is an ordinary Windows laptop configuration, not the Linux DGX Spark / GB10 product.

## User-observed sequence

1. Stop visible animations. Leave the desktop mostly static for approximately 10 seconds.
2. Type individual English characters or Pinyin in Notepad or another input box.
3. Observe a brief perceived brightness increase, particularly near the screen corners and in dark-gray areas.
4. Enable the browser baseline's moving-dot rAF animation while it is actually visible. The user reports substantial relief.
5. Cover the animation with another window/page. The user reports that the benefit disappears. An open background tab is insufficient.

Active computer interaction or capture can mask the symptom. The effect must be judged with automation stopped. Pure RGB 0 black looks normal; near-black gray has colored grain. Its relationship to the brightness pulse is unconfirmed.

## Historical unsuccessful trials

Fixed Windows 60 / 120 Hz with DRR off, G-SYNC off, MSI display power-saver off, brightness changes, True Color presets, MSI Balanced mode, NVIDIA maximum-performance preference, temporary MPO override with reboot (then restored), HAGS and NVIDIA overlay disabled with reboot, default GPU memory allocation and reboot, and AC versus battery did not resolve the reported symptom.

These are historical trials, not a statement that all settings are currently disabled. During a later read-only audit, G-SYNC was on for full-screen applications; DRR was observed off and later on; Windows VRR and windowed-game optimizations were on. Driver reinstalls and manual setting comparisons mean a new trial must record its own baseline.

An ordinary reinstall plus user shutdown/power-on produced a possible slight subjective improvement, followed by continued flicker. Full kernel cold-start completion was not verified. No cause is established.

## Proposed hidden experiment

The new executable has a real hidden host-window mode and a separate visible browser-engine reference. It records callback/visibility diagnostics without recording typed characters or capturing the screen. In the first actual hidden-mode trial, the user reported that flicker persisted while the process was verified active, the real host HWND was hidden and rAF callbacks continued at approximately 60/s. See [the result note](results-2026-10-23.md). This does not establish that every hidden rendering method must fail.

WebView2/compositor visibility rules may suppress rendering even when a process or callback loop continues. A background workload cannot be assumed to reproduce the successful visible-animation case. PSR/Panel Replay, internal dither and physical panel scanout have not been measured.

Engineering feedback sought: a model-supported comparison for the idle-to-redraw path and minimal Windows ARM64 / RTX Spark-compatible display/ETW logging instructions. Phone video could compare static typing, visible animation, covered animation and hidden mode; no video is bundled.

