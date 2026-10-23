# Spark Refresh Lab

[English](#english) · [中文](#中文)

## English

**This tool shows a small visible animation to try to reduce the brief brightness pulse that can happen when typing into an otherwise still screen.**

The user of one MSI Prestige N16 Flip AI+ / NVIDIA RTX Spark laptop has tested both options: **method 1, Moving Ball, and method 2, Canvas Redraw, markedly alleviate the flicker on that machine.** This is one user's experience on one computer. It does not establish that the tool will work on other computers, or that the cause has been identified or repaired.

### Download and use

**Chinese and English are separate app packages. Each includes both animation methods. Choose one language and run only that version.**

1. Download the complete [English portable ZIP](https://github.com/LexZeon/spark-refresh-lab/releases/download/v0.3.0-experimental/SparkRefreshLab-0.3.0-en-US-win-x64.zip), then extract the whole ZIP into a folder. Keep the EXE, DLLs, configuration file, and launch scripts together. The [release page](https://github.com/LexZeon/spark-refresh-lab/releases/tag/v0.3.0-experimental) also offers a separate [Chinese portable ZIP](https://github.com/LexZeon/spark-refresh-lab/releases/download/v0.3.0-experimental/SparkRefreshLab-0.3.0-zh-CN-win-x64.zip). GitHub's automatic `Source code` archives contain source files, not the ready-to-run app.
2. Double-click **`Open controls.vbs`** or **`SparkRefreshLab-EN.exe`** in the English folder. The controller opens stopped. Choose **Moving Ball or Canvas Redraw**, using just one method at a time.
3. A small, topmost dark-gray strip appears near the bottom-right of the screen. **Keep the moving dot and its strip actually visible**, then type normally and judge the effect. It continues running until you stop or exit; there is no 60-second limit. Click **Exit** on the right of the strip to finish.

Covering the animation with another window may remove the benefit. There is currently no established effective mode that remains completely hidden with no animation visible.

You can also double-click `Start moving ball.vbs` to start Moving Ball, or `Start canvas redraw.vbs` to start Canvas Redraw. Starting the same version while it is already running opens its current controller; choose the other method there to switch. Exit the current version before changing languages. Both languages share the same single-instance restriction.

### Stop, Exit, and the Windows taskbar

- **Stop** pauses the animation, hides the strip, and returns to the controller. The app remains open and may retain WebView2 processes.
- **Exit** closes the app and ends its refresh activity. Use the strip's Exit button, the controller's Exit button, the tray's Exit menu item, or close the controller window.
- **Pin to the Windows taskbar:** open the controller, right-click its taskbar icon, and choose **Pin to taskbar**. Later, open the controller from that icon and choose a method. Clicking the same pinned app while it is running shows its current controller. The app does not pin itself automatically and has no autostart.

Use only one method at a time and stop other refresh tools or browser animations. Stop immediately if a method feels worse.

### What the user reported

| Method | User's experience on this computer | Ordinary v0.3 controls |
| --- | --- | --- |
| 1 Moving Ball | Markedly alleviates flicker | Available for continuous use |
| 2 Canvas Redraw | Markedly alleviates flicker | Available for continuous use |
| 3 Native GDI drawing | Felt worse | Not offered |
| 4 WPF drawing | No effect | Not offered |

See [the user's comparison record](docs/results-2026-10-23-local-multimethod.md). These are subjective observations without instrumented brightness measurements. The earlier fully hidden v0.1 method still flickered; its [original result](docs/results-2026-10-23.md) is preserved.

For your own comparison, fully exit the app, wait about 10 seconds, and type a short English or Pinyin sample. Then enable one method, wait about 10 seconds, and type the same sample. Stop screen capture and remote automation while observing, because these activities have masked the symptom. An increasing counter does not establish relief.

### Runtime requirements

Extract the complete ZIP to use the tool. The app does not install a display driver or require administrator elevation. Your computer must **already have .NET Framework 4.8.1 and Microsoft Edge WebView2 Evergreen Runtime installed**; neither runtime is bundled in the ZIP.

The release is **x64**, used in the x64-compatible environment on the affected ARM64 Windows computer. It is **not a native ARM64 app**, and compatibility with every Windows ARM computer has not been tested.

The tool does not change the display refresh rate, brightness, ICC profile, registry, driver, or global browser settings. It does not record typed text, capture the screen, or automatically upload logs. Its animation uses local content. The installed Microsoft WebView2 Runtime has its own update and service behavior.

## 中文

**给“画面静止时，一打字屏幕就跳亮一下”的电脑做一个可见的小动画，尝试减轻闪烁。**

这台 MSI Prestige N16 Flip AI+ / NVIDIA RTX Spark 电脑的使用者已经实测：**方法 1「小球移动」和方法 2「画布重画（Canvas）」都能明显缓和闪烁。** 这是同一台电脑上的使用者反馈，尚不能保证其他电脑也有效，也没有修复或确认闪烁的根本原因。

### 下载后怎么用

**中文版、英文版是两个单独的软件包，每个版本都包含小球和画布两种方式。只选一种语言，不要同时开两个版本。**

| 界面语言 / Interface language | 完整程序包 / Complete portable ZIP |
| --- | --- |
| English | [SparkRefreshLab-0.3.0-en-US-win-x64.zip](https://github.com/LexZeon/spark-refresh-lab/releases/download/v0.3.0-experimental/SparkRefreshLab-0.3.0-en-US-win-x64.zip) |
| 简体中文 | [SparkRefreshLab-0.3.0-zh-CN-win-x64.zip](https://github.com/LexZeon/spark-refresh-lab/releases/download/v0.3.0-experimental/SparkRefreshLab-0.3.0-zh-CN-win-x64.zip) |

1. 从上表下载一种语言的**完整 ZIP**，然后把整个 ZIP 解压到一个文件夹。不要只取出 EXE；程序需要同文件夹里的 DLL、配置文件和脚本。[版本页面](https://github.com/LexZeon/spark-refresh-lab/releases/tag/v0.3.0-experimental)中 GitHub 自动提供的 `Source code` 是源码，不是可直接运行的程序包。
2. 中文版双击 **`打开控制面板.vbs`** 或 **`SparkRefreshLab-ZH.exe`**。选择 **「小球移动」或「画布重画」中的一个**，不用同时开两个。程序默认停止，只有选好方式才开始。英文版对应文件和按钮见上方 English instructions。
3. 屏幕右下方会出现一条置顶的深灰色小条。**让小点一直动，并保持这条小条实际可见**，再正常打字观察。它会一直运行，没有 60 秒自动停止。想结束时，点击小条右侧的 **「退出」**。

小条被其他窗口遮住后，之前的改善可能消失。目前没有验证有效的“完全隐藏、不显示任何动画”的办法。

中文版也可双击 `小球移动（持续）.vbs` 或 `画布重画（持续）.vbs` 直接启动。程序已经运行时，启动同一版本会打开当前控制面板；需要换方式就在面板中选择。切换语言前先退出当前版本。

### 停止、退出和固定到任务栏

- **停止**：暂停动画、隐藏小条、返回控制面板。程序仍在，可能保留 WebView2 进程。
- **退出**：关闭本工具，结束它的刷新活动。小条上的「退出」、控制面板的「退出」、托盘菜单的「退出」，以及关闭控制面板窗口，都可以完整退出。
- **固定到 Windows 任务栏**：打开控制面板后，右键任务栏上的程序图标，选择「固定到任务栏」。以后从任务栏打开面板，再选一种方式；程序已在运行时，点同一个固定图标会显示当前面板。程序不会自动固定任务栏，也没有开机自启。

每次只用一种方式，先关闭其他刷新工具或浏览器小球。若某种方式让眼睛更难受，立即停止。

### 这次实际测到了什么效果

| 方法 | 这台电脑上使用者的反馈 | v0.3 普通界面 |
| --- | --- | --- |
| 1 小球移动 | 明显缓和闪烁 | 提供，可持续运行 |
| 2 画布重画（Canvas） | 明显缓和闪烁 | 提供，可持续运行 |
| 3 原生 GDI 绘图 | 感觉更难受 | 不提供 |
| 4 WPF 绘图 | 没有效果 | 不提供 |

详见[使用者对照记录](docs/results-2026-10-23-local-multimethod.md)。这些是主观体验，还没有用仪器测量屏幕亮度。先前 v0.1 的完全隐藏方式仍然闪烁，其[原始记录](docs/results-2026-10-23.md)予以保留。

如果想自己对照：完全退出程序，等约 10 秒，输入一段英文或拼音；再开一种方式，等约 10 秒，输入相同文字。观察时停止截图和远程自动操作，因为这些活动曾掩盖症状。不要只看程序计数就判断有效。

### 运行环境

完整 ZIP 解压后即可运行本工具，无需为本工具安装显示驱动或使用管理员权限。电脑需要**已经安装 .NET Framework 4.8.1 和 Microsoft Edge WebView2 Evergreen Runtime**；ZIP 不包含这两个运行环境。

发布的程序是 **x64 版本**，已在上述 ARM64 Windows 电脑的 x64 兼容环境中使用，**不是原生 ARM64 程序**。尚未验证所有 Windows ARM 电脑的兼容性。

本工具不修改显示刷新率、亮度、ICC、注册表、驱动或全局浏览器设置。它不会记录输入的文字、不会截图，也不会自动上传日志。内嵌动画只使用本地页面；已安装的 Microsoft WebView2 Runtime 有其自身的更新和服务行为。

## Technical notes / 技术补充

The topmost strip is **302 × 22 logical pixels: a 252-pixel drawing area plus a 50-pixel Exit button**, scaled by Windows display scaling. Ball uses rAF-driven CSS `translateX`; Canvas uses rAF-driven Canvas 2D clear/draw of the same neutral dot. The original browser comparison found that covering the animation removed the benefit.

Logs contain modes, visibility, geometry, and callback/drawing counts. **A rAF callback, Canvas draw, GDI paint, or WPF Rendering callback is not a physical screen-refresh measurement.** Increasing counters do not establish continuous DWM presentation or panel scanout. Diagnostic sampling is part of the complete workload, so observed relief cannot be attributed to one callback alone.

The ordinary v0.3 controls offer only Ball and Canvas. Unsuccessful native GDI/WPF and hidden-mode CLI probes remain for historical developer diagnostics; they are not recommended workarounds. In the original v0.1 hidden trial, flicker persisted with a genuinely hidden host and approximately 60 rAF callbacks/s. This does not prove every possible hidden method must fail.

No cause involving hardware failure, PWM, PSR, Panel Replay, internal dithering, or a specific driver defect has been established. No external-display comparison or photometric measurement has been performed. The tested configuration and historical comparisons are in [the reproduction](docs/reproduction.md). The independent [browser baseline](web/static-vs-redraw.html) starts stopped; [the test sheet](docs/test-results-template.md) can be used to record another comparison.

### Build and dependencies

Build with `scripts/build.ps1` (defaults to x64). The build checks the pinned SDK package and used-file hashes. The x64 .NET Framework host uses its matching x64 WebView2 loader; an AnyCPU PE must not be assumed to select a native ARM64 CLR.

The Microsoft WebView2 SDK and loader retain their own license; see the dependency notices included by the build. Only source, documentation, and sanitized observations belong in Git. Build outputs, runtime profiles, and runtime logs are excluded. Review generated data before manually sharing it.

### Official references

- [WebView2 controller visibility](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.isvisible)
- [WebView2 browser flags and diagnostic-use limitations](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags)
- [Chromium native window occlusion tracking](https://chromium.googlesource.com/chromium/src/+/main/docs/windows_native_window_occlusion_tracking.md)
- [NVIDIA driver-feedback instructions](https://nvidia.custhelp.com/app/answers/detail/a_id/3141)
