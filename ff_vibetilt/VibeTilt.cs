using Cantops.FlexFab;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cantops.FlexFab
{
    public class VibeTilt : ModuleBase, IProcess
    {
        public const string MODULE_VERSION = "R1b3";

        public VibeTilt()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "vibetilt" },
                { "desc", "VibeTilt sensor inspection module (CTS-IOTS-VL01)" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object?>> {
                    new Dictionary<string, object?> { { "id", "VER_232" },     { "desc", "버전 통신검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "VER_485" },     { "desc", "버전 통신검사 RS-485" } },
                    new Dictionary<string, object?> { { "id", "SENTEMP_232" }, { "desc", "센서온도 검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "SENTEMP_485" }, { "desc", "센서온도 검사 RS-485" } },
                    new Dictionary<string, object?> { { "id", "APPCFG_232" },  { "desc", "Config 데이터 확인 RS-232" } },
                    new Dictionary<string, object?> { { "id", "APPCFG_485" },  { "desc", "Config 데이터 확인 RS-485" } },
                    new Dictionary<string, object?> { { "id", "VIBE_232" },    { "desc", "진동검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "VIBE_485" },    { "desc", "진동검사 RS-485" } },
                    new Dictionary<string, object?> { { "id", "TILT_232" },    { "desc", "기울기검사 RS-232" } },
                    new Dictionary<string, object?> { { "id", "TILT_485" },    { "desc", "기울기검사 RS-485" } },
                    new Dictionary<string, object?> { { "id", "TILT_X_232" },  { "desc", "기울기검사 X축 RS-232" } },
                    new Dictionary<string, object?> { { "id", "TILT_X_485" },  { "desc", "기울기검사 X축 RS-485" } },
                    new Dictionary<string, object?> { { "id", "TILT_Y_232" },  { "desc", "기울기검사 Y축 RS-232" } },
                    new Dictionary<string, object?> { { "id", "TILT_Y_485" },  { "desc", "기울기검사 Y축 RS-485" } },
                    new Dictionary<string, object?> { { "id", "UID_232" },     { "desc", "시리얼번호 설정 RS-232" } },
                    new Dictionary<string, object?> { { "id", "UID_485" },     { "desc", "시리얼번호 설정 RS-485" } },
                    new Dictionary<string, object?> { { "id", "OFFSET_232" },  { "desc", "Calibration RS-232" } },
                    new Dictionary<string, object?> { { "id", "OFFSET_485" },  { "desc", "Calibration RS-485" } },
                    new Dictionary<string, object?> { { "id", "APPCFG_SAVE_232" }, { "desc", "파라미터 저장 RS-232" } },
                    new Dictionary<string, object?> { { "id", "APPCFG_SAVE_485" }, { "desc", "파라미터 저장 RS-485" } },
                    new Dictionary<string, object?> { { "id", "REFTEMP_232" }, { "desc", "센서온도 저장 RS-232" } },
                    new Dictionary<string, object?> { { "id", "REFTEMP_485" }, { "desc", "센서온도 저장 RS-485" } },
                    new Dictionary<string, object?> { { "id", "RCONF_232" },   { "desc", "출하 상태 설정 RS-232" } },
                    new Dictionary<string, object?> { { "id", "RCONF_485" },   { "desc", "출하 상태 설정 RS-485" } },
                }}
            };
        }

        public void Begin() { }
        public void End() { }

        // ──────────────────────────────────────────────
        //  Helper: UART resolve
        // ──────────────────────────────────────────────

        private IComm? ResolveUart(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string configKey = "uart_232")
        {
            string? uartId = null;

            if (config_ != null &&
                config_.TryGetValue(configKey, out var cfgUart) &&
                cfgUart is string cfgUartId &&
                !string.IsNullOrWhiteSpace(cfgUartId))
            {
                uartId = cfgUartId;
            }

            // param에서 uart_id로 오버라이드 가능 (밀어내기식 2포트 지원)
            if (param.TryGetValue("uart_id", out var uartIdOverride) &&
                uartIdOverride != null &&
                !string.IsNullOrWhiteSpace(uartIdOverride.ToString()))
            {
                uartId = uartIdOverride.ToString();
            }

            if (string.IsNullOrWhiteSpace(uartId) &&
                param.TryGetValue(configKey, out var prmUart) &&
                prmUart != null &&
                !string.IsNullOrWhiteSpace(prmUart.ToString()))
            {
                uartId = prmUart.ToString();
            }

            if (string.IsNullOrWhiteSpace(uartId))
            {
                logAction($"[VibeTilt] FAIL : uart id not set (key={configKey})");
                return null;
            }

            if (!libraries.TryGetValue(uartId, out var libObj) || libObj is not IComm comm)
            {
                logAction($"[VibeTilt] FAIL : uart '{uartId}' not found or not IComm");
                return null;
            }

            return comm;
        }

        // ──────────────────────────────────────────────
        //  Helper: param utilities
        // ──────────────────────────────────────────────

        private static object? GetParamValue(IDictionary<string, object?> param, string key)
        {
            return param.TryGetValue(key, out var v) ? v : null;
        }

        private static int GetParamInt(IDictionary<string, object?> param, string key, int def = 0)
        {
            var v = GetParamValue(param, key);
            if (v == null) return def;
            if (int.TryParse(v.ToString(), out var n)) return n;
            return def;
        }

        // ws 모듈 config(vibetilt.config) 글로벌 값 조회 — param에 없을 때 fallback
        private int GetConfigInt(string key, int def = 0)
        {
            if (config_ == null) return def;
            if (!config_.TryGetValue(key, out var v) || v == null) return def;
            if (int.TryParse(v.ToString(), out var n)) return n;
            return def;
        }

        private static double GetParamDouble(IDictionary<string, object?> param, string key, double def = 0)
        {
            var v = GetParamValue(param, key);
            if (v == null) return def;
            if (double.TryParse(v.ToString(), out var n)) return n;
            return def;
        }

        private static string GetParamString(IDictionary<string, object?> param, string key, string def = "")
        {
            var v = GetParamValue(param, key);
            return v?.ToString() ?? def;
        }

        private static bool GetParamBool(IDictionary<string, object?> param, string key, bool def = false)
        {
            var v = GetParamValue(param, key);
            if (v == null) return def;

            if (v is bool b) return b;

            var s = v.ToString()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(s)) return def;

            return s switch
            {
                "1" => true,
                "0" => false,
                "true" => true,
                "false" => false,
                "yes" => true,
                "no" => false,
                "y" => true,
                "n" => false,
                _ => def
            };
        }

        private static object? GetTestValue(object? test, string key)
        {
            if (test is IDictionary<string, object?> d1 && d1.TryGetValue(key, out var v1))
                return v1;
            if (test is IDictionary<string, object> d2 && d2.TryGetValue(key, out var v2))
                return v2;
            return null;
        }

        private static string GetTestString(object? test, string key)
        {
            var v = GetTestValue(test, key);
            return v?.ToString() ?? "";
        }

        private static int GetTestInt(object? test, string key, int defaultValue = 0)
        {
            var v = GetTestValue(test, key);
            if (v == null) return defaultValue;
            if (int.TryParse(v.ToString(), out var n)) return n;
            return defaultValue;
        }

        private static double GetTestDouble(object? test, string key, double defaultValue = 0)
        {
            var v = GetTestValue(test, key);
            if (v == null) return defaultValue;
            if (double.TryParse(v.ToString(), out var n)) return n;
            return defaultValue;
        }

        // ──────────────────────────────────────────────
        //  Helper: steps/tests 배열 읽기
        // ──────────────────────────────────────────────

        private static IEnumerable<object?> GetStepList(IDictionary<string, object?> param)
        {
            if (param.TryGetValue("steps", out var stepsObj) && stepsObj is System.Collections.IEnumerable stepsEnum)
                return stepsEnum.Cast<object?>();
            if (param.TryGetValue("tests", out var testsObj) && testsObj is System.Collections.IEnumerable testsEnum)
                return testsEnum.Cast<object?>();
            return Array.Empty<object?>();
        }

        // ──────────────────────────────────────────────
        //  Helper: send / recv
        // ──────────────────────────────────────────────

        private static string ExtractValue(IDictionary<string, object?>? resp)
        {
            if (resp == null) return "";
            if (resp.TryGetValue("value", out var v) && v != null)
                return v.ToString() ?? "";
            return "";
        }

        private static bool IsBracketResponse(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string t = text.Trim();
            return t.StartsWith("[") && t.EndsWith("]");
        }

        private static string[] SplitBracketResponse(string text)
        {
            return text.Trim().TrimStart('[').TrimEnd(']').Split(',');
        }

        private static List<string> ExtractBracketFrames(string text)
        {
            var frames = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return frames;

            string s = text.Trim();
            int i = 0;
            while (i < s.Length)
            {
                int start = s.IndexOf('[', i);
                if (start < 0) break;
                int end = s.IndexOf(']', start + 1);
                if (end < 0) break;

                frames.Add(s.Substring(start, end - start + 1));
                i = end + 1;
            }

            return frames;
        }

        private static bool TryGetRvCode(string frame, out int rv)
        {
            rv = -1;
            if (!IsBracketResponse(frame)) return false;
            var parts = SplitBracketResponse(frame);
            if (parts.Length < 2) return false;
            return int.TryParse(parts[1].Trim(), out rv);
        }

        private static string GetRvDescription(int rv)
        {
            return rv switch
            {
                0 => "정상 수신 및 동작",
                1 => "패킷 수신 불가 상태",
                2 => "유효하지 않은 패킷",
                4 => "알 수 없는 명령어",
                5 => "레지스터 데이터 불일치",
                6 => "드라이버 접근 실패",
                7 => "레지스터 쓰기 실패",
                8 => "레지스터 읽기 실패",
                9 => "파라미터 범위 초과",
                _ => "정의되지 않은 RV"
            };
        }

        private static bool TryParseDoubleFlexible(string text, out double value)
        {
            return
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseGacAt(
            string[] parts,
            int idx,
            out double x,
            out double y,
            out double z,
            out double rms)
        {
            x = y = z = rms = 0;
            return parts.Length >= idx + 4 &&
                TryParseDoubleFlexible(parts[idx], out x) &&
                TryParseDoubleFlexible(parts[idx + 1], out y) &&
                TryParseDoubleFlexible(parts[idx + 2], out z) &&
                TryParseDoubleFlexible(parts[idx + 3], out rms);
        }

        private static bool TryParseGacSample(
            string frame,
            bool isRs485,
            Action<string> logAction,
            out double x,
            out double y,
            out double z,
            out double rms)
        {
            x = y = z = rms = 0;
            if (!IsBracketResponse(frame)) return false;

            var parts = SplitBracketResponse(frame);
            if (parts.Length == 0 || !parts[0].Trim().Equals("GAC", StringComparison.OrdinalIgnoreCase))
                return false;

            if (isRs485)
            {
                // RS-485 폴링: [GAC,0,ID,X,Y,Z,RMS] (errcode 포함, length=7, offset=3)
                if (parts.Length >= 7 && TryParseGacAt(parts, 3, out x, out y, out z, out rms))
                    return true;
                // RS-485 스트리밍 호환: [GAC,ID,X,Y,Z,RMS] (length=6, offset=2)
                if (parts.Length >= 6 && TryParseGacAt(parts, 2, out x, out y, out z, out rms))
                    return true;
                return false;
            }

            // RS-232 정상: [GAC,X,Y,Z,RMS]
            if (parts.Length >= 5 && TryParseGacAt(parts, 1, out x, out y, out z, out rms))
                return true;

            // 간헐 비정상: [GAC,RV,X,Y,Z,RMS] → RV==0만 허용
            if (parts.Length >= 6 && int.TryParse(parts[1].Trim(), out int rvCode))
            {
                if (rvCode != 0)
                {
                    logAction($"[VIBE] GAC 비정상 프레임 무시: {frame} ({GetRvDescription(rvCode)})");
                    return false;
                }
                return TryParseGacAt(parts, 2, out x, out y, out z, out rms);
            }

            return false;
        }

        private static bool EvaluateVibeSample(
            double x, double y, double z, double rms,
            double oneMin, double oneMax,
            double zeroMin, double zeroMax,
            double rmsMin, double rmsMax,
            bool checkRms,
            out List<Dictionary<string, object?>> details)
        {
            details = new List<Dictionary<string, object?>>();

            double[] values = { x, y, z };
            string[] axisNames = { "X", "Y", "Z" };
            bool ok = true;

            for (int i = 0; i < 3; i++)
            {
                bool isOneAxis = Math.Abs(values[i]) > 0.5;
                double min = isOneAxis ? oneMin : zeroMin;
                double max = isOneAxis ? oneMax : zeroMax;
                string rangeLabel = isOneAxis ? "1축" : "0축";
                bool axisOk = values[i] >= min && values[i] <= max;
                if (!axisOk) ok = false;

                details.Add(new Dictionary<string, object?>
                {
                    { "item", $"진동 {axisNames[i]}({rangeLabel})" },
                    { "value", Math.Round(values[i], 3) },
                    { "min", min },
                    { "max", max },
                    { "result", axisOk ? "OK" : "NG" }
                });
            }

            if (checkRms)
            {
                bool rmsOk = rms >= rmsMin && rms <= rmsMax;
                if (!rmsOk) ok = false;
                details.Add(new Dictionary<string, object?>
                {
                    { "item", "진동 RMS" },
                    { "value", Math.Round(rms, 3) },
                    { "min", rmsMin },
                    { "max", rmsMax },
                    { "result", rmsOk ? "OK" : "NG" }
                });
            }

            return ok;
        }

        private static string NormalizeCommandToken(string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return "";

            string c = command.Trim();
            if (c.StartsWith("<") && c.EndsWith(">") && c.Length >= 2)
                c = c.Substring(1, c.Length - 2);

            int comma = c.IndexOf(',');
            if (comma >= 0)
                c = c.Substring(0, comma);

            return c.Trim().ToUpperInvariant();
        }

        private static string GetLineEnding(IDictionary<string, object?> param)
        {
            if (param.TryGetValue("line_ending", out var le) && le != null)
            {
                var s = le.ToString()!.ToLowerInvariant();
                if (s == "crlf") return "\r\n";
                if (s == "cr") return "\r";
                if (s == "lf") return "\n";
                if (s == "none") return "";
            }
            return "\n";
        }

        private bool SendCommand(
            IComm comm,
            string command,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // VibeTilt 프로토콜은 <CMD> / <CMD,arg> 형식을 사용한다.
            string framed = command.Trim();
            if (!(framed.StartsWith("<") && framed.EndsWith(">")))
                framed = $"<{framed}>";

            string lineEnding = GetLineEnding(param);
            string body = framed + lineEnding;

            // VL20 485 STOP: PDF 사양 — 송수신 충돌 대비 N회 반복 + ret(0) early-exit
            string token = NormalizeCommandToken(command);
            bool isRs485Stop = token == "STOP" && command.Contains(",1");
            if (isRs485Stop)
            {
                // 우선순위: param (검사별) > config_ (ws 모듈 글로벌) > default(10)
                int repeatCount = GetParamInt(param, "stop_repeat_count",
                                       GetConfigInt("stop_repeat_count", 10));
                int intervalMs  = GetParamInt(param, "stop_interval_ms",
                                       GetConfigInt("stop_interval_ms", 10));
                try
                {
                    bool gotRet0 = false;
                    int actualSent = 0;
                    for (int i = 0; i < repeatCount; i++)
                    {
                        comm.Send(new Dictionary<string, object?>
                        {
                            { "body", body },
                            { "no_newline", true }
                        });
                        actualSent = i + 1;

                        // 짧은 응답 윈도우 — ret(0) 감지 시 early-exit
                        var resp = comm.Recv(new Dictionary<string, object?>
                        {
                            { "timeout", 30 },
                            { "recv_mode", "line" }
                        });
                        var v = ExtractValue(resp);
                        if (!string.IsNullOrWhiteSpace(v) && v.Contains("[STOP,0,"))
                        {
                            gotRet0 = true;
                            break;
                        }

                        if (i + 1 < repeatCount) System.Threading.Thread.Sleep(intervalMs);
                    }
                    logAction($"[VibeTilt] TX: {framed} (485 stop x{actualSent}/{repeatCount}{(gotRet0 ? ", ret=0 ✓" : "")})");
                    return true;
                }
                catch (Exception ex)
                {
                    logAction($"[VibeTilt] FAIL : send '{command}' error: {ex.Message}");
                    return false;
                }
            }

            try
            {
                comm.Send(new Dictionary<string, object?>
                {
                    { "body", body },
                    { "no_newline", true }
                });
                logAction($"[VibeTilt] TX: {framed}");
                return true;
            }
            catch (Exception ex)
            {
                logAction($"[VibeTilt] FAIL : send '{command}' error: {ex.Message}");
                return false;
            }
        }

        private void ReadStep(
            IComm comm,
            int readTimeoutMs,
            Action<string> logAction,
            out string stepCollected,
            out string stepLast,
            int quietAfterReceiveMs = 120,
            int recvChunkTimeoutMs = 200)
        {
            stepCollected = "";
            stepLast = "";
            var end = DateTime.UtcNow.AddMilliseconds(readTimeoutMs);
            var sawAny = false;
            var lastRxAt = DateTime.MinValue;

            while (DateTime.UtcNow < end)
            {
                int remainMs = (int)(end - DateTime.UtcNow).TotalMilliseconds;
                if (remainMs <= 0) break;
                int chunkTimeout = Math.Min(Math.Max(20, recvChunkTimeoutMs), remainMs);

                var resp = comm.Recv(new Dictionary<string, object?>
                {
                    { "timeout", chunkTimeout },
                    { "recv_mode", "line" }
                });
                var v = ExtractValue(resp);
                if (!string.IsNullOrWhiteSpace(v))
                {
                    stepLast = v;
                    stepCollected += v + "\n";
                    logAction($"[VibeTilt] RX: {v}");
                    sawAny = true;
                    lastRxAt = DateTime.UtcNow;
                    continue;
                }

                // 응답을 이미 받았다면, 짧은 무응답 구간 이후 즉시 단계 종료
                if (sawAny && (DateTime.UtcNow - lastRxAt).TotalMilliseconds >= quietAfterReceiveMs)
                {
                    break;
                }
            }
        }

        // v0.6.9: 능동 STOP + silence detection으로 강화
        // - 시작 시 STOP 강제 송신 (잔류 streaming 차단 시도, 485/232 양 형식 모두 시도)
        // - maxAttempts 10 → 30 (1초 → 3초까지 drain)
        // - 연속 빈응답 2 → 3 (안전 마진)
        // - 잔류 패킷 감지 시 1회 로그 (디버깅 용)
        private void ClearCommBuffer(IComm comm, Action<string> logAction, int maxAttempts = 30, int timeout = 100)
        {
            // 1) 잔류 streaming 강제 정지 시도 (응답 무시, 보드가 인식하는 형식만 처리)
            try
            {
                comm.Send(new Dictionary<string, object?>
                {
                    { "body", "<STOP,1>\n" },
                    { "no_newline", true }
                });
            }
            catch { }
            try
            {
                comm.Send(new Dictionary<string, object?>
                {
                    { "body", "<STOP>\n" },
                    { "no_newline", true }
                });
            }
            catch { }
            System.Threading.Thread.Sleep(50);  // 보드 처리 시간

            // 2) 적극적 buffer drain (silence detection)
            int emptyCount = 0;
            int residueCount = 0;
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var resp = comm.Recv(new Dictionary<string, object?>
                    {
                        { "timeout", timeout },
                        { "recv_mode", "line" }
                    });
                    var v = ExtractValue(resp);
                    if (string.IsNullOrWhiteSpace(v))
                    {
                        if (++emptyCount >= 3) break;  // 연속 3회 빈 응답이면 진짜 멈춤
                    }
                    else
                    {
                        emptyCount = 0;
                        residueCount++;
                    }
                }
                catch { break; }
            }

            if (residueCount > 0 && LogConfig.IsTest)
                logAction($"[CLEAR] 잔류 패킷 {residueCount}개 정리 완료");
        }

        // ──────────────────────────────────────────────
        //  Helper: baudrate 변경
        // ──────────────────────────────────────────────

        private bool ChangeBaudRate(IComm comm, int newBaud, Action<string> logAction)
        {
            try
            {
                comm.Disconnect();
                var status = comm.GetStatus();
                if (status == null)
                {
                    logAction($"[VibeTilt] FAIL : GetStatus returned null during baud change");
                    return false;
                }

                var newConfig = new Dictionary<string, object?>();
                if (status.TryGetValue("portname", out var pn)) newConfig["port"] = pn;
                newConfig["baudrate"] = newBaud;
                if (status.TryGetValue("databits", out var db)) newConfig["databits"] = db;
                if (status.TryGetValue("parity", out var pa)) newConfig["parity"] = pa;
                if (status.TryGetValue("stopbits", out var sb)) newConfig["stopbits"] = sb;

                comm.SetConfig(newConfig);
                bool ok = comm.Connect();
                logAction($"[VibeTilt] BaudRate changed to {newBaud}, connect={ok}");
                return ok;
            }
            catch (Exception ex)
            {
                logAction($"[VibeTilt] FAIL : ChangeBaudRate error: {ex.Message}");
                return false;
            }
        }

        // ──────────────────────────────────────────────
        //  Helper: retmsg builder
        // ──────────────────────────────────────────────

        private static IDictionary<string, object?> MakeResult(
            bool success,
            string procId,
            string procName,
            object? resultData,
            string resultStatus)
        {
            var retObj = new Dictionary<string, object?>
            {
                { "Name", procName },
                { "id", procId },
                { "data", resultData },
                { "result", resultStatus }
            };

            return new Dictionary<string, object?>
            {
                { "success", success },
                { "retmsg", JsonSerializer.Serialize(retObj) }
            };
        }

        // ══════════════════════════════════════════════
        //  검사 메서드들
        // ══════════════════════════════════════════════

        public IDictionary<string, object?> VER_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_232", "VER_232", "버전 통신검사 RS-232");
        }

        public IDictionary<string, object?> VER_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_485", "VER_485", "버전 통신검사 RS-485");
        }

        public IDictionary<string, object?> COMM_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_232", "COMM_232", "통신검사 RS-232");
        }

        public IDictionary<string, object?> COMM_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_485", "COMM_485", "통신검사 RS-485");
        }

        public IDictionary<string, object?> FWVER_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_232", "FWVER_232", "FW 버전 확인 RS-232");
        }

        public IDictionary<string, object?> FWVER_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_485", "FWVER_485", "FW 버전 확인 RS-485");
        }

        // ── UID: 시리얼 번호 설정 ──────────────────────
        public IDictionary<string, object?> UID_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_232", "UID_232", "시리얼번호 설정 RS-232", "UID");
        }

        public IDictionary<string, object?> UID_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_485", "UID_485", "시리얼번호 설정 RS-485", "UID");
        }

        // ── OFFSET: Calibration ────────────────────────
        public IDictionary<string, object?> OFFSET_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_232", "OFFSET_232", "Calibration RS-232");
        }

        public IDictionary<string, object?> OFFSET_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_485", "OFFSET_485", "Calibration RS-485");
        }

        // ── RCONF: 출하 상태 설정 ──────────────────────
        // UID·센서온도·현재 cal 값을 출하상태로 저장. 응답 완전일치 PASS (VER_Internal else 분기 재사용).
        // OFFSET(Y-Cal) 직후 / APPCFG_SAVE 직전 배치 (양축 cal 확정 상태 저장 + SAVE가 뒤따라 영구화)
        public IDictionary<string, object?> RCONF_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_232", "RCONF_232", "출하 상태 설정 RS-232");
        }

        public IDictionary<string, object?> RCONF_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return VER_Internal(libraries, param, logAction, "uart_485", "RCONF_485", "출하 상태 설정 RS-485");
        }

        // ── APPCFG_SAVE: 파라미터 저장 ────────────────
        // JSON param: "retry" (재시도 횟수, 기본 0), "retry_delay_ms" (재시도 전 대기, 기본 500)
        public IDictionary<string, object?> APPCFG_SAVE_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return APPCFG_SAVE_WithRetry(libraries, param, logAction, "uart_232", "APPCFG_SAVE_232", "파라미터 저장 RS-232");
        }

        public IDictionary<string, object?> APPCFG_SAVE_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return APPCFG_SAVE_WithRetry(libraries, param, logAction, "uart_485", "APPCFG_SAVE_485", "파라미터 저장 RS-485");
        }

        private IDictionary<string, object?> APPCFG_SAVE_WithRetry(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, string defaultProcId, string defaultProcName)
        {
            int retry = GetParamInt(param, "retry", 0);
            int retryDelayMs = GetParamInt(param, "retry_delay_ms", 500);

            var result = VER_Internal(libraries, param, logAction, uartKey, defaultProcId, defaultProcName);

            for (int i = 0; i < retry; i++)
            {
                if (result.TryGetValue("success", out var s) && Convert.ToBoolean(s))
                    break;

                logAction($"[APPCFG] SAVE 실패 → {retryDelayMs}ms 대기 후 재시도 ({i + 1}/{retry})");
                Thread.Sleep(retryDelayMs);
                result = VER_Internal(libraries, param, logAction, uartKey, defaultProcId, defaultProcName);
            }

            return result;
        }

        public IDictionary<string, object?> SENTEMP_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return SENTEMP_Internal(libraries, param, logAction, "uart_232", "SENTEMP_232", "센서온도 RS-232");
        }

        public IDictionary<string, object?> SENTEMP_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return SENTEMP_Internal(libraries, param, logAction, "uart_485", "SENTEMP_485", "센서온도 RS-485");
        }

        // ── REFTEMP: 센서 온도 저장 (읽기 → 범위가드 → 저장 → echo) ──
        public IDictionary<string, object?> REFTEMP_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return REFTEMP_Internal(libraries, param, logAction, "uart_232", "REFTEMP_232", "센서온도 저장 RS-232");
        }

        public IDictionary<string, object?> REFTEMP_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return REFTEMP_Internal(libraries, param, logAction, "uart_485", "REFTEMP_485", "센서온도 저장 RS-485");
        }

        public IDictionary<string, object?> APPCFG_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return APPCFG_Internal(libraries, param, logAction, "uart_232", "APPCFG_232", "Config확인 RS-232");
        }

        public IDictionary<string, object?> APPCFG_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            return APPCFG_Internal(libraries, param, logAction, "uart_485", "APPCFG_485", "Config확인 RS-485");
        }

        public IDictionary<string, object?> VIBE_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return VIBE_WithJudgeMode(libraries, param, logAction, "uart_232", false, "VIBE_232", "진동검사 RS-232", confirmCallback);
        }

        public IDictionary<string, object?> VIBE_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return VIBE_WithJudgeMode(libraries, param, logAction, "uart_485", true, "VIBE_485", "진동검사 RS-485", confirmCallback);
        }

        // 정지(rest) 안정성 검사 — 진동검사 앞. 정지 상태 센서 노이즈(출렁임) 검출.
        public IDictionary<string, object?> VIBE_STILL_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return VIBE_STILL_Internal(libraries, param, logAction, "uart_232", false, "VIBE_STILL_232", "정지 상태 검사 RS-232", confirmCallback);
        }

        public IDictionary<string, object?> VIBE_STILL_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return VIBE_STILL_Internal(libraries, param, logAction, "uart_485", true, "VIBE_STILL_485", "정지 상태 검사 RS-485", confirmCallback);
        }

        private IDictionary<string, object?> VIBE_WithJudgeMode(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, bool isRs485,
            string defaultProcId, string defaultProcName,
            Func<string, bool>? confirmCallback)
        {
            int judgeMode = GetParamInt(param, "judge_mode", 1);
            if (judgeMode <= 1)
                return VIBE_Internal(libraries, param, logAction, uartKey, isRs485, defaultProcId, defaultProcName, confirmCallback);

            // judge_mode=2: X, Y 각각 확인 (방향: X+=우측, Y+=뒤)
            string[] axisNames = { "X", "Y" };
            string[] prompts = {
                "우측으로 기울이세요.\nX축 진동을 확인합니다.",
                "뒤로 기울이세요.\nY축 진동을 확인합니다."
            };

            for (int i = 0; i < 2; i++)
            {
                var paramRound = new Dictionary<string, object?>(param);
                paramRound["operator_prompt"] = prompts[i];
                logAction($"[VIBE] judge_mode=2: {axisNames[i]}축 검사 ({i + 1}/2)");

                var result = VIBE_Internal(libraries, paramRound, logAction, uartKey, isRs485, defaultProcId, defaultProcName + $"({axisNames[i]})", confirmCallback);

                if (result.TryGetValue("success", out var s) && !Convert.ToBoolean(s))
                {
                    logAction($"[VIBE] judge_mode=2: {axisNames[i]}축 FAIL → 최종 FAIL");
                    return result;
                }
            }

            logAction("[VIBE] judge_mode=2: X/Y 2축 모두 PASS");
            return new Dictionary<string, object?> { { "success", true }, { "result", "PASS" } };
        }

        public IDictionary<string, object?> TILT_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return TILT_Internal(libraries, param, logAction, "uart_232", false, "TILT_232", "기울기검사 RS-232", confirmCallback);
        }

        public IDictionary<string, object?> TILT_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return TILT_Internal(libraries, param, logAction, "uart_485", true, "TILT_485", "기울기검사 RS-485", confirmCallback);
        }

        public IDictionary<string, object?> TILT_X_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return TILT_WithJudgeMode(libraries, param, logAction, "uart_232", false, "TILT_X_232", "기울기검사 X축 RS-232", confirmCallback);
        }

        public IDictionary<string, object?> TILT_X_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return TILT_WithJudgeMode(libraries, param, logAction, "uart_485", true, "TILT_X_485", "기울기검사 X축 RS-485", confirmCallback);
        }

        public IDictionary<string, object?> TILT_Y_232(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return TILT_WithJudgeMode(libraries, param, logAction, "uart_232", false, "TILT_Y_232", "기울기검사 Y축 RS-232", confirmCallback);
        }

        public IDictionary<string, object?> TILT_Y_485(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            return TILT_WithJudgeMode(libraries, param, logAction, "uart_485", true, "TILT_Y_485", "기울기검사 Y축 RS-485", confirmCallback);
        }

        private IDictionary<string, object?> TILT_WithJudgeMode(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, bool isRs485,
            string defaultProcId, string defaultProcName,
            Func<string, bool>? confirmCallback)
        {
            int judgeMode = GetParamInt(param, "judge_mode", 1);
            if (judgeMode <= 1)
                return TILT_Internal(libraries, param, logAction, uartKey, isRs485, defaultProcId, defaultProcName, confirmCallback);

            // judge_mode=2: +방향 → -방향 각각 검사
            // 방향 매핑: X+ = 우측, X- = 좌측, Y+ = 뒤, Y- = 앞
            string axis = GetParamString(param, "check_axis", "x").ToLowerInvariant();
            string axisLabel = axis == "x" ? "X" : "Y";
            string posDirLabel = axis == "x" ? "우측" : "뒤";
            string negDirLabel = axis == "x" ? "좌측" : "앞";

            // +방향 SPEC: 원래 min~max 그대로
            double posMin, posMax, negMin, negMax;
            if (axis == "x")
            {
                posMin = GetParamDouble(param, "x_min", 5.0);
                posMax = GetParamDouble(param, "x_max", 95.0);
                negMin = -posMax;  // -95
                negMax = -posMin;  // -5
            }
            else
            {
                posMin = GetParamDouble(param, "y_min", 5.0);
                posMax = GetParamDouble(param, "y_max", 95.0);
                negMin = -posMax;
                negMax = -posMin;
            }

            // 라운드1: +방향
            var paramRound1 = new Dictionary<string, object?>(param);
            paramRound1["operator_prompt"] = $"{posDirLabel}으로 기울이세요.\n{axisLabel}축 {posDirLabel} 기울기를 확인합니다.";
            if (axis == "x") { paramRound1["x_min"] = posMin; paramRound1["x_max"] = posMax; }
            else { paramRound1["y_min"] = posMin; paramRound1["y_max"] = posMax; }
            logAction($"[TILT] judge_mode=2: {axisLabel}축 {posDirLabel}(+) 검사 ({posMin}~{posMax}도)");
            var result1 = TILT_Internal(libraries, paramRound1, logAction, uartKey, isRs485, defaultProcId, defaultProcName + "(+)", confirmCallback);

            if (result1.TryGetValue("success", out var s1) && !Convert.ToBoolean(s1))
            {
                logAction($"[TILT] judge_mode=2: {axisLabel}축 {posDirLabel}(+) FAIL → 최종 FAIL");
                return result1;
            }

            // 라운드2: -방향 (부호 반전)
            var paramRound2 = new Dictionary<string, object?>(param);
            paramRound2["operator_prompt"] = $"{negDirLabel}으로 기울이세요.\n{axisLabel}축 {negDirLabel} 기울기를 확인합니다.";
            if (axis == "x") { paramRound2["x_min"] = negMin; paramRound2["x_max"] = negMax; }
            else { paramRound2["y_min"] = negMin; paramRound2["y_max"] = negMax; }
            logAction($"[TILT] judge_mode=2: {axisLabel}축 {negDirLabel}(-) 검사 ({negMin}~{negMax}도)");
            var result2 = TILT_Internal(libraries, paramRound2, logAction, uartKey, isRs485, defaultProcId, defaultProcName + "(-)", confirmCallback);

            if (result2.TryGetValue("success", out var s2) && !Convert.ToBoolean(s2))
            {
                logAction($"[TILT] judge_mode=2: {axisLabel}축 {negDirLabel}(-) FAIL → 최종 FAIL");
                return result2;
            }

            logAction($"[TILT] judge_mode=2: {axisLabel}축 양방향 PASS");
            return result2;
        }

        // ──────────────────────────────────────────────
        //  VER_Internal : 통신검사 (tests 배열 기반)
        // ──────────────────────────────────────────────

        private IDictionary<string, object?> VER_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, string defaultProcId, string defaultProcName,
            string logTag = "VER")
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;
            int readTimeoutMs = GetParamInt(param, "TIMEOUT", 5000);

            var steps = new List<Dictionary<string, object?>>();
            bool ok = true;

            ClearCommBuffer(uart, logAction);

            var stepList = GetStepList(param).ToList();

            if (stepList.Count == 0)
            {
                logAction($"[{logTag}] 실행할 테스트가 없습니다.");
                ok = false;
            }
            else
            {
                // {serial} 치환용 시리얼 번호
                string serialForReplace = GetParamString(param, "__serial", "");

                foreach (var t in stepList)
                {
                    int sleepMs = GetTestInt(t, "sleep", 0);
                    if (sleepMs > 0)
                    {
                        logAction($"Waiting {sleepMs} ms...");
                        System.Threading.Thread.Sleep(sleepMs);
                        continue;
                    }

                    string command = GetTestString(t, "command");
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    // JSON 명령의 {serial} 플레이스홀더를 실제 시리얼 번호로 치환
                    if (!string.IsNullOrEmpty(serialForReplace) && command.Contains("{serial}"))
                        command = command.Replace("{serial}", serialForReplace);

                    if (!SendCommand(uart, command, param, logAction))
                    {
                        ok = false;
                        steps.Add(new Dictionary<string, object?>
                        {
                            { "command", command },
                            { "value", null },
                            { "result", "NG" }
                        });
                        continue;
                    }

                    ReadStep(uart, readTimeoutMs, logAction, out var stepCollected, out var stepLast);

                    // 485 패킷 분리 대응: UART 수신이 여러 청크로 나뉘면 [OFFSET,0,1 + ,1] 처럼 잘림
                    // 줄바꿈 제거 후 합쳐서 대괄호 프레임을 재추출 → [OFFSET,0,1,1] 복원
                    string mergedCollected = stepCollected.Replace("\n", "").Replace("\r", "");
                    var mergedFrames = ExtractBracketFrames(mergedCollected);
                    string mergedLast = mergedFrames.Count > 0 ? mergedFrames[mergedFrames.Count - 1] : stepLast;

                    string expected = GetTestString(t, "expected_response");
                    if (!string.IsNullOrEmpty(serialForReplace) && expected.Contains("{serial}"))
                        expected = expected.Replace("{serial}", serialForReplace);
                    bool stepOk = IsBracketResponse(mergedLast);

                    // 명령어 토큰 추출 (VER 명령인지 확인용)
                    string commandToken = NormalizeCommandToken(command);
                    bool isVerCommand = commandToken.Equals("VER", StringComparison.OrdinalIgnoreCase);
                    bool isStopToken = commandToken.StartsWith("STOP", StringComparison.OrdinalIgnoreCase);

                    if (!stepOk)
                    {
                        if (isStopToken)
                        {
                            // 485 STOP setup: SendCommand가 N회 송수신하며 응답을 이미 소비 → ReadStep 버퍼 빔이 정상.
                            // 잔류 스트리밍 차단용 명령이므로 빈 응답을 실패로 보지 않는다. (0.6.9 동작 복원, 0.7.0 회귀 수정)
                            logAction($"[{logTag}] STOP setup(응답 소비됨, 정상)");
                            stepOk = true;
                        }
                        else
                        {
                            logAction($"[{logTag}] 응답 형식 오류(대괄호 필요): {mergedLast}");
                            ok = false;
                        }
                    }
                    else
                    {
                        var parts = SplitBracketResponse(mergedLast);

                        // VER 명령인 경우에만 VER 응답 검증
                        if (isVerCommand)
                        {
                            if (parts.Length == 0 || !parts[0].Trim().Equals("VER", StringComparison.OrdinalIgnoreCase))
                            {
                                logAction($"[{logTag}] 응답 명령 불일치: {mergedLast}");
                                ok = false;
                                stepOk = false;
                            }
                            else if (!TryGetRvCode(mergedLast, out int rv) || rv != 0)
                            {
                                logAction($"[{logTag}] RV 오류: {rv} ({GetRvDescription(rv)})");
                                ok = false;
                                stepOk = false;
                            }
                            else
                            {
                                logAction($"[{logTag}] 응답: {mergedLast}");
                            }
                        }
                        else
                        {
                            // VER 명령이 아닌 경우(ATIM 등), RV 코드만 확인
                            // STOP은 SendCommand 내부에서 N회 반복 + ret(0) early-exit 처리 완료 → RV 검증 스킵
                            if (!isStopToken && (!TryGetRvCode(mergedLast, out int rv) || rv != 0))
                            {
                                logAction($"[{logTag}] {commandToken} RV 오류: {rv} ({GetRvDescription(rv)})");
                                ok = false;
                                stepOk = false;
                            }
                        }
                    }
                    // expected_response 검증
                    //  - "[..." 로 시작하면 전체 프레임 → 완전 일치 (대소문자 구분)
                    //  - 그 외(예: "VER", 시리얼값 등)는 부분 포함 검사 (마커 용도)
                    if (!string.IsNullOrWhiteSpace(expected))
                    {
                        bool expectedMatched;
                        if (expected.StartsWith("["))
                        {
                            expectedMatched = mergedFrames.Any(f => string.Equals(f, expected, StringComparison.Ordinal));
                            if (!expectedMatched)
                                logAction($"[{logTag}] expected 프레임 불일치: 기대=[{expected}], 수신프레임={string.Join(" ", mergedFrames)}");
                        }
                        else
                        {
                            expectedMatched = mergedCollected.Contains(expected);
                            if (!expectedMatched)
                                logAction($"[{logTag}] 누락 항목: {expected}");
                        }
                        if (!expectedMatched)
                        {
                            ok = false;
                            stepOk = false;
                        }
                    }

                    // FW 버전 확인 (expected_fw_version 있으면) — 완전 매칭 (대소문자 구분, 부분 포함 불허)
                    string expectedFw = GetTestString(t, "expected_fw_version");
                    if (!string.IsNullOrWhiteSpace(expectedFw) && stepOk)
                    {
                        // 응답에서 버전 문자열 추출 (마지막 ',' 와 ']' 사이)
                        // 예: [VER,0,F/W ver.1.0.4]          → "F/W ver.1.0.4"
                        //     [VER,0,1,F/W ver.2.0.2.3]      → "F/W ver.2.0.2.3"
                        string actualFw = null;
                        if (mergedLast != null)
                        {
                            int lastComma = mergedLast.LastIndexOf(',');
                            int closeBracket = lastComma >= 0 ? mergedLast.IndexOf(']', lastComma) : -1;
                            if (lastComma >= 0 && closeBracket > lastComma)
                                actualFw = mergedLast.Substring(lastComma + 1, closeBracket - lastComma - 1);
                        }
                        bool fwMatch = actualFw != null && string.Equals(actualFw, expectedFw, StringComparison.Ordinal);
                        if (!fwMatch)
                        {
                            logAction($"[{logTag}] FW 버전 불일치: 기대=[{expectedFw}], 실제=[{actualFw ?? "추출실패"}], 응답={mergedLast}");
                            ok = false;
                            stepOk = false;
                        }
                        else
                        {
                            logAction($"[{logTag}] FW 버전 일치: {expectedFw}");
                        }
                    }

                    steps.Add(new Dictionary<string, object?>
                    {
                        { "command", command },
                        { "value", string.IsNullOrWhiteSpace(mergedLast) ? null : mergedLast },
                        { "result", stepOk ? "OK" : "NG" }
                    });
                }
            }

            // ── OFFSET 후 AN 검증: MODE 06 스트리밍으로 AN 값 수집 후 0도 확인 ──
            // 흐름: STOP → MODE 0 → MODE 06 → SAM → (ATIM) → START → AN 수집 → STOP → MODE 0
            // 485: SAM=10 필수 (115200 baud 최소 6ms), STOP 5회 반복 (충돌 대비), 버퍼 클리어
            if (ok && GetParamBool(param, "verify_an", false))
            {
                string anAxis = GetParamString(param, "verify_an_axis", "Y");
                double anMin = GetParamDouble(param, "verify_an_min", -0.05);
                double anMax = GetParamDouble(param, "verify_an_max", 0.05);

                bool isRs485 = uartKey.Contains("485");

                // 1) 이전 상태 초기화
                ClearCommBuffer(uart, logAction);
                SendCommand(uart, isRs485 ? "<STOP,1>" : "<STOP>", param, logAction);
                Thread.Sleep(100);
                ReadStep(uart, 300, logAction, out _, out _);

                // 2) MODE 06 (기울기 모드) 설정
                SendCommand(uart, isRs485 ? "<MODE,1,0>" : "<MODE,0>", param, logAction);
                Thread.Sleep(200);
                ReadStep(uart, 500, logAction, out _, out _);
                SendCommand(uart, isRs485 ? "<MODE,1,06>" : "<MODE,06>", param, logAction);
                Thread.Sleep(200);
                ReadStep(uart, 500, logAction, out _, out _);

                // 3) SAM/ATIM — 232/485 모두 기본값(SAM=10, ATIM=0) 사용하므로 설정 불필요

                // 4) START → AN 스트리밍 수집 (1초, OFFSET 직후 안정 상태)
                SendCommand(uart, isRs485 ? "<START,1>" : "<START>", param, logAction);
                Thread.Sleep(1000);
                ReadStep(uart, 1000, logAction, out var anCollected, out _);

                // 5) STOP + 정리
                if (isRs485)
                {
                    // 485 STOP 다회 반복 + ret(0) early-exit은 SendCommand 내부 처리 (PDF 주의사항)
                    SendCommand(uart, "<STOP,1>", param, logAction);
                    Thread.Sleep(500);       // 잔여 AN 데이터 수신 완료 대기
                    ClearCommBuffer(uart, logAction);
                    Thread.Sleep(200);
                    ClearCommBuffer(uart, logAction);  // 2차 클리어
                    SendCommand(uart, "<MODE,1,0>", param, logAction);
                    Thread.Sleep(200);
                    ReadStep(uart, 500, logAction, out _, out _);
                    ClearCommBuffer(uart, logAction);  // 최종 클리어
                }
                else
                {
                    SendCommand(uart, "<STOP>", param, logAction);
                    Thread.Sleep(100);
                    SendCommand(uart, "<MODE,0>", param, logAction);
                    ReadStep(uart, 500, logAction, out _, out _);
                }

                // 6) AN 파싱 — 스트리밍 수집된 AN 샘플 평균으로 0도 검증
                // 232: [AN,X,Y] → X=parts[1], Y=parts[2]
                // 485: [AN,ID,X,Y] → X=parts[2], Y=parts[3] (ID 필드 1칸 offset)
                double anValue = double.NaN;
                int anCount = 0;
                double anSum = 0;
                foreach (var frame in ExtractBracketFrames(anCollected))
                {
                    var parts = SplitBracketResponse(frame);
                    if (parts.Length >= 3 && parts[0].Equals("AN", StringComparison.OrdinalIgnoreCase))
                    {
                        int offset = isRs485 ? 1 : 0;
                        int idx = (anAxis == "X" ? 1 : 2) + offset;
                        if (idx < parts.Length && double.TryParse(parts[idx], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v))
                        {
                            anSum += v;
                            anCount++;
                        }
                    }
                }

                if (anCount > 0)
                {
                    anValue = anSum / anCount;
                    bool anOk = anValue >= anMin && anValue <= anMax;
                    logAction($"[CAL] OFFSET 검증: AN {anAxis}={anValue:F3} [{anMin}~{anMax}] {(anOk ? "OK" : "NG")} ({anCount}샘플)");
                    steps.Add(new Dictionary<string, object?>
                    {
                        { "command", "verify_an" },
                        { "verify_an_value", Math.Round(anValue, 3) },
                        { "_verify_an_desc", $"VL센서 AN스트리밍 약1초 {anCount}샘플 평균 (0도 기준)" },
                        { "gyro_spec_min", anMin },
                        { "gyro_spec_max", anMax },
                        { "result", anOk ? "OK" : "NG" }
                    });
                    if (!anOk) ok = false;
                }
                else
                {
                    logAction("[CAL] OFFSET 검증 실패: AN 데이터 없음");
                    ok = false;
                }
            }

            return MakeResult(ok, procId, procName, steps, ok ? "OK" : "NG");
        }

        // ──────────────────────────────────────────────
        //  SENTEMP_Internal : 센서온도 검사
        // ──────────────────────────────────────────────

        private IDictionary<string, object?> SENTEMP_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, string defaultProcId, string defaultProcName)
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            bool isRs485 = uartKey.Contains("485");
            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;
            int readTimeoutMs = GetParamInt(param, "TIMEOUT", 5000);
            double tempMin = GetParamDouble(param, "min", 20);
            double tempMax = GetParamDouble(param, "max", 40);

            var details = new List<Dictionary<string, object?>>();
            bool ok = false;

            ClearCommBuffer(uart, logAction);

            var stepList = GetStepList(param).ToList();
            if (stepList.Count == 0)
            {
                logAction("[SENTEMP] steps/tests 배열이 비어있습니다.");
            }
            else
            {
                foreach (var t in stepList)
                {
                    string command = GetTestString(t, "command");
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    if (!SendCommand(uart, command, param, logAction))
                        continue;

                    ReadStep(uart, readTimeoutMs, logAction, out var stepCollected, out var stepLast);

                    // 응답 형식: RS-232: [SENTEMP,0,25.3] / RS-485: [SENTEMP,0,1,25.3]
                    if (!string.IsNullOrWhiteSpace(stepLast) && IsBracketResponse(stepLast))
                    {
                        var parts = SplitBracketResponse(stepLast);
                        bool parsed = false;
                        double temp = 0;

                        int minLen = isRs485 ? 4 : 3;
                        if (parts.Length >= minLen &&
                            parts[0].Trim().Equals("SENTEMP", StringComparison.OrdinalIgnoreCase))
                        {
                            if (TryGetRvCode(stepLast, out int rv) && rv == 0)
                            {
                                int tempIndex = isRs485 ? 3 : 2;
                                string tempText = parts[tempIndex].Trim();
                                parsed =
                                    double.TryParse(tempText, NumberStyles.Float, CultureInfo.InvariantCulture, out temp) ||
                                    double.TryParse(tempText, NumberStyles.Float, CultureInfo.CurrentCulture, out temp);
                            }
                            else
                            {
                                logAction($"[SENTEMP] RV 오류: {rv} ({GetRvDescription(rv)})");
                            }
                        }

                        if (!parsed)
                        {
                            // 예외 포맷 대응: 마지막 숫자를 온도로 시도
                            var m = Regex.Matches(stepLast, @"[-+]?\d+(?:[.,]\d+)?");
                            if (m.Count > 0)
                            {
                                string lastNum = m[m.Count - 1].Value;
                                parsed =
                                    double.TryParse(lastNum, NumberStyles.Float, CultureInfo.InvariantCulture, out temp) ||
                                    double.TryParse(lastNum, NumberStyles.Float, CultureInfo.CurrentCulture, out temp);
                            }
                        }

                        if (parsed)
                        {
                            ok = temp >= tempMin && temp <= tempMax;
                            logAction($"[SENTEMP] 온도: {temp:F3} (범위: {tempMin:F3}~{tempMax:F3}) → {(ok ? "OK" : "NG")}");
                            details.Add(new Dictionary<string, object?>
                            {
                                { "item", "센서온도" },
                                { "value", Math.Round(temp, 3) },
                                { "min", Math.Round(tempMin, 3) },
                                { "max", Math.Round(tempMax, 3) },
                                { "result", ok ? "OK" : "NG" }
                            });
                        }
                        else
                        {
                            logAction($"[SENTEMP] 응답 파싱 실패: {stepLast}");
                        }
                    }
                    else
                    {
                        logAction($"[SENTEMP] 응답 형식 오류(대괄호 필요): {stepLast}");
                    }
                }
            }

            if (details.Count == 0)
            {
                details.Add(new Dictionary<string, object?>
                {
                    { "item", "센서온도" },
                    { "value", null },
                    { "result", "NG" }
                });
            }

            return MakeResult(ok, procId, procName, details, ok ? "OK" : "NG");
        }

        // ──────────────────────────────────────────────
        //  REFTEMP_Internal : 센서온도 저장
        //   ① SENTEMP 읽기 → ② 범위 가드(20~40 밖이면 저장 안 함, FAIL)
        //   → ③ REFTEMP,<읽은온도> 저장 → RV0 + echo(보낸값==응답값) 일치 PASS
        //   tests[0]=SENTEMP 명령, tests[1]=REFTEMP 명령(템플릿 {temp} 치환)
        // ──────────────────────────────────────────────

        private IDictionary<string, object?> REFTEMP_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, string defaultProcId, string defaultProcName)
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            bool isRs485 = uartKey.Contains("485");
            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;
            int readTimeoutMs = GetParamInt(param, "TIMEOUT", 5000);
            double tempMin = GetParamDouble(param, "min", 20);
            double tempMax = GetParamDouble(param, "max", 40);
            int tempIndex = isRs485 ? 3 : 2;   // [SENTEMP,0,1,XX.XX] / [SENTEMP,0,XX.XX]

            var details = new List<Dictionary<string, object?>>();
            bool ok = false;

            ClearCommBuffer(uart, logAction);

            var stepList = GetStepList(param).ToList();
            string senCmd = stepList.Count > 0 ? GetTestString(stepList[0], "command") : "";
            string refCmdTemplate = stepList.Count > 1 ? GetTestString(stepList[1], "command") : "";

            if (string.IsNullOrWhiteSpace(senCmd) || string.IsNullOrWhiteSpace(refCmdTemplate))
            {
                logAction("[REFTEMP] tests 배열에 SENTEMP/REFTEMP 명령 2개가 필요합니다.");
                details.Add(new Dictionary<string, object?> { { "item", "센서온도 저장" }, { "value", null }, { "result", "NG" } });
                return MakeResult(false, procId, procName, details, "NG");
            }

            // ── ① SENTEMP 읽기 ──
            string? tempText = null;
            double temp = 0;
            if (SendCommand(uart, senCmd, param, logAction))
            {
                ReadStep(uart, readTimeoutMs, logAction, out var collected, out var last);
                string merged = collected.Replace("\n", "").Replace("\r", "");
                var frames = ExtractBracketFrames(merged);
                string lastFrame = frames.Count > 0 ? frames[frames.Count - 1] : last;
                if (IsBracketResponse(lastFrame))
                {
                    var parts = SplitBracketResponse(lastFrame);
                    if (parts.Length > tempIndex &&
                        parts[0].Trim().Equals("SENTEMP", StringComparison.OrdinalIgnoreCase) &&
                        TryGetRvCode(lastFrame, out int rv) && rv == 0)
                    {
                        string t = parts[tempIndex].Trim();
                        if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out temp) ||
                            double.TryParse(t, NumberStyles.Float, CultureInfo.CurrentCulture, out temp))
                            tempText = t;
                        else
                            logAction($"[REFTEMP] 온도 파싱 실패: {lastFrame}");
                    }
                    else
                    {
                        logAction($"[REFTEMP] SENTEMP 응답 오류: {lastFrame}");
                    }
                }
                else
                {
                    logAction($"[REFTEMP] SENTEMP 응답 형식 오류(대괄호 필요): {lastFrame}");
                }
            }

            if (tempText == null)
            {
                logAction("[REFTEMP] 센서온도 읽기 실패 → FAIL (저장 안 함)");
                details.Add(new Dictionary<string, object?> { { "item", "센서온도 읽기" }, { "value", null }, { "result", "NG" } });
                return MakeResult(false, procId, procName, details, "NG");
            }

            // ── ② 범위 가드 (20~40 밖이면 저장 안 함) ──
            if (temp < tempMin || temp > tempMax)
            {
                logAction($"[REFTEMP] 센서온도 {temp:F2} 범위 밖 ({tempMin:F0}~{tempMax:F0}) → 저장 안 함, FAIL");
                details.Add(new Dictionary<string, object?>
                {
                    { "item", "센서온도 저장" },
                    { "value", tempText },
                    { "min", tempMin }, { "max", tempMax },
                    { "_desc", "범위 밖이라 REFTEMP 저장 차단(보정 baseline 오염 방지)" },
                    { "result", "NG" }
                });
                return MakeResult(false, procId, procName, details, "NG");
            }
            logAction($"[REFTEMP] 센서온도 읽기 OK: {tempText} (범위 {tempMin:F0}~{tempMax:F0})");

            // ── ③ REFTEMP 저장 ({temp} 치환) + echo 확인 ──
            string refCmd = refCmdTemplate.Replace("{temp}", tempText);
            if (SendCommand(uart, refCmd, param, logAction))
            {
                ReadStep(uart, readTimeoutMs, logAction, out var collected, out var last);
                string merged = collected.Replace("\n", "").Replace("\r", "");
                var frames = ExtractBracketFrames(merged);
                string lastFrame = frames.Count > 0 ? frames[frames.Count - 1] : last;
                if (IsBracketResponse(lastFrame))
                {
                    var parts = SplitBracketResponse(lastFrame);
                    bool cmdOk = parts.Length > tempIndex &&
                                 parts[0].Trim().Equals("REFTEMP", StringComparison.OrdinalIgnoreCase) &&
                                 TryGetRvCode(lastFrame, out int rv) && rv == 0;
                    string echoTemp = (parts.Length > tempIndex) ? parts[tempIndex].Trim() : "";
                    bool echoOk = string.Equals(echoTemp, tempText, StringComparison.Ordinal);
                    ok = cmdOk && echoOk;
                    logAction($"[REFTEMP] 저장 응답: {lastFrame} (echo={echoTemp}, 보낸값={tempText}) → {(ok ? "OK" : "NG")}");
                    details.Add(new Dictionary<string, object?>
                    {
                        { "item", "센서온도 저장" },
                        { "value", tempText },
                        { "echo", echoTemp },
                        { "result", ok ? "OK" : "NG" }
                    });
                }
                else
                {
                    logAction($"[REFTEMP] 저장 응답 형식 오류(대괄호 필요): {lastFrame}");
                    details.Add(new Dictionary<string, object?> { { "item", "센서온도 저장" }, { "value", tempText }, { "result", "NG" } });
                }
            }
            else
            {
                logAction("[REFTEMP] REFTEMP 송신 실패");
                details.Add(new Dictionary<string, object?> { { "item", "센서온도 저장" }, { "value", tempText }, { "result", "NG" } });
            }

            return MakeResult(ok, procId, procName, details, ok ? "OK" : "NG");
        }

        // ──────────────────────────────────────────────
        //  APPCFG_Internal : Config 데이터 확인
        // ──────────────────────────────────────────────

        private IDictionary<string, object?> APPCFG_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, string defaultProcId, string defaultProcName)
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;
            int readTimeoutMs = GetParamInt(param, "TIMEOUT", 5000);

            // RS-485 여부 판단
            bool isRs485 = uartKey.Contains("485");

            var details = new List<Dictionary<string, object?>>();
            bool ok = true;
            bool sawMode0Ok = false;
            bool sawAppCfgOk = false;

            ClearCommBuffer(uart, logAction);

            var stepList = GetStepList(param).ToList();
            if (stepList.Count == 0)
            {
                logAction("[APPCFG] steps/tests 배열이 비어있습니다.");
            }
            else
            {
                foreach (var t in stepList)
                {
                    string command = GetTestString(t, "command");
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    if (!SendCommand(uart, command, param, logAction))
                    {
                        ok = false;
                        continue;
                    }

                    ReadStep(uart, readTimeoutMs, logAction, out var stepCollected, out var stepLast);

                    string expected = GetTestString(t, "expected_response");
                    string cmdToken = NormalizeCommandToken(command);
                    string frame = stepLast;
                    var frames = ExtractBracketFrames(stepCollected);
                    if (frames.Count > 0)
                    {
                        var matched = frames.FirstOrDefault(f =>
                        {
                            var p = SplitBracketResponse(f);
                            return p.Length > 0 && p[0].Trim().Equals(cmdToken, StringComparison.OrdinalIgnoreCase);
                        });
                        if (!string.IsNullOrWhiteSpace(matched))
                            frame = matched;
                    }

                    bool stepOk = IsBracketResponse(frame);
                    if (!stepOk)
                    {
                        logAction($"[APPCFG] 응답 형식 오류(대괄호 필요): {frame}");
                        ok = false;
                    }
                    else
                    {
                        var parts = SplitBracketResponse(frame);
                        if (parts.Length == 0 || !parts[0].Trim().Equals(cmdToken, StringComparison.OrdinalIgnoreCase))
                        {
                            logAction($"[APPCFG] 응답 명령 불일치: {frame}");
                            ok = false;
                            stepOk = false;
                        }
                        else if (!TryGetRvCode(frame, out int rv) || rv != 0)
                        {
                            logAction($"[APPCFG] RV 오류: {rv} ({GetRvDescription(rv)})");
                            ok = false;
                            stepOk = false;
                        }
                    }

                    // APPCFG expected 완전 일치 (대소문자 구분, 부분 포함 불허)
                    bool expectedOk = true;
                    if (!string.IsNullOrWhiteSpace(expected))
                    {
                        expectedOk = string.Equals(frame, expected, StringComparison.Ordinal);
                        if (!expectedOk)
                        {
                            logAction($"[APPCFG] 응답 불일치: 기대=[{expected}], 실제=[{frame}]");
                            ok = false;
                            stepOk = false;
                        }
                    }

                    // MODE,0 단계는 응답 본문 00까지 추가 확인
                    if (cmdToken == "MODE")
                    {
                        var parts = IsBracketResponse(frame) ? SplitBracketResponse(frame) : Array.Empty<string>();
                        int modeIndex = isRs485 ? 3 : 2;
                        int minLen = isRs485 ? 4 : 3;
                        bool mode0Ok = parts.Length >= minLen && parts[modeIndex].Trim() == "00";
                        if (!mode0Ok)
                        {
                            logAction($"[APPCFG] MODE,0 확인 실패: {frame}");
                            ok = false;
                            stepOk = false;
                        }
                    }

                    if (stepOk)
                    {
                        if (cmdToken == "MODE")
                            sawMode0Ok = true;
                        if (cmdToken == "APPCFG" && expectedOk)
                            sawAppCfgOk = true;
                    }

                    ok = ok && stepOk;

                    if (LogConfig.IsTest) logAction($"[APPCFG] expected={expected}, result={frame} → {(stepOk ? "OK" : "NG")}");
                    details.Add(new Dictionary<string, object?>
                    {
                        { "item", cmdToken == "MODE" ? "MODE,0" : "Config" },
                        { "value", frame },
                        { "expected", expected },
                        { "result", stepOk ? "OK" : "NG" }
                    });
                }
            }

            ok = ok && sawMode0Ok && sawAppCfgOk;

            if (details.Count == 0)
            {
                details.Add(new Dictionary<string, object?>
                {
                    { "item", "Config" },
                    { "value", null },
                    { "result", "NG" }
                });
            }

            return MakeResult(ok, procId, procName, details, ok ? "OK" : "NG");
        }

        // ──────────────────────────────────────────────
        //  VIBE_Internal : 진동검사 (tests 배열 기반 시퀀스)
        // ──────────────────────────────────────────────

        private IDictionary<string, object?> VIBE_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, bool isRs485,
            string defaultProcId, string defaultProcName,
            Func<string, bool>? confirmCallback)
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;
            int overallTimeoutMs = GetParamInt(param, "TIMEOUT", 15000);
            if (overallTimeoutMs < 1000) overallTimeoutMs = 1000;
            int stepReadTimeoutMs = Math.Min(overallTimeoutMs, 300);
            if (stepReadTimeoutMs < 50) stepReadTimeoutMs = 50;
            int samplingMs = GetParamInt(param, "sampling_ms", 10);
            if (samplingMs < 1) samplingMs = 10;
            int sampleLogIntervalMs = GetParamInt(param, "sample_log_interval_ms", 500);
            if (sampleLogIntervalMs < 0) sampleLogIntervalMs = 500;
            int passCandidateLogIntervalMs = GetParamInt(param, "pass_candidate_log_interval_ms", 500);
            if (passCandidateLogIntervalMs < 0) passCandidateLogIntervalMs = 500;
            int passMinElapsedMs = GetParamInt(param, "pass_min_elapsed_ms", 1000);
            if (passMinElapsedMs < 0) passMinElapsedMs = 0;
            int passConsecutiveCount = GetParamInt(param, "pass_consecutive_count", 5);
            if (passConsecutiveCount < 1) passConsecutiveCount = 1;
            bool requireMotion = GetParamBool(param, "require_motion", true);
            double motionDetectMargin = GetParamDouble(param, "motion_detect_margin", 0.03);
            if (motionDetectMargin < 0) motionDetectMargin = 0.03;
            double oneMin = GetParamDouble(param, "one_min", 0.9);
            double oneMax = GetParamDouble(param, "one_max", 1.1);
            double zeroMin = GetParamDouble(param, "zero_min", -0.1);
            double zeroMax = GetParamDouble(param, "zero_max", 0.1);
            double rmsMin = GetParamDouble(param, "rms_min", 0.0);
            double rmsMax = GetParamDouble(param, "rms_max", 999.0);
            bool checkRms = param.ContainsKey("rms_min") || param.ContainsKey("rms_max");

            var details = new List<Dictionary<string, object?>>();
            bool ok = false;

            ClearCommBuffer(uart, logAction);

            string operatorPrompt = GetParamString(param, "operator_prompt", "");
            if (string.IsNullOrWhiteSpace(operatorPrompt))
            {
                operatorPrompt =
                    "지그를 앞/뒤 좌/우로 움직여주세요.\n" +
                    $"PASS 조건: 1축 {oneMin}~{oneMax}, 0축 {zeroMin}~{zeroMax}, RMS {rmsMin}~{rmsMax}\n" +
                    $"최대 대기시간: {overallTimeoutMs / 1000.0:F1}초\n" +
                    "확인(Enter/Space) 시 측정을 시작합니다.";
            }

            // 시퀀스 실행
            var stepList = GetStepList(param).ToList();
            if (stepList.Count == 0)
            {
                logAction("[VIBE] steps/tests 배열이 비어있습니다.");
            }
            else
            {
                var samples = new List<double[]>(); // [X, Y, Z, RMS]
                var latestSampleDetails = new List<Dictionary<string, object?>>();
                DateTime measureStartUtc = DateTime.UtcNow;
                bool startIssued = false;
                int passStreak = 0;
                bool motionDetected = false;
                DateTime nextSampleLogUtc = DateTime.MinValue;
                DateTime nextPassWaitLogUtc = DateTime.MinValue;
                DateTime nextPassMotionLogUtc = DateTime.MinValue;
                double motionZeroThreshold = param.ContainsKey("motion_threshold")
                    ? GetParamDouble(param, "motion_threshold", 0.05)
                    : Math.Max(Math.Abs(zeroMin), Math.Abs(zeroMax)) + motionDetectMargin;

                try
                {
                    // GAC 명령 미리 추출 (START 이후에 위치하더라도 찾을 수 있도록 전체 스캔)
                    string? gacCommand = stepList
                        .Select(t => GetTestString(t, "command"))
                        .FirstOrDefault(cmd => NormalizeCommandToken(cmd) == "GAC");

                    // 초기화 명령들: workspace.json tests 배열에서 읽기 (START까지 실행)
                    foreach (var t in stepList)
                    {
                        string command = GetTestString(t, "command");
                        string token = NormalizeCommandToken(command);

                        if (token == "GAC")
                            break; // 데이터 수집 마커, while 루프에서 처리

                        if (token == "AN")
                            break; // 데이터 수집 마커, while 루프에서 처리

                        if (string.IsNullOrWhiteSpace(command))
                            continue;

                        if (token == "START")
                        {
                            // START 직전에 작업자 확인 팝업 표시
                            if (!string.IsNullOrWhiteSpace(operatorPrompt))
                            {
                                if (confirmCallback != null)
                                {
                                    bool confirmed;
                                    try
                                    {
                                        confirmed = confirmCallback(operatorPrompt);
                                    }
                                    catch (Exception ex)
                                    {
                                        logAction($"[VIBE] 작업자 확인 팝업 오류: {ex.Message}");
                                        confirmed = false;
                                    }

                                    if (!confirmed)
                                    {
                                        logAction("[VIBE] 작업자 확인이 취소되어 진동검사를 중단합니다.");
                                        details.Add(new Dictionary<string, object?>
                                        {
                                            { "item", "작업자 확인" },
                                            { "value", "취소됨" },
                                            { "result", "NG" }
                                        });
                                        return MakeResult(false, procId, procName, details, "NG");
                                    }
                                }
                                else
                                {
                                    logAction("[VIBE] confirmCallback 미지정: 작업자 확인 없이 진동검사를 시작합니다.");
                                }
                            }

                            // START 명령 실행
                            SendCommand(uart, command, param, logAction);
                            startIssued = true;
                            System.Threading.Thread.Sleep(100); // 자동 스트림 시작 대기
                            break; // START 후 즉시 while 루프로 (데이터 수집)
                        }

                        SendCommand(uart, command, param, logAction);

                        // 각 명령 후 응답 읽기
                        ReadStep(uart, 500, logAction, out var initCollected, out var initLast);
                        System.Threading.Thread.Sleep(200);
                    }

                    // GAC 데이터 수집: PASS 조건 만족 시 즉시 합격, 아니면 TIMEOUT까지 대기
                    measureStartUtc = DateTime.UtcNow;
                    var deadline = measureStartUtc.AddMilliseconds(overallTimeoutMs);

                    while (DateTime.UtcNow < deadline && !ok)
                    {
                        if (!string.IsNullOrEmpty(gacCommand))
                        {
                            // RS-232/RS-485 공통: GAC 폴링 방식
                            SendCommand(uart, gacCommand, param, logAction);
                        }
                        ReadStep(uart, stepReadTimeoutMs, logAction, out var stepCollected, out var stepLast);

                        foreach (var frame in ExtractBracketFrames(stepCollected))
                        {
                            if (!TryParseGacSample(frame, isRs485, logAction, out double x, out double y, out double z, out double rms))
                                continue;

                            samples.Add(new double[] { x, y, z, rms });
                            var nowUtc = DateTime.UtcNow;
                            if (sampleLogIntervalMs == 0 || nowUtc >= nextSampleLogUtc)
                            {
                                if (LogConfig.IsTest) logAction($"[VIBE] GAC: X={x:F4}, Y={y:F4}, Z={z:F4}, RMS={rms:F4}");
                                nextSampleLogUtc = nowUtc.AddMilliseconds(sampleLogIntervalMs);
                            }

                            // 움직임 감지: 중력축이 아닌 축이 허용 0축 범위를 일정 마진 이상 벗어나는지 확인
                            if (!motionDetected)
                            {
                                double[] axes = { x, y, z };
                                int gravityAxis = 0;
                                double gravityAbs = Math.Abs(axes[0]);
                                for (int ai = 1; ai < 3; ai++)
                                {
                                    var a = Math.Abs(axes[ai]);
                                    if (a > gravityAbs)
                                    {
                                        gravityAbs = a;
                                        gravityAxis = ai;
                                    }
                                }

                                for (int ai = 0; ai < 3; ai++)
                                {
                                    if (ai == gravityAxis) continue;
                                    if (Math.Abs(axes[ai]) > motionZeroThreshold)
                                    {
                                        motionDetected = true;
                                        logAction($"[VIBE] 모션 감지: axis={ai}, value={axes[ai]:F4}, threshold={motionZeroThreshold:F4}");
                                        break;
                                    }
                                }
                            }

                            bool samplePass = EvaluateVibeSample(
                                x, y, z, rms,
                                oneMin, oneMax, zeroMin, zeroMax,
                                rmsMin, rmsMax, checkRms,
                                out var sampleDetails);

                            latestSampleDetails = sampleDetails;
                            int elapsedMs = (int)(DateTime.UtcNow - measureStartUtc).TotalMilliseconds;

                            bool passEligible = samplePass &&
                                elapsedMs >= passMinElapsedMs &&
                                (!requireMotion || motionDetected);

                            if (passEligible)
                            {
                                passStreak++;
                            }
                            else
                            {
                                passStreak = 0;
                            }

                            if (samplePass && elapsedMs < passMinElapsedMs)
                            {
                                if (passCandidateLogIntervalMs == 0 || nowUtc >= nextPassWaitLogUtc)
                                {
                                    if (LogConfig.IsTest) logAction($"[VIBE] PASS 후보 샘플(대기중): {elapsedMs}ms/{passMinElapsedMs}ms");
                                    nextPassWaitLogUtc = nowUtc.AddMilliseconds(passCandidateLogIntervalMs);
                                }
                            }
                            else if (samplePass && requireMotion && !motionDetected)
                            {
                                if (passCandidateLogIntervalMs == 0 || nowUtc >= nextPassMotionLogUtc)
                                {
                                    if (LogConfig.IsTest) logAction("[VIBE] PASS 후보 샘플(모션 미감지): 흔들림 감지 대기중");
                                    nextPassMotionLogUtc = nowUtc.AddMilliseconds(passCandidateLogIntervalMs);
                                }
                            }

                            if (passStreak >= passConsecutiveCount)
                            {
                                ok = true;
                                details.AddRange(sampleDetails);
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "샘플수" },
                                    { "value", samples.Count },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "판정시점(ms)" },
                                    { "value", elapsedMs },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "연속 PASS 샘플" },
                                    { "value", passStreak },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "모션 감지" },
                                    { "value", motionDetected },
                                    { "result", "INFO" }
                                });
                                // 측정값 |v|>0.5 → 1축(중력축), 그 외 → 0축. 판정 로직(L365)과 동일 분기로 라벨/spec 매핑
                                string xLabel = Math.Abs(x) > 0.5 ? "1축" : "0축";
                                string yLabel = Math.Abs(y) > 0.5 ? "1축" : "0축";
                                string zLabel = Math.Abs(z) > 0.5 ? "1축" : "0축";
                                double xMin = Math.Abs(x) > 0.5 ? oneMin : zeroMin, xMax = Math.Abs(x) > 0.5 ? oneMax : zeroMax;
                                double yMin = Math.Abs(y) > 0.5 ? oneMin : zeroMin, yMax = Math.Abs(y) > 0.5 ? oneMax : zeroMax;
                                double zMin = Math.Abs(z) > 0.5 ? oneMin : zeroMin, zMax = Math.Abs(z) > 0.5 ? oneMax : zeroMax;
                                logAction($"[VIBE] PASS X={x:F2}[{xLabel}:{xMin}~{xMax}] Y={y:F2}[{yLabel}:{yMin}~{yMax}] Z={z:F2}[{zLabel}:{zMin}~{zMax}] RMS={rms:F2}[{rmsMin}~{rmsMax}]");
                                break;
                            }
                        }

                        System.Threading.Thread.Sleep(10);
                    }
                }
                finally
                {
                    // 종료 명령들: START 이후 명령들을 종료로 실행 (STOP, MODE,0 등)
                    if (LogConfig.IsTest) logAction("[VIBE] finally 블록 시작 - 종료 명령 실행");
                    bool afterStart = false;
                    foreach (var t in stepList)
                    {
                        string command = GetTestString(t, "command");
                        string token = NormalizeCommandToken(command);

                        if (token == "START") { afterStart = true; continue; }
                        if (token == "GAC") { continue; }  // 폴링 구분자는 스킵 (하위 호환)

                        if (afterStart)
                        {
                            // sleep 처리
                            int sleepMs = GetTestInt(t, "sleep", 0);
                            if (sleepMs > 0)
                            {
                                logAction($"[VIBE] Sleeping {sleepMs}ms...");
                                System.Threading.Thread.Sleep(sleepMs);
                                continue;
                            }

                            // command 처리
                            if (!string.IsNullOrWhiteSpace(command))
                            {
                                try
                                {
                                    SendCommand(uart, command, param, logAction);
                                    ReadStep(uart, 500, logAction, out var _, out var _);
                                    System.Threading.Thread.Sleep(200);

                                    // STOP 명령인 경우 스트리밍이 실제로 멈췄는지 확인
                                    if (token == "STOP")
                                    {
                                        if (LogConfig.IsTest) logAction("[VIBE] STOP 후 버퍼 클리어");
                                        ClearCommBuffer(uart, logAction);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logAction($"[VIBE] 종료 명령 실패: {command} - {ex.Message}");
                                }
                            }
                        }
                    }
                    if (LogConfig.IsTest) logAction("[VIBE] finally 블록 종료 명령 완료");
                }

                if (!ok)
                {
                    if (samples.Count == 0)
                    {
                        logAction("[VIBE] 진동 데이터 수집 실패 (TIMEOUT)");
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "진동데이터" },
                            { "value", "no samples" },
                            { "result", "NG" }
                        });
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "실패사유" },
                            { "value", $"TIMEOUT({overallTimeoutMs}ms)" },
                            { "result", "NG" }
                        });
                    }
                    else
                    {
                        double avgX = samples.Average(s => s[0]);
                        double avgY = samples.Average(s => s[1]);
                        double avgZ = samples.Average(s => s[2]);
                        double avgRms = samples.Average(s => s[3]);
                        // 측정값 |v|>0.5 → 1축(중력축), 그 외 → 0축. 판정 로직(L365)과 동일 분기로 라벨/spec 매핑
                        string xLabelF = Math.Abs(avgX) > 0.5 ? "1축" : "0축";
                        string yLabelF = Math.Abs(avgY) > 0.5 ? "1축" : "0축";
                        string zLabelF = Math.Abs(avgZ) > 0.5 ? "1축" : "0축";
                        double xMinF = Math.Abs(avgX) > 0.5 ? oneMin : zeroMin, xMaxF = Math.Abs(avgX) > 0.5 ? oneMax : zeroMax;
                        double yMinF = Math.Abs(avgY) > 0.5 ? oneMin : zeroMin, yMaxF = Math.Abs(avgY) > 0.5 ? oneMax : zeroMax;
                        double zMinF = Math.Abs(avgZ) > 0.5 ? oneMin : zeroMin, zMaxF = Math.Abs(avgZ) > 0.5 ? oneMax : zeroMax;
                        logAction($"[VIBE] FAIL X={avgX:F2}[{xLabelF}:{xMinF}~{xMaxF}] Y={avgY:F2}[{yLabelF}:{yMinF}~{yMaxF}] Z={avgZ:F2}[{zLabelF}:{zMinF}~{zMaxF}] RMS={avgRms:F2}[{rmsMin}~{rmsMax}] (TIMEOUT {overallTimeoutMs}ms)");
                        details.AddRange(latestSampleDetails);
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "샘플수" },
                            { "value", samples.Count },
                            { "result", "INFO" }
                        });
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "모션 감지" },
                            { "value", motionDetected },
                            { "result", "INFO" }
                        });
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "실패사유" },
                            { "value", requireMotion && !motionDetected
                                ? $"모션 미감지 (TIMEOUT {overallTimeoutMs}ms)"
                                : $"PASS 조건 미충족 (TIMEOUT {overallTimeoutMs}ms)" },
                            { "result", "NG" }
                        });
                    }
                }
            }

            return MakeResult(ok, procId, procName, details, ok ? "OK" : "NG");
        }

        // ──────────────────────────────────────────────
        //  VIBE_STILL_Internal : 정지(rest) 안정성 검사
        //  목적: 정지 상태에서 센서가 출렁이는(노이즈성) 불량 검출.
        //  특징: require_motion 없음(가만히 둠) / retry 없음 / 전 구간 / 평균 아님(RMS·p2p)
        //  임계(still_rms_max, still_p2p_max) · duration_ms · measure_only 전부 JSON 파라미터, 프로토콜은 tests 배열
        // ──────────────────────────────────────────────
        private IDictionary<string, object?> VIBE_STILL_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, bool isRs485,
            string defaultProcId, string defaultProcName,
            Func<string, bool>? confirmCallback)
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;

            int durationMs = GetParamInt(param, "duration_ms", 1000);
            if (durationMs < 100) durationMs = 100;
            int stepReadTimeoutMs = Math.Max(50, Math.Min(durationMs, 300));

            // 측정 전용 모드: true면 측정·로깅만 하고 항상 PASS (임계 확정 전 안전 운용)
            bool measureOnly = GetParamBool(param, "measure_only", true);
            double rmsMax = GetParamDouble(param, "still_rms_max", 999.0);
            double p2pMax = GetParamDouble(param, "still_p2p_max", 999.0);
            bool checkRms = param.ContainsKey("still_rms_max");
            bool checkP2p = param.ContainsKey("still_p2p_max");

            var details = new List<Dictionary<string, object?>>();
            var samples = new List<double[]>(); // [X, Y, Z, RMS]

            // 정지 상태 자동 측정 — 작업자 팝업 없음 (보드가 이미 정지 상태이므로 확인 불필요)
            ClearCommBuffer(uart, logAction);

            var stepList = GetStepList(param).ToList();
            string? gacCommand = stepList
                .Select(t => GetTestString(t, "command"))
                .FirstOrDefault(cmd => NormalizeCommandToken(cmd) == "GAC");

            try
            {
                // 초기화 명령(START까지) — tests 배열에서 (MODE 02 등). 프로토콜은 JSON(하드코딩 X)
                foreach (var t in stepList)
                {
                    string command = GetTestString(t, "command");
                    string token = NormalizeCommandToken(command);
                    if (token == "GAC" || token == "AN") break;
                    if (string.IsNullOrWhiteSpace(command)) continue;
                    if (token == "START")
                    {
                        SendCommand(uart, command, param, logAction);
                        System.Threading.Thread.Sleep(100);
                        break;
                    }
                    SendCommand(uart, command, param, logAction);
                    ReadStep(uart, 500, logAction, out var _, out var _);
                    System.Threading.Thread.Sleep(200);
                }

                // 정지 수집: duration 동안 전 구간 수집 (PASS 조기종료 없음, 모션 요구 없음)
                var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);
                DateTime nextLogUtc = DateTime.MinValue;
                while (DateTime.UtcNow < deadline)
                {
                    if (!string.IsNullOrEmpty(gacCommand))
                        SendCommand(uart, gacCommand, param, logAction);
                    ReadStep(uart, stepReadTimeoutMs, logAction, out var stepCollected, out var _);
                    foreach (var frame in ExtractBracketFrames(stepCollected))
                    {
                        if (!TryParseGacSample(frame, isRs485, logAction, out double x, out double y, out double z, out double rms))
                            continue;
                        samples.Add(new double[] { x, y, z, rms });
                        var nowUtc = DateTime.UtcNow;
                        if (LogConfig.IsTest && nowUtc >= nextLogUtc)
                        {
                            logAction($"[STILL] GAC: X={x:F4}, Y={y:F4}, Z={z:F4}, RMS={rms:F4}");
                            nextLogUtc = nowUtc.AddMilliseconds(500);
                        }
                    }
                    System.Threading.Thread.Sleep(10);
                }
            }
            finally
            {
                // 종료 명령(STOP, MODE0) — tests 배열 START 이후 명령 실행
                bool afterStart = false;
                foreach (var t in stepList)
                {
                    string command = GetTestString(t, "command");
                    string token = NormalizeCommandToken(command);
                    if (token == "START") { afterStart = true; continue; }
                    if (token == "GAC") continue;
                    if (!afterStart) continue;
                    int sleepMs = GetTestInt(t, "sleep", 0);
                    if (sleepMs > 0) { System.Threading.Thread.Sleep(sleepMs); continue; }
                    if (string.IsNullOrWhiteSpace(command)) continue;
                    try
                    {
                        SendCommand(uart, command, param, logAction);
                        ReadStep(uart, 500, logAction, out var _, out var _);
                        System.Threading.Thread.Sleep(200);
                        if (token == "STOP") ClearCommBuffer(uart, logAction);
                    }
                    catch (Exception ex) { logAction($"[STILL] 종료 명령 실패: {command} - {ex.Message}"); }
                }
            }

            if (samples.Count == 0)
            {
                logAction("[STILL] 정지 데이터 수집 실패 (no samples)");
                details.Add(new Dictionary<string, object?> { { "item", "정지데이터" }, { "value", "no samples" }, { "result", "NG" } });
                return MakeResult(false, procId, procName, details, "NG");
            }

            // 통계: 평균 아님 — RMS(최대) + 0축 변동폭(p2p = max-min)
            double rmsMaxV = samples.Max(s => s[3]);
            double rmsMeanV = samples.Average(s => s[3]);
            double p2pX = samples.Max(s => s[0]) - samples.Min(s => s[0]);
            double p2pY = samples.Max(s => s[1]) - samples.Min(s => s[1]);
            double p2pZ = samples.Max(s => s[2]) - samples.Min(s => s[2]);
            // X,Y,Z 전 축 변동폭(p2p) 최대값으로 판정 (노이즈가 어느 축이든 감지). RMS는 정지/동작 무관(≈0.58)이라 판정 제외, 참고만.
            double p2pMaxV = Math.Max(p2pX, Math.Max(p2pY, p2pZ));

            bool rmsOk = !checkRms || rmsMaxV <= rmsMax;
            bool p2pOk = !checkP2p || p2pMaxV <= p2pMax;
            bool judged = !measureOnly && (checkRms || checkP2p);
            bool ok = measureOnly ? true : (rmsOk && p2pOk);

            string rmsResult = !judged ? "INFO" : (rmsOk ? "OK" : "NG");
            string p2pResult = !judged ? "INFO" : (p2pOk ? "OK" : "NG");

            details.Add(new Dictionary<string, object?>
            {
                { "item", "정지 RMS(최대)" },
                { "value", Math.Round(rmsMaxV, 4) },
                { "max", checkRms ? (object?)rmsMax : null },
                { "result", rmsResult }
            });
            details.Add(new Dictionary<string, object?>
            {
                { "item", "정지 변동폭(p2p최대)" },
                { "value", Math.Round(p2pMaxV, 4) },
                { "max", checkP2p ? (object?)p2pMax : null },
                { "result", p2pResult }
            });
            details.Add(new Dictionary<string, object?> { { "item", "RMS(평균)" }, { "value", Math.Round(rmsMeanV, 4) }, { "result", "INFO" } });
            details.Add(new Dictionary<string, object?> { { "item", "변동폭 X/Y/Z" }, { "value", $"{p2pX:F4}/{p2pY:F4}/{p2pZ:F4}" }, { "result", "INFO" } });
            details.Add(new Dictionary<string, object?> { { "item", "샘플수" }, { "value", samples.Count }, { "result", "INFO" } });
            if (measureOnly)
                details.Add(new Dictionary<string, object?> { { "item", "모드" }, { "value", "measure_only(측정만, 판정X)" }, { "result", "INFO" } });

            logAction($"[STILL] {(measureOnly ? "MEASURE" : (ok ? "PASS" : "FAIL"))} value={p2pMaxV:F4}{(checkP2p ? $" spec<={p2pMax}" : "")} (p2p X={p2pX:F4} Y={p2pY:F4} Z={p2pZ:F4}) RMS={rmsMaxV:F4}(참고) ({samples.Count}샘플)");

            return MakeResult(ok, procId, procName, details, ok ? "OK" : "NG");
        }

        // ──────────────────────────────────────────────
        //  TILT_Internal : 기울기검사 (tests 배열 기반)
        // ──────────────────────────────────────────────

        private IDictionary<string, object?> TILT_Internal(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            string uartKey, bool isRs485,
            string defaultProcId, string defaultProcName,
            Func<string, bool>? confirmCallback)
        {
            var uart = ResolveUart(libraries, param, logAction, uartKey);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            string procId = (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? defaultProcId;
            string procName = (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? defaultProcName;
            int overallTimeoutMs = GetParamInt(param, "TIMEOUT", GetParamInt(param, "DURATION", 5000));
            if (overallTimeoutMs < 1000) overallTimeoutMs = 1000;
            int readTimeoutMs = Math.Min(overallTimeoutMs, 300);
            if (readTimeoutMs < 50) readTimeoutMs = 50;
            int normalBaud = GetParamInt(param, "normal_baud", 115200);
            int tiltBaud = GetParamInt(param, "tilt_baud", normalBaud);
            int samplingDefault = tiltBaud >= 460800 ? 1 : 10;
            int samplingMs = GetParamInt(param, "sampling_ms", samplingDefault);
            if (samplingMs < 1) samplingMs = samplingDefault;
            int sampleLogIntervalMs = GetParamInt(param, "sample_log_interval_ms", 500);
            if (sampleLogIntervalMs < 0) sampleLogIntervalMs = 500;
            int passCandidateLogIntervalMs = GetParamInt(param, "pass_candidate_log_interval_ms", 500);
            if (passCandidateLogIntervalMs < 0) passCandidateLogIntervalMs = 500;
            bool aliveCheck = GetParamBool(param, "alive_check", false);
            double aliveMinDeltaX = GetParamDouble(param, "alive_min_delta_x", 0.15);
            double aliveMinDeltaY = GetParamDouble(param, "alive_min_delta_y", 0.15);
            string checkAxis = GetParamString(param, "check_axis", "xy").ToLowerInvariant();
            int passMinElapsedMs = GetParamInt(param, "pass_min_elapsed_ms", 0);
            if (passMinElapsedMs < 0) passMinElapsedMs = 0;
            int passConsecutiveCount = GetParamInt(param, "pass_consecutive_count", 1);
            if (passConsecutiveCount < 1) passConsecutiveCount = 1;
            bool requireMotion = GetParamBool(param, "require_motion", false);
            double motionDetectXSpan = GetParamDouble(param, "motion_detect_x_span", 0.20);
            double motionDetectYSpan = GetParamDouble(param, "motion_detect_y_span", 0.20);
            double xMin = GetParamDouble(param, "x_min", 0.50);
            double xMax = GetParamDouble(param, "x_max", 0.65);
            double yMin = GetParamDouble(param, "y_min", -0.05);
            double yMax = GetParamDouble(param, "y_max", 0.05);

            var details = new List<Dictionary<string, object?>>();
            bool ok = false;

            ClearCommBuffer(uart, logAction);

            string operatorPrompt = GetParamString(param, "operator_prompt", "");
            if (string.IsNullOrWhiteSpace(operatorPrompt))
            {
                operatorPrompt =
                    "기울기 검사를 시작합니다.\n" +
                    (aliveCheck
                        ? $"PASS 조건: X 변화폭 >= {aliveMinDeltaX:F2} 또는 Y 변화폭 >= {aliveMinDeltaY:F2}\n"
                        : checkAxis == "x"
                        ? $"PASS 조건: X {xMin}~{xMax}\n"
                        : checkAxis == "y"
                        ? $"PASS 조건: Y {yMin}~{yMax}\n"
                        : $"PASS 조건: X {xMin}~{xMax}, Y {yMin}~{yMax}\n") +
                    "확인(Enter/Space) 시 측정을 시작합니다.";
            }

            // 변수 선언을 try 블록 밖으로 이동 (finally에서 접근 가능하도록)
            var stepList = GetStepList(param).ToList();
            string? anCommand = null;
            var samples = new List<double[]>(); // [X, Y]
            int passStreak = 0;
            bool motionDetected = false;
            DateTime nextSampleLogUtc = DateTime.MinValue;
            DateTime nextPassWaitLogUtc = DateTime.MinValue;
            DateTime nextPassMotionLogUtc = DateTime.MinValue;
            double minXSeen = double.MaxValue;
            double maxXSeen = double.MinValue;
            double minYSeen = double.MaxValue;
            double maxYSeen = double.MinValue;
            double lastX = 0;
            double lastY = 0;
            bool hasLast = false;

            try
            {
                // AN 명령 미리 추출 (START 이후에 위치하더라도 찾을 수 있도록 전체 스캔)
                anCommand = stepList
                    .Select(t => GetTestString(t, "command"))
                    .FirstOrDefault(cmd => NormalizeCommandToken(cmd) == "AN");

                // 초기화 명령들: workspace.json tests 배열에서 읽기 (MODE,0, MODE,6, SAM, START)
                foreach (var t in stepList)
                {
                    string command = GetTestString(t, "command");
                    string token = NormalizeCommandToken(command);

                    if (token == "AN")
                        break; // 데이터 수집 마커, while 루프에서 처리

                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    if (token == "START")
                    {
                        // START 직전에 작업자 확인 팝업 표시
                        if (!string.IsNullOrWhiteSpace(operatorPrompt))
                        {
                            if (confirmCallback != null)
                            {
                                bool confirmed;
                                try
                                {
                                    confirmed = confirmCallback(operatorPrompt);
                                }
                                catch (Exception ex)
                                {
                                    logAction($"[TILT] 작업자 확인 팝업 오류: {ex.Message}");
                                    confirmed = false;
                                }

                                if (!confirmed)
                                {
                                    logAction("[TILT] 작업자 확인이 취소되어 기울기검사를 중단합니다.");
                                    details.Add(new Dictionary<string, object?>
                                    {
                                        { "item", "작업자 확인" },
                                        { "value", "취소됨" },
                                        { "result", "NG" }
                                    });
                                    return MakeResult(false, procId, procName, details, "NG");
                                }
                            }
                            else
                            {
                                logAction("[TILT] confirmCallback 미지정: 작업자 확인 없이 기울기검사를 시작합니다.");
                            }
                        }

                        // START 명령 실행
                        SendCommand(uart, command, param, logAction);
                        System.Threading.Thread.Sleep(100); // 자동 스트림 시작 대기
                        break; // START 후 즉시 while 루프로 (AN 데이터 수집)
                    }

                    SendCommand(uart, command, param, logAction);

                    // 각 명령 후 응답 읽기 (버퍼 비우기)
                    ReadStep(uart, 500, logAction, out var initCollected, out var initLast);
                    System.Threading.Thread.Sleep(200);

                    // MODE,6 실행 후 Baudrate 변경 (Motion 검사 시)
                    if (token == "MODE" && command.Contains("6"))
                    {
                        if (tiltBaud != normalBaud && !ChangeBaudRate(uart, tiltBaud, logAction))
                        {
                            details.Add(new Dictionary<string, object?>
                            {
                                { "item", "Baudrate변경" },
                                { "value", $"{tiltBaud} 변경 실패" },
                                { "result", "NG" }
                            });
                            return MakeResult(false, procId, procName, details, "NG");
                        }
                        System.Threading.Thread.Sleep(100);
                    }
                }

                // AN 데이터 수집
                DateTime measureStartUtc = DateTime.UtcNow;
                var deadline = DateTime.UtcNow.AddMilliseconds(overallTimeoutMs);

                while (DateTime.UtcNow < deadline)
                {
                    if (!string.IsNullOrEmpty(anCommand))
                    {
                        // RS-232/RS-485 공통: AN 폴링 방식
                        SendCommand(uart, anCommand, param, logAction);
                    }
                    ReadStep(uart, Math.Min(readTimeoutMs, 300), logAction, out var stepCollected, out var stepLast);

                    // 응답 파싱: RS-232: [AN,X,Y], RS-485 폴링: [AN,0,ID,X,Y], RS-485 스트리밍 호환: [AN,ID,X,Y]
                    foreach (var frame in ExtractBracketFrames(stepCollected))
                    {
                        var parts = SplitBracketResponse(frame);
                        if (parts.Length == 0 || !parts[0].Trim().Equals("AN", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // RS-485 폴링: [AN,0,ID,X,Y] (length=5, offset=3)
                        // RS-485 스트리밍 호환: [AN,ID,X,Y] (length=4, offset=2)
                        // RS-232: [AN,X,Y] (length=3, offset=1)
                        int offset, requiredLength;
                        if (!isRs485)
                        {
                            offset = 1; requiredLength = 3;
                        }
                        else if (parts.Length >= 5)
                        {
                            offset = 3; requiredLength = 5; // 폴링 포맷
                        }
                        else
                        {
                            offset = 2; requiredLength = 4; // 스트리밍 호환
                        }

                        if (parts.Length >= requiredLength &&
                            double.TryParse(parts[offset], out double x) &&
                            double.TryParse(parts[offset + 1], out double y))
                        {
                            samples.Add(new double[] { x, y });
                            var nowUtc = DateTime.UtcNow;
                            if (sampleLogIntervalMs == 0 || nowUtc >= nextSampleLogUtc)
                            {
                                if (LogConfig.IsTest) logAction($"[TILT] AN: X={x:F2}, Y={y:F2}");
                                nextSampleLogUtc = nowUtc.AddMilliseconds(sampleLogIntervalMs);
                            }

                            hasLast = true;
                            lastX = x;
                            lastY = y;

                            if (x < minXSeen) minXSeen = x;
                            if (x > maxXSeen) maxXSeen = x;
                            if (y < minYSeen) minYSeen = y;
                            if (y > maxYSeen) maxYSeen = y;

                            if (!motionDetected &&
                                (maxXSeen - minXSeen >= motionDetectXSpan || maxYSeen - minYSeen >= motionDetectYSpan))
                            {
                                motionDetected = true;
                                if (LogConfig.IsTest) logAction($"[TILT] 모션 감지: ΔX={(maxXSeen - minXSeen):F2}, ΔY={(maxYSeen - minYSeen):F2}");
                            }

                            int elapsedMs = (int)(DateTime.UtcNow - measureStartUtc).TotalMilliseconds;
                            double spanX = maxXSeen - minXSeen;
                            double spanY = maxYSeen - minYSeen;

                            bool samplePass;
                            int judgeMode = GetParamInt(param, "judge_mode", 1);
                            if (aliveCheck)
                            {
                                samplePass = (spanX >= aliveMinDeltaX || spanY >= aliveMinDeltaY);
                            }
                            else if (judgeMode >= 2)
                            {
                                // judge_mode=2: 부호 포함 판정 (절대값 사용 안 함)
                                if (checkAxis == "x")
                                    samplePass = (x >= xMin && x <= xMax);
                                else if (checkAxis == "y")
                                    samplePass = (y >= yMin && y <= yMax);
                                else
                                    samplePass = (x >= xMin && x <= xMax && y >= yMin && y <= yMax);
                            }
                            else if (checkAxis == "x")
                            {
                                samplePass = (Math.Abs(x) >= xMin && Math.Abs(x) <= xMax);
                            }
                            else if (checkAxis == "y")
                            {
                                samplePass = (Math.Abs(y) >= yMin && Math.Abs(y) <= yMax);
                            }
                            else
                            {
                                samplePass = (x >= xMin && x <= xMax && y >= yMin && y <= yMax);
                            }

                            bool passEligible = samplePass && elapsedMs >= passMinElapsedMs &&
                                (!requireMotion || motionDetected);

                            if (passEligible)
                            {
                                passStreak++;
                            }
                            else
                            {
                                passStreak = 0;
                            }

                            if (samplePass && elapsedMs < passMinElapsedMs)
                            {
                                if (passCandidateLogIntervalMs == 0 || nowUtc >= nextPassWaitLogUtc)
                                {
                                    logAction($"[TILT] PASS 후보 샘플(대기중): {elapsedMs}ms/{passMinElapsedMs}ms");
                                    nextPassWaitLogUtc = nowUtc.AddMilliseconds(passCandidateLogIntervalMs);
                                }
                            }
                            else if (samplePass && requireMotion && !motionDetected)
                            {
                                if (passCandidateLogIntervalMs == 0 || nowUtc >= nextPassMotionLogUtc)
                                {
                                    logAction("[TILT] PASS 후보 샘플(모션 미감지): 흔들림 감지 대기중");
                                    nextPassMotionLogUtc = nowUtc.AddMilliseconds(passCandidateLogIntervalMs);
                                }
                            }

                            if (passStreak >= passConsecutiveCount)
                            {
                                ok = true;
                                if (aliveCheck)
                                {
                                    details.Add(new Dictionary<string, object?>
                                    {
                                        { "item", "기울기 변화폭 X" },
                                        { "value", Math.Round(spanX, 3) },
                                        { "min", aliveMinDeltaX },
                                        { "max", null },
                                        { "result", spanX >= aliveMinDeltaX ? "OK" : "NG" }
                                    });
                                    details.Add(new Dictionary<string, object?>
                                    {
                                        { "item", "기울기 변화폭 Y" },
                                        { "value", Math.Round(spanY, 3) },
                                        { "min", aliveMinDeltaY },
                                        { "max", null },
                                        { "result", spanY >= aliveMinDeltaY ? "OK" : "NG" }
                                    });
                                }
                                else
                                {
                                    if (checkAxis != "y")
                                    {
                                        details.Add(new Dictionary<string, object?>
                                        {
                                            { "item", "기울기 X" },
                                            { "value", Math.Round(x, 3) },
                                            { "_value_desc", "절대값 판정 (부호 무시, |값|이 min~max 이내면 PASS)" },
                                            { "min", xMin },
                                            { "max", xMax },
                                            { "result", "OK" }
                                        });
                                    }
                                    if (checkAxis != "x")
                                    {
                                        details.Add(new Dictionary<string, object?>
                                        {
                                            { "item", "기울기 Y" },
                                            { "value", Math.Round(y, 3) },
                                            { "_value_desc", "절대값 판정 (부호 무시, |값|이 min~max 이내면 PASS)" },
                                            { "min", yMin },
                                            { "max", yMax },
                                            { "result", "OK" }
                                        });
                                    }
                                }
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "샘플수" },
                                    { "value", samples.Count },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "판정시점(ms)" },
                                    { "value", elapsedMs },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "연속 PASS 샘플" },
                                    { "value", passStreak },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "모션 감지" },
                                    { "value", motionDetected },
                                    { "result", "INFO" }
                                });
                                details.Add(new Dictionary<string, object?>
                                {
                                    { "item", "변화폭(ΔX/ΔY)" },
                                    { "value", $"{spanX:F2}/{spanY:F2}" },
                                    { "result", "INFO" }
                                });
                                var tiltPassMsg = checkAxis == "x" ? $"[TILT] PASS X={x:F2}[|X|:{xMin}~{xMax}]"
                                    : checkAxis == "y" ? $"[TILT] PASS Y={y:F2}[|Y|:{yMin}~{yMax}]"
                                    : $"[TILT] PASS X={x:F2}[|X|:{xMin}~{xMax}] Y={y:F2}[|Y|:{yMin}~{yMax}]";
                                logAction(tiltPassMsg);
                                break;
                            }
                        }
                    }

                    if (ok) break;
                    System.Threading.Thread.Sleep(10);
                }
            }
            finally
            {
                // 종료 명령들: START 이후 명령들을 종료로 실행 (STOP, MODE,0)
                if (LogConfig.IsTest) logAction("[TILT] finally 블록 시작 - 종료 명령 실행");
                bool afterStart = false;
                foreach (var t in stepList)
                {
                    string command = GetTestString(t, "command");
                    string token = NormalizeCommandToken(command);

                    if (token == "START") { afterStart = true; continue; }
                    if (token == "AN") { continue; }  // 폴링 구분자는 스킵 (하위 호환)

                    if (afterStart)
                    {
                        // sleep 처리
                        int sleepMs = GetTestInt(t, "sleep", 0);
                        if (sleepMs > 0)
                        {
                            logAction($"[TILT] Sleeping {sleepMs}ms...");
                            System.Threading.Thread.Sleep(sleepMs);
                            continue;
                        }

                        // command 처리
                        if (!string.IsNullOrWhiteSpace(command))
                        {
                            try
                            {
                                SendCommand(uart, command, param, logAction);
                                ReadStep(uart, 500, logAction, out var _, out var _);
                                System.Threading.Thread.Sleep(200);

                                // STOP 명령인 경우 스트리밍이 실제로 멈췄는지 확인
                                if (token == "STOP")
                                {
                                    if (LogConfig.IsTest) logAction("[TILT] STOP 후 버퍼 클리어");
                                    ClearCommBuffer(uart, logAction);
                                }
                            }
                            catch (Exception ex)
                            {
                                logAction($"[TILT] 종료 명령 실패: {command} - {ex.Message}");
                            }
                        }
                    }
                }
                if (LogConfig.IsTest) logAction("[TILT] finally 블록 종료 명령 완료");
                // 판정
                if (samples.Count == 0)
                {
                    logAction("[TILT] 기울기 데이터 수집 실패");
                    details.Add(new Dictionary<string, object?>
                    {
                        { "item", "기울기데이터" },
                        { "value", "no samples" },
                        { "result", "NG" }
                    });
                }
                else if (!ok)
                {
                    double avgX = samples.Average(s => s[0]);
                    double avgY = samples.Average(s => s[1]);
                    double spanX = maxXSeen - minXSeen;
                    double spanY = maxYSeen - minYSeen;

                    var tiltFailMsg = checkAxis == "x" ? $"[TILT] FAIL X={avgX:F2}[|X|:{xMin}~{xMax}] (TIMEOUT {overallTimeoutMs}ms)"
                        : checkAxis == "y" ? $"[TILT] FAIL Y={avgY:F2}[|Y|:{yMin}~{yMax}] (TIMEOUT {overallTimeoutMs}ms)"
                        : $"[TILT] FAIL X={avgX:F2}[|X|:{xMin}~{xMax}] Y={avgY:F2}[|Y|:{yMin}~{yMax}] (TIMEOUT {overallTimeoutMs}ms)";
                    logAction(tiltFailMsg);

                    if (aliveCheck)
                    {
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "기울기 변화폭 X" },
                            { "value", Math.Round(spanX, 3) },
                            { "min", aliveMinDeltaX },
                            { "max", null },
                            { "result", "NG" }
                        });
                        details.Add(new Dictionary<string, object?>
                        {
                            { "item", "기울기 변화폭 Y" },
                            { "value", Math.Round(spanY, 3) },
                            { "min", aliveMinDeltaY },
                            { "max", null },
                            { "result", "NG" }
                        });
                    }
                    else
                    {
                        if (checkAxis != "y")
                        {
                            details.Add(new Dictionary<string, object?>
                            {
                                { "item", "기울기 X" },
                                { "value", Math.Round(hasLast ? lastX : avgX, 3) },
                                { "_value_desc", "절대값 판정 (부호 무시, |값|이 min~max 이내면 PASS)" },
                                { "min", xMin },
                                { "max", xMax },
                                { "result", "NG" }
                            });
                        }
                        if (checkAxis != "x")
                        {
                            details.Add(new Dictionary<string, object?>
                            {
                                { "item", "기울기 Y" },
                                { "value", Math.Round(hasLast ? lastY : avgY, 3) },
                                { "_value_desc", "절대값 판정 (부호 무시, |값|이 min~max 이내면 PASS)" },
                                { "min", yMin },
                                { "max", yMax },
                                { "result", "NG" }
                            });
                        }
                    }

                    details.Add(new Dictionary<string, object?>
                    {
                        { "item", "샘플수" },
                        { "value", samples.Count },
                        { "result", "INFO" }
                    });
                    details.Add(new Dictionary<string, object?>
                    {
                        { "item", "모션 감지" },
                        { "value", motionDetected },
                        { "result", "INFO" }
                    });
                    details.Add(new Dictionary<string, object?>
                    {
                        { "item", "실패사유" },
                        { "value", aliveCheck
                            ? $"변화폭 부족 (ΔX={spanX:F2}, ΔY={spanY:F2}, TIMEOUT {overallTimeoutMs}ms)"
                            : requireMotion && !motionDetected
                            ? $"모션 미감지 (TIMEOUT {overallTimeoutMs}ms)"
                            : $"PASS 조건 미충족 (TIMEOUT {overallTimeoutMs}ms)" },
                        { "result", "NG" }
                    });
                }

                // 반드시 baudrate 복원
                if (tiltBaud != normalBaud)
                {
                    logAction($"[TILT] Baudrate 복원: {normalBaud}");
                    ChangeBaudRate(uart, normalBaud, logAction);
                }
            }

            return MakeResult(ok, procId, procName, details, ok ? "OK" : "NG");
        }

        // ─────────────────────────────────────────────────────────────────────
        // DPM 전압/전류 검사 (Powertest00) - ff_colorimeter에서 이전
        // ─────────────────────────────────────────────────────────────────────
        public IDictionary<string, object?> DPM_24V(
            IDictionary<string, object> libraries,
            IDictionary<string, object> param,
            Action<string> logAction)
            => Powertest00(libraries, param, logAction);

        public IDictionary<string, object?> DPM_3V3(
            IDictionary<string, object> libraries,
            IDictionary<string, object> param,
            Action<string> logAction)
            => Powertest00(libraries, param, logAction);

        public IDictionary<string, object?> DPM_CURR(
            IDictionary<string, object> libraries,
            IDictionary<string, object> param,
            Action<string> logAction)
            => Powertest00(libraries, param, logAction);

        public IDictionary<string, object?> Powertest00(
            IDictionary<string, object> libraries,
            IDictionary<string, object> param,
            Action<string> logAction)
        {
            // 1) 사용할 UART 핸들 결정
            // 우선순위: param["dpmport"] → config_["dpm"]의 uart → 기본값 "uart_dpm"
            string uartId =
                (param.TryGetValue("dpmport", out var dp) && dp != null && !string.IsNullOrWhiteSpace(dp.ToString()))
                    ? dp.ToString()!
                : (config_ != null && config_.TryGetValue("dpm", out var dpmId) && dpmId != null
                    && libraries.TryGetValue(dpmId.ToString()!, out var dpmLib) && dpmLib is not null)
                    ? (config_.TryGetValue("uart", out var u) && u != null ? u.ToString()! : "uart_dpm")
                : "uart_dpm";

            if (!libraries.TryGetValue(uartId, out var uartObj) || uartObj is not IComm uart)
            {
                return new Dictionary<string, object?>
                {
                    { "success", false },
                    { "error",   $"UART 핸들을 찾을 수 없습니다. (id='{uartId}')" }
                };
            }

            // 2) UART 연결 보장
            void EnsureConnected(IComm handle)
            {
                bool IsConn()
                {
                    try
                    {
                        var st = handle.GetStatus();
                        if (st == null) return false;
                        if (st.TryGetValue("Connected", out var c) && c is bool b1) return b1;
                        if (st.TryGetValue("is_connected", out var u2) && u2 is bool b2) return b2;
                    }
                    catch { }
                    return false;
                }
                if (IsConn()) return;
                try { handle.GetType().GetMethod("EnsureConnected")?.Invoke(handle, null); } catch { }
                if (IsConn()) return;
                try { handle.GetType().GetMethod("Connect")?.Invoke(handle, null); } catch { }
                if (IsConn()) return;
                try { handle.GetType().GetMethod("Begin")?.Invoke(handle, null); } catch { }
                logAction($"Powertest00: uart '{uartId}' 연결 상태: {IsConn()}");
            }
            EnsureConnected(uart);

            // 3) 테스트 목록
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null) throw new ArgumentException("tests 배열이 없습니다.");

            string procId =
                (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
                (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
                "Powertest00";

            string procName =
                (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
                (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
                "PowerTest";

            int stepDelayMs = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 1000;
            int readTimeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            // unit/scale: proc 레벨에서 주입된 값 읽기
            string unit = (param.TryGetValue("__proc_unit", out var _pu) && _pu != null) ? _pu.ToString()! : "";
            double scale = 1.0;
            if (param.TryGetValue("__proc_scale", out var _ps) && _ps != null)
                double.TryParse(_ps.ToString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out scale);

            // unit_test_count: 개별 항목 반복 횟수 (기본값 1)
            int testCount = 1;
            try { if (param.ContainsKey("unit_test_count")) testCount = Math.Max(1, Convert.ToInt32(param["unit_test_count"])); } catch { }
            int testCountDelay = 500;
            try { if (param.ContainsKey("unit_test_delay")) testCountDelay = Convert.ToInt32(param["unit_test_delay"]); } catch { }

            string lastCmdUart = "";
            string lastResponse = "";
            bool finalOk = true;
            double? lastValue = null, minRange = null, maxRange = null;
            var retLines = new List<string>();
            int tcPassCount = 0; // test_count 반복 중 PASS 횟수

            // 4) 테스트 루프 (test_count 반복)
            for (int tcRep = 0; tcRep < testCount; tcRep++)
            {
            if (testCount > 1)
                logAction($"[반복 {tcRep + 1}/{testCount}] 테스트 시작");

            bool roundOk = true;

            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;

                string idToken = test.ContainsKey("id") ? (test["id"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(idToken))
                {
                    logAction("Powertest00: id가 비어있습니다. 테스트를 건너뜁니다.");
                    finalOk = false;
                    retLines.Add(new Newtonsoft.Json.Linq.JObject
                    {
                        ["Name"] = procName, ["id"] = procId, ["Range"] = "-", ["Measure"] = ""
                    }.ToString(Newtonsoft.Json.Formatting.None));
                    continue;
                }

                string dpmFrame = Dpm_BuildFrame(idToken);
                lastCmdUart = dpmFrame;

                // retry 설정 읽기
                int retry = 0;
                try { if (test.ContainsKey("retry")) retry = Convert.ToInt32(test["retry"]); } catch { }
                int retryDelay = 300;
                try { if (test.ContainsKey("retry_delay")) retryDelay = Convert.ToInt32(test["retry_delay"]); } catch { }

                (minRange, maxRange) = Dpm_ParseMinMax(test);
                string errorRate = test.ContainsKey("error_rate") ? (test["error_rate"]?.ToString() ?? "") : "";

                bool okThis = false;
                for (int attempt = 0; attempt <= retry; attempt++)
                {
                    if (attempt > 0)
                    {
                        logAction($"Powertest00 재시도 {attempt}/{retry} (delay {retryDelay}ms)");
                        System.Threading.Thread.Sleep(retryDelay);
                    }

                    Dpm_FlushUart(uart, 3, 100);

                    if (LogConfig.IsTest) logAction($"Powertest00 DPM TX: {Dpm_Esc(dpmFrame)}");
                    uart.Send(new Dictionary<string, object?>
                    {
                        { "body", dpmFrame },
                        { "no_newline", true },
                        { "porttype", "uart" }
                    });

                    System.Threading.Thread.Sleep(stepDelayMs);

                    string payload = Dpm_RecvEtX(uart, readTimeout);
                    if (LogConfig.IsTest) logAction($"Powertest00 수신된 메시지: {payload}");
                    lastResponse = payload;

                    var m = System.Text.RegularExpressions.Regex.Match(
                        payload ?? "", @"adc\s*,\s*([-+]?\d*\.?\d+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    double v = 0.0;
                    bool hasValue = m.Success && double.TryParse(
                        m.Groups[1].Value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out v);

                    if (hasValue)
                    {
                        lastValue = v * scale;
                        if (LogConfig.IsTest) logAction($"Powertest00 추출된 값: {lastValue.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}{unit}");
                    }
                    else
                    {
                        logAction("Powertest00: adc, 뒤의 숫자 추출 실패");
                        lastValue = null;
                    }

                    okThis = false;
                    if (minRange.HasValue && maxRange.HasValue && lastValue.HasValue)
                    {
                        okThis = (lastValue.Value >= minRange.Value) && (lastValue.Value <= maxRange.Value);
                        string errorRateLog = string.IsNullOrEmpty(errorRate) ? "" : $"error_rate={errorRate}, ";
                        logAction($"Powertest00 범위 판정: {errorRateLog}min={minRange.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, max={maxRange.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, value={lastValue.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}{unit} => {(okThis ? "OK" : "FAIL")}");
                    }
                    else
                    {
                        logAction("Powertest00: min/max가 없거나 수신값이 없어 판정을 건너뜁니다.");
                    }

                    if (okThis) break; // PASS면 재시도 중단
                }
                finalOk &= okThis;

                Dpm_FlushUart(uart, 2, 100);

                string rangeText =
                    (minRange.HasValue || maxRange.HasValue)
                    ? $"{minRange?.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) ?? ""}-{maxRange?.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) ?? ""}"
                    : "-";
                string measuredText = lastValue?.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) ?? "";

                var retObj = new Newtonsoft.Json.Linq.JObject
                {
                    ["Name"] = procName,
                    ["id"] = procId,
                    ["Range"] = rangeText,
                    ["Measure"] = measuredText
                };
                if (!string.IsNullOrEmpty(unit))
                    retObj["Unit"] = unit;
                if (!string.IsNullOrEmpty(errorRate))
                    retObj["error_rate"] = errorRate;
                retObj["result"] = roundOk ? "OK" : "NG";

                retLines.Add(retObj.ToString(Newtonsoft.Json.Formatting.None));
            }

            // test_count 반복 회차 판정
            if (roundOk) tcPassCount++;
            finalOk &= roundOk;

            if (testCount > 1)
                logAction($"[반복 {tcRep + 1}/{testCount}] 결과: {(roundOk ? "OK" : "FAIL")}");

            // 마지막 회차가 아니면 대기
            if (tcRep < testCount - 1)
                System.Threading.Thread.Sleep(testCountDelay);

            } // end test_count loop

            if (testCount > 1)
                logAction($"[반복 완료] OK {tcPassCount}/{testCount}");

            return new Dictionary<string, object?>
            {
                { "success",      finalOk },
                { "command_uart", Dpm_Esc(lastCmdUart) },
                { "response",     lastResponse },
                { "value",        lastValue?.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) },
                { "min",          minRange?.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) },
                { "max",          maxRange?.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) },
                { "retmsg",       string.Join(Environment.NewLine, retLines) },
                { "test_count_result", testCount > 1 ? $"{tcPassCount}/{testCount}" : null }
            };
        }

        // DPM 헬퍼 메서드
        private string Dpm_BuildFrame(string idToken)
        {
            string core = (idToken ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(core)) return "\u0002ID00=ADC\u0003";
            if (!core.Contains("=")) core += "=ADC";
            return "\u0002" + core + "\u0003";
        }

        private void Dpm_FlushUart(IComm uart, int attempts, int toMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    var r = uart.Recv(new Dictionary<string, object?>
                    {
                        { "timeout", toMs }, { "recv_mode", "etx" },
                        { "stx_guard", true }, { "etx", 0x03 }
                    });
                    var s = Dpm_ExtractValue(r);
                    if (string.IsNullOrWhiteSpace(s)) break;
                }
                catch { break; }
            }
        }

        private string Dpm_RecvEtX(IComm uart, int timeout)
        {
            var r1 = uart.Recv(new Dictionary<string, object?>
            {
                { "timeout", timeout }, { "recv_mode", "etx" },
                { "stx_guard", true }, { "etx", 0x03 }
            });
            string s1 = Dpm_ExtractValue(r1) ?? "";
            if (!string.IsNullOrWhiteSpace(s1)) return s1;

            for (int i = 0; i < 2; i++)
            {
                var r = uart.Recv(new Dictionary<string, object?>
                {
                    { "timeout", 250 }, { "recv_mode", "etx" },
                    { "stx_guard", true }, { "etx", 0x03 }
                });
                var s = Dpm_ExtractValue(r);
                if (!string.IsNullOrWhiteSpace(s)) return s!;
            }
            return "";
        }

        private (double?, double?) Dpm_ParseMinMax(IDictionary<string, object> testItem)
        {
            double? rmin = null, rmax = null;
            if (testItem.ContainsKey("min") && double.TryParse(
                testItem["min"]?.ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var a)) rmin = a;
            if (testItem.ContainsKey("max") && double.TryParse(
                testItem["max"]?.ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var b)) rmax = b;
            return (rmin, rmax);
        }

        private string Dpm_Esc(string s)
            => string.IsNullOrEmpty(s) ? s
               : s.Replace("\u0002", "\\x02").Replace("\u0003", "\\x03")
                   .Replace("\r", "\\r").Replace("\n", "\\n");

        private string? Dpm_ExtractValue(object? recv)
        {
            if (recv == null) return null;
            if (recv is IDictionary<string, object?> d && d.TryGetValue("value", out var v) && v != null) return v.ToString();
            if (recv is string s) return s;
            return recv.ToString();
        }

        // ══════════════════════════════════════════════
        //  OPERATOR_CONFIRM: 작업자 시각 확인 (LED 등)
        //  - 통신/측정 없이 팝업만 띄우고 확인/취소 결과로 PASS/FAIL 판정
        //  - JSON의 operator_prompt 에서 문구 지정
        // ══════════════════════════════════════════════
        public IDictionary<string, object?> OPERATOR_CONFIRM(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction,
            Func<string, bool> confirmCallback)
        {
            string procId   = GetParamString(param, "proc_id", "OPERATOR_CONFIRM");
            string procName = GetParamString(param, "proc_name", "작업자 확인");
            string prompt   = GetParamString(param, "operator_prompt", "확인해주세요");

            logAction($"[OPERATOR_CONFIRM] 작업자 확인 대기: {prompt}");

            // 취소 가능한 YesNo 콜백 우선 사용 (FlexfabForm에서 주입). 없으면 기본 confirmCallback(확인만).
            Func<string, bool>? cb = confirmCallback;
            if (libraries != null
                && libraries.TryGetValue("confirmCallbackYesNo", out var cbObj)
                && cbObj is Func<string, bool> cbYesNo)
            {
                cb = cbYesNo;
            }

            bool confirmed;
            try
            {
                confirmed = cb?.Invoke(prompt) ?? false;
            }
            catch (Exception ex)
            {
                logAction($"[OPERATOR_CONFIRM] 팝업 오류: {ex.Message}");
                return MakeResult(false, procId, procName, null, "NG");
            }

            if (!confirmed)
            {
                logAction("[OPERATOR_CONFIRM] 작업자 취소 → FAIL");
                return MakeResult(false, procId, procName, null, "NG");
            }

            logAction("[OPERATOR_CONFIRM] PASS");
            return MakeResult(true, procId, procName, null, "OK");
        }
    }
}
