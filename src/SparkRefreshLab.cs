using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

[assembly: AssemblyTitle(SparkRefreshLab.UiText.AppTitle)]
[assembly: AssemblyDescription(SparkRefreshLab.UiText.AppDescription)]
[assembly: AssemblyProduct(SparkRefreshLab.UiText.AppTitle)]
[assembly: AssemblyVersion("0.3.0.0")]

namespace SparkRefreshLab
{
    internal enum RunMode { Stopped, Hidden, Visible, Ball, Canvas, Gdi, Wpf, Edge }

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
                        throw new ArgumentException(UiText.DurationError);
                }
                else if (arg == "--mode" && ++i < args.Length)
                {
                    if (!Enum.TryParse<RunMode>(args[i], true, out result.Mode) || !Enum.IsDefined(typeof(RunMode), result.Mode))
                        throw new ArgumentException(UiText.ModeError);
                }
                else throw new ArgumentException(UiText.OptionError + arg);
            }
            if (result.SelfTest) result.Duration = 25;
            if (autoExitRequested && result.Duration == 0)
                throw new ArgumentException(UiText.AutoExitError);
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
            catch (Exception error) { MessageBox.Show(error.Message, UiText.AppTitle); return 2; }
            string showControlsEventName;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                showControlsEventName = @"Local\SparkRefreshLab.ShowControls." + identity.User.Value + "." + UiText.Locale;
            bool created;
            using (Mutex mutex = new Mutex(true, @"Local\SparkRefreshLab-0.1", out created))
            {
                if (!created)
                {
                    try
                    {
                        using (EventWaitHandle existing = EventWaitHandle.OpenExisting(showControlsEventName))
                            if (!existing.Set()) throw new InvalidOperationException("Unable to signal the existing window.");
                        return 0;
                    }
                    catch (Exception error)
                    {
                        DiagnosticLog.Write("show-controls-signal-error", new { type = error.GetType().Name, hresult = error.HResult });
                        if (!options.SelfTest && options.Duration == 0)
                            MessageBox.Show(UiText.SignalFailed, UiText.AppTitle);
                        return 3;
                    }
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                EventWaitHandle showControlsSignal = null;
                try
                {
                    showControlsSignal = new EventWaitHandle(false, EventResetMode.AutoReset, showControlsEventName);
                    using (LabContext context = new LabContext(options, showControlsSignal))
                    {
                        showControlsSignal = null; // LabContext now owns the handle and registered wait.
                        Application.Run(context);
                    }
                }
                catch (Exception error)
                {
                    DiagnosticLog.Write("fatal", new { type = error.GetType().Name, hresult = error.HResult });
                    if (!options.SelfTest && options.Duration == 0)
                        MessageBox.Show(UiText.Error(UiText.StartupFailed, error), UiText.AppTitle);
                    return 1;
                }
                finally
                {
                    if (showControlsSignal != null) showControlsSignal.Dispose();
                    mutex.ReleaseMutex();
                }
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
        private readonly Button stopButton = new Button();
        private bool compactPresentation;
        internal event EventHandler StopRequested;

        internal RenderHost()
        {
            Text = UiText.AppTitle;
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(220, 48);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = false;
            BackColor = Color.FromArgb(32, 32, 32);
            StartPosition = FormStartPosition.Manual;
            stopButton.Text = UiText.Exit;
            stopButton.Font = new Font(UiText.FontName, 9F);
            stopButton.BackColor = Color.FromArgb(32, 32, 32);
            stopButton.ForeColor = Color.FromArgb(192, 192, 192);
            stopButton.FlatStyle = FlatStyle.Flat;
            stopButton.FlatAppearance.BorderColor = Color.FromArgb(96, 96, 96);
            stopButton.UseVisualStyleBackColor = false;
            stopButton.Visible = false;
            stopButton.Click += delegate { EventHandler handler = StopRequested; if (handler != null) handler(this, EventArgs.Empty); };
            Controls.Add(stopButton);
        }

        internal Rectangle RenderBounds
        {
            get
            {
                int reserved = compactPresentation ? (int)Math.Round(50 * DeviceDpi / 96F) : 0;
                return new Rectangle(0, 0, Math.Max(0, ClientSize.Width - reserved), ClientSize.Height);
            }
        }

        internal bool StopButtonVisible { get { return stopButton.Visible; } }
        internal void PlaceInCorner()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 16, area.Bottom - Height - 16);
        }
        internal void ConfigureMode(RunMode mode)
        {
            bool compact = mode == RunMode.Ball || mode == RunMode.Canvas || mode == RunMode.Gdi || mode == RunMode.Wpf || mode == RunMode.Edge;
            compactPresentation = compact;
            float scale = DeviceDpi / 96F;
            ClientSize = new Size((int)Math.Round((compact ? 302 : 220) * scale), (int)Math.Round((compact ? 22 : 48) * scale));
            Rectangle render = RenderBounds;
            stopButton.Bounds = new Rectangle(render.Right, 0, ClientSize.Width - render.Width, ClientSize.Height);
            stopButton.Visible = compact;
            stopButton.BringToFront();
            TopMost = compact;
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
        private readonly Stopwatch modeClock = new Stopwatch();
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private readonly TaskCompletionSource<bool> browserExited = new TaskCompletionSource<bool>();
        private CoreWebView2Environment environment;
        private CoreWebView2Controller controller;
        private IRefreshSurface nativeSurface;
        private EventWaitHandle showControlsSignal;
        private RegisteredWaitHandle showControlsWait;
        private Task initializeTask;
        private TaskCompletionSource<bool> navigationReady;
        private RunMode wantedMode = RunMode.Stopped;
        private RunMode currentMode = RunMode.Stopped;
        private volatile bool exiting;
        private bool applying, polling, ready;
        private int selfTestStage = -1;
        private int modeRevision;
        private double lastCount, lastPollTime;
        private uint browserPid;
        private bool uiTrial;
        private bool consoleRequested;

        internal LabContext(Options settings, EventWaitHandle controlsSignal)
        {
            options = settings;
            showControlsSignal = controlsSignal;
            SetupConsole();
            SetupTray();
            host.Resize += delegate
            {
                if (controller != null && !exiting) controller.Bounds = host.RenderBounds;
                if (nativeSurface != null) nativeSurface.View.Bounds = host.RenderBounds;
            };
            host.StopRequested += async delegate { await QuitAsync(); };
            host.FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!exiting) { e.Cancel = true; SetMode(RunMode.Stopped); } };
            // Force a real HWND without showing it. Hidden mode never uses transparency or off-screen placement.
            IntPtr hostHandle = host.Handle;
            IntPtr consoleHandle = console.Handle;
            showControlsWait = ThreadPool.RegisterWaitForSingleObject(showControlsSignal, OnShowControlsRequested,
                null, Timeout.Infinite, false);
            pollTimer.Interval = 1000;
            pollTimer.Tick += async delegate { await PollAsync(); };
            deadlineTimer.Interval = 200;
            deadlineTimer.Tick += DeadlineTick;
            if (options.SelfTest || options.Duration > 0) deadlineTimer.Start();
            ushort processMachine, nativeMachine;
            bool architectureRead = IsWow64Process2(Process.GetCurrentProcess().Handle, out processMachine, out nativeMachine);
            if (!options.SelfTest && options.Mode == RunMode.Stopped && options.Duration > 0) modeClock.Start();
            DiagnosticLog.Write("startup", new { version = "local0.3.0", pid = Process.GetCurrentProcess().Id,
                processArchitecture = Environment.Is64BitProcess ? "64-bit" : "32-bit",
                processEnvironmentArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
                processMachine = architectureRead ? "0x" + processMachine.ToString("X4") : "unknown",
                nativeMachine = architectureRead ? "0x" + nativeMachine.ToString("X4") : "unknown",
                mode = options.Mode.ToString(), selfTest = options.SelfTest, language = UiText.Locale,
                experimentalBackgroundTimers = options.ExperimentalBackgroundTimers,
                evidenceLimit = "rAF callback counts are not presentation, scanout or panel refresh measurements." });
            console.BeginInvoke(new Action(async delegate
            {
                if (options.SelfTest)
                {
                    await EnsureInitializedAsync();
                    if (ready && !exiting) { selfTestClock.Start(); selfTestStage = 0; SetMode(RunMode.Hidden); }
                }
                else if (options.Mode != RunMode.Stopped) SetMode(options.Mode);
                else ShowConsole();
            }));
        }

        private void OnShowControlsRequested(object state, bool timedOut)
        {
            if (timedOut || exiting || console.IsDisposed || !console.IsHandleCreated) return;
            try
            {
                console.BeginInvoke(new Action(delegate
                {
                    if (!exiting && !console.IsDisposed) ShowConsole();
                }));
            }
            catch (InvalidOperationException) { } // The window can close after the guards above.
        }

        private void StopControlsListener()
        {
            if (showControlsWait != null) { showControlsWait.Unregister(null); showControlsWait = null; }
            if (showControlsSignal != null) { showControlsSignal.Dispose(); showControlsSignal = null; }
        }

        private void SetupConsole()
        {
            console.Text = UiText.AppTitle + " · v0.3.0";
            console.ClientSize = new Size(600, 355);
            console.StartPosition = FormStartPosition.CenterScreen;
            console.Font = new Font(UiText.FontName, 9F);
            Label intro = new Label();
            intro.Text = UiText.Intro;
            intro.Bounds = new Rectangle(12, 10, 576, 44);
            console.Controls.Add(intro);
            AddButton(UiText.BallButton, 12, delegate { StartContinuous(RunMode.Ball); }, 180);
            AddButton(UiText.CanvasButton, 202, delegate { StartContinuous(RunMode.Canvas); }, 180);
            AddButton(UiText.Stop, 392, delegate { SetMode(RunMode.Stopped); });
            AddButton(UiText.Exit, 490, async delegate { await QuitAsync(); });
            status.Text = UiText.InitialStatus;
            status.Bounds = new Rectangle(12, 99, 576, 35);
            console.Controls.Add(status);
            details.Multiline = true;
            details.ReadOnly = true;
            details.ScrollBars = ScrollBars.Vertical;
            details.Bounds = new Rectangle(12, 137, 576, 180);
            details.Text = UiText.InitialDetails + "\r\n\r\n" + UiText.LogLabel + DiagnosticLog.FilePath;
            console.Controls.Add(details);
            Label footer = new Label();
            footer.Text = UiText.Footer;
            footer.Bounds = new Rectangle(12, 325, 576, 23);
            console.Controls.Add(footer);
            console.FormClosing += delegate(object sender, FormClosingEventArgs e) { if (!exiting) { e.Cancel = true; RequestQuit(); } };
        }

        private void ShowConsole()
        {
            if (exiting || console.IsDisposed) return;
            consoleRequested = true;
            console.Show();
            // STARTUPINFO/SW_HIDE can consume the first ShowWindow call. Retry only
            // after the message loop runs, and never override an intentional Hide.
            console.BeginInvoke(new Action(delegate
            {
                if (exiting || console.IsDisposed || !consoleRequested) return;
                if (!IsWindowVisible(console.Handle)) { console.Hide(); console.Show(); }
                console.Activate();
            }));
        }

        private void HideConsole()
        {
            consoleRequested = false;
            console.Hide();
        }

        private void AddButton(string text, int left, EventHandler action, int width = 88)
        {
            Button button = new Button();
            button.Text = text;
            button.Bounds = new Rectangle(left, 60, width, 30);
            button.Click += action;
            console.Controls.Add(button);
        }

        private void SetupTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(UiText.Stop, null, delegate { SetMode(RunMode.Stopped); });
            menu.Items.Add(UiText.BallButton, null, delegate { StartContinuous(RunMode.Ball); });
            menu.Items.Add(UiText.CanvasButton, null, delegate { StartContinuous(RunMode.Canvas); });
            menu.Items.Add(UiText.Controls, null, delegate { ShowConsole(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(UiText.Exit, null, async delegate { await QuitAsync(); });
            tray.Icon = (Icon)SystemIcons.Application.Clone();
            tray.Text = UiText.AppTitle + " · " + UiText.Stopped;
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowConsole(); };
            tray.Visible = true;
            ContextMenuStrip renderMenu = new ContextMenuStrip();
            renderMenu.Items.Add(UiText.StopAndReturn, null, delegate { SetMode(RunMode.Stopped); });
            renderMenu.Items.Add(UiText.Exit, null, async delegate { await QuitAsync(); });
            host.ContextMenuStrip = renderMenu;
        }

        private void StartContinuous(RunMode mode)
        {
            uiTrial = false;
            modeClock.Reset();
            if (options.Duration == 0 && !options.SelfTest) deadlineTimer.Stop();
            status.Text = ModeLabel(mode) + (options.Duration > 0 ? UiText.PreparingTimed : UiText.PreparingContinuous);
            SetMode(mode);
        }

        private void StartTrial(RunMode mode)
        {
            uiTrial = true;
            modeClock.Reset();
            deadlineTimer.Start();
            status.Text = ModeLabel(mode) + UiText.PreparingTrial;
            SetMode(mode);
        }

        private static string ModeLabel(RunMode mode)
        {
            if (mode == RunMode.Ball || mode == RunMode.Edge) return UiText.Ball;
            if (mode == RunMode.Canvas) return UiText.Canvas;
            if (mode == RunMode.Gdi) return UiText.T("图形接口诊断", "GDI diagnostic");
            if (mode == RunMode.Wpf) return UiText.T("窗口绘制诊断", "WPF diagnostic");
            if (mode == RunMode.Hidden) return UiText.T("隐藏诊断", "Hidden diagnostic");
            if (mode == RunMode.Visible) return UiText.T("可见诊断", "Visible diagnostic");
            return UiText.Stopped;
        }

        private static bool IsNative(RunMode mode) { return mode == RunMode.Gdi || mode == RunMode.Wpf; }

        private object HostGeometry()
        {
            return new { x = host.Left, y = host.Top, width = host.Width, height = host.Height,
                clientWidth = host.ClientSize.Width, clientHeight = host.ClientSize.Height, dpi = host.DeviceDpi,
                topMost = host.TopMost, renderBounds = host.RenderBounds.ToString(), stopButtonVisible = host.StopButtonVisible,
                controllerBounds = controller != null ? controller.Bounds.ToString() : null };
        }

        private void StopNative()
        {
            if (nativeSurface != null) nativeSurface.Stop();
        }

        private void ClearNative()
        {
            if (nativeSurface == null) return;
            nativeSurface.Stop();
            DiagnosticLog.Write("native-stopped", new { mechanism = nativeSurface.Mechanism, running = nativeSurface.IsRunning,
                callbacks = nativeSurface.CallbackCount, draws = nativeSurface.DrawCount, wpfTier = nativeSurface.WpfRenderingTier });
            host.Controls.Remove(nativeSurface.View);
            nativeSurface.Dispose();
            nativeSurface = null;
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
                controller.Bounds = host.RenderBounds;
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
                status.Text = UiText.Ready;
            }
            catch (Exception error)
            {
                DiagnosticLog.Write("initialization-error", new { type = error.GetType().Name, hresult = error.HResult });
                status.Text = UiText.Error(UiText.InitializationFailed, error);
                if (controller != null) { try { controller.Close(); } catch (Exception) { } controller = null; }
                host.Hide();
                if (options.SelfTest || options.Duration > 0) { Environment.ExitCode = 1; RequestQuit(); }
                else if (!exiting) ShowConsole();
            }
        }

        private async void SetMode(RunMode mode)
        {
            wantedMode = mode;
            StopNative();
            host.Hide();
            if (mode == RunMode.Stopped)
            {
                uiTrial = false;
                modeClock.Stop();
                modeRevision++;
                pollTimer.Stop();
                if (controller != null) { try { controller.IsVisible = false; } catch (Exception) { } }
                if (!options.SelfTest && options.Duration == 0)
                {
                    deadlineTimer.Stop();
                    if (!exiting) ShowConsole();
                }
            }
            if (exiting || applying) return;
            applying = true;
            try
            {
                while (!exiting)
                {
                    RunMode target = wantedMode;
                    modeRevision++;
                    if (target != RunMode.Stopped && !IsNative(target))
                    {
                        await EnsureInitializedAsync();
                        if (!ready) { wantedMode = RunMode.Stopped; currentMode = RunMode.Stopped; tray.Text = UiText.AppTitle + " · " + UiText.Stopped; return; }
                    }
                    if (exiting) return;
                    if (target != wantedMode) continue;
                    pollTimer.Stop();
                    host.Hide();
                    ClearNative();
                    host.ConfigureMode(target);
                    if (controller != null && ready)
                    {
                        controller.IsVisible = false;
                        await ExecuteScriptBoundedAsync("window.spark.stop()", 1000);
                        if (exiting) return;
                        if (target != wantedMode) continue;
                        if (target != RunMode.Stopped && !IsNative(target))
                            await ExecuteScriptBoundedAsync("window.spark.setMethod('" + (target == RunMode.Canvas ? "canvas" : "ball") + "')", 1000);
                        if (exiting) return;
                        if (target != wantedMode) continue;
                        if (target == RunMode.Visible || target == RunMode.Ball || target == RunMode.Canvas || target == RunMode.Edge) { host.PlaceInCorner(); host.Show(); controller.IsVisible = true; }
                        else if (target == RunMode.Hidden) { host.Hide(); controller.IsVisible = true; }
                        else { host.Hide(); controller.IsVisible = false; }
                        if (target != RunMode.Stopped && !IsNative(target))
                            await ExecuteScriptBoundedAsync("window.spark.start(" + (options.SelfTest ? "true" : "false") + ")", 1000);
                        if (exiting) return;
                        if (target != wantedMode) continue;
                    }
                    if (IsNative(target))
                    {
                        nativeSurface = target == RunMode.Gdi ? NativeRefreshSurfaces.CreateGdi() : NativeRefreshSurfaces.CreateWpf();
                        nativeSurface.View.Dock = DockStyle.None;
                        nativeSurface.View.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        nativeSurface.View.Bounds = host.RenderBounds;
                        nativeSurface.View.ContextMenuStrip = host.ContextMenuStrip;
                        host.Controls.Add(nativeSurface.View);
                        host.PlaceInCorner();
                        host.Show();
                        nativeSurface.Start();
                    }
                    currentMode = target;
                    tray.Text = UiText.AppTitle + " · " + ModeLabel(target);
                    status.Text = target == RunMode.Stopped ? UiText.InitialStatus : ModeLabel(target) + (uiTrial ? UiText.TrialStatus : UiText.RunningStatus);
                    details.Text = UiText.MethodLabel + ModeLabel(target) + "\r\n" + UiText.Details + "\r\n\r\n" + UiText.LogLabel + DiagnosticLog.FilePath;
                    lastCount = 0;
                    lastPollTime = 0;
                    DiagnosticLog.Write("mode", new { mode = target.ToString(), controllerIsVisible = controller != null && controller.IsVisible,
                        hostIsWindowVisible = IsWindowVisible(host.Handle), geometry = HostGeometry(), selfTestNoPaint = options.SelfTest,
                        nativeMechanism = nativeSurface != null ? nativeSurface.Mechanism : null });
                    if (target != RunMode.Stopped)
                    {
                        modeClock.Restart();
                        HideConsole();
                        pollTimer.Start();
                    }
                    await PollAsync();
                    if (target == wantedMode) break;
                }
            }
            catch (Exception error)
            {
                DiagnosticLog.Write("mode-error", new { type = error.GetType().Name, hresult = error.HResult });
                status.Text = UiText.Error(UiText.SwitchFailed, error);
                host.Hide();
                pollTimer.Stop();
                StopNative();
                if (controller != null) { try { controller.IsVisible = false; } catch (Exception) { } }
                if (options.SelfTest || options.Duration > 0) { Environment.ExitCode = 1; RequestQuit(); }
                else if (!exiting) ShowConsole();
            }
            finally { applying = false; }
        }

        private async Task PollAsync()
        {
            if (exiting || polling) return;
            if (nativeSurface != null)
            {
                DiagnosticLog.Write("sample", new { mode = currentMode.ToString(), controllerIsVisible = controller != null && controller.IsVisible,
                    hostIsWindowVisible = IsWindowVisible(host.Handle), geometry = HostGeometry(),
                    native = new { mechanism = nativeSurface.Mechanism, running = nativeSurface.IsRunning,
                        callbackCount = nativeSurface.CallbackCount, drawCount = nativeSurface.DrawCount, wpfTier = nativeSurface.WpfRenderingTier },
                    measurement = "Native callbacks and OnPaint/OnRender counts only; not GPU presents, DWM frames or panel refreshes." });
                return;
            }
            if (!ready || controller == null) return;
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
                    geometry = HostGeometry(),
                    measurement = "JavaScript rAF callbacks only; not GPU presents, DWM frames or panel refreshes." };
                DiagnosticLog.Write("sample", data);
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
            if (uiTrial && modeClock.IsRunning && modeClock.Elapsed.TotalSeconds >= 60)
            {
                DiagnosticLog.Write("trial-completed", new { mode = currentMode.ToString(), seconds = modeClock.Elapsed.TotalSeconds });
                SetMode(RunMode.Stopped);
                return;
            }
            if (options.SelfTest && selfTestClock.IsRunning)
            {
                double seconds = selfTestClock.Elapsed.TotalSeconds;
                if (selfTestStage == 0 && seconds >= 4) { selfTestStage = 1; SetMode(RunMode.Visible); }
                else if (selfTestStage == 1 && seconds >= 8) { selfTestStage = 2; SetMode(RunMode.Stopped); }
                else if (selfTestStage == 2 && seconds >= 9) { await QuitAsync(); return; }
            }
            bool deadline = options.SelfTest ? lifetime.Elapsed.TotalSeconds >= options.Duration : modeClock.IsRunning && modeClock.Elapsed.TotalSeconds >= options.Duration;
            if (options.Duration > 0 && deadline)
            {
                if (options.SelfTest && !ready) Environment.ExitCode = 1;
                DiagnosticLog.Write("deadline", new { seconds = options.Duration });
                await QuitAsync();
            }
            else if (options.Duration > 0 && !modeClock.IsRunning && lifetime.Elapsed.TotalSeconds >= 20)
            {
                Environment.ExitCode = 1;
                DiagnosticLog.Write("mode-start-timeout", new { seconds = 20 });
                await QuitAsync();
            }
        }

        private async void RequestQuit() { await QuitAsync(); }

        private async Task QuitAsync()
        {
            if (exiting) return;
            exiting = true;
            StopControlsListener();
            deadlineTimer.Stop();
            pollTimer.Stop();
            StopNative();
            ClearNative();
            host.Hide();
            HideConsole();
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
                StopControlsListener();
                pollTimer.Dispose();
                deadlineTimer.Dispose();
                ClearNative();
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
