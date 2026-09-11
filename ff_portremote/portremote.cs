using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Cantops.FlexFab
{
    public class Portremote : ModuleBase, IDevice, IProcess
    {
        private IComm _tcpHandle;  // TCP 핸들만 사용

        private const char STX = '\u0002'; // Start of Text
        private const char ETX = '\u0003'; // End of Text

        public const string MODULE_VERSION = "1.0";

        public Portremote()
        {
            info_ = new Dictionary<string, object>
            {
                { "name", "portremote" },
                { "desc", "TCP-based Portremote communication" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object>> {
                    new Dictionary<string, object> {
                        { "id", "proc_01" },
                        { "desc", "입력 포트 검사" },
                    },
                    new Dictionary<string, object> {
                        { "id", "proc_02" },
                        { "desc", "릴레이 출력" },
                    },
                }}
            };
        }

        public void Initialize(IDictionary<string, object> libraries)
        {
            // 1. config_가 null인지 확인
            if (config_ == null)
            {
                throw new InvalidOperationException("config_가 null입니다.");
            }

            // 2. "tcp"가 존재하는지 확인
            if (!config_.ContainsKey("tcp"))
            {
                Log($"config_에 존재하는 키 목록: {string.Join(", ", config_.Keys)}"); // 존재하는 키 출력
                throw new InvalidOperationException("TCP 설정이 유효하지 않습니다.");
            }

            // 3. TCP 설정 가져오기
            string tcpId = config_["tcp"].ToString();

            // 4. 라이브러리에 해당 핸들이 있는지 확인
            if (!libraries.ContainsKey(tcpId))
            {
                throw new InvalidOperationException($"TCP({tcpId}) 핸들을 찾을 수 없습니다.");
            }

            // 5. 핸들 연결
            _tcpHandle = libraries[tcpId] as IComm;

            // 6. 핸들 검증
            if (_tcpHandle == null)
            {
                throw new InvalidOperationException("TCP 핸들 초기화 실패");
            }
        }

        public override IDictionary<string, object?> GetStatus()
        {
            var status = new Dictionary<string, object?>();

            // TCP 상태를 딕셔너리에 추가
            status["tcp"] = _tcpHandle?.GetStatus();

            return status;
        }

        public void Begin()
        {
            _tcpHandle?.Connect();
            //Log("Portremote 시작");
        }

        public void End()
        {
            _tcpHandle?.Disconnect();
            //Log("Portremote 종료");
        }

        public IDictionary<string, object?> Read(IDictionary<string, object?> param)
        {
            //Log("Portremote Read 호출");

            if (_tcpHandle == null)
            {
                throw new InvalidOperationException("TCP 핸들이 초기화되지 않았습니다.");
            }

            // `timeout`을 파라미터에서 가져옴, 기본값은 1000ms
            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 1000;

            // TCP로부터 데이터 수신
            var response = _tcpHandle.Recv(param);  // 여기에서 TCP로부터 수신된 데이터

            // 수신된 데이터를 올바르게 처리
            if (response == null || !response.ContainsKey("value"))
            {
                Log("수신된 데이터가 없거나 잘못된 형식입니다.");
                return new Dictionary<string, object?> { { "value", string.Empty } };
            }

            // 수신된 데이터를 문자열로 추출
            string receivedMessage = response["value"]?.ToString() ?? string.Empty;
            Log($"수신 데이터: {receivedMessage}");

            // 수신된 메시지에서 STX와 ETX 제거
            if (receivedMessage.StartsWith(STX.ToString()) && receivedMessage.EndsWith(ETX.ToString()))
            {
                receivedMessage = receivedMessage.Substring(1, receivedMessage.Length - 2); // 앞뒤 문자 제거
            }

            // IDictionary 형식으로 반환
            return new Dictionary<string, object?> { { "value", receivedMessage } };
        }

        // IDevice.Write 구현
        public void Write(IDictionary<string, object?> param)
        {
            Log("Portremote Write 호출");

            if (_tcpHandle == null)
            {
                Log("오류: _tcpHandle이 null입니다. TCP 핸들이 초기화되지 않았습니다.");
                throw new InvalidOperationException("TCP 핸들이 초기화되지 않았습니다.");
            }

            // `body` 파라미터가 반드시 있어야 함
            if (!param.ContainsKey("body"))
            {
                throw new ArgumentException("Write 매개변수에 'body'가 필요합니다.");
            }

            string dataToSend = param["body"]?.ToString() ?? throw new ArgumentNullException("body");

            Log($"데이터 전송 중: {dataToSend}");

            // TCP로 데이터 전송
            _tcpHandle.Send(param);

            //Log("데이터 전송 완료");
        }

        public IDictionary<string, object?> PRMSG(
    IDictionary<string, object?> libraries,
    IDictionary<string, object?> param,
    Action<string> logAction)
        {
            // 0) _tcpHandle 확보 (Initialize를 안 탔을 가능성 대비)
            if (_tcpHandle == null)
            {
                // 우선순위: param.port > config_.tcp
                string? tcpId = null;
                if (param != null && param.ContainsKey("port") && param["port"] != null)
                    tcpId = param["port"]?.ToString();
                else if (config_ != null && config_.ContainsKey("tcp") && config_["tcp"] != null)
                    tcpId = config_["tcp"]?.ToString();

                if (!string.IsNullOrEmpty(tcpId) && libraries != null && libraries.ContainsKey(tcpId))
                {
                    _tcpHandle = libraries[tcpId] as IComm;
                    if (_tcpHandle != null)
                        logAction?.Invoke($"PRMSG: _tcpHandle이 없어 libraries['{tcpId}']로 자동 세팅.");
                }

                if (_tcpHandle == null)
                {
                    logAction?.Invoke("PRMSG: TCP 핸들을 초기화할 수 없습니다.");
                    return new Dictionary<string, object?> {
                { "success", false },
                { "error", "TCP 핸들 없음" }
            };
                }
            }

            // 1) 테스트 목록 확보
            var tests = param != null && param.ContainsKey("tests")
                ? param["tests"] as IEnumerable<object>
                : null;

            if (tests == null)
            {
                logAction?.Invoke("PRMSG: tests 배열이 비어있거나 존재하지 않습니다.");
                return new Dictionary<string, object?> {
            { "success", false },
            { "error", "tests 없음" }
        };
            }

            // 2) 옵션 파라미터
            int timeout = (param != null && param.ContainsKey("timeout"))
                ? Convert.ToInt32(param["timeout"])
                : 5000;

            int delay = (param != null && param.ContainsKey("delay"))
                ? Convert.ToInt32(param["delay"])
                : 0;

            bool stripStxEtx = (param != null && param.ContainsKey("strip_stx_etx") && param["strip_stx_etx"] is bool b && b)
                ? true
                : true; // 기본 true

            // 3) 연결 보장 (가능하면 연결)
            try
            {
                var st = _tcpHandle.GetStatus() as IDictionary<string, object?>;
                bool connected = st != null && st.ContainsKey("Connected") && st["Connected"] is bool cb && cb;
                if (!connected)
                {
                    try { _tcpHandle.Connect(); } catch { }
                    st = _tcpHandle.GetStatus() as IDictionary<string, object?>;
                    connected = st != null && st.ContainsKey("Connected") && st["Connected"] is bool cb2 && cb2;
                    if (!connected)
                    {
                        logAction?.Invoke("PRMSG: TCP 연결 실패 (소켓이 열려있지 않습니다).");
                        return new Dictionary<string, object?> {
                    { "success", false },
                    { "error", "TCP 미연결" }
                };
                    }
                }
            }
            catch
            {
                // 상태 조회 실패시에도 일단 진행
            }

            // 4) 버퍼 정리 함수 (TCPMSG와 동일한 개념)
            void ClearSocketBuffer(int maxAttempts = 5, int flushTimeout = 100)
            {
                for (int i = 0; i < maxAttempts; i++)
                {
                    var flush = _tcpHandle.Recv(new Dictionary<string, object?> { { "timeout", flushTimeout } });
                    if (flush != null && flush.ContainsKey("value"))
                    {
                        string leftover = flush["value"]?.ToString() ?? "";
                        if (!string.IsNullOrWhiteSpace(leftover))
                        {
                            //logAction?.Invoke($"PRMSG: 잔여 버퍼 비움 -> [{leftover}]");
                            continue;
                        }
                    }
                    break;
                }
            }

            // 5) STX/ETX 제거 유틸
            string Normalize(string? s)
            {
                if (string.IsNullOrEmpty(s)) return s ?? "";
                var text = s!;
                if (stripStxEtx && text.Length >= 2 && text[0] == STX && text[text.Length - 1] == ETX)
                {
                    text = text.Substring(1, text.Length - 2);
                }
                return text.Trim();
            }

            // 시작 시 버퍼 정리
            ClearSocketBuffer();

            string lastCommand = "";
            string lastResponse = "";
            string expectedResponse = "";
            bool finalResult = true;
            bool any = false;

            foreach (var testObj in tests)
            {
                any = true;

                var test = testObj as IDictionary<string, object?>;
                if (test == null || !test.ContainsKey("command"))
                {
                    logAction?.Invoke("PRMSG: 테스트 항목에 'command'가 없습니다.");
                    finalResult = false;
                    continue;
                }

                string command = test["command"]?.ToString() ?? "";
                expectedResponse = test.ContainsKey("expected_response") ? (test["expected_response"]?.ToString() ?? "") : "";
                lastCommand = command;

                logAction?.Invoke($"명령어 전송: {command}");
                try
                {
                    _tcpHandle.Send(new Dictionary<string, object?> { { "body", command } });
                }
                catch (Exception exSend)
                {
                    logAction?.Invoke($"PRMSG Send 예외: {exSend.Message}");
                    finalResult = false;
                    continue;
                }

                if (delay > 0)
                    System.Threading.Thread.Sleep(delay);

                var response = _tcpHandle.Recv(new Dictionary<string, object?> { { "timeout", timeout } });
                lastResponse = (response != null && response.ContainsKey("value"))
                    ? (response["value"]?.ToString() ?? "")
                    : "";

                if (string.IsNullOrWhiteSpace(lastResponse))
                {
                    logAction?.Invoke("응답 없음 (Timeout 또는 연결 문제)");
                    finalResult = false;
                    continue;
                }

                // STX/ETX 제거(옵션) 및 로그
                var normalized = Normalize(lastResponse);
                logAction?.Invoke($"수신된 메시지: {normalized}");

                bool match = string.IsNullOrEmpty(expectedResponse) || normalized.Contains(expectedResponse);
                finalResult &= match;
                logAction?.Invoke($"응답 비교 - 기대값: {expectedResponse}, 실제값: {normalized}, 성공 여부: {(match ? "일치" : "불일치")}");
            }

            // 끝나고도 버퍼 정리
            ClearSocketBuffer();

            if (!any)
            {
                logAction?.Invoke("PRMSG: tests 배열이 비어있습니다.");
                finalResult = false;
            }

            return new Dictionary<string, object?>
    {
        { "command",  lastCommand },
        { "response", lastResponse },
        { "expected", expectedResponse },
        { "success",  finalResult }
    };

        }
    }
}