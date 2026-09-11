


using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System.Threading;

// DictionaryExtensions.cs
public static class DictionaryExtensions
{
    // GetValueOrDefault 확장 메서드
    public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue?> dictionary, TKey key)
    {
        // 키가 존재하면 값을 반환하고, 없으면 기본값 (null) 반환
        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
}


namespace Cantops.FlexFab
{
    /// <summary>
    /// UART 통신을 구현하는 클래스입니다.
    /// </summary>
    public class Uart : ModuleBase, IComm, IProcess
    {
        private readonly SerialPort serial_port_;
        private bool _isConnected;
        public bool IsConnected => _isConnected;
        private IComm _uartHandle;

        public const string MODULE_VERSION = "1.0"; //버전 정보

        /// <summary>
        /// 기본 생성자: 초기 SerialPort 객체를 생성합니다.
        /// </summary>
        public Uart()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "uart" },
                { "desc", "UART communication class implementing IComm using System.IO.Ports.SerialPort" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object?>> {
                    new Dictionary<string, object?> {
                        { "id", "proc_01" },
                        { "desc", "통신 검사" },
                    },
                }}
            };
            serial_port_ = new SerialPort();
            _isConnected = false;
        }

        /// <summary>
        /// UART의 설정을 적용합니다.
        /// </summary>
        /// <param name="config">설정 정보. 예: { Port = "COM3", BaudRate = 9600 }</param>
        /// 




        public override void SetConfig(IDictionary<string, object> cfg)
        {
            base.SetConfig(cfg);
            if (_isConnected)
                throw new InvalidOperationException("Cannot change configuration while connected.");

            // 필수
            var portName = cfg.TryGetValue("port", out var portObj)
                ? Convert.ToString(portObj) ?? throw new ArgumentNullException("port")
                : throw new ArgumentNullException("port");

            // 숫자는 long/double/string 어떤 형태로 와도 Convert.ToInt32로 처리
            var baudRate = cfg.TryGetValue("baudrate", out var baudObj)
                ? Convert.ToInt32(baudObj)
                : 9600;

            var dataBits = cfg.TryGetValue("databits", out var dataBitsObj)
                ? Convert.ToInt32(dataBitsObj)
                : 8;

            // 열거형은 대소문자 무시하고 파싱
            var parity = cfg.TryGetValue("parity", out var parityObj)
                ? Enum.Parse<Parity>(Convert.ToString(parityObj) ?? "None", ignoreCase: true)
                : Parity.None;

            var stopBits = cfg.TryGetValue("stopbits", out var stopBitsObj)
                ? Enum.Parse<StopBits>(Convert.ToString(stopBitsObj) ?? "One", ignoreCase: true)
                : StopBits.One;

            serial_port_.PortName = portName;
            serial_port_.BaudRate = baudRate;     // ← 이제 57600 잘 들어갑니다
            serial_port_.DataBits = dataBits;
            serial_port_.Parity = parity;
            serial_port_.StopBits = stopBits;

            if (LogConfig.IsTest) Log($"Configured UART: Port={serial_port_.PortName}, BaudRate={serial_port_.BaudRate}, DataBits={serial_port_.DataBits}, Parity={serial_port_.Parity}, StopBits={serial_port_.StopBits}");
        }


        /// <summary>
        /// UART의 현재 상태를 반환합니다.
        /// </summary>
        public override IDictionary<string, object?> GetStatus()
        {
            return new Dictionary<string, object?>
            {
                { "is_connected", _isConnected },
                { "portname", serial_port_.PortName },
                { "baudrate", serial_port_.BaudRate },
                { "databits", serial_port_.DataBits },
                { "parity", serial_port_.Parity.ToString() }, // Enum을 문자열로 변환
                { "stopbits", serial_port_.StopBits.ToString() } // Enum을 문자열로 변환
            };
        }

        /// <summary>
        /// UART 연결을 설정합니다.
        /// </summary>
        public bool Connect()
        {
            if (serial_port_.IsOpen)
                serial_port_.Close();



            serial_port_.DtrEnable = false; // DTR 설정
            serial_port_.RtsEnable = false; // RTS 설정

            try
            {
                if (LogConfig.IsTest) Log($"Opening UART Port: {serial_port_.PortName}");
                serial_port_.Open();
                _isConnected = true;
                if (LogConfig.IsTest) Log($"UART Connected: {serial_port_.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                Log($"UART Connection Failed: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        private void ClearUartBuffer(Action<string>? log = null, int maxMs = 300)
        {
            if (!_isConnected) return;
            try { serial_port_.DiscardInBuffer(); } catch { }
            try { serial_port_.DiscardOutBuffer(); } catch { }

            var stop = DateTime.UtcNow.AddMilliseconds(maxMs);
            while (DateTime.UtcNow < stop)
            {
                try
                {
                    if (serial_port_.BytesToRead == 0) break;
                    _ = serial_port_.ReadExisting(); // 드라이버 잔여도 비움
                }
                catch { break; }
                System.Threading.Thread.Sleep(10);
            }
        }


        /// <summary>
        /// UART 연결을 종료합니다.
        /// </summary>
        public void Disconnect()
        {
            if (serial_port_.IsOpen)
            {
                serial_port_.Close(); // 주석 제거
                _isConnected = false;
            }
        }

        /// <summary>
        /// UART 통신의 타임아웃을 설정합니다.
        /// </summary>
        /// <param name="timeout">타임아웃(밀리초).</param>
        public void SetTimeout(int timeout)
        {
            //if (_isConnected)
            //    throw new InvalidOperationException("Cannot change timeout while connected.");

            //serial_port_.ReadTimeout = timeout;
            //serial_port_.WriteTimeout = timeout;

            if (timeout <= 0)
                throw new ArgumentException("Timeout 값은 0보다 커야 합니다.");


            serial_port_.ReadTimeout = timeout > 0 ? timeout : 1000; // 기본 타임아웃 1초
            serial_port_.WriteTimeout = timeout > 0 ? timeout : 1000; // 기본 타임아웃 1초
            Log($"Timeout 설정: ReadTimeout={serial_port_.ReadTimeout}, WriteTimeout={serial_port_.WriteTimeout}");
        }


        /// <summary>
        /// 데이터를 UART로 전송합니다.
        /// </summary>
        /// <param name="message">전송할 데이터.</param>


        public void Send(IDictionary<string, object?> param)
        {
            if (!_isConnected)
                throw new InvalidOperationException("UART is not connected.");

            try
            {
                string dataToSend = param.ContainsKey("body")
                    ? param["body"]?.ToString() ?? throw new InvalidOperationException("'body'는 필수 매개변수입니다.")
                    : throw new InvalidOperationException("'body'가 설정되지 않았습니다.");

                // ▶ 개행을 붙이지 않도록 제어할 수 있게 옵션 추가
                bool noNewline = param.ContainsKey("no_newline") && param["no_newline"] is bool b && b;

                serial_port_.Write(noNewline ? dataToSend : (dataToSend + "\n"));
            }
            catch (Exception e)
            {
                Log($"데이터 전송 실패: {e.Message}");
                throw;
            }
        }




        /// <summary>
        /// UART로부터 데이터를 수신합니다.
        /// </summary>
        /// <param name="par">수신 옵션 또는 매개변수(사용하지 않을 수 있음).</param>
        /// <returns>수신한 데이터.</returns>


        public IDictionary<string, object?>? Recv(IDictionary<string, object?>? param)
        {
            if (!_isConnected)
                throw new InvalidOperationException("UART is not connected.");

            try
            {
                // 타임아웃 적용
                int timeout = 5000;
                if (param?.ContainsKey("timeout") == true)
                    timeout = Convert.ToInt32(param["timeout"]);
                serial_port_.ReadTimeout = Math.Max(1, timeout);

                // ▶ recv_mode: "line"(기본) | "etx"
                string mode = param?.ContainsKey("recv_mode") == true
                    ? (param["recv_mode"]?.ToString() ?? "line")
                    : "line";

                if (string.Equals(mode, "etx", StringComparison.OrdinalIgnoreCase))
                {
                    // ETX(0x03) 도달까지 읽기, 필요 시 STX(0x02) 이후부터만 캡처
                    bool stxGuard = param?.ContainsKey("stx_guard") == true && param["stx_guard"] is bool g && g;
                    int etx = param?.ContainsKey("etx") == true ? Convert.ToInt32(param["etx"]) : 0x03;

                    var sb = new System.Text.StringBuilder();
                    var deadline = DateTime.UtcNow.AddMilliseconds(timeout);
                    bool started = !stxGuard; // stxGuard면 STX가 올 때까지 무시

                    while (DateTime.UtcNow < deadline)
                    {
                        try
                        {
                            int b = serial_port_.ReadByte(); // ReadTimeout 적용
                            if (b == -1) continue;

                            if (!started)
                            {
                                if (b == 0x02) started = true; // STX
                                continue;
                            }

                            if (b == etx) break; // ETX 도달하면 종료
                            sb.Append((char)b);
                        }
                        catch (TimeoutException)
                        {
                            // 전체 타임아웃 전에 조기 타임아웃이 올 수 있으니 루프 계속
                            break;
                        }
                    }

                    string received = sb.ToString(); // Trim 불필요: ETX로 정확히 끊음
                    return new Dictionary<string, object?> { { "value", received } };
                }
                else
                {
                    // 기존 라인 모드 유지 (기존 코드와 호환)
                    string line = serial_port_.ReadLine().TrimEnd('\r', '\n');
                    return new Dictionary<string, object?> { { "value", line } };
                }
            }
            catch (TimeoutException)
            {
                // ReadLine()은 개행이 없으면 Timeout이 나므로, 버퍼에 남은 데이터가 있으면 회수한다.
                try
                {
                    if (serial_port_.BytesToRead > 0)
                    {
                        string remain = serial_port_.ReadExisting().TrimEnd('\r', '\n');
                        if (!string.IsNullOrWhiteSpace(remain))
                            return new Dictionary<string, object?> { { "value", remain } };
                    }
                }
                catch
                {
                    // ignore and return null below
                }

                return new Dictionary<string, object?> { { "value", null } };
            }
            catch (IOException ex)
            {
                //Log($"I/O 예외 발생: {ex.Message}");
                return new Dictionary<string, object?> { { "value", null } };
            }
        }




        /// <summary>
        /// 실행을 시작합니다.
        /// </summary>
        public void Begin()
        {
            //Log("library begin");
            this.Connect();
        }

        /// <summary>
        /// 실행을 종료합니다.
        /// </summary>
        public void End()
        {
            this.Disconnect();
            //Log("library end");
        }

        public void Dispose()
        {
            if (_isConnected)
                this.Disconnect();

            if (serial_port_ != null)
            {
                if (serial_port_.IsOpen)
                {
                    serial_port_.Close();
                }
                serial_port_.Dispose();
            }
        }


        public void EnsureConnected()
        {
            if (!_isConnected)
            {
                try
                {
                    Log("UART 연결을 시도합니다...");
                    Connect();  // UART 연결 시도
                    _isConnected = IsConnected;
                    Log($"UART 연결 상태: {_isConnected}");
                }
                catch (Exception ex)
                {
                    Log($"UART 재연결 실패: {ex.Message}");
                }
            }
        }

        // Uart 클래스 내부 (다른 메서드들과 동일한 레벨)
        private void ClearSocketBuffer(Action<string> logAction)
        {
            if (!_isConnected) return;
            try { serial_port_.DiscardInBuffer(); } catch { }
            try { serial_port_.DiscardOutBuffer(); } catch { }
        }


        /// <summary>
        /// proc_01 : 통신 검사
        /// 특정 데이터를 송신한 후 수신된 데이터를 확인
        /// </summary>
        // 플래그/태그를 실제 STX/ETX로 변환
        private static string DecodeFramingAliases(string s, out bool framed)
        {
            framed = false;
            if (string.IsNullOrEmpty(s)) return s;

            string t = s;
            // STX/ETX 표기 → 제어문자
            if (t.Contains("#02") || t.Contains("<STX>") || t.Contains("\\u0002")) { t = t.Replace("#02", "\u0002").Replace("<STX>", "\u0002").Replace("\\u0002", "\u0002"); framed = true; }
            if (t.Contains("#03") || t.Contains("<ETX>") || t.Contains("\\u0003")) { t = t.Replace("#03", "\u0003").Replace("<ETX>", "\u0003").Replace("\\u0003", "\u0003"); framed = true; }

            // 개행/캐리지리턴 토큰도 지원 (장비가 CR/CRLF 요구 시)
            t = t.Replace("<CR>", "\r").Replace("<LF>", "\n").Replace("#0D", "\r").Replace("#0A", "\n");

            return t;
        }

        public IDictionary<string, object?> UARTMSG(
    IDictionary<string, object?> libraries,
    IDictionary<string, object?> param,
    Action<string> logAction)
        {
            // 0) tests 배열 (또는 과거 호환: send_data/recv_data/recv_par → tests로 변환)
            IEnumerable<dynamic>? tests = null;
            if (param.ContainsKey("tests"))
            {
                tests = param["tests"] as IEnumerable<dynamic>;
            }
            else if (param.ContainsKey("send_data")) // 과거 파라미터 호환
            {
                var t = new Dictionary<string, object?>
        {
            { "command",            param["send_data"]?.ToString() ?? "" },
            { "expected_response",  param.ContainsKey("recv_data") ? param["recv_data"]?.ToString() ?? "" : "" }
        };
                tests = new List<Dictionary<string, object?>>() { t };
                // timeout/delay는 아래 공통 파라미터 사용
            }

            if (tests == null || !tests.Any())
            {
                logAction("UARTMSG: tests 배열이 비어있거나 존재하지 않습니다.");
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");
            }

            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;
            int delay = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 100;
            bool noNewline = param.ContainsKey("no_newline") && param["no_newline"] is bool b && b;

            // 1) 연결 보장
            bool IsConnected()
            {
                try
                {
                    var st = GetStatus() as IDictionary<string, object>;
                    return st != null && st.TryGetValue("is_connected", out var v) && v is bool b2 && b2;
                }
                catch { return false; }
            }
            if (!IsConnected())
            {
                try { EnsureConnected(); } catch { }
                if (!IsConnected()) try { Connect(); } catch { }
                if (!IsConnected()) try { Begin(); } catch { }
            }
            if (!IsConnected())
            {
                logAction("UARTMSG: UART 연결 실패 (포트가 열려있지 않습니다).");
                return new Dictionary<string, object?> {
            { "command", "" }, { "response", "" }, { "expected", "" }, { "success", false }
        };
            }

            // 2) 시작 시 간단 버퍼 클리어
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    if (serial_port_.IsOpen && serial_port_.BytesToRead > 0)
                    {
                        _ = serial_port_.ReadExisting();
                        Thread.Sleep(30);
                    }
                    else break;
                }
            }
            catch { /* 무시 */ }

            Thread.Sleep(120); // 안정화

            // 3) 테스트 실행
            string lastCommand = "";
            string lastResponse = "";
            string expectedResponse = "";
            bool finalResult = true;

            foreach (var testObj in tests)
            {
                var test = testObj as IDictionary<string, object>;
                if (test == null || !test.ContainsKey("command"))
                {
                    logAction("UARTMSG: 테스트 항목에 'command'가 없습니다.");
                    continue;
                }

                string command = test["command"]?.ToString() ?? "";
                expectedResponse = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";
                lastCommand = command;

                logAction($"명령어 전송: {command}");
                try
                {
                    Send(new Dictionary<string, object?> {
                { "body", command },
                { "no_newline", noNewline }
            });
                }
                catch (Exception sendEx)
                {
                    logAction($"UARTMSG Send 예외: {sendEx.Message}");
                    finalResult = false;
                    continue;
                }

                Thread.Sleep(delay);

                var response = Recv(new Dictionary<string, object?> {
            { "timeout", timeout }
        });

                lastResponse = (response != null && response.ContainsKey("value"))
                    ? (response["value"]?.ToString() ?? "")
                    : "";

                if (string.IsNullOrWhiteSpace(lastResponse))
                {
                    logAction("응답 없음 (Timeout 또는 연결 문제)");
                    finalResult = false;
                    continue;
                }

                logAction($"수신된 메시지: {lastResponse}");

                bool match;
                if (string.IsNullOrEmpty(expectedResponse))
                    match = !string.IsNullOrWhiteSpace(lastResponse);   // 기대값 없으면 응답 유무로 판단
                else
                    match = lastResponse.Contains(expectedResponse);    // 부분매치 (TCPMSG와 동일)

                finalResult &= match;
                logAction($"응답 비교 - 기대값: {expectedResponse}, 실제값: {lastResponse}, 성공 여부: {(match ? "일치" : "불일치")}");
            }

            // 4) 종료 시 버퍼 정리(선택)
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    if (serial_port_.IsOpen && serial_port_.BytesToRead > 0)
                    {
                        _ = serial_port_.ReadExisting();
                        Thread.Sleep(30);
                    }
                    else break;
                }
            }
            catch { /* 무시 */ }

            return new Dictionary<string, object?>
    {
        { "command",  lastCommand },
        { "response", lastResponse },
        { "expected", expectedResponse },
        { "success",  finalResult }
    };
        }
        //함수 종료
    }
}
