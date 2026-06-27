<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="HearthPortableWebServer.Welcome.Default" %>

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Hearth · Portable ASP.NET Web Server</title>
    <link rel="icon" href="hearth-logo-250x250.png" />

    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="crossorigin" />
    <link href="https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,400;9..144,600;9..144,700&family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />

    <style>
        /* ---- Hearth design tokens (from the hearth-cs theme) ---- */
        :root {
            --bg: #f4ede2;
            --surface: #fffdf9;
            --surface-alt: #efe6d8;
            --ink: #2b2520;
            --ink-soft: #6b6055;
            --ink-faint: #9a8d7e;
            --ember: #c0572e;
            --ember-dark: #a5471f;
            --ember-soft: #f3ddd0;
            --border: #e4d8c7;
            --border-soft: #ede3d4;
            --serif: 'Fraunces', Georgia, 'Times New Roman', serif;
            --sans: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            --mono: 'SFMono-Regular', 'JetBrains Mono', Consolas, 'Liberation Mono', Menlo, monospace;
            --radius: 16px;
            --radius-sm: 10px;
            --radius-pill: 999px;
            --shadow-sm: 0 1px 2px rgba(58,42,28,.05), 0 2px 8px rgba(58,42,28,.04);
            --shadow-md: 0 4px 18px rgba(58,42,28,.08);
            --maxw: 1000px;
        }

        *, *::before, *::after { box-sizing: border-box; }

        body {
            margin: 0;
            background: var(--bg);
            color: var(--ink);
            font-family: var(--sans);
            font-size: 17px;
            line-height: 1.65;
            -webkit-font-smoothing: antialiased;
            text-rendering: optimizeLegibility;
        }

        img { max-width: 100%; height: auto; }
        a { color: var(--ember); text-decoration: none; }
        a:hover { color: var(--ember-dark); }
        strong { color: var(--ink); font-weight: 600; }

        h1, h2, h3 {
            font-family: var(--serif);
            color: var(--ink);
            font-weight: 600;
            line-height: 1.2;
            letter-spacing: -.01em;
            margin: 0 0 .5em;
        }

        ::selection { background: var(--ember-soft); }

        .container { width: 100%; max-width: var(--maxw); margin-inline: auto; padding-inline: 24px; }
        .block { padding-block: 52px; }

        /* ---- Buttons ---- */
        .btn {
            display: inline-flex; align-items: center; gap: 8px;
            font-family: var(--sans); font-weight: 600; font-size: .95rem; line-height: 1;
            padding: 12px 20px; border-radius: var(--radius-pill);
            border: 1px solid transparent; cursor: pointer;
            transition: background .15s ease, color .15s ease, border-color .15s ease, transform .05s ease;
        }
        .btn:active { transform: translateY(1px); }
        .btn-sm { padding: 9px 16px; font-size: .9rem; }
        .btn-primary { background: var(--ember); color: #fff; }
        .btn-primary:hover { background: var(--ember-dark); color: #fff; }
        .btn-ghost { background: transparent; color: var(--ink); border-color: var(--border); }
        .btn-ghost:hover { background: var(--surface-alt); color: var(--ink); }

        /* ---- Top bar ---- */
        .topbar { background: var(--surface); border-bottom: 1px solid var(--border); }
        .topbar-inner { display: flex; align-items: center; justify-content: space-between; gap: 16px; min-height: 60px; }
        .brand { display: inline-flex; align-items: center; gap: 10px; color: var(--ink); }
        .brand:hover { color: var(--ink); }
        .brand-logo { width: 30px; height: 30px; border-radius: 8px; display: block; }
        .brand-name { font-family: var(--serif); font-weight: 700; font-size: 1.25rem; letter-spacing: -.02em; }
        .brand-tag { color: var(--ink-faint); font-size: .85rem; border-left: 1px solid var(--border); padding-left: 10px; }

        /* ---- Hero ---- */
        .hero { text-align: center; padding-block: 64px 8px; }
        .hero-logo img { width: 104px; height: 104px; border-radius: 24px; box-shadow: var(--shadow-md); display: block; margin: 0 auto 22px; }
        .hero-eyebrow {
            display: inline-flex; align-items: center; gap: 9px;
            color: var(--ember); font-weight: 600; font-size: .82rem;
            text-transform: uppercase; letter-spacing: .08em;
            background: var(--ember-soft); padding: 6px 14px; border-radius: var(--radius-pill);
        }
        .hero-title { font-size: clamp(2.3rem, 6vw, 3.5rem); margin: 20px 0 14px; }
        .hero-sub { font-size: 1.13rem; color: var(--ink-soft); max-width: 600px; margin: 0 auto; }
        .hero-actions { display: flex; flex-wrap: wrap; gap: 12px; justify-content: center; margin-top: 28px; }

        .dot { width: 8px; height: 8px; border-radius: 50%; background: #2faa5a; box-shadow: 0 0 0 0 rgba(47,170,90,.5); animation: pulse 2s infinite; }
        @keyframes pulse {
            0%   { box-shadow: 0 0 0 0 rgba(47,170,90,.45); }
            70%  { box-shadow: 0 0 0 8px rgba(47,170,90,0); }
            100% { box-shadow: 0 0 0 0 rgba(47,170,90,0); }
        }

        /* ---- Live proof card ---- */
        .proof {
            background: var(--surface); border: 1px solid var(--border);
            border-radius: var(--radius); box-shadow: var(--shadow-sm);
            padding: 28px; max-width: 760px; margin: 0 auto;
        }
        .proof-head h2 { font-size: 1.4rem; margin: 0 0 4px; }
        .proof-head p { color: var(--ink-soft); margin: 0 0 20px; font-size: .97rem; }

        .facts {
            display: grid; grid-template-columns: repeat(3, 1fr); gap: 1px;
            background: var(--border-soft); border: 1px solid var(--border-soft);
            border-radius: var(--radius-sm); overflow: hidden;
        }
        .fact { background: var(--surface); padding: 14px 16px; display: flex; flex-direction: column; gap: 4px; }
        .fact-k { font-size: .7rem; text-transform: uppercase; letter-spacing: .07em; color: var(--ink-faint); font-weight: 600; }
        .fact-v { font-family: var(--mono); font-size: .92rem; color: var(--ink); word-break: break-word; }
        .fact-v .ok { color: #2faa5a; }

        .pingbar { display: flex; align-items: center; gap: 16px; flex-wrap: wrap; margin-top: 20px; }
        .ping-msg { color: var(--ink-soft); font-size: .95rem; }
        .proof-note { margin: 16px 0 0; font-size: .88rem; color: var(--ink-faint); }

        /* ---- Stats strip ---- */
        .stats {
            display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; text-align: center;
            border-top: 1px solid var(--border); border-bottom: 1px solid var(--border);
            padding-block: 36px;
        }
        .stat-num { display: block; font-family: var(--serif); font-weight: 700; font-size: 2.1rem; color: var(--ember); line-height: 1; }
        .stat-unit { font-size: 1rem; font-weight: 600; margin-left: 2px; }
        .stat-label { display: block; color: var(--ink-soft); font-size: .9rem; margin-top: 8px; }

        /* ---- Features ---- */
        .features-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 30px; }
        .feature-icon {
            display: inline-flex; align-items: center; justify-content: center;
            width: 46px; height: 46px; border-radius: var(--radius-sm);
            background: var(--ember-soft); color: var(--ember); margin-bottom: 14px;
        }
        .feature-icon svg { width: 22px; height: 22px; }
        .feature h3 { font-size: 1.18rem; margin-bottom: 8px; }
        .feature p { color: var(--ink-soft); margin: 0; font-size: .97rem; }

        /* ---- Footer ---- */
        .site-footer { background: var(--surface); border-top: 1px solid var(--border); margin-top: 56px; padding-block: 28px; }
        .footer-inner { display: flex; align-items: center; justify-content: space-between; gap: 16px; flex-wrap: wrap; }
        .foot-brand { display: inline-flex; align-items: center; gap: 10px; font-family: var(--serif); font-weight: 600; color: var(--ink); }
        .foot-brand img { width: 26px; height: 26px; border-radius: 7px; display: block; }
        .foot-meta { color: var(--ink-faint); font-size: .88rem; }

        /* ---- Responsive ---- */
        @media (max-width: 720px) {
            body { font-size: 16px; }
            .container { padding-inline: 18px; }
            .block { padding-block: 40px; }
            .brand-tag { display: none; }
            .facts { grid-template-columns: 1fr 1fr; }
            .stats { grid-template-columns: 1fr 1fr; gap: 28px 20px; }
            .proof { padding: 22px; }
        }
        @media (max-width: 440px) {
            .facts { grid-template-columns: 1fr; }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">

        <header class="topbar">
            <div class="container topbar-inner">
                <a class="brand" href="https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server">
                    <img class="brand-logo" src="hearth-logo-250x250.png" alt="Hearth logo" />
                    <span class="brand-name">Hearth</span>
                    <span class="brand-tag">Portable ASP.NET Web Server</span>
                </a>
                <a class="btn btn-ghost btn-sm" href="https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server">View on GitHub &#8599;</a>
            </div>
        </header>

        <main>
            <!-- HERO -->
            <section class="hero container">
                <div class="hero-logo"><img src="hearth-logo-250x250.png" alt="" /></div>
                <span class="hero-eyebrow"><span class="dot"></span> Live &middot; served without IIS</span>
                <h1 class="hero-title">It&#8217;s already running.</h1>
                <p class="hero-sub">
                    The page you&#8217;re reading is a live <strong>ASP.NET Web Forms</strong> application,
                    rendered by the Hearth Portable Web Server &mdash; a self-contained, IIS-free host
                    built on C#&nbsp;7.3 / .NET&nbsp;Framework&nbsp;4.8.
                </p>
                <div class="hero-actions">
                    <a class="btn btn-primary" href="https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server">Get Hearth</a>
                    <a class="btn btn-ghost" href="https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server/wiki/Performance-Benchmark">See the benchmark</a>
                </div>
            </section>

            <!-- LIVE PROOF -->
            <section class="block">
                <div class="container">
                    <div class="proof">
                        <div class="proof-head">
                            <h2>Proof it&#8217;s live</h2>
                            <p>Every value below was generated by the server, just now, to render this request.</p>
                        </div>

                        <div class="facts">
                            <div class="fact"><span class="fact-k">Served by</span><span class="fact-v"><%: WorkerProcess %></span></div>
                            <div class="fact"><span class="fact-k">Host</span><span class="fact-v"><%: HostAuthority %></span></div>
                            <div class="fact"><span class="fact-k">Machine</span><span class="fact-v"><%: MachineName %></span></div>
                            <div class="fact"><span class="fact-k">Logical CPUs</span><span class="fact-v"><%= ProcessorCount %></span></div>
                            <div class="fact"><span class="fact-k">Runtime</span><span class="fact-v">C#&nbsp;7.3 &middot; .NET&nbsp;4.8</span></div>
                            <div class="fact"><span class="fact-k">Server time</span><span class="fact-v"><%: ServerTime %></span></div>
                        </div>

                        <div class="pingbar">
                            <asp:Button ID="PingButton" runat="server" CssClass="btn btn-primary"
                                Text="Post back to the server" OnClick="Ping_Click" />
                            <asp:Label ID="PingLabel" runat="server" CssClass="ping-msg" />
                        </div>

                        <p class="proof-note">
                            That button fires a full page postback through the ASP.NET lifecycle. The counter
                            survives via <strong>ViewState</strong> &mdash; the exact same pipeline IIS drives, only here
                            it runs straight from <code>HttpListener</code> to <code>HttpRuntime.ProcessRequest</code>.
                        </p>
                    </div>
                </div>
            </section>

            <!-- STATS -->
            <section class="container">
                <div class="stats">
                    <div class="stat"><span class="stat-num">14,980</span><span class="stat-label">peak requests / sec</span></div>
                    <div class="stat"><span class="stat-num">+16%</span><span class="stat-label">faster than stock IIS</span></div>
                    <div class="stat"><span class="stat-num">0</span><span class="stat-label">failures in 1M+ requests</span></div>
                    <div class="stat"><span class="stat-num">8.5<span class="stat-unit">ms</span></span><span class="stat-label">avg latency at peak load</span></div>
                </div>
            </section>

            <!-- FEATURES -->
            <section class="block">
                <div class="container">
                    <div class="features-grid">
                        <div class="feature">
                            <span class="feature-icon">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"></path></svg>
                            </span>
                            <h3>No IIS required</h3>
                            <p>Drop in your app and run one <code>.exe</code>. Loopback when unprivileged, all interfaces when elevated &mdash; same <code>HTTP.sys</code> kernel driver IIS uses.</p>
                        </div>
                        <div class="feature">
                            <span class="feature-icon">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="3" width="18" height="18" rx="3"></rect><path d="M9 3v18M3 9h6"></path></svg>
                            </span>
                            <h3>One isolated worker</h3>
                            <p>A dedicated ASP.NET <code>AppDomain</code> via <code>ApplicationHost.CreateApplicationHost</code> &mdash; exactly how IIS isolates an app pool. An async <code>HttpListener</code> loop keeps accepts outstanding and dispatches every request onto the thread pool.</p>
                        </div>
                        <div class="feature">
                            <span class="feature-icon">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="4 17 10 11 4 5"></polyline><line x1="12" y1="19" x2="20" y2="19"></line></svg>
                            </span>
                            <h3>Runs headless</h3>
                            <p>The host is a standalone process &mdash; launch it straight from the command line with <code>--port</code> and <code>--root</code>, no GUI required. Shut it down just as simply: a <code>--stop</code> signal or <code>Ctrl+C</code>.</p>
                        </div>
                        <div class="feature">
                            <span class="feature-icon">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M13 2 4 14h7l-1 8 9-12h-7l1-8z"></path></svg>
                            </span>
                            <h3>Lean pipeline</h3>
                            <p>No native-module chain, no access logging, no app-pool overhead. Fewer cycles per request &mdash; and zero dropped requests under load.</p>
                        </div>
                        <div class="feature">
                            <span class="feature-icon">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>
                            </span>
                            <h3>Hot reload on change</h3>
                            <p>A file watcher tracks <code>/bin/*.dll</code>, <code>/App_Code/*.cs</code>, and <code>web.config</code>, restarting the worker the moment any of them changes &mdash; the same auto-recycle ASP.NET performs under IIS.</p>
                        </div>
                        <div class="feature">
                            <span class="feature-icon">
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18.36 6.64a9 9 0 1 1-12.73 0"></path><line x1="12" y1="2" x2="12" y2="12"></line></svg>
                            </span>
                            <h3>Launcher &amp; auto-start</h3>
                            <p>A WinForms launcher sets the port and web root, then starts, stops, and browses &mdash; plus one-click install / uninstall as a Windows service that auto-starts on boot (UAC-prompted).</p>
                        </div>
                    </div>
                </div>
            </section>
        </main>

        <footer class="site-footer">
            <div class="container footer-inner">
                <span class="foot-brand">
                    <img src="hearth-logo-250x250.png" alt="" />
                    Hearth Portable ASP.NET Web Server
                </span>
                <span class="foot-meta">
                    C#&nbsp;7.3 &middot; .NET&nbsp;Framework&nbsp;4.8 &middot;
                    <a href="https://github.com/ASP-NET-Web-Forms-Club/Hearth-ASPNET-Server">ASP-NET-Web-Forms-Club</a>
                </span>
            </div>
        </footer>

    </form>
</body>
</html>
