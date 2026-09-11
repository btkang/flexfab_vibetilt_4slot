

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Cantops.FlexFab
{
    public class Tcp : ModuleBase, IComm, IProcess
    {
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _isConnected;
        private IComm _tcpHandle;

        public const string MODULE_VERSION = "1.0"; // 원하는 버전

        public Tcp()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "tcp" },
                { "desc", "TCP module" },
                { "ver",  MODULE_VERSION },
            };
            _tcpClient = new TcpClient();
        }


        public override IDictionary<string, object?> GetStatus()
        {
            return new Dictionary<string, object?>
            {
                { "Connected", _tcpClient?.Connected ?? false }
            };
        }

        public bool Connect()
        {
            if (_tcpClient == null || _tcpClient.Client == null || !_tcpClient.Connected)
            {
                // 기존 TCP 클라이언트가 닫혀있다면 새로 생성
                _tcpClient = new TcpClient();
            }

            // IP와 포트가 설정되었는지 확인
            if (config_ == null || !config_.ContainsKey("ip") || !config_.ContainsKey("port"))
            {
                Log("TCP 설정이 유효하지 않습니다.");
                throw new InvalidOperationException("TCP 설정이 유효하지 않습니다.");
            }

            string ip = config_["ip"].ToString();
            int port = Convert.ToInt32(config_["port"]);

            Log($"TCP 연결 시도 중: {ip}:{port}");

            try
            {
                // 포트 재사용 설정
                _tcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // TCP 연결 시도 (5초 타임아웃)
                if (!_tcpClient.ConnectAsync(ip, port).Wait(5000))
                    throw new TimeoutException($"TCP 연결 타임아웃 (5초): {ip}:{port}");
                _networkStream = _tcpClient.GetStream(); // 네트워크 스트림 확보

                Log("TCP 연결 성공");
                return true;
            }
            catch (SocketException ex)
            {
                Log($"TCP 연결 실패 (SocketException): {ex.Message} (ErrorCode: {ex.ErrorCode})");
            }
            catch (TimeoutException ex)
            {
                Log($"TCP 연결 실패 (Timeout): {ex.Message}");
            }
            catch (Exception ex)
            {
                Log($"TCP 연결 실패 (Exception): {ex.Message}");
            }

            //Log("TCP 연결 실패");
            return false;
        }




        public void Disconnect()
        {
            if (_tcpClient?.Connected ?? false)
            {
                //Log("TCP 연결 종료");
                _networkStream?.Close();
                _tcpClient.Close();
            }
        }

        // TCP 연결을 시작하는 메서드 (begin())
        public void Begin()
        {
            if (!_isConnected)
            {
                //Log("TCP 연결을 시도합니다...");
                Connect();
            }
        }

        // TCP 연결을 종료하는 메서드 (end())
        public void End()
        {
            if (_isConnected)
            {
                //Log("TCP 연결 종료...");
                Disconnect();
            }
        }

        public void SetTimeout(int timeout)
        {
            if (_tcpClient.Connected)
                throw new InvalidOperationException("연결 상태에서 타임아웃을 변경할 수 없습니다.");

            _tcpClient.ReceiveTimeout = timeout;
            _tcpClient.SendTimeout = timeout;
        }


        public void Send(IDictionary<string, object?> param)
        {
            try
            {
                if (!_tcpClient.Connected)
                    throw new InvalidOperationException("TCP가 연결되지 않았습니다.");

                // 'body' 파라미터를 받아오기
                string dataToSend = param.ContainsKey("body") ? param["body"]?.ToString() ?? throw new InvalidOperationException("body가 설정되지 않았습니다.") : throw new InvalidOperationException("body가 설정되지 않았습니다.");

                byte[] bytes = Encoding.UTF8.GetBytes(dataToSend + "\n");
                _networkStream.Write(bytes, 0, bytes.Length);

                //Log("TCP 데이터 전송 완료");
            }
            catch (Exception ex)
            {
                Log($"TCP 데이터 전송 실패: {ex.Message}");
                // 예외 발생해도 계속 진행하기 위해 예외를 무시하고 넘어감
            }
        }



        public IDictionary<string, object?>? Recv(IDictionary<string, object?>? param)
        {
            try
            {
                if (_networkStream == null || !_tcpClient.Connected)
                {
                    Log("TCP 연결이 되어 있지 않습니다. 데이터를 수신할 수 없습니다.");
                    return null;
                }

                int timeout = param?.ContainsKey("timeout") == true ? Convert.ToInt32(param["timeout"]) : 5000;
                _networkStream.ReadTimeout = timeout;
                //Log("TCP 데이터 수신 대기 중...");

                byte[] buffer = new byte[1024];

                try
                {
                    int bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string receivedData = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                        //Log($"TCP 데이터 수신 완료: {receivedData}");

                        // 🔄 추가 잔여 수신 (비워주지는 않지만 존재하면 로그)
                        try
                        {
                            _networkStream.ReadTimeout = 100;
                            int leftoverRead = _networkStream.Read(buffer, 0, buffer.Length);
                            if (leftoverRead > 0)
                            {
                                string extraData = Encoding.ASCII.GetString(buffer, 0, leftoverRead).Trim();
                                if (!string.IsNullOrWhiteSpace(extraData)) ;
                                    //Log($"⚠️ 추가 잔여 데이터 발견: {extraData}");
                            }
                        }
                        catch (IOException)
                        {
                            // 추가 데이터 없음: 무시
                        }

                        return new Dictionary<string, object?> { { "value", receivedData } };
                    }
                    else
                    {
                        Log("수신된 데이터가 없습니다.");
                        return null;
                    }
                }
                catch (IOException ex)
                {
                    //Log($"TCP 데이터 수신 오류: {ex.Message}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                //Log($"예기치 않은 TCP 수신 오류: {ex.Message}");
                return null;
            }
        }

        // 버퍼 비우기
        private void ClearSocketBuffer(string portType, Action<string> logAction, int maxAttempts = 5, int timeout = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                var flushParam = new Dictionary<string, object?> {
            { "timeout", timeout },
            { "porttype", portType }
        };
                var flushResponse = Recv(flushParam);
                if (flushResponse?.ContainsKey("value") == true)
                {
                    string leftover = flushResponse["value"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(leftover))
                    {
                        //logAction($"⚠️ 잔여 응답 버퍼 비움: {leftover}");
                    }
                    else
                    {
                        //logAction("✔️ 버퍼 비움 완료 (공백 수신)");
                        break;
                    }
                }
                else
                {
                    //logAction("✔️ 더 이상 버퍼에 남은 응답 없음");
                    break;
                }
            }
        }


        public IDictionary<string, object?> TCPMSG(
    IDictionary<string, object?> libraries,
    IDictionary<string, object?> param,
    Action<string> logAction)
        {
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null || !tests.Any())
            {
                logAction("tests 배열이 비어있거나 존재하지 않습니다.");
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");
            }

            // 0) 리턴 메시지 메타 (우선순위: __proc_* > proc_* > param.name > "")
            string procId =
                (param.TryGetValue("__proc_id", out var pid0) && pid0 is string s0 && !string.IsNullOrWhiteSpace(s0)) ? s0 :
                (param.TryGetValue("proc_id", out var pid1) && pid1 is string s1 && !string.IsNullOrWhiteSpace(s1)) ? s1 :
                "TCPMSG";

            string procName =
                (param.TryGetValue("__proc_name", out var pn0) && pn0 is string n0 && !string.IsNullOrWhiteSpace(n0)) ? n0 :
                (param.TryGetValue("proc_name", out var pn1) && pn1 is string n1 && !string.IsNullOrWhiteSpace(n1)) ? n1 :
                (param.TryGetValue("name", out var pn2) && pn2 is string n2 && !string.IsNullOrWhiteSpace(n2)) ? n2 :
                "";

            // 1) 어떤 TCP 핸들을 쓸지: param["port"] 우선, 없으면 this
            IComm target = this;
            if (param.TryGetValue("port", out var pv) && pv is string portId && !string.IsNullOrWhiteSpace(portId))
            {
                if (libraries.TryGetValue(portId, out var handleObj) && handleObj is IComm handle)
                {
                    target = handle;
                    logAction($"TCPMSG: param['port']='{portId}' 핸들 사용.");
                }
                else
                {
                    logAction($"TCPMSG: param['port']='{portId}' 핸들을 libraries에서 찾을 수 없어 현재 인스턴스를 사용합니다.");
                }
            }

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
            if (!IsConnected(target))
            {
                try { target.GetType().GetMethod("Begin")?.Invoke(target, null); } catch { }
                if (!IsConnected(target))
                {
                    try { target.GetType().GetMethod("Connect")?.Invoke(target, null); } catch { }
                }
            }

            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            // 3) 시작 시 버퍼 정리
            void Flush(IComm c)
            {
                if (ReferenceEquals(c, this))
                {
                    try { ClearSocketBuffer("tcp", logAction); } catch { /* ignore */ }
                }
                else
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
            }
            Flush(target);

            // 4) 테스트 루프
            string lastCommand = "";
            string lastResponse = "";
            string expectedResponse = "";
            bool finalResult = true;

            var sbRet = new System.Text.StringBuilder(); // ← JSON 객체(한 줄) 누적

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
                target.Send(new Dictionary<string, object?> {
            { "body", command },
            { "porttype", "tcp" }
        });

                var response = target.Recv(new Dictionary<string, object?> {
            { "timeout", timeout },
            { "porttype", "tcp" }
        });

                lastResponse = (response != null && response.ContainsKey("value"))
                    ? response["value"]?.ToString() ?? ""
                    : "";

                bool testOk;
                if (string.IsNullOrEmpty(expectedResponse))
                {
                    // 기대값이 없으면, 응답이 비어있지 않으면 OK
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

                // ★ retmsg: JSON 객체로 한 줄씩
                var obj = new JObject
                {
                    ["Name"] = procName,
                    ["id"] = procId,
                    ["Result"] = testOk ? "OK" : "NG",
                    ["Expected_Response"] = expectedResponse,
                    ["Response"] = lastResponse
                };
                sbRet.AppendLine(obj.ToString(Newtonsoft.Json.Formatting.None));
            }

            // 5) 종료 시 버퍼 정리
            Flush(target);

            return new Dictionary<string, object?>
    {
        { "command",  lastCommand },
        { "response", lastResponse },
        { "expected", expectedResponse },
        { "success",  finalResult },
        { "retmsg",   sbRet.ToString() } // ← 줄바꿈으로 구분된 JSON 객체 문자열들
    };
        }







        public IDictionary<string, object?> TCPYesNo(
    IDictionary<string, object?> libraries,
    IDictionary<string, object?> param,
    Action<string> logAction,
    Func<string, bool> confirmCallback)
        {
            var tests = param.ContainsKey("tests") ? param["tests"] as IEnumerable<dynamic> : null;
            if (tests == null || !tests.Any())
            {
                logAction("tests 배열이 비어있거나 존재하지 않습니다.");
                throw new ArgumentException("tests 배열이 비어있거나 존재하지 않습니다.");
            }

            var test = tests.FirstOrDefault() as IDictionary<string, object>;
            if (test == null || !test.ContainsKey("command"))
            {
                logAction("테스트 항목에 'command'가 없습니다.");
                throw new ArgumentException("테스트 항목에 'command'가 없습니다.");
            }

            // 포트 타입 고정
            string portType = "tcp";
            string command = test["command"]?.ToString() ?? "";
            string comment = test.ContainsKey("comment") ? (test["comment"]?.ToString() ?? "확인을 진행하시겠습니까?") : "확인을 진행하시겠습니까?";
            int timeout = param.ContainsKey("timeout") ? Convert.ToInt32(param["timeout"]) : 5000;

            // 진행 메타 (retmsg용): __proc_* 우선, 없으면 proc_*, 없으면 기본값
            string procId =
                (param.TryGetValue("__proc_id", out var pid0) && pid0 != null) ? pid0.ToString()! :
                (param.TryGetValue("proc_id", out var pid1) && pid1 != null) ? pid1.ToString()! :
                "TCPYesNo";

            string procName =
                (param.TryGetValue("__proc_name", out var pn0) && pn0 != null) ? pn0.ToString()! :
                (param.TryGetValue("proc_name", out var pn1) && pn1 != null) ? pn1.ToString()! :
                "Yes/No";

            // 시작 시 버퍼 정리
            ClearSocketBuffer(portType, logAction);

            // 명령 전송
            logAction($"명령어 전송: {command}");
            var writeParam = new Dictionary<string, object?> {
        { "body", command },
        { "porttype", portType }
    };
            Send(writeParam);

            // 응답 수신
            var readParam = new Dictionary<string, object?> {
        { "timeout", timeout },
        { "porttype", portType }
    };
            var response = Recv(readParam);
            string receivedMessage = response != null && response.ContainsKey("value")
                ? (response["value"]?.ToString() ?? "")
                : "";

            logAction($"✅ TCPYesNo 수신 메시지: [{receivedMessage}]");

            // 사용자 확인
            bool isConfirmed = confirmCallback?.Invoke(comment) ?? false;
            logAction($"사용자 선택 결과: {(isConfirmed ? "OK" : "FAIL")}");

            // 잔여 응답 버퍼 정리(짧게 폴링)
            for (int i = 0; i < 5; i++)
            {
                var flushParam = new Dictionary<string, object?> {
            { "timeout", 200 },
            { "porttype", portType }
        };
                var flushResponse = Recv(flushParam);
                if (flushResponse?.ContainsKey("value") == true)
                {
                    string leftover = flushResponse["value"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(leftover)) break;
                }
                else break;
            }

            // 끝나고도 버퍼 정리
            ClearSocketBuffer(portType, logAction);

            // ★ retmsg를 JSON 객체(한 줄)로 생성
            var retObj = new JObject
            {
                ["Name"] = procName,
                ["id"] = procId,
                ["Result"] = isConfirmed ? "OK" : "NG"
            };
            string retmsg = retObj.ToString(Newtonsoft.Json.Formatting.None);

            return new Dictionary<string, object?>
    {
        { "command",  command },
        { "response", receivedMessage },
        { "success",  isConfirmed },
        { "retmsg",   retmsg }
    };
        }


    }


}

