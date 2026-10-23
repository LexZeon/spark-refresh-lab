using System;
using System.Diagnostics;
using System.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using Wpf = System.Windows;
using Media = System.Windows.Media;
using Integration = System.Windows.Forms.Integration;

namespace SparkRefreshLab
{
    // All methods, properties and disposal belong to the creating UI thread.
    // Callback/draw counters describe application work, never DWM or panel presents.
    internal interface IRefreshSurface : IDisposable
    {
        Forms.Control View { get; }
        string Mechanism { get; }
        bool IsRunning { get; }
        long CallbackCount { get; }
        long DrawCount { get; }
        int? WpfRenderingTier { get; }
        void Start();
        void Stop();
    }

    internal static class NativeRefreshSurfaces
    {
        internal static IRefreshSurface CreateGdi() { return new GdiAnimationSurface(); }
        internal static IRefreshSurface CreateWpf() { return new WpfAnimationSurface(); }

        // Same neutral, six-logical-pixel cosine motion as the original browser ball.
        internal static double Position(double milliseconds, double availableTravel)
        {
            return Math.Max(0, Math.Min(246, availableTravel)) *
                (1 - Math.Cos(milliseconds * Math.PI * 2 / 2400)) / 2;
        }
    }

    internal sealed class GdiAnimationSurface : Forms.Control, IRefreshSurface
    {
        private readonly int ownerThread = Thread.CurrentThread.ManagedThreadId;
        private readonly Forms.Timer timer = new Forms.Timer();
        private readonly Stopwatch clock = new Stopwatch();
        private readonly Drawing.SolidBrush backgroundBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(32, 32, 32));
        private readonly Drawing.SolidBrush dotBrush = new Drawing.SolidBrush(Drawing.Color.FromArgb(128, 128, 128));
        private bool running, disposed;
        private long callbacks, draws;
        private double position;

        internal GdiAnimationSurface()
        {
            BackColor = Drawing.Color.FromArgb(32, 32, 32);
            TabStop = false;
            SetStyle(Forms.ControlStyles.UserPaint | Forms.ControlStyles.AllPaintingInWmPaint |
                Forms.ControlStyles.OptimizedDoubleBuffer | Forms.ControlStyles.ResizeRedraw |
                Forms.ControlStyles.Opaque, true);
            // A nominal ~60 Hz request. WinForms timer scheduling and painting may coalesce.
            timer.Interval = 16;
            timer.Tick += OnTick;
        }

        public Forms.Control View { get { return this; } }
        public string Mechanism { get { return "GDI WinForms Timer / OnPaint"; } }
        public bool IsRunning { get { return running; } }
        public long CallbackCount { get { return callbacks; } }
        public long DrawCount { get { return draws; } }
        public int? WpfRenderingTier { get { return null; } }

        public void Start()
        {
            VerifyThread();
            if (disposed) throw new ObjectDisposedException("GdiAnimationSurface");
            if (running) return;
            running = true;
            position = 0;
            clock.Restart();
            timer.Start();
            Invalidate();
        }

        public void Stop()
        {
            VerifyThread();
            if (disposed) return;
            running = false;
            timer.Stop();
            clock.Stop();
        }

        private void OnTick(object sender, EventArgs args)
        {
            if (!running || disposed) return;
            callbacks++;
            double scale = DeviceDpi / 96.0;
            position = NativeRefreshSurfaces.Position(clock.Elapsed.TotalMilliseconds,
                ClientSize.Width / scale - 6);
            Invalidate();
        }

