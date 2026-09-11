using Cantops.FlexFab;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Cantops.FlexFab
{
    public class Colorimeter : ModuleBase, IDevice
    {
        private IComm? _comm;
        public const string MODULE_VERSION = "1.0";

        public Colorimeter()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "colorimeter" },
                { "desc", "Colorimeter module" },
                { "ver",  MODULE_VERSION }
            };
        }

        public override IDictionary<string, object?>? GetStatus()
            => new Dictionary<string, object?> { { "connected", _comm != null } };

        public void Initialize(IDictionary<string, object?> libraries)
        {
            if (config_ == null)
                throw new InvalidOperationException("Colorimeter: config_ is null.");

            if (!config_.TryGetValue("commlib", out var idObj) ||
                idObj is not string commId || string.IsNullOrWhiteSpace(commId))
                throw new InvalidOperationException("Colorimeter: config_에 'commlib'가 없습니다.");

            if (!libraries.TryGetValue(commId, out var libObj) || libObj is not IComm comm)
                throw new InvalidOperationException($"Colorimeter: IComm '{commId}'를 찾을 수 없습니다.");

            _comm = comm;
            Log($"Colorimeter: commlib='{commId}' 사용.");
        }

        public void Write(IDictionary<string, object?> param)
        {
            if (_comm == null)
                throw new InvalidOperationException("Colorimeter: Initialize 먼저 호출 필요.");

            if (!param.ContainsKey("body"))
                throw new ArgumentException("Colorimeter.Write: 'body' 필요.");

            _comm.Send(param);
        }

        public IDictionary<string, object?> Read(IDictionary<string, object?> param)
        {
            if (_comm == null)
                throw new InvalidOperationException("Colorimeter: Initialize 먼저 호출 필요.");

            var recv = _comm.Recv(param);
            string val = "";

            if (recv != null && recv.TryGetValue("value", out var v) && v != null)
                val = v.ToString() ?? "";

            return new Dictionary<string, object?> { { "value", val } };
        }

        public override void PreRun(IDictionary<string, object?> libraries)
        {
            base.PreRun(libraries);
            Initialize(libraries);
        }

        public override void PostRun()
        {
            base.PostRun();
            _comm = null;
        }

        public IDictionary<string, object?> Powertest00(IDictionary<string, object> libraries,IDictionary<string, object> param,Action<string> logAction)
        {
            // ---- 1) 사용할 UART 핸들 결정 ----
            // 우선순위: param["dpmport"] → config_["uart"] → 기본값 "uart_dpm"
            string uartId =
                (param.TryGetValue("dpmport", out var dp) && dp != null && !string.IsNullOrWhiteSpace(dp.ToString()))
                    ? dp.ToString()!
                : (config_ != null && config_.TryGetValue("uart", out var u) && u != null && !string.IsNullOrWhiteSpace(u.ToString()))
                    ? u.ToString()!
                : "uart_dpm";

            if (!libraries.TryGetValue(uartId, out var uartObj) || uartObj is not IComm uart)
            {
                return new Dictionary<string, object?>
                {
                    { "success", false },
                    { "error",   $"UART 핸들을 찾을 수 없습니다. (id='{uartId}')" }
                };
            }

            // ---- 2) UART 연결 보장 ----
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

                logAction($"Colorimeter.Powertest00: uart '{uartId}' 연결 상태: {IsConn()}");
            }

            EnsureConnected(uart);

            // ---- 3) 테스트 목록 ----
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null) throw new ArgumentException("tests 배열이 없습니다.");

            // 메타 (retmsg용) : __proc_* 우선, 없으면 proc_* 폴백, 모두 없으면 기본값
            string procId =
                (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
                (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
                "Powertest00";

            string procName =
                (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
                (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
                "PowerTest";

            // 지연 / 타임아웃
            int stepDelayMs = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 1000;
            int readTimeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            string lastCmdUart = "";
            string lastResponse = "";
            bool finalOk = true;
            double? lastValue = null, minRange = null, maxRange = null;

            var retLines = new List<string>(); // ★ retmsg 누적 (JSON 한 줄씩)

            // ---- 4) 테스트 루프 (UART만 사용) ----
            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;

                // (A) ID 토큰 → DPM 프레임 (예: "id18" → <STX>ID18=ADC<ETX>)
                string idToken = test.ContainsKey("id") ? (test["id"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(idToken))
                {
                    logAction("Powertest00: id가 비어있습니다. 테스트를 건너뜁니다.");
                    finalOk = false;

                    retLines.Add(
                        new JObject
                        {
                            ["Name"] = procName,
                            ["id"] = procId,
                            ["Range"] = "-",
                            ["Measure"] = ""
                        }.ToString(Formatting.None)
                    );
                    continue;
                }

                string dpmFrame = BuildDpmFrameFromId(idToken); // <STX>ID18=ADC<ETX>
                lastCmdUart = dpmFrame;

                // UART 송신 전 버퍼 플러시
                FlushUart(uart, 3, 100);

                logAction($"Powertest00 DPM TX: {Esc(dpmFrame)}");
                uart.Send(new Dictionary<string, object?>
                {
                    { "body", dpmFrame },
                    { "no_newline", true },   // 개행 없이
                    { "porttype",  "uart" }   // 힌트 (필요 없으면 제거해도 됨)
                });

                System.Threading.Thread.Sleep(stepDelayMs);

                // (B) UART 수신(ETX 프레이밍)
                string payload = RecvUartEtX(uart, readTimeout);
                logAction($"Powertest00 수신된 메시지: {payload}");
                lastResponse = payload;

                // adc, 뒤 숫자 추출
                var m = Regex.Match(payload ?? "", @"adc\s*,\s*([-+]?\d*\.?\d+)", RegexOptions.IgnoreCase);

                double v = 0.0;
                bool hasValue = m.Success && double.TryParse(
                    m.Groups[1].Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out v
                );

                if (hasValue)
                {
                    lastValue = v;
                    logAction($"Powertest00 추출된 값: {v.ToString(CultureInfo.InvariantCulture)}");
                }
                else
                {
                    logAction("Powertest00: adc, 뒤의 숫자 추출 실패");
                    lastValue = null;
                }

                // min / max 파싱
                (minRange, maxRange) = ParseMinMax(test);

                // 판정 (최종 성공 여부만 사용, retmsg에는 OK/NG 안 넣는 형태)
                bool okThis = false;
                if (minRange.HasValue && maxRange.HasValue && lastValue.HasValue)
                {
                    okThis = (lastValue.Value >= minRange.Value) && (lastValue.Value <= maxRange.Value);
                    logAction($"Powertest00 범위 판정: min={minRange.Value}, max={maxRange.Value}, value={lastValue.Value} => {(okThis ? "OK" : "FAIL")}");
                }
                else
                {
                    logAction("Powertest00: min/max가 없거나 수신값이 없어 판정을 건너뜁니다.");
                }
                finalOk &= okThis;

                // 수신 후 짧게 플러시
                FlushUart(uart, 2, 100);

                // (C) retmsg 라인 추가: { Name, id, Range, Measure }  (Result 없음)
                string rangeText =
                    (minRange.HasValue || maxRange.HasValue)
                    ? $"{minRange?.ToString(CultureInfo.InvariantCulture) ?? ""}-{maxRange?.ToString(CultureInfo.InvariantCulture) ?? ""}"
                    : "-";

                string measuredText = lastValue?.ToString(CultureInfo.InvariantCulture) ?? "";

                retLines.Add(
                    new JObject
                    {
                        ["Name"] = procName,   // 예: "Main 5V" (MainDoProject쪽에서 __proc_name으로 넘겨줌)
                        ["id"] = procId,     // "Powertest00"
                        ["Range"] = rangeText,
                        ["Measure"] = measuredText
                    }.ToString(Formatting.None)
                );
            }

            // ---- 5) 최종 리턴 ----
            return new Dictionary<string, object?>
                {
                    { "success",      finalOk },
                    { "command_uart", Esc(lastCmdUart) },
                    { "response",     lastResponse },
                    { "value",        lastValue?.ToString(CultureInfo.InvariantCulture) },
                    { "min",          minRange?.ToString(CultureInfo.InvariantCulture) },
                    { "max",          maxRange?.ToString(CultureInfo.InvariantCulture) },
                    { "retmsg",       string.Join(Environment.NewLine, retLines) } // ← PIM_Powertest00와 동일 형식
                };
        }

        // "id18" → "<STX>ID18=ADC<ETX>"
        string BuildDpmFrameFromId(string idToken)
        {
            string core = (idToken ?? "").Trim();
            if (string.IsNullOrEmpty(core)) return "\u0002ID00=ADC\u0003";
            core = core.ToUpperInvariant();
            if (!core.Contains("=")) core += "=ADC";
            return "\u0002" + core + "\u0003";
        }

        void FlushUart(IComm uart, int attempts, int toMs)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    var r = uart.Recv(new Dictionary<string, object?>
                    {
                        { "timeout",   toMs },
                        { "recv_mode", "etx" },
                        { "stx_guard", true },
                        { "etx",       0x03 }
                    });
                    var s = TryExtractValue(r);
                    if (string.IsNullOrWhiteSpace(s)) break;
                }
                catch { break; }
            }
        }

        string RecvUartEtX(IComm uart, int timeout)
        {
            var r1 = uart.Recv(new Dictionary<string, object?>
            {
                { "timeout",   timeout },
                { "recv_mode", "etx" },
                { "stx_guard", true },
                { "etx",       0x03 }
            });
            string s1 = TryExtractValue(r1) ?? "";
            if (!string.IsNullOrWhiteSpace(s1)) return s1;

            // 짧게 2회 더 폴링
            for (int i = 0; i < 2; i++)
            {
                var r = uart.Recv(new Dictionary<string, object?>
                {
                    { "timeout",   250 },
                    { "recv_mode", "etx" },
                    { "stx_guard", true },
                    { "etx",       0x03 }
                });
                var s = TryExtractValue(r);
                if (!string.IsNullOrWhiteSpace(s)) return s!;
            }
            return "";
        }


        // min / max 파싱
        (double?, double?) ParseMinMax(IDictionary<string, object> testItem)
        {
            double? rmin = null, rmax = null;
            if (testItem.ContainsKey("min")
                && double.TryParse(testItem["min"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
                rmin = a;
            if (testItem.ContainsKey("max")
                && double.TryParse(testItem["max"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
                rmax = b;
            return (rmin, rmax);
        }

        string Esc(string s)
            => string.IsNullOrEmpty(s) ? s
               : s.Replace("\u0002", "\\x02").Replace("\u0003", "\\x03")
                   .Replace("\r", "\\r").Replace("\n", "\\n");

        string? TryExtractValue(object? recv)
        {
            if (recv == null) return null;
            if (recv is IDictionary<string, object?> d && d.TryGetValue("value", out var v) && v != null) return v.ToString();
            if (recv is string s) return s;
            return recv.ToString();
        }

        public Dictionary<string, object> Porttest00(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
        {
            // ============================================================
            // 0️⃣ 메타정보 (procName / procId) - Powertest00 방식 적용
            // ============================================================
            string procId =
                (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
                (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
                "Porttest00";

            string procName =
                (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
                (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
                "Port 검사";

            // ============================================================
            // 1️⃣ 포트 준비
            // ============================================================
            if (!param.TryGetValue("port", out var portObj) || portObj == null)
                throw new ArgumentException("param['port'] 누락");
            string portId = portObj.ToString()!;

            if (!libraries.TryGetValue(portId, out var libObj) || libObj is not IComm uart)
                return new()
        {
            { "success", false },
            { "error", $"'{portId}' not IComm" }
        };

            logAction($"[Porttest00] Port={portId}");

            // ============================================================
            // 2️⃣ 연결 보장
            // ============================================================
            try { uart.Connect(); } catch { }

            Thread.Sleep(200); // 안정화 대기
            uart.Recv(new Dictionary<string, object?> { { "timeout", 1000 } }); // flush 1회

            // ============================================================
            // 3️⃣ VER 송신
            // ============================================================
            string cmd = "VER\r\n";
            uart.Send(new Dictionary<string, object?> { { "body", cmd } });
            logAction($"→ Sent: {cmd.Replace("\r", "\\r").Replace("\n", "\\n")}");

            Thread.Sleep(50);

            // ============================================================
            // 4️⃣ 응답 수신
            // ============================================================
            int totalWait = param.TryGetValue("delay", out var dObj) ? Convert.ToInt32(dObj) : 2000;
            int perTimeout = param.TryGetValue("timeout", out var tObj) ? Convert.ToInt32(tObj) : 1000;
            int tries = Math.Max(1, totalWait / perTimeout);

            string all = "";
            for (int i = 0; i < tries; i++)
            {
                var resp = uart.Recv(new Dictionary<string, object?>
        {
            { "timeout", perTimeout },
            { "recv_mode", "etx" },
            { "etx", 0 }
        });

                string chunk = Extract(resp);
                if (!string.IsNullOrEmpty(chunk))
                {
                    all += chunk;
                    if (all.Contains("Colorimeter", StringComparison.OrdinalIgnoreCase))
                        break;
                }
            }

            bool ok = all.Contains("Colorimeter", StringComparison.OrdinalIgnoreCase);
            logAction($"Final RX: {all}");

            // ============================================================
            // 5️⃣ 결과 메시지 (retmsg)
            // ============================================================
            var ret = new JObject
            {
                ["Name"] = procName,   // ✅ 이제 "Hub1 Port 검사"가 정상 표시됨
                ["id"] = procId,       // ✅ "Porttest00"
                ["result"] = ok ? "OK" : "NG"
            };

            return new()
    {
        { "success", ok },
        { "response", all },
        { "retmsg", ret.ToString(Newtonsoft.Json.Formatting.None) }
    };

            // ============================================================
            // (내부 헬퍼)
            // ============================================================
            static string Extract(object? recv)
            {
                if (recv is IDictionary<string, object?> d && d.TryGetValue("value", out var v))
                    return v?.ToString() ?? "";
                return recv?.ToString() ?? "";
            }
        }




        public Dictionary<string, object> Frametest00(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
        {
            // 1️⃣ 포트 준비
            if (!param.TryGetValue("port", out var portObj) || portObj == null)
                throw new ArgumentException("Frametest00: param['port'] 누락");

            string portId = portObj.ToString()!;
            if (!libraries.TryGetValue(portId, out var libObj) || libObj is not IComm uart)
                return new() { { "success", false }, { "error", $"'{portId}' not IComm" } };

            // 2️⃣ 설정값 읽기
            int frameTarget = param.TryGetValue("frame", out var fObj) ? Convert.ToInt32(fObj) : 15;
            string procName = param.TryGetValue("name", out var pn) ? pn?.ToString() ?? "Frame 검사" : "Frame 검사";
            string procId = param.TryGetValue("proc_id", out var pid) ? pid?.ToString() ?? "Frametest00" : "Frametest00";

            logAction($"[Frametest00] Port={portId}, Warm-up=2s, Capture=3s, TargetFrame={frameTarget}");

            // 3️⃣ 포트 연결
            try { uart.Connect(); } catch { }
            Thread.Sleep(300);

            // 4️⃣ DTR/RTS 강제 활성화
            try
            {
                dynamic dyn = uart;
                var sp = dyn.GetType()
                    .GetField("serial_port_", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(dyn);
                if (sp != null)
                {
                    sp.DtrEnable = true;
                    sp.RtsEnable = true;
                    logAction("※ DTR/RTS manually enabled for device wake-up (via dynamic)");
                }
            }
            catch (Exception ex)
            {
                logAction($"⚠️ DTR/RTS enable failed: {ex.Message}");
            }

            uart.Send(new Dictionary<string, object?> { { "body", "<IMG D>" } }); //1회 전송
            Thread.Sleep(100);


            // 5️⃣ 프레임 측정 시작
            uart.Send(new Dictionary<string, object?> { { "body", "<IMG T>" } });
            Thread.Sleep(100);
            logAction("→ Sent: <IMG T>");

            // 6️⃣ 2초 워밍업
            Thread.Sleep(2000);
            logAction("… Warm-up complete, start 1s capture …");

            // 7️⃣ 3초간 데이터 수신
            int receivedCount = 0;
            long totalBytes = 0;
            const int captureMs = 1000;
            var start = DateTime.UtcNow;

            while ((DateTime.UtcNow - start).TotalMilliseconds < captureMs)
            {
                var resp = uart.Recv(new Dictionary<string, object?>
        {
            { "timeout", 200 },
            { "recv_mode", "etx" },
            { "etx", 0x03 }
        });

                if (resp != null && resp.ContainsKey("value"))
                {
                    string chunk = resp["value"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        receivedCount++;
                        totalBytes += chunk.Length;
                        // logAction($"RX[{receivedCount}]: {chunk.Length} bytes"); // 필요시 로그
                    }
                }
            }

            // 8️⃣ 프레임 계산 (3초 구간 → 초당 프레임)
            double framePerSec = receivedCount / 1.0;
            int roundedFrame = (int)Math.Round(framePerSec);

            bool ok = (framePerSec >= frameTarget);

            // 9️⃣ 종료 명령
            uart.Send(new Dictionary<string, object?> { { "body", "<IMG D>" } });
            logAction("→ Sent: <IMG D>");

            // 🔟 결과 요약
            //logAction($"수신된 총 이벤트 수: {receivedCount}");
            //logAction($"총 수신 데이터량: {totalBytes} bytes");
            logAction($"프레임 속도: {roundedFrame} Frame/sec");
            logAction(ok ? "✅ Frame test PASS" : "❌ Frame test FAIL");

            // 11️⃣ Porttest 스타일로 반환
            var ret = new JObject
            {
                ["Name"] = procName,
                ["id"] = procId,
                ["Frame"] = frameTarget.ToString(),
                ["Measure"] = roundedFrame.ToString(),
                ["result"] = ok ? "OK" : "NG"
            };

            return new()
    {
        { "success", ok },
        { "response", $"{roundedFrame} frames/sec, {totalBytes} bytes" },
        { "retmsg", ret.ToString(Newtonsoft.Json.Formatting.None) }
    };
        }



        //함수 종료
    }
}
