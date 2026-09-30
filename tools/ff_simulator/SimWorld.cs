using Newtonsoft.Json.Linq;

namespace Cantops.FlexFab
{
    /// <summary>
    /// 시뮬 공유 상태 — 지그 1대의 물리 상태(모터 위치·경사계 방향)와 고장 주입 시나리오.
    /// 같은 DLL은 호스트에서 한 번만 로드되므로 static 으로 UartSim·TcpSim 이 공유한다.
    ///
    /// 각도 모델 (실측 로그 기준, 모터 1 step = 0.1125°):
    ///   φ(pos) = -pos × 0.1125          지그 각도 (경사계 setdir6 판독값과 같은 부호)
    ///   경사계 판독 = setdir6 ? φ : -φ
    ///   X 슬롯 보드: AN X = φ,  GAC = (0.02, sin φ, cos φ)
    ///   Y 슬롯 보드: AN Y = -φ, GAC = (sin φ, 0.02, cos φ)
    /// 홈(PMO011) = pos -1197 → φ≈+134.66, 이후 +1250 이동 시 φ≈-5.96 (지그 수평감시 baseline -6.0 부근)
    /// </summary>
    public static class SimWorld
    {
        public const double STEP_DEG = 0.1125;
        public const int HOME_POS = -1197;

        static readonly object _lock = new();
        static int _pos = 0;
        static int _gyroDir = 6;

        public static int Pos { get { lock (_lock) return _pos; } }
        public static void MoveRel(int steps) { lock (_lock) _pos += steps; }
        public static void Home() { lock (_lock) _pos = HOME_POS; }
        public static void SetGyroDir(int d) { lock (_lock) _gyroDir = d; }
        public static int GyroDir { get { lock (_lock) return _gyroDir; } }
        public static double Phi { get { lock (_lock) return -_pos * STEP_DEG; } }
        public static double GyroReading => GyroDir == 6 ? Phi : -Phi;

        // ───────────── 고장 주입 시나리오 (실행 폴더 sim_scenario.json) ─────────────
        // {"rules":[{"target":"uart_232_x2","match":"<VER>","action":"fail","times":1}]}
        //  target : 라이브러리 id (워크스페이스 sim_id) 또는 "*"
        //  match  : 명령 문자열 포함(대소문자 무시). 스트림 규칙은 무시
        //  action : fail(판정코드 1) | timeout(무응답) | garbage(깨진 응답) | exception(Send 예외)
        //           | stream_offset(스트림 값에 value 더함, mode "06"/"02"/생략=둘 다) | stream_every(value 개 중 1개만 송출)
        //  times  : 적용 횟수, -1 = 계속 (기본 -1). 파일이 바뀌면 횟수 초기화
        //  skip   : 처음 skip 번 맞는 명령은 정상 응답 후 적용 (기본 0)
        public sealed class Rule
        {
            public string Target = "*", Match = "", Action = "", Mode = "";
            public int Times = -1;
            public int Skip;        // 처음 Skip 번 맞는 명령은 통과시킨 뒤 적용 (예: 반복 2회차에만 FAIL)
            public double Value;
            public int Used, Seen;
        }

        static List<Rule> _rules = new();
        static DateTime _scenarioStamp = DateTime.MinValue;
        static string _scenarioText = "";   // 복사된 파일은 수정 시각이 원본과 같을 수 있어 내용도 비교 (SIL 0930 V08)
        static string ScenarioPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sim_scenario.json");

        /// <summary>사이클 시작(Connect)마다 호출 — 파일이 바뀌었을 때만 다시 읽는다.</summary>
        public static void ReloadScenario(Action<string>? log)
        {
            lock (_lock)
            {
                try
                {
                    if (!File.Exists(ScenarioPath))
                    {
                        if (_rules.Count > 0) log?.Invoke("[SIM] sim_scenario.json 없음 → 고장 주입 해제");
                        _rules = new(); _scenarioStamp = DateTime.MinValue; _scenarioText = ""; return;
                    }
                    var stamp = File.GetLastWriteTimeUtc(ScenarioPath);
                    var text = File.ReadAllText(ScenarioPath);
                    if (stamp == _scenarioStamp && text == _scenarioText) return;
                    var root = JObject.Parse(text);
                    var list = new List<Rule>();
                    foreach (var r in root["rules"] as JArray ?? new JArray())
                    {
                        list.Add(new Rule
                        {
                            Target = r["target"]?.ToString() ?? "*",
                            Match = r["match"]?.ToString() ?? "",
                            Action = (r["action"]?.ToString() ?? "").ToLowerInvariant(),
                            Mode = r["mode"]?.ToString() ?? "",
                            Times = r["times"]?.Value<int>() ?? -1,
                            Skip = r["skip"]?.Value<int>() ?? 0,
                            Value = r["value"]?.Value<double>() ?? 0,
                        });
                    }
                    _rules = list; _scenarioStamp = stamp; _scenarioText = text;
                    log?.Invoke($"[SIM] sim_scenario.json 로드: 규칙 {list.Count}개");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"[SIM] sim_scenario.json 읽기 실패 → 무시: {ex.Message}");
                    _rules = new();
                }
            }
        }

        /// <summary>명령 규칙 조회 — 맞으면 사용 횟수를 1 올리고 반환.</summary>
        public static Rule? Take(string target, string command)
        {
            lock (_lock)
            {
                foreach (var r in _rules)
                {
                    if (r.Action is "stream_offset" or "stream_every") continue;
                    if (r.Target != "*" && !r.Target.Equals(target, StringComparison.OrdinalIgnoreCase)) continue;
                    if (r.Match.Length > 0 && command.IndexOf(r.Match, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (r.Times >= 0 && r.Used >= r.Times) continue;
                    if (r.Seen++ < r.Skip) continue;
                    r.Used++;
                    return r;
                }
                return null;
            }
        }

        // stream_every: 스트림 프레임을 value 개 중 1개만 내보냄 (샘플 부족 재현, 예 value 60 → 1초에 약 2개)
        public static int StreamEvery(string target, string mode)
        {
            lock (_lock)
            {
                int every = 1;
                foreach (var r in _rules)
                    if (r.Action == "stream_every"
                        && (r.Target == "*" || r.Target.Equals(target, StringComparison.OrdinalIgnoreCase))
                        && (r.Mode.Length == 0 || r.Mode == mode))
                        every = Math.Max(every, (int)r.Value);
                return every;
            }
        }

        public static double StreamOffset(string target, string mode)
        {
            lock (_lock)
            {
                double sum = 0;
                foreach (var r in _rules)
                    if (r.Action == "stream_offset"
                        && (r.Target == "*" || r.Target.Equals(target, StringComparison.OrdinalIgnoreCase))
                        && (r.Mode.Length == 0 || r.Mode == mode))
                        sum += r.Value;
                return sum;
            }
        }

        // 결정적 잔떨림 (±0.004) — 매 실행 같은 값
        static int _seed = 12345;
        public static double Jitter()
        {
            lock (_lock)
            {
                _seed = unchecked(_seed * 1103515245 + 12345);
                return ((_seed >> 16) & 0x7FFF) / 32767.0 * 0.008 - 0.004;
            }
        }
    }
}