        protected override void OnPaint(Forms.PaintEventArgs args)
        {
            if (disposed) return;
            draws++;
            float scale = DeviceDpi / 96.0f;
            float diameter = 6 * scale;
            args.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
            args.Graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            args.Graphics.FillEllipse(dotBrush, (float)position * scale,
                (ClientSize.Height - diameter) / 2, diameter, diameter);
            base.OnPaint(args);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                VerifyThread();
                Stop();
                disposed = true;
                timer.Tick -= OnTick;
                timer.Dispose();
                backgroundBrush.Dispose();
                dotBrush.Dispose();
            }
            base.Dispose(disposing);
        }

        private void VerifyThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThread)
                throw new InvalidOperationException("Native animation surfaces require their creating UI thread.");
        }
    }

    internal sealed class WpfAnimationSurface : IRefreshSurface
    {
        private readonly int ownerThread = Thread.CurrentThread.ManagedThreadId;
        private readonly Integration.ElementHost host = new Integration.ElementHost();
        private readonly WpfDotElement element = new WpfDotElement();
        private readonly Stopwatch clock = new Stopwatch();
        private readonly int tier;
        private bool running, renderingHooked, disposed;
        private long callbacks;
        private TimeSpan? lastRenderingTime;

        internal WpfAnimationSurface()
        {
            host.BackColor = Drawing.Color.FromArgb(32, 32, 32);
            host.BackColorTransparent = false;
            host.TabStop = false;
            host.Child = element;
            tier = Media.RenderCapability.Tier >> 16;
            element.MouseRightButtonUp += OnRightButtonUp;
            host.Disposed += OnHostDisposed;
        }

        public Forms.Control View { get { return host; } }
        public string Mechanism { get { return "WPF CompositionTarget.Rendering / OnRender"; } }
        public bool IsRunning { get { return running; } }
        public long CallbackCount { get { return callbacks; } }
        public long DrawCount { get { return element.DrawCount; } }
        // Capability metadata only: it does not establish which adapter/path presented a frame.
        public int? WpfRenderingTier { get { return tier; } }

        public void Start()
        {
            VerifyThread();
            if (disposed || host.IsDisposed) throw new ObjectDisposedException("WpfAnimationSurface");
            if (running) return;
            running = true;
            clock.Restart();
            lastRenderingTime = null;
            element.Position = 0;
            if (!renderingHooked)
            {
                Media.CompositionTarget.Rendering += OnRendering;
                renderingHooked = true;
            }
            element.InvalidateVisual();
        }

        public void Stop()
        {
            VerifyThread();
            running = false;
            if (renderingHooked)
            {
                Media.CompositionTarget.Rendering -= OnRendering;
                renderingHooked = false;
            }
            clock.Stop();
            lastRenderingTime = null;
        }

        private void OnRendering(object sender, EventArgs args)
        {
            if (!running || disposed) return;
            Media.RenderingEventArgs rendering = args as Media.RenderingEventArgs;
            if (rendering != null)
            {
                if (lastRenderingTime.HasValue && rendering.RenderingTime == lastRenderingTime.Value) return;
                lastRenderingTime = rendering.RenderingTime;
            }
            callbacks++;
            element.Position = NativeRefreshSurfaces.Position(clock.Elapsed.TotalMilliseconds,
                element.ActualWidth - 6);
            element.InvalidateVisual();
        }

        private void OnRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            if (disposed || host.IsDisposed || host.ContextMenuStrip == null) return;
            host.ContextMenuStrip.Show(host, host.PointToClient(Forms.Cursor.Position));
            args.Handled = true;
        }

        private void OnHostDisposed(object sender, EventArgs args)
        {
            // Even an owning Form disposing its child directly must release the static event.
            Dispose();
        }

        public void Dispose()
        {
            VerifyThread();
            if (disposed) return;
            Stop();
            disposed = true;
            element.MouseRightButtonUp -= OnRightButtonUp;
            host.Disposed -= OnHostDisposed;
            if (!host.IsDisposed)
            {
                host.Child = null;
                host.Dispose();
            }
        }

        private void VerifyThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != ownerThread)
                throw new InvalidOperationException("Native animation surfaces require their creating UI thread.");
        }

        private sealed class WpfDotElement : Wpf.FrameworkElement
        {
            private readonly Media.SolidColorBrush backgroundBrush = new Media.SolidColorBrush(Media.Color.FromRgb(32, 32, 32));
            private readonly Media.SolidColorBrush dotBrush = new Media.SolidColorBrush(Media.Color.FromRgb(128, 128, 128));
            internal double Position;
            internal long DrawCount;

            internal WpfDotElement()
            {
                backgroundBrush.Freeze();
                dotBrush.Freeze();
                Focusable = false;
            }

            protected override void OnRender(Media.DrawingContext context)
            {
                DrawCount++;
                context.DrawRectangle(backgroundBrush, null, new Wpf.Rect(0, 0, ActualWidth, ActualHeight));
                context.DrawEllipse(dotBrush, null, new Wpf.Point(Position + 3, ActualHeight / 2), 3, 3);
                base.OnRender(context);
            }
        }
    }
}
