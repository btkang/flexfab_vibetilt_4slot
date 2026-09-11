using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;


namespace Cantops.FlexFab
{
    public class Pim : ModuleBase, IProcess
    {
        private IComm _uartHandle;  // UART 핸들 저장
        private IComm _tcpHandle;   // TCP 핸들 저장
        private IProtocol? ctsp_ = null; // PreRun에서 초기화
        private IComm? comm_; // comm_ 멤버 변수를 추가

        public const string MODULE_VERSION = "1.0"; // 버전 정보

        public Pim()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "pim" },
                { "desc", "PIM" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object>> {
                    new Dictionary<string, object> {
                        { "id", "pim_proc_01" },
                        { "desc", "DT보드 검사" },
                    }
                }}
            };
        }

        

        public override IDictionary<string, object?>? GetStatus()
        {
            // 원하는 상태 정보를 반환
            return new Dictionary<string, object?>
            {
                { "Status", "OK" },
                { "Description", "PIM 상태 정상" }
            };
        }

        /*public void SetConfig(IDictionary<string, object> config)
        {
            config_ = config;
            if (config.TryGetValue("log_action", out var value) && value is Action<string> logact)
            {
                log_action_ = logact;
            }
            else
            {
                throw new InvalidOperationException("Log action이 설정되지 않았습니다.");
            }
        }*/ //삭제

        public void Initialize(IDictionary<string, object> libraries)
        {
            if (config_ == null)
            {
                throw new InvalidOperationException("config_가 null입니다.");
            }

            // UART 및 TCP 핸들 초기화
            foreach (var key in config_.Keys)
            {
                if (key.StartsWith("uart"))
                {
                    string uartId = config_[key].ToString();
                    Log($"pim: 설정된 UART ID = {uartId}");

                    _uartHandle = libraries.ContainsKey(uartId) ? libraries[uartId] as IComm : null;
                    if (_uartHandle == null)
                    {
                        Log($"pim: {uartId} UART 핸들을 찾을 수 없습니다.");
                        continue;
                    }

                    Log($"pim: {uartId} UART 핸들 정상적으로 로드됨.");
                }
                else if (key.StartsWith("tcp"))
                {
                    string tcpId = config_[key].ToString();
                    _tcpHandle = libraries.ContainsKey(tcpId) ? libraries[tcpId] as IComm : null;
                    if (_tcpHandle == null)
                    {
                        Log($"pim: {tcpId} TCP 핸들을 찾을 수 없습니다.");
                        continue;
                    }

                    Log($"pim: {tcpId} TCP 핸들 정상적으로 로드됨.");
                }
            }
        }


        public void Begin()
        {
            //Log("pim: Begin 호출됨");
        }

        public void End()
        {
            //Log("pim: End 호출됨");
        }

        private void Write(IDictionary<string, object?> param)
        {
            string portType = param.ContainsKey("porttype") ? param["porttype"]?.ToString() : "uart";  // 기본값은 'uart'

            if (portType == "uart")
            {
                if (_uartHandle == null)
                {
                    Log($"Write() 호출 시 _uartHandle이 null입니다. Initialize()가 정상 실행되었는지 확인하세요.");
                    throw new InvalidOperationException("UART 핸들이 초기화되지 않았습니다.");
                }

                if (!param.ContainsKey("body"))
                {
                    throw new ArgumentException("Write 매개변수에 'body'가 필요합니다.");
                }

                string dataToSend = param["body"]?.ToString() ?? throw new ArgumentNullException("body");

                Log($"UART로 전송할 데이터: {dataToSend}");

                _uartHandle.Send(param);  // Send에 param을 전달
                //Log("데이터 전송 완료");
            }
            else if (portType == "tcp")
            {
                if (_tcpHandle == null)
                {
                    Log($"Write() 호출 시 _tcpHandle이 null입니다. Initialize()가 정상 실행되었는지 확인하세요.");
                    throw new InvalidOperationException("TCP 핸들이 초기화되지 않았습니다.");
                }

                if (!param.ContainsKey("body"))
                {
                    throw new ArgumentException("Write 매개변수에 'body'가 필요합니다.");
                }

                string dataToSend = param["body"]?.ToString() ?? throw new ArgumentNullException("body");

                Log($"TCP로 전송할 데이터: {dataToSend}");

                _tcpHandle.Send(param);  // Send에 param을 전달
                //Log("데이터 전송 완료");
            }
            else
            {
                Log("알 수 없는 포트 타입입니다.");
                throw new InvalidOperationException("알 수 없는 포트 타입");
            }
        }




        private IDictionary<string, object?> Read(IDictionary<string, object?> param)
        {
            string portType = param.ContainsKey("porttype") ? param["porttype"]?.ToString() : "uart";

            if (portType == "uart")
            {
                if (_uartHandle == null)
                    throw new InvalidOperationException("UART 핸들이 초기화되지 않았습니다.");

                // ⚠️ param 그대로 전달 (recv_mode/etx 등의 옵션을 그대로 흡수)
                var recvObj = _uartHandle.Recv(param);

                // ✅ 딕셔너리/문자열 안전 추출
                string receivedData = ExtractValueSafe(recvObj);
                Log($"UART 수신된 원본 데이터: {receivedData}");

                return new Dictionary<string, object?> { { "value", receivedData } };
            }
            else if (portType == "tcp")
            {
                if (_tcpHandle == null)
                    throw new InvalidOperationException("TCP 핸들이 초기화되지 않았습니다.");

                var recvObj = _tcpHandle.Recv(param);
                string receivedData = ExtractValueSafe(recvObj);
                Log($"TCP 수신된 원본 데이터: {receivedData}");

                return new Dictionary<string, object?> { { "value", receivedData } };
            }
            else
            {
                Log("알 수 없는 포트 타입입니다.");
                throw new InvalidOperationException("알 수 없는 포트 타입");
            }
        }
        // Pim 클래스 내부 어딘가 (private helper)
        private static string ExtractValueSafe(object? recv)
        {
            if (recv == null) return string.Empty;

            // { "value": ... } 형태면 값만 추출
            if (recv is IDictionary<string, object?> d)
            {
                if (d.TryGetValue("value", out var v) && v != null)
                    return v.ToString() ?? string.Empty;
                return string.Empty;
            }

            // 문자열 그대로
            if (recv is string s) return s;

            // 그 외 타입
            return recv.ToString() ?? string.Empty;
        }

        private void ClearSocketBuffer(string portType, Action<string> logAction, int maxAttempts = 5, int timeout = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var flushParam = new Dictionary<string, object?> {
            { "timeout", timeout },
            { "porttype", portType }
        };

                if (ctsp_ != null)
                {
                    // ctsp_에서 DoCommand를 사용하여 수신 데이터를 비움
                    var response = ctsp_.DoCommand(flushParam);

                    if (response != null && response.ContainsKey("cmd"))
                    {
                        string leftover = response["cmd"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(leftover))
                        {
                            logAction($"잔여 응답 버퍼 비움: [{leftover}]");
                        }
                    }
                }
                else
                {
                    logAction("ctsp_ 인스턴스가 null입니다. 응답 버퍼를 비울 수 없습니다.");
                }
            }
        }





        public override void PreRun(IDictionary<string, object?> libraries)
        {
            Log("PIM: PreRun() 실행 중...");

            // 'ctsplib'이 없으면 'ctsp'로 기본값 설정
            if (config_ == null || !config_.TryGetValue("ctsplib", out var libnameObj) || libnameObj is not string libname)
            {
                libname = "ctsp"; // 기본값으로 "ctsp" 할당
                config_["ctsplib"] = libname;  // config_에 'ctsplib' 키 설정
                Log($"config_에 'ctsplib' 키가 없으므로 기본값 'ctsp'로 설정됨.");
            }
            else
            {
                Log($"config_에 'ctsplib' 키가 이미 설정되어 있습니다. 값: {libname}");
            }

            // CTSP 라이브러리 초기화
            if (!libraries.ContainsKey(libname))
            {
                throw new KeyNotFoundException($"CTSP 라이브러리 '{libname}'를 찾을 수 없습니다.");
            }
            ctsp_ = (IProtocol?)libraries[libname];
            if (ctsp_ == null)
            {
                throw new InvalidOperationException($"'{libname}' 라이브러리가 초기화되지 않았습니다.");
            }

            Log("CTSP 라이브러리 초기화 완료");

            // comm_ 객체 설정 (예: "tcp_1" 키에 해당하는 IComm 구현체 가져오기)
            if (!libraries.ContainsKey("tcp_1"))
            {
                throw new InvalidOperationException($"'tcp_1' 커뮤니케이션 핸들이 초기화되지 않았습니다.");
            }

            comm_ = (IComm?)libraries["tcp_1"]; // "tcp_1" 포트에 해당하는 comm_ 객체 할당
            if (comm_ == null)
            {
                throw new InvalidOperationException($"'tcp_1' 커뮤니케이션 핸들이 초기화되지 않았습니다.");
            }

            Log("PIM: CTSP 라이브러리 및 커뮤니케이션 핸들 초기화 완료.");
        }







        public override void PostRun()
        {
            Log("PIM: PostRun() - CTSP 라이브러리 해제");
            ctsp_ = null; // CTSP 객체 해제
            comm_ = null; // comm_ 객체 해제
        }



        // PIM_DPM DPM(Uart), Portremote(TCP) 조합 사용, DPM으로 이동예정
        /*public Dictionary<string, object> PIM_DPM(IDictionary<string, object> libraries, IDictionary<string, object> param, Action<string> logAction)
        {
            // log_action_에 logAction을 저장
            this.log_action_ = logAction;

            Log("PIM_DPM 실행 시작");

            // DPM 인스턴스 가져오기
            if (!libraries.ContainsKey("dpm"))
            {
                Log("PIM_DPM: 'dpm' 라이브러리가 없습니다.");
                return new Dictionary<string, object> { { "success", false }, { "error", "DPM 라이브러리를 찾을 수 없음" } };
            }

            var dpm = libraries["dpm"] as IDevice;
            if (dpm == null)
            {
                Log("PIM_DPM: DPM 핸들을 가져올 수 없습니다.");
                return new Dictionary<string, object> { { "success", false }, { "error", "DPM 핸들 없음" } };
            }

            // param에서 값 가져오기
            if (!param.ContainsKey("id") || !param.ContainsKey("min") || !param.ContainsKey("max") || !param.ContainsKey("porttype"))
            {
                Log("PIM_DPM: 'id', 'min', 'max', 'porttype' 값이 필요합니다.");
                return new Dictionary<string, object> { { "success", false }, { "error", "필수 파라미터 누락" } };
            }

            int id = Convert.ToInt32(param["id"]);
            double minValue = Convert.ToDouble(param["min"]);
            double maxValue = Convert.ToDouble(param["max"]);
            string porttype = param["porttype"].ToString();  // porttype을 받음

            // DPM을 사용해 "ID{id}=ADC" 전송
            string command = $"ID{id}=ADC";
            Log($"PIM_DPM: 명령어 전송 - {command}");

            // 포트타입에 따른 Write 호출
            var writeParam = new Dictionary<string, object> { { "body", command }, { "porttype", porttype } };
            if (porttype == "uart")
            {
                dpm.Write(writeParam);  // Write 함수에 param 하나만 전달
            }
            else if (porttype == "tcp")
            {
                dpm.Write(writeParam);  // Write 함수에 param 하나만 전달
            }
            else
            {
                Log($"PIM_DPM: 알 수 없는 포트타입 '{porttype}'입니다.");
                return new Dictionary<string, object> { { "success", false }, { "error", "잘못된 포트타입" } };
            }

            Log($"PIM_DPM: 명령어 전송 완료 - {command}");

            // 데이터 수신
            Log($"PIM_DPM: dpm.Read() 실행");

            // 포트타입에 따른 Read 호출
            var readParam = new Dictionary<string, object> { { "timeout", 5000 }, { "porttype", porttype } };
            var result = dpm.Read(readParam);  // Read 함수에 param 하나만 전달

            // 수신된 데이터 확인
            if (result == null || !result.ContainsKey("value"))
            {
                Log("PIM_DPM: 수신된 데이터가 없습니다.");
                return new Dictionary<string, object> { { "success", false }, { "error", "데이터 수신 실패" } };
            }

            double receivedValue;
            if (!double.TryParse(result["value"].ToString(), out receivedValue))
            {
                Log("PIM_DPM: 수신된 데이터를 숫자로 변환할 수 없습니다.");
                return new Dictionary<string, object> { { "success", false }, { "error", "수신된 데이터 변환 실패" } };
            }

            Log($"PIM_DPM: 수신된 값: {receivedValue}");

            // 값 검증
            bool isValid = (receivedValue >= minValue && receivedValue <= maxValue);
            Log($"PIM_DPM: 값 검증 - min: {minValue}, max: {maxValue}, 수신값: {receivedValue}, 성공 여부: {isValid}");

            return new Dictionary<string, object>
    {
        { "success", isValid },
        { "id", id },
        { "value", receivedValue }
    };
        }*/

        private void ClearCommBuffer(string portType, Action<string> logAction, int maxAttempts = 5, int timeout = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var flushParam = new Dictionary<string, object?> {
                    { "timeout", timeout },
                    { "porttype", portType }
                };

                var resp = Read(flushParam);
                if (resp != null && resp.ContainsKey("value"))
                {
                    var leftover = resp["value"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(leftover))
                    {
                        //logAction($"잔여 응답 버퍼 비움: [{leftover}]");
                        continue;
                    }
                }
                break; // 더 이상 남은 게 없으면 종료
            }
        }
        public IDictionary<string, object?> PIM_TCPMSG(
            IDictionary<string, object> libraries,
            IDictionary<string, object> param,
            Action<string> logAction)
        {
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null || !tests.Any())
            {
                logAction("tests 배열이 비어있거나 존재하지 않습니다.");
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");
            }

            // 핸들 보장 (Initialize가 못 채웠을 때 대비)
            string portId = param.ContainsKey("port") ? param["port"]?.ToString() ?? "tcp_1" : "tcp_1";
            if (_tcpHandle == null && libraries.ContainsKey(portId))
            {
                _tcpHandle = libraries[portId] as IComm;
                logAction($"PIM_TCPMSG: _tcpHandle이 없어 libraries['{portId}']로 자동 세팅.");
            }
            if (_tcpHandle == null)
            {
                logAction("PIM_TCPMSG: TCP 핸들이 없습니다.");
                return new Dictionary<string, object?> { { "success", false }, { "error", "TCP handle null" } };
            }

            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;
            int delay = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 100;

            // 시작 시 버퍼 정리
            ClearCommBuffer("tcp", logAction);

            string lastCommand = "";
            string lastResponse = "";
            string expectedValue = "";
            bool finalResult = true;

            foreach (var testObj in tests)
            {
                var test = testObj as IDictionary<string, object>;
                if (test == null || !test.ContainsKey("command"))
                {
                    logAction("테스트 항목에 'command'가 없습니다.");
                    continue;
                }

                string command = test["command"]?.ToString() ?? "";
                expectedValue = test.ContainsKey("expected_response") ? test["expected_response"]?.ToString() ?? "" : "";
                lastCommand = command;

                logAction($"명령어 전송: {command}");
                Write(new Dictionary<string, object?> {
                    { "body", command },
                    { "porttype", "tcp" }
                });

                System.Threading.Thread.Sleep(delay);

                var resp = Read(new Dictionary<string, object?> {
                    { "timeout",  timeout },
                    { "porttype", "tcp" }
                });

                lastResponse = (resp != null && resp.ContainsKey("value") && resp["value"] != null)
                               ? resp["value"]!.ToString()!
                               : "";

                if (string.IsNullOrWhiteSpace(lastResponse))
                {
                    logAction("응답 없음 (Timeout 또는 연결 문제)");
                    finalResult = false;
                    continue;
                }

                logAction($"수신된 메시지: {lastResponse}");
                bool match = string.IsNullOrEmpty(expectedValue) ? true : lastResponse.Contains(expectedValue);
                finalResult &= match;

                logAction($"응답 비교 - 기대값: {expectedValue}, 실제값: {lastResponse}, 성공 여부: {(match ? "일치" : "불일치")}");
            }

            // 끝나고도 버퍼 정리
            ClearCommBuffer("tcp", logAction);

            return new Dictionary<string, object?>
            {
                { "command",  lastCommand },
                { "response", lastResponse },
                { "expected", expectedValue },
                { "success",  finalResult }
            };
        }

        // UART 메시지 테스트 (TCPMSG와 동일 + UART 옵션 전달)
        public IDictionary<string, object?> PIM_UARTMSG(
            IDictionary<string, object> libraries,
            IDictionary<string, object> param,
            Action<string> logAction)
        {
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null || !tests.Any())
            {
                logAction("tests 배열이 비어있거나 존재하지 않습니다.");
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");
            }

            // 핸들 보장
            string portId = param.ContainsKey("port") ? param["port"]?.ToString() ?? "uart_dpm" : "uart_dpm";
            if (_uartHandle == null && libraries.ContainsKey(portId))
            {
                _uartHandle = libraries[portId] as IComm;
                logAction($"PIM_UARTMSG: _uartHandle이 없어 libraries['{portId}']로 자동 세팅.");
            }
            if (_uartHandle == null)
            {
                logAction("PIM_UARTMSG: UART 핸들이 없습니다.");
                return new Dictionary<string, object?> { { "success", false }, { "error", "UART handle null" } };
            }

            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;
            int delay = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 100;

            // 선택 옵션 (필요시만 사용)
            bool noNewline = param.ContainsKey("no_newline") && param["no_newline"] is bool b && b;
            string recvMode = param.ContainsKey("recv_mode") ? (param["recv_mode"]?.ToString() ?? "line") : "line"; // "line" | "etx"
            bool stxGuard = param.ContainsKey("stx_guard") && param["stx_guard"] is bool g && g;

            // 시작 시 버퍼 정리
            ClearCommBuffer("uart", logAction);

            string lastCommand = "";
            string lastResponse = "";
            string expectedValue = "";
            bool finalResult = true;

            foreach (var testObj in tests)
            {
                var test = testObj as IDictionary<string, object>;
                if (test == null || !test.ContainsKey("command"))
                {
                    logAction("테스트 항목에 'command'가 없습니다.");
                    continue;
                }

                string command = test["command"]?.ToString() ?? "";
                expectedValue = test.ContainsKey("expected_response") ? test["expected_response"]?.ToString() ?? "" : "";
                lastCommand = command;

                logAction($"명령어 전송: {command}");
                Write(new Dictionary<string, object?> {
                    { "body", command },
                    { "porttype", "uart" },
                    { "no_newline", noNewline }   // Uart.Send가 지원
                });

                System.Threading.Thread.Sleep(delay);

                var resp = Read(new Dictionary<string, object?> {
                    { "timeout",   timeout },
                    { "porttype",  "uart" },
                    { "recv_mode", recvMode },   // Uart.Recv가 지원 ("line"|"etx")
                    { "stx_guard", stxGuard }    // etx 모드에서 STX 이후부터만
                });

                lastResponse = (resp != null && resp.ContainsKey("value") && resp["value"] != null)
                               ? resp["value"]!.ToString()!
                               : "";

                if (string.IsNullOrWhiteSpace(lastResponse))
                {
                    logAction("응답 없음 (Timeout 또는 프레이밍 불일치)");
                    finalResult = false;
                    continue;
                }

                logAction($"수신된 메시지: {lastResponse}");
                bool match = string.IsNullOrEmpty(expectedValue) ? true : lastResponse.Contains(expectedValue);
                finalResult &= match;

                logAction($"응답 비교 - 기대값: {expectedValue}, 실제값: {lastResponse}, 성공 여부: {(match ? "일치" : "불일치")}");
            }

            // 끝나고도 버퍼 정리
            ClearCommBuffer("uart", logAction);

            return new Dictionary<string, object?>
            {
                { "command",  lastCommand },
                { "response", lastResponse },
                { "expected", expectedValue },
                { "success",  finalResult }
            };
        }

        public Dictionary<string, object> PIM_RTC_SET(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
        {
            // 0) 사용할 포트 결정: param["port"] > tcp_2 > tcp_1
            string portId =
                (param.ContainsKey("port") && param["port"] is string p && !string.IsNullOrWhiteSpace(p)) ? p :
                (libraries.ContainsKey("tcp_2") ? "tcp_2" :
                (libraries.ContainsKey("tcp_1") ? "tcp_1" : null));

            if (string.IsNullOrEmpty(portId) || !libraries.ContainsKey(portId))
                return new Dictionary<string, object> { { "success", false }, { "error", "사용할 TCP 핸들을 찾을 수 없습니다." } };

            if (!(libraries[portId] is IComm tcp))
                return new Dictionary<string, object> { { "success", false }, { "error", $"TCP 핸들 캐스팅 실패 (portId='{portId}')" } };

            // 1) 연결 보장
            bool IsConnected(IComm c)
            {
                try
                {
                    var st = c.GetStatus();
                    return st != null && st.TryGetValue("Connected", out var v) && v is bool b && b;
                }
                catch { return false; }
            }
            if (!IsConnected(tcp))
            {
                try { tcp.GetType().GetMethod("Begin")?.Invoke(tcp, null); } catch { }
                if (!IsConnected(tcp))
                {
                    try { tcp.GetType().GetMethod("Connect")?.Invoke(tcp, null); } catch { }
                }
            }
            if (!IsConnected(tcp))
                return new Dictionary<string, object> { { "success", false }, { "error", $"TCP 연결 실패 (portId='{portId}')" } };

            // 2) 파라미터
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null)
                throw new InvalidOperationException("tests 리스트가 유효하지 않습니다.");

            int delay = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 1000;
            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            // (retmsg용) id/name: __proc_* 우선 → proc_* → 기본값
            string procId =
                (param.TryGetValue("__proc_id", out var pid0) && pid0 != null) ? pid0.ToString()! :
                (param.TryGetValue("proc_id", out var pid1) && pid1 != null) ? pid1.ToString()! :
                "PIM_RTC_SET";

            string procName =
                (param.TryGetValue("__proc_name", out var pn0) && pn0 != null) ? pn0.ToString()! :
                (param.TryGetValue("proc_name", out var pn1) && pn1 != null) ? pn1.ToString()! :
                "RTC";

            // 3) 시작 시 잔여 수신 비우기
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    var pre = tcp.Recv(new Dictionary<string, object?> { { "timeout", 100 }, { "porttype", "tcp" } });
                    if (ExtractValue(pre).Length == 0) break;
                }
                catch { break; }
            }

            bool overall = true;
            var retLines = new List<string>(); // ← JSON 한 줄씩 누적

            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null || !test.ContainsKey("command") || !test.ContainsKey("expected_response"))
                    throw new InvalidOperationException("테스트 항목이 잘못되었습니다.");

                // yyyyMMddHHmmss 형식으로 현재 시간 삽입
                string now = DateTime.Now.ToString("yyyyMMddHHmmss");
                string prefix = test["command"]?.ToString() ?? "";
                string command = $"{prefix}{now}>";

                logAction($"전송할 명령어: {command}");
                logAction($"명령어 전송: {command}");

                tcp.Send(new Dictionary<string, object?> {
            { "body", command },
            { "porttype", "tcp" }
        });

                // 처리 시간 대기
                System.Threading.Thread.Sleep(delay);

                var respObj = tcp.Recv(new Dictionary<string, object?> {
            { "timeout",  timeout },
            { "porttype", "tcp" }
        });
                string received = ExtractValue(respObj);
                logAction($"수신된 메시지: {received}");

                string expected = test["expected_response"]?.ToString() ?? "";
                bool ok = string.Equals(received.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase);
                logAction($"응답 비교 - 기대값: {expected}, 실제값: {received}, 성공 여부: {ok}");
                overall &= ok;

                // ★ 여기! 리턴 메시지 최소화: Name, id, Exp, Rsp
                var retObj = new JObject
                {
                    ["Name"] = procName,
                    ["id"] = procId,
                    ["Exp"] = expected,
                    ["Rsp"] = received
                };
                retLines.Add(retObj.ToString(Newtonsoft.Json.Formatting.None));

                // 응답 후 잔여 버퍼 정리
                for (int i = 0; i < 2; i++)
                {
                    var flush = tcp.Recv(new Dictionary<string, object?> { { "timeout", 100 }, { "porttype", "tcp" } });
                    if (ExtractValue(flush).Length == 0) break;
                }
            }

            return new Dictionary<string, object> {
        { "success", overall },
        { "retmsg",  string.Join(Environment.NewLine, retLines) }
    };

            // ---- Recv 반환 안전 추출 ----
            static string ExtractValue(object? x)
            {
                if (x == null) return "";
                if (x is IDictionary<string, object?> d)
                    return d.TryGetValue("value", out var v) && v != null ? v.ToString() ?? "" : "";
                if (x is string s) return s;
                return x.ToString() ?? "";
            }
        }



        // IDxx -> <STX>IDxx=ADC<ETX> 로 자동 프레이밍
        private static string BuildDpmFrame(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? "";

            string s = raw.Trim();

            // 이미 제어코드(#02/#03) 표기면 실제 STX/ETX로 변환
            s = s.Replace("#02", ((char)0x02).ToString())
                 .Replace("#03", ((char)0x03).ToString());

            // 이미 STX/ETX 포함이면 그대로 사용
            if (s.Contains(((char)0x02)) || s.Contains(((char)0x03)))
                return s;

            // "ID.." 로 시작하면 "=ADC" 보장 + STX/ETX 프레임
            if (s.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
            {
                if (!s.Contains("=")) s += "=ADC";
                return $"{(char)0x02}{s}{(char)0x03}";
            }

            // 그 외는 그대로
            return s;
        }

        private bool IsUartConnected()
        {
            try
            {
                var st = _uartHandle?.GetStatus();
                if (st is IDictionary<string, object> d && d.TryGetValue("is_connected", out var v) && v is bool b) return b;
                if (st is IDictionary<string, object?> d2 && d2.TryGetValue("is_connected", out var v2) && v2 is bool b2) return b2;
            }
            catch { }
            return false;
        }

        // PIM_DPMMSG: tests[].command = "ID22" 만 써도 자동으로 #02ID22=ADC#03 송신
        private void EnsureUartConnected(Action<string> logAction)
        {
            if (IsUartConnected()) return;

            // IProcess 구현이면 Begin()으로 포트 오픈
            if (_uartHandle is IProcess p)
            {
                logAction("uart UART 연결을 시도합니다...");
                try { p.Begin(); } catch { /* ignore */ }
            }

            if (!IsUartConnected())
            {
                // 마지막으로 Connect() 직접 호출 시도
                var mi = _uartHandle?.GetType().GetMethod("Connect", Type.EmptyTypes);
                try { mi?.Invoke(_uartHandle, null); } catch { /* ignore */ }
            }

            logAction($"uart UART 연결 상태: {IsUartConnected()}");
        }

        // --- helper: UART 버퍼 안전 플러시 ---
        private void ClearUartBufferSafe(Action<string> logAction, int attempts = 5, int timeout = 100)
        {
            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    var resp = Read(new Dictionary<string, object?> {
                        { "timeout", timeout },
                        { "porttype", "uart" }
                    });
                    if (resp?.ContainsKey("value") == true)
                    {
                        var v = resp["value"]?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(v)) break;
                        //logAction($"플러시: [{v}]");
                    }
                    else break;
                }
                catch (Exception ex)
                {
                    logAction($"(무시) 버퍼 플러시 중 예외: {ex.Message}");
                    break;
                }
            }
        }

        

        private static string BuildDpmCommand(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? "";

            string s = raw.Trim();
            // "#02", "#03" 표기 → 실제 STX/ETX로 치환
            s = s.Replace("#02", ((char)0x02).ToString())
                 .Replace("#03", ((char)0x03).ToString());

            // 이미 STX/ETX가 들어 있으면 그대로 사용
            if (s.Contains(((char)0x02)) || s.Contains(((char)0x03)))
                return s;

            // "ID.." 형태면 "=ADC" 보장 후 STX/ETX 프레이밍
            if (s.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
            {
                if (!s.Contains("=")) s += "=ADC";
                return $"{(char)0x02}{s}{(char)0x03}";
            }

            return s;
        }

        // --- 본체: PIM_DPMMSG ---
        public IDictionary<string, object?> PIM_DPMMSG(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
        {
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null || !tests.Any())
            {
                logAction("tests 배열이 비어있거나 존재하지 않습니다.");
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");
            }

            // UART 핸들 확보
            string portId = "uart_dpm";
            if (_uartHandle == null && libraries.ContainsKey(portId))
            {
                _uartHandle = libraries[portId] as IComm;
                logAction($"PIM_DPMMSG: _uartHandle이 없어 libraries['{portId}']로 자동 세팅.");
            }
            if (_uartHandle == null)
            {
                return new Dictionary<string, object?> {
            { "success", false }, { "error", "UART handle null" }
        };
            }

            // 연결 보장
            EnsureUartConnected(logAction);

            // 시작 시 버퍼 정리
            if (IsUartConnected())
                ClearUartBufferSafe(logAction);

            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;
            int delay = param.ContainsKey("delay") ? Convert.ToInt32(param["delay"]) : 100;

            string lastCommand = "", lastResponse = "", expectedValue = "";
            bool finalResult = true;

            foreach (var testObj in tests)
            {
                var test = testObj as IDictionary<string, object>;
                if (test == null || !test.ContainsKey("command"))
                {
                    logAction("테스트 항목에 'command'가 없습니다.");
                    continue;
                }

                // "ID22" → <STX>ID22=ADC<ETX>
                string userCmd = test["command"]?.ToString() ?? "";
                string cmd = userCmd.Trim();
                if (!cmd.Contains("=")) cmd += "=ADC";
                string framed = $"{(char)0x02}{cmd}{(char)0x03}";

                lastCommand = userCmd;
                expectedValue = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";

                var pretty = framed.Replace("\u0002", "\\x02").Replace("\u0003", "\\x03");
                logAction($"DPMMSG TX: {pretty}");

                // ✅ 개행 없이 전송 (장비 요구사항 반영)
                Write(new Dictionary<string, object?> {
            { "body", framed },
            { "porttype", "uart" },
            { "no_newline", true }     // 중요!
        });

                System.Threading.Thread.Sleep(delay);

                // ✅ ETX까지 수신 + STX 이후만 페이로드로
                var resp = Read(new Dictionary<string, object?> {
            { "timeout",   timeout },
            { "porttype",  "uart" },
            { "recv_mode", "etx" },   // 중요!
            { "stx_guard", true  },   // 중요!
            { "etx",       0x03  }    // 중요!
        });

                lastResponse = (resp != null && resp.ContainsKey("value") && resp["value"] != null)
                               ? resp["value"]!.ToString()!
                               : "";

                if (string.IsNullOrWhiteSpace(lastResponse))
                {
                    logAction("응답 없음 (Timeout 또는 연결 문제)");
                    finalResult = false;
                    continue;
                }

                logAction($"수신된 메시지: {lastResponse}");

                bool match = string.IsNullOrEmpty(expectedValue) ? true : lastResponse.Contains(expectedValue);
                finalResult &= match;
                logAction($"응답 비교 - 기대값: {expectedValue}, 실제값: {lastResponse}, 성공 여부: {(match ? "일치" : "불일치")}");
            }

            // 종료 전 버퍼 정리 (선택)
            if (IsUartConnected())
                ClearUartBufferSafe(logAction);

            return new Dictionary<string, object?>
    {
        { "command",  lastCommand },
        { "response", lastResponse },
        { "expected", expectedValue },
        { "success",  finalResult }
    };
        }




        // 아래부터 검사 함수와 코드
        // 00 테스트 셋팅 초기화


        public Dictionary<string, object> PIM_CTSP_00(IDictionary<string, object> libraries, IDictionary<string, object> param, Action<string> logAction)
        {
            logAction("PIM_CTSP_00 시작");

            // PreRun을 호출하여 ctsp_ 초기화
            if (ctsp_ == null)
            {
                logAction("CTSP 인스턴스가 초기화되지 않았습니다. PreRun을 확인해주세요.");
                PreRun(libraries);  // PreRun을 명시적으로 호출하여 초기화
            }

            var result = new Dictionary<string, object>
    {
        { "success", false },
        { "display", "NG" },
        { "result", null }
    };

            // ctsp_가 초기화되었는지 다시 확인
            if (ctsp_ == null)
            {
                logAction("CTSP 인스턴스가 여전히 초기화되지 않았습니다. 초기화를 확인하고 다시 시도하세요.");
                return result; // 초기화가 안 되면 종료
            }

            // 'cmd'와 'args'가 param에 포함되어 있는지 확인하고, 없으면 오류 처리
            if (!param.ContainsKey("cmd") || !(param["cmd"] is string cmd))
            {
                logAction("❌ 'cmd'가 포함되지 않았습니다.");
                return result; // 'cmd'가 없으면 종료
            }

            if (!param.ContainsKey("args") || !(param["args"] is IEnumerable<object> args))
            {
                logAction("❌ 'args'가 포함되지 않았습니다.");
                return result; // 'args'가 없으면 종료
            }

            string command = cmd;  // 실제 명령어는 param에서 받아온 'cmd'
            string expected = "[TEST_SETTING,0,1]";  // 예상 응답

            // 🧹 송신 전 버퍼 비움
            ClearSocketBuffer("tcp", logAction); // 포트타입에 맞게 조정

            // 명령 전송 및 응답 수신
            var response = ctsp_.DoCommand(new Dictionary<string, object?>
    {
        { "cmd", command },  // 실제 명령어를 "TEST_SETTING"으로 지정
        { "args", args }     // 명령어에 필요한 인자 전달
    });

            // 응답 파싱 및 확인
            if (response != null && response.ContainsKey("cmd"))
            {
                string respStr = $"{response["cmd"]},{response["res"]}";

                if (response.ContainsKey("args") && response["args"] is IEnumerable<object> responseArgs)
                {
                    foreach (var arg in responseArgs)
                    {
                        respStr += $",{arg}";
                    }
                }

                logAction($"CTSP 수신 응답: {respStr}");
                bool isMatch = respStr.Contains(expected);

                if (isMatch)
                {
                    result["success"] = true;
                    result["display"] = "OK";
                    result["result"] = response;
                }
                else
                {
                    logAction($"❌ 응답 불일치 - 기대값: {expected}, 실제값: {respStr}");
                }
            }
            else
            {
                logAction("❌ 응답 없음 또는 형식 오류");
            }

            // 🧹 응답 후 버퍼 정리
            ClearSocketBuffer("tcp", logAction);

            return result;
        }

        public IDictionary<string, object?> PIM_Powertest00(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
        {
            // ---- 포트 아이디 ----
            string prPortId = param.ContainsKey("prport") ? param["prport"]?.ToString() ?? "tcp_1" : "tcp_1";
            string dpmPortId = param.ContainsKey("dpmport") ? param["dpmport"]?.ToString() ?? "uart_dpm" : "uart_dpm";

            // ---- 핸들 보강 ----
            if (_tcpHandle == null && libraries.ContainsKey(prPortId))
            {
                _tcpHandle = libraries[prPortId] as IComm;
                logAction($"pim: TCP 핸들을 libraries['{prPortId}']에서 세팅.");
            }
            if (_uartHandle == null && libraries.ContainsKey(dpmPortId))
            {
                _uartHandle = libraries[dpmPortId] as IComm;
                logAction($"pim: UART 핸들을 libraries['{dpmPortId}']에서 세팅.");
            }
            if (_tcpHandle == null) return Fail("TCP 핸들이 없습니다.", "tcp handle null");
            if (_uartHandle == null) return Fail("UART 핸들이 없습니다.", "uart handle null");

            // ---- 연결 보장 ----
            EnsureConnected(_tcpHandle, "tcp");
            EnsureConnected(_uartHandle, "uart");

            // ---- 테스트 목록 ----
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null) throw new ArgumentException("tests 배열이 없습니다.");

            // 메타 (retmsg용) : __proc_* 우선, 없으면 proc_* 폴백, 모두 없으면 기본값
            string procId =
                (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
                (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
                "PIM_Powertest00";

            string procName =
                (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
                (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
                "PowerTest";

            // 요구사항: 각 단계마다 1초 대기
            const int stepDelayMs = 1000;
            int readTimeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            string lastCmdTcp = "";
            string lastCmdUart = "";
            string lastResponse = "";
            bool finalOk = true;
            double? lastValue = null, minRange = null, maxRange = null;

            var retLines = new List<string>(); // ★ retmsg 누적

            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;

                // (A) PR 사전 명령 전송
                string prCommand = test.ContainsKey("command") ? (test["command"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(prCommand))
                {
                    logAction("PR command가 비어있습니다. 테스트를 건너뜁니다.");
                    finalOk = false;

                    // retmsg도 NG로 한 줄 남김
                    //retLines.Add($"{{{procId}}},{{{procName}}},{{NG}},{{-}},{{}}");
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
                lastCmdTcp = prCommand;
                logAction($"PR TX: {Esc(prCommand)}");
                _tcpHandle.Send(new Dictionary<string, object?> { { "body", prCommand } });
                System.Threading.Thread.Sleep(stepDelayMs);

                // (B) DPM(UART)로 측정 명령
                string idToken = test.ContainsKey("id") ? (test["id"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(idToken))
                {
                    logAction("DPM id가 비어있습니다. 테스트를 건너뜁니다.");
                    finalOk = false;

                    //retLines.Add($"{{{procId}}},{{{procName}}},{{NG}},{{-}},{{}}");
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
                string dpmFrame = BuildDpmFrameFromId(idToken); // <STX>ID22=ADC<ETX>
                lastCmdUart = dpmFrame;

                // UART 송신 전 짧게 플러시
                FlushUart(3, 100);

                logAction($"DPM TX: {Esc(dpmFrame)}");
                _uartHandle.Send(new Dictionary<string, object?> {
            { "body", dpmFrame },
            { "no_newline", true } // 개행 없이
        });

                System.Threading.Thread.Sleep(stepDelayMs);

                // UART 수신(ETX 프레이밍)
                string payload = RecvUartEtX(readTimeout);
                logAction($"수신된 메시지: {payload}");
                lastResponse = payload;

                // adc, 뒤 숫자 추출
                // adc, 뒤 숫자 추출
                var m = Regex.Match(payload ?? "", @"adc\s*,\s*([-+]?\d*\.?\d+)", RegexOptions.IgnoreCase);

                // v를 미리 선언(초기화)해 두면 CS0165 예방
                double v = 0.0;
                bool hasValue = false;

                if (m.Success)
                {
                    hasValue = double.TryParse(
                        m.Groups[1].Value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out v
                    );
                }

                if (hasValue)
                {
                    lastValue = v;
                    logAction($"추출된 값: {v.ToString(CultureInfo.InvariantCulture)}");
                }
                else
                {
                    logAction("adc, 뒤의 숫자 추출 실패");
                    lastValue = null;
                }


                // min/max 파싱
                (minRange, maxRange) = ParseMinMax(test);

                // 판정
                bool okThis = false;
                if (minRange.HasValue && maxRange.HasValue && lastValue.HasValue)
                {
                    okThis = (lastValue.Value >= minRange.Value) && (lastValue.Value <= maxRange.Value);
                    logAction($"범위 판정: min={minRange.Value}, max={maxRange.Value}, value={lastValue.Value} => {(okThis ? "OK" : "FAIL")}");
                }
                else
                {
                    logAction("min/max가 없거나 수신값이 없어 판정을 건너뜁니다.");
                }
                finalOk &= okThis;

                // 수신 후 플러시(잔여가 있다면 비움)
                FlushUart(2, 100);

                // (C) PR 리셋(항상 전송)
                try
                {
                    System.Threading.Thread.Sleep(stepDelayMs);
                    const string resetCmd = "\u0002PIO160000000000000000\u0003";
                    logAction($"PR RESET TX: {Esc(resetCmd)}");
                    _tcpHandle.Send(new Dictionary<string, object?> { { "body", resetCmd } });
                }
                catch (Exception exReset)
                {
                    logAction($"PR 리셋 전송 예외: {exReset.Message}");
                }

                // ★ retmsg 라인 추가: {id},{name},{OK/NG},{A-B},{측정값}
                string rangeText =
                    (minRange.HasValue || maxRange.HasValue)
                    ? $"{minRange?.ToString(CultureInfo.InvariantCulture) ?? ""}-{maxRange?.ToString(CultureInfo.InvariantCulture) ?? ""}"
                    : "-";

                string measuredText = lastValue?.ToString(CultureInfo.InvariantCulture) ?? "";

                //retLines.Add($"{{{procId}}},{{{procName}}},{{{(okThis ? "OK" : "NG")}}},{{{rangeText}}},{{{measuredText}}}");
                retLines.Add(
                        new JObject
                        {
                            ["Name"] = procName,
                            ["id"] = procId,
                            ["Range"] = rangeText,
                            ["Measure"] = measuredText
                        }.ToString(Formatting.None)
                    );
            }

            return new Dictionary<string, object?>
    {
        { "success", finalOk },
        { "command_tcp", lastCmdTcp },
        { "command_uart", Esc(lastCmdUart) },
        { "response", lastResponse },
        { "value", lastValue?.ToString(CultureInfo.InvariantCulture) },
        { "min",   minRange?.ToString(CultureInfo.InvariantCulture) },
        { "max",   maxRange?.ToString(CultureInfo.InvariantCulture) },
        { "retmsg", string.Join(Environment.NewLine, retLines) } // ★ 여기!
    };

            // ===== Helpers =====
            IDictionary<string, object?> Fail(string log, string err)
            {
                logAction(log);
                return new Dictionary<string, object?> { { "success", false }, { "error", err } };
            }

            void EnsureConnected(IComm handle, string kind)
            {
                bool IsConn()
                {
                    try
                    {
                        var st = handle.GetStatus();
                        if (st == null) return false;
                        if (st.TryGetValue("Connected", out var c) && c is bool b1) return b1;
                        if (st.TryGetValue("is_connected", out var u) && u is bool b2) return b2;
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
                logAction($"{kind} 연결 상태: {IsConn()}");
            }

            string BuildDpmFrameFromId(string idToken)
            {
                // "id22" → "ID22=ADC" → <STX>...<ETX>
                string core = (idToken ?? "").Trim();
                if (string.IsNullOrEmpty(core)) return "\u0002ID00=ADC\u0003";
                core = core.ToUpperInvariant();
                if (!core.Contains("=")) core += "=ADC";
                return "\u0002" + core + "\u0003";
            }

            // UART 버퍼 플러시(ETX 모드로 짧게)
            void FlushUart(int attempts, int toMs)
            {
                for (int i = 0; i < attempts; i++)
                {
                    try
                    {
                        var r = _uartHandle!.Recv(new Dictionary<string, object?> {
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

            // UART 수신(ETX 프레이밍 + 재시도)
            string RecvUartEtX(int timeout)
            {
                var r1 = _uartHandle!.Recv(new Dictionary<string, object?> {
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
                    var r = _uartHandle!.Recv(new Dictionary<string, object?> {
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

            (double?, double?) ParseMinMax(IDictionary<string, object> testItem)
            {
                double? rmin = null, rmax = null;
                if (testItem.ContainsKey("min") && double.TryParse(testItem["min"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
                    rmin = a;
                if (testItem.ContainsKey("max") && double.TryParse(testItem["max"]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var b))
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
        }

        public Dictionary<string, object> PIM_CMD_TEST(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
        {
            // 0) 사용할 TCP 핸들 결정: param["port"] > tcp_2 > tcp_1
            string portId =
                (param.ContainsKey("port") && param["port"] is string p && !string.IsNullOrWhiteSpace(p)) ? p :
                (libraries.ContainsKey("tcp_2") ? "tcp_2" :
                (libraries.ContainsKey("tcp_1") ? "tcp_1" : null));

            if (string.IsNullOrEmpty(portId) || !libraries.ContainsKey(portId))
                return new Dictionary<string, object> { { "success", false }, { "error", "사용할 TCP 핸들을 찾을 수 없습니다." } };

            if (!(libraries[portId] is IComm tcp))
                return new Dictionary<string, object> { { "success", false }, { "error", $"TCP 핸들 캐스팅 실패 (portId='{portId}')" } };

            // 1) 리턴메시지 메타 (__proc_* > proc_* > param.name > 기본)
            string procId =
                (param.TryGetValue("__proc_id", out var pid0) && pid0 != null && !string.IsNullOrWhiteSpace(pid0.ToString())) ? pid0.ToString()! :
                (param.TryGetValue("proc_id", out var pid1) && pid1 != null && !string.IsNullOrWhiteSpace(pid1.ToString())) ? pid1.ToString()! :
                "TCPMSG";

            string procName =
                (param.TryGetValue("__proc_name", out var pn0) && pn0 != null && !string.IsNullOrWhiteSpace(pn0.ToString())) ? pn0.ToString()! :
                (param.TryGetValue("proc_name", out var pn1) && pn1 != null && !string.IsNullOrWhiteSpace(pn1.ToString())) ? pn1.ToString()! :
                (param.TryGetValue("name", out var pn2) && pn2 != null && !string.IsNullOrWhiteSpace(pn2.ToString())) ? pn2.ToString()! :
                "";

            // 2) 연결 보장
            bool IsConnected(IComm c)
            {
                try
                {
                    var st = c.GetStatus();
                    return st != null && st.TryGetValue("Connected", out var v) && v is bool b && b;
                }
                catch { return false; }
            }
            if (!IsConnected(tcp))
            {
                try { tcp.GetType().GetMethod("Begin")?.Invoke(tcp, null); } catch { }
                if (!IsConnected(tcp))
                {
                    try { tcp.GetType().GetMethod("Connect")?.Invoke(tcp, null); } catch { }
                }
            }
            if (!IsConnected(tcp))
                return new Dictionary<string, object> { { "success", false }, { "error", $"TCP 연결 실패 (portId='{portId}')" } };

            // 3) 파라미터/타임아웃
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null || !tests.Any())
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");

            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            // 시작시 버퍼 비우기
            void Flush(IComm c)
            {
                for (int i = 0; i < 5; i++)
                {
                    var r = c.Recv(new Dictionary<string, object?> { { "timeout", 100 }, { "porttype", "tcp" } });
                    if (r is IDictionary<string, object?> d && d.TryGetValue("value", out var v) && v is string s && !string.IsNullOrWhiteSpace(s))
                    {
                        // drop
                    }
                    else break;
                }
            }
            Flush(tcp);

            // 4) 테스트 루프
            string lastCommand = "";
            string lastResponse = "";
            string expectedResponse = "";
            bool finalResult = true;

            var sbRet = new System.Text.StringBuilder(); // JSON 한 줄씩

            foreach (var testObj in tests)
            {
                var test = testObj as IDictionary<string, object>;
                if (test == null || !test.ContainsKey("command"))
                {
                    logAction("테스트 항목에 'command'가 없습니다.");
                    continue;
                }

                string command = test["command"]?.ToString() ?? "";
                expectedResponse = test.ContainsKey("expected_response") ? test["expected_response"]?.ToString() ?? "" : "";
                lastCommand = command;

                logAction($"명령어 전송: {command}");
                tcp.Send(new Dictionary<string, object?> {
            { "body", command },
            { "porttype", "tcp" }
        });

                var response = tcp.Recv(new Dictionary<string, object?> {
            { "timeout", timeout },
            { "porttype", "tcp" }
        });

                lastResponse = (response != null && response.ContainsKey("value"))
                    ? response["value"]?.ToString() ?? ""
                    : "";

                bool testOk;
                if (string.IsNullOrEmpty(expectedResponse))
                {
                    // 기대값이 없으면 응답이 비어있지 않으면 OK
                    testOk = !string.IsNullOrWhiteSpace(lastResponse);
                }
                else
                {
                    testOk = !string.IsNullOrEmpty(lastResponse) && lastResponse.Contains(expectedResponse);
                }

                if (string.IsNullOrWhiteSpace(lastResponse))
                {
                    logAction("응답 없음 (Timeout 또는 연결 문제)");
                    finalResult = false;
                }
                else
                {
                    logAction($"수신된 메시지: {lastResponse}");
                    finalResult &= testOk;
                    logAction($"응답 비교 - 기대값: {expectedResponse}, 실제값: {lastResponse}, 성공 여부: {(testOk ? "일치" : "불일치")}");
                }

                // ★ 여기서 Result 필드 제거, Expected/Response 짧게
                var obj = new JObject
                {
                    ["Name"] = procName,
                    ["id"] = procId,
                    ["Exp"] = expectedResponse,
                    ["Rsp"] = lastResponse
                };
                sbRet.AppendLine(obj.ToString(Newtonsoft.Json.Formatting.None));
            }

            // 5) 종료 시 버퍼 정리
            Flush(tcp);

            return new Dictionary<string, object> {
        { "command",  lastCommand },
        { "response", lastResponse },
        { "expected", expectedResponse },
        { "success",  finalResult },
        { "retmsg",   sbRet.ToString() }
    };
        }


        public Dictionary<string, object> ShipConfigClearTest(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
{
    string portId =
        (param.ContainsKey("port") && !string.IsNullOrWhiteSpace(param["port"]?.ToString()))
            ? param["port"]!.ToString()!
            : (libraries.ContainsKey("uart_e84a") ? "uart_e84a" : "uart_dpm");

    if (_uartHandle == null && libraries.ContainsKey(portId))
    {
        _uartHandle = libraries[portId] as IComm;
        logAction($"ShipConfigClearTest: _uartHandle이 없어 libraries['{portId}']로 자동 세팅.");
    }
    if (_uartHandle == null)
    {
        return new Dictionary<string, object> { { "success", false }, { "error", "UART handle null" } };
    }

    EnsureUartConnected(logAction);

    int bootWaitMs = param.ContainsKey("BOOT_WAIT") ? Convert.ToInt32(param["BOOT_WAIT"]) : 2000;
    int readTimeoutMs = param.ContainsKey("TIMEOUT") ? Convert.ToInt32(param["TIMEOUT"]) : 5000;

    string lastResponse = "";
    string collected = "";
    bool ok = true;
    var steps = new JArray();

    // retmsg 메타
    string procId =
        (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
        (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
        "ShipConfigClearTest";
    string procName =
        (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
        (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
        "Ship Config Clear Test";

    try
    {
        ClearCommBuffer("uart", logAction);

        var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
        if (tests == null || !tests.Any())
        {
            logAction("[ConfigClear] tests 배열이 비어있거나 존재하지 않습니다.");
            ok = false;
        }
        else
        {
            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;

                if (test.ContainsKey("sleep"))
                {
                    int sleepMs = Convert.ToInt32(test["sleep"]);
                    logAction($"Waiting {sleepMs} ms...");
                    System.Threading.Thread.Sleep(sleepMs);
                    continue;
                }

                string command = test.ContainsKey("command") ? (test["command"]?.ToString() ?? "") : "";
                string expected = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(command)) continue;

                logAction($"명령어 전송: {command}");
                Write(new Dictionary<string, object?> {
                    { "body", command + "\r\n" },
                    { "porttype", "uart" },
                    { "no_newline", true }
                });

                var end = DateTime.UtcNow.AddMilliseconds(readTimeoutMs);
                int emptyCount = 0;
                bool gotBody = false;
                string stepCollected = "";
                string stepLast = "";
                while (DateTime.UtcNow < end)
                {
                    var resp = Read(new Dictionary<string, object?> {
                        { "timeout", 500 },
                        { "porttype", "uart" },
                        { "recv_mode", "line" }
                    });
                    var v = resp != null && resp.ContainsKey("value") ? resp["value"]?.ToString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        lastResponse = v;
                        collected += v + "\n";
                        stepCollected += v + "\n";
                        stepLast = v;
                        logAction(v);
                        emptyCount = 0;
                        if (!gotBody && !v.Contains(">"))
                            gotBody = true;
                        if (v.Contains(">") && gotBody)
                            break;
                    }
                    else
                    {
                        emptyCount++;
                        if (emptyCount >= 3) break;
                    }
                }

                bool stepOk = true;
                if (!string.IsNullOrWhiteSpace(expected) && !stepCollected.Contains(expected))
                {
                    logAction($"[ConfigClear] 누락 항목: {expected}");
                    ok = false;
                    stepOk = false;
                }

                // steps 기록
                var stepObj = new JObject
                {
                    ["command"] = command,
                    ["expected"] = string.IsNullOrWhiteSpace(expected) ? null : expected,
                    ["value"] = string.IsNullOrWhiteSpace(stepLast) ? null : stepLast,
                    ["result"] = stepOk ? "OK" : "NG"
                };
                steps.Add(stepObj);
            }
        }

        if (tests != null && tests.Any())
        {
            logAction($"Waiting {bootWaitMs} ms for reboot...");
            System.Threading.Thread.Sleep(bootWaitMs);
        }

    }
    catch (Exception ex)
    {
        logAction($"ShipConfigClearTest 예외: {ex.Message}");
        ok = false;
    }

    var retObj = new JObject
    {
        ["Name"] = procName,
        ["id"] = procId,
        ["steps"] = steps,
        ["result"] = ok ? "OK" : "NG"
    };

    return new Dictionary<string, object>
    {
        { "success", ok },
        { "response", string.IsNullOrWhiteSpace(collected) ? lastResponse : collected },
        { "retmsg", retObj.ToString(Newtonsoft.Json.Formatting.None) }
    };
}

        public Dictionary<string, object> ShipConfigCheckTest(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
{
    string portId =
        (param.ContainsKey("port") && !string.IsNullOrWhiteSpace(param["port"]?.ToString()))
            ? param["port"]!.ToString()!
            : (libraries.ContainsKey("uart_e84a") ? "uart_e84a" : "uart_dpm");

    if (_uartHandle == null && libraries.ContainsKey(portId))
    {
        _uartHandle = libraries[portId] as IComm;
        logAction($"ShipConfigCheckTest: _uartHandle이 없어 libraries['{portId}']로 자동 세팅.");
    }
    if (_uartHandle == null)
    {
        return new Dictionary<string, object> { { "success", false }, { "error", "UART handle null" } };
    }

    EnsureUartConnected(logAction);

    int readTimeoutMs = param.ContainsKey("TIMEOUT") ? Convert.ToInt32(param["TIMEOUT"]) : 6000;

    string lastResponse = "";
    string collected = "";
    bool ok = true;
    var checks = new JArray();

    // retmsg 메타
    string procId =
        (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
        (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
        "ShipConfigCheckTest";
    string procName =
        (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
        (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
        "Ship Config Check Test";

    try
    {
        var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
        if (tests == null || !tests.Any())
        {
            logAction("[ConfigCheck] tests 배열이 비어있거나 존재하지 않습니다.");
            ok = false;
        }
        else
        {
            // 1) command 처리 (workspace 기반)
            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;

                string command = test.ContainsKey("command") ? (test["command"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(command)) continue;

                logAction($"명령어 전송: {command}");
                Write(new Dictionary<string, object?> {
                    { "body", command + "\r\n" },
                    { "porttype", "uart" },
                    { "no_newline", true }
                });

                // 응답 수신 (본문 1라인 이상 후 '>' 종료 인정)
                var end = DateTime.UtcNow.AddMilliseconds(readTimeoutMs);
                int emptyCount = 0;
                bool gotBody = false;
                while (DateTime.UtcNow < end)
                {
                    var resp = Read(new Dictionary<string, object?> {
                        { "timeout", 500 },
                        { "porttype", "uart" },
                        { "recv_mode", "line" }
                    });
                    var v = resp != null && resp.ContainsKey("value") ? resp["value"]?.ToString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        lastResponse = v;
                        collected += v + "\n";
                        logAction(v);
                        emptyCount = 0;
                        if (!gotBody && !v.Contains(">"))
                            gotBody = true;
                        if (v.Contains(">") && gotBody)
                            break;
                    }
                    else
                    {
                        emptyCount++;
                        if (emptyCount >= 3) break;
                    }
                }
            }

            // 2) expected_response 검증 (command 유무와 무관)
            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;
                string expected = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(expected)) continue;

                string foundLine = "";
                if (!string.IsNullOrWhiteSpace(collected))
                {
                    var lines = collected.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains(expected))
                        {
                            foundLine = line.Trim();
                            break;
                        }
                    }
                }

                bool hit = !string.IsNullOrWhiteSpace(foundLine);
                if (!hit)
                {
                    logAction($"[ConfigCheck] 누락 항목: {expected}");
                    ok = false;
                }

                var checkObj = new JObject
                {
                    ["expected"] = expected,
                    ["value"] = hit ? foundLine : null,
                    ["result"] = hit ? "OK" : "NG"
                };
                checks.Add(checkObj);
            }
        }
    }
    catch (Exception ex)
    {
        logAction($"ShipConfigCheckTest 예외: {ex.Message}");
        ok = false;
    }

    return new Dictionary<string, object>
    {
        { "success", ok },
        { "response", collected },
        { "retmsg", new JObject
            {
                ["Name"] = procName,
                ["id"] = procId,
                ["checks"] = checks,
                ["result"] = ok ? "OK" : "NG"
            }.ToString(Newtonsoft.Json.Formatting.None)
        }
    };
}

        public Dictionary<string, object> ShipLogClearTest(
    IDictionary<string, object> libraries,
    IDictionary<string, object> param,
    Action<string> logAction)
{
    string portId =
        (param.ContainsKey("port") && !string.IsNullOrWhiteSpace(param["port"]?.ToString()))
            ? param["port"]!.ToString()!
            : (libraries.ContainsKey("uart_e84a") ? "uart_e84a" : "uart_dpm");

    if (_uartHandle == null && libraries.ContainsKey(portId))
    {
        _uartHandle = libraries[portId] as IComm;
        logAction($"ShipLogClearTest: _uartHandle이 없어 libraries['{portId}']로 자동 세팅.");
    }
    if (_uartHandle == null)
    {
        return new Dictionary<string, object> { { "success", false }, { "error", "UART handle null" } };
    }

    EnsureUartConnected(logAction);

    int readTimeoutMs = param.ContainsKey("TIMEOUT") ? Convert.ToInt32(param["TIMEOUT"]) : 6000;

    string lastResponse = "";
    string collected = "";
    bool ok = true;
    var steps = new JArray();

    // retmsg 메타
    string procId =
        (param.TryGetValue("__proc_id", out var _pid) && _pid != null && _pid.ToString()!.Length > 0) ? _pid.ToString()! :
        (param.TryGetValue("proc_id", out var _pid2) && _pid2 != null && _pid2.ToString()!.Length > 0) ? _pid2.ToString()! :
        "ShipLogClearTest";
    string procName =
        (param.TryGetValue("__proc_name", out var _pname) && _pname != null && _pname.ToString()!.Length > 0) ? _pname.ToString()! :
        (param.TryGetValue("proc_name", out var _pname2) && _pname2 != null && _pname2.ToString()!.Length > 0) ? _pname2.ToString()! :
        "Ship Log Clear Test";

    try
    {
        var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
        if (tests == null || !tests.Any())
        {
            logAction("[LogClear] tests 배열이 비어있거나 존재하지 않습니다.");
            ok = false;
        }
        else
        {
            // 1) command 처리 (workspace 기반)
            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;

                string command = test.ContainsKey("command") ? (test["command"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(command)) continue;

                logAction($"명령어 전송: {command}");
                Write(new Dictionary<string, object?> {
                    { "body", command + "\r\n" },
                    { "porttype", "uart" },
                    { "no_newline", true }
                });

                // 응답 수신 (프롬프트만으로 종료하지 않음)
                var end = DateTime.UtcNow.AddMilliseconds(readTimeoutMs);
                int emptyCount = 0;
                bool gotBody = false;
                string stepCollected = "";
                string stepLast = "";
                while (DateTime.UtcNow < end)
                {
                    var resp = Read(new Dictionary<string, object?> {
                        { "timeout", 500 },
                        { "porttype", "uart" },
                        { "recv_mode", "line" }
                    });
                    var v = resp != null && resp.ContainsKey("value") ? resp["value"]?.ToString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        lastResponse = v;
                        collected += v + "\n";
                        stepCollected += v + "\n";
                        stepLast = v;
                        logAction(v);
                        emptyCount = 0;
                        if (!gotBody && !v.Contains(">"))
                            gotBody = true;
                        if (v.Contains(">") && gotBody)
                            break;
                    }
                    else
                    {
                        emptyCount++;
                        if (emptyCount >= 3) break;
                    }
                }

                string expected = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";
                bool stepOk = true;
                if (!string.IsNullOrWhiteSpace(expected) && !stepCollected.Contains(expected))
                {
                    stepOk = false;
                }

                var stepObj = new JObject
                {
                    ["command"] = command,
                    ["expected"] = string.IsNullOrWhiteSpace(expected) ? null : expected,
                    ["value"] = string.IsNullOrWhiteSpace(stepLast) ? null : stepLast,
                    ["result"] = stepOk ? "OK" : "NG"
                };
                steps.Add(stepObj);
            }

            // 2) expected_response 검증 (command 유무와 무관)
            foreach (var t in tests)
            {
                var test = t as IDictionary<string, object>;
                if (test == null) continue;
                string expected = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";
                if (string.IsNullOrWhiteSpace(expected)) continue;

                if (!collected.Contains(expected))
                {
                    logAction($"[LogClear] FAIL : {expected} 미포함");
                    ok = false;
                }
            }

            if (ok)
                logAction("[LogClear] PASS");
            else
                logAction(collected);
        }
    }
    catch (Exception ex)
    {
        logAction($"ShipLogClearTest 예외: {ex.Message}");
        ok = false;
    }

    return new Dictionary<string, object>
    {
        { "success", ok },
        { "response", collected },
        { "retmsg", new JObject
            {
                ["Name"] = procName,
                ["id"] = procId,
                ["steps"] = steps,
                ["result"] = ok ? "OK" : "NG"
            }.ToString(Newtonsoft.Json.Formatting.None)
        }
    };
}

        //함수 종료
    }
}
