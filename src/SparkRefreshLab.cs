using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

[assembly: AssemblyTitle("Spark Refresh Lab")]
[assembly: AssemblyVersion("0.1.0.0")]

namespace SparkRefreshLab
{
    internal enum RunMode { Stopped, Hidden, Visible }

    internal sealed class Options
    {
        internal RunMode Mode = RunMode.Stopped;
        internal int Duration;
        internal bool SelfTest;
        internal bool ExperimentalBackgroundTimers;
        internal static Options Parse(string[] args)
        {
            Options result = new Options();
            bool autoExitRequested = false;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--self-test") result.SelfTest = true;
                else if (arg == "--experimental-background-timers") result.ExperimentalBackgroundTimers = true;
                else if (arg == "--auto-exit") autoExitRequested = true;
                else if (arg == "--duration" && ++i < args.Length)
                {
                    if (!int.TryParse(args[i], out result.Duration) || result.Duration < 1 || result.Duration > 300)
                        throw new ArgumentException("--duration must be 1..300 seconds.");
                }
                else if (arg == "--mode" && ++i < args.Length)
                {
                    if (!Enum.TryParse<RunMode>(args[i], true, out result.Mode) || !Enum.IsDefined(typeof(RunMode), result.Mode))
                        throw new ArgumentException("--mode must be stopped, hidden or visible.");
                }
                else throw new ArgumentException("Unknown or incomplete option: " + arg);
            }
            if (result.SelfTest) result.Duration = 25;
            if (autoExitRequested && result.Duration == 0)
                throw new ArgumentException("--auto-exit requires --duration seconds or --self-test.");
            return result;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Options options;
            try { options = Options.Parse(args); }
            catch (Exception error) { MessageBox.Show(error.Message, "Spark Refresh Lab"); return 2; }
            bool created;
            using (Mutex mutex = new Mutex(true, @"Local\SparkRefreshLab-0.1", out created))
            {
                if (!created)
                {
                    if (!options.SelfTest && options.Duration == 0)
                        MessageBox.Show("程序已在运行。请从托盘打开控制台或退出。", "Spark Refresh Lab");
                    return 3;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    using (LabContext context = new LabContext(options)) Application.Run(context);
                }
                catch (Exception error)
                {
                    DiagnosticLog.Write("fatal", new { type = error.GetType().Name, hresult = error.HResult });
                    if (!options.SelfTest && options.Duration == 0)
                        MessageBox.Show(error.Message, "Spark Refresh Lab · 启动失败");
                    return 1;
                }
                finally { mutex.ReleaseMutex(); }
            }
            return Environment.ExitCode;
        }
    }

    internal static class DiagnosticLog
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
        internal static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SparkRefreshLab");
        internal static readonly string FilePath = Path.Combine(Root, "Logs", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Process.GetCurrentProcess().Id + ".jsonl");
        internal static void Write(string name, object data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.AppendAllText(FilePath, Serializer.Serialize(new { utc = DateTime.UtcNow.ToString("o"), eventName = name, data = data }) + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    internal sealed class RenderHost : Form
    {
        internal RenderHost()
        {
            Text = "Spark Refresh Lab · 可见对照";
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(220, 48);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            BackColor = Color.FromArgb(32, 32, 32);
            StartPosition = FormStartPosition.Manual;
        }
        internal void PlaceInCorner()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
    }

    internal sealed class LabContext : ApplicationContext
    {
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool IsWow64Process2(IntPtr process, out ushort processMachine, out ushort nativeMachine);
        private readonly Options options;
        private readonly RenderHost host = new RenderHost();
        private readonly Form console = new Form();
        private readonly TextBox details = new TextBox();
        private readonly Label status = new Label();
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly System.Windows.Forms.Timer pollTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer deadlineTimer = new System.Windows.Forms.Timer();
        private readonly Stopwatch lifetime = Stopwatch.StartNew();
        private readonly Stopwatch selfTestClock = new Stopwatch();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private readonly TaskCompletionSource<bool> browserExited = new TaskCompletionSource<bool>();
        private CoreWebView2Environment environment;
        private CoreWebView2Controller controller;
        private Task initializeTask;
        private TaskCompletionSource<bool> navigationReady;
        private RunMode wantedMode = RunMode.Stopped;
        private RunMode currentMode = RunMode.Stopped;
        private bool exiting, applying, polling, ready;
        private int selfTestStage = -1;
        private int modeRevision;
        private double lastCount, lastPollTime;
        private uint browserPid;

        internal LabContext(Options settings)
        {
            options = settings;
            SetupConsole();
            SetupTray();
            host.Resize += delegate { if (controller != null && !exiting) controller.Bounds = host.ClientRectangle; };
            host.FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!exiting) { e.Cancel = true; SetMode(RunMode.Stopped); } };
            // Force a real HWND without showing it. Hidden mode never uses transparency or off-screen placement.
            IntPtr hostHandle = host.Handle;
            IntPtr consoleHandle = console.Handle;
            pollTimer.Interval = 1000;
            pollTimer.Tick += async delegate { await PollAsync(); };
            deadlineTimer.Interval = 200;
            deadlineTimer.Tick += DeadlineTick;
            if (options.SelfTest || options.Duration > 0) deadlineTimer.Start();
            ushort processMachine, nativeMachine;
            bool architectureRead = IsWow64Process2(Process.GetCurrentProcess().Handle, out processMachine, out nativeMachine);
            DiagnosticLog.Write("startup", new { version = "0.1.0", pid = Process.GetCurrentProcess().Id,
                processArchitecture = Environment.Is64BitProcess ? "64-bit" : "32-bit",
                processEnvironmentArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
                processMachine = architectureRead ? "0x" + processMachine.ToString("X4") : "unknown",
                nativeMachine = architectureRead ? "0x" + nativeMachine.ToString("X4") : "unknown",
                mode = options.Mode.ToString(), selfTest = options.SelfTest,
                experimentalBackgroundTimers = options.ExperimentalBackgroundTimers,
                evidenceLimit = "rAF callback counts are not presentation, scanout or panel refresh measurements." });
            if (!options.SelfTest && options.Mode == RunMode.Stopped) console.Show();
            console.BeginInvoke(new Action(async delegate
            {
                if (options.SelfTest)
                {
                    await EnsureInitializedAsync();
                    if (ready && !exiting) { selfTestClock.Start(); selfTestStage = 0; SetMode(RunMode.Hidden); }
                }
                else if (options.Mode != RunMode.Stopped) SetMode(options.Mode);
            }));
        }

        private void SetupConsole()
        {
            console.Text = "Spark Refresh Lab · 控制台";
            console.ClientSize = new Size(600, 355);
            console.StartPosition = FormStartPosition.CenterScreen;
            console.Font = new Font("Microsoft YaHei UI", 9F);
            Label intro = new Label();
            intro.Text = "实验工具：隐藏可能被系统暂停，不能保证防闪。回调数 ≠ 实际屏幕刷新。\n隐藏模式会隐藏渲染窗；仅托盘保留退出入口。测试时请关闭此控制台。";
            intro.Bounds = new Rectangle(12, 10, 576, 44);
            console.Controls.Add(intro);
            AddButton("停止", 12, delegate { SetMode(RunMode.Stopped); });
            AddButton("完全隐藏测试", 110, delegate { SetMode(RunMode.Hidden); });
            AddButton("可见对照", 248, delegate { SetMode(RunMode.Visible); });
            AddButton("退出", 386, async delegate { await QuitAsync(); });
            status.Text = "已停止；尚未启动 WebView2。";
            status.Bounds = new Rectangle(12, 99, 576, 35);
            console.Controls.Add(status);
            details.Multiline = true;
            details.ReadOnly = true;
            details.ScrollBars = ScrollBars.Vertical;
            details.Bounds = new Rectangle(12, 137, 576, 180);
            details.Text = "仅加载随程序嵌入的本地页面。无系统/驱动更改、无自启动。\r\n日志：" + DiagnosticLog.FilePath;
            console.Controls.Add(details);
            Label footer = new Label();
            footer.Text = "关闭此控制台仅隐藏窗口。完全退出请点“退出”或托盘菜单“退出”。";
            footer.Bounds = new Rectangle(12, 325, 576, 23);
            console.Controls.Add(footer);
            console.FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!exiting) { e.Cancel = true; console.Hide(); } };
        }

        private void AddButton(string text, int left, EventHandler action)
        {
            Button button = new Button();
            button.Text = text;
            button.Bounds = new Rectangle(left, 60, text == "停止" ? 88 : 128, 30);
            button.Click += action;
            console.Controls.Add(button);
        }

        private void SetupTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("停止", null, delegate { SetMode(RunMode.Stopped); });
            menu.Items.Add("完全隐藏测试", null, delegate { SetMode(RunMode.Hidden); });
            menu.Items.Add("可见对照", null, delegate { SetMode(RunMode.Visible); });
            menu.Items.Add("控制台", null, delegate { if (!exiting) { console.Show(); console.Activate(); } });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出", null, async delegate { await QuitAsync(); });
            tray.Icon = (Icon)SystemIcons.Application.Clone();
            tray.Text = "Spark Refresh Lab · 已停止";
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { if (!exiting) { console.Show(); console.Activate(); } };
            tray.Visible = true;
        }

        private async Task EnsureInitializedAsync()
        {
            if (initializeTask == null) initializeTask = InitializeAsync();
            await initializeTask;
        }

        private async Task InitializeAsync()
        {
            try
            {
                string flags = options.ExperimentalBackgroundTimers ? "--disable-background-timer-throttling" : "";
                CoreWebView2EnvironmentOptions envOptions = new CoreWebView2EnvironmentOptions(flags);
                string userData = Path.Combine(DiagnosticLog.Root, options.SelfTest ? "WebView2-SelfTest-" + Process.GetCurrentProcess().Id : "WebView2");
                environment = await CoreWebView2Environment.CreateAsync(null, userData, envOptions);
                environment.BrowserProcessExited += delegate { browserExited.TrySetResult(true); };
                foreach (CoreWebView2ProcessInfo info in environment.GetProcessInfos())
                    if (info.Kind == CoreWebView2ProcessKind.Browser) browserPid = (uint)info.ProcessId;
                if (exiting) return;
                controller = await environment.CreateCoreWebView2ControllerAsync(host.Handle);
                browserPid = controller.CoreWebView2.BrowserProcessId;
                if (exiting) { controller.Close(); controller = null; return; }
                controller.Bounds = host.ClientRectangle;
                controller.DefaultBackgroundColor = Color.FromArgb(32, 32, 32);
                controller.IsVisible = false;
                CoreWebView2 core = controller.CoreWebView2;
                browserPid = core.BrowserProcessId;
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.AreDefaultScriptDialogsEnabled = false;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.IsZoomControlEnabled = false;
                core.Settings.IsWebMessageEnabled = false;
                core.Settings.IsGeneralAutofillEnabled = false;
                core.Settings.IsPasswordAutosaveEnabled = false;
                core.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs e)
                {
                    bool allowed = e.Uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase) ||
                        e.Uri.StartsWith("data:text/html", StringComparison.OrdinalIgnoreCase);
                    e.Cancel = !allowed;
                    string scheme = e.Uri.IndexOf(':') >= 0 ? e.Uri.Substring(0, e.Uri.IndexOf(':')) : "none";
                    DiagnosticLog.Write("local-navigation-starting", new { navigationId = e.NavigationId, allowed = allowed, scheme = scheme });
                };
                core.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; };
                core.PermissionRequested += delegate(object sender, CoreWebView2PermissionRequestedEventArgs e) { e.State = CoreWebView2PermissionState.Deny; };
                core.AddWebResourceRequestedFilter("http://*", CoreWebView2WebResourceContext.All);
                core.AddWebResourceRequestedFilter("https://*", CoreWebView2WebResourceContext.All);
                core.WebResourceRequested += delegate(object sender, CoreWebView2WebResourceRequestedEventArgs e)
                {
                    e.Response = environment.CreateWebResourceResponse(new MemoryStream(new byte[0]), 403, "Blocked", "Content-Type: text/plain");
                };
                navigationReady = new TaskCompletionSource<bool>();
                core.NavigationCompleted += delegate(object sender, CoreWebView2NavigationCompletedEventArgs e)
                {
                    DiagnosticLog.Write("local-navigation-completed", new { navigationId = e.NavigationId, success = e.IsSuccess, status = e.WebErrorStatus.ToString() });
                    if (e.IsSuccess) navigationReady.TrySetResult(true);
                    else if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
                        navigationReady.TrySetException(new InvalidOperationException("Local navigation failed: " + e.WebErrorStatus));
                };
                using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream("animation.html"))
                using (StreamReader reader = new StreamReader(source, Encoding.UTF8)) core.NavigateToString(reader.ReadToEnd());
                if (await Task.WhenAny(navigationReady.Task, Task.Delay(10000)) != navigationReady.Task)
                    throw new TimeoutException("Local document navigation did not finish within 10 seconds.");
                await navigationReady.Task;
                if (exiting) return;
                ready = true;
                DiagnosticLog.Write("initialized", new { runtimeVersion = environment.BrowserVersionString, browserPid = browserPid,
                    controllerIsVisible = controller.IsVisible, hostIsWindowVisible = IsWindowVisible(host.Handle), flags = flags });
                status.Text = "WebView2 已初始化；从托盘选择模式。";
            }
            catch (Exception error)
            {
                DiagnosticLog.Write("initialization-error", new { type = error.GetType().Name, hresult = error.HResult });
                status.Text = "初始化失败，可退出：" + error.Message;
                if (controller != null) { try { controller.Close(); } catch (Exception) { } controller = null; }
                host.Hide();
                if (options.SelfTest || options.Duration > 0) { Environment.ExitCode = 1; RequestQuit(); }
                else if (!exiting) console.Show();
            }
        }

        private async void SetMode(RunMode mode)
        {
            wantedMode = mode;
            if (mode == RunMode.Stopped)
            {
                modeRevision++;
                pollTimer.Stop();
                host.Hide();
                if (controller != null) { try { controller.IsVisible = false; } catch (Exception) { } }
            }
            if (exiting || applying) return;
            applying = true;
            try
            {
                while (!exiting)
                {
                    RunMode target = wantedMode;
                    modeRevision++;
                    if (target != RunMode.Stopped)
                    {
                        await EnsureInitializedAsync();
                        if (!ready) { wantedMode = RunMode.Stopped; currentMode = RunMode.Stopped; tray.Text = "Spark Refresh Lab · 已停止"; return; }
                    }
                    if (exiting) return;
                    if (target != wantedMode) continue;
                    pollTimer.Stop();
                    host.Hide();
                    if (controller != null && ready)
                    {
                        controller.IsVisible = false;
                        await ExecuteScriptBoundedAsync("window.spark.stop()", 1000);
                        if (exiting) return;
                        if (target != wantedMode) continue;
                        if (target == RunMode.Visible) { host.PlaceInCorner(); host.Show(); controller.IsVisible = true; }
                        else if (target == RunMode.Hidden) { host.Hide(); controller.IsVisible = true; }
                        else { host.Hide(); controller.IsVisible = false; }
                        if (target != RunMode.Stopped)
                            await ExecuteScriptBoundedAsync("window.spark.start(" + (options.SelfTest ? "true" : "false") + ")", 1000);
                        if (exiting) return;
                    }
                    else host.Hide();
                    currentMode = target;
                    tray.Text = "Spark Refresh Lab · " + target;
                    status.Text = "模式：" + target + "；回调数仅作诊断，不代表实际呈现。";
                    lastCount = 0;
                    lastPollTime = 0;
                    DiagnosticLog.Write("mode", new { mode = target.ToString(), controllerIsVisible = controller != null && controller.IsVisible,
                        hostIsWindowVisible = IsWindowVisible(host.Handle), selfTestNoPaint = options.SelfTest });
                    if (target != RunMode.Stopped) { console.Hide(); pollTimer.Start(); }
                    await PollAsync();
                    if (target == wantedMode) break;
                }
            }
            catch (Exception error)
            {
                DiagnosticLog.Write("mode-error", new { type = error.GetType().Name, hresult = error.HResult });
                status.Text = "模式切换失败，可退出：" + error.Message;
                host.Hide();
                pollTimer.Stop();
                if (controller != null) { try { controller.IsVisible = false; } catch (Exception) { } }
                if (options.SelfTest || options.Duration > 0) { Environment.ExitCode = 1; RequestQuit(); }
                else if (!exiting) console.Show();
            }
            finally { applying = false; }
        }

        private async Task PollAsync()
        {
            if (exiting || polling || !ready || controller == null) return;
            polling = true;
            int revision = modeRevision;
            try
            {
                string json = await ExecuteScriptBoundedAsync("window.spark.snapshot()", 1000);
                if (exiting || controller == null || revision != modeRevision) return;
                Dictionary<string, object> page = serializer.Deserialize<Dictionary<string, object>>(json);
                double count = Convert.ToDouble(page["callbackCount"]);
                double now = lifetime.Elapsed.TotalSeconds;
                double? callbackHz = lastPollTime > 0 ? (double?)((count - lastCount) / (now - lastPollTime)) : null;
                object data = new { mode = currentMode.ToString(), controllerIsVisible = controller.IsVisible,
                    hostIsWindowVisible = IsWindowVisible(host.Handle), callbackHz = callbackHz, page = page,
                    measurement = "JavaScript rAF callbacks only; not GPU presents, DWM frames or panel refreshes." };
                DiagnosticLog.Write("sample", data);
                details.Text = serializer.Serialize(data) + "\r\n\r\n日志：" + DiagnosticLog.FilePath;
                lastCount = count;
                lastPollTime = now;
            }
            catch (Exception error) { if (!exiting) DiagnosticLog.Write("sample-error", new { type = error.GetType().Name, hresult = error.HResult }); }
            finally { polling = false; }
        }

        private async Task<string> ExecuteScriptBoundedAsync(string script, int timeoutMilliseconds)
        {
            Task<string> operation = controller.CoreWebView2.ExecuteScriptAsync(script);
            if (await Task.WhenAny(operation, Task.Delay(timeoutMilliseconds)) != operation)
                throw new TimeoutException("The renderer did not acknowledge a script within the bounded interval.");
            return await operation;
        }

        private async void DeadlineTick(object sender, EventArgs e)
        {
            if (exiting) return;
            if (options.SelfTest && selfTestClock.IsRunning)
            {
                double seconds = selfTestClock.Elapsed.TotalSeconds;
                if (selfTestStage == 0 && seconds >= 4) { selfTestStage = 1; SetMode(RunMode.Visible); }
                else if (selfTestStage == 1 && seconds >= 8) { selfTestStage = 2; SetMode(RunMode.Stopped); }
                else if (selfTestStage == 2 && seconds >= 9) { await QuitAsync(); return; }
            }
            if (options.Duration > 0 && lifetime.Elapsed.TotalSeconds >= options.Duration)
            {
                if (options.SelfTest && !ready) Environment.ExitCode = 1;
                DiagnosticLog.Write("deadline", new { seconds = options.Duration });
                await QuitAsync();
            }
        }

        private async void RequestQuit() { await QuitAsync(); }

        private async Task QuitAsync()
        {
            if (exiting) return;
            exiting = true;
            deadlineTimer.Stop();
            pollTimer.Stop();
            host.Hide();
            console.Hide();
            bool initializationFinished = initializeTask == null || initializeTask.IsCompleted;
            if (!initializationFinished)
            {
                await Task.WhenAny(initializeTask, Task.Delay(5000));
                initializationFinished = initializeTask.IsCompleted;
                if (!initializationFinished) { Environment.ExitCode = 1; DiagnosticLog.Write("cleanup-initialization-timeout", new { cleanupConfirmed = false }); }
            }
            if (controller != null)
            {
                try
                {
                    Task stop = controller.CoreWebView2.ExecuteScriptAsync("window.spark && window.spark.stop()");
                    await Task.WhenAny(stop, Task.Delay(500));
                }
                catch (Exception) { }
                finally { try { controller.Close(); } catch (Exception) { Environment.ExitCode = 1; } controller = null; }
            }
            bool observedExit = browserPid == 0 && initializationFinished;
            if (browserPid != 0)
            {
                await Task.WhenAny(browserExited.Task, Task.Delay(3000));
                observedExit = browserExited.Task.IsCompleted;
            }
            if (!observedExit) Environment.ExitCode = 1;
            DiagnosticLog.Write("shutdown", new { browserPid = browserPid, browserExitObserved = observedExit, initializationFinished = initializationFinished, exitCode = Environment.ExitCode });
            tray.Visible = false;
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                exiting = true;
                pollTimer.Dispose();
                deadlineTimer.Dispose();
                if (controller != null) { try { controller.Close(); } catch (Exception) { } controller = null; }
                if (tray.Icon != null) tray.Icon.Dispose();
                if (tray.ContextMenuStrip != null) tray.ContextMenuStrip.Dispose();
                tray.Dispose();
                host.Dispose();
                console.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
