using System.Text.RegularExpressions;

namespace Cantops.FlexFab
{
    /// <summary>
    /// ff_common Tcp 대체 — EMIO 모터·IO 컨트롤러(100.100.100.70:2233) 흉내.
    /// 요청은 STX+명령+ETX(+\n), 응답은 STX 없는 평문 소문자 (MotionJig EmioRecvUntil/EmioRecvRaw 기준).
    ///   PII00          → pii01400000000000000   (과회전 비트 전부 0)
    ///   PPC00          → ppc006000000
    ///   PMS…           → pms0…
    ///   PMD0{len}1ML{n}→ pmd0011                (모터 위치 += n)
    ///   PMO011 / 012   → pmo0011 / pmo0012      (모터 위치 = 홈)
    /// config: sim_id (고장 주입 target, 기본 tcp_emio)
    /// </summary>
    public class TcpSim : ModuleBase, IComm, IProcess
    {
        readonly object _lk = new();
        readonly Queue<string> _out = new();
        bool _connected;

        public TcpSim()
        {
            info_ = new Dictionary<string, object?> { { "name", "TcpSim" }, { "version", "0.1.0.1" } };
        }

        string Id
        {
            get
            {
                try { return config_ != null && config_.TryGetValue("sim_id", out var v) && v != null ? v.ToString() ?? "tcp_emio" : "tcp_emio"; }
                catch { return "tcp_emio"; }
            }
        }

        public override IDictionary<string, object?>? GetStatus()
        {
            bool c; lock (_lk) c = _connected;
            return new Dictionary<string, object?> { { "Connected", c }, { "is_connected", c }, { "type", "tcp" } };
        }

        public bool Connect()
        {
            SimWorld.ReloadScenario(log_action_);
            lock (_lk) { _connected = true; _out.Clear(); }
            return true;
        }

        public void Disconnect() { lock (_lk) { _connected = false; _out.Clear(); } }
        public void Begin() { Connect(); }
        public void End() { Disconnect(); }
        public void SetTimeout(int timeout) { }

        public void Send(IDictionary<string, object?> param)
        {
            string body = param.TryGetValue("body", out var b) ? b?.ToString() ?? "" : "";
            string cmd = Regex.Replace(body, @"[\x00-\x1F\s]", "").ToUpperInvariant();
            if (cmd.Length == 0) return;

            var rule = SimWorld.Take(Id, cmd);
            if (rule != null)
            {
                log_action_?.Invoke($"[SIM] {Id} 고장 주입 {rule.Action}: {cmd}");
                switch (rule.Action)
                {
                    case "timeout": return;
                    case "garbage": Enq("@#$%&*"); return;
                    case "fail": Enq("err"); return;
                    case "exception": return;   // 실제 Tcp.Send 는 예외를 삼킨다 → 무응답과 같음
                }
            }

            string resp;
            if (cmd.StartsWith("PII")) resp = "pii01400000000000000";
            else if (cmd.StartsWith("PPC")) resp = "ppc006000000";
            else if (cmd.StartsWith("PMS")) resp = "pms0" + cmd.Substring(3).ToLowerInvariant();
            else if (cmd.StartsWith("PMD"))
            {
                var m = Regex.Match(cmd, @"ML(-?\d+)$");
                if (m.Success) SimWorld.MoveRel(int.Parse(m.Groups[1].Value));
                resp = "pmd0011";
            }
            else if (cmd.StartsWith("PMO01"))
            {
                SimWorld.Home();
                resp = "pmo001" + cmd.Substring(5);   // PMO011 → pmo0011, PMO012 → pmo0012
            }
            else resp = cmd.ToLowerInvariant();
            Enq(resp);
        }

        void Enq(string s) { lock (_lk) { _out.Enqueue(s); if (_out.Count > 200) _out.Dequeue(); } }

        public IDictionary<string, object?>? Recv(IDictionary<string, object?>? param)
        {
            int timeout = 1000;
            try { if (param != null && param.TryGetValue("timeout", out var t) && t != null) timeout = Convert.ToInt32(t); } catch { }
            var deadline = DateTime.Now.AddMilliseconds(Math.Max(0, timeout));
            while (true)
            {
                lock (_lk) if (_out.Count > 0) return new Dictionary<string, object?> { { "value", _out.Dequeue() } };
                var remain = deadline - DateTime.Now;
                if (remain <= TimeSpan.Zero) return null;   // 실제 Tcp: 데이터 없음 = null
                Thread.Sleep((int)Math.Min(remain.TotalMilliseconds, 20));
            }
        }
    }
}
