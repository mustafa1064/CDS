# 🚀 CDS — Central Debug System

> A structured, real-time debug **signal system** for .NET — built for developers and AI-assisted debugging.

---

## 🧭 Why CDS?

Traditional debugging is broken:

* ❌ Noisy output windows
* ❌ Unstructured logs
* ❌ Important signals get buried

👉 CDS turns your app into a **signal-driven system**:

* Emit **structured debug signals**
* Observe them in a **real-time UI**
* Route them to **file or custom sinks**
* Analyze without stopping execution

---

# ⚡ 30-Second Setup (Deterministic)

## 1. Install

```powershell
Install-Package CDS.Core 
Install-Package CDS.Wpf
```

---

## 🚀 Quick Install

```bash
dotnet add package CDS.Core
dotnet add package CDS.Wpf
````

> Always installs the latest version.
> 💡 Tip: Omit `--version` to always install the latest release.
---

## 2. Enable CDS (REQUIRED)

📍 Place inside: `App.xaml.cs → OnStartup`

```csharp
using CDS.Core.Diagnostics;
using CDS.Wpf.Sinks;

private FileDebugSink _fileSink;

protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // ✅ Enable CDS
    DebugConfig.Enabled = true;

    // ✅ Optional but recommended (file logging)
    _fileSink = new FileDebugSink();
    DebugBus.RegisterSink(_fileSink);

    // ✅ Sanity check
    DebugBus.Emit("System", DebugLevel.Info, "CDS Initialized", "App");
}
```

📍 Optional cleanup:

```csharp
protected override void OnExit(ExitEventArgs e)
{
    _fileSink?.Stop();
    base.OnExit(e);
}
```

---

## 3. Add Debug Panel (REQUIRED)

```xml
xmlns:cds="clr-namespace:CDS.Wpf.Views;assembly=CDS.Wpf"

<cds:DebugPanelView />
```

👉 The UI automatically connects to the signal stream.

---

## 4. Emit Signals Anywhere

```csharp
DebugBus.Emit("UI", DebugLevel.Info, "Button clicked");
```

---

# ✅ Expected Result

If setup is correct, you will get:

* 🔴 Live debug stream in UI
* 🔵 Auto-scroll + high-performance rendering
* 🟡 Filtering (level, category, search)
* 🧠 Duplicate grouping (xN)
* ⏸️ Pause → inspect → resume (with buffered replay)
* 📁 File logging (if enabled)

---

# 🧠 Mental Model (Important)

CDS has **two independent pipelines**:

```
1. Signal Pipeline (Core)
   DebugBus.Emit → Sinks (file, etc.)

2. Observation Pipeline (UI)
   DebugPanelView → Collector → Live UI
```

👉 You explicitly wire the system. Nothing is hidden.

---

# 🔧 Debug Configuration

### Default behavior:

* ✅ DEBUG → enabled automatically
* 🔒 RELEASE → disabled unless configured

### Enable in production:

```csharp
DebugConfig.Enabled = true;
```

OR:

```
APP_DEBUG=1
```

---

# 📁 File Logging (Optional but Recommended)

```csharp
var fileSink = new FileDebugSink();
DebugBus.RegisterSink(fileSink);
```

📍 Logs are saved to:

```
%LOCALAPPDATA%\CDS\
```

---

# 🧠 Structured Signals (Advanced)

```csharp
DebugBus.Emit(new DebugEvent
{
    Category = "Network",
    Level = DebugLevel.Warning,
    Message = "Slow response",
    Source = "ApiClient",
    SubCategory = "Latency",
    Data =
    {
        ["DurationMs"] = 1200,
        ["Endpoint"] = "/users"
    }
});
```

---

# ⚠️ Important Notes

### 🔴 CDS disabled in Release

If nothing shows:

```csharp
DebugConfig.Enabled = true;
```

---

### 🔴 WPF Required for UI

`DebugPanelView` requires a WPF app (`Application.Current.Dispatcher`).

---

### 🔴 Pause Behavior (Key Feature)

Pause does NOT stop logging.

* Signals continue flowing
* UI freezes for inspection
* On resume → buffered signals replay in order

👉 Zero data loss.

---

# 🧭 Best Practices

* ✔ Use meaningful categories (`UI`, `Data`, `Network`)
* ✔ Use `Source` for origin tracking
* ✔ Use `Data` for structured insights (AI-friendly)
* ✔ Use `DebugThrottle` for high-frequency logs
* ✔ Think in **signals, not logs**

---

# 🏗️ Architecture

```
Your Code
   ↓
DebugBus.Emit(...)
   ↓
───────────────
Signal Pipeline
───────────────
   ↓
Sinks (File, future extensions)
   ↓
───────────────
Observation Pipeline
───────────────
   ↓
DebugPanelView (UI)
```

---

# 🔥 Core Idea

> CDS is not a logger.
> It is a **real-time signal system for your application**.

---

# 📄 License

MIT