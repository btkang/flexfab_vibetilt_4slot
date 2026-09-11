using Cantops.FlexFab;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Cantops.FlexFab
{
    public class MotionJig : ModuleBase, IProcess
    {
        public const string MODULE_VERSION = "R1b3";

        // ══════════════════════════════════════════════
        //  모터 파라미터 (JSON에서 로딩, 기본값 하드코딩)
        // ══════════════════════════════════════════════
        private const double FINE_DIVERGE_LIMIT = 30.0; // C2: FINE 목표대비 이탈 한계(도). 초과 시 발산으로 보고 중단(케이블 보호)
        private double _stepResolution = 0.1125;  // 도/step
        private int _stepsToZero = 1250;           // 원점→0도
        private int _sensorDiffError = 10;         // 자이로-VL 차이 에러 임계값
        private double _reverseThreshold = 0.02;   // FineAdjust 방향 반전 임계값
        private int _moveTimeoutMin = 3000;        // MoveSteps 최소 타임아웃
        private int _moveTimeoutMax = 5000;        // MoveSteps 최대 타임아웃
        private int _homeTimeout = 15000;          // 홈 복귀 타임아웃
        private int _homePreWait = 500;            // 홈 복귀 전 대기
        private int _homePollInterval = 500;       // 자이로 폴링 간격
        private int _zeroSettleWait = 1500;        // 0도 이동 후 안정화
        private int _tiltMoveWait = 500;           // TILT coarse 이동 후
        private int _tiltReturnWait = 500;         // TILT 0도 복귀 후
        private int _vibePremoveWait = 500;        // VIBE pre_move 후
        private int _vibeMoveWait = 500;           // VIBE coarse 이동 후
        private int _fineConfirmWait = 500;        // FineAdjust 수렴 확인
        private int _fineStepWait = 500;           // FineAdjust 이동 후
        private int _fineRetryWait = 2000;         // FineAdjust 재시도 대기
        private int _levelReturnWait = 1500;       // 수평 복귀 전 대기
        private int _testStartWait = 2000;         // 테스트 시작 이동 후
        private int _vlStreamSettle = 300;         // VL 스트리밍 안정화
        private bool _saveTiltCsv = false;         // csv_tilt 저장 여부 (기본 off=양산, save_tilt_csv=true 시 저장)

        private void LoadParams(IDictionary<string, object?> param)
        {
            _stepResolution = GetParamDouble(param, "step_resolution", 0.1125);
            _stepsToZero = GetParamInt(param, "steps_to_zero", 1250);
            _sensorDiffError = GetParamInt(param, "sensor_diff_error", 10);
            _reverseThreshold = GetParamDouble(param, "reverse_threshold", 0.02);
            _saveTiltCsv = GetParamBool(param, "save_tilt_csv", false);   // csv_tilt 저장 (기본 off=양산)
            _moveTimeoutMin = GetParamInt(param, "move_timeout_min", 3000);
            _moveTimeoutMax = GetParamInt(param, "move_timeout_max", 5000);
            _homeTimeout = GetParamInt(param, "home_timeout", 15000);
            _homePreWait = GetParamInt(param, "home_pre_wait", 500);
            _homePollInterval = GetParamInt(param, "home_poll_interval", 500);
            _zeroSettleWait = GetParamInt(param, "zero_settle_wait", 1500);
            _tiltMoveWait = GetParamInt(param, "tilt_move_wait", 500);
            _tiltReturnWait = GetParamInt(param, "tilt_return_wait", 500);
            _vibePremoveWait = GetParamInt(param, "vibe_premove_wait", 500);
            _vibeMoveWait = GetParamInt(param, "vibe_move_wait", 500);
            _fineConfirmWait = GetParamInt(param, "fine_confirm_wait", 500);
            _fineStepWait = GetParamInt(param, "fine_step_wait", 500);
            _fineRetryWait = GetParamInt(param, "fine_retry_wait", 2000);
            _levelReturnWait = GetParamInt(param, "level_return_wait", 1500);
            _testStartWait = GetParamInt(param, "test_start_wait", 2000);
            _vlStreamSettle = GetParamInt(param, "vl_stream_settle", 300);
        }

        public MotionJig()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "motionjig" },
                { "desc", "Motion jig inspection module (260326 사양서)" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object?>> {
                    // 기존 호환
                    new Dictionary<string, object?> { { "id", "MOTION_232" },      { "desc", "모션지그 검사 RS-232 (VL10)" } },
                    new Dictionary<string, object?> { { "id", "MOTION_485" },      { "desc", "모션지그 검사 RS-485 (VL20)" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_232" }, { "desc", "모션지그 진동검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_TILT_232" }, { "desc", "모션지그 기울기검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_485" }, { "desc", "모션지그 진동검사 RS-485" } },
                    new Dictionary<string, object?> { { "id", "MOTION_TILT_485" }, { "desc", "모션지그 기울기검사 RS-485" } },
                    // 영점 초기화 (독립 검사항목)
                    new Dictionary<string, object?> { { "id", "MOTION_ZERO_232" }, { "desc", "영점 초기화 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_ZERO_485" }, { "desc", "영점 초기화 RS-485" } },
                    // 신규: 사양서 항목별
                    new Dictionary<string, object?> { { "id", "MOTION_TILT_X_232" }, { "desc", "#4 X축 기울기 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_TILT_X_485" }, { "desc", "#4 X축 기울기 RS-485" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_Y_232" }, { "desc", "#5 Y축 진동 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_Y_485" }, { "desc", "#5 Y축 진동 RS-485" } },
                    new Dictionary<string, object?> { { "id", "MOTION_TILT_Y_232" }, { "desc", "#6 Y축 기울기 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_TILT_Y_485" }, { "desc", "#6 Y축 기울기 RS-485" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_X_232" }, { "desc", "#7 X축 진동 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_X_485" }, { "desc", "#7 X축 진동 RS-485" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_Z_232" }, { "desc", "#8 Z축 진동 RS-232" } },
                    new Dictionary<string, object?> { { "id", "MOTION_VIBE_Z_485" }, { "desc", "#8 Z축 진동 RS-485" } },
                    // 통신검사
                    new Dictionary<string, object?> { { "id", "COMM_232" }, { "desc", "통신검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "COMM_485" }, { "desc", "통신검사 RS-485" } },
                }}
            };
        }

        public void Begin() { _stopRequested = false; _zeroInitialized = false; }
        public void End() { _stopRequested = true; }

        /// <summary>
        /// PII00으로 IN2 센서 상태를 확인하여 과회전 감지
        /// </summary>
        public bool CheckOverRotation(IComm emio, Action<string> log)
        {
            try
            {
                EmioSend(emio, "PII00", log);
                string resp = EmioRecvRaw(emio, 2000);
                if (string.IsNullOrEmpty(resp)) return false; // 응답 없으면 무시
                // pii014000000000X0000 → X 위치의 비트: bit0=IN1, bit1=IN2, bit2=IN3
                // 03=IN1+IN2, 01=IN1만, 02=IN2만, 00=없음
                // IN2 감지(bit1=1) = 과회전 위치에 도달
                int ioValue = ParsePiiInputBits(resp);
                bool in2Detected = (ioValue & 0x02) != 0;
                if (in2Detected)
                {
                    log($"[LIMIT] 과회전 감지! IN2 활성 (PII00={resp}, IO=0x{ioValue:X2})");
                    return true;
                }
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[LIMIT] PII00 정상 (IO=0x{ioValue:X2})");
                return false;
            }
            catch { return false; }
        }

        private int ParsePiiInputBits(string piiResp)
        {
            // pii01400000000030000 → "03" 부분 추출 (13~14번째 문자)
            // pii014 + 00000000 + XX + 0000
            if (piiResp.Length >= 15 && piiResp.StartsWith("pii"))
            {
                string hex = piiResp.Substring(13, 2);
                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int val))
                    return val;
            }
            return 0;
        }

        private string EmioRecvRaw(IComm emio, int timeoutMs)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remain <= 0) break;
                var resp = emio.Recv(new Dictionary<string, object?> { { "timeout", Math.Min(remain, 500) } });
                if (resp == null || !resp.TryGetValue("value", out var vObj)) continue;
                string v = vObj?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(v) && v.StartsWith("pii"))
                    return v;
            }
            return "";
        }

        // 영점 완료 상태 (프로세스 간 공유)
        private bool _zeroInitialized = false;
        private double _currentAngle = 0; // 현재 모터 위치 (도)
        private volatile bool _stopRequested = false; // Stop 중단 플래그

        // ══════════════════════════════════════════════
        //  에러 테이블 (error_table.json 캐시)
        // ══════════════════════════════════════════════
        private static Dictionary<string, JsonElement>? _errorTableCommon;
        private static Dictionary<string, JsonElement>? _errorTableProject;
        private static bool _errorTableLoaded = false;

        private static void LoadErrorTable()
        {
            if (_errorTableLoaded) return;
            _errorTableLoaded = true;
            try
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string path = Path.Combine(dir, "error_table.json");
                if (!File.Exists(path)) return;
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("common", out var common))
                {
                    _errorTableCommon = new Dictionary<string, JsonElement>();
                    foreach (var prop in common.EnumerateObject())
                        _errorTableCommon[prop.Name] = prop.Value.Clone();
                }
                if (root.TryGetProperty("project", out var project))
                {
                    _errorTableProject = new Dictionary<string, JsonElement>();
                    foreach (var prop in project.EnumerateObject())
                        _errorTableProject[prop.Name] = prop.Value.Clone();
                }
            }
            catch { /* 로딩 실패 시 무시 — 검사 중단 안 함 */ }
        }

        private static void LogErrorInfo(string errorKey, Action<string> log)
        {
            LoadErrorTable();
            JsonElement entry = default;
            bool found = false;
            if (_errorTableCommon != null && _errorTableCommon.TryGetValue(errorKey, out entry))
                found = true;
            else if (_errorTableProject != null && _errorTableProject.TryGetValue(errorKey, out entry))
                found = true;
            if (!found) return;

            string msg = entry.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
            string chk = entry.TryGetProperty("check", out var c) ? c.GetString() ?? "" : "";
            log($"[ERROR_TABLE] {errorKey}: {msg}");
            if (!string.IsNullOrEmpty(chk))
            {
                foreach (var line in chk.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        log($"[ERROR_TABLE] {trimmed}");
                }
            }
        }

        // ══════════════════════════════════════════════
        //  공개 메서드 — 신규 사양서 항목별
        // ══════════════════════════════════════════════

        // ══════════════════════════════════════════════
        //  통신검사: X축 + Y축 VER + 자이로 get---x
        // ══════════════════════════════════════════════

        public IDictionary<string, object?> COMM_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => CommCheckInternal(libraries, param, logAction, false, "COMM_232", "통신검사 RS-232");

        public IDictionary<string, object?> COMM_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => CommCheckInternal(libraries, param, logAction, true, "COMM_485", "통신검사 RS-485");

        private IDictionary<string, object?> CommCheckInternal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            bool isRs485, string procId, string procName)
        {
            var log = logAction;
            log($"[COMM] === {procName} 시작 ===");

            string vlKey = isRs485 ? "uart_485" : "uart_232";
            string vlKeyY = isRs485 ? "uart_485_y" : "uart_232_y";
            string verCmd = isRs485 ? "<VER,1>" : "<VER>";

            var details = new List<Dictionary<string, object?>>();
            bool allOk = true;

            // 이전 회차 스트리밍 잔류 대비: 양쪽 포트 STOP + 버퍼 클리어
            string stopCmd = isRs485 ? "<STOP,1>" : "<STOP>";
            foreach (var portKey in new[] { vlKey, vlKeyY })
            {
                var port = ResolveComm(libraries, portKey, log);
                if (port == null) continue;
                try
                {
                    for (int i = 0; i < 3; i++)
                        port.Send(new Dictionary<string, object?> { { "body", stopCmd } });
                    Thread.Sleep(50);
                    for (int i = 0; i < 10; i++)
                    {
                        var resp = port.Recv(new Dictionary<string, object?> { { "timeout", 50 }, { "recv_mode", "line" } });
                        if (resp == null) break;
                        if (!resp.TryGetValue("value", out var vo)) break;
                        if (string.IsNullOrWhiteSpace(vo?.ToString())) break;
                    }
                }
                catch { /* ignore */ }
            }

            // 1) X축 VL 통신검사
            var vlX = ResolveComm(libraries, vlKey, log);
            if (vlX != null)
            {
                bool xOk = CheckVlComm(vlX, verCmd, "X축", log);
                details.Add(new Dictionary<string, object?> { { "item", "X축 VL" }, { "result", xOk ? "OK" : "NG" } });
                if (!xOk) allOk = false;
            }
            else
            {
                log($"[COMM] X축 uart ({vlKey}) 없음");
                details.Add(new Dictionary<string, object?> { { "item", "X축 VL" }, { "result", "NG" } });
                allOk = false;
            }

            // 2) Y축 VL 통신검사 (싱글모드면 스킵)
            string testMode = GetParamString(param, "test_mode", "dual").ToLower();
            if (testMode == "single")
            {
                log($"[COMM] Y축: 싱글모드 — 스킵");
                details.Add(new Dictionary<string, object?> { { "item", "Y축 VL" }, { "result", "SKIP" } });
            }
            else
            {
                var vlY = ResolveComm(libraries, vlKeyY, log);
                if (vlY != null)
                {
                    bool yOk = CheckVlComm(vlY, verCmd, "Y축", log);
                    details.Add(new Dictionary<string, object?> { { "item", "Y축 VL" }, { "result", yOk ? "OK" : "NG" } });
                    if (!yOk) allOk = false;
                }
                else
                {
                    log($"[COMM] Y축 uart ({vlKeyY}) 없음");
                    details.Add(new Dictionary<string, object?> { { "item", "Y축 VL" }, { "result", "NG" } });
                    allOk = false;
                }
            }

            // 3) 자이로 통신검사
            var gyro = ResolveComm(libraries, "uart_gyro", log);
            if (gyro != null)
            {
                bool gOk = GyroGetAngle(gyro, log, out double angle);
                log($"[COMM] 자이로: {(gOk ? $"OK ({angle:F2}도)" : "NG (응답 없음)")}");
                details.Add(new Dictionary<string, object?> { { "item", "자이로 통신검사" }, { "value", gOk ? angle : null }, { "result", gOk ? "OK" : "NG" } });
                if (!gOk) allOk = false;
            }
            else
            {
                log("[COMM] 자이로 uart (uart_gyro) 없음");
                details.Add(new Dictionary<string, object?> { { "item", "자이로 통신검사" }, { "result", "NG" } });
                allOk = false;
            }

            if (!allOk)
            {
                var failItems = details.Where(d => d.TryGetValue("result", out var r) && r?.ToString() == "NG")
                                       .Select(d => d.TryGetValue("item", out var i) ? i?.ToString() : "?");
                log($"[COMM] FAIL 원인: {string.Join(", ", failItems)} 통신 실패");
                // FAIL 요약 시점에 에러 테이블 재출력 (작업자 알림)
                foreach (var d in details)
                {
                    if (d.TryGetValue("result", out var r) && r?.ToString() == "NG" && d.TryGetValue("item", out var item))
                    {
                        string errKey = item?.ToString()?.Contains("자이로") == true ? "COMM_GYRO" : "COMM_UART";
                        LogErrorInfo(errKey, log);
                    }
                }
            }
            log($"[COMM] === {procName} {(allOk ? "PASS" : "FAIL")} ===");
            return MakeResult(allOk, procId, procName, details, allOk ? "PASS" : "FAIL");
        }

        private bool CheckVlComm(IComm uart, string verCmd, string label, Action<string> log)
        {
            try
            {
                uart.Send(new Dictionary<string, object?> { { "body", verCmd } });
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[COMM] {label} TX: {verCmd}");
            }
            catch (Exception ex)
            {
                log($"[COMM] {label} Send 실패: {ex.Message}");
                return false;
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(3000);
            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                var resp = uart.Recv(new Dictionary<string, object?>
                {
                    { "timeout", Math.Min(remain, 500) },
                    { "recv_mode", "line" }
                });
                if (resp == null) continue;
                if (!resp.TryGetValue("value", out var vo)) continue;
                string v = vo?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(v)) continue;
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[COMM] {label} RX: {v}");
                if (v.Contains("VER"))
                {
                    log($"[COMM] {label}: OK");
                    return true;
                }
            }
            log($"[COMM] {label}: NG (응답 없음)");
            return false;
        }

        // ══════════════════════════════════════════════
        //  영점 초기화
        // ══════════════════════════════════════════════

        public IDictionary<string, object?> MOTION_ZERO_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionZeroInternal(libraries, param, logAction, false, "MOTION_ZERO_232", "영점 초기화 RS-232");

        public IDictionary<string, object?> MOTION_ZERO_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionZeroInternal(libraries, param, logAction, true, "MOTION_ZERO_485", "영점 초기화 RS-485");

        public IDictionary<string, object?> MOTION_TILT_X_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, false, "MOTION_TILT_X_232", "#4 X축 기울기 RS-232");

        public IDictionary<string, object?> MOTION_TILT_X_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, true, "MOTION_TILT_X_485", "#4 X축 기울기 RS-485");

        public IDictionary<string, object?> MOTION_VIBE_Y_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, false, "MOTION_VIBE_Y_232", "#5 Y축 진동 RS-232");

        public IDictionary<string, object?> MOTION_VIBE_Y_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, true, "MOTION_VIBE_Y_485", "#5 Y축 진동 RS-485");

        public IDictionary<string, object?> MOTION_TILT_Y_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, false, "MOTION_TILT_Y_232", "#6 Y축 기울기 RS-232");

        public IDictionary<string, object?> MOTION_TILT_Y_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, true, "MOTION_TILT_Y_485", "#6 Y축 기울기 RS-485");

        public IDictionary<string, object?> MOTION_VIBE_X_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, false, "MOTION_VIBE_X_232", "#7 X축 진동 RS-232");

        public IDictionary<string, object?> MOTION_VIBE_X_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, true, "MOTION_VIBE_X_485", "#7 X축 진동 RS-485");

        public IDictionary<string, object?> MOTION_VIBE_Z_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, false, "MOTION_VIBE_Z_232", "#8 Z축 진동 RS-232");

        public IDictionary<string, object?> MOTION_VIBE_Z_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MotionNewInternal(libraries, param, logAction, true, "MOTION_VIBE_Z_485", "#8 Z축 진동 RS-485");

        // ══════════════════════════════════════════════
        //  기존 호환 메서드 (보드레벨 모션검사용 — 당분간 유지)
        // ══════════════════════════════════════════════

        public IDictionary<string, object?> MOTION_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MakeResult(false, "MOTION_232", "모션지그 검사 RS-232", null, "SKIP: 신규 프로세스 사용 권장");

        public IDictionary<string, object?> MOTION_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MakeResult(false, "MOTION_485", "모션지그 검사 RS-485", null, "SKIP: 신규 프로세스 사용 권장");

        public IDictionary<string, object?> MOTION_VIBE_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MakeResult(false, "MOTION_VIBE_232", "모션지그 진동검사 RS-232", null, "SKIP: 신규 프로세스 사용 권장");

        public IDictionary<string, object?> MOTION_TILT_232(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MakeResult(false, "MOTION_TILT_232", "모션지그 기울기검사 RS-232", null, "SKIP: 신규 프로세스 사용 권장");

        public IDictionary<string, object?> MOTION_VIBE_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MakeResult(false, "MOTION_VIBE_485", "모션지그 진동검사 RS-485", null, "SKIP: 신규 프로세스 사용 권장");

        public IDictionary<string, object?> MOTION_TILT_485(IDictionary<string, object?> libraries, IDictionary<string, object?> param, Action<string> logAction)
            => MakeResult(false, "MOTION_TILT_485", "모션지그 기울기검사 RS-485", null, "SKIP: 신규 프로세스 사용 권장");

        // ══════════════════════════════════════════════
        //  신규 내부 진입점
        // ══════════════════════════════════════════════

        private IDictionary<string, object?> MotionNewInternal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            bool isRs485,
            string procId,
            string procName)
        {
            if (Cantops.FlexFab.LogConfig.IsTest) logAction($"[MotionJig] === {procId} 시작 ===");
            _stopRequested = false;
            LoadParams(param);

            // EMIO TCP
            var emio = ResolveComm(libraries, "tcp_emio", logAction);
            if (emio == null)
            {
                LogErrorInfo("COMM_TCP", logAction);
                return MakeResult(false, procId, procName, null, "FAIL: tcp_emio not found");
            }

            // 과회전 체크 (IN2 폴링)
            if (CheckOverRotation(emio, logAction))
            {
                LogErrorInfo("OVERROTATION", logAction);
                return MakeResult(false, procId, procName, null, "FAIL: 과회전 감지 (IN2)");
            }

            // VL 센서 (232 or 485, param의 uart_id로 오버라이드 가능)
            string vlKey = isRs485 ? "uart_485" : "uart_232";
            if (param.TryGetValue("uart_id", out var uartIdObj) && uartIdObj != null)
                vlKey = uartIdObj.ToString();
            var vl = ResolveComm(libraries, vlKey, logAction);
            if (vl == null)
            {
                LogErrorInfo("COMM_UART", logAction);
                return MakeResult(false, procId, procName, null, $"FAIL: {vlKey} not found");
            }

            // 자이로 센서 (SOLAR-360-420)
            var gyro = ResolveComm(libraries, "uart_gyro", logAction);
            if (gyro == null)
            {
                LogErrorInfo("COMM_GYRO", logAction);
                return MakeResult(false, procId, procName, null, "FAIL: uart_gyro not found");
            }

            // EMIO TCP 연결 확인
            try
            {
                var st = emio.GetStatus();
                bool connected = st != null && st.TryGetValue("Connected", out var cv) && cv is bool b && b;
                if (!connected)
                {
                    logAction("[MotionJig] EMIO 연결 시도...");
                    emio.GetType().GetMethod("Connect")?.Invoke(emio, null);
                }
            }
            catch (Exception ex) { logAction($"[MotionJig] EMIO 연결 확인 오류: {ex.Message}"); }

            // 작업자 팝업 (operator_prompt)
            string prompt = GetParamString(param, "operator_prompt", "");
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                logAction($"[MotionJig] 작업자 확인 대기: {prompt}");
                if (libraries.TryGetValue("confirmCallback", out var cbObj) && cbObj is Func<string, bool> confirmCb)
                {
                    if (!confirmCb(prompt))
                    {
                        logAction("[MotionJig] 작업자가 취소했습니다.");
                        return MakeResult(false, procId, procName, null, "작업자 취소");
                    }
                }
            }

            // 영점 초기화 (최초 1회)
            if (!_zeroInitialized)
            {
                string gyroDir = GetParamString(param, "gyro_dir", "setdir6");
                if (!InitMotionZero(emio, gyro, gyroDir, param, logAction, out double jigRPre, out bool jigHomed))
                    return MakeResult(false, procId, procName, null, "FAIL: 영점 초기화 실패");
                _zeroInitialized = true;
                _currentAngle = 0;
                if (!JigHealthCheck(libraries, param, gyroDir, jigRPre, jigHomed, logAction, out _))
                    return MakeResult(false, procId, procName, null, "FAIL: 지그점검 취소");
            }
            else
            {
                // C3 안전: 영점 스킵(이전 사이클서 완료) 시에도 soft limit을 운용값으로 보장.
                // 측정 종료 시 ±8000으로 풀어두므로(다음 영점 PMO011용), 스킵하면 ±8000 방치 → FINE 발산 시 케이블 감김.
                int softP = GetParamInt(param, "soft_limit_plus", 1600);
                int softM = GetParamInt(param, "soft_limit_minus", -1600);
                EmioSend(emio, $"PMS07128{softP}", logAction);
                EmioRecvUntil(emio, "pms", 3000, logAction);
                EmioSend(emio, $"PMS08129{softM}", logAction);
                EmioRecvUntil(emio, "pms", 3000, logAction);
                if (Cantops.FlexFab.LogConfig.IsTest) logAction($"[SAFE] 영점 스킵 — soft limit 운용값 보장 ({softP}/{softM})");
            }

            // measure_type에 따라 분기
            string measureType = GetParamString(param, "measure_type", "");
            bool pass;
            List<Dictionary<string, object?>> details;

            if (measureType == "tilt")
                pass = RunTiltMeasure(emio, vl, gyro, param, isRs485, logAction, out details);
            else if (measureType == "vibe")
                pass = RunVibeMeasure(emio, vl, gyro, param, isRs485, logAction, out details);
            else
            {
                details = new List<Dictionary<string, object?>>();
                logAction($"[MotionJig] 알 수 없는 measure_type: {measureType}");
                return MakeResult(false, procId, procName, null, $"FAIL: unknown measure_type '{measureType}'");
            }

            // rollback mode 0
            if (GetParamBool(param, "rollback_mode0", false))
            {
                if (Cantops.FlexFab.LogConfig.IsTest) logAction("[MotionJig] MODE 0 rollback");
                StopVlStreaming(vl, isRs485, logAction);
            }

            // FAIL 시 영점 상태 리셋 (다음 실행 시 원점부터 시작)
            if (!pass)
                _zeroInitialized = false;

            // 검사 완료 후 0도 복귀 (수평)
            if (Math.Abs(_currentAngle) > 5)
            {
                string moveAxis = GetParamString(param, "move_axis", "X");
                int returnSteps = AngleToSteps(0 - _currentAngle, moveAxis);
                MoveSteps(emio, returnSteps, logAction);
                _currentAngle = 0;
                logAction($"[EMIO] 수평 복귀: {returnSteps} step 이동");
            }

            // 소프트 리밋 해제 (다음 영점 PMO011 방해 안 되게)
            EmioSend(emio, "PMS071288000", logAction);
            EmioRecvUntil(emio, "pms", 3000, logAction);
            EmioSend(emio, "PMS08129-8000", logAction);
            EmioRecvUntil(emio, "pms", 3000, logAction);

            string status = pass ? "PASS" : "FAIL";
            if (Cantops.FlexFab.LogConfig.IsTest) logAction($"[MotionJig] === {procId} {status} ===");
            return MakeResult(pass, procId, procName, details, status);
        }

        // ══════════════════════════════════════════════
        //  영점 초기화 (독립 검사항목)
        // ══════════════════════════════════════════════

        private IDictionary<string, object?> MotionZeroInternal(
            IDictionary<string, object?> libraries, IDictionary<string, object?> param,
            Action<string> logAction, bool isRs485, string procId, string procName)
        {
            if (Cantops.FlexFab.LogConfig.IsTest) logAction($"[MotionJig] === {procId} 시작 ===");
            _stopRequested = false;
            LoadParams(param);
            double _testStartAngle = GetParamDouble(param, "test_start_angle", 0);
            if (Math.Abs(_testStartAngle) > 0.01)
                logAction($"[MotionJig] 테스트 초기각도: {_testStartAngle}도");

            var emio = ResolveComm(libraries, "tcp_emio", logAction);
            if (emio == null)
            {
                LogErrorInfo("COMM_TCP", logAction);
                return MakeResult(false, procId, procName, null, "FAIL: tcp_emio not found");
            }

            var gyro = ResolveComm(libraries, "uart_gyro", logAction);
            if (gyro == null)
            {
                LogErrorInfo("COMM_GYRO", logAction);
                return MakeResult(false, procId, procName, null, "FAIL: uart_gyro not found");
            }

            // 과회전 체크 (IN2 폴링)
            if (CheckOverRotation(emio, logAction))
            {
                LogErrorInfo("OVERROTATION", logAction);
                return MakeResult(false, procId, procName, null, "FAIL: 과회전 감지 (IN2)");
            }

            // 테스트용: 영점 전에 지정 각도로 이동 (±180도 클램핑: 자이로 wrap 방지)
            double testStartAngle = GetParamDouble(param, "test_start_angle", 0);
            if (Math.Abs(testStartAngle) > 180.0)
            {
                logAction($"[MotionJig] 경고: test_start_angle {testStartAngle}도 → ±180도로 클램핑");
                testStartAngle = Math.Sign(testStartAngle) * 180.0;
            }
            if (Math.Abs(testStartAngle) > 0.01)
            {
                // 테스트 이동 전 에러클리어 + 소프트리밋 확장
                EmioSend(emio, "PPC00", logAction);
                EmioRecvUntil(emio, "ppc00", 3000, logAction);
                EmioSend(emio, "PMS071288000", logAction);
                EmioRecvUntil(emio, "pms", 3000, logAction);
                EmioSend(emio, "PMS08129-8000", logAction);
                EmioRecvUntil(emio, "pms", 3000, logAction);

                logAction($"[MotionJig] 테스트: {testStartAngle}도로 이동 후 영점 시작");
                int steps = (int)Math.Round(testStartAngle / _stepResolution);
                MoveSteps(emio, steps, logAction);
                Thread.Sleep(_testStartWait);
            }

            // 작업자 팝업 (operator_prompt) — 반복 2회차부터는 자동 진행
            int repeatRound = 0;
            try { repeatRound = Convert.ToInt32(((IDictionary<string, object>)param).TryGetValue("__repeat_round", out var _rr) ? _rr : 0); } catch { }

            string prompt = GetParamString(param, "operator_prompt", "");
            if (!string.IsNullOrWhiteSpace(prompt) && repeatRound == 0)
            {
                logAction($"[ZERO] 작업자 확인 대기: {prompt}");
                if (libraries.TryGetValue("confirmCallback", out var cbObj) && cbObj is Func<string, bool> confirmCb)
                {
                    if (!confirmCb(prompt))
                    {
                        logAction("[ZERO] 작업자가 취소했습니다.");
                        return MakeResult(false, procId, procName, null, "작업자 취소");
                    }
                }
            }

            string gyroDir = GetParamString(param, "gyro_dir", "setdir6");
            bool pass = InitMotionZero(emio, gyro, gyroDir, param, logAction, out double jigRPre, out bool jigHomed);
            if (pass)
            {
                _zeroInitialized = true;
                _currentAngle = 0;
            }
            else
            {
                _zeroInitialized = false;
            }

            Dictionary<string, object?>? jigDetail = null;
            bool jigOk = !pass || JigHealthCheck(libraries, param, gyroDir, jigRPre, jigHomed, logAction, out jigDetail);
            var details = jigDetail != null ? new List<Dictionary<string, object?>> { jigDetail } : null;
            if (pass && !jigOk)
                return MakeResult(false, procId, procName, details, "FAIL: 지그점검 취소");

            string status = pass ? "PASS" : "FAIL";
            if (Cantops.FlexFab.LogConfig.IsTest) logAction($"[MotionJig] === {procId} {status} ===");
            return MakeResult(pass, procId, procName, details, status);
        }

        // ══════════════════════════════════════════════
        //  영점 초기화 (내부 로직)
        // ══════════════════════════════════════════════

        private static int _lastHomeSensor = 1; // 마지막 사용한 홈 센서 (1 or 2, 교대용)
        private static int _motionZeroCount = 0; // X영점 호출 횟수 (home_every_n: 최초1회/N회마다 홈복귀용)

        // ── 지그 수평 감시 (정기점검) ──
        // setzcur 제거(R1b1)로 경사계 0점이 공장(중력) 기준에 고정 → 홈 수행 영점 사이클의
        // FineAdjust 진입 첫 판독(r_pre)이 기구 자세의 수평 오차. baseline 대비 변화량(dev)으로
        // 지그 틀어짐(볼트 풀림·홈센서 마모·경사계 장착 이동)을 조기 감지.
        // 홈 생략 사이클은 r_pre가 이전 위치 잔차라 판정 제외. 스펙은 전부 JSON(#3 param).
        private static int _jigNgStreak = 0;        // NG 연속 카운터 (홈 수행 사이클만 증감, 재시작 시 리셋)
        private static bool _jigAlertShown = false; // 팝업은 프로그램 실행당 1회 (로그·카운트는 계속)

        private bool JigHealthCheck(IDictionary<string, object?> libraries,
            IDictionary<string, object?> param, string gyroDir,
            double rPre, bool homed, Action<string> log,
            out Dictionary<string, object?>? detail)
        {
            detail = null; // 결과 JSON(details) 기록용 — jig_health_check=false면 미기록 (R1b3)
            if (!GetParamBool(param, "jig_health_check", false)) return true;
            string axis = gyroDir.Contains("5") ? "Y" : "X";
            if (!homed)
            {
                detail = new Dictionary<string, object?> { { "jig_zero_axis", axis }, { "jig_homed", false } };
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[JIG] 기구영점 {axis}: 홈 생략 사이클 — 판정 생략");
                return true;
            }
            if (double.IsNaN(rPre))
            {
                detail = new Dictionary<string, object?> { { "jig_zero_axis", axis }, { "jig_homed", true }, { "jig_zero_result", "NO_READ" } };
                log($"[JIG] 기구영점 {axis}: 판독 없음 — 판정 생략");
                return true;
            }

            double baseline = GetParamDouble(param, "jig_health_baseline", 0.0);
            double warn = GetParamDouble(param, "jig_health_warn", 0.10);
            double ng = GetParamDouble(param, "jig_health_ng", 0.25);
            double absLimit = GetParamDouble(param, "jig_health_abs_limit", 1.0);
            int streakN = GetParamInt(param, "jig_health_ng_streak", 2);

            double dev = Math.Abs(rPre - baseline);
            bool absHit = Math.Abs(rPre) > absLimit; // baseline 오염/센서 탈락 가드 → streak 무관 즉시
            string grade = (absHit || dev > ng) ? "NG" : (dev > warn) ? "WARN" : "OK";
            _jigNgStreak = (grade == "NG") ? _jigNgStreak + 1 : 0;
            log($"[JIG] 기구영점 {axis}: read={rPre:+0.000;-0.000} base={baseline:+0.000;-0.000} dev={dev:0.000} (경고 {warn:0.00} / NG {ng:0.00}) {grade}");

            detail = new Dictionary<string, object?>
            {
                { "jig_zero_axis", axis },
                { "jig_homed", true },
                { "jig_zero_read", Math.Round(rPre, 3) },
                { "jig_zero_base", baseline },
                { "jig_zero_dev", Math.Round(dev, 3) },
                { "jig_zero_result", grade },
                { "jig_ng_streak", _jigNgStreak },
                { "_comment", "지그 수평감시: 홈+1250스텝 도착각(미세조정 전) vs baseline. 판정 불개입" }
            };

            if (!(absHit || _jigNgStreak >= streakN) || _jigAlertShown) return true;
            if (!(libraries.TryGetValue("confirmCallback", out var cbObj) && cbObj is Func<string, bool> confirmCb))
                return true; // 팝업 훅 없는 호스트면 로그만

            _jigAlertShown = true;
            detail["jig_check_prompted"] = true;
            string msg = $"지그 수평이 점검 스펙을 벗어났습니다. ({axis}축)\n\n" +
                         $"측정: {rPre:+0.000;-0.000}도 / 기준선: {baseline:+0.000;-0.000}도\n" +
                         $"변화량: {dev:0.000}도 (스펙: 경고 {warn:0.00} / NG {ng:0.00}, 연속 {streakN}회)\n\n" +
                         "[점검] 1.지그 고정볼트 2.홈센서 위치/오염 3.경사계 장착\n" +
                         "점검 후 확인. 취소 시 검사 중단.\n" +
                         "점검 조치 후 프로그램을 껐다 켜면 즉시 재판정됩니다.";
            if (!confirmCb(msg))
            {
                detail["jig_check_result"] = "cancelled";
                log($"[JIG] 작업자 취소 — 지그 점검 필요 (dev={dev:0.000})");
                return false;
            }
            detail["jig_check_result"] = "confirmed";
            log($"[JIG] 작업자 점검 확인 (dev={dev:0.000})");
            return true;
        }

        private bool InitMotionZero(IComm emio, IComm gyro, string gyroDir,
            IDictionary<string, object?> param, Action<string> log,
            out double jigRPre, out bool jigHomed)
        {
            jigRPre = double.NaN;  // FineAdjust 진입 첫 판독 (r_pre). 미도달/실패 시 NaN
            jigHomed = false;      // 홈센서+1250step 실제 수행 여부 (지그 수평감시 게이트)
            log("[ZERO] 영점 초기화 시작");
            _stopRequested = false;
            EmioFlush(emio, log);

            // skip_home: 홈 센서 생략 → 자이로 FineAdjust만으로 영점 (VIBE 복귀 후 ~0도 근처일 때)
            bool skipHome = GetParamBool(param, "skip_home", false);

            // home_every_n: 최초1회 + N회마다만 홈복귀, 나머지는 자동 skip (시간단축). 1=매번(기존동작)
            int homeEveryN = GetParamInt(param, "home_every_n", 1);
            double homeSkipMaxDeg = GetParamDouble(param, "home_skip_max_deg", 7.0);
            bool autoSkip = false;
            if (!skipHome && homeEveryN != 1)
            {
                _motionZeroCount++;
                bool mustHome = (_motionZeroCount == 1)                                   // 최초 1회 필수(원판 위치 미지)
                             || (homeEveryN > 1 && (_motionZeroCount - 1) % homeEveryN == 0); // N회마다 1회
                autoSkip = !mustHome;
            }

            // 1. 에러 클리어
            EmioSend(emio, "PPC00", log);
            EmioRecvUntil(emio, "ppc00", 3000, log);

            // 1.1 소프트 리밋 넓게 (PMO011 방해 안 되게)
            EmioSend(emio, "PMS071288000", log);
            EmioRecvUntil(emio, "pms", 3000, log);
            EmioSend(emio, "PMS08129-8000", log);
            EmioRecvUntil(emio, "pms", 3000, log);

            // 자이로 방향 설정
            if (!GyroSendCmd(gyro, gyroDir, log))
                log($"[ZERO] 자이로 방향 설정 실패, 계속 진행");

            // 자동 skip 안전장치: 0도에서 멀면 강제 홈복귀 (FineAdjust 도달 실패 방지)
            if (autoSkip)
            {
                if (GyroGetAngle(gyro, log, out double curAngle) && Math.Abs(curAngle) > homeSkipMaxDeg)
                {
                    log($"[ZERO] 0도 이탈({curAngle:F1}도 > {homeSkipMaxDeg:F0}) → 안전 홈복귀");
                    autoSkip = false;
                }
                else
                    log($"[ZERO] 홈복귀 생략 (N회마다 1회, count={_motionZeroCount}) → FineAdjust");
            }

            if (skipHome || autoSkip)
            {
                // 홈 생략 모드: 현재 위치에서 바로 FineAdjust
                if (skipHome) log("[ZERO] 홈 센서 생략 (skip_home=true) → FineAdjust만 수행");
                Thread.Sleep(_zeroSettleWait);
                goto SkipToFineAdjust;
            }
            jigHomed = true; // 이 아래 경로는 홈센서+1250step을 실제 수행

            // 1.5 정지 토크 설정 (0.07A × 14 = 0.98A ≈ 1A)
            EmioSend(emio, "PMS050614", log);
            EmioRecvUntil(emio, "pms", 3000, log);

            // 1.6 PMO011 전 수평(0도) 복귀 — 0도에서 PMO011이 +방향 ~132도만 돌면 홈 도달 (케이블 꼬임 없음)
            if (GyroGetAngle(gyro, log, out double preAngle) && Math.Abs(preAngle) > 20)
            {
                string preAxis = gyroDir.Contains("5") ? "Y" : "X";
                int preSteps = AngleToSteps(-preAngle * 0.8, preAxis);
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[ZERO] 수평 복귀: 현재 {preAngle:F1}도 → {preSteps} step 이동");
                MoveSteps(emio, preSteps, log);
                Thread.Sleep(_levelReturnWait);
            }

            // 2. 홈 센서 선택 (home_sensor param: "in1", "in2", "auto")
            string homeSensorSetting = GetParamString(param, "home_sensor", "in1");
            int stepsToZeroIn2 = GetParamInt(param, "steps_to_zero_in2", 400);
            int selectedSensor = 1;

            if (homeSensorSetting == "in2")
            {
                selectedSensor = 2;
            }
            else if (homeSensorSetting == "auto")
            {
                // 교대 방식: 이전과 반대 센서 선택
                selectedSensor = (_lastHomeSensor == 1) ? 2 : 1;
            }

            string homeCmd = (selectedSensor == 2) ? "PMO012" : "PMO011";
            string homeResp = (selectedSensor == 2) ? "pmo0012" : "pmo0011";
            int stepsToZero = (selectedSensor == 2) ? stepsToZeroIn2 : _stepsToZero;
            _lastHomeSensor = selectedSensor;
            log($"[ZERO] 홈 센서: IN{selectedSensor} ({homeCmd}), 0도까지 {stepsToZero} step");

            // 원점복귀(홈) — 센서 홈(notch) 위에 있으면 ACK4 발생하므로 반대 방향으로 벗어남
            int escapeDir = (selectedSensor == 2) ? 1 : -1; // IN1: -방향, IN2: +방향
            MoveSteps(emio, escapeDir * 200, log);
            Thread.Sleep(_homePreWait);
            EmioSend(emio, "PPC00", log);
            EmioRecvUntil(emio, "ppc00", 3000, log);
            bool homeFound = false;
            for (int retry = 0; retry < 5; retry++)
            {
                EmioSend(emio, homeCmd, log);
                if (EmioRecvUntil(emio, homeResp, _homeTimeout, log))
                {
                    homeFound = true;
                    break;
                }
                // ACK4 (notch 위) → 50 step 추가 이동 후 재시도
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[ZERO] {homeCmd} ACK4 재시도 {retry + 1}/5: {escapeDir * 50} step 추가 이동");
                MoveSteps(emio, escapeDir * 50, log);
                EmioSend(emio, "PPC00", log);
                EmioRecvUntil(emio, "ppc00", 3000, log);
            }
            if (!homeFound)
            {
                log($"[ZERO] FAIL: {homeCmd} timeout (5회 재시도 실패)");
                LogErrorInfo("ZERO_HOME_FAIL", log);
                return false;
            }
            // 원점복귀 완료 대기: 자이로 폴링으로 모터 정지 감지
            {
                double prevAngle = double.NaN;
                int stableCount = 0;
                var waitDeadline = DateTime.UtcNow.AddMilliseconds(20000); // 최대 20초
                while (DateTime.UtcNow < waitDeadline)
                {
                    Thread.Sleep(_homePollInterval);
                    if (GyroGetAngle(gyro, _ => { }, out double curAngle))
                    {
                        if (Cantops.FlexFab.LogConfig.IsTest) log($"[ZERO] 홈 이동 중: {curAngle:F1}도");
                        if (!double.IsNaN(prevAngle) && Math.Abs(curAngle - prevAngle) < 0.5)
                        {
                            stableCount++;
                            if (stableCount >= 2) break; // 연속 2회 안정 → 정지 판정
                        }
                        else
                        {
                            stableCount = 0;
                        }
                        prevAngle = curAngle;
                    }
                }
            }

            // 3. 자이로 방향 설정 (setdir6=X축, setdir5=Y축)
            if (!GyroSendCmd(gyro, gyroDir, log))
            {
                log($"[ZERO] FAIL: {gyroDir} 실패");
                LogErrorInfo("GYRO_READ", log);
                return false;
            }

            // 4. 0도 위치로 이동 (IN1: 1250 step, IN2: ~400 step)
            if (!MoveSteps(emio, stepsToZero, log))
            {
                log("[ZERO] FAIL: 0도 이동 실패");
                LogErrorInfo("ZERO_MOVE_FAIL", log);
                return false;
            }
            Thread.Sleep(_zeroSettleWait); // 안정화 대기

            SkipToFineAdjust:
            // 5. 자이로 기준으로 수평(0도) 미세조정
            // 주의: setzcur를 FineAdjust 전에 하면 자이로가 0으로 리셋되어
            //       FineAdjust가 실제 수평을 찾지 못함 (이대리 코드도 setzcur 없이 FineAdjust)
            if (!FineAdjust(emio, gyro, 0, "X", 250, 0.03, 0.05, out jigRPre, log))
            {
                log("[ZERO] FAIL: 0도 미세조정 실패");
                LogErrorInfo("ZERO_FINE_FAIL", log);
                return false;
            }

            // 6. 자이로 기준점 설정 (setzcur) — 기본 미실행
            // 지그요청서에 없는 명령(BG01 이식 잔재, 2026-02-24). FineAdjust가 공장영점 기준으로
            // 수평을 만들므로 setzcur는 잔차만 비휘발 영점에 누적시킴 (τ=+0.090° 실측, 2026-07-16).
            // 제거 적용 전 센서에 setzfac(공장영점 복원) 1회 선행 필수.
            // 되돌리기: JSON #3/#10 param에 "gyro_setzcur": true
            if (GetParamBool(param, "gyro_setzcur", false))
            {
                if (!GyroSetZero(gyro, log))
                {
                    log("[ZERO] FAIL: setzcur 실패");
                    LogErrorInfo("GYRO_SETZCUR", log);
                    return false;
                }
                log("[ZERO] setzcur 실행함 (gyro_setzcur=true)");
            }
            else
            {
                log("[ZERO] setzcur 실행안함 (gyro_setzcur=false, 경사계 공장영점 기준)");
            }

            log("[ZERO] 영점 초기화 완료");

            // 소프트 리밋 설정 (±1600 = 180도: 케이블 꼬임 방지, 자이로 wrap 범위 이내)
            int softLimitPlus = GetParamInt(param, "soft_limit_plus", 1600);
            int softLimitMinus = GetParamInt(param, "soft_limit_minus", -1600);
            EmioSend(emio, $"PMS07128{softLimitPlus}", log);
            if (EmioRecvUntil(emio, "pms", 3000, log))
                log($"[ZERO] 소프트 리밋+ 설정: {softLimitPlus}");
            EmioSend(emio, $"PMS08129{softLimitMinus}", log);
            if (EmioRecvUntil(emio, "pms", 3000, log))
                log($"[ZERO] 소프트 리밋- 설정: {softLimitMinus}");

            return true;
        }

        // ══════════════════════════════════════════════
        //  기울기 측정 (#4, #6)
        // ══════════════════════════════════════════════

        private bool RunTiltMeasure(
            IComm emio, IComm vl, IComm gyro,
            IDictionary<string, object?> param,
            bool isRs485,
            Action<string> log,
            out List<Dictionary<string, object?>> details)
        {
            details = new List<Dictionary<string, object?>>();
            string axis = GetParamString(param, "axis", "X");
            int sensorDiffError = GetParamInt(param, "sensor_diff_error", _sensorDiffError);
            double strictTol = GetParamDouble(param, "fine_adjust_strict_tolerance", 0.05);
            double relaxedTol = GetParamDouble(param, "fine_adjust_relaxed_tolerance", 0.05);
            int maxSteps = GetParamInt(param, "fine_adjust_max_steps", 250);

            // angles 배열 읽기
            var angles = GetParamDoubleArray(param, "angles");
            if (angles.Length == 0)
            {
                log("[TILT] FAIL: angles 배열 없음");
                return false;
            }

            // tilt_spec 읽기
            var tiltSpec = GetParamDict(param, "tilt_spec");

            // 판정 파라미터 (루프 밖에서 1회 읽기)
            int streamDuration = GetParamInt(param, "tilt_stream_duration", 1000);
            int tailCount = GetParamInt(param, "tilt_judge_tail_count", 10);
            int retryCount = GetParamInt(param, "tilt_retry_count", 3);
            string judgeMode = GetParamString(param, "tilt_judge_mode", "all_in_spec");
            bool noiseCheck = GetParamBool(param, "tilt_noise_check", false);
            double noiseP2pLimit = GetParamDouble(param, "tilt_noise_p2p_limit", 0.2);
            int maxAttempts = 1 + retryCount;

            string[] modeNames = { "last", "average", "median", "trimmed_mean", "all_in_spec" };
            string[] modeLabels = { "last(느슨)", "average(중간)", "median(중간)", "trimmed_mean(중상)", "all_in_spec(엄격)" };
            int modeIdx = Array.IndexOf(modeNames, judgeMode);
            string modeLabel = modeIdx >= 0 ? modeLabels[modeIdx] : judgeMode;

            // 판정 설정 로그 출력
            log($"[TILT] 판정설정: mode={modeLabel}, stream={streamDuration}ms, tail={tailCount}, retry={retryCount}, noise={noiseCheck}{(noiseCheck ? $"(P2P≤{noiseP2pLimit})" : "")}");

            // 판정 설정을 결과 JSON에 추가
            details.Add(new Dictionary<string, object?>
            {
                { "item", "판정설정" },
                { "judge_mode", modeLabel },
                { "stream_duration_ms", streamDuration },
                { "tail_count", tailCount },
                { "retry_count", retryCount },
                { "noise_check", noiseCheck },
                { "noise_p2p_limit", noiseCheck ? noiseP2pLimit : null },
                { "result", "INFO" }
            });

            bool allPass = true;
            foreach (double targetAngle in angles)
            {
                if (_stopRequested) { log("[TILT] 중단 요청"); allPass = false; break; }
                log($"[TILT] === {axis}축 {targetAngle:+0;-0}도 측정 시작 ===");

                // coarse 이동
                int steps = AngleToSteps(targetAngle - _currentAngle, axis);
                if (Math.Abs(steps) > 0)
                {
                    if (!MoveSteps(emio, steps, log))
                    {
                        log($"[TILT] FAIL: {targetAngle}도 이동 실패");
                        LogErrorInfo("TILT_MOVE_FAIL", log);
                        allPass = false;
                        break;
                    }
                    // C1: coarse 이동 완료 보장 — 큰 이동(방향전환 65도≈578step)은 이동시간(중속1000pps≈step당1ms)이
                    // 고정 _tiltMoveWait(300)보다 길어, 이동 중 자이로를 읽으면 FINE이 헛오차로 출발→왕복.
                    // 이동량 비례 대기(step당 1ms + 여유)로 작은 각도는 그대로, 큰 각도만 충분히 대기.
                    int coarseSettle = Math.Max(_tiltMoveWait, Math.Abs(steps) + 200);
                    Thread.Sleep(coarseSettle);
                }
                _currentAngle = targetAngle;

                // 미세조정
                if (!FineAdjust(emio, gyro, targetAngle, axis, maxSteps, strictTol, relaxedTol, out _, log))
                {
                    log($"[TILT] FAIL: {targetAngle}도 미세조정 실패");
                    LogErrorInfo("TILT_FINE_FAIL", log);
                    allPass = false;
                    break;
                }

                // 자이로 실제 각도 읽기
                if (!GyroGetAngle(gyro, log, out double gyroAngle))
                {
                    log("[TILT] FAIL: 자이로 읽기 실패");
                    LogErrorInfo("GYRO_READ", log);
                    allPass = false;
                    break;
                }

                // SPEC 판정 파라미터
                double absAngle = Math.Abs(targetAngle);
                double tolerance = absAngle >= 40
                    ? GetParamDouble(param, "tilt_tolerance_45", 0.5)
                    : GetParamDouble(param, "tilt_tolerance_20", 0.2);
                double specMin = -tolerance, specMax = tolerance;

                bool anglePass = false;
                double vlAngle = 0;
                List<double> anSamples = new();
                double error = 0;
                double judgeValue = 0; // 판정에 사용된 대표값
                int tiltRetryUsed = 0; // summary용 리트라이 카운터

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    if (_stopRequested) break;

                    // VL 센서 AN 스트리밍 읽기 (STOP→버퍼클리어→MODE→START→스트리밍 수집→STOP)
                    StopVlStreaming(vl, isRs485, log);
                    Thread.Sleep(100);
                    VlClear(vl, log);
                    StartVlStreaming(vl, isRs485, "06", log);
                    int tiltSettleWait = GetParamInt(param, "tilt_settle_wait", _vlStreamSettle);
                    Thread.Sleep(tiltSettleWait);
                    VlClear(vl, log); // 초기 과도 데이터 버리기
                    Thread.Sleep(100);
                    string raw = VlRecv(vl, streamDuration, log);
                    if (!TryParseAnFromRaw(raw, isRs485, axis, out vlAngle, out anSamples))
                    {
                        log($"[TILT] AN 파싱 실패 (시도 {attempt}/{maxAttempts}): {raw}");
                        if (attempt == maxAttempts)
                        {
                            LogErrorInfo("TILT_AN_PARSE_FAIL", log);
                            allPass = false;
                        }
                        continue;
                    }

                    // AN 데이터 CSV 저장 (테스트용) — save_tilt_csv=true 일 때만 (기본 off=양산 누적방지)
                    if (_saveTiltCsv) SaveAnCsv(axis, targetAngle, gyroAngle, anSamples, log);

                    // 안전 체크: 자이로-VL 차이 (리트라이 불가 — 하드웨어 문제)
                    double diff = Math.Abs(gyroAngle - vlAngle);
                    if (diff >= sensorDiffError)
                    {
                        log($"[TILT] ERROR: 센서 차이 {diff:F2}도 >= {sensorDiffError}도 (자이로={gyroAngle:F2}, VL={vlAngle:F2})");
                        LogErrorInfo("TILT_SENSOR_DIFF", log);
                        details.Add(new Dictionary<string, object?> { { "item", $"센서 차이 에러 {targetAngle}도" }, { "value", Math.Round(diff, 3) }, { "result", "NG" } });
                        allPass = false;
                        break; // 센서 차이는 리트라이 무의미 — 즉시 중단
                    }

                    // 마지막 N개 샘플 추출
                    int actualTail = Math.Min(tailCount, anSamples.Count);
                    var tailSamples = anSamples.GetRange(anSamples.Count - actualTail, actualTail);

                    // 판정 모드별 처리
                    switch (judgeMode)
                    {
                        case "last":
                            // 마지막 1개 값 (BG01 방식, 가장 느슨)
                            judgeValue = tailSamples[actualTail - 1];
                            error = judgeValue - gyroAngle;
                            anglePass = error >= specMin && error <= specMax;
                            break;

                        case "average":
                            // 마지막 N개 평균 (중간, 노이즈 민감)
                            judgeValue = tailSamples.Average();
                            error = judgeValue - gyroAngle;
                            anglePass = error >= specMin && error <= specMax;
                            break;

                        case "median":
                            // 마지막 N개 중앙값 (중간, 이상치 내성)
                            var sorted = tailSamples.OrderBy(v => v).ToList();
                            judgeValue = (actualTail % 2 == 1)
                                ? sorted[actualTail / 2]
                                : (sorted[actualTail / 2 - 1] + sorted[actualTail / 2]) / 2.0;
                            error = judgeValue - gyroAngle;
                            anglePass = error >= specMin && error <= specMax;
                            break;

                        case "trimmed_mean":
                            // 상하 10% 제거 후 평균 (중상, 이상치 제거)
                            var trimSorted = tailSamples.OrderBy(v => v).ToList();
                            int trimCount = Math.Max(1, actualTail / 10); // 상하 10% (최소 1개)
                            var trimmed = trimSorted.Skip(trimCount).Take(actualTail - trimCount * 2).ToList();
                            judgeValue = trimmed.Count > 0 ? trimmed.Average() : tailSamples.Average();
                            error = judgeValue - gyroAngle;
                            anglePass = error >= specMin && error <= specMax;
                            break;

                        case "all_in_spec":
                        default:
                            // 마지막 N개 전부 SPEC 이내 (가장 엄격)
                            int outCount = 0;
                            foreach (var sv in tailSamples)
                            {
                                double e = sv - gyroAngle;
                                if (e < specMin || e > specMax) outCount++;
                            }
                            anglePass = outCount == 0;
                            judgeValue = tailSamples.Average(); // 대표값: tail N개 평균
                            error = judgeValue - gyroAngle;
                            if (!anglePass)
                                log($"[TILT] {targetAngle:+0;-0}도: tail {actualTail}개 중 {outCount}개 SPEC 벗어남 (시도 {attempt}/{maxAttempts})");
                            break;
                    }

                    // 노이즈 판정 (P2P): 활성화 시 Max-Min > limit → FAIL
                    if (anglePass && noiseCheck)
                    {
                        double p2p = tailSamples.Max() - tailSamples.Min();
                        if (p2p > noiseP2pLimit)
                        {
                            anglePass = false;
                            log($"[TILT] {targetAngle:+0;-0}도: 노이즈 P2P={p2p:F3} > {noiseP2pLimit} → NG (시도 {attempt}/{maxAttempts})");
                        }
                    }

                    if (anglePass)
                    {
                        tiltRetryUsed = attempt - 1;
                        if (attempt > 1) log($"[TILT] {targetAngle:+0;-0}도: 리트라이 {attempt - 1}회 후 PASS");
                        break;
                    }
                    else if (attempt == maxAttempts)
                    {
                        tiltRetryUsed = retryCount;
                    }
                    else if (judgeMode != "all_in_spec")
                    {
                        log($"[TILT] {targetAngle:+0;-0}도: 오차={error:F3} SPEC 벗어남 (시도 {attempt}/{maxAttempts}, {modeLabel})");
                    }
                }

                if (!anglePass) { LogErrorInfo("TILT_SPEC_OUT", log); allPass = false; break; }

                // summary 계산 (품질 추세 분석용)
                int actualTailForSummary = Math.Min(tailCount, anSamples.Count);
                var tailForSummary = anSamples.GetRange(anSamples.Count - actualTailForSummary, actualTailForSummary);
                var tiltErrors = tailForSummary.Select(v => v - gyroAngle).ToList();
                var sortedErrors = tiltErrors.OrderBy(v => v).ToList();
                double summaryMedian = (sortedErrors.Count % 2 == 1)
                    ? sortedErrors[sortedErrors.Count / 2]
                    : (sortedErrors[sortedErrors.Count / 2 - 1] + sortedErrors[sortedErrors.Count / 2]) / 2.0;
                double summaryMean = tiltErrors.Average();
                double summaryStd = Math.Sqrt(tiltErrors.Sum(e => (e - summaryMean) * (e - summaryMean)) / tiltErrors.Count);
                double summaryP2p = tiltErrors.Max() - tiltErrors.Min();

                log($"[TILT] {targetAngle:+0;-0}도: 자이로={gyroAngle:F3}, VL={judgeValue:F3}, 오차={error:F3} [{specMin}~{specMax}] {(anglePass ? "OK" : "NG")} ({modeLabel}, 샘플 {anSamples.Count}개, tail {Math.Min(tailCount, anSamples.Count)}개)");
                details.Add(new Dictionary<string, object?>
                {
                    { "item", $"{axis}축 기울기 {targetAngle:+0;-0}도" },
                    { "deviation", Math.Round(error, 3) },
                    { "_deviation_desc", $"편차 = 마지막{actualTailForSummary}개 VL센서 평균 - 자이로 기준값" },
                    { "gyro_ref", Math.Round(gyroAngle, 3) },
                    { "gyro_spec", relaxedTol },
                    { "_gyro_spec_desc", $"자이로 수렴 허용치 (목표각도 ±{relaxedTol}도)" },
                    { "vl_avg", Math.Round(judgeValue, 3) },
                    { "spec_min", specMin },
                    { "spec_max", specMax },
                    { "judge_mode", modeLabel },
                    { "_judge_mode_desc", "last=마지막1개 | average=N개평균 | median=중앙값 | trimmed_mean=상하10%제거평균 | all_in_spec=N개전부(현재)" },
                    { "result", anglePass ? "OK" : "NG" },
                    { "summary", new Dictionary<string, object?>
                        {
                            { "median", Math.Round(summaryMedian, 4) },
                            { "_median_desc", "중앙값 (tail 정렬 후 가운데)" },
                            { "standard_deviation", Math.Round(summaryStd, 4) },
                            { "_std_desc", "표준편차 (흩어짐 정도, 작을수록 안정)" },
                            { "p2p", Math.Round(summaryP2p, 4) },
                            { "_p2p_desc", "최대-최소 차이 (센서 안정성)" },
                            { "sample_count", anSamples.Count },
                            { "tail_count", actualTailForSummary },
                            { "_tail_desc", $"마지막 {actualTailForSummary}개 전부 spec 이내여야 PASS" },
                            { "retry_used", tiltRetryUsed },
                            { "_retry_desc", "리트라이 사용 횟수 (0=첫 시도 PASS)" }
                        }
                    },
                    { "_noise_comment", "noise_check=true시 tail샘플의 최대-최소(P2P)가 한계값 초과하면 FAIL (센서 불안정 검출용)" },
                    { "_comment", $"마지막{actualTailForSummary}개 전부 spec이내=PASS, AN스트리밍 {streamDuration/1000}초(~{streamDuration/10}샘플), FAIL시 {retryCount}회 재시도" }
                });
            }

            // 스트리밍 중지
            StopVlStreaming(vl, isRs485, log);

            // 기울기 측정 완료 후 0도 복귀 (케이블 꼬임 방지)
            if (Math.Abs(_currentAngle) > 5)
            {
                int returnSteps = AngleToSteps(0 - _currentAngle, axis);
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[TILT] 0도 복귀: {_currentAngle:F0}도 → {returnSteps} step");
                MoveSteps(emio, returnSteps, log);
                _currentAngle = 0;
                Thread.Sleep(_tiltReturnWait);
            }

            return allPass;
        }

        // ══════════════════════════════════════════════
        //  진동 측정 (#5, #7, #8)
        // ══════════════════════════════════════════════

        private bool RunVibeMeasure(
            IComm emio, IComm vl, IComm gyro,
            IDictionary<string, object?> param,
            bool isRs485,
            Action<string> log,
            out List<Dictionary<string, object?>> details)
        {
            details = new List<Dictionary<string, object?>>();
            string axis = GetParamString(param, "axis", "X");
            double targetAngle = GetParamDouble(param, "target_angle", 0);
            int checkIndex = GetParamInt(param, "check_axis_index", 0);
            double vibeMin = GetParamDouble(param, "vibe_min", 0.99);
            double vibeMax = GetParamDouble(param, "vibe_max", 1.01);
            double strictTol = GetParamDouble(param, "fine_adjust_strict_tolerance", 0.05);
            double relaxedTol = GetParamDouble(param, "fine_adjust_relaxed_tolerance", 0.05);
            int maxSteps = GetParamInt(param, "fine_adjust_max_steps", 250);
            int sensorDiffError = GetParamInt(param, "sensor_diff_error", _sensorDiffError);

            double preMoveAngle = GetParamDouble(param, "pre_move_angle", 0);
            string moveAxis = GetParamString(param, "move_axis", "X"); // 물리적 이동 축 (지그 기준)

            log($"[VIBE] === {axis}축 진동 측정 (목표 {targetAngle}도) ===");

            // pre_move: 먼저 자이로 확인 가능한 각도로 이동 후 FineAdjust
            if (Math.Abs(preMoveAngle) > 0.001)
            {
                log($"[VIBE] pre_move: {preMoveAngle}도로 먼저 이동 (move_axis={moveAxis})");
                int preSteps = AngleToSteps(preMoveAngle - _currentAngle, moveAxis);
                if (Math.Abs(preSteps) > 0)
                {
                    if (!MoveSteps(emio, preSteps, log))
                    {
                        log($"[VIBE] FAIL: pre_move {preMoveAngle}도 이동 실패");
                        LogErrorInfo("VIBE_MOVE_FAIL", log);
                        return false;
                    }
                    Thread.Sleep(_vibePremoveWait); // 안정화 대기
                }
                if (!FineAdjust(emio, gyro, preMoveAngle, moveAxis, maxSteps, strictTol, relaxedTol, out _, log))
                {
                    log($"[VIBE] FAIL: pre_move {preMoveAngle}도 미세조정 실패");
                    LogErrorInfo("VIBE_MOVE_FAIL", log);
                    return false;
                }
                _currentAngle = preMoveAngle;
            }

            // coarse 이동 (pre_move 위치 → 목표 각도)
            int steps = AngleToSteps(targetAngle - _currentAngle, moveAxis);
            if (Math.Abs(steps) > 0)
            {
                if (!MoveSteps(emio, steps, log))
                {
                    log($"[VIBE] FAIL: {targetAngle}도 이동 실패");
                    LogErrorInfo("VIBE_MOVE_FAIL", log);
                    return false;
                }
                Thread.Sleep(_vibeMoveWait);
            }
            _currentAngle = targetAngle;

            // 90도 근처는 자이로 FineAdjust 건너뜀 (경사계 특성)
            if (Math.Abs(targetAngle) >= 85)
            {
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[VIBE] {targetAngle}도: step 이동만 사용 (FineAdjust 건너뜀)");
            }
            else if (!FineAdjust(emio, gyro, targetAngle, axis, maxSteps, strictTol, relaxedTol, out _, log))
            {
                log($"[VIBE] FAIL: {targetAngle}도 미세조정 실패");
                LogErrorInfo("VIBE_MOVE_FAIL", log);
                return false;
            }

            // 스트리밍 판정 파라미터
            int streamDuration = GetParamInt(param, "vibe_stream_duration", 1000);
            int tailCount = GetParamInt(param, "vibe_judge_tail_count", 5);
            int retryCount = GetParamInt(param, "vibe_retry_count", 3);
            string judgeMode = GetParamString(param, "vibe_judge_mode", "all_in_spec");
            bool noiseCheck = GetParamBool(param, "vibe_noise_check", false);
            double noiseP2pLimit = GetParamDouble(param, "vibe_noise_p2p_limit", 0.02);
            int gacSettleWait = GetParamInt(param, "gac_settle_wait", 500);
            int maxAttempts = 1 + retryCount;

            string[] modeNames = { "last", "average", "median", "trimmed_mean", "all_in_spec" };
            string[] modeLabels = { "last(느슨)", "average(중간)", "median(중간)", "trimmed_mean(중상)", "all_in_spec(엄격)" };
            int modeIdx = Array.IndexOf(modeNames, judgeMode);
            string modeLabel = modeIdx >= 0 ? modeLabels[modeIdx] : judgeMode;

            string[] axisNames = { "X", "Y", "Z" };
            string checkAxisName = checkIndex < axisNames.Length ? axisNames[checkIndex] : $"idx{checkIndex}";

            log($"[VIBE] 판정설정: mode={modeLabel}, stream={streamDuration}ms, tail={tailCount}, retry={retryCount}, noise={noiseCheck}{(noiseCheck ? $"(P2P≤{noiseP2pLimit})" : "")}");

            // GAC 안정화 대기 (이동 후 센서 안정화)
            Thread.Sleep(gacSettleWait);

            bool vibePass = false;
            double judgeValue = 0;
            List<double> gacSamples = new();
            var gacAllAxis = new List<double[]>(); // 3축 전체 보존 (summary용)
            int vibeRetryUsed = 0; // summary용 리트라이 카운터

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (_stopRequested) break;

                // MODE 02 스트리밍 수집 (STOP→클리어→MODE→START→수집→STOP)
                StopVlStreaming(vl, isRs485, log);
                Thread.Sleep(100);
                VlClear(vl, log);
                StartVlStreaming(vl, isRs485, "02", log);
                Thread.Sleep(gacSettleWait); // 스트리밍 안정화 대기
                VlClear(vl, log); // 초기 과도 데이터 버리기
                Thread.Sleep(100);
                string raw = VlRecv(vl, streamDuration, log);
                StopVlStreaming(vl, isRs485, log);

                // GAC 스트리밍 파싱
                gacSamples = new List<double>();
                gacAllAxis = new List<double[]>(); // 3축 전체 보존 (summary용)
                foreach (var frame in ExtractBracketFrames(raw))
                {
                    if (!TryParseGacSample(frame, isRs485, log, out double gx, out double gy, out double gz, out double rms))
                        continue;
                    double[] vals = { gx, gy, gz };
                    double checkVal = checkIndex < vals.Length ? vals[checkIndex] : 0;
                    gacSamples.Add(checkVal);
                    gacAllAxis.Add(new double[] { gx, gy, gz });
                }

                if (gacSamples.Count == 0)
                {
                    log($"[VIBE] GAC 파싱 실패 (시도 {attempt}/{maxAttempts}): {raw?.Substring(0, Math.Min(raw?.Length ?? 0, 200))}");
                    if (attempt == maxAttempts)
                    {
                        LogErrorInfo("VIBE_GAC_PARSE_FAIL", log);
                        details.Add(new Dictionary<string, object?> { { "item", $"{axis}축 진동 파싱실패" }, { "result", "NG" } });
                        return false;
                    }
                    continue;
                }

                // 마지막 N개 샘플 추출
                int actualTail = Math.Min(tailCount, gacSamples.Count);
                var tailSamples = gacSamples.GetRange(gacSamples.Count - actualTail, actualTail);

                // 판정 모드별 처리
                switch (judgeMode)
                {
                    case "last":
                        judgeValue = tailSamples[actualTail - 1];
                        vibePass = judgeValue >= vibeMin && judgeValue <= vibeMax;
                        break;

                    case "average":
                        judgeValue = tailSamples.Average();
                        vibePass = judgeValue >= vibeMin && judgeValue <= vibeMax;
                        break;

                    case "median":
                        var sorted = tailSamples.OrderBy(v => v).ToList();
                        judgeValue = (actualTail % 2 == 1)
                            ? sorted[actualTail / 2]
                            : (sorted[actualTail / 2 - 1] + sorted[actualTail / 2]) / 2.0;
                        vibePass = judgeValue >= vibeMin && judgeValue <= vibeMax;
                        break;

                    case "trimmed_mean":
                        var trimSorted = tailSamples.OrderBy(v => v).ToList();
                        int trimCount = Math.Max(1, actualTail / 10);
                        var trimmed = trimSorted.Skip(trimCount).Take(actualTail - trimCount * 2).ToList();
                        judgeValue = trimmed.Count > 0 ? trimmed.Average() : tailSamples.Average();
                        vibePass = judgeValue >= vibeMin && judgeValue <= vibeMax;
                        break;

                    case "all_in_spec":
                    default:
                        int outCount = 0;
                        foreach (var sv in tailSamples)
                        {
                            if (sv < vibeMin || sv > vibeMax) outCount++;
                        }
                        vibePass = outCount == 0;
                        judgeValue = tailSamples[actualTail - 1];
                        if (!vibePass)
                            log($"[VIBE] {checkAxisName}축: tail {actualTail}개 중 {outCount}개 SPEC 벗어남 (시도 {attempt}/{maxAttempts})");
                        break;
                }

                // 노이즈 판정 (P2P)
                if (vibePass && noiseCheck)
                {
                    double p2p = tailSamples.Max() - tailSamples.Min();
                    if (p2p > noiseP2pLimit)
                    {
                        vibePass = false;
                        log($"[VIBE] {checkAxisName}축: 노이즈 P2P={p2p:F4} > {noiseP2pLimit} → NG (시도 {attempt}/{maxAttempts})");
                    }
                }

                if (vibePass)
                {
                    vibeRetryUsed = attempt - 1;
                    if (attempt > 1) log($"[VIBE] {checkAxisName}축: 리트라이 {attempt - 1}회 후 PASS");
                    break;
                }
                else if (attempt == maxAttempts)
                {
                    vibeRetryUsed = retryCount;
                }
                else if (judgeMode != "all_in_spec")
                {
                    log($"[VIBE] {checkAxisName}축: value={judgeValue:F4} SPEC 벗어남 (시도 {attempt}/{maxAttempts}, {modeLabel})");
                }
            }

            if (!vibePass) LogErrorInfo("VIBE_GAC_FAIL", log);

            // summary 계산 (품질 추세 분석용)
            int actualTailVibe = Math.Min(tailCount, gacSamples.Count);
            var tailForVibeSummary = gacSamples.GetRange(gacSamples.Count - actualTailVibe, actualTailVibe);
            var sortedVibe = tailForVibeSummary.OrderBy(v => v).ToList();
            double vibeMedian = (sortedVibe.Count % 2 == 1)
                ? sortedVibe[sortedVibe.Count / 2]
                : (sortedVibe[sortedVibe.Count / 2 - 1] + sortedVibe[sortedVibe.Count / 2]) / 2.0;
            double vibeMean = tailForVibeSummary.Average();
            double vibeStd = Math.Sqrt(tailForVibeSummary.Sum(v => (v - vibeMean) * (v - vibeMean)) / tailForVibeSummary.Count);
            double vibeP2p = tailForVibeSummary.Max() - tailForVibeSummary.Min();

            // norm, non_target 계산 (3축 데이터에서)
            double vibeNorm = 0;
            double nonTargetMax = 0;
            if (gacAllAxis.Count > 0)
            {
                var lastGac = gacAllAxis[gacAllAxis.Count - 1];
                vibeNorm = Math.Sqrt(lastGac[0] * lastGac[0] + lastGac[1] * lastGac[1] + lastGac[2] * lastGac[2]);
                // non_target: check_axis 이외 축의 절대값 최대
                var nonTargetVals = new List<double>();
                for (int ai = 0; ai < 3; ai++)
                    if (ai != checkIndex) nonTargetVals.Add(Math.Abs(lastGac[ai]));
                nonTargetMax = nonTargetVals.Max();
            }

            log($"[VIBE] GAC {checkAxisName}={judgeValue:F4} [{vibeMin}~{vibeMax}] {(vibePass ? "OK" : "NG")} ({modeLabel}, 샘플 {gacSamples.Count}개, tail {Math.Min(tailCount, gacSamples.Count)}개)");
            details.Add(new Dictionary<string, object?>
            {
                { "item", $"{axis}축 진동 ({targetAngle}도)" },
                { "gyro_spec", relaxedTol },
                { "_gyro_spec_desc", $"자이로 수렴 허용치 (목표각도 ±{relaxedTol}도)" },
                { "value", Math.Round(judgeValue, 2) },
                { "_value_desc", $"GAC 해당축 가속도 마지막{actualTailVibe}개 평균 (1G기준)" },
                { "spec_min", vibeMin },
                { "spec_max", vibeMax },
                { "judge_mode", modeLabel },
                { "_judge_mode_desc", "last=마지막1개 | average=N개평균 | median=중앙값 | trimmed_mean=상하10%제거평균 | all_in_spec=N개전부(현재)" },
                { "result", vibePass ? "OK" : "NG" },
                { "summary", new Dictionary<string, object?>
                    {
                        { "median", Math.Round(vibeMedian, 2) },
                        { "_median_desc", "중앙값 (tail 정렬 후 가운데)" },
                        { "standard_deviation", Math.Round(vibeStd, 4) },
                        { "_std_desc", "표준편차 (흩어짐 정도, 작을수록 안정)" },
                        { "p2p", Math.Round(vibeP2p, 4) },
                        { "_p2p_desc", "최대-최소 차이 (센서 안정성)" },
                        { "norm", Math.Round(vibeNorm, 4) },
                        { "_norm_desc", "3축 벡터크기 √(x²+y²+z²), 참고용" },
                        { "non_target_max", Math.Round(nonTargetMax, 2) },
                        { "_non_target_desc", "비대상축 최대값 (0에 가까울수록 정상)" },
                        { "sample_count", gacSamples.Count },
                        { "tail_count", actualTailVibe },
                        { "_tail_desc", $"마지막 {actualTailVibe}개 전부 spec 이내여야 PASS" },
                        { "retry_used", vibeRetryUsed },
                        { "_retry_desc", "리트라이 사용 횟수 (0=첫 시도 PASS)" }
                    }
                },
                { "_comment", $"GAC 해당축 값이 spec이내=PASS, norm/non_target은 참고용(판정무관)" }
            });
            return vibePass;
        }

        // ══════════════════════════════════════════════
        //  미세조정 (참조: FineAdjustToTargetAsync)
        // ══════════════════════════════════════════════

        private bool FineAdjust(
            IComm emio, IComm gyro,
            double targetAngle, string axis,
            int maxSteps,
            double strictTolerance, double relaxedTolerance,
            out double entryAngle,
            Action<string> log)
        {
            entryAngle = double.NaN; // 진입 판독 실패 시 NaN 유지 (지그 수평감시는 NaN이면 판정 스킵)
            bool isZero = Math.Abs(targetAngle) < 0.001;
            double strictTol = isZero ? Math.Min(strictTolerance, 0.03) : strictTolerance;
            double relaxedTol = isZero ? Math.Min(relaxedTolerance, 0.05) : relaxedTolerance;
            var strictDeadline = DateTime.UtcNow.AddSeconds(10);
            int axisSign = (axis == "X") ? -1 : 1;
            int stepDirectionFactor = 1;
            double reverseThreshold = _reverseThreshold;

            if (!GyroGetAngle(gyro, log, out double currentAngle))
            {
                log("[FINE] FAIL: 자이로 초기 읽기 실패");
                LogErrorInfo("GYRO_READ", log);
                return false;
            }
            entryAngle = currentAngle;
            if (Cantops.FlexFab.LogConfig.IsTest) log($"[FINE] 시작: target={targetAngle:F2}, current={currentAngle:F3}, 오차={Math.Abs(targetAngle - currentAngle):F3}");

            for (int i = 0; i < maxSteps; i++)
            {
                if (_stopRequested) { log("[FINE] 중단 요청"); return false; }

                double tolerance = DateTime.UtcNow <= strictDeadline ? strictTol : relaxedTol;
                double delta = targetAngle - currentAngle;

                if (Math.Abs(delta) <= tolerance)
                {
                    // 수렴 확인
                    Thread.Sleep(_fineConfirmWait);
                    if (!GyroGetAngle(gyro, log, out double confirmAngle))
                        return false;

                    if (Math.Abs(targetAngle - confirmAngle) <= tolerance)
                    {
                        log($"[FINE] 수렴 완료: target={targetAngle:F2}, actual={confirmAngle:F3}, 오차={Math.Abs(targetAngle - confirmAngle):F3}");
                        return true;
                    }
                    currentAngle = confirmAngle;
                    continue;
                }

                // 오차 크기에 비례한 step 수 계산 (큰 오차 → 빠른 이동)
                int stepsNeeded = (int)Math.Round(Math.Abs(delta) / _stepResolution);
                if (stepsNeeded < 1) stepsNeeded = 1;
                int moveSteps;
                if (Math.Abs(delta) > 1.0)
                    moveSteps = (int)(stepsNeeded * 0.8); // 80% 접근 (오버슈트 방지)
                else if (Math.Abs(delta) > 0.2)
                    moveSteps = Math.Max(1, (int)(stepsNeeded * 0.5)); // 50% 접근
                else
                    moveSteps = 1; // 0.2도 이내 미세 보정

                if (moveSteps < 1) moveSteps = 1;

                double prevAbsDelta = Math.Abs(delta);
                int step = (delta > 0 ? moveSteps : -moveSteps) * stepDirectionFactor * axisSign;
                if (!MoveSteps(emio, step, log))
                {
                    log("[FINE] 모터 이동 실패, 대기 후 재시도");
                    Thread.Sleep(_fineRetryWait);
                    if (!MoveSteps(emio, step, log))
                    {
                        log("[FINE] FAIL: 모터 이동 2회 연속 실패");
                        LogErrorInfo("MOVE_TIMEOUT", log);
                        return false;
                    }
                }

                // 안정화 대기
                int waitMs = Math.Abs(moveSteps) > 10 ? _fineStepWait : (_fineStepWait / 2);
                Thread.Sleep(waitMs);

                if (!GyroGetAngle(gyro, log, out currentAngle))
                    return false;

                double newAbsDelta = Math.Abs(targetAngle - currentAngle);
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[FINE] step {i}: moved={step}, current={currentAngle:F3}, 오차={newAbsDelta:F3}");

                // C2 발산 안전가드: 목표 대비 이탈이 한계 초과 시 즉시 중단(케이블 감김 방지).
                // 정상 기울기 FINE은 시작오차 2~3도, 방향전환 일시 오버슈트도 ~21도. 30도 초과 = 영점 미실행/탈조로 발산.
                if (newAbsDelta > FINE_DIVERGE_LIMIT)
                {
                    log($"[FINE] 발산 감지 중단: 오차 {newAbsDelta:F1}도 > {FINE_DIVERGE_LIMIT}도 (영점 미실행/탈조 의심 — 모터 보호)");
                    LogErrorInfo("FINE_DIVERGE", log);
                    return false;
                }

                // 오차 증가 시 방향 반전
                if ((newAbsDelta - prevAbsDelta) > reverseThreshold && prevAbsDelta > tolerance)
                {
                    stepDirectionFactor *= -1;
                    if (Cantops.FlexFab.LogConfig.IsTest) log($"[FINE] 오차 증가 감지, 방향 반전");
                }
            }

            log($"[FINE] maxSteps({maxSteps}) 초과, target={targetAngle:F2}, current={currentAngle:F3}");
            return false;
        }

        // ══════════════════════════════════════════════
        //  모터 이동
        // ══════════════════════════════════════════════

        private int AngleToSteps(double angleDelta, string axis)
        {
            int axisSign = (axis == "X") ? -1 : 1;
            return (int)Math.Round(angleDelta / _stepResolution) * axisSign;
        }

        private bool MoveSteps(IComm emio, int steps, Action<string> log)
        {
            if (steps == 0) return true;
            string stepText = steps.ToString(CultureInfo.InvariantCulture);
            int len = 3 + stepText.Length;
            string cmd = $"PMD0{len}1ML{stepText}";
            EmioSend(emio, cmd, log);
            // step 수 비례 timeout: |steps| × 5ms + 3초 마진 (최소 3초, 최대 15초)
            int timeoutMs = Math.Clamp(Math.Abs(steps) * 5 + _moveTimeoutMin, _moveTimeoutMin, _moveTimeoutMax);
            return EmioRecvUntil(emio, "pmd0011", timeoutMs, log);
        }

        // ══════════════════════════════════════════════
        //  VL 센서 스트리밍 시작/중지
        // ══════════════════════════════════════════════

        private void StartVlStreaming(IComm vl, bool isRs485, string mode, Action<string> log)
        {
            if (isRs485)
            {
                // 485: SAM/ATIM은 제품 기본값(SAM=10, ATIM=0) 사용 — 설정 불필요
                VlSend(vl, $"<MODE,1,{mode}>", log);
                VlRecv(vl, 500, log);   // [MODE,0,1,{mode}] 응답 확인
                Thread.Sleep(200);
                VlSend(vl, "<START,1>", log);
                VlRecv(vl, 500, log);   // [START,0,1] 응답 확인
            }
            else
            {
                VlSend(vl, $"<MODE,{mode}>", log);
                VlRecv(vl, 500, log);   // [MODE,0,{mode}] 응답 확인
                Thread.Sleep(200);
                // SAM/ATIM: 232도 기본값(SAM=10, ATIM=0) 사용 — 설정 불필요
                VlSend(vl, "<START>", log);
                VlRecv(vl, 500, log);   // [START,0] 응답 확인
            }
        }

        private void StopVlStreaming(IComm vl, bool isRs485, Action<string> log)
        {
            string stopCmd = isRs485 ? "<STOP,1>" : "<STOP>";
            // VL20 STOP: 10ms 주기로 여러 번 송신 (물리적 충돌 대비)
            if (isRs485)
            {
                for (int i = 0; i < 5; i++)
                {
                    VlSend(vl, stopCmd, log);
                    Thread.Sleep(10);
                }
            }
            else
            {
                VlSend(vl, stopCmd, log);
            }
            Thread.Sleep(100);
            // 485: STOP 응답 + 잔류 스트리밍 데이터 드레인 (다음 명령 혼입 방지)
            if (isRs485) VlDrain(vl, log);
            string modeCmd = isRs485 ? "<MODE,1,0>" : "<MODE,0>";
            VlSend(vl, modeCmd, log);
            VlRecv(vl, 500, log);   // [MODE,0,...] 응답 확인
        }

        // ══════════════════════════════════════════════
        //  AN 파싱 (기울기)
        // ══════════════════════════════════════════════

        private static bool TryParseAnFromRaw(string raw, bool isRs485, string axis, out double value, out List<double> allSamples)
        {
            value = 0;
            allSamples = new List<double>();
            bool found = false;
            foreach (var frame in ExtractBracketFrames(raw))
            {
                if (!TryParseAnSample(frame, isRs485, out double x, out double y))
                    continue;
                double v = (axis == "X") ? x : y;
                allSamples.Add(v);
                value = v;
                found = true;
            }
            return found;
        }

        private static void SaveAnCsv(string axis, double targetAngle, double gyroAngle, List<double> samples, Action<string> log)
        {
            try
            {
                string baseDir = Path.Combine(AppContext.BaseDirectory, "csv_tilt");
                Directory.CreateDirectory(baseDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string sign = targetAngle >= 0 ? "p" : "m";
                string fileName = $"AN_{axis}_{sign}{Math.Abs(targetAngle):F0}deg_{timestamp}.csv";
                string filePath = Path.Combine(baseDir, fileName);

                using var sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
                sw.WriteLine($"# axis={axis}, target={targetAngle:F2}, gyro={gyroAngle:F3}, samples={samples.Count}");
                sw.WriteLine("index,value");
                for (int i = 0; i < samples.Count; i++)
                    sw.WriteLine($"{i},{samples[i]:F3}");

                if (Cantops.FlexFab.LogConfig.IsTest) log($"[TILT] CSV 저장: {fileName} ({samples.Count}개 샘플)");
            }
            catch (Exception ex)
            {
                log($"[TILT] CSV 저장 실패: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════
        //  파싱 헬퍼
        // ══════════════════════════════════════════════

        private static IEnumerable<string> ExtractBracketFrames(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            int i = 0;
            while (i < raw.Length)
            {
                int s = raw.IndexOf('[', i);
                if (s < 0) yield break;
                int e = raw.IndexOf(']', s + 1);
                if (e < 0) yield break;
                yield return raw.Substring(s, e - s + 1);
                i = e + 1;
            }
        }

        private static string[] SplitBracketResponse(string frame)
        {
            if (string.IsNullOrWhiteSpace(frame)) return Array.Empty<string>();
            frame = frame.Trim();
            if (frame.StartsWith("[")) frame = frame.Substring(1);
            if (frame.EndsWith("]")) frame = frame.Substring(0, frame.Length - 1);
            var tokens = frame.Split(',');
            for (int i = 0; i < tokens.Length; i++) tokens[i] = tokens[i].Trim();
            return tokens;
        }

        private static bool TryParseDoubleFlexible(string text, out double value)
        {
            return
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseGacAt(string[] parts, int idx, out double x, out double y, out double z, out double rms)
        {
            x = y = z = rms = 0;
            return parts.Length >= idx + 4 &&
                   TryParseDoubleFlexible(parts[idx], out x) &&
                   TryParseDoubleFlexible(parts[idx + 1], out y) &&
                   TryParseDoubleFlexible(parts[idx + 2], out z) &&
                   TryParseDoubleFlexible(parts[idx + 3], out rms);
        }

        private static bool TryParseGacSample(string frame, bool isRs485, Action<string> log, out double x, out double y, out double z, out double rms)
        {
            x = y = z = rms = 0;
            var parts = SplitBracketResponse(frame);
            if (parts.Length == 0 || !parts[0].Equals("GAC", StringComparison.OrdinalIgnoreCase))
                return false;

            if (isRs485)
            {
                // 7필드: [GAC,RV,ID,X,Y,Z,RMS] — <GAC,1> 쿼리 응답
                if (parts.Length >= 7 && int.TryParse(parts[1], out int rv485) && int.TryParse(parts[2], out _))
                {
                    if (rv485 != 0)
                    {
                        if (Cantops.FlexFab.LogConfig.IsTest) log($"[VIBE] GAC 485 비정상 프레임 무시: {frame} (RV={rv485})");
                        return false;
                    }
                    return TryParseGacAt(parts, 3, out x, out y, out z, out rms);
                }
                // 6필드: [GAC,ID,X,Y,Z,RMS] — 스트리밍 프레임
                return parts.Length >= 6 && TryParseGacAt(parts, 2, out x, out y, out z, out rms);
            }

            // 6필드: [GAC,rv,X,Y,Z,RMS] — <GAC> 쿼리 응답 (rv_code 포함)
            if (parts.Length >= 6 && int.TryParse(parts[1], out int rvCode))
            {
                if (rvCode != 0)
                {
                    if (Cantops.FlexFab.LogConfig.IsTest) log($"[VIBE] GAC 비정상 프레임 무시: {frame} (RV={rvCode})");
                    return false;
                }
                return TryParseGacAt(parts, 2, out x, out y, out z, out rms);
            }

            // 5필드: [GAC,X,Y,Z,RMS] — 스트리밍 프레임
            if (parts.Length >= 5 && TryParseGacAt(parts, 1, out x, out y, out z, out rms))
                return true;

            return false;
        }

        private static bool TryParseAnSample(string frame, bool isRs485, out double x, out double y)
        {
            x = y = 0;
            var parts = SplitBracketResponse(frame);
            if (parts.Length == 0 || !parts[0].Equals("AN", StringComparison.OrdinalIgnoreCase))
                return false;
            int offset = isRs485 ? 2 : 1;
            int required = isRs485 ? 4 : 3;
            return parts.Length >= required &&
                   TryParseDoubleFlexible(parts[offset], out x) &&
                   TryParseDoubleFlexible(parts[offset + 1], out y);
        }

        // ══════════════════════════════════════════════
        //  EMIO 헬퍼 (TCP, STX/ETX 프레임)
        // ══════════════════════════════════════════════

        private static void EmioSend(IComm emio, string cmd, Action<string> log)
        {
            string body = "\x02" + cmd + "\x03";
            emio.Send(new Dictionary<string, object?> { { "body", body } });
            if (Cantops.FlexFab.LogConfig.IsTest) log($"[EMIO] TX: {cmd}");
        }

        private bool EmioRecvUntil(IComm emio, string expected, int timeoutMs, Action<string> log)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remain <= 0) break;

                var resp = emio.Recv(new Dictionary<string, object?> { { "timeout", Math.Min(remain, 500) } });
                if (resp == null || !resp.TryGetValue("value", out var vObj)) continue;

                string v = vObj?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(v)) continue;

                if (Cantops.FlexFab.LogConfig.IsTest) log($"[EMIO] RX: {v}");

                // 무시할 응답
                if (v.StartsWith("pmd5011") || v.StartsWith("pmd4011") ||
                    v.StartsWith("pmp0011") || v.StartsWith("ppc6"))
                    continue;

                if (v.Contains(expected))
                    return true;
            }
            log($"[EMIO] TIMEOUT: '{expected}' ({timeoutMs}ms)");
            return false;
        }

        private static void EmioFlush(IComm emio, Action<string> log, int maxAttempts = 5)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var resp = emio.Recv(new Dictionary<string, object?> { { "timeout", 100 } });
                if (resp == null) break;
                if (!resp.TryGetValue("value", out var v) || string.IsNullOrWhiteSpace(v?.ToString())) break;
            }
        }

        // ══════════════════════════════════════════════
        //  VL 센서 헬퍼 (UART)
        // ══════════════════════════════════════════════

        private static bool VlSend(IComm vl, string cmd, Action<string> log)
        {
            string framed = (cmd.StartsWith("<") && cmd.EndsWith(">")) ? cmd : $"<{cmd}>";
            try
            {
                vl.Send(new Dictionary<string, object?> { { "body", framed + "\n" }, { "no_newline", true } });
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[VL] TX: {framed}");
                return true;
            }
            catch (Exception ex)
            {
                log($"[VL] Send error: {ex.Message}");
                return false;
            }
        }

        private string VlRecv(IComm vl, int timeoutMs, Action<string> log, bool logRx = true)
        {
            var sb = new StringBuilder();
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            bool sawAny = false;
            var lastRx = DateTime.MinValue;

            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remain <= 0) break;

                var resp = vl.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   Math.Min(200, remain) },
                    { "recv_mode", "line" }
                });

                string v = "";
                if (resp != null && resp.TryGetValue("value", out var vo))
                    v = vo?.ToString() ?? "";

                if (!string.IsNullOrWhiteSpace(v))
                {
                    sb.Append(v).Append("\n");
                    if (logRx && Cantops.FlexFab.LogConfig.IsTest)
                        log($"[VL] RX: {v}");
                    sawAny = true;
                    lastRx = DateTime.UtcNow;
                    continue;
                }

                if (sawAny && (DateTime.UtcNow - lastRx).TotalMilliseconds >= 120) break;
            }
            return sb.ToString().Trim();
        }

        private static void VlClear(IComm vl, Action<string> log, int maxAttempts = 10)
        {
            int emptyCount = 0;
            for (int i = 0; i < maxAttempts; i++)
            {
                var resp = vl.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   100  },
                    { "recv_mode", "line" }
                });
                if (resp == null || !resp.TryGetValue("value", out var v) || string.IsNullOrWhiteSpace(v?.ToString()))
                {
                    if (++emptyCount >= 2) break;  // 연속 2회 빈 응답이면 종료
                }
                else
                {
                    emptyCount = 0;  // 데이터 있으면 카운터 리셋
                }
            }
        }

        /// <summary>485 STOP 후 잔류 응답 드레인 (연속 3회 빈 응답 or 200ms 타임아웃)</summary>
        private static void VlDrain(IComm vl, Action<string> log, int timeoutMs = 200)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            int emptyCount = 0;
            while (emptyCount < 3 && DateTime.UtcNow < deadline)
            {
                var resp = vl.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   50   },
                    { "recv_mode", "line" }
                });
                if (resp == null || !resp.TryGetValue("value", out var v) || string.IsNullOrWhiteSpace(v?.ToString()))
                    emptyCount++;
                else
                    emptyCount = 0;
            }
        }

        // ══════════════════════════════════════════════
        //  자이로 헬퍼 (SOLAR-360-420, RS232 38400 8N1)
        // ══════════════════════════════════════════════

        private static bool GyroSendCmd(IComm gyro, string cmd, Action<string> log)
        {
            try
            {
                gyro.Send(new Dictionary<string, object?> { { "body", cmd + "\n" }, { "no_newline", true } });
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[GYRO] TX: {cmd}");
            }
            catch (Exception ex)
            {
                log($"[GYRO] Send error: {ex.Message}");
                return false;
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(800);
            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                var resp = gyro.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   Math.Min(remain, 300) },
                    { "recv_mode", "line" }
                });
                if (resp == null) continue;
                if (!resp.TryGetValue("value", out var vo)) continue;
                string v = vo?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(v)) continue;
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[GYRO] RX: {v}");
                if (v.Contains("OK")) return true;
            }
            log($"[GYRO] {cmd}: OK 응답 없음");
            return false;
        }

        private static bool GyroSetZero(IComm gyro, Action<string> log)
        {
            try
            {
                gyro.Send(new Dictionary<string, object?> { { "body", "setzcur\n" }, { "no_newline", true } });
                if (Cantops.FlexFab.LogConfig.IsTest) log("[GYRO] TX: setzcur");
            }
            catch (Exception ex)
            {
                log($"[GYRO] Send error: {ex.Message}");
                return false;
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(800);
            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                var resp = gyro.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   Math.Min(remain, 300) },
                    { "recv_mode", "line" }
                });
                if (resp == null) continue;
                if (!resp.TryGetValue("value", out var vo)) continue;
                string v = vo?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(v)) continue;
                if (Cantops.FlexFab.LogConfig.IsTest) log($"[GYRO] RX: {v}");
                if (v.Contains("OK")) return true;
            }
            log("[GYRO] setzcur: OK 응답 없음");
            return false;
        }

        private static bool GyroGetAngle(IComm gyro, Action<string> log, out double angle)
        {
            angle = 0;
            try
            {
                gyro.Send(new Dictionary<string, object?> { { "body", "get---x\n" }, { "no_newline", true } });
            }
            catch (Exception ex)
            {
                log($"[GYRO] Send error: {ex.Message}");
                return false;
            }

            var deadline = DateTime.UtcNow.AddMilliseconds(800);
            while (DateTime.UtcNow < deadline)
            {
                int remain = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                var resp = gyro.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   Math.Min(remain, 300) },
                    { "recv_mode", "line" }
                });
                if (resp == null) continue;
                if (!resp.TryGetValue("value", out var vo)) continue;
                string v = vo?.ToString()?.Trim() ?? "";
                if (string.IsNullOrEmpty(v)) continue;

                string parse = v.StartsWith("OK", StringComparison.OrdinalIgnoreCase)
                    ? v.Substring(2).Trim()
                    : v;

                if (double.TryParse(parse, NumberStyles.Float, CultureInfo.InvariantCulture, out angle))
                    return true;
            }
            log("[GYRO] get---x: 파싱 실패");
            return false;
        }

        // ══════════════════════════════════════════════
        //  공통 유틸
        // ══════════════════════════════════════════════

        private IComm? ResolveComm(IDictionary<string, object?> libraries, string key, Action<string> log)
        {
            if (!libraries.TryGetValue(key, out var obj) || obj is not IComm comm)
            {
                log($"[MotionJig] FAIL: library '{key}' not found or not IComm");
                return null;
            }
            return comm;
        }

        private static int GetParamInt(IDictionary<string, object?> param, string key, int def = 0)
        {
            if (!param.TryGetValue(key, out var v) || v == null) return def;
            if (v is int i) return i;
            if (v is long l) return (int)l;
            if (v is JsonElement je && je.ValueKind == JsonValueKind.Number) return je.GetInt32();
            return int.TryParse(v.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : def;
        }

        private static double GetParamDouble(IDictionary<string, object?> param, string key, double def = 0)
        {
            if (!param.TryGetValue(key, out var v) || v == null) return def;
            if (v is double d) return d;
            if (v is float f) return f;
            if (v is decimal m) return (double)m;
            if (v is JsonElement je && je.ValueKind == JsonValueKind.Number) return je.GetDouble();
            return TryParseDoubleFlexible(v.ToString() ?? "", out double r) ? r : def;
        }

        private static string GetParamString(IDictionary<string, object?> param, string key, string def = "")
        {
            if (!param.TryGetValue(key, out var v) || v == null) return def;
            var s = v.ToString();
            return string.IsNullOrWhiteSpace(s) ? def : s!;
        }

        private static bool GetParamBool(IDictionary<string, object?> param, string key, bool def = false)
        {
            if (!param.TryGetValue(key, out var v) || v == null) return def;
            if (v is bool b) return b;
            var s = v.ToString()?.Trim().ToLowerInvariant();
            return s switch
            {
                "1" or "true" or "yes" or "y" => true,
                "0" or "false" or "no" or "n" => false,
                _ => def
            };
        }

        private static double[] GetParamDoubleArray(IDictionary<string, object?> param, string key)
        {
            if (!param.TryGetValue(key, out var v) || v == null) return Array.Empty<double>();

            if (v is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                var list = new List<double>();
                foreach (var item in je.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Number)
                        list.Add(item.GetDouble());
                }
                return list.ToArray();
            }

            if (v is IEnumerable<object> enumerable)
            {
                var list = new List<double>();
                foreach (var item in enumerable)
                {
                    if (item is double d) list.Add(d);
                    else if (item is int i) list.Add(i);
                    else if (item is long l) list.Add(l);
                    else if (item is JsonElement itemJe && itemJe.ValueKind == JsonValueKind.Number)
                        list.Add(itemJe.GetDouble());
                    else if (TryParseDoubleFlexible(item?.ToString() ?? "", out double parsed))
                        list.Add(parsed);
                }
                return list.ToArray();
            }

            return Array.Empty<double>();
        }

        private static IDictionary<string, object?>? GetParamDict(IDictionary<string, object?> param, string key)
        {
            if (!param.TryGetValue(key, out var v) || v == null) return null;

            if (v is IDictionary<string, object?> dict) return dict;

            if (v is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                var result = new Dictionary<string, object?>();
                foreach (var prop in je.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        var inner = new Dictionary<string, object?>();
                        foreach (var innerProp in prop.Value.EnumerateObject())
                        {
                            if (innerProp.Value.ValueKind == JsonValueKind.Number)
                                inner[innerProp.Name] = innerProp.Value.GetDouble();
                            else if (innerProp.Value.ValueKind == JsonValueKind.String)
                                inner[innerProp.Name] = innerProp.Value.GetString();
                        }
                        result[prop.Name] = (IDictionary<string, object?>)inner;
                    }
                }
                return result;
            }

            return null;
        }

        private static IDictionary<string, object?> MakeResult(
            bool success, string procId, string procName,
            object? data, string resultStatus)
        {
            var retObj = new Dictionary<string, object?>
            {
                { "Name",   procName     },
                { "id",     procId       },
                { "data",   data         },
                { "result", resultStatus }
            };
            return new Dictionary<string, object?>
            {
                { "success", success },
                { "retmsg",  JsonSerializer.Serialize(retObj) }
            };
        }
    }
}
