# Manual A/B result / 手动对照记录

Date / 日期:

Tool version or commit / 版本:

Driver / 驱动:

Windows build:

Brightness / 亮度:

Refresh selector and DRR / 刷新率选择及动态刷新:

G-SYNC and Windows VRR:

AC or battery / 接电或电池:

Other animations stopped / 其他动画已停止:

| Mode / 模式 | Actually visible? / 实际可见 | Typing pulses / 打字闪动 | Comfort / 舒适度 | Callback status (not scanout) / 回调状态 |
|---|---|---|---|---|
| Fully exited / 完全退出 | No tool window or refresh loop | Not tested | Not tested | — |
| Stopped / 停止（可能保留WebView2进程） | No animation | Not tested | Not tested | — |
| Hidden / 真正隐藏 | No animation window | Not tested | Not tested | Not tested |
| Visible / 可见 | Yes | Not tested | Not tested | Not tested |
| Visible, then covered / 可见后遮挡 | Covered | Not tested | Not tested | Not tested |

Wait about 10 seconds after each mode change; type the same short text without screen capture or remote-control activity. Stop a mode immediately if it feels worse. Record the observation, including "no improvement"; do not label a mode successful from a counter alone.

Runtime diagnostic polling is part of the hidden/visible experimental workload. For an independent baseline, exit this program completely and stop other animation tools.
