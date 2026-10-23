# Spark Refresh Lab

Experimental WebView2 redraw probe for Windows on ARM / NVIDIA RTX Spark.
**The affected user's first hidden-mode trial did not eliminate the flicker. This is a diagnostic experiment, not a display-driver fix.**

## 中文使用说明

这个实验来自以下对照：静止画面中打字会触发短暂亮度跳动；浏览器中的一个小点持续移动时明显舒服，动画被其他窗口遮住后改善消失。

**首次实际测试结果：隐藏版正在运行、窗口确实隐藏、网页回调仍约每秒60次时，使用者仍反馈闪烁。因此它未解决该机器的闪烁，不应作为已验证的防闪工具推荐。** 详见 [测试结果](docs/results-2026-10-23.md)。

本工具用已安装的 Microsoft Edge WebView2 运行同类动画，并提供停止、真正隐藏、可见对照三个模式。隐藏模式不使用透明像素或屏外小窗假装“没有显示”。只有托盘图标保留，用来暂停、显示控制窗口和退出。

- 普通打开程序：先显示控制窗口，默认停止。
- 隐藏模式：渲染宿主窗口真正隐藏。是否仍有网页回调、是否能缓解，均需测试。
- 可见对照：小型普通窗口显示中性灰小点，不会自动置顶；它被遮住后可能失效。
- 托盘菜单可以随时停止或退出；退出即结束本工具的刷新活动。
- 无开机自启，不改刷新率、亮度、ICC、注册表、驱动或全局浏览器设置。

建议每次只比较一个模式：停止 10 秒后在记事本输入同样的字，再分别试隐藏和可见模式。不要同时开原测试页动画或另一个刷新工具，否则无法知道哪个起效。若感觉更难受，立即停止。主观效果由使用者记录，程序不会自动判断“修复成功”。

完全无工具的基线请先“退出”，不要仅点击“停止”：停止会取消本页动画，但可能保留 WebView2 进程。隐藏／可见运行时每秒一次的诊断轮询本身也可能影响闲置行为，所以结果针对整个实验程序，不能只归因于 rAF。

## What this measures

The process records WebView2 controller visibility, actual host-window visibility, document visibility, and JavaScript callback counts. **A requestAnimationFrame callback is not a physical screen refresh.** A healthy process, a moving counter, or successful rendering calls do not establish continuous DWM presentation or panel scanout.

Hidden/occluded content may stop rendering or be throttled by the runtime or compositor. The opt-in background-timer experiment changes only this application's WebView2 arguments; it does not guarantee rAF, compositor, GPU-clock, PSR, or panel behavior. It is not a stable browser-feature contract.

## Build and dependencies

- Windows with .NET Framework 4.8.1 and Microsoft Edge WebView2 Evergreen Runtime.
- Build with the pinned SDK using `scripts/build.ps1` (defaults to x64). The tested release is an x64 .NET Framework host running compatibly on the ARM64 machine with its matching x64 loader; it is not a native ARM64 executable. The script verifies its SDK package and used-file hashes. An AnyCPU PE must not be assumed to select a native ARM64 CLR.
- The Microsoft WebView2 SDK and loader are Microsoft components with their own license, not relicensed by this project's MIT license. See the dependency notices included by the build.
- No new system driver or administrator installation is required by this experiment.

Only source, documentation and sanitized technical observations belong in this repository. Build outputs, local runtime profiles and logs are excluded from Git. Runtime-generated data must be reviewed before manually sharing it.

The embedded document has no remote resources or navigation. WebView2 Evergreen is an installed Microsoft runtime with its own update/service behavior; this project does not claim that the runtime makes zero background connections. No diagnostics are automatically uploaded by this application's code.

## Report a result

Use [the test sheet](docs/test-results-template.md). Record the mode, whether the animation was actually visible or covered, driver version, selected refresh rate and DRR/G-SYNC state. Do not infer symptom improvement from callback counts. The initial hidden-mode user result is recorded in [the results note](docs/results-2026-10-23.md); a new-tool visible/covered comparison has not been supplied.

Technical background and the original reproduction are in [docs/reproduction.md](docs/reproduction.md). The independent browser baseline is [web/static-vs-redraw.html](web/static-vs-redraw.html); it starts with animation disabled.

## Limits

This tool does not diagnose hardware failure, PWM sensitivity, PSR, Panel Replay or a specific driver bug. Browser visibility behavior can explain why a background page behaves differently, but does not by itself identify the cause of the display pulses. No external-display comparison or photometric measurement has been performed.

The earlier native GDI floating-window experiment made flashing worse; it is deliberately not included or launched here.

## Official references

- [WebView2 controller visibility](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.isvisible)
- [WebView2 browser flags and diagnostic-use limitations](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags)
- [Chromium native window occlusion tracking](https://chromium.googlesource.com/chromium/src/+/main/docs/windows_native_window_occlusion_tracking.md)
- [NVIDIA driver-feedback instructions](https://nvidia.custhelp.com/app/answers/detail/a_id/3141)

