# Visibility experiment notes

Version 0.3 offers the visible Ball and Canvas methods for continuous use. The affected user reported relief from both in the [visible-method comparison](results-2026-10-23-local-multimethod.md). The two language packages keep the same drawing content and geometry; repeated same-language launches show the existing controller instead of starting another animation. The following hidden-mode notes describe the original diagnostic experiment, which remains outside the ordinary controls.

The hidden mode intentionally hides its real top-level WinForms HWND. It leaves the WebView2 controller's requested visibility flag enabled as an experiment and logs the actual HWND visibility separately. The control flag, HWND visibility and document visibility must not be conflated. No transparent visible window or off-screen presentation is substituted for the hidden mode.

WebView2 may still stop rendering or throttle callbacks based on host visibility and occlusion. A callback counter is diagnostic information about JavaScript execution, not a measurement of swap-chain presentation, DWM dirty regions, physical panel scanout, or flicker. A hidden mode that does not help is a valid experimental outcome.

The normal hidden/visible run polls a local JavaScript snapshot once per second. That polling, browser-process lifetime, and any GPU activity are part of the intervention; the experiment does not isolate rAF alone. A fully exited baseline is distinct from Stopped, which cancels the local animation and sampling but can retain the WebView2 process.

The finite native self-test uses a no-paint page mode so it can validate host visibility, callback diagnostics and shutdown without repeatedly moving visible pixels. Passing it does not establish efficacy or validate the physical display. The first normal hidden-mode trial subsequently ran with noPaint=false; the user still reported flicker despite verified active callbacks. Later visible Ball and Canvas user observations are documented separately in the result linked above.

Optional `--experimental-background-timers` adds only `--disable-background-timer-throttling` to this app's WebView2 environment. It does not change the user's ordinary Edge/Chrome profile or global browser configuration, and it does not guarantee background rAF or rendering. Runtime flags are diagnostic and may change between runtime releases.

The native host is built with a .NET Framework compiler and a matching architecture-specific WebView2 loader. On the affected machine, the first AnyCPU build selected an AMD64 CLR and could not load the ARM64 loader. That failed initialization was not a successful ARM64 test. The host build and runtime architecture must be reported explicitly in validation notes.

The application's embedded document has a restrictive CSP, no external resources, and navigation/resource restrictions. Installed WebView2 runtime background services remain subject to Microsoft's own implementation; this is not an OS-wide network-isolation tool.

Relevant official documentation:

- https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2controller.isvisible
- https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/webview-features-flags
- https://chromium.googlesource.com/chromium/src/+/main/docs/windows_native_window_occlusion_tracking.md
- https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-status
- https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmflush
