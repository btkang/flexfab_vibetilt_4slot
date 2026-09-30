using System.Globalization;
using System.Text.RegularExpressions;

namespace Cantops.FlexFab
{
    /// <summary>
    /// ff_common Uart 대체 — 워크스페이스 libraries 의 filename/classname 만 바꿔 끼운다.
    /// config (SIM 워크스페이스 생성 스크립트가 채움)
    ///   sim_id     : 라이브러리 id (고장 주입 target)
    ///   sim_role   : "vl"(제품 보드) | "gyro"(경사계)
    ///   sim_proto  : "232" | "485"  (485 는 응답에 주소 1 이 들어감)
    ///   sim_axis   : "X" | "Y"      (보드가 꽂힌 슬롯 방향 — 스트림 값 계산)
    ///   sim_fw     : VER 응답 펌웨어 문자열 (워크스페이스 expected_fw_version)
    ///   sim_appcfg : APPCFG 조회 응답 프레임 전체 (워크스페이스 expected_response)
    ///   sim_temp   : SENTEMP 응답 온도 (기본 27.79)
    /// 동작 규약은 실기 로그(2026-07-21 R1b3 232 MP)와 VibeTilt/MotionJig 수신 코드 기준.
    /// </summary>
    public class UartSim : ModuleBase, IComm, IProcess
    {
        readonly object _lk = new();
        readonly Queue<string> _out = new();
        bool _connected;
        bool _streaming;
        string _mode = "00";
        DateTime _nextFrame;
        long _frameNo;

        public UartSim()
        {
            info_ = new Dictionary<string, object?> { { "name", "UartSim" }, { "version", "0.1.0.1" } };
        }

        string Cfg(string key, string def = "")
        {
            try { return config_ != null && config_.TryGetValue(key, out var v) && v != null ? v.ToString() ?? def : def; }
            catch { return def; }
        }
        string Id => Cfg("sim_id", "uart");
        string Role => Cfg("sim_role", "vl");
        bool Is485 => Cfg("sim_proto", "232") == "485";
        string Axis => Cfg("sim_axis", "X").ToUpperInvariant();

        public override IDictionary<string, object?>? GetStatus()
        {
            bool c; lock (_lk) c = _connected;
            return new Dictionary<string, object?>
            {
                { "is_connected", c }, { "Connected", c }, { "type", "uart" },
                { "portname", "SIM:" + Id }, { "baudrate", Cfg("baudrate", "115200") },
                { "databits", 8 }, { "parity", "None" }, { "stopbits", "One" },
            };
        }

        public bool Connect()
        {
            SimWorld.ReloadScenario(log_action_);
            lock (_lk) { _connected = true; _out.Clear(); _streaming = false; _mode = "00"; }
            return true;
        }

        public void Disconnect() { lock (_lk) { _connected = false; _streaming = false; _out.Clear(); } }
        public void Begin() { Connect(); }
        public void End() { Disconnect(); }
        public void SetTimeout(int timeout) { }

        public void Send(IDictionary<string, object?> param)
        {
            lock (_lk) if (!_connected) throw new InvalidOperationException($"[SIM] {Id} 포트가 열려 있지 않습니다.");
            string body = param.TryGetValue("body", out var b) ? b?.ToString() ?? "" : "";

            if (Role == "gyro")
            {
                foreach (var line in body.Split('\n', '\r'))
                {
                    var cmd = line.Trim();
                    if (cmd.Length == 0) continue;
                    HandleGyro(cmd);
                }
                return;
            }

            foreach (Match m in Regex.Matches(body, "<([^<>]*)>"))
                HandleVl(m.Groups[1].Value.Trim());
        }

        // ───────────── 경사계 ─────────────
        void HandleGyro(string cmd)
        {
            var rule = SimWorld.Take(Id, cmd);
            if (Inject(rule, cmd, out var injected)) { if (injected != null) Enq(injected); return; }

            string lc = cmd.ToLowerInvariant();
            if (lc.StartsWith("get"))
            {
                // 실기 형식 "+000.951" / "-006.097". 음의 0(-0.0)은 .NET 섹션 서식에서 "-+000.000"이 되므로 부호를 직접 붙인다
                double v = Math.Round(SimWorld.GyroReading, 3);
                if (v == 0) v = 0;
                Enq((v < 0 ? "-" : "+") + Math.Abs(v).ToString("000.000", CultureInfo.InvariantCulture));
            }
            else if (lc == "setdir5") { SimWorld.SetGyroDir(5); Enq("OK"); }
            else if (lc == "setdir6") { SimWorld.SetGyroDir(6); Enq("OK"); }
            else Enq("OK");   // setzcur 등
        }

        // ───────────── 제품 보드 (VL10 232 / VL20 485) ─────────────
        void HandleVl(string inner)
        {
            var parts = inner.Split(',');
            string cmd = parts[0].Trim().ToUpperInvariant();
            var args = parts.Skip(1).Select(s => s.Trim()).ToList();

            var rule = SimWorld.Take(Id, "<" + inner + ">");
            if (rule != null && rule.Action != "fail")
            {
                if (Inject(rule, inner, out var injected)) { if (injected != null) Enq(injected); return; }
            }

            string resp;
            var fields = new List<string>(args);
            switch (cmd)
            {
                case "VER":
                    fields.Add(Cfg("sim_fw", "F/W ver.1.0.4"));
                    resp = Frame(cmd, fields);
                    break;
                case "SENTEMP":
                    fields.Add(Cfg("sim_temp", "27.79"));
                    resp = Frame(cmd, fields);
                    break;
                case "MODE":
                    if (fields.Count > 0)
                    {
                        string m = fields[^1];
                        if (m.Length == 1) m = "0" + m;
                        fields[^1] = m;
                        lock (_lk) _mode = m;
                    }
                    resp = Frame(cmd, fields);
                    break;
                case "APPCFG" when fields.Count == (Is485 ? 1 : 0):
                    resp = Cfg("sim_appcfg", Is485 ? "[APPCFG,0,1/00/0/10/1/115200]" : "[APPCFG,0,00/10/1/115200]");
                    break;
                case "START":
                    lock (_lk) { _streaming = _mode is "06" or "02"; _nextFrame = DateTime.Now.AddMilliseconds(10); }
                    resp = Frame(cmd, fields);
                    break;
                case "STOP":
                    lock (_lk) _streaming = false;
                    resp = Frame(cmd, fields);
                    break;
                default:
                    resp = Frame(cmd, fields);   // UID·OFFSET·APPCFG,SAVE·RCONF·REFTEMP = 보낸 인자 그대로
                    break;
            }

            if (rule != null && rule.Action == "fail")
            {
                resp = "[" + cmd + ",1" + resp.Substring(cmd.Length + 3);   // 판정코드(RV) 0 → 1
                log_action_?.Invoke($"[SIM] {Id} 고장 주입 fail: <{inner}> → {resp}");
            }
            Enq(resp);
        }

        static string Frame(string cmd, List<string> fields)
            => "[" + cmd + ",0" + string.Concat(fields.Select(f => "," + f)) + "]";

        bool Inject(SimWorld.Rule? rule, string cmd, out string? response)
        {
            response = null;
            if (rule == null) return false;
            log_action_?.Invoke($"[SIM] {Id} 고장 주입 {rule.Action}: {cmd}");
            switch (rule.Action)
            {
                case "timeout": return true;
                case "garbage": response = "@#$%&*"; return true;
                case "exception": throw new IOException($"[SIM] {Id} 주입 예외 ({cmd})");
                case "fail": response = "ERR"; return true;
                default: return false;
            }
        }

        void Enq(string line) { lock (_lk) { _out.Enqueue(line); if (_out.Count > 2000) _out.Dequeue(); } }

        // 스트림: 10ms 주기 프레임을 경과 시간만큼 쌓는다 (START 후 아무도 안 읽어도 버퍼에 누적 — 실기 시리얼 버퍼와 같음)
        void PumpStream()
        {
            var now = DateTime.Now;
            int every = _streaming ? SimWorld.StreamEvery(Id, _mode) : 1;
            while (_streaming && _nextFrame <= now)
            {
                if (_frameNo++ % every == 0)
                {
                    _out.Enqueue(MakeFrame());
                    if (_out.Count > 2000) _out.Dequeue();
                }
                _nextFrame = _nextFrame.AddMilliseconds(10);
            }
        }

        string MakeFrame()
        {
            double phi = SimWorld.Phi;
            double rad = phi * Math.PI / 180.0;
            double off = SimWorld.StreamOffset(Id, _mode);
            var ci = CultureInfo.InvariantCulture;
            string id = Is485 ? "1," : "";
            if (_mode == "06")
            {
                double x = Axis == "X" ? phi : 0.01, y = Axis == "X" ? 0.01 : -phi;
                if (Axis == "X") x += off; else y += off;
                x += SimWorld.Jitter(); y += SimWorld.Jitter();
                return $"[AN,{id}{x.ToString("0.00", ci)},{y.ToString("0.00", ci)}]";
            }
            double gx = Axis == "X" ? 0.02 : Math.Sin(rad);
            double gy = Axis == "X" ? Math.Sin(rad) : 0.02;
            double gz = Math.Cos(rad);
            gx += off + SimWorld.Jitter(); gy += off + SimWorld.Jitter(); gz += off + SimWorld.Jitter();
            return $"[GAC,{id}{gx.ToString("0.00", ci)},{gy.ToString("0.00", ci)},{gz.ToString("0.00", ci)},0.58]";
        }

        public IDictionary<string, object?>? Recv(IDictionary<string, object?>? param)
        {
            int timeout = 5000;
            try { if (param != null && param.TryGetValue("timeout", out var t) && t != null) timeout = Convert.ToInt32(t); } catch { }
            var deadline = DateTime.Now.AddMilliseconds(Math.Max(0, timeout));
            while (true)
            {
                bool streaming;
                lock (_lk)
                {
                    PumpStream();
                    if (_out.Count > 0) return new Dictionary<string, object?> { { "value", _out.Dequeue() } };
                    streaming = _streaming;
                }
                var remain = deadline - DateTime.Now;
                if (remain <= TimeSpan.Zero) return new Dictionary<string, object?> { { "value", null } };   // Uart 와 같이 null 이 아닌 value=null
                Thread.Sleep(streaming ? 5 : (int)Math.Min(remain.TotalMilliseconds, 20));
            }
        }
    }
}
