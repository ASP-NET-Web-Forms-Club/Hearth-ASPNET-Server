---
name: hearth-web-server
description: Control and configure Hearth Portable Web Server (ASP.NET Web Forms, PHP, Python, Node.js, .NET Core) on Windows via CLI, manager_sites.json, and Windows Service state files.
---

# Hearth Portable Web Server Control Guide

Use this skill whenever you need to start, stop, configure, or inspect local websites hosted by **Hearth Portable Web Server**.

> Replace `<HEARTH_DIR>` with the folder containing `HearthPortableWebServer.Manager.exe` and `HearthPortableWebServer.Host.exe` (for example, `D:\aifiles\Hearth-ASP.NET-Portable-Web-Server\github\build\Release`).

---

## 1. Quick Single-Site ASP.NET Control (`Host.exe`)

Use `HearthPortableWebServer.Host.exe` directly to run or stop a standalone classic ASP.NET Web Forms site without modifying `manager_sites.json`.

* **Start a site (background):**
  ```powershell
  Start-Process -FilePath "<HEARTH_DIR>\HearthPortableWebServer.Host.exe" `
    -ArgumentList '--port 8080 --root "D:\path\to\wwwroot"' `
    -WindowStyle Hidden
  ```
* **Stop a running ASP.NET site gracefully:**
  ```powershell
  & "<HEARTH_DIR>\HearthPortableWebServer.Host.exe" --stop --port 8080
  ```
* **Install / Uninstall as a per-port Windows Service (Admin required):**
  ```powershell
  & "<HEARTH_DIR>\HearthPortableWebServer.Host.exe" --install --port 8080 --root "D:\path\to\wwwroot"
  & "<HEARTH_DIR>\HearthPortableWebServer.Host.exe" --uninstall --port 8080
  ```
  Service name format: `HearthPortableWebServer_Port<port>`.

---

## 2. Multi-Site Manager Control (`Manager.exe`)

`HearthPortableWebServer.Manager.exe` manages multiple sites across different runtimes (`ASP.NET`, `PHP`, `Python`, `Node.js`, `.NET Core`, `Other`) using `<HEARTH_DIR>\manager_sites.json`.

* **Start all configured sites:**
  ```powershell
  & "<HEARTH_DIR>\HearthPortableWebServer.Manager.exe" --start-all
  ```
* **Stop all configured sites:**
  ```powershell
  & "<HEARTH_DIR>\HearthPortableWebServer.Manager.exe" --stop-all
  ```

### Managing Sites via `manager_sites.json`

Edit `<HEARTH_DIR>\manager_sites.json` (UTF-8 without BOM) to add, update, or remove sites:

```json
[
  {
    "Id": "228c4c7fa2794b6a8aaefdcfd33771ca",
    "Name": "My ASP.NET Site",
    "Engine": "ASP.NET",
    "Port": 8080,
    "Root": "D:\\wwwroot\\myapp",
    "Script": "",
    "Executable": "",
    "AutoStart": true,
    "AutoRestart": true
  }
]
```

**Field Reference:**
* `Id`: 32-char hex GUID (`[guid]::NewGuid().ToString("N")`).
* `Name`: Display name of the site.
* `Engine`: One of `"ASP.NET"`, `"PHP"`, `"Python"`, `"Node.js"`, `".NET Core"`, or `"Other"`.
* `Port`: TCP port number (must be unique per site).
* `Root`: Absolute path, or path relative to `<HEARTH_DIR>` (e.g., `"wwwroot"`). Do not end with a trailing backslash.
* `Script`: Entry script or arguments:
  * `ASP.NET`: Leave empty `""`.
  * `Python`: Entry script (default `"app.py"`). Hearth passes `PORT` and `HTTP_PLATFORM_PORT` env vars.
  * `Node.js`: Entry script (default `"server.js"`). Hearth passes `PORT` and `HTTP_PLATFORM_PORT` env vars.
  * `PHP`: Optional router script (Hearth runs `php -S 127.0.0.1:<port> -t "<root>"`).
  * `.NET Core`: Optional DLL/script or `"run"` (Hearth sets `ASPNETCORE_URLS=http://localhost:<port>`).
  * `Other`: Command arguments; `{port}` or `%port%` is replaced with the port number.
* `Executable`: Leave empty `""` to auto-discover bundled runtimes (`<HEARTH_DIR>\runtimes\...`), project `.venv`, or system `PATH`. Otherwise set an explicit executable path.
* `AutoStart`: If `true`, `HearthManagerService` automatically starts and keeps this site running.
* `AutoRestart`: If `true`, restarts the site automatically when source files change (live reload).

---

## 3. Controlling Sites When `HearthManagerService` Is Running

When the Windows Service `HearthManagerService` is active, its watchdog polls every ~1 second:
* **Check service status:** `Get-Service HearthManagerService`
* **Start/Stop the service (Admin):** `Start-Service HearthManagerService` / `Stop-Service HearthManagerService`
* **Auto-start a site:** Set `"AutoStart": true` in `manager_sites.json` and ensure its port is **not** in `<HEARTH_DIR>\manual_stopped.txt`.
* **Temporarily stop a single site:** Append its port number (one port per line) to `<HEARTH_DIR>\manual_stopped.txt`. The watchdog will stop the process within ~1 second without changing `AutoStart`.
* **Resume a manually stopped site:** Remove its port number from `<HEARTH_DIR>\manual_stopped.txt`.
* **Delete or change a site's port:** Update `manager_sites.json`; the watchdog automatically stops orphaned processes whose ports are no longer configured.

---

## 4. Checking Running Status

* **Check listening port:**
  ```powershell
  Get-NetTCPConnection -LocalPort 8080 -State Listen -ErrorAction SilentlyContinue
  ```
* **Check tracked non-ASP.NET processes:** Read `<HEARTH_DIR>\tracked_processes.txt` (`port|pid|processName` per line).
* **Check ASP.NET Host status / trigger recycle via IPC:**
  * Running mutex: `Global\HearthPortableWebServer_Running_<port>`
  * Ready event: `Global\HearthPortableWebServer_Ready_<port>`
  * Shutdown event: `Global\HearthPortableWebServer_Shutdown_<port>`
  * Recycle AppDomain event: `Global\HearthPortableWebServer_Recycle_<port>`
