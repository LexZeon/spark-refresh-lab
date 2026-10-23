# Visible multimethod result, 2026-10-23

**The affected user reports that Ball and Canvas markedly alleviate typing-triggered brightness flicker on one MSI Prestige N16 Flip AI+ C1XMT / NVIDIA RTX Spark N1X machine.** These observations support offering the two visible methods as an experimental workaround. They do not establish a universal fix, the root cause, or a repaired display driver.

## User comparison

The user tested the local v0.2 suite and reported:

| Method | User observation |
| --- | --- |
| Ball: original cosine CSS `translateX` animation in a genuinely visible WebView2 strip | Initially “第一种效果非常好”; subsequent feedback “第一第二都行” — reported marked relief |
| Canvas: rAF clear/draw of the same neutral dot | “第一第二都行” — reported relief alongside Ball |
| Native WinForms timer / GDI paint | “第三难受” — felt worse |
| WPF Rendering / OnRender | “第四没效果” — no reported effect |

A hands-off repeat was requested after the initial Ball report; the subsequent message supplied the comparative observations above. Exact durations, flicker severity, and independent luminance/presentation measurements were not supplied. The original browser comparison also found that covering its animation removed the benefit. There is no established effective fully hidden mode.

The tested system was Windows 11 Pro 26H1 ARM64 build 28000.3231, with GPU/NPU driver 617.42 / 32.0.16.1742. Prior driver versions also reproduced the original symptom; see [the reproduction](reproduction.md). These are observations of the complete application workload, including diagnostic sampling, rather than proof that a particular callback causes the relief.

## Version 0.3 behavior

Version 0.3 keeps the visible Ball and Canvas drawing content and geometry: a 252 × 22 logical-pixel drawing area with a 50-pixel Exit button, for a 302 × 22 logical-pixel topmost strip scaled by Windows. Separate Chinese and English apps each contain both methods; only one language version can run at a time. The controller starts stopped, while the English `Start moving ball.vbs` / `Start canvas redraw.vbs` and Chinese `小球移动（持续）.vbs` / `画布重画（持续）.vbs` launchers run without a duration limit. Stop pauses the animation and returns to the controller; Exit, the strip's Exit button, or closing the controller fully exits the program. There is no autostart.

The ordinary controls no longer offer the unsuccessful GDI, WPF, or hidden methods. Their legacy CLI diagnostics remain available for historical reproduction. The x64 .NET Framework host has been tested compatibly on this ARM64 system; a native ARM64 build has not been established. The portable release requires .NET Framework 4.8.1 and Microsoft Edge WebView2 Evergreen Runtime to be installed already.

## Preserved earlier observations

The [initial v0.1 hidden-mode failure](results-2026-10-23.md) remains separate and unchanged: the user still saw flicker with a genuinely hidden host and approximately 60 rAF callbacks/s. Callback activity did not demonstrate an effective workaround.

An earlier continuous-flicker observation reverted to typing-only flicker after the helper and its dedicated WebView2 processes exited. The precise causal timing, and which earlier drawing intervention, if any, caused that observation, were not established.

No conclusion is drawn about measured panel scanout, PSR/Panel Replay, internal dithering, PWM, or a physical hardware cause. No external-display comparison or photometric measurement has been performed.
