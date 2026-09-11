
using Newtonsoft.Json;

namespace Cantops.FlexFab
{
    public class Dpm : ModuleBase, IDevice, IProcess
    {
        private IComm? uartHandle;

        public const string MODULE_VERSION = "1.0"; //버전 정보 여기서 수정

        public Dpm()
        {
            info_ = new Dictionary<string, object>
            {
                { "name", "dpm" },
                { "desc", "UART communication class implementing IComm using System.IO.Ports.SerialPort" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object>> {
                    new Dictionary<string, object> {
                        { "id", "proc_01" },
                        { "desc", "5V 입력 범위 검사" },
                    },
                }}
            };
        }

        public override IDictionary<string, object?>? GetStatus()
        {
            return new Dictionary<string, object?>
            {
                { "Status", "OK" },
                { "Description", "DPM 상태 정상" }
            };
        }

        public void Initialize(IDictionary<string, object?> libraries)
        {
            if (config_ == null)
            {
                throw new InvalidOperationException("Dpm 클래스의 config 객체가 null입니다.");
            }

            if (!config_.TryGetValue("uart", out var uartId) || uartId == null)
            {
                throw new InvalidOperationException("Dpm 클래스의 config.uart 설정이 없습니다.");
            }

            uartHandle = libraries[uartId as string] as IComm;
            if (uartHandle == null)
            {
                throw new InvalidOperationException("UART 핸들을 가져올 수 없습니다.");
            }

            //Log($"DPM 초기화 완료: UART ID = {uartId}");
        }

        public override void SetConfig(IDictionary<string, object> config)
        {
            base.SetConfig(config); // 기본 설정 호출

            if (config.TryGetValue("uart", out var uartId) && uartId is string)
            {
                if (LogConfig.IsTest) Log($"Dpm 클래스에 UART 설정: {uartId}");
            }
            else
            {
                throw new InvalidOperationException("Dpm 클래스의 config.uart 설정이 없습니다.");
            }
        }

        public void Begin()
        {
            //Log("DPM Begin 호출됨");
        }

        public void End()
        {
            //Log("DPM End 호출됨");
        }

        // IDevice 인터페이스의 Read 메서드 구현
        public IDictionary<string, object?> Read(IDictionary<string, object?> param)
        {
            if (uartHandle == null)
            {
                throw new InvalidOperationException("UART 핸들이 초기화되지 않았습니다.");
            }

            //Log("UART 데이터 수신 대기 중...");

            // Timeout을 파라미터에서 가져옴
            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 1000;

            // 데이터 수신 (변경된 Send/Recv 메서드 호출)
            var recv_body = uartHandle.Recv(new Dictionary<string, object?> { { "timeout", timeout } });

            Log($"UART 데이터 수신 완료: {recv_body}");

            if (recv_body == null)
            {
                throw new InvalidOperationException("UART 데이터 수신 실패");
            }

            // 수신된 데이터를 숫자 형식으로 변환 시도
            if (!double.TryParse(recv_body.ToString(), out double parsedValue))
            {
                throw new InvalidOperationException("수신된 데이터를 숫자로 변환할 수 없습니다.");
            }

            return new Dictionary<string, object?> { { "value", parsedValue } };
        }

        // IDevice 인터페이스의 Write 메서드 구현
        public void Write(IDictionary<string, object?> param)
        {
            if (uartHandle == null)
            {
                throw new InvalidOperationException("UART 핸들이 초기화되지 않았습니다.");
            }

            if (!param.ContainsKey("body"))
            {
                throw new ArgumentException("Write 매개변수에 'body'가 필요합니다.");
            }

            string dataToSend = param["body"]?.ToString() ?? throw new ArgumentNullException("body");

            Log($"UART로 전송할 데이터: {dataToSend}");

            // UART로 데이터 전송 (변경된 Send/Recv 메서드 호출)
            uartHandle.Send(new Dictionary<string, object?> { { "body", dataToSend } });

            //Log("데이터 전송 완료");
        }
        public IDictionary<string, object?> DPMMSG(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            const char STX = '\u0002';
            const char ETX = '\u0003';

            // 1) uart 핸들 확보 (이미 Initialize에서 세팅되었을 수도 있고, 아닐 수도 있으니 보강)
            if (uartHandle == null)
            {
                // 우선 순위: param["port"] -> "uart_dpm" -> config_["uart"]
                string? portId = null;
                if (param.ContainsKey("port") && param["port"] is string p1) portId = p1;
                if (string.IsNullOrWhiteSpace(portId) && libraries.ContainsKey("uart_dpm")) portId = "uart_dpm";
                if (string.IsNullOrWhiteSpace(portId) && config_ != null && config_.TryGetValue("uart", out var cfgUart) && cfgUart is string p2) portId = p2;

                if (string.IsNullOrWhiteSpace(portId) || !libraries.ContainsKey(portId!))
                    return new Dictionary<string, object?> { { "success", false }, { "error", "UART 핸들을 찾을 수 없음" } };

                uartHandle = libraries[portId!] as IComm;
                if (uartHandle == null)
                    return new Dictionary<string, object?> { { "success", false }, { "error", "UART 핸들 캐스팅 실패" } };

                logAction("DPMMSG: uartHandle이 없어 libraries에서 자동 세팅.");
            }

            // 2) 연결 보장 (Uart 쪽은 그대로 두고, 가능한 메서드만 순차적으로 호출 시도)
            bool IsConnected()
            {
                try
                {
                    var st = uartHandle!.GetStatus();
                    return st != null && st.TryGetValue("is_connected", out var v) && v is bool b && b;
                }
                catch { return false; }
            }
            if (!IsConnected())
            {
                try { uartHandle!.GetType().GetMethod("EnsureConnected")?.Invoke(uartHandle, null); } catch { }
                if (!IsConnected()) try { uartHandle!.GetType().GetMethod("Connect")?.Invoke(uartHandle, null); } catch { }
                if (!IsConnected()) try { uartHandle!.GetType().GetMethod("Begin")?.Invoke(uartHandle, null); } catch { }
            }
            if (!IsConnected())
                return new Dictionary<string, object?> { { "success", false }, { "error", "UART 연결 실패" } };

            // 3) 파라미터
            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;
            int delay = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 1000;

            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "'tests'가 없음" } };

