using System;

namespace SparkRefreshLab
{
    // Language is selected by the compiler, never by a mixed-language runtime switch.
    internal static class UiText
    {
#if UI_ENGLISH
        internal const string Locale = "en-US";
        internal const string AppTitle = "Screen Refresh Helper (English)";
        internal const string AppDescription = "English continuous moving-ball and canvas-redraw helper";
        internal const string FontName = "Segoe UI";
        internal static string T(string chinese, string english) { return english; }
#else
        internal const string Locale = "zh-CN";
        internal const string AppTitle = "屏幕刷新助手（中文版）";
        internal const string AppDescription = "中文版小球移动与画布重画持续运行助手";
        internal const string FontName = "Microsoft YaHei UI";
        internal static string T(string chinese, string english) { return chinese; }
#endif
        internal static string Exit { get { return T("退出", "Exit"); } }
        internal static string Stop { get { return T("停止", "Stop"); } }
        internal static string Controls { get { return T("打开控制面板", "Open controls"); } }
        internal static string BallButton { get { return T("① 小球移动（持续）", "1. Moving ball (continuous)"); } }
        internal static string CanvasButton { get { return T("② 画布重画（持续）", "2. Canvas redraw (continuous)"); } }
        internal static string Ball { get { return T("小球移动", "Moving ball"); } }
        internal static string Canvas { get { return T("画布重画", "Canvas redraw"); } }
        internal static string Stopped { get { return T("已停止", "Stopped"); } }
        internal static string StopAndReturn { get { return T("停止并返回控制面板", "Stop and return to controls"); } }
        internal static string Intro { get { return T(
            "这台电脑实测：小球移动和画布重画可缓解闪动。选择一种即可持续运行。\n小条需保持可见；点小条“退出”或关闭本窗口可完全关闭。",
            "Both methods helped on this computer. Pick one to keep it running.\nKeep the strip visible. Click Exit on the strip or close this window to quit."); } }
        internal static string InitialStatus { get { return T("已停止。请选择一种方式。", "Stopped. Choose a method."); } }
        internal static string InitialDetails { get { return T(
            "启动后持续运行，不会在60秒后自动停止。每次只运行一种方式。\r\n停止会返回本窗口；退出会完整关闭程序。",
            "Runs continuously until you stop or exit. Only one method runs at a time.\r\nStop returns to these controls. Exit closes the app completely."); } }
        internal static string Footer { get { return T(
            "关闭窗口或点退出会完全退出；运行时控制面板自动隐藏。",
            "Close this window or click Exit to quit. Controls hide while a method runs."); } }
        internal static string LogLabel { get { return T("本地记录：", "Local log: "); } }
        internal static string MethodLabel { get { return T("当前方式：", "Current method: "); } }
        internal static string Details { get { return T(
            "选择另一种方式会先停止当前方式。停止返回本窗口；退出完整关闭程序。",
            "Selecting another method stops the current one first. Stop returns here; Exit quits."); } }
        internal static string PreparingContinuous { get { return T(
            "：准备持续运行；停止或退出前不会自动结束。",
            ": preparing to run continuously until you stop or exit."); } }
        internal static string PreparingTimed { get { return T(
            "：正在准备，仍会遵守命令行指定的退出时间。",
            ": preparing; the command-line time limit still applies."); } }
        internal static string PreparingTrial { get { return T(
            "：准备中；开始后测试60秒，结束返回控制面板。",
            ": preparing a 60-second trial, then returning to controls."); } }
        internal static string TrialStatus { get { return T(
            "：60秒测试，结束后返回控制面板；可随时停止。",
            ": 60-second trial, then returns to controls. You may stop at any time."); } }
        internal static string RunningStatus { get { return T(
            "：正在运行，可从托盘停止或退出。",
            ": running. Use the tray to stop or exit."); } }
        internal static string Ready { get { return T("已准备好。可从托盘选择一种方式。", "Ready. Choose a method from the tray."); } }
        internal static string SignalFailed { get { return T(
            "另一语言或旧版本可能正在运行。请先退出该版本，再打开本版本；也可从托盘打开现有控制面板。",
            "Another language or an older version may already be running. Please exit it before opening this version, or open its controls from the tray."); } }
        internal static string DurationError { get { return T("--duration 必须是1到300秒。", "--duration must be 1..300 seconds."); } }
        internal static string ModeError { get { return T(
            "--mode 必须是 stopped、hidden、visible、ball、canvas、gdi、wpf 或 edge。",
            "--mode must be stopped, hidden, visible, ball, canvas, gdi, wpf or edge."); } }
        internal static string OptionError { get { return T("未知或不完整的参数：", "Unknown or incomplete option: "); } }
        internal static string AutoExitError { get { return T(
            "--auto-exit 必须同时提供 --duration 秒数或 --self-test。",
            "--auto-exit requires --duration seconds or --self-test."); } }
        internal static string StartupFailed { get { return T("启动失败，可关闭程序。", "Startup failed. You can close the app."); } }
        internal static string InitializationFailed { get { return T("准备失败，可退出后重试。", "Could not prepare the method. Exit and try again."); } }
        internal static string SwitchFailed { get { return T("切换失败，可退出后重试。", "Could not switch methods. Exit and try again."); } }
        internal static string Error(string message, Exception error)
        {
            return message + "\r\n" + T("错误代码：", "Error code: ") + "0x" + error.HResult.ToString("X8");
        }
    }
}
