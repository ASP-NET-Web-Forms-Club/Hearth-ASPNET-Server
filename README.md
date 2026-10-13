# Hearth Portable ASP.NET Web Server

[https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server](https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server)

A lightweight, high performance, portable web server designed for ASP.NET applications.
A self-contained, IIS-free host for **ASP.NET Web Forms** applications, built on
**C# 7.3 / .NET Framework 4.8**. It pairs a WinForms launcher and a powerful multi-site manager
supporting **ASP.NET, .NET Core, Python, Node.js, and PHP** side-by-side with a console /
Windows-Service worker process.

**Screenshot - The Site Manager - Handling Multiple Sites**<br>
![Screenshot - The Site Manager - Handling Multiple Sites](https://raw.githubusercontent.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server/refs/heads/main/wiki/screenshot-manager.png)

**Screenshot - Single Site Launcher**<br>
![Screenshot Hearth ASP.NET Server](https://raw.githubusercontent.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server/refs/heads/main/wiki/screenshot.png)

## In What Circumstances is Hearth Portable ASP.NET Web Server Useful Compared to IIS?

While Windows (Pro/Enterprise/Server) includes IIS, IIS has distinct characteristics that make it unsuitable or cumbersome in specific scenarios:

| Dimension | Full Windows IIS | Hearth Portable ASP.NET Server |
| :--- | :--- | :--- |
| **Portability & Footprint** | System component requiring OS feature activation (`dism` / Windows Features). Often locked down or absent in Windows Home editions, embedded Windows, or restricted corporate machines. | **Zero install / fully portable**. Run from a USB stick, a local folder, or bundled with an installer. |
| **Administrative Rights** | Requires Administrator privileges to configure sites, change bindings, edit `applicationHost.config`, and register app pools. | **Runs under standard user rights** (via loopback `http://localhost:<port>/` or `127.0.0.1:<port>/`), requiring no admin elevation unless installing as a Windows service or binding `http://+:port/`. |
| **Isolation & Clean Teardown** | Global system-wide service; configuration drifts inside `applicationHost.config`; file permission issues with `IIS_IUSRS`. | Complete directory and process encapsulation. Stop or delete the folder, and zero residue remains on the host machine. |
| **App-in-a-Box / Self-Contained Desktop/Edge Appliance** | Difficult to ship a pre-packaged desktop app that uses ASP.NET Web Forms as the UI / local server (e.g. POS systems, medical devices, internal network kiosk appliances). | **Ideal for embedded web-based desktop tools**. You can bundle `Host.exe` + Web Forms + SQLite/LocalDB and run it seamlessly like an Electron or desktop app backend. |
| **Raw Pipeline Throughput** | Heavier pipeline (HTTP modules, request filtering, Windows Auth modules, ETW logging, WAS). | **16% faster raw throughput** than single-worker IIS in dynamic workloads (14,980 vs 12,881 req/s) because it goes straight from `HttpListener` into `HttpRuntime.ProcessRequest` with minimal middleware overhead. |
| **Rapid Dev / Multi-tenant Sandboxing** | Managing 10 different sites in IIS requires managing 10 app pools and site records in global IIS config. | Hearth Manager (`manager_sites.json`) lets you launch, stop, monitor, or watchdog multiple separate Web Forms instances dynamically by port. |

## Solution layout

| Project | Output | Role |
|---------|--------|------|
| `HearthPortableWebServer.Common` | `.dll` | Shared IPC names, `server.config` reader/writer, ACL'd named sync primitives. |
| `HearthPortableWebServer.Hosting` | `.dll` | The ASP.NET runtime: `HttpListener` + custom `HttpWorkerRequest` feeding `HttpRuntime.ProcessRequest`. Loaded into the isolated worker AppDomain. |
| `HearthPortableWebServer.Host` | `.exe` | Console / Windows-Service worker process. Owns the worker AppDomain lifecycle. |
| `HearthPortableWebServer.Launcher` | `.exe` | WinForms UI: configure, start/stop, browse, install/uninstall service (single-site mode). |
| `HearthPortableWebServer.Manager` | `.exe` | WinForms & Windows Service multi-site manager: orchestrate multiple isolated web applications, manage ports/roots, system tray, auto-start on boot, and debounced window persistence. |
| `HearthPortableWebServer.StressTest` | `.exe` | Load generator: concurrency sweep, throughput + latency percentiles, writes a timestamped log report. |

All projects build to a shared `build\<Configuration>\` folder.

## Architecture (IIS-equivalent worker processes)

### Single Worker Process (Host)
```
 Launcher.exe (WinForms)              Host.exe (separate process)
 ────────────────────────            ─────────────────────────────
  port / root config                  default AppDomain
  Start ─► launch detached ─────────► ServerController
  Stop  ─► signal named event ──────►   │  ApplicationHost.CreateApplicationHost
  status ◄─ named mutex                 ▼
                                      ASP.NET worker AppDomain  ◄── "w3wp.exe" equivalent
                                        AspNetHost  (MarshalByRefObject)
                                        HttpListener  (32 concurrent accepts)
                                        ListenerHttpWorkerRequest ─► HttpRuntime.ProcessRequest
```

* **Single worker process** — one dedicated ASP.NET `AppDomain` created via
  `ApplicationHost.CreateApplicationHost`, exactly how IIS isolates an app pool.
* **Concurrency** — an async `HttpListener` accept loop keeps 32 outstanding
  accepts and dispatches each request on the thread pool; Server GC is enabled.
* **UI independence** — the Host is a separate, detached process. Closing the
  launcher leaves the server running; it is stopped only by an explicit signal,
  Ctrl+C, or the Service Control Manager.

### Multi-Site Manager Architecture
```
                 HearthPortableWebServer.Manager.exe
               (WinForms Dashboard or Session 0 Service)
                                │
        ┌───────────────────────┼───────────────────────┬───────────────────────┐
        ▼                       ▼                       ▼                       ▼
  Host.exe (Port 8080)    python.exe (Port 8081)   node.exe (Port 8082)   app.exe (Port 8083)
   AppDomain: Site #1        Python: Site #2         Node: Site #3        .NET Core: Site #4
  (wwwroot / ASP.NET)       (app.py / Flask/etc.)   (server.js / Express) (Kestrel / isolated)
```

* **Multi-Engine Orchestration** — Spawns and manages dedicated, independent worker processes for **ASP.NET (Web Forms / .NET 4.8)**, **.NET Core**, **Python**, **Node.js**, **PHP**, and custom executables side-by-side without interference or cross-talk.
* **Auto Port & Env Binding** — Automatically passes assigned ports via standard environment variables (`PORT`, `HTTP_PLATFORM_PORT`, `ASPNETCORE_URLS`, `ASPNETCORE_HTTP_PORTS`) so child runtimes bind seamlessly without manual config changes.
* **Smart Runtime Detection** — Auto-detects local virtual environments (such as `.venv\Scripts\python.exe` or `venv\Scripts\python.exe`), supports custom executable paths, and expands Windows environment variables (`%VAR%`).
* **Built-in Live Reload (File Watcher)** — Optional live-reload file watcher monitors project files (`.py`, `.js`, `.ts`, `.php`, `.cs`, `.json`, `.config`) with a 500ms debounce, automatically restarting child processes upon save.
* **Safe Stop Lifecycle** — Stopping a running server automatically toggles its `Auto-Start` setting to `No` first, preventing unexpected background service or watchdog revivals, and terminates the full process tree (`taskkill /F /T`) so ports are freed with zero orphan processes.
* **Session 0 Pre-Login Service** — Can run headless at machine boot as `HearthManagerService` before user login, with an active watchdog that auto-starts designated sites and auto-heals unexpected terminations.
* **Permission Inheritance** — Automatically configures directory ACLs for `Authenticated Users` so SQLite files and uploads created by the background service (`LocalSystem`) remain writable by interactive users.
* **Debounced Layout Persistence** — Modern `1200x715` window fits laptop displays (1366x768) natively. Automatically monitors window adjustments and launches a 3-second countdown timer, persisting window size and coordinates to `window_layout.txt`.

## Usage

### Multi-Site Manager (GUI & Windows Service)
Run `HearthPortableWebServer.Manager.exe`:
* **Multi-Site Dashboard**: View real-time status (● Running / ○ Stopped / ⚠ Port in use) for all configured applications across distinct engines in the dedicated **Type** column. "Port in use" means another application (e.g. IIS) holds the site's port; Hearth never treats or stops such a process as one of its own sites.
* **Multi-Engine Selection**: Choose between **ASP.NET**, **.NET Core**, **Python**, **Node.js**, **PHP**, or **Other**.
* **Smart Entry Defaults**: Auto-suggests entry scripts (e.g. `app.py`, `server.js`, `index.php`), while allowing optional custom executables or arguments.
* **Per-Site Controls**: Dedicated **Start Server**, **Stop Server**, **Browse in Browser**, and **Open Web Root in Explorer** buttons for each application entry.
* **Live Reload Checkbox**: Toggle `Live Reload: Auto-restart on file change` for rapid iterative development.
* **Temporary Stop**: **Stop Server** / **Stop All** stop sites temporarily — the watchdog service will not revive them, but their `Auto-Start` setting is kept and applies again on the next boot / service launch. **Start Server** / **Start All** lift the temporary stop.
* **Port Conflict Detection**: **Add Website** picks a free port automatically; saving a site on a port already used by another application shows a warning.
* **Global Controls**: **Start All Sites** and **Stop All Sites** for bulk operations.
* **Add & Configure Websites**: Specify custom Site Name, Engine Type, dedicated HTTP Port, and relative or absolute application folder path (`manager_sites.json`). Auto-migrates existing `Settings.txt` or `server.config` on first launch.
* **System Tray Minimization**: Closing or minimizing with running servers gives the option to minimize to the notification area with tray menu controls.
* **Debounced Window Layout Saving**: Automatically detects resize or reposition events and saves window location and size to `window_layout.txt` after 3 seconds of inactivity.
* **Windows Service Setup (Session 0 Boot)**: Click **⚙ Windows Service (Boot)** to install `HearthManagerService`. Runs headless on machine boot before user login, auto-starts designated websites, and automatically restores them if a process terminates unexpectedly.

```
HearthPortableWebServer.Manager.exe              (launches interactive WinForms dashboard)
HearthPortableWebServer.Manager.exe --service    (runs as background Windows Service via SCM)
```

### Launcher (Single-Site GUI)
A quick, one-folder / one-port tool for **ASP.NET** sites only. For several sites or
for other engines (.NET Core, Python, Node.js, PHP), use the **Multi-Site Manager**.

Run `HearthPortableWebServer.Launcher.exe`:
* **Port** (default `8080`) and **Web root** (default `<startup path>\wwwroot`).
* **Start / Stop Web Server**, **Browse Web App**.
* **Install / Uninstall Service** (auto-start with Windows; prompts for UAC).
* **Port conflict detection**: if another application (e.g. IIS) already uses the
  port, **Start** reports it and the status bar shows
  `Stopped - port N is in use by another application`.
* **Reliable stop**: **Stop** sends a graceful shutdown signal; if the host does not
  exit within 3 seconds, the Hearth host for that port is force-closed. Other
  applications on the port are never touched.

Closing the launcher while its server is running prompts: **[Yes]** stop the server
and close, **[No]** keep the launcher open. To keep a server running after closing,
install it as a Windows Service.

> **Launcher vs. Manager:** use one or the other for a given port. They do not
> coordinate with each other — e.g. a Manager site with Auto-Start on port 8080 will
> be restarted by the Manager service even if you stop it from the Launcher.

### Host (command line)
```
HearthPortableWebServer.Host.exe --port 8080 --root "C:\site\wwwroot"
HearthPortableWebServer.Host.exe --stop --port 8080
HearthPortableWebServer.Host.exe --install --port 8080 --root "C:\site\wwwroot"   (admin)
HearthPortableWebServer.Host.exe --uninstall                                      (admin)
HearthPortableWebServer.Host.exe --service        (used by the SCM; reads server.config)
```

### Stress test
Run `HearthPortableWebServer.StressTest.exe`. With no `--url` it asks for the target host
**interactively at runtime** (accepts `localhost:8080`, `host:port`, or a full URL);
pass `--url` to script it. It ramps concurrency through several levels, measures
throughput and latency percentiles at each, prints the peak req/sec, and writes a
timestamped report into `stress-logs\`.
```
HearthPortableWebServer.StressTest.exe
HearthPortableWebServer.StressTest.exe --url http://localhost:8080/ --duration 10 --levels 1,2,4,8,16,32,64,128,256
```

## Binding & permissions
The listener tries `http://+:<port>/` (all interfaces, IIS-like) first and falls
back to loopback (`localhost` / `127.0.0.1`) when not elevated. Service install
and the all-interfaces binding require Administrator rights.

## web.config support (partial)
Hearth hosts ASP.NET through `ApplicationHost.CreateApplicationHost` +
`HttpRuntime.ProcessRequest` (the same model as Cassini / the classic Visual
Studio dev server), not through IIS. As a result `web.config` is only
**partially** honoured:

- **Fully supported** — everything the ASP.NET runtime reads itself:
  `<system.web>` (`compilation`, `pages`, `customErrors`, `httpRuntime`
  incl. `maxRequestLength`, `authentication`, `authorization`, `sessionState`,
  `machineKey`, `httpModules`, `httpHandlers`), plus `<connectionStrings>` and
  `<appSettings>`. Changes to `web.config` or `bin\` recycle the site automatically.
- **Partially supported** — `<system.webServer>` is an IIS-only section that
  ASP.NET itself ignores. Hearth reads the following subset from the site's
  **root** `web.config` itself:

  | Element | Supported |
  |---|---|
  | `<defaultDocument>` | `enabled`, `<files>` with `<add>` / `<remove>` / `<clear>`. Site entries are tried first, then the built-in list (`Default.aspx`, `Index.aspx`, `index.html`, `index.htm`, `Default.htm`) unless `<clear/>` is used. Sub-paths such as `office/Default.aspx` work. |
  | `<httpProtocol><customHeaders>` | `<add>` / `<remove>` / `<clear>`; headers are added to every response. |
  | `<staticContent><mimeMap>` | `<mimeMap>` / `<remove>` / `<clear>`; overrides the Content-Type of static files. |
  | `<staticContent><clientCache>` | `cacheControlMode` = `UseMaxAge` (with `cacheControlMaxAge`), `DisableCache`, `UseExpires` (with `httpExpires`), plus `cacheControlCustom`; static files only. |

  "Static files" means existing files whose extension is not handled by
  ASP.NET (`.aspx`, `.ashx`, `.asmx`, `.axd`, `.svc`, `.cshtml`, ... are excluded).
  Editing `web.config` recycles the site, so changes apply automatically.
- **Ignored** — everything else in `<system.webServer>`, including `<rewrite>`,
  integrated-mode `<modules>` / `<handlers>`, `<httpErrors>`,
  `<security><requestFiltering>`, `<httpPlatform>`, `<location>`-scoped
  settings and `web.config` files in sub-folders.

Practical notes:
- Register HTTP handlers/modules under `<system.web>` (classic pipeline), not
  only under `<system.webServer>`.
- Upload size is governed by `<httpRuntime maxRequestLength>` (KB);
  `requestLimits maxAllowedContentLength` has no effect.
- Use `<customErrors>` (in `<system.web>`) for custom error pages instead of `<httpErrors>`.
- Non-ASP.NET engines (PHP, Python, Node.js, .NET Core) do not read
  `web.config` at all. For PHP pretty URLs (e.g. WordPress permalinks), use a
  router script instead of URL Rewrite rules.

## Build
```
msbuild HearthPortableWebServer.sln /p:Configuration=Release
```

## Verified behavior
* ASP.NET Web Forms page renders with ViewState; server-side button postback
  updates a `Label` (full page lifecycle).
* ASP.NET AJAX `[WebMethod]` (page methods) and `.asmx` web services supported (decomposes `PathInfo` sub-paths like `/Page.aspx/MyMethod`).
* `api.aspx` echoes GET query and POST form values.
* Static files served with `ETag` / `Last-Modified` caching headers.
* 200 concurrent requests → 200 × HTTP 200 in ~0.12 s.
* `--stop` signal shuts the worker down gracefully and frees the port.
* Manager orchestrates multiple concurrent sites across distinct engines (ASP.NET, .NET Core, Python, Node.js, PHP) and distinct ports (`8080`, `8081`, etc.) with individual start/stop lifecycle management.
* Live reload automatically detects file updates and restarts child processes without manual intervention.
* Stopping a site safely sets `Auto-Start` to `No` first, preventing watchdog restart loops, and terminates all child process trees cleanly.
* Manager automatically debounces and restores window layout coordinates (`window_layout.txt`) via a 3-second timer.

## Performance & load testing

Read more details report at: [Wiki - Performance Benchmark](https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server/wiki/Performance-Benchmark)

The **same ASP.NET Web Forms application** was load-tested twice on the **same
machine** — once hosted by this portable server, once by the Windows 11 built-in
**IIS** (single worker process) — using `HearthPortableWebServer.StressTest`. Each run
ramped concurrency from 1 → 256 clients, 10 s measured per level (3 s warmup), with
every response body fully read.

**Test machine (both client and server ran here, over loopback):**

| Component | Spec |
|-----------|------|
| CPU | Intel Core i7-4770S @ 3.10 GHz (Turbo 3.90 GHz), 4 cores / 8 logical |
| Cache | L1 256 KB · L2 1.0 MB · L3 8.0 MB |
| RAM | 24 GB DDR3 |
| Storage | Samsung SSD 870 EVO 500 GB |
| OS | Windows 11 |

### Throughput comparison (requests/second)

| Concurrent clients | Portable server | IIS (1 worker) |
|---:|---:|---:|
| 1   | 3,346  | 2,825  |
| 2   | 6,088  | 4,698  |
| 4   | 9,847  | 7,777  |
| 8   | 13,460 | 10,783 |
| 16  | 14,494 | 12,349 |
| 32  | 14,700 | 12,456 |
| 64  | 13,849 | 12,683 |
| **128 (peak)** | **14,980** | **12,881** |
| 256 | 12,696 | 12,208 |

| Summary | Portable server | IIS (1 worker) |
|---|---:|---:|
| Peak throughput | **≈ 14,980 req/s** (≈ 899K req/min) | ≈ 12,881 req/s (≈ 773K req/min) |
| Peak at concurrency | 128 | 128 |
| Failures | **0** | 0 |
| Total requests served | 1,034,924 | 887,020 |

On this dynamic workload the portable server was **~16 % faster at peak** and faster
at every concurrency level, with lower latency — because it runs a far leaner request
pipeline (no native-module chain, no access logging, no process-management overhead),
while sharing the same underlying **HTTP.sys** kernel driver as IIS.

> The full per-level latency breakdown (p50/p90/p95/p99/max) is published on the
> project wiki. This README keeps the summary only.

### What this tells us

Both servers show **identical behavior under load**: throughput saturates around
concurrency 8–16, peaks at 128, eases off at 256, and neither drops a single request.
The latency growth past saturation is textbook Little's Law. In short — **it behaves
like a real server under load.**

While the portable server intentionally does not match IIS's full feature set (kernel
output caching, web gardens, request filtering, TLS/auth modules, logging, health
monitoring, app-pool recycling…), it fully delivers on its core mission: **an efficient
web server that yields the output** — here, matching and slightly exceeding stock
single-worker IIS in raw throughput.

**Caveats.** Client and server shared the same 8 logical CPUs over loopback, so both
ceilings are understated; a second-machine test over the network would raise both. The
~16 % gap reflects stock/default configuration of each (e.g. IIS logging enabled) and a
non-cacheable dynamic page — a cacheable response would let IIS's kernel cache pull far
ahead.

