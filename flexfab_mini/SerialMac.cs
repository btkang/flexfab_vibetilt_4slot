using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace flexfab
{
    public partial class SerialMac : Form
    {
        public event Action<string, string> OnSerialMacEntered;

        // ▼ 추가: 경로/상태
        private readonly string _baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private string _serialFileName = "serial.txt";
        public string SerialFileName { set { _serialFileName = value; } }
        private string SerialPath => Path.Combine(_baseDir, _serialFileName);
        private string MacPath => Path.Combine(_baseDir, "MacAddress.txt");
        private string ConfigPath => Path.Combine(_baseDir, "Config", "config.ini");
        private bool _macWriteEnabled = true; // ini로 결정

        public SerialMac()
        {
            InitializeComponent();
            this.Text = "Serial & Mac Input";
            this.StartPosition = FormStartPosition.CenterParent;

            this.Load += SerialMac_Load; // 폼 로드 시 자동 채움
        }

        private void SerialMac_Load(object sender, EventArgs e)
        {
            // 1) ini 읽기
            _macWriteEnabled = ReadIniBool(ConfigPath, "General", "MacAddressWrite", true);

            // 2) serial 자동입력 (파일 없으면 생성 후 "000001")
            textBox_Serial.Text = GetLastOrInit(SerialPath, "00001");
            textBox_Serial.MaxLength = 5;

            // 3) mac 자동입력
            if (_macWriteEnabled)
                textBox_Mac.Text = GetLastOrInit(MacPath, "000000000001");
            else
                textBox_Mac.Text = "Not Use";

            // 4) +/- 버튼 추가
            Action<TextBox, int> adjustSerial = (txt, delta) =>
            {
                string val = txt.Text.Trim();
                string pfx = ""; string num = val;
                int d = val.LastIndexOf('-');
                if (d >= 0 && d < val.Length - 1) { pfx = val.Substring(0, d + 1); num = val.Substring(d + 1); }
                if (num.All(char.IsDigit) && long.TryParse(num, out long n))
                {
                    n = Math.Max(1, n + delta);
                    txt.Text = pfx + n.ToString().PadLeft(num.Length, '0');
                }
            };

            // 시리얼 텍스트박스 폭 줄이기 (+/- 공간 확보)
            textBox_Serial.Width = 350;

            var btnMinus = new Button
            {
                Text = "-",
                Location = new Point(textBox_Serial.Right + 5, textBox_Serial.Top - 2),
                Size = new Size(50, 40),
                Font = new Font("맑은 고딕", 16, FontStyle.Bold)
            };
            var btnPlus = new Button
            {
                Text = "+",
                Location = new Point(btnMinus.Right + 3, textBox_Serial.Top - 2),
                Size = new Size(50, 40),
                Font = new Font("맑은 고딕", 16, FontStyle.Bold)
            };
            btnPlus.Click += (s, ev) => adjustSerial(textBox_Serial, 1);
            btnMinus.Click += (s, ev) => adjustSerial(textBox_Serial, -1);

            // 초기화 버튼 (OK 버튼 오른쪽)
            var btnReset = new Button
            {
                Text = "초기화",
                Location = new Point(450, 310),
                Size = new Size(140, 75),
                Font = new Font("맑은 고딕", 16)
            };
            btnReset.Click += (s, ev) =>
            {
                string pfx = "";
                string val = textBox_Serial.Text.Trim();
                int d = val.LastIndexOf('-');
                if (d >= 0) pfx = val.Substring(0, d + 1);
                textBox_Serial.Text = pfx + "00001";
            };

            this.Controls.Add(btnPlus);
            this.Controls.Add(btnMinus);
            this.Controls.Add(btnReset);
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            string serial = string.IsNullOrWhiteSpace(textBox_Serial.Text) ? "Null" : textBox_Serial.Text;
            string mac = _macWriteEnabled
                            ? (string.IsNullOrWhiteSpace(textBox_Mac.Text) ? "Null" : textBox_Mac.Text)
                            : "Not Use";

            // MainForm에 전달
            OnSerialMacEntered?.Invoke(serial, mac);

            // ▼ 파일 증감/추가
            TryAppendNext(SerialPath, serial);            // serial은 항상 증감
            if (_macWriteEnabled) TryAppendNext(MacPath, mac); // Mac은 옵션

            this.Close();
        }

        // ===== Helpers =====

        // INI bool 읽기 (섹션/키 대소문자 무시, "true/1/on/yes" = true)
        private static bool ReadIniBool(string path, string section, string key, bool defaultValue)
        {
            try
            {
                if (!File.Exists(path)) return defaultValue;
                string curSec = "";
                foreach (var raw in File.ReadAllLines(path, new UTF8Encoding(false)))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";")) continue;
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        curSec = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }
                    if (!curSec.Equals(section, StringComparison.OrdinalIgnoreCase)) continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var k = line.Substring(0, eq).Trim();
                    var v = line.Substring(eq + 1).Trim();

                    if (k.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        var s = v.ToLowerInvariant();
                        return s == "true" || s == "1" || s == "on" || s == "yes";
                    }
                }
            }
            catch { }
            return defaultValue;
        }

        // 파일이 있으면 마지막 유효줄 반환, 없으면 생성+시드 리턴
        private static string GetLastOrInit(string path, string seed)
        {
            var enc = new UTF8Encoding(false);
            try
            {
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, seed + Environment.NewLine, enc);
                    return seed;
                }

                var lines = File.ReadAllLines(path, enc);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    var t = lines[i].Trim();
                    if (t.Length > 0) return t;
                }

                // 빈 파일이면 시드 기록
                File.AppendAllText(path, seed + Environment.NewLine, enc);
                return seed;
            }
            catch
            {
                return seed;
            }
        }

        // 현재 값 기반 다음 번호를 동일 자릿수로 +1 해서 한 줄 추가
        private static void TryAppendNext(string path, string current)
        {
            try
            {
                // 숫자만 +1 (영문/기타면 그대로 기록만 수행)
                if (current.All(char.IsDigit))
                {
                    int width = current.Length;
                    if (BigInteger.TryParse(current, out var num))
                    {
                        num += 1;
                        string next = num.ToString().PadLeft(width, '0');
                        File.AppendAllText(path, next + Environment.NewLine, new UTF8Encoding(false));
                        return;
                    }
                }
                // 숫자 파싱 실패 시: 현재값 그대로 한 줄 추가(보수적)
                File.AppendAllText(path, current + Environment.NewLine, new UTF8Encoding(false));
            }
            catch { /* ignore */ }
        }
    }
}