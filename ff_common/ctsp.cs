using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;

namespace Cantops.FlexFab
{
    public class CtspProtoPacket
    {
        public required string cmd;
        public int? res = null;
        public required List<string> args;

    }
    public class CtspProto
    {
        private byte packet_type_ = 0;
        private List<byte>? buffer_ = null;

        // 패킷 
        public byte[] BuildPacket(string cmd, IEnumerable args, int? res = null, bool crc = true)
        {
            string str = "<" + cmd;
            foreach (var iter in args)
            {
                str += "," + iter.ToString();
            }
            str += ">";

            // CRC 처리 추가 예정
            return Encoding.UTF8.GetBytes(str);
        }

        // 패킷 파싱 - public으로 변경
        public List<CtspProtoPacket> ParsingPacket(byte[] buf)
        {
            List<CtspProtoPacket> ret = new();
            foreach (byte c in buf)
            {
                if (c == '[')
                {
                    packet_type_ = c;
                    buffer_ = new();
                }
                else if (c == ']')
                {
                    if (packet_type_ != '[')
                    {
                        buffer_ = null;
                        packet_type_ = 0;
                    }
                    else
                    {
                        if (buffer_ != null)
                        {
                            string[] str_array = Encoding.UTF8.GetString(buffer_.ToArray()).Split(',');
                            if (str_array.Length < 2) continue;

                            string cmd = str_array[0];
                            int? res = int.TryParse(str_array[1], out var resValue) ? resValue : null;
                            List<string> args = new List<string>(str_array.Skip(2));

                            ret.Add(new CtspProtoPacket
                            {
                                cmd = cmd,
                                res = res,
                                args = args
                            });
                        }
                    }
                }
                else
                {
                    buffer_?.Add(c);
                }
            }
            return ret;
        }
    }


    public class Ctsp : ModuleBase, IProtocol
    {
        private IComm? comm_ = null;
        private bool crc_enable_ = false;

        public Ctsp()
        {
            info_ = new Dictionary<string, object?>
        {
            { "name", "ctsp" },
            { "desc", "CTSP protocol" },
            { "ver", "1.0" }
        };
        }

        // PreRun을 통해 comm_ 객체 설정 및 초기화
        public override void PreRun(IDictionary<string, object?> libraries)
        {
            if (config_ == null || config_["commlib"] == null || !(config_["commlib"] is string commlibKey))
            {
                throw new InvalidOperationException("Configuration for 'commlib' is missing or invalid.");
            }

            if (!libraries.ContainsKey(commlibKey))
            {
                throw new KeyNotFoundException($"CTSP library with key '{commlibKey}' not found or is not of type IProtocol.");
            }

            comm_ = (IComm?)libraries[commlibKey];
            if (comm_ == null)
            {
                throw new InvalidOperationException($"'commlib' is not IComm");
            }

            var stat = comm_.GetStatus();
            if (stat != null && stat.ContainsKey("type"))
            {
                if (stat["type"] is string commtype && commtype == "uart")
                {
                    crc_enable_ = true;
                }
            }
        }


        // 명령어 전송 및 응답 수신
        public IDictionary<string, object?>? DoCommand(IDictionary<string, object?> command)
        {
            var ret = new Dictionary<string, object?>();

            if (comm_ == null)
            {
                throw new ArgumentException("Communication Handle is NULL");
            }

            if (!command.ContainsKey("cmd") || !(command["cmd"] is string cmd_string))
            {
                throw new ArgumentException("Command must contain a valid 'cmd' string.");
            }

            if (!command.ContainsKey("args") || !(command["args"] is IEnumerable args))
            {
                throw new ArgumentException("Command must contain a valid 'args' IEnumerable.");
            }

            CtspProto proto = new();

            // Send command
            byte[] packet = proto.BuildPacket(cmd_string, args, crc: crc_enable_);
            comm_.Send(new Dictionary<string, object?> { { "body", packet } });

            // RECV: 응답 수신 및 파싱
            while (true)
            {
                var rxbuf = comm_.Recv(null); // Receive data
                byte[] rxData = rxbuf != null && rxbuf.ContainsKey("value") ? (byte[])rxbuf["value"] : null;

                if (rxData != null)
                {
                    List<CtspProtoPacket> packets = proto.ParsingPacket(rxData);
                    if (packets.Count > 0)
                    {
                        // 응답 반환
                        return new Dictionary<string, object?> { { "cmd", packets[0].cmd }, { "res", packets[0].res }, { "args", packets[0].args } };
                    }
                }
            }

            return null;
        }
    }
}