            // 4) 시작 시 잔여 수신 비우기(짧은 타임아웃으로 몇 번 읽어봄)
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var pre = uartHandle.Recv(new Dictionary<string, object?> { { "timeout", 100 } });
                    if (TryExtractValue(pre) is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        // 잔여가 있으면 비워주고 계속
                    }
                    else break;
                }
                catch { break; }
            }

            string lastCommand = "";
            string lastResponse = "";
            string expectedResponse = "";
            bool finalResult = true;

            foreach (var t in tests)
            {
                var item = t as IDictionary<string, object>;
                if (item == null || !item.ContainsKey("command"))
                    continue;

                // "ID22" -> "ID22=ADC" 보정
                string cmd = (item["command"]?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(cmd)) { finalResult = false; continue; }

                if (!cmd.Contains("=")) cmd += "=ADC"; // 자동 보정

                // 프레이밍: <STX> ... <ETX>
                string framed = $"{STX}{cmd}{ETX}";
                lastCommand = framed;
                expectedResponse = item.ContainsKey("expected_response") ? (item["expected_response"]?.ToString() ?? "") : "";

                logAction($"DPMMSG TX: {EscapeForLog(framed)}"); // 로그에 제어문자 보이게

                // 5) 전송 (개행 없이 보내야 하는 계측기 대응 -> no_newline 힌트 전달)
                try
                {
                    uartHandle.Send(new Dictionary<string, object?> {
                        { "body", framed },
                        { "no_newline", true } // Uart가 지원하면 개행 없이 전송, 미지원이어도 무해
                    });
                }
                catch (Exception ex)
                {
                    logAction($"DPMMSG Send 예외: {ex.Message}");
                    finalResult = false;
                    continue;
                }

                // 장치 처리 시간
                Thread.Sleep(delay);

                // 6) 수신: ETX 기반으로 페이로드만 받기 (Uart가 etx 모드 지원 시 활용)
                string payload = "";
                try
                {
                    var resp = uartHandle.Recv(new Dictionary<string, object?> {
                        { "timeout", timeout },
                        { "recv_mode", "etx" }, // 지원 시 ETX까지 읽기
                        { "stx_guard", true },  // STX 이후부터 페이로드 수집
                        { "etx", 0x03 }
                    });
                    var val = TryExtractValue(resp);
                    payload = val ?? "";
                }
                catch (Exception ex)
                {
                    logAction($"DPMMSG Recv 예외: {ex.Message}");
                    payload = "";
                }

                lastResponse = payload;

                if (string.IsNullOrEmpty(expectedResponse))
                {
                    // 기대값이 비어있으면, "응답이 비어있지 않으면 성공" 규칙
                    bool ok = !string.IsNullOrWhiteSpace(payload);
                    finalResult &= ok;
                    logAction(ok
                        ? $"응답 OK (비어있지 않은 페이로드 수신) -> [{payload}]"
                        : "응답 없음 (Timeout 또는 연결 문제)");
                }
                else
                {
                    bool match = payload.Contains(expectedResponse);
                    finalResult &= match;
                    logAction($"응답 비교 - 기대값: {expectedResponse}, 실제값: {payload}, 성공 여부: {(match ? "일치" : "불일치")}");
                }

                // 응답 후 잔여 버퍼 한 번 더 비움
                try
                {
                    var flush = uartHandle.Recv(new Dictionary<string, object?> { { "timeout", 100 } });
                    // 값이 오더라도 무시 (비움 목적)
                }
                catch { /* ignore */ }
            }

            return new Dictionary<string, object?> {
                { "success", finalResult },
                { "command", lastCommand },
                { "response", lastResponse },
                { "expected", expectedResponse }
            };
        }

        // --- 유틸: uartHandle.Recv 반환 타입이 구현마다 다를 수 있어 안전하게 값 추출 ---
        private static string? TryExtractValue(object? recvResult)
        {
            if (recvResult == null) return null;

            // IDictionary<string, object?> 형태: { "value": "..." }
            if (recvResult is IDictionary<string, object?> dict)
            {
                if (dict.TryGetValue("value", out var v) && v != null)
                    return v.ToString();
                return null;
            }

            // 문자열 직접 반환하는 구현
            if (recvResult is string s) return s;

            // 다른 케이스는 ToString()
            return recvResult.ToString();
        }

        // --- 로그에서 제어문자 보이게 ---
        private static string EscapeForLog(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s
                .Replace("\u0002", "\\x02")
                .Replace("\u0003", "\\x03")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }

}

