using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// SIL E2E 구동기 — 실행 폴더(격리 복사본) 안에서 flexfab MainForm을 띄우고 Start·시리얼 창(4슬롯 4칸 / 2슬롯 2칸)·팝업을 자동 처리, 사이클마다 자동 판정.
// 사용: simui.exe <report.txt> <cycles.txt> [마감 "YYYY-MM-DD HH:MM"]   (준비: tools/sil_e2e/setup_run.py, 설명: README.md)
//   cycles.txt 한 줄 = 이름|X1|X2|Y1|Y2|scenario파일(없으면 -)|예아니오(Y/N)|준비동작(;구분)|기대(&구분)
static class P
{
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr h, EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern IntPtr PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("kernel32.dll")] static extern uint SetThreadExecutionState(uint flags);

    static readonly StringBuilder R = new();
    static void W(string s) { lock (R) R.AppendLine($"{DateTime.Now:HH:mm:ss} {s}"); }
    static Form main;
    static string[] cur = new string[4];
    static bool yesAnswer = true;
    static volatile int dialogsSeen;
    static readonly List<string> popupTexts = new();
    static int checkedN, mismatch;
    static DateTime argDeadline = DateTime.MaxValue;
    static readonly HashSet<Form> handled = new();
    static readonly HashSet<IntPtr> handledNative = new();

    [STAThread]
    static void Main(string[] args)
    {
        // 밤샘 실행: 이 프로세스가 도는 동안 시스템 절전 금지 (프로세스 종료 시 자동 해제)
        SetThreadExecutionState(0x80000000u | 0x00000001u);   // ES_CONTINUOUS | ES_SYSTEM_REQUIRED
        string report = args[0];
        if (args.Length >= 3) argDeadline = DateTime.Parse(args[2]);
        var cycles = File.ReadAllLines(args[1]).Where(l => l.Trim().Length > 0 && !l.StartsWith("#")).ToList();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var asm = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "flexfab.dll"));
        var t = asm.GetType("flexfab.MainForm", true);
        main = (Form)Activator.CreateInstance(t, new object[] { null });
        var timer = new System.Windows.Forms.Timer { Interval = 150 };
        timer.Tick += (_, _) => { try { HandleDialogs(); } catch (Exception e) { W("handler 예외 " + e.Message); } };
        timer.Start();
        main.Shown += (_, _) => new Thread(() => Driver(t, cycles, report)) { IsBackground = true }.Start();
        Application.Run(main);
    }

    static object F(Type t, string name) => t.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(main);

    static void Driver(Type t, List<string> cycles, string report)
    {
        try
        {
            Thread.Sleep(3000);
            DateTime deadline = argDeadline;
            foreach (var line in cycles)
            {
                if (DateTime.Now > deadline) { W($"===== 마감 시각 {deadline:MM-dd HH:mm} 도달 → 종료 (판정 {checkedN} / 불일치 {mismatch})"); break; }
                var c = line.Split('|');
                string name = c[0];
                cur = new[] { c[1], c[2], c[3], c[4] }.Select(s => s == "-" ? "" : s).ToArray();
                string scen = c[5];
                yesAnswer = c.Length < 7 || c[6] != "N";
                string scenPath = Path.Combine(AppContext.BaseDirectory, "sim_scenario.json");
                if (scen == "-") { if (File.Exists(scenPath)) File.Delete(scenPath); }
                else File.Copy(Path.Combine(AppContext.BaseDirectory, "..", scen), scenPath, true);
                W($"===== CYCLE {name}  X1={cur[0]} X2={cur[1]} Y1={cur[2]} Y2={cur[3]} scenario={scen}");
                int stopAfter = -1;
                if (c.Length >= 8 && c[7].Trim().Length > 0)
                    foreach (var act in c[7].Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        W("  [준비] " + act);
                        if (act.StartsWith("stop:")) stopAfter = int.Parse(act[5..]);
                        else PreAction(t, act.Trim());
                    }
                ListPending("  [시작 전 pending]");
                main.BeginInvoke(() => t.GetMethod("button_start_Click", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(main, new object[] { main, EventArgs.Empty }));
                if (stopAfter >= 0)
                    new Thread(() =>
                    {
                        Thread.Sleep(stopAfter * 1000);
                        W($"  [STOP] {stopAfter}s 경과 → buttonStop_Click");
                        main.BeginInvoke(() => t.GetMethod("buttonStop_Click", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(main, new object[] { main, EventArgs.Empty }));
                    }) { IsBackground = true }.Start();
                var sw = System.Diagnostics.Stopwatch.StartNew();
                bool started = false;
                lock (popupTexts) popupTexts.Clear();
                while (sw.Elapsed.TotalSeconds < 20) { if ((bool)main.Invoke(() => F(t, "_isRunning"))) { started = true; break; } Thread.Sleep(200); }
                if (!started) W("  (검사 시작 안 됨)");
                bool timedOut = false;
                while (started && (bool)main.Invoke(() => F(t, "_isRunning")))
                {
                    Thread.Sleep(500);
                    if (!timedOut && sw.Elapsed.TotalSeconds > 600)
                    {
                        timedOut = true;
                        W("  [TIMEOUT] 600s 초과 → STOP");
                        main.BeginInvoke(() => t.GetMethod("buttonStop_Click", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(main, new object[] { main, EventArgs.Empty }));
                    }
                }
                int quiet = 0;
                while (quiet < 12) { int n = dialogsSeen; Thread.Sleep(250); quiet = (n == dialogsSeen && OpenDialogs() == 0) ? quiet + 1 : 0; }
                W($"  소요 {sw.Elapsed.TotalSeconds:F0}s");
                ListPending("  [종료 후 pending]");
                // 자동 판정 (9번째 칸): '&'로 구분한 기대 조건 — 문구 포함 / pending=N / nopopup / !문구(없어야 함)
                if (c.Length >= 9 && c[8].Trim().Length > 0)
                {
                    string all; lock (popupTexts) all = string.Join(" ## ", popupTexts);
                    int pend = Directory.GetFiles(Base, "pending_*.json").Length;
                    var bad = new List<string>();
                    foreach (var e in c[8].Split('&', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var x = e.Trim();
                        if (x.StartsWith("pending=")) { if (pend != int.Parse(x[8..])) bad.Add($"{x}(실제 {pend})"); }
                        else if (x == "nopopup") { if (popupTexts.Count > 0) bad.Add("nopopup"); }
                        else if (x.StartsWith("!")) { if (all.Contains(x[1..])) bad.Add(x); }
                        else if (!all.Contains(x)) bad.Add(x);
                    }
                    if (timedOut) bad.Add("TIMEOUT");
                    string verdict = bad.Count == 0 ? "CHECK OK" : "CHECK MISMATCH: " + string.Join(" / ", bad);
                    W("  " + verdict);
                    if (bad.Count > 0) { mismatch++; W("    팝업: " + all); }
                    checkedN++;
                }
                using (var pr = System.Diagnostics.Process.GetCurrentProcess())
                    W($"  [MEM] WS={pr.WorkingSet64 / 1048576}MB Private={pr.PrivateMemorySize64 / 1048576}MB GC={GC.GetTotalMemory(false) / 1048576}MB Handles={pr.HandleCount}  누적 판정 {checkedN} / 불일치 {mismatch}");
                main.Invoke(() =>
                {
                    var g = (DataGridView)F(t, "dataGridView1");
                    var hdr = string.Join(" | ", g.Columns.Cast<DataGridViewColumn>().Skip(2).Select(col => col.HeaderText.Replace("\n", "/")));
                    W("  GRID 헤더: " + hdr);
                    foreach (DataGridViewRow r in g.Rows)
                    {
                        if (r.IsNewRow) continue;
                        var vals = r.Cells.Cast<DataGridViewCell>().Select(x => x.Value?.ToString() ?? "").ToList();
                        W("  GRID " + string.Join(" | ", vals));
                    }
                });
                File.WriteAllText(report, R.ToString());
            }
        }
        catch (Exception e) { W("DRIVER 예외 " + e); }
        File.WriteAllText(report, R.ToString());
        main.BeginInvoke(() => Environment.Exit(0));
    }

    static string Base => AppContext.BaseDirectory;

    static void ListPending(string label)
    {
        var fs = Directory.GetFiles(Base, "pending*").Select(Path.GetFileName).OrderBy(x => x).ToList();
        var ds = Directory.GetDirectories(Base, "pending*").Select(d => Path.GetFileName(d) + "/").ToList();
        W(label + " " + (fs.Count + ds.Count == 0 ? "(없음)" : string.Join(", ", fs.Concat(ds))));
    }

    // 사이클 전 준비 동작
    //  clean | pend:<lane>:<serial>:<ok|wsbad|noresult|old25h|noserial> | legacy:<lane> | inuse:<lane>:<serial>
    //  tmpdir:<lane> | rmtmpdir:<lane> | jsondir:<serial> | rmjsondir:<serial> | repeat:<N> | fc:<0|1> | skiprow:<i> | unskip
    static void PreAction(Type t, string act)
    {
        var a = act.Split(':');
        string wsKey = Path.GetFileNameWithoutExtension((string)main.Invoke(() => F(t, "_currentWorkspacePath")));
        string PP(string lane) => Path.Combine(Base, $"pending_{wsKey}_x{lane}.json");
        string now(double h = 0) => DateTime.Now.AddHours(h).ToString("yyyy-MM-dd HH:mm:ss");
        string ProjDir()
        {
            string name = (string)main.Invoke(() =>
            {
                dynamic ws = F(t, "workspace");
                return ((string)((IDictionary<string, object>)((IList<object>)ws.projects)[0])["name"]).Replace(" ", "");
            });
            return Path.Combine(Directory.GetCurrentDirectory(), "Result", name, DateTime.Now.ToString("yyyy-MM-dd"));
        }
        switch (a[0])
        {
            case "clean":
                foreach (var f in Directory.GetFiles(Base, "pending*")) File.Delete(f);
                foreach (var d in Directory.GetDirectories(Base, "pending*")) Directory.Delete(d, true);
                break;
            case "pend":
            {
                string lane = a[1], sn = a[2], v = a[3];
                string ws = v == "wsbad" ? "OTHER_workspace" : wsKey;
                string xr = v == "noresult" ? "" : ",\"x_result\":\"PASS\"";
                string tm = v == "old25h" ? now(-25) : now(-0.1);
                string s = v == "noserial" ? "" : sn;
                File.WriteAllText(PP(lane), $"{{\"serial\":\"{s}\",\"slot\":\"X{lane}슬롯\",\"ws\":\"{ws}\"{xr},\"time\":\"{tm}\",\"retmsg\":[]}}");
                break;
            }
            case "legacy":
                File.WriteAllText(Path.Combine(Base, $"pending_x{a[1]}_result.json"), "{\"serial\":\"VL1-LEGACY\"}");
                break;
            case "inuse":
                File.WriteAllText(PP(a[1]) + ".inuse", $"{{\"serial\":\"{a[2]}\",\"ws\":\"{wsKey}\",\"x_result\":\"PASS\",\"time\":\"{now(-0.1)}\",\"retmsg\":[]}}");
                break;
            case "tmpdir": Directory.CreateDirectory(PP(a[1]) + ".tmp"); break;
            case "rmtmpdir": if (Directory.Exists(PP(a[1]) + ".tmp")) Directory.Delete(PP(a[1]) + ".tmp", true); break;
            case "jsondir": Directory.CreateDirectory(Path.Combine(ProjDir(), a[1] + ".json")); break;
            case "rmjsondir": { var p = Path.Combine(ProjDir(), a[1] + ".json"); if (Directory.Exists(p)) Directory.Delete(p, true); break; }
            case "repeat":
                main.Invoke(() =>
                {
                    dynamic ws = F(t, "workspace");
                    ((IDictionary<string, object>)((IList<object>)ws.projects)[0])["repeat_all_count"] = long.Parse(a[1]);
                });
                break;
            case "fc":
                main.Invoke(() => ((CheckBox)F(t, "checkBox_Process")).Checked = a[1] == "1");
                break;
            case "skiprow":
                main.Invoke(() => ((DataGridView)F(t, "dataGridView1")).Rows[int.Parse(a[1])].Cells[0].Style.BackColor = System.Drawing.Color.Red);
                break;
            case "unskip":
                main.Invoke(() => { foreach (DataGridViewRow r in ((DataGridView)F(t, "dataGridView1")).Rows) r.Cells[0].Style.BackColor = System.Drawing.Color.White; });
                break;
            default: W("  [준비] 알 수 없는 동작: " + act); break;
        }
    }

    static int OpenDialogs()
    {
        int n = 0;
        main.Invoke(() => { foreach (Form f in Application.OpenForms) if (f != main && f.Visible) n++; });
        return n + NativeDialogs().Count;
    }

    static IEnumerable<string> Texts(Control c)
    {
        foreach (Control ch in c.Controls)
        {
            if ((ch is Label || ch is TextBox || ch is Button) && !string.IsNullOrWhiteSpace(ch.Text))
                yield return ch.GetType().Name + ":" + ch.Text.Replace("\r", "").Replace("\n", "/");
            foreach (var s in Texts(ch)) yield return s;
        }
    }
    static IEnumerable<T> All<T>(Control c) where T : Control
    {
        foreach (Control ch in c.Controls) { if (ch is T x) yield return x; foreach (var y in All<T>(ch)) yield return y; }
    }

    static void HandleDialogs()
    {
        foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
        {
            if (f == main || !f.Visible || handled.Contains(f)) continue;
            handled.Add(f); dialogsSeen++;
            var tbs = All<TextBox>(f).Where(x => x.MaxLength == 11).ToList();
            if (tbs.Count == 4)
            {
                W("  [시리얼창] 기본값 X1=" + tbs[0].Text + " X2=" + tbs[1].Text + " Y1=" + tbs[2].Text + " Y2=" + tbs[3].Text
                  + "  placeholder Y1=" + tbs[2].PlaceholderText + " Y2=" + tbs[3].PlaceholderText);
                for (int i = 0; i < 4; i++) tbs[i].Text = cur[i];
                All<Button>(f).First(b => b.Text == "확인").PerformClick();
                continue;
            }
            if (tbs.Count == 2)   // 2슬롯 시리얼 창: X = X1칸, Y = Y1칸
            {
                W("  [시리얼창 2슬롯] 기본값 X=" + tbs[0].Text + " Y=" + tbs[1].Text);
                tbs[0].Text = cur[0]; tbs[1].Text = cur[2];
                All<Button>(f).First(b => b.Text == "확인").PerformClick();
                continue;
            }
            string ptxt = $"[{f.GetType().Name}] {f.Text} {string.Join(" ; ", Texts(f))}";
            lock (popupTexts) popupTexts.Add(ptxt);
            W($"  [팝업 {f.GetType().Name}] 제목={f.Text} 내용={string.Join(" ; ", Texts(f))}");
            var btns = All<Button>(f).ToList();
            var yes = btns.FirstOrDefault(b => b.Text == "예" || b.Text == "확인" || b.Text == "OK" || b.Text == "덮어쓰기");
            var no = btns.FirstOrDefault(b => b.Text == "아니오" || b.Text == "취소");
            var pick = (!yesAnswer && no != null) ? no : yes;
            if (pick != null) { W("    -> " + pick.Text); pick.PerformClick(); }
            else { W("    -> Close"); f.Close(); }
        }
        foreach (var h in NativeDialogs())
        {
            if (handledNative.Contains(h)) continue;
            handledNative.Add(h); dialogsSeen++;
            var sb = new StringBuilder();
            EnumChildWindows(h, (ch, _) => { var s = new StringBuilder(512); GetWindowText(ch, s, 512); if (s.Length > 0) sb.Append(s).Append(" ; "); return true; }, IntPtr.Zero);
            var title = new StringBuilder(256); GetWindowText(h, title, 256);
            lock (popupTexts) popupTexts.Add($"[MessageBox] {title} {sb}");
            W($"  [MessageBox] 제목={title} 내용={sb.ToString().Replace("\n", "/")}");
            PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero);   // WM_CLOSE
        }
    }

    static List<IntPtr> NativeDialogs()
    {
        var list = new List<IntPtr>();
        uint me = (uint)Environment.ProcessId;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out var pid);
            if (pid != me || !IsWindowVisible(h)) return true;
            var cls = new StringBuilder(64); GetClassName(h, cls, 64);
            if (cls.ToString() == "#32770") list.Add(h);
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
