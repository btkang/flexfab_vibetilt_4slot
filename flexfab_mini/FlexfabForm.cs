using Cantops.FlexFab;
using Cantops.FlexFab.Mini;
using ff_common;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace flexfab
{
    public partial class MainForm : Form
    {

        private App app; // 기존 App 인스턴스
        private dynamic workspace; // 로드된 워크스페이스
        private Action<string> logAction; // 로그 출력용 델리게이트
        private Label label_CurrentTime;
        private CancellationTokenSource cancellationTokenSource; //stop 버튼을 위한 취소동작 토큰
        private Label label2;
        private Button buttonStop;
        private CheckBox checkBox_Process;
        private Button buttonClear;
        private System.Windows.Forms.Timer _timer;

        private IProtocol ctsp_;  // ctsp_를 멤버 변수로 선언
        private Button button_Copy;
        private Button button_CopyAll;
        private IDictionary<string, object> libraries; // 필요한 경우 libraries를 선언하여 관련 데이터를 저장
        private bool isWindowShrunk = false; // 창이 축소되었는지 여부를 추적하는 변수
        private JArray _libSnapshot = new JArray(); // SaveLog에서 쓸 라이브러리 메타(순서 보존)

        private PassForm passForm; // PassForm 인스턴스
        private Label label_Serial;
        private Label label_mac;
        private FailForm failForm; // FailForm 인스턴스

        internal string serialNumber;
        internal string serialNumberY; // Y축 제품 시리얼 (밀어내기식)
        private bool _serialAutoIncrement = false; // serial_auto_increment: 시리얼 자동증가
        private Label label_passSaved; // 최종 PASS 시리얼 표시
        private Button button_expand;
        private string macAddress;

        private dynamic project;
        private int okCount = 0;
        private int failCount = 0;
        private Label label_inspector;
        private Button button_ChangeInspector;
        private int skipCount = 0;

        private bool _isRunning;
        private Button button_OpenWorkspace;
        internal string _currentWorkspacePath; //어느 workspace를 열었는지 저장
        private string _logProjFolder = "";  // 로그 프로젝트 폴더명 (공백제거)

        // ── 불량보기(FailInfoForm) 이식 (pSMC) : 회차 단위 누적 불량 목록 ──
        private readonly List<FailRecord> _failRecords = new();   // 불량 상세 누적
        private bool _failDetailPopup = true;  // workspace fail_detail (true=FAIL시 불량보기 자동표시)
        private int _autoFailInfoIdx = -1;     // 자동 선택할 행(-1=없음)
        private Button button_FailInfo;        // "불량보기" 버튼(프로그래밍 추가)

        // ── 테스트 설정(F2 런타임 오버라이드 + 저장) pSMC 이식 ──
        private Button button_TestSettings;    // "테스트 설정" 버튼(불량보기 왼쪽)
        private Label  label_overrides;        // 런타임 오버라이드 요약(작게)
        private bool   _settingsOverridden;    // 런타임 오버라이드 적용됨 표시
        private Font   _logBoldFont;           // 로그 굵게용 캐시(FAIL·TEST 마커) pSMC 이식


        public MainForm(dynamic workspace)
        {
            InitializeComponent();
            this.workspace = workspace;
            this.Load += MainForm_Load;


            app = new App(); // App 인스턴스 생성
            logAction = message => Log(message); // 로그 액션 정의

            // 타이머 초기화 및 설정
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000;  // 1초 간격
            _timer.Tick += Timer_Tick;
            _timer.Start();  // 타이머 시작

            // KeyDown 이벤트 핸들러를 설정
            this.KeyDown += new KeyEventHandler(MainForm_KeyDown);

            // 폼에 Focus를 줄 수 있도록 설정
            this.KeyPreview = true;



            // PassForm과 FailForm 인스턴스를 초기화합니다.
            passForm = new PassForm();
            failForm = new FailForm();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // 현재 시간 가져오기
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // label_CurrentTime에 현재 시간 표시
            label_CurrentTime.Text = currentTime;
        }

        // 단축키 처리
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // 창 크기 조절 (Ctrl + Alt + X)
            if (e.Control && e.Alt && e.KeyCode == Keys.X)
            {
                ToggleWindowSize();
                e.Handled = true;
            }
            // 로그 전체 선택 (Ctrl + A)
            else if (e.Control && e.KeyCode == Keys.A)
            {
                if (listBox_Log.Items.Count > 0)
                {
                    listBox_Log.BeginUpdate();
                    for (int i = 0; i < listBox_Log.Items.Count; i++)
                    {
                        listBox_Log.SetSelected(i, true);
                    }
                    listBox_Log.EndUpdate();
                }
                e.Handled = true;
                e.SuppressKeyPress = true; // 띵 소리 방지
            }
            // 로그 복사 (Ctrl + C)
            else if (e.Control && e.KeyCode == Keys.C)
            {
                PerformLogCopy(silent: true);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void PerformLogCopy(bool silent)
        {
            if (listBox_Log.SelectedItems.Count == 0)
            {
                if (!silent) MessageBox.Show("선택된 항목이 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var sb = new StringBuilder();
                foreach (var item in listBox_Log.SelectedItems)
                {
                    sb.AppendLine(item?.ToString() ?? "");
                }

                string text = sb.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                    if (!silent) MessageBox.Show("선택된 항목이 클립보드에 복사되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (!silent) MessageBox.Show($"복사 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeLogContextMenu()
        {
            var ctx = new ContextMenuStrip();
            
            var itemCopy = ctx.Items.Add("복사 (Ctrl+C)");
            itemCopy.Click += (s, e) => PerformLogCopy(silent: false);

            var itemSelectAll = ctx.Items.Add("전체 선택 (Ctrl+A)");
            itemSelectAll.Click += (s, e) =>
            {
                listBox_Log.BeginUpdate();
                for (int i = 0; i < listBox_Log.Items.Count; i++) listBox_Log.SetSelected(i, true);
                listBox_Log.EndUpdate();
            };

            ctx.Items.Add(new ToolStripSeparator());

            var itemClear = ctx.Items.Add("로그 지우기");
            itemClear.Click += (s, e) => listBox_Log.Items.Clear();

            listBox_Log.ContextMenuStrip = ctx;
        }

        // 창 크기 변경 (축소 및 복구)
        private void ToggleWindowSize()
        {
            if (isWindowShrunk)
            {
                // 원래 크기로 복구
                this.Size = new Size(1541, 819); // 원래 창 크기

                listBox_Log.Visible = true;
            }
            else
            {
                // 창 크기 축소
                this.Size = new Size(941, 819); // 축소된 크기

                listBox_Log.Visible = false;
            }

            // 상태 업데이트
            isWindowShrunk = !isWindowShrunk;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            dataGridView1.ReadOnly = true;

            // "불량보기" 버튼 (Copy 버튼 행 좌측에 프로그래밍 추가) — 누적 불량 상세 확인
            button_FailInfo = new Button
            {
                Name = "button_FailInfo",
                Text = "불량보기",
                Location = new Point(651, 615),   // 검사자변경(651,665) 위
                Size = new Size(145, 29),         // 검사자변경 버튼과 동일 크기
                UseVisualStyleBackColor = true
            };
            button_FailInfo.Click += (_, __) => ShowFailInfo(-1);
            Controls.Add(button_FailInfo);
            button_FailInfo.BringToFront();

            // "테스트 설정" 버튼 (불량보기 1013 왼쪽 895) — 런타임 오버라이드 진입(게이트 경유)
            button_TestSettings = new Button
            {
                Name = "button_TestSettings",
                Text = "테스트 설정",
                Location = new Point(1013, 1),   // Copy All(1132) 왼쪽
                Size = new Size(113, 27),
                UseVisualStyleBackColor = true
            };
            button_TestSettings.Click += (_, __) => OpenTestSettings();
            Controls.Add(button_TestSettings);
            button_TestSettings.BringToFront();

            // 런타임 오버라이드 요약(작게) — 오버라이드 적용 시에만 텍스트 표시
            label_overrides = new Label
            {
                Name = "label_overrides",
                Location = new Point(651, 590),   // 불량보기(651,615) 위
                AutoSize = true,
                Font = new Font("맑은 고딕", 8),
                ForeColor = Color.DimGray,
                Text = ""
            };
            Controls.Add(label_overrides);
            label_overrides.BringToFront();

            // VibeTilt DLL 버전 로그
            var vtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ff_vibetilt.dll");
            var vtVer = File.Exists(vtPath) ? System.Diagnostics.FileVersionInfo.GetVersionInfo(vtPath).ProductVersion ?? "?" : "?";
            var vtPlus = vtVer.IndexOf('+'); if (vtPlus >= 0) vtVer = vtVer[..vtPlus];
            Log("──────────────────────────────────────────────");
            Log($"[INFO] ff_vibetilt.dll version: {vtVer}");

            InitConfig(); // config.ini 먼저 초기화 (IniRead 사용 전)

            // workspace 로드: config.ini의 LastWorkspace 기반
            string lastWs = IniRead("General", "LastWorkspace", "").Trim();
            if (!string.IsNullOrWhiteSpace(lastWs))
            {
                string wsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, lastWs);
                if (File.Exists(wsPath))
                {
                    workspace = MainLoadWorkspace(wsPath, Log);
                    _currentWorkspacePath = wsPath;
                    Log($"[INFO] workspace: {lastWs}");
                }
            }

            // log_level 설정 읽기
            if (workspace != null)
            {
                try
                {
                    string ll = workspace.log_level;
                    if (!string.IsNullOrEmpty(ll)) Cantops.FlexFab.LogConfig.LogLevel = ll;
                }
                catch { }
            }
            try { _serialAutoIncrement = (bool)(workspace?.serial_auto_increment ?? false); } catch { _serialAutoIncrement = false; }
            // fail_detail: FAIL 카운트 팝업 직후 불량보기 자동표시 on/off (기본 true, 키 없으면 catch로 유지). pSMC 동일 패턴.
            try { _failDetailPopup = Convert.ToInt32(workspace.fail_detail) != 0; } catch { _failDetailPopup = true; }
            { string tm = "dual"; try { int tmVal = Convert.ToInt32(workspace?.test_mode ?? 0); tm = tmVal == 1 ? "single" : "dual"; } catch { }
            int rc = 1; try { rc = Convert.ToInt32(((IDictionary<string, object>)workspace.projects[0]).TryGetValue("repeat_all_count", out var _rc) ? _rc : 1); } catch { }
            Log($"[INFO] log_level: {Cantops.FlexFab.LogConfig.LogLevel} | test_mode: {tm} | repeat: {rc}");
            label_cycle.Text = rc > 1 ? $"(repeat: {rc})" : ""; }

            // 설정값 로그
            if (workspace != null)
            {
                try
                {
                    bool sl = false; try { sl = Convert.ToInt32(workspace.show_log) != 0; } catch { }
                    int dc = 0; try { dc = Convert.ToInt32(workspace.serial_duplicate_check); } catch { }
                    // fail_continue: JSON에서 체크박스 초기값 설정
                    try { bool fc = Convert.ToInt32(workspace.fail_continue) != 0; checkBox_Process.Checked = fc; } catch { }
                    string sp = ""; try { sp = (string)(workspace?.serial_prefix ?? ""); } catch { }
                    int spc = 0; try { spc = Convert.ToInt32(workspace?.serial_prefix_check ?? 0); } catch { }
                    // spcLabel은 사용하지 않음
                    Log($"[INFO] show_log: {sl.ToString().ToLower()} | serial_duplicate_check: {dc} (1:경고팝업 2:차단 0:안함) | fail_continue: {checkBox_Process.Checked.ToString().ToLower()}");
                    Log($"[INFO] serial_auto_increment: {_serialAutoIncrement.ToString().ToLower()}");
                    if (!string.IsNullOrEmpty(sp)) Log($"[INFO] serial_prefix: {sp} | serial_prefix_check: {spc} (1:경고팝업 2:차단 0:안함)");

                    // 검사 판정조건 로그 (워크스페이스 procs에서 TILT/VIBE 파라미터 읽기)
                    try
                    {
                        dynamic procs = workspace.projects[0].procs;
                        bool isMotionJig = false;
                        foreach (var proc in procs) { if (((string)(proc.id ?? "")).Contains("MOTION_")) { isMotionJig = true; break; } }

                        if (isMotionJig)
                        {
                            // 모션지그: TILT/VIBE 판정설정
                            foreach (var proc in procs)
                            {
                                string procId = (string)(proc.id ?? "");
                                if (procId.Contains("TILT") && procId.Contains("X"))
                                {
                                    dynamic p = proc.param;
                                    string jm = (string)(p?.tilt_judge_mode ?? "");
                                    int tc = (int)(p?.tilt_judge_tail_count ?? 0);
                                    int rc = (int)(p?.tilt_retry_count ?? 0);
                                    int sd = (int)(p?.tilt_stream_duration ?? 0);
                                    double t20 = (double)(p?.tilt_tolerance_20 ?? 0);
                                    double t45 = (double)(p?.tilt_tolerance_45 ?? 0);
                                    double st = (double)(p?.fine_adjust_strict_tolerance ?? 0.05);
                                    int tsw = (int)(p?.tilt_settle_wait ?? 500);
                                    Log($"[INFO] TILT: mode={jm} | tail={tc} | retry={rc} | stream={sd}ms | settle={tsw}ms | spec: ±20도=±{t20}, ±45도=±{t45} | 자이로=±{st}");
                                    break;
                                }
                            }
                            foreach (var proc in procs)
                            {
                                string procId = (string)(proc.id ?? "");
                                if (procId.Contains("VIBE") && procId.Contains("Y"))
                                {
                                    dynamic p = proc.param;
                                    string jm = (string)(p?.vibe_judge_mode ?? "");
                                    int tc = (int)(p?.vibe_judge_tail_count ?? 0);
                                    int rc = (int)(p?.vibe_retry_count ?? 0);
                                    int sd = (int)(p?.vibe_stream_duration ?? 0);
                                    double vmin = (double)(p?.vibe_min ?? 0);
                                    double vmax = (double)(p?.vibe_max ?? 0);
                                    int gsw = (int)(p?.gac_settle_wait ?? 500);
                                    Log($"[INFO] VIBE: mode={jm} | tail={tc} | retry={rc} | stream={sd}ms | settle={gsw}ms | spec: {vmin}~{vmax}");
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // 보드레벨: VIBE/TILT 판정설정
                            // judge_mode 표시 (VIBE 또는 TILT 첫 번째 항목에서 읽기)
                            foreach (var proc in procs)
                            {
                                string procId = (string)(proc.id ?? "");
                                if (procId.StartsWith("VIBE_") || procId.StartsWith("TILT_"))
                                {
                                    int jm = 1; try { jm = (int)(proc.param?.judge_mode ?? 1); } catch { }
                                    Log($"[INFO] judge_mode={jm} (1:값변화만확인 2:방향별각각확인)");
                                    break;
                                }
                            }
                            foreach (var proc in procs)
                            {
                                string procId = (string)(proc.id ?? "");
                                if (procId.StartsWith("VIBE_"))
                                {
                                    dynamic p = proc.param;
                                    double oneMin = (double)(p?.one_min ?? 0); double oneMax = (double)(p?.one_max ?? 0);
                                    double zeroMin = (double)(p?.zero_min ?? 0); double zeroMax = (double)(p?.zero_max ?? 0);
                                    double rmsMin = (double)(p?.rms_min ?? 0); double rmsMax = (double)(p?.rms_max ?? 0);
                                    double mt = (double)(p?.motion_threshold ?? 0.05);
                                    int pc = (int)(p?.pass_consecutive_count ?? 1);
                                    int to = (int)(p?.TIMEOUT ?? 10000);
                                    Log($"[INFO] VIBE: 1축={oneMin}~{oneMax} | 0축={zeroMin}~{zeroMax} | RMS={rmsMin}~{rmsMax} | moving={mt}(0축>{mt}시 움직임감지) | pass_count={pc} | TIMEOUT={to / 1000}s");
                                    break;
                                }
                            }
                            foreach (var proc in procs)
                            {
                                string procId = (string)(proc.id ?? "");
                                if (procId.StartsWith("TILT_X"))
                                {
                                    dynamic p = proc.param;
                                    string ax = (string)(p?.check_axis ?? "x");
                                    double xMin = (double)(p?.x_min ?? 0); double xMax = (double)(p?.x_max ?? 0);
                                    int pc = (int)(p?.pass_consecutive_count ?? 1);
                                    int to = (int)(p?.TIMEOUT ?? 10000);
                                    Log($"[INFO] TILT: axis=X spec={xMin}~{xMax}도 | axis=Y spec 동일 | pass_count={pc} | TIMEOUT={to / 1000}s");
                                    break;
                                }
                            }
                        }
                        // Y축 영점 skip_home 표시
                        foreach (var proc in procs)
                        {
                            string procName = (string)(proc.name ?? "");
                            if (procName.Contains("#9") && procName.Contains("Y축"))
                            {
                                bool sh = false;
                                try { sh = (bool)(proc.param?.skip_home ?? false); } catch { }
                                Log($"[INFO] Y축 영점(#9): skip_home={sh.ToString().ToLower()} ({(sh ? "홈생략→FineAdjust만" : "홈센서 사용")})");
                                break;
                            }
                        }
                    }
                    catch { }
                }
                catch { }
            }

            if (workspace != null)
            {
                try
                {
                    label_CurrentTest.Text = workspace.projects[0].name;
                    _logProjFolder = ((string)workspace.projects[0].name).Replace(" ", "");
                }
                catch { label_CurrentTest.Text = "(no project)"; }
            }
            else
            {
                label_CurrentTest.Text = "워크스페이스를 선택하세요";
            }

            button_start.Enabled = true;
            buttonStop.Enabled = false;

            // 로딩된 libraries 확인
            if (workspace != null && workspace.libraries != null)
            {
                var libraries = new Dictionary<string, object>();
                foreach (var lib in workspace.libraries)
                {
                    libraries[lib.id] = lib.instance; // 'instance'를 libraries에 추가
                }

                // libraries 확인
                //Log($"라이브러리 로딩 완료, 라이브러리 목록: {string.Join(", ", libraries.Keys)}");

                // InitializeCTSP 실행
                try
                {
                    InitializeCTSP(libraries);
                }
                catch (Exception ex)
                {
                    //Log($"CTSP 초기화 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                Log("워크스페이스 또는 libraries가 올바르게 로드되지 않았습니다.");
            }

            // show_log: 로그창 표시 여부 (기본값: TEST_LOG이면 true, MP_LOG이면 false)
            bool showLog = Cantops.FlexFab.LogConfig.IsTest;
            try { showLog = Convert.ToInt32(workspace.show_log) != 0; } catch { }

            if (showLog)
            {
                this.Size = new Size(1541, 819);
                listBox_Log.Visible = true;
                isWindowShrunk = false;
            }
            else
            {
                this.Size = new Size(941, 819);
                listBox_Log.Visible = false;
                isWindowShrunk = true;
            }

            LoadInspectorFromIni();
            InitializeLogContextMenu();
        }

        private void InitializeComponent()
        {
            button_start = new Button();
            button_Stop = new Button();
            listBox_Log = new ListBox();
            listBox_Log.DrawMode = DrawMode.OwnerDrawFixed;   // FAIL 줄 빨강+굵게, TEST_BEGIN/FINISH 굵게 (pSMC 이식)
            listBox_Log.DrawItem += listBox_Log_DrawItem;
            dataGridView1 = new DataGridView();
            Number = new DataGridViewTextBoxColumn();
            Name = new DataGridViewTextBoxColumn();
            Result = new DataGridViewTextBoxColumn();
            label1 = new Label();
            label_CurrentTest = new Label();
            label_cycle = new Label();
            label_CurrentTime = new Label();
            label2 = new Label();
            buttonStop = new Button();
            checkBox_Process = new CheckBox();
            buttonClear = new Button();
            button_Copy = new Button();
            button_CopyAll = new Button();
            label_Serial = new Label();
            label_mac = new Label();
            button_expand = new Button();
            label_inspector = new Label();
            button_ChangeInspector = new Button();
            button_OpenWorkspace = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // button_start
            // 
            button_start.Location = new Point(87, 656);
            button_start.Name = "button_start";
            button_start.Size = new Size(264, 112);
            button_start.TabIndex = 12;
            button_start.Text = "Start";
            button_start.UseVisualStyleBackColor = true;
            button_start.Click += button_start_Click;
            // 
            // button_Stop
            // 
            button_Stop.Location = new Point(12, 1);
            button_Stop.Name = "button_Stop";
            button_Stop.Size = new Size(20, 19);
            button_Stop.TabIndex = 14;
            button_Stop.Text = "Stop";
            button_Stop.UseVisualStyleBackColor = true;
            button_Stop.Visible = false;
            button_Stop.Click += button_Stop_Click;
            // 
            // listBox_Log
            // 
            listBox_Log.FormattingEnabled = true;
            listBox_Log.ItemHeight = 15;
            listBox_Log.Location = new Point(810, 32);
            listBox_Log.Name = "listBox_Log";
            listBox_Log.SelectionMode = SelectionMode.MultiExtended;
            listBox_Log.Size = new Size(702, 739);
            listBox_Log.TabIndex = 15;
            listBox_Log.HorizontalScrollbar = true;
            // 
            // dataGridView1
            // 
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Number, Name, Result });
            dataGridView1.Location = new Point(87, 90);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.Size = new Size(542, 560);
            dataGridView1.TabIndex = 16;
            dataGridView1.CellClick += dataGridView1_CellClick;
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
            dataGridView1.ColumnHeaderMouseClick += dataGridView1_ColumnHeaderMouseClick;
            // 
            // Number
            // 
            Number.HeaderText = "번호";
            Number.Name = "Number";
            Number.Width = 60;
            Number.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // Name
            //
            Name.HeaderText = "테스트 항목";
            Name.Name = "Name";
            Name.Width = 300;
            Name.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // Result
            // 
            Result.HeaderText = "결과";
            Result.Name = "Result";
            Result.SortMode = DataGridViewColumnSortMode.NotSortable;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(421, 63);
            label1.Name = "label1";
            label1.Size = new Size(35, 15);
            label1.TabIndex = 17;
            label1.Text = "Test :";
            // 
            // label_CurrentTest
            // 
            label_CurrentTest.AutoSize = true;
            label_CurrentTest.Location = new Point(462, 63);
            label_CurrentTest.Name = "label_CurrentTest";
            label_CurrentTest.Size = new Size(56, 15);
            label_CurrentTest.TabIndex = 18;
            label_CurrentTest.Text = "현재 Test";
            label_CurrentTest.MouseClick += (s, ev) =>
            {
                if (ev.Button == MouseButtons.Right && !string.IsNullOrEmpty(_currentWorkspacePath) && File.Exists(_currentWorkspacePath))
                {
                    var menu = new ContextMenuStrip();
                    var fileName = Path.GetFileName(_currentWorkspacePath);
                    menu.Items.Add(fileName, null, (_, _) =>
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_currentWorkspacePath) { UseShellExecute = true });
                    });
                    menu.Items.Add("폴더 열기", null, (_, _) =>
                    {
                        var folder = Path.GetDirectoryName(_currentWorkspacePath);
                        if (folder != null)
                            System.Diagnostics.Process.Start("explorer.exe", folder);
                    });
                    menu.Show((Control)s!, ev.Location);
                }
            };
            //
            // label_cycle
            //
            label_cycle.AutoSize = true;
            label_cycle.Location = new Point(330, 63);
            label_cycle.Name = "label_cycle";
            label_cycle.Font = new Font("Malgun Gothic", 9, FontStyle.Regular);
            label_cycle.ForeColor = Color.Black;
            label_cycle.Text = "";
            //
            // label_CurrentTime
            // 
            label_CurrentTime.AutoSize = true;
            label_CurrentTime.Location = new Point(110, 24);
            label_CurrentTime.Name = "label_CurrentTime";
            label_CurrentTime.Size = new Size(39, 15);
            label_CurrentTime.TabIndex = 19;
            label_CurrentTime.Text = "label2";
            label_CurrentTime.Click += label_CurrentTime_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(38, 24);
            label2.Name = "label2";
            label2.Size = new Size(66, 15);
            label2.TabIndex = 20;
            label2.Text = "현재 시간 :";
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(365, 656);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(264, 112);
            buttonStop.TabIndex = 21;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += buttonStop_Click;
            // 
            // checkBox_Process
            // 
            checkBox_Process.AutoSize = true;
            checkBox_Process.Location = new Point(426, 43);
            checkBox_Process.Name = "checkBox_Process";
            checkBox_Process.Size = new Size(192, 19);
            checkBox_Process.TabIndex = 22;
            checkBox_Process.Text = "중간 Fail 상관없이 끝까지 진행";
            checkBox_Process.UseVisualStyleBackColor = true;
            // 
            // buttonClear
            // 
            buttonClear.Location = new Point(1370, 1);
            buttonClear.Name = "buttonClear";
            buttonClear.Size = new Size(142, 28);
            buttonClear.TabIndex = 23;
            buttonClear.Text = "Clear";
            buttonClear.UseVisualStyleBackColor = true;
            buttonClear.Click += buttonClear_Click;
            // 
            // button_Copy
            // 
            button_Copy.Location = new Point(1251, 1);
            button_Copy.Name = "button_Copy";
            button_Copy.Size = new Size(113, 27);
            button_Copy.TabIndex = 24;
            button_Copy.Text = "Copy";
            button_Copy.UseVisualStyleBackColor = true;
            button_Copy.Click += button_Copy_Click;
            //
            // button_CopyAll
            //
            button_CopyAll.Location = new Point(1132, 1);
            button_CopyAll.Name = "button_CopyAll";
            button_CopyAll.Size = new Size(113, 27);
            button_CopyAll.TabIndex = 30;
            button_CopyAll.Text = "Copy All";
            button_CopyAll.UseVisualStyleBackColor = true;
            button_CopyAll.Click += button_CopyAll_Click;
            //
            // label_Serial
            // 
            label_Serial.AutoSize = true;
            label_Serial.Location = new Point(639, 119);
            label_Serial.Name = "label_Serial";
            label_Serial.Size = new Size(63, 15);
            label_Serial.TabIndex = 25;
            label_Serial.Text = "SerialNo : ";
            // 
            // label_mac
            // 
            label_mac.AutoSize = true;
            label_mac.Location = new Point(639, 144);
            label_mac.Name = "label_mac";
            label_mac.Size = new Size(37, 15);
            label_mac.TabIndex = 26;
            label_mac.Text = "Mac :";
            // 
            // button_expand
            // 
            button_expand.Location = new Point(651, 33);
            button_expand.Name = "button_expand";
            button_expand.Size = new Size(145, 26);   // 46→26: 아래 "Test : 프로젝트명" 라벨(y=63)과 겹침 방지 (4슬롯 이름 길어짐)
            button_expand.TabIndex = 27;
            button_expand.Text = "Log 표시/끄기";
            button_expand.UseVisualStyleBackColor = true;
            button_expand.Click += button_expand_Click;
            // 
            // label_inspector
            // 
            label_inspector.AutoSize = true;
            label_inspector.Location = new Point(639, 94);
            label_inspector.Name = "label_inspector";
            label_inspector.Size = new Size(50, 15);
            label_inspector.TabIndex = 28;
            label_inspector.Text = "검사자 :";
            // 
            // button_ChangeInspector
            // 
            button_ChangeInspector.Location = new Point(651, 665);
            button_ChangeInspector.Name = "button_ChangeInspector";
            button_ChangeInspector.Size = new Size(145, 29);
            button_ChangeInspector.TabIndex = 29;
            button_ChangeInspector.Text = "검사자 변경";

            // 최종 PASS 시리얼 표시 label (검사자 변경 버튼 아래)
            label_passSaved = new Label();
            label_passSaved.AutoSize = true;
            label_passSaved.Location = new Point(639, 645);
            label_passSaved.Font = new Font("Malgun Gothic", 9, FontStyle.Regular);
            label_passSaved.ForeColor = Color.Black;
            label_passSaved.Text = "";
            // 우클릭 메뉴: 결과 파일 / 폴더 열기
            var passMenu = new ContextMenuStrip();
            passMenu.Opening += (s, ev) =>
            {
                string sn = label_passSaved.Text.Replace("PASS SAVED: ", "").Trim();
                if (passMenu.Items.Count > 0)
                    passMenu.Items[0].Text = string.IsNullOrEmpty(sn) ? "결과 파일" : $"결과 파일: {sn}.json";
            };
            passMenu.Items.Add("결과 파일", null, (s, ev) =>
            {
                string sn = label_passSaved.Text.Replace("PASS SAVED: ", "").Trim();
                if (string.IsNullOrEmpty(sn)) return;
                string projFolder = "";
                try { projFolder = ((string)workspace.projects[0].name).Replace(" ", ""); } catch { }
                if (string.IsNullOrWhiteSpace(projFolder)) projFolder = "unknown";
                string dateDir = DateTime.Now.ToString("yyyy-MM-dd");
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Result", projFolder, dateDir, $"{sn}.json");
                if (File.Exists(filePath))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
                else
                    MessageBox.Show($"파일을 찾을 수 없습니다.\n{filePath}", "알림");
            });
            passMenu.Items.Add("폴더 열기", null, (s, ev) =>
            {
                string projFolder = "";
                try { projFolder = ((string)workspace.projects[0].name).Replace(" ", ""); } catch { }
                if (string.IsNullOrWhiteSpace(projFolder)) projFolder = "unknown";
                string dateDir = DateTime.Now.ToString("yyyy-MM-dd");
                string dirPath = Path.Combine(Directory.GetCurrentDirectory(), "Result", projFolder, dateDir);
                if (Directory.Exists(dirPath))
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dirPath) { UseShellExecute = true });
                else
                    MessageBox.Show($"폴더를 찾을 수 없습니다.\n{dirPath}", "알림");
            });
            label_passSaved.ContextMenuStrip = passMenu;
            this.Controls.Add(label_passSaved);
            button_ChangeInspector.UseVisualStyleBackColor = true;
            button_ChangeInspector.Click += button_ChangeInspector_Click;

            // button_ChangePort (포트 변경)
            var button_ChangePort = new Button();
            button_ChangePort.Location = new Point(651, 697);
            button_ChangePort.Size = new Size(145, 29);
            button_ChangePort.Text = "포트 변경";
            button_ChangePort.UseVisualStyleBackColor = true;
            button_ChangePort.Click += (s, e) => ShowPortChangeDialog();
            this.Controls.Add(button_ChangePort);
            //
            // button_OpenWorkspace
            // 
            button_OpenWorkspace.Location = new Point(88, 49);
            button_OpenWorkspace.Name = "button_OpenWorkspace";
            button_OpenWorkspace.Size = new Size(235, 37);
            button_OpenWorkspace.TabIndex = 30;
            button_OpenWorkspace.Text = "Open Workspace";
            button_OpenWorkspace.UseVisualStyleBackColor = true;
            button_OpenWorkspace.Click += button_OpenWorkspace_Click;

            // v0.6.9: 우클릭 컨텍스트 메뉴 — 현재 워크스페이스 재로드
            var openWsContextMenu = new ContextMenuStrip();
            var reloadMenuItem = new ToolStripMenuItem("🔄 현재 워크스페이스 재로드");
            reloadMenuItem.Click += (s, e) => ReloadCurrentWorkspace();
            openWsContextMenu.Items.Add(reloadMenuItem);
            // 메뉴가 열리기 직전 — 현재 경로가 없으면 disable
            openWsContextMenu.Opening += (s, e) =>
            {
                reloadMenuItem.Enabled = !string.IsNullOrEmpty(_currentWorkspacePath);
                if (!reloadMenuItem.Enabled)
                    reloadMenuItem.Text = "🔄 재로드 (워크스페이스 미로드)";
                else
                    reloadMenuItem.Text = $"🔄 재로드 ({Path.GetFileName(_currentWorkspacePath)})";
            };
            button_OpenWorkspace.ContextMenuStrip = openWsContextMenu;
            // 
            // MainForm
            // 
            ClientSize = new Size(1525, 780);
            Controls.Add(button_OpenWorkspace);
            Controls.Add(button_ChangeInspector);
            Controls.Add(label_inspector);
            Controls.Add(button_expand);
            Controls.Add(label_mac);
            Controls.Add(label_Serial);
            Controls.Add(button_CopyAll);
            Controls.Add(button_Copy);
            Controls.Add(buttonClear);
            Controls.Add(checkBox_Process);
            Controls.Add(buttonStop);
            Controls.Add(label2);
            Controls.Add(label_CurrentTime);
            Controls.Add(label_cycle);
            Controls.Add(label_CurrentTest);
            Controls.Add(label1);
            Controls.Add(dataGridView1);
            Controls.Add(listBox_Log);
            Controls.Add(button_Stop);
            Controls.Add(button_start);
            //Name = "MainForm";
            Text = "CanTops Flexfab v1.0";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Button button_start;
        private ListBox listBox_Log;
        private Button button_Stop;

        // ===== INI 경로 =====
        private string _configDir;
        private string _configPath;
        // 섹션/키 대소문자 구분 없이 처리

        private List<Dictionary<string, string>> _loadedLibsForLog = new(); // 폼 필드 (클래스 멤버)

        private Dictionary<string, Dictionary<string, string>> LoadIniUtf8(string path)
        {
            var map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return map;

            // BOM 감지 활성화: UTF-8/UTF-16 모두 안전
            using var sr = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string? line;
            string section = "";

            while ((line = sr.ReadLine()) != null)
            {
                var t = line.Trim();
                if (t.Length == 0 || t.StartsWith(";") || t.StartsWith("#")) continue;

                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    section = t.Substring(1, t.Length - 2).Trim();
                    if (!map.ContainsKey(section))
                        map[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }

                int eq = t.IndexOf('=');
                if (eq < 0) continue;

                var key = t.Substring(0, eq).Trim();
                var val = t.Substring(eq + 1).Trim();

                if (!map.ContainsKey(section))
                    map[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                map[section][key] = val;
            }

            return map;
        }

        private void SaveIniUtf8(string path, Dictionary<string, Dictionary<string, string>> map)
        {
            var sb = new StringBuilder();
            sb.AppendLine("; FlexFab config");

            foreach (var sec in map)
            {
                sb.AppendLine($"[{sec.Key}]");
                foreach (var kv in sec.Value)
                    sb.AppendLine($"{kv.Key}={kv.Value}");
                sb.AppendLine();
            }

            var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            File.WriteAllText(path, sb.ToString(), utf8Bom);
        }

        // 키 읽기/쓰기 편의 함수
        private string IniRead(string section, string key, string defaultValue = "")
        {
            var ini = LoadIniUtf8(_configPath);
            if (ini.TryGetValue(section, out var sec) && sec.TryGetValue(key, out var val))
                return val;
            return defaultValue;
        }

        private void IniWrite(string section, string key, string value)
        {
            var ini = LoadIniUtf8(_configPath);
            if (!ini.ContainsKey(section))
                ini[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            ini[section][key] = value ?? "";
            SaveIniUtf8(_configPath, ini);
        }

        // ===== 초기화/로드/저장 =====
        private void InitConfig()
        {
            _configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
            Directory.CreateDirectory(_configDir);
            _configPath = Path.Combine(_configDir, "config.ini");

            if (!File.Exists(_configPath))
            {
                var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                var template = new StringBuilder();
                template.AppendLine("; FlexFab config");
                template.AppendLine("[General]");
                template.AppendLine("Inspector=");
                template.AppendLine("LastWorkspace=");        // 마지막 workspace 파일명
                template.AppendLine("MongoDBUpload=True");    // ★ 기본값
                template.AppendLine("MacAddressWrite=True");
                File.WriteAllText(_configPath, template.ToString(), utf8Bom);
            }
        }

        private void LoadInspectorFromIni()
        {
            string name = IniRead("General", "Inspector", "").Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "미등록";
            label_inspector.Text = $"검사자 : {name}";
        }

        private void SaveInspectorToIni(string name)
        {
            string clean = (name ?? "").Trim();
            IniWrite("General", "Inspector", clean);
            label_inspector.Text = $"검사자 : {clean}";
        }



        public dynamic MainLoadWorkspace(string workspace_filename, Action<string> logAction)
        {
            dynamic workspace = null;

            var libsForSave = new JArray();

            try
            {
                if (!File.Exists(workspace_filename))
                {
                    logAction($"Error: '{workspace_filename}' 파일이 존재하지 않습니다.");
                    return null;
                }

                string jsonString = File.ReadAllText(workspace_filename);
                workspace = JsonConvert.DeserializeObject<ExpandoObject>(jsonString);

                // <<--- START OF ADDED DEBUG LOGGING --->>
                if (LogConfig.IsTest) logAction($"[DEBUG] Loaded workspace content for '{workspace_filename}':");
                if (LogConfig.IsTest) logAction(JsonConvert.SerializeObject(workspace, Newtonsoft.Json.Formatting.Indented));
                // <<--- END OF ADDED DEBUG LOGGING --->>

                foreach (var iter in workspace.libraries)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(iter.filename);
                        var type = assembly.GetType(iter.classname);
                        if (type == null)
                            throw new Exception($"클래스 '{iter.classname}'를 찾을 수 없습니다.");

                        iter.classtype = type;
                        iter.instance = Activator.CreateInstance(type);

                        if (iter.instance is IModule module)
                        {
                            var config = (IDictionary<string, object>)iter.config;
                            config["log_action"] = logAction;
                            module.SetConfig(config);
                        }

                        // ==== 라이브러리 메타 수집 (classname 제외, id/filename/ver만) ====
                        string ver = "unknown";
                        try
                        {
                            var info = (iter.instance as IModule)?.GetInfo();
                            if (info != null)
                            {
                                if (info.TryGetValue("ver", out var vv) && vv != null)
                                    ver = vv.ToString();
                                else if (info.TryGetValue("var", out var vv2) && vv2 != null) // 과거 키 호환
                                    ver = vv2.ToString();
                            }
                        }
                        catch { /* ignore */ }

                        libsForSave.Add(new JObject
                        {
                            ["id"] = (string)iter.id,
                            ["filename"] = (string)iter.filename,
                            ["ver"] = ver
                        });
                    }
                    catch (Exception ex)
                    {
                        logAction($"Library '{iter.id}' 로드 중 오류 발생: {ex.Message}");
                    }

                    _libSnapshot = libsForSave;  // SaveLog에서 이 스냅샷을 그대로 사용
                }

                // 프로세스 목록 가져오기
                var testList = new List<string>();
                var skipList = new List<bool>();

                foreach (var project in workspace.projects)
                {
                    foreach (var proc in project.procs)
                    {
                        testList.Add(proc.name); // "테스트 항목" 열에 들어갈 데이터
                        bool isSkip = false;
                        try { isSkip = (bool)proc.skip; } catch { }
                        skipList.Add(isSkip);
                    }
                }

                // DataGridView의 행 수를 testList의 항목 수와 비교하여 적절히 설정
                int rowCount = Math.Max(11, testList.Count); // 11줄이 기본, 항목 수가 많으면 그만큼 더 추가

                // DataGridView에서 행을 지우고, 필요한 만큼 행을 추가
                dataGridView1.Rows.Clear();
                ApplySlotLayout(workspace); // 4슬롯: 방금 로드한 객체(지역변수)로 slot_layout 읽고 결과 열 1→4 구성 (2슬롯이면 원복)

                // 필요한 만큼 행 추가
                for (int i = 0; i < rowCount; i++)
                {
                    if (i < testList.Count)
                    {
                        dataGridView1.Rows.Add((i + 1).ToString(), testList[i], ""); // 테스트 항목 추가
                        // JSON skip: true → 빨간색으로 표시 (기존 토글과 동일 동작)
                        if (i < skipList.Count && skipList[i])
                            dataGridView1.Rows[i].Cells[0].Style.BackColor = Color.Red;
                    }
                    else
                    {
                        dataGridView1.Rows.Add((i + 1).ToString(), "", ""); // 남은 빈 항목 추가
                    }
                }

                return workspace;
            }
            catch (Exception ex)
            {
                logAction($"워크스페이스 로드 중 오류 발생: {ex.Message}");
                return null;
            }
        }
        private string ExtractLibraryVersion(object instance, Assembly asm)
        {
            try
            {
                if (instance is IModule m)
                {
                    var info = m.GetInfo();
                    if (info != null)
                    {
                        if (TryGetStr(info, "ver", out var v)) return v; // ← 핵심: ver
                        if (TryGetStr(info, "version", out var v2)) return v2;
                    }
                }
            }
            catch { /* ignore */ }

            // 폴백: 파일/제품 버전 → AssemblyName 버전
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(asm.Location);
                if (!string.IsNullOrWhiteSpace(fvi.ProductVersion)) return fvi.ProductVersion;
                if (!string.IsNullOrWhiteSpace(fvi.FileVersion)) return fvi.FileVersion;
            }
            catch { /* ignore */ }

            try
            {
                var v = asm.GetName()?.Version?.ToString();
                if (!string.IsNullOrWhiteSpace(v)) return v;
            }
            catch { /* ignore */ }

            return "unknown";
        }

        private bool TryGetStr(IDictionary<string, object?> dict, string key, out string value)
        {
            if (dict.TryGetValue(key, out var obj) && obj != null)
            {
                value = obj.ToString() ?? "";
                return !string.IsNullOrWhiteSpace(value);
            }
            value = "";
            return false;
        }

        public void UpdateDataGridView(int rowIndex,
                               string resultText,
                               Color bgColor,
                               Color? fgColor = null)   // ← 선택적 인자
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string, Color, Color?>(UpdateDataGridView),
                       rowIndex, resultText, bgColor, fgColor);
                return;
            }

            var numberCell = dataGridView1.Rows[rowIndex].Cells[0]; // 번호(1열)
            // 4슬롯: 결과 열이 X1..Y2(Cells[2..5]) → 행 단위 갱신은 4칸 모두 (공통 항목·초기화·Skip). 2슬롯: Cells[2]만
            int lastCol = (_slot4 && dataGridView1.Columns.Contains("Slot_X2")) ? 5 : 2;
            for (int c = 2; c <= lastCol; c++)
            {
                var resultCell = dataGridView1.Rows[rowIndex].Cells[c];

                // 빨간색(불량) 행이면 Skip 처리
                if (numberCell.Style.BackColor == Color.Red)
                {
                    resultCell.Value = "Skip";
                    resultCell.Style.BackColor = Color.LightBlue;
                    resultCell.Style.ForeColor = Color.Black;
                    continue;
                }

                // 정상 처리
                resultCell.Value = resultText;
                resultCell.Style.BackColor = bgColor;
                resultCell.Style.ForeColor = fgColor ?? Color.White;   // 값 없으면 흰색
            }
        }








        public void Log(string message)
        {
            string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");  // 현재 시간 구하기
            string logMessage = $"[{currentTime}] {message}";  // 시간과 메시지 결합

            if (listBox_Log.InvokeRequired)
            {
                listBox_Log.Invoke(new Action(() => AddLogMessage(logMessage)));
            }
            else
            {
                AddLogMessage(logMessage);
            }
        }

        private void AddLogMessage(string message)
        {
            try
            {
                listBox_Log.Items.Add(message);
                listBox_Log.TopIndex = listBox_Log.Items.Count - 1; // 자동 스크롤

                // 로그 파일 저장 (Log/{프로젝트명}/YYYY-MM-DD.log)
                try
                {
                    string logBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                    string logDir = string.IsNullOrEmpty(_logProjFolder) ? logBase : Path.Combine(logBase, _logProjFolder);
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                    string logFile = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                    File.AppendAllText(logFile, message + Environment.NewLine, new UTF8Encoding(false));
                }
                catch { /* 파일 저장 실패해도 화면 로그는 유지 */ }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로그 추가 중 예외 발생: {ex.Message}");
            }
        }

        public void SaveLog(string serialNumber, string macAddress, Dictionary<string, object> _ /*호환*/)
        {
            // 1) retmsg 라인 수집
            List<string> retLines;
            if (this.InvokeRequired)
                retLines = (List<string>)this.Invoke(new Func<List<string>>(CollectReturnLinesFromGrid));
            else
                retLines = CollectReturnLinesFromGrid();

            // 2) 파일 경로: Result/{프로젝트명(공백제거)}/{날짜}/{시리얼}.json
            string projFolder = "";
            try { projFolder = ((string)workspace.projects[0].name).Replace(" ", ""); } catch { }
            if (string.IsNullOrWhiteSpace(projFolder)) projFolder = "unknown";

            string dateDir = DateTime.Now.ToString("yyyy-MM-dd");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Result", projFolder, dateDir);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string safeName = string.IsNullOrWhiteSpace(serialNumber)
                ? $"log_{DateTime.Now:yyyyMMddHHmmss}"
                : serialNumber;
            string filePath = Path.Combine(dir, $"{safeName}.json");

            // 3) Inspector
            string inspectorName = "";
            try
            {
                var raw = label_inspector?.Text ?? ""; // "검사자 : 홍길동"
                int idx = raw.IndexOf(':');
                inspectorName = (idx >= 0) ? raw.Substring(idx + 1).Trim() : raw.Trim();
            }
            catch { /* ignore */ }

            // 3-1) Project Name (Proj) – workspace에서 가져오기
            string projName = "";
            try
            {
                if (workspace != null)
                {
                    // active_project 우선
                    string activeId = null;
                    try { activeId = workspace.active_project as string; } catch { }

                    if (!string.IsNullOrEmpty(activeId))
                    {
                        foreach (var prj in workspace.projects)
                        {
                            if (prj.id == activeId)
                            {
                                projName = prj.name;
                                break;
                            }
                        }
                    }

                    // fallback: 첫 번째 프로젝트 이름
                    if (string.IsNullOrEmpty(projName))
                    {
                        var first = workspace.projects[0];
                        projName = first.name;
                    }
                }
            }
            catch
            {
                projName = "";
            }

            // 3.5) libraries를 문자열 배열 형태로 변환: "{filename},{ver}"
            var libLines = new JArray();
            if (_libSnapshot != null && _libSnapshot.Count > 0)
            {
                // filename 기준으로 중복 제거
                var grouped = _libSnapshot
                    .OfType<JObject>()
                    .GroupBy(o => (string)o["filename"]);

                foreach (var g in grouped)
                {
                    string fn = g.Key ?? "";
                    string ver = g.Select(o => (string)o["ver"]).FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";
                    libLines.Add($"{{{fn}}},{{{ver}}}");
                }
            }

            // 4) result: JSON 텍스트는 객체로, 나머지는 문자열로 저장
            var resultArr = new JArray();
            foreach (var line in retLines ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var t = line.Trim();

                if (t.StartsWith("{") && t.EndsWith("}"))
                {
                    try
                    {
                        resultArr.Add(JObject.Parse(t));   // JSON 객체로 저장
                    }
                    catch
                    {
                        resultArr.Add(t);                  // 파싱 실패 시 문자열로
                    }
                }
                else
                {
                    resultArr.Add(t);                      // 예전 포맷은 문자열로
                }
            }

            // 5) JSON 구성 (간소화 키 + Proj 추가)
            string projectType = projName.Contains("모션", StringComparison.OrdinalIgnoreCase) || projName.Contains("motion", StringComparison.OrdinalIgnoreCase) ? "motion" : "board";
            string commType = projName.Contains("485") ? "RS-485" : "RS-232";
            var root = new JObject
            {
                ["time"] = DateTime.UtcNow,                          // DateTime 타입으로 저장
                ["Ins"] = inspectorName,
                ["Proj"] = projName,
                ["project_type"] = projectType,
                ["comm_type"] = commType,
                ["lib"] = libLines,
                ["sn"] = serialNumber,
                ["mac"] = string.Equals(macAddress, "Not Use",
                                         StringComparison.OrdinalIgnoreCase)
                            ? null                                   // Not Use면 null로
                            : macAddress,
                ["result"] = resultArr
            };

            // 6) 저장
            var json = root.ToString(Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            // 7) MongoDB 업로드(설정값이 True일 때만 시도; 실패 시 로컬만 유지)
            bool wantMongo = ReadBoolFromIni("General", "MongoDBUpload", defaultValue: true);
            if (wantMongo)
            {
                bool uploaded = TryUploadToMongo(json, safeName, Log);
                if (!uploaded) Log("Mongo 업로드 실패 → 로컬 JSON만 유지합니다.");
            }
            else
            {
                Log("MongoDBUpload=False → 로컬 JSON만 저장합니다.");
            }
        }


        private bool ReadBoolFromIni(string section, string key, bool defaultValue = true)
        {
            try
            {
                if (string.IsNullOrEmpty(_configPath) || !File.Exists(_configPath))
                    return defaultValue;

                var lines = File.ReadAllLines(_configPath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                bool inSection = false;

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        var sec = line.Substring(1, line.Length - 2).Trim();
                        inSection = string.Equals(sec, section, StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (!inSection) continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    var k = line.Substring(0, eq).Trim();
                    var v = line.Substring(eq + 1).Trim();

                    if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        return ParseBool(v, defaultValue);
                }
            }
            catch { /* ignore and fallback */ }

            return defaultValue;
        }

        private bool ParseBool(string? text, bool defaultValue = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return defaultValue;

            switch (text.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "y":
                case "on":
                    return true;

                case "0":
                case "false":
                case "no":
                case "n":
                case "off":
                    return false;

                default:
                    return defaultValue;
            }
        }
        // DataGridView(3열 Tag)에 쌓아둔 retmsg 라인을 수집
        private List<string> CollectReturnLinesFromGrid()
        {
            var lines = new List<string>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var tagObj = row.Cells[2].Tag; // 결과(3열)에 보관해둔 retmsg
                if (tagObj == null) continue;

                var tagText = tagObj.ToString();
                if (string.IsNullOrWhiteSpace(tagText)) continue;

                // 여러 줄일 수 있으니 분리
                var split = tagText.Replace("\r\n", "\n").Split('\n');
                foreach (var raw in split)
                {
                    var trimmed = raw.Trim();
                    if (trimmed.Length > 0)
                        lines.Add(trimmed);
                }
            }
            return lines;
        }

        // X축/Y축 retmsg 분리 수집 (isYAxis=true면 Y축 행만, false면 X축 행만)
        private static void CollectRetmsgByAxis(MainForm form, bool isYAxis, JArray target)
        {
            foreach (DataGridViewRow row in form.dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                var name = row.Cells[1].Value?.ToString() ?? "";
                bool isY = name.StartsWith("#0-Y") || name.StartsWith("#7 ") || name.StartsWith("#8 ") ||
                           name.StartsWith("#9 ") || name.StartsWith("#10 ") || name.StartsWith("#11 ") || name.StartsWith("#12 ");
                if (isY != isYAxis) continue;

                var tagObj = row.Cells[2].Tag;
                if (tagObj == null) continue;
                var tagText = tagObj.ToString();
                if (string.IsNullOrWhiteSpace(tagText)) continue;

                foreach (var raw in tagText.Replace("\r\n", "\n").Split('\n'))
                {
                    var trimmed = raw.Trim();
                    if (trimmed.Length == 0) continue;
                    if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                    {
                        try { target.Add(JObject.Parse(trimmed)); } catch { target.Add(trimmed); }
                    }
                    else
                    {
                        target.Add(trimmed);
                    }
                }
            }
        }

        // 합산 저장용 (DataGridView 대신 retmsg 직접 전달)
        public void SaveLogDirect(string sn, JArray resultArr, Action<string> logAction, string slotLane = null)
        {
            string projFolder = "";
            try { projFolder = ((string)workspace.projects[0].name).Replace(" ", ""); } catch { }
            if (string.IsNullOrWhiteSpace(projFolder)) projFolder = "unknown";

            string dateDir = DateTime.Now.ToString("yyyy-MM-dd");
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Result", projFolder, dateDir);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filePath = Path.Combine(dir, $"{sn}.json");

            string inspectorName = "";
            try
            {
                var raw = label_inspector?.Text ?? "";
                int idx = raw.IndexOf(':');
                inspectorName = (idx >= 0) ? raw.Substring(idx + 1).Trim() : raw.Trim();
            }
            catch { }

            string projName = "";
            try { projName = (string)workspace.projects[0].name; } catch { }

            var libLines = new JArray();
            if (_libSnapshot != null)
            {
                foreach (var g in _libSnapshot.OfType<JObject>().GroupBy(o => (string)o["filename"]))
                {
                    string fn = g.Key ?? "";
                    string ver = g.Select(o => (string)o["ver"]).FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? "";
                    libLines.Add($"{{{fn}}},{{{ver}}}");
                }
            }

            string projectType2 = projName.Contains("모션", StringComparison.OrdinalIgnoreCase) || projName.Contains("motion", StringComparison.OrdinalIgnoreCase) ? "motion" : "board";
            string commType2 = projName.Contains("485") ? "RS-485" : "RS-232";
            var root = new JObject
            {
                ["time"] = DateTime.UtcNow,
                ["Ins"] = inspectorName,
                ["Proj"] = projName,
                ["project_type"] = projectType2,
                ["comm_type"] = commType2,
                ["lib"] = libLines,
                ["sn"] = sn,
                ["mac"] = "Not Use",
                ["result"] = resultArr
            };
            if (!string.IsNullOrEmpty(slotLane)) root["slot"] = slotLane;   // 4슬롯: 검사 레인 (예: "X1->Y1") — 슬롯 편중 불량 추적용

            File.WriteAllText(filePath, root.ToString(), new System.Text.UTF8Encoding(false));
            logAction($"결과 파일 저장: {filePath}");

            // MongoDB 업로드
            TryUploadToMongo(root.ToString(), sn, logAction);
        }

        // MongoDB 업로드 헬퍼
        private bool TryUploadToMongo(string json, string serialNumber, Action<string> log)
        {
            // ★ 모든 업로드 경로 공통 게이트 — MongoDBUpload=False면 DB 연결 시도 자체를 건너뜀.
            //    DB 미연결 환경(제조팀 랜선 없음)의 연결 타임아웃(4~5초) 제거. 보드(SaveLog)/모션(SaveLogDirect) 일괄 제어.
            if (!ReadBoolFromIni("General", "MongoDBUpload", defaultValue: true))
            {
                log("MongoDBUpload=False → 로컬 JSON만 저장합니다.");
                return false;
            }
            try
            {
                string mongoUri;
                try { mongoUri = workspace.mongodb_uri; }
                catch { mongoUri = "mongodb://192.168.10.10:45029/?serverSelectionTimeoutMS=4000&connectTimeoutMS=4000&socketTimeoutMS=4000&directConnection=true"; }

                const string dbName = "ctsm";
                const string collName = "product";

                var client = new MongoClient(mongoUri);

                // 연결 확인
                client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                if (LogConfig.IsTest) log("MongoDB ping OK.");

                // JSON → BSON
                var doc = BsonSerializer.Deserialize<BsonDocument>(json);

                // time을 BSON Date로 보정 (파일에서는 문자열, DB에서는 Date 타입)
                if (doc.Contains("time"))
                {
                    var t = doc["time"];
                    if (t.IsString)
                    {
                        if (DateTime.TryParse(t.AsString, null,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                        {
                            doc["time"] = new BsonDateTime(dt);
                        }
                    }
                }

                // _id는 설정하지 않음 → 드라이버가 ObjectId 자동 생성
                if (doc.Contains("_id"))
                {
                    // JSON에 실수로 _id 문자열이 들어왔다면 제거해서 ObjectId 자동생성 유도
                    if (!doc["_id"].IsObjectId) doc.Remove("_id");
                }

                var db = client.GetDatabase(dbName);
                var coll = db.GetCollection<BsonDocument>(collName).WithWriteConcern(WriteConcern.Acknowledged);

                coll.InsertOne(doc);

                // 드라이버가 삽입 시 _id(ObjectId)를 doc에 채워줌
                var insertedId = doc.Contains("_id") ? doc["_id"].ToString() : "(no _id)";
                log($"MongoDB 업로드 성공: {dbName}.{collName}  _id={insertedId}");
                return true;
            }
            catch (Exception ex)
            {
                log($"MongoDB 업로드 실패: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }



        // 포트 변경 다이얼로그
        private void ShowPortChangeDialog()
        {
            if (workspace == null || workspace.libraries == null) { MessageBox.Show("워크스페이스가 로드되지 않았습니다."); return; }

            // 현재 포트 + FTDI 시리얼 읽기 (serial은 FTDI 자동채움용 — 없으면 null, 통신 로직 영향 없음)
            var ports = new List<(string id, string port, string? serial)>();
            foreach (var lib in workspace.libraries)
            {
                try
                {
                    if ((string)lib.classname == "Cantops.FlexFab.Uart" && lib.config?.port != null)
                    {
                        string? serial = null;
                        try { serial = (string?)lib.config?.serial; } catch { }
                        ports.Add(((string)lib.id, (string)lib.config.port, serial));
                    }
                }
                catch { }
            }

            if (ports.Count == 0) { MessageBox.Show("UART 포트가 없습니다."); return; }

            // 2026-06-11: 현재 PC에 꽂힌 포트 스캔 (장치관리자 왕복 제거용, read-only)
            var scanned = PortDeviceScanner.Scan();
            Func<string, string> describeCom = (com) =>
            {
                var d = scanned.FirstOrDefault(p => string.Equals(p.Com, (com ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
                return d != null ? "← " + d.Describe() : "← (미인식)";
            };

            using var dlg = new Form();
            dlg.Text = "COM 포트 변경";
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            // ── 상단: 현재 꽂힌 포트 목록 (칩/시리얼/위치) ──────────────
            var lblScanTitle = new Label { Text = "현재 꽂힌 포트", Location = new Point(20, 12), AutoSize = true, Font = new Font("맑은 고딕", 9, FontStyle.Bold) };
            int scanBoxH = Math.Max(44, scanned.Count * 18 + 8);
            var scanBox = new TextBox
            {
                Location = new Point(20, 34),
                Width = 600,
                Height = scanBoxH,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                Text = scanned.Count > 0
                    ? string.Join("\r\n", scanned.Select(p => p.Describe()))
                    : "(스캔된 포트 없음)"
            };
            dlg.Controls.Add(lblScanTitle);
            dlg.Controls.Add(scanBox);

            int rowsTop = scanBox.Bottom + 14;

            // v0.6.9: COM 포트 번호 +/- 조정 헬퍼 ("COM13" → "COM14" 등)
            Action<TextBox, int> adjustPort = (tb, delta) =>
            {
                string val = (tb.Text ?? "").Trim().ToUpper();
                string numPart = val.StartsWith("COM") ? val.Substring(3) : val;
                if (int.TryParse(numPart, out int n))
                {
                    n = Math.Max(1, n + delta);
                    tb.Text = "COM" + n;
                }
            };

            var textBoxes = new List<(string id, TextBox txt)>();
            int tabIdx = 0;
            int portDx = _slot4 ? 50 : 0;   // 4슬롯: 라벨 "X1슬롯 (UART_232)"이 길어 입력칸 이하 오른쪽으로 이동
            for (int i = 0; i < ports.Count; i++)
            {
                int rowY = rowsTop + i * 40;
                // 2026-06-11: 표시 라벨만 X 명시(uart_485 → UART_485_X). 실제 id는 불변(통신/저장은 ports[i].id 사용)
                string dispName = string.Equals(ports[i].id, "uart_485", StringComparison.OrdinalIgnoreCase) ? "uart_485_x" : ports[i].id;
                string lblText = dispName.ToUpper();
                float lblFont = 10;
                // 4슬롯: 지그 표기(X1슬롯 등)를 앞에, 라이브러리 id를 괄호로 (id·통신·저장은 그대로 ports[i].id)
                if (_slot4)
                {
                    int si = Array.FindIndex(SLOT_NAMES, n => _slotUarts.TryGetValue(n, out var u) && string.Equals(u, ports[i].id, StringComparison.OrdinalIgnoreCase));
                    if (si >= 0) { lblText = $"{SLOT_LABELS[si]} ({ports[i].id.ToUpper()})"; lblFont = 9; }
                    else if (string.Equals(ports[i].id, "uart_gyro", StringComparison.OrdinalIgnoreCase)) { lblText = "경사계 (UART_GYRO)"; lblFont = 9; }
                }
                var lbl = new Label { Text = lblText, Location = new Point(20, rowY), AutoSize = true, Font = new Font("맑은 고딕", lblFont) };
                var txt = new TextBox { Text = ports[i].port, Location = new Point(160 + portDx, rowY - 3), Width = 90, Font = new Font("맑은 고딕", 10), TabIndex = tabIdx++ };
                var btnMinus = new Button { Text = "−", Location = new Point(258 + portDx, rowY - 5), Size = new Size(40, 28), Font = new Font("맑은 고딕", 11, FontStyle.Bold), TabIndex = tabIdx++ };
                var btnPlus  = new Button { Text = "+", Location = new Point(302 + portDx, rowY - 5), Size = new Size(40, 28), Font = new Font("맑은 고딕", 11, FontStyle.Bold), TabIndex = tabIdx++ };
                // 2026-06-11: 입력된 COM이 실제 어떤 장치인지 즉시 표시 (장치관리자 대조 불필요)
                var lblInfo = new Label { Text = describeCom(ports[i].port), Location = new Point(350 + portDx, rowY), AutoSize = false, Width = 280, Height = 24, Font = new Font("맑은 고딕", 9), TextAlign = ContentAlignment.MiddleLeft };

                // 클로저 캡처 안전 (for 루프 내부에 txt 변수가 매 반복마다 새로 선언됨)
                btnMinus.Click += (s, e) => adjustPort(txt, -1);
                btnPlus.Click  += (s, e) => adjustPort(txt, +1);
                txt.TextChanged += (s, e) => lblInfo.Text = describeCom(txt.Text);

                dlg.Controls.Add(lbl);
                dlg.Controls.Add(txt);
                dlg.Controls.Add(btnMinus);
                dlg.Controls.Add(btnPlus);
                dlg.Controls.Add(lblInfo);
                textBoxes.Add((ports[i].id, txt));
            }

            // id → FTDI 시리얼 맵 (serial 필드가 있는 라이브러리만)
            var serialMap = ports.Where(p => !string.IsNullOrWhiteSpace(p.serial))
                                  .ToDictionary(p => p.id, p => p.serial!, StringComparer.OrdinalIgnoreCase);

            int btnY = rowsTop + ports.Count * 40 + 15;

            // 2026-06-11: [자동(FTDI)] — serial 필드 가진 라이브러리를 FTDI 시리얼 기준으로 자동 채움 (드리프트/스왑 차단)
            var btnAuto = new Button { Text = "자동(FTDI)", Location = new Point(20, btnY), Width = 110, TabIndex = tabIdx++ };
            btnAuto.Enabled = serialMap.Count > 0;
            btnAuto.Click += (s, e) =>
            {
                int hit = 0; var miss = new List<string>();
                foreach (var (id, txt) in textBoxes)
                {
                    if (!serialMap.TryGetValue(id, out var ser)) continue;
                    var com = PortDeviceScanner.FindComByFtdiSerial(ser, scanned);
                    if (com != null) { txt.Text = com; hit++; }
                    else miss.Add($"{id}({ser})");
                }
                string msg = $"FTDI 자동채움: {hit}개 적용";
                if (miss.Count > 0) msg += $"\n미발견(연결 확인 필요): {string.Join(", ", miss)}";
                MessageBox.Show(msg, "자동채움", MessageBoxButtons.OK,
                    miss.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            };

            var btnOk = new Button { Text = "적용", DialogResult = DialogResult.OK, Location = new Point(380 + portDx, btnY), Width = 80, TabIndex = tabIdx++ };
            var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(470 + portDx, btnY), Width = 80, TabIndex = tabIdx++ };
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            dlg.ClientSize = new Size(640 + portDx, btnY + 45);
            dlg.Controls.AddRange(new Control[] { btnAuto, btnOk, btnCancel });

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            // workspace JSON에 반영 (IDictionary 우선, dynamic fallback, 에러 명시)
            bool changed = false;
            foreach (var (id, txt) in textBoxes)
            {
                string newPort = txt.Text.Trim().ToUpper();
                if (string.IsNullOrEmpty(newPort)) continue;
                foreach (var lib in workspace.libraries)
                {
                    try
                    {
                        // lib의 id 매칭 (IDictionary 우선, fallback dynamic)
                        string libId = null;
                        if (lib is IDictionary<string, object> libDict && libDict.TryGetValue("id", out var idObj))
                            libId = idObj as string;
                        else
                            try { libId = (string)((dynamic)lib).id; } catch { }
                        if (libId != id) continue;

                        // config.port 변경 (IDictionary 우선, fallback dynamic)
                        string curPort = null;
                        if (lib is IDictionary<string, object> lDict &&
                            lDict.TryGetValue("config", out var cfgObj) &&
                            cfgObj is IDictionary<string, object> cfgDict)
                        {
                            if (cfgDict.TryGetValue("port", out var portObj)) curPort = portObj as string;
                            if (curPort == newPort) continue;
                            cfgDict["port"] = newPort;
                            changed = true;
                            Log($"[INFO] 포트 변경: {id} {curPort ?? "null"} → {newPort}");
                        }
                        else
                        {
                            try
                            {
                                dynamic cfg = ((dynamic)lib).config;
                                curPort = (string)cfg.port;
                                if (curPort == newPort) continue;
                                cfg.port = newPort;
                                changed = true;
                                Log($"[INFO] 포트 변경: {id} {curPort ?? "null"} → {newPort}");
                            }
                            catch (Exception dex)
                            {
                                Log($"[ERROR] 포트 변경 실패 ({id}): dynamic 접근 불가 - {dex.Message}");
                            }
                        }
                    }
                    catch (Exception ex) { Log($"[ERROR] 포트 변경 예외 ({id}): {ex.GetType().Name} - {ex.Message}"); }
                }
            }

            // 2026-06-11: FTDI 시리얼 자동 동기화 준비 — 적용 시점 재스캔(가드2). serial 필드 있는 줄만(가드1, serialMap).
            //   COM이 안 바뀌어도 시리얼이 달라졌으면 저장이 돌도록 changed 트리거.
            var applyScan = PortDeviceScanner.Scan();
            foreach (var (id, txt) in textBoxes)
            {
                if (!serialMap.TryGetValue(id, out var oldSer)) continue;     // 가드1: serial 가진 줄만
                string np = txt.Text.Trim().ToUpper();
                var dev = applyScan.FirstOrDefault(p => string.Equals(p.Com, np, StringComparison.OrdinalIgnoreCase)
                                                        && p.Kind == PortChipKind.Ftdi && !string.IsNullOrWhiteSpace(p.Serial));
                if (dev != null && !string.Equals(oldSer, dev.Serial, StringComparison.OrdinalIgnoreCase))
                    changed = true;
            }

            // 저장: 원본 JSON 파일을 JObject로 재파싱 → port(+serial) 업데이트 → 쓰기
            // (in-memory workspace는 런타임 중 delegate/form 참조로 오염되어 직접 serialize 불가)
            bool saveOk = false;
            string saveMsg = "";
            if (changed)
            {
                try
                {
                    string wsPath = _currentWorkspacePath ?? "";
                    if (string.IsNullOrEmpty(wsPath))
                    {
                        saveMsg = "_currentWorkspacePath 비어있음 (config.ini의 LastWorkspace 확인 필요)";
                    }
                    else if (!File.Exists(wsPath))
                    {
                        saveMsg = $"파일 없음: {wsPath}";
                    }
                    else
                    {
                        var jo = JObject.Parse(File.ReadAllText(wsPath));
                        var libs = jo["libraries"] as JArray;
                        if (libs == null) throw new Exception("libraries 배열 없음");
                        int updates = 0;
                        foreach (var (id, txt) in textBoxes)
                        {
                            string newPort = txt.Text.Trim().ToUpper();
                            if (string.IsNullOrEmpty(newPort)) continue;
                            foreach (var lib in libs)
                            {
                                if ((string)lib["id"] != id) continue;
                                var cfg = lib["config"];
                                if (cfg?["port"] == null) continue;
                                bool libChanged = false;
                                string cur = (string)cfg["port"];
                                if (cur != newPort) { cfg["port"] = newPort; libChanged = true; }

                                // 2026-06-11: 가드1 — serial 필드 있는 줄만. 가드2 — 적용 스캔의 FTDI 시리얼로 동기화 + 로그
                                if (cfg["serial"] != null)
                                {
                                    var dev = applyScan.FirstOrDefault(p => string.Equals(p.Com, newPort, StringComparison.OrdinalIgnoreCase)
                                                                            && p.Kind == PortChipKind.Ftdi && !string.IsNullOrWhiteSpace(p.Serial));
                                    if (dev != null)
                                    {
                                        string oldSerial = (string)cfg["serial"];
                                        if (!string.Equals(oldSerial, dev.Serial, StringComparison.OrdinalIgnoreCase))
                                        {
                                            cfg["serial"] = dev.Serial;
                                            libChanged = true;
                                            Log($"[INFO] serial 갱신: {id} {oldSerial ?? "null"} → {dev.Serial}");
                                        }
                                    }
                                }
                                if (libChanged) updates++;
                            }
                        }
                        if (updates == 0)
                        {
                            saveMsg = "JSON에서 업데이트할 항목 없음";
                        }
                        else
                        {
                            File.WriteAllText(wsPath, jo.ToString(Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
                            saveOk = true;
                            saveMsg = $"{updates}개 항목 저장 완료: {wsPath}";
                        }
                    }
                }
                catch (Exception ex) { saveMsg = $"{ex.GetType().Name} - {ex.Message}"; }

                if (saveOk)
                {
                    Log($"[INFO] 포트 변경이 저장되었습니다 → {saveMsg}. 프로그램을 재시작해주세요.");
                    MessageBox.Show("포트가 변경되었습니다.\n프로그램을 재시작해주세요.", "포트 변경", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Log($"[ERROR] 포트 저장 실패: {saveMsg}");
                    MessageBox.Show($"포트 변경 저장 실패!\n\n{saveMsg}\n\n로그창에서 상세 확인", "포트 변경 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Log("[INFO] 포트 변경: 변경사항 없음");
                MessageBox.Show("변경사항 없음 (입력값이 현재 포트와 동일하거나 매칭되는 라이브러리 없음)", "포트 변경", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // 작업자 확인 팝업 (대형 폰트)
        private DialogResult ShowLargeConfirmDialog(string message)
        {
            using var dlg = new Form();
            dlg.Text = "작업자 확인";
            dlg.Size = new Size(508, 315);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            var lbl = new Label
            {
                Text = message,
                Font = new Font("맑은 고딕", 16),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 0),
                Size = new Size(493, 176)
            };

            var btnOk = new Button
            {
                Text = "확인",
                Font = new Font("맑은 고딕", 14),
                DialogResult = DialogResult.OK,
                Size = new Size(200, 55),
                Location = new Point(154, 187)
            };

            dlg.AcceptButton = btnOk;
            dlg.ControlBox = false; // X 버튼 비활성
            dlg.Controls.AddRange(new Control[] { lbl, btnOk });

            dlg.ShowDialog(this);
            return DialogResult.OK;
        }

        // 대형 폰트 Yes/No 팝업
        private DialogResult ShowLargeYesNoDialog(string message)
        {
            using var dlg = new Form();
            dlg.Text = "확인";
            dlg.Size = new Size(508, 315);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;
            dlg.ControlBox = false;

            var lbl = new Label
            {
                Text = message,
                Font = new Font("맑은 고딕", 16),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 0),
                Size = new Size(493, 176)
            };

            var btnYes = new Button
            {
                Text = "예",
                Font = new Font("맑은 고딕", 14),
                DialogResult = DialogResult.Yes,
                Size = new Size(150, 55),
                Location = new Point(80, 187)
            };

            var btnNo = new Button
            {
                Text = "아니오",
                Font = new Font("맑은 고딕", 14),
                DialogResult = DialogResult.No,
                Size = new Size(150, 55),
                Location = new Point(270, 187)
            };

            dlg.AcceptButton = btnYes;
            dlg.CancelButton = btnNo;
            dlg.Controls.AddRange(new Control[] { lbl, btnYes, btnNo });

            return dlg.ShowDialog(this);
        }

        // 밀어내기식 2개 시리얼 입력 다이얼로그
        // 프로젝트별 시리얼 파일명 (모션=serial.txt, 보드=serial_board.txt)
        private string GetSerialFileName()
        {
            try
            {
                string activeProj = (string)(workspace?.active_project ?? "");
                bool isMotion = activeProj.Contains("motion", StringComparison.OrdinalIgnoreCase);
                bool is485 = activeProj.Contains("485", StringComparison.OrdinalIgnoreCase);
                if (isMotion)
                    return is485 ? "serial_motion_485.txt" : "serial_motion_232.txt";
                else
                    return is485 ? "serial_board_485.txt" : "serial_board_232.txt";
            }
            catch { }
            return "serial_board.txt";
        }

        // 이전 X축 시리얼 기억 파일도 프로젝트별 분리 (한 폴더에서 4종 병행 시 시리얼 섞임 방지)
        private string GetLastSerialXFileName()
        {
            try
            {
                string activeProj = (string)(workspace?.active_project ?? "");
                bool isMotion = activeProj.Contains("motion", StringComparison.OrdinalIgnoreCase);
                bool is485 = activeProj.Contains("485", StringComparison.OrdinalIgnoreCase);
                if (isMotion)
                    return is485 ? "last_serial_x_motion_485.txt" : "last_serial_x_motion_232.txt";
                else
                    return is485 ? "last_serial_x_board_485.txt" : "last_serial_x_board_232.txt";
            }
            catch { }
            return "last_serial_x.txt";
        }

        private static string _lastSerialX = ""; // 이전 X축 시리얼 기억 (자동증가용)
        private static JObject _previousPendingData = null; // 이전 X축 pending 결과

        private bool ShowDualSerialDialog()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var serialPath = Path.Combine(baseDir, GetSerialFileName());

            // serial.txt에서 마지막 번호 읽기
            string lastSerial = "00001";
            try
            {
                if (File.Exists(serialPath))
                {
                    var lines = File.ReadAllLines(serialPath, new System.Text.UTF8Encoding(false));
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        var t = lines[i].Trim();
                        if (t.Length > 0) { lastSerial = t; break; }
                    }
                }
            }
            catch { }

            // 접두사 자동 붙이기 (serial.txt에 숫자만 있는 경우)
            string serialPrefix = "";
            try { if (workspace != null) serialPrefix = Convert.ToString(workspace.serial_prefix) ?? ""; } catch { }
            if (!string.IsNullOrEmpty(serialPrefix) && !lastSerial.Contains("-"))
                lastSerial = serialPrefix + lastSerial;

            // Y축 시리얼: 이전 X축 시리얼 (밀어내기)
            // 프로젝트별 파일에서 매번 새로 로드 (프로젝트 전환 시 다른 접두사 섞임 방지)
            {
                var lastXPath = Path.Combine(baseDir, GetLastSerialXFileName());
                _lastSerialX = "";
                try { if (File.Exists(lastXPath)) _lastSerialX = File.ReadAllText(lastXPath, new System.Text.UTF8Encoding(false)).Trim(); } catch { }
            }

            string defaultY = "";
            if (!string.IsNullOrEmpty(_lastSerialX))
            {
                defaultY = _lastSerialX;
            }
            else
            {
                // 첫 실행: serial.txt의 마지막 번호 - 1
                string numPart = lastSerial;
                string pfx = "";
                int dash = lastSerial.LastIndexOf('-');
                if (dash >= 0 && dash < lastSerial.Length - 1) { pfx = lastSerial.Substring(0, dash + 1); numPart = lastSerial.Substring(dash + 1); }
                if (numPart.All(char.IsDigit) && System.Numerics.BigInteger.TryParse(numPart, out var prevNum) && prevNum > 1)
                    defaultY = pfx + (prevNum - 1).ToString().PadLeft(numPart.Length, '0');
            }

            var bigFont = new Font("맑은 고딕", 16);
            var midFont = new Font("맑은 고딕", 12);

            using var dlg = new Form();
            dlg.Text = "시리얼 입력 (밀어내기식)";
            dlg.Size = new Size(600, 460);
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
            dlg.MaximizeBox = false;
            dlg.MinimizeBox = false;

            // 운영 안내 (좌)
            var lblGuide = new Label
            {
                Text = "시작 사이클: Y 비우기 → Y 스킵 (Y에 더미 보드)\r\n마감 사이클: X 비우기 → X 스킵 (X에 더미 보드)\r\n정상 사이클: 둘 다 입력 → 둘 다 검사",
                Location = new Point(20, 10),
                Size = new Size(330, 62),
                Font = new Font("맑은 고딕", 10),
                ForeColor = Color.DarkBlue,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.AliceBlue,
            };

            var lblX = new Label { Text = "X축 시리얼 (예: Y축 시리얼 + 1)", Location = new Point(20, 82), AutoSize = true, Font = midFont };
            // MaxLength 11: 접두사(VL1-) 4자 + 최대 7자리 정수 (스펙: 5자리 00001~99999 → 6자리 100000~ → 7자리 1000000~)
            var txtX = new TextBox { Text = lastSerial, Location = new Point(20, 117), Width = 350, MaxLength = 11, Font = bigFont };
            txtX.SelectionStart = txtX.Text.Length;
            txtX.GotFocus += (s, e) => { txtX.SelectionStart = txtX.Text.Length; };

            // +/- 버튼: 시리얼 숫자 증감 (minVal: X축=1, Y축=0)
            Action<TextBox, int, long> adjustSerial = (txt, delta, minVal) =>
            {
                string val = txt.Text.Trim();
                string pfx = ""; string num = val;
                int d = val.LastIndexOf('-');
                if (d >= 0 && d < val.Length - 1) { pfx = val.Substring(0, d + 1); num = val.Substring(d + 1); }
                if (num.All(char.IsDigit) && long.TryParse(num, out long n))
                {
                    n = Math.Max(minVal, n + delta);
                    txt.Text = pfx + n.ToString().PadLeft(num.Length, '0');
                }
            };
            var btnXMinus = new Button { Text = "-", Location = new Point(380, 115), Size = new Size(50, 40), Font = new Font("맑은 고딕", 16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            var btnXPlus  = new Button { Text = "+", Location = new Point(438, 115), Size = new Size(50, 40), Font = new Font("맑은 고딕", 16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            btnXMinus.Click += (s, e) => adjustSerial(txtX, -1, 1);
            btnXPlus.Click  += (s, e) => adjustSerial(txtX, 1, 1);

            var lblY = new Label { Text = "Y축 시리얼", Location = new Point(20, 182), AutoSize = true, Font = midFont };
            var txtY = new TextBox { Text = defaultY, Location = new Point(20, 217), Width = 350, MaxLength = 11, Font = bigFont };
            txtY.SelectionStart = txtY.Text.Length;
            txtY.GotFocus += (s, e) => { txtY.SelectionStart = txtY.Text.Length; };
            if (string.IsNullOrEmpty(defaultY)) txtY.PlaceholderText = "(없음 - 시작 사이클)";

            var btnYMinus = new Button { Text = "-", Location = new Point(380, 215), Size = new Size(50, 40), Font = new Font("맑은 고딕", 16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            var btnYPlus  = new Button { Text = "+", Location = new Point(438, 215), Size = new Size(50, 40), Font = new Font("맑은 고딕", 16, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
            btnYMinus.Click += (s, e) => adjustSerial(txtY, -1, 1);
            btnYPlus.Click  += (s, e) => adjustSerial(txtY, 1, 0);

            // 프리셋 버튼 (안내문 우측) — txtX/txtY 선언 후 핸들러 등록
            var btnPresetStart = new Button { Text = "시작 사이클 (Y 비우기)", Location = new Point(360, 10), Size = new Size(220, 28), Font = new Font("맑은 고딕", 10), ForeColor = Color.DarkRed };
            var btnPresetEnd   = new Button { Text = "마감 사이클 (X 비우기)", Location = new Point(360, 44), Size = new Size(220, 28), Font = new Font("맑은 고딕", 10), ForeColor = Color.DarkRed };
            btnPresetStart.Click += (s, e) => { txtY.Text = ""; txtY.PlaceholderText = "(없음 - 시작 사이클)"; };
            btnPresetEnd.Click   += (s, e) => { txtX.Text = ""; txtX.PlaceholderText = "(없음 - 마감 사이클)"; };

            // 버튼 2개 가운데 정렬 (폼 600, 버튼 160x55, 간격 20)
            var btnOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(170, 332), Size = new Size(160, 55), Font = bigFont };
            var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(350, 332), Size = new Size(160, 55), Font = bigFont };
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            dlg.Controls.AddRange(new Control[] { lblGuide, btnPresetStart, btnPresetEnd, lblX, txtX, btnXPlus, btnXMinus, lblY, txtY, btnYPlus, btnYMinus, btnOk, btnCancel });

            if (dlg.ShowDialog(this) != DialogResult.OK) return false;

            serialNumber = string.IsNullOrWhiteSpace(txtX.Text) ? "Null" : txtX.Text.Trim();
            serialNumberY = string.IsNullOrWhiteSpace(txtY.Text) ? "" : txtY.Text.Trim();
            macAddress = "Not Use";

            // 시리얼 접두사 검증 (JSON: serial_prefix, serial_prefix_check)
            // serial_prefix_check: 0=안함, 1=경고(진행가능), 2=차단(진행불가) | 양산 권장: 2
            string expectedPrefix = "";
            int prefixCheck = 1; // 기본: 경고
            try { expectedPrefix = (string)(workspace?.serial_prefix ?? ""); } catch { }
            try { prefixCheck = (int)(workspace?.serial_prefix_check ?? 1); } catch { }

            if (!string.IsNullOrEmpty(expectedPrefix) && prefixCheck > 0)
            {
                bool xBad = serialNumber != "Null" && !serialNumber.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
                bool yBad = !string.IsNullOrEmpty(serialNumberY) && !serialNumberY.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
                if (xBad || yBad)
                {
                    if (prefixCheck >= 2)
                    {
                        // 차단 모드
                        ShowLargeConfirmDialog($"시리얼 접두사를 확인하세요\n({expectedPrefix} 필요)");
                        return false;
                    }
                    else
                    {
                        // 경고 모드
                        var answer = ShowLargeYesNoDialog($"시리얼 접두사를 확인하세요\n({expectedPrefix} 필요)\n\n계속 진행하시겠습니까?");
                        if (answer == DialogResult.No) return false;
                    }
                }
            }

            // 숫자값 < 1 차단 (FW 2.0.3: 00000 거부, 유효 범위 00001~9999999)
            {
                bool numBadX = false, numBadY = false;
                if (serialNumber != "Null")
                {
                    string xNum2 = serialNumber; int xDash = serialNumber.LastIndexOf('-');
                    if (xDash >= 0 && xDash < serialNumber.Length - 1) xNum2 = serialNumber.Substring(xDash + 1);
                    if (xNum2.All(char.IsDigit) && long.TryParse(xNum2, out long xVal) && (xVal < 1 || xNum2.Length < 5)) numBadX = true;
                }
                if (!string.IsNullOrEmpty(serialNumberY))
                {
                    string yNum2 = serialNumberY; int yDash = serialNumberY.LastIndexOf('-');
                    if (yDash >= 0 && yDash < serialNumberY.Length - 1) yNum2 = serialNumberY.Substring(yDash + 1);
                    if (yNum2.All(char.IsDigit) && long.TryParse(yNum2, out long yVal) && (yVal < 1 || yNum2.Length < 5)) numBadY = true;
                }
                if (numBadX || numBadY)
                {
                    ShowLargeConfirmDialog("시리얼 형식 오류\n• 숫자값 00001 이상\n• 5자리 이상 (예: VL2-00001)\n(FW 2.0.3 스펙)");
                    return false;
                }
            }

            // X·Y 둘 다 빈값 차단 (검사할 제품 없음)
            if (serialNumber == "Null" && string.IsNullOrEmpty(serialNumberY))
            {
                ShowLargeConfirmDialog("X·Y 시리얼이 모두 비어있습니다.\n최소 한 축에 시리얼을 입력하세요.");
                return false;
            }

            // X = Y 동일 시리얼 차단 (오입력 방지)
            if (!string.IsNullOrEmpty(serialNumberY) && serialNumber != "Null" &&
                string.Equals(serialNumber, serialNumberY, StringComparison.OrdinalIgnoreCase))
            {
                ShowLargeConfirmDialog($"X·Y 시리얼이 동일합니다 ({serialNumber}).\n서로 다른 시리얼을 입력하세요.");
                return false;
            }

            // X = Y+1 확인 (밀어내기식: Y=직전 사이클 X → 정상이면 X=Y+1)
            // 접두사(VL2-) 떼고 숫자부만 비교. 양 축 모두 있을 때만(시작/마감 단축 사이클은 제외).
            // 어긋나면 경고(진행 가능) — 불량 재검·수동 건너뜀 등 정상 예외는 작업자가 Yes로 진행.
            if (serialNumber != "Null" && !string.IsNullOrEmpty(serialNumberY))
            {
                string xN = serialNumber; int xd = serialNumber.LastIndexOf('-');
                if (xd >= 0 && xd < serialNumber.Length - 1) xN = serialNumber.Substring(xd + 1);
                string yN = serialNumberY; int yd = serialNumberY.LastIndexOf('-');
                if (yd >= 0 && yd < serialNumberY.Length - 1) yN = serialNumberY.Substring(yd + 1);
                if (xN.All(char.IsDigit) && yN.All(char.IsDigit) &&
                    long.TryParse(xN, out var xNum) && long.TryParse(yN, out var yNum) &&
                    xNum != yNum + 1)
                {
                    string hint = xNum <= yNum ? "(역행 의심: X가 Y보다 작거나 같음)" : "(번호 건너뜀)";
                    var answer = ShowLargeYesNoDialog(
                        $"시리얼 순서가 맞지 않습니다.\nX축: {serialNumber}, Y축: {serialNumberY}\n정상: X = Y + 1  {hint}\n\n계속 진행하시겠습니까?");
                    if (answer == DialogResult.No) return false;
                }
            }

            label_Serial.Text = $"X축: {serialNumber}\nY축: {(string.IsNullOrEmpty(serialNumberY) ? "없음" : serialNumberY)}";
            label_mac.Text = "";

            // 다음 사이클을 위해 현재 X시리얼 기억 (마감 사이클 X=Null은 저장 안 함)
            _prevLastSerialX = _lastSerialX; // 롤백용 백업
            if (serialNumber != "Null")
            {
                _lastSerialX = serialNumber;
                try { File.WriteAllText(Path.Combine(baseDir, GetLastSerialXFileName()), serialNumber, new System.Text.UTF8Encoding(false)); } catch { }
            }

            // serial.txt에 다음 번호 기록 (자동증가)
            if (!_serialAutoIncrement) return true;
            try
            {
                // 접두사+숫자 형식 지원 (예: VL2-00001 → VL2-00002)
                string prefix = "";
                string numPart = serialNumber;
                int lastDash = serialNumber.LastIndexOf('-');
                if (lastDash >= 0 && lastDash < serialNumber.Length - 1)
                {
                    prefix = serialNumber.Substring(0, lastDash + 1);
                    numPart = serialNumber.Substring(lastDash + 1);
                }
                if (numPart.All(char.IsDigit) && System.Numerics.BigInteger.TryParse(numPart, out var num))
                {
                    num += 1;
                    string next = prefix + num.ToString().PadLeft(numPart.Length, '0');
                    File.AppendAllText(serialPath, next + Environment.NewLine, new System.Text.UTF8Encoding(false));
                }
            }
            catch { }

            return true;
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            // START 전 검사자 확인 — 미등록/홍길동(기본값)이면 경고 팝업(진행은 가능)
            {
                string inspector = IniRead("General", "Inspector", "").Trim();
                if (string.IsNullOrWhiteSpace(inspector) || inspector == "미등록")
                    MessageBox.Show("검사자가 등록되지 않았습니다.\n[검사자 변경]으로 등록하세요.",
                        "검사자 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (inspector == "홍길동")
                    MessageBox.Show("검사자가 '홍길동'(기본값)입니다.\n실제 검사자로 변경하세요.",
                        "검사자 확인", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // 모션지그 프로젝트 여부 확인
            bool isMotionProject = false;
            try
            {
                string activeProj = (string)workspace?.active_project ?? "";
                isMotionProject = activeProj.Contains("motion", StringComparison.OrdinalIgnoreCase);
            }
            catch { }

            // test_mode 확인: "single" = 1개 제품, "dual"(기본) = 밀어내기식 2개
            string testMode = "dual";
            try { int tmVal = Convert.ToInt32(workspace?.test_mode ?? 0); testMode = tmVal == 1 ? "single" : "dual"; } catch { }
            bool isSingleMode = testMode == "single";

            // F0(v05): 4슬롯 워크스페이스는 dual 전용 — single은 X1 한 번으로 Y 단계 없이 완성 PASS가 되므로 거부
            if (_slot4 && isSingleMode)
            {
                ShowLargeConfirmDialog("4슬롯 지그는 dual 전용입니다.\ntest_mode를 0(dual)으로 두고 검사하세요.");
                Log("[4슬롯] single 모드 Start 거부 (dual 전용)");
                return;
            }

            if (isMotionProject && !isSingleMode)
            {
                // dual 모드: 밀어내기식 시리얼 입력 — 4슬롯(slot_layout=4)은 4칸, 아니면 기존 2칸
                if (_slot4) { if (!ShowQuadSerialDialog()) return; }
                else if (!ShowDualSerialDialog()) return;
            }
            else
            {
                // single 모드 또는 비모션: 기존 SerialMac 폼 (시리얼 + MAC)
                serialNumber = null; // X버튼 취소 감지용 초기화
                serialNumberY = ""; // single 모드용 초기화
                SerialMac serialMacForm = new SerialMac();
                serialMacForm.SerialFileName = GetSerialFileName();
                serialMacForm.OnSerialMacEntered += (serial, mac) =>
                {
                    serialNumber = serial;
                    macAddress = mac;
                    label_Serial.Text = "Serial No: " + serialNumber;
                    label_mac.Text = "Mac: " + macAddress;
                };
                serialMacForm.ShowDialog();

                // 시리얼 미입력(취소) 시 검사 중단
                if (string.IsNullOrWhiteSpace(serialNumber) || serialNumber == "Null")
                {
                    return;
                }
                // v0.6.8: 자동증가 append는 SerialMac.cs 내부에서 이미 처리되므로 상위 레이어 중복 호출 제거
            }

            if (workspace == null)
            {
                Log("워크스페이스가 로드되지 않았습니다.");
                return;
            }

            // 시리얼 중복 체크: Result/{프로젝트폴더} 내에서 동일 시리얼 파일 검색
            // 4슬롯(dual)은 X1·X2 각각 검사, 그 외는 기존대로 serialNumber 1개
            int dupCheck = 0;
            try { dupCheck = Convert.ToInt32(workspace.serial_duplicate_check); } catch { }
            var dupTargets = (isMotionProject && !isSingleMode && _slot4)
                ? new[] { _slotSerial[SLOT_X1], _slotSerial[SLOT_X2] }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
                : new List<string> { serialNumber };
            foreach (var dupSn in dupTargets)
            {
                if (dupCheck <= 0 || string.IsNullOrWhiteSpace(dupSn) || dupSn == "Null") continue;
                string projFolder = "";
                try { projFolder = ((string)workspace.projects[0].name).Replace(" ", ""); } catch { }
                if (string.IsNullOrWhiteSpace(projFolder)) projFolder = "unknown";
                string resultBase = Path.Combine(Directory.GetCurrentDirectory(), "Result", projFolder);
                bool found = false;
                try
                {
                    if (Directory.Exists(resultBase))
                        found = Directory.GetFiles(resultBase, $"{dupSn}.json", SearchOption.AllDirectories).Length > 0;
                }
                catch { }

                if (found)
                {
                    if (dupCheck == 2)
                    {
                        // 옵션 2: 무조건 차단
                        ShowLargeConfirmDialog($"시리얼 [{dupSn}]은\n이미 검사 완료된 항목입니다.");
                        Log($"시리얼 중복 — 검사 차단: {dupSn}");
                        return;
                    }
                    else
                    {
                        // 옵션 1: 경고 팝업 (덮어쓰기/취소 선택)
                        var answer = ShowLargeYesNoDialog($"시리얼 [{dupSn}]은\n이미 검사 완료된 항목입니다.\n덮어쓰시겠습니까?");
                        if (answer == DialogResult.No)
                        {
                            Log($"시리얼 중복 — 검사 취소: {dupSn}");
                            return;
                        }
                    }
                }
            }

            // clear_log_on_run_start: 시작 시 화면 로그 비움 (파일 로그는 계속 append)
            if (_clearLogOnRunStart) listBox_Log.Items.Clear();

            _isRunning = true;

            string project_id = workspace.active_project;
            Log("──────────────────────────────────────────────");
            Log($"프로젝트 '{project_id}' 실행 시작...");

            // 버튼 상태 변경
            button_start.Enabled = false;
            buttonStop.Enabled = true;

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            // DoProject 실행
            Task.Run(async () =>
            {
                await MainDoProject(workspace, project_id, new Action<string>(Log), this, token);

                // UI 스레드에서 버튼 복원
                this.Invoke(new Action(() =>
                {
                    button_start.Enabled = true;
                    buttonStop.Enabled = false;
                    _isRunning = false;
                }));
            });
        }

        public DataGridView dataGridView1;
        private DataGridViewTextBoxColumn Number;
        private DataGridViewTextBoxColumn Name;
        private DataGridViewTextBoxColumn Result;
        private Label label1;
        private Label label_CurrentTest;
        internal Label label_cycle;



        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_isRunning) return; // F6(v05): 검사 중 항목 제외 토글 금지 (실행 중 빨강 → Skip → 부분 검사 PASS 방지)
            // 클릭된 셀이 첫 번째 열(번호)인지 확인
            if (e.ColumnIndex == 0 && e.RowIndex >= 0)  // 0번 열(번호), 헤더 클릭 방지
            {
                DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                // 현재 색상이 흰색이면 빨간색, 아니면 흰색으로 토글
                if (cell.Style.BackColor == Color.White || cell.Style.BackColor == Color.Empty)
                {
                    cell.Style.BackColor = Color.Red; // 빨간색으로 변경
                }
                else
                {
                    cell.Style.BackColor = Color.White; // 다시 원래 색상으로 변경
                }
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_isRunning) return; // F6(v05): 검사 중 전체 토글 금지
            // '번호' 컬럼(0번) 헤더 클릭 → 전체 행 스킵 토글
            if (e.ColumnIndex != 0) return;

            // 현재 빨간색 아닌 행이 하나라도 있으면 → 전체 빨간색(스킵)
            // 전부 빨간색이면 → 전체 해제
            bool allRed = true;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells[0].Style.BackColor != Color.Red)
                { allRed = false; break; }
            }

            Color target = allRed ? Color.White : Color.Red;
            foreach (DataGridViewRow row in dataGridView1.Rows)
                row.Cells[0].Style.BackColor = target;
        }

        private void DisableColumnSorting()
        {
            foreach (DataGridViewColumn column in dataGridView1.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable; //표의 자동 오름차순, 내림차순정렬 비활성화
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            // F6(v05): 검사 중 더블클릭 전부 무시 — 항목명 단독실행이 UI 스레드에서 모터·포트를 이중 구동하던 경로 차단
            if (_isRunning) return;

            // 2열(테스트 이름) 더블클릭: 단독실행
            if (e.ColumnIndex == 1)
            {
                if (workspace == null)
                {
                    Log("워크스페이스가 로드되지 않았습니다.");
                    return;
                }

                // F6(v05): 쓰기 항목(시리얼·출하상태·파라미터 저장)은 단독실행 차단 — 이전 보드 시리얼로 UID 기록되는 오염 방지
                string pickedId = "";
                try
                {
                    var procList = (IList<object>)((IDictionary<string, object>)((IList<object>)workspace.projects)[0])["procs"];
                    if (e.RowIndex < procList.Count) pickedId = ((IDictionary<string, object>)procList[e.RowIndex])["id"]?.ToString() ?? "";
                }
                catch { }
                if (pickedId.StartsWith("UID_") || pickedId.StartsWith("RCONF_") || pickedId.StartsWith("APPCFG_SAVE_"))
                {
                    Log($"[단독실행] 쓰기 항목 차단: {pickedId} (시리얼·출하상태·저장은 전체 검사에서만)");
                    return;
                }

                // 단독실행 전 번호셀 색 저장 → 끝나면 복원 (빨강 잔존 시 다음 Start가 1항목만 검사하던 문제)
                var savedColors = new List<Color>();
                foreach (DataGridViewRow row in dataGridView1.Rows) savedColors.Add(row.Cells[0].Style.BackColor);

                foreach (DataGridViewRow row in dataGridView1.Rows)
                    row.Cells[0].Style.BackColor = Color.Red; // 전부 Skip
                dataGridView1.Rows[e.RowIndex].Cells[0].Style.BackColor = Color.White; // 선택 행만 실행 허용

                // 이전 런의 __serial 메타 제거 — 단독실행에 남은 시리얼이 치환되지 않게
                try
                {
                    foreach (var p in (IList<object>)((IDictionary<string, object>)((IList<object>)workspace.projects)[0])["procs"])
                        if (((IDictionary<string, object>)p).TryGetValue("param", out var pr) && pr is IDictionary<string, object> pd)
                            pd.Remove("__serial");
                }
                catch { }

                string project_id = workspace.active_project;
                Log($"프로젝트 '{project_id}' 실행 시작...");

                _isRunning = true;
                button_start.Enabled = false;
                try
                {
                    app.DoProject(workspace, project_id, new Action<string>(Log), this);
                }
                finally
                {
                    for (int r = 0; r < dataGridView1.Rows.Count && r < savedColors.Count; r++)
                        dataGridView1.Rows[r].Cells[0].Style.BackColor = savedColors[r];
                    _isRunning = false;
                    button_start.Enabled = true;
                }
                return;
            }

            // 3열(결과) 더블클릭: ret message 표시 (실행 중에는 무시). 4슬롯은 X1..Y2 열(2..5) 각각
            if (e.ColumnIndex >= 2)
            {
                if (_isRunning) return; // 실행 중이면 열지 않음

                var row = dataGridView1.Rows[e.RowIndex];
                var name = row.Cells[1].Value?.ToString() ?? "세부 로그";
                if (_slot4 && e.ColumnIndex - 2 < SLOT_LABELS.Length) name = $"[{SLOT_LABELS[e.ColumnIndex - 2]}] {name}";
                var msg = row.Cells[e.ColumnIndex].Tag as string; // SetResultMessage에서 저장한 값

                if (string.IsNullOrWhiteSpace(msg))
                {
                    MessageBox.Show(this, "저장된 상세 메시지가 없습니다.", name,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, msg, name,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }


        private void button_Stop_Click(object sender, EventArgs e)
        {


        }

        private void label_CurrentTime_Click(object sender, EventArgs e)
        {

        }

        private bool TryConnectToComm(string commId,
                              IDictionary<string, object> libraries,
                              Action<string> logAction)
        {
            // 0) 라이브러리 조회 & 타입 체크
            if (!libraries.TryGetValue(commId, out var obj))
            {
                logAction($"오류: '{commId}' 라이브러리를 찾을 수 없습니다.");
                return false;
            }
            if (obj is not IComm comm)
            {
                logAction($"오류: '{commId}'는 IComm 타입이 아닙니다.");
                return false;
            }

            // 1) 연결상태 확인 유틸
            bool IsConnected()
            {
                try
                {
                    var st = comm.GetStatus();
                    if (st == null) return false;

                    object v;
                    return (st.TryGetValue("Connected", out v) && v is bool b1 && b1)     // Tcp 등
                        || (st.TryGetValue("is_connected", out v) && v is bool b2 && b2)  // Uart 등
                        || (st.TryGetValue("isConnected", out v) && v is bool b3 && b3);  // 예비
                }
                catch { return false; }
            }

            // 2) 이미 연결이면 패스
            if (IsConnected())
            {
                logAction($"커넥션 이미 연결됨: {commId}");
                return true;
            }

            // 3) 호출할 메서드 '하나'만 선택 (Connect → Begin → EnsureConnected)
            var t = comm.GetType();
            var miConnect = t.GetMethod("Connect", Type.EmptyTypes);
            var miBegin = t.GetMethod("Begin", Type.EmptyTypes);
            var miEnsure = t.GetMethod("EnsureConnected", Type.EmptyTypes);

            var chosen = miConnect ?? miBegin ?? miEnsure;
            var chosenName = chosen?.Name ?? "(없음)";

            if (chosen == null)
            {
                logAction($"오류: '{commId}'에 사용할 연결 메서드(Connect/Begin/EnsureConnected)가 없습니다.");
                return false;
            }

            // 4) 선택한 메서드 '한 번'만 호출
            try
            {
                bool ok;
                if (chosen.ReturnType == typeof(bool))
                {
                    ok = (bool)chosen.Invoke(comm, null)!;       // Connect()가 bool 반환하는 경우
                }
                else
                {
                    chosen.Invoke(comm, null);                    // Begin()/EnsureConnected()는 void일 가능성
                    ok = IsConnected();
                }

                if (ok)
                {
                    if (LogConfig.IsTest) logAction($"커넥션 성공({chosenName}): {commId}");
                    return true;
                }
                else
                {
                    logAction($"오류: {commId} 커넥션 실패 (메서드: {chosenName})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logAction($"'{commId}' {chosenName} 예외: {ex.Message}");
                return false;
            }
        }


        public async Task MainDoProject(dynamic workspace, string project_id, Action<string> logAction, MainForm mainForm, CancellationToken token)
        {
            // F0(v05): 사이클 스냅샷 — 4슬롯 여부·워크스페이스 키는 시작 시 고정, 실행 중 재계산 금지
            bool slot4 = mainForm.Slot4Run;
            string wsKey = string.IsNullOrEmpty(mainForm._currentWorkspacePath) ? "unknown" : Path.GetFileNameWithoutExtension(mainForm._currentWorkspacePath);

            // 결과 셀 초기화 + 이전 run의 return message(Tag) 비우기 — 결과 열 전부(2슬롯 1열 / 4슬롯 4열)
            for (int i = 0; i < mainForm.dataGridView1.Rows.Count; i++)
            {
                for (int c = 2; c < mainForm.dataGridView1.Columns.Count; c++)
                {
                    var resultCell = mainForm.dataGridView1.Rows[i].Cells[c];
                    resultCell.Value = "";
                    resultCell.Style.BackColor = Color.White;
                    resultCell.Style.ForeColor = Color.Black;
                    resultCell.Tag = null;
                }
            }


            if (LogConfig.IsTest) logAction($"MainDoProject() 실행 시작: project_id = {project_id}");

            if (workspace == null)
            {
                logAction("오류: workspace가 null입니다.");
                RollbackSerialAndMacIfFail(logAction);
                return;
            }

            var project = ((IEnumerable<dynamic>)workspace.projects).FirstOrDefault(p => p.id == project_id);
            if (project == null)
            {
                logAction($"오류: project_id '{project_id}'에 해당하는 프로젝트를 찾을 수 없습니다.");
                RollbackSerialAndMacIfFail(logAction);
                return;
            }

            logAction($"프로젝트 이름: {project.name}");
            if (LogConfig.IsTest) logAction($"프로젝트에 등록된 라이브러리: {JsonConvert.SerializeObject(project.library_depencency)}");

            var libraries = new Dictionary<string, object>();
            mainForm.libraries = libraries; // Stop 버튼에서 접근할 수 있도록 멤버에 저장
            foreach (var lib_dep in project.library_depencency)
            {
                var library = ((IEnumerable<dynamic>)workspace.libraries)
                    .FirstOrDefault(lib => lib.id == lib_dep) as IDictionary<string, object>;

                if (library != null)
                {
                    if (library.ContainsKey("instance"))
                    {
                        libraries[lib_dep] = library["instance"];
                    }
                    else
                    {
                        logAction($"오류: 라이브러리 '{lib_dep}'의 instance가 없습니다.");
                    }
                }
                else
                {
                    logAction($"오류: 라이브러리 '{lib_dep}'를 찾을 수 없습니다.");
                }
            }

            // 작업자 확인 팝업 콜백 등록 (커스텀 대형 폰트 팝업)
            libraries["confirmCallback"] = new Func<string, bool>(msg =>
            {
                DialogResult result = DialogResult.OK;
                if (InvokeRequired)
                    Invoke(new Action(() => { result = ShowLargeConfirmDialog(msg); }));
                else
                    result = ShowLargeConfirmDialog(msg);
                return result == DialogResult.OK;
            });

            // 취소 가능한 YesNo 팝업 콜백 (OPERATOR_CONFIRM 등에서 사용)
            libraries["confirmCallbackYesNo"] = new Func<string, bool>(msg =>
            {
                DialogResult result = DialogResult.Yes;
                if (InvokeRequired)
                    Invoke(new Action(() => { result = ShowLargeYesNoDialog(msg); }));
                else
                    result = ShowLargeYesNoDialog(msg);
                return result == DialogResult.Yes;
            });

            // CTSP 인스턴스 초기화
            if (libraries.ContainsKey("ctsp"))
            {
                ctsp_ = libraries["ctsp"] as IProtocol;
                if (ctsp_ == null)
                {
                    logAction("CTSP 라이브러리 인스턴스 초기화 실패");
                    RollbackSerialAndMacIfFail(logAction);
                    return;
                }
            }
            else
            {
                logAction("CTSP 라이브러리가 libraries에 존재하지 않습니다.");
                RollbackSerialAndMacIfFail(logAction);
                return;
            }

            // ===== 모든 IComm 포트 연결 시도 (한 번씩만) =====
            var commIds = libraries.Where(kv => kv.Value is IComm).Select(kv => kv.Key).ToList();
            foreach (var id in commIds)
            {
                if (!TryConnectToComm(id, libraries, logAction))   // ← 당신이 제공한 3-파라미터 버전 사용
                {
                    logAction($"통신 포트 연결 실패: '{id}'. 검사 종료.");

                    // 연결 실패 유형별 안내 팝업 (UI 스레드에서 최상위로 표시)
                    if (libraries.TryGetValue(id, out var lib))
                    {
                        string msg = null;
                        if (lib is Tcp)
                            msg = $"'{id}' TCP 연결에 실패했습니다.\nLAN 케이블 연결을 확인해주세요.";
                        else if (lib is Uart)
                        {
                            var commType = id.Contains("485") ? "RS-485" : "RS-232";
                            msg = $"{commType} 연결에 실패했습니다.\nUSB 포트를 확인해주세요.";
                        }
                        if (msg != null)
                            mainForm.Invoke(() => MessageBox.Show(mainForm, msg, "연결 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning));
                    }
                    RollbackSerialAndMacIfFail(logAction);
                    return;
                }
            }

            int okCount = 0, failCount = 0, skipCount = 0;
            bool wasCanceled = false;   // Stop 등으로 사용자 중단 여부
            mainForm._failRecords.Clear();   // 불량보기: 검사 시작 시 이전 누적 비움

            // 파라미터에 proc_id/proc_name 넣어주는 헬퍼
            void UpsertParamMeta(object paramObj, string key, object val)
            {
                if (paramObj is IDictionary<string, object> d1) d1[key] = val;
                else if (paramObj is IDictionary<string, object?> d2) d2[key] = val;
                else
                {
                    // ExpandoObject(dynamic) 등도 대부분 IDictionary<string, object>로 캐스팅 가능
                    if (paramObj is System.Dynamic.ExpandoObject eo)
                    {
                        var d = (IDictionary<string, object>)eo;
                        d[key] = val;
                    }
                    // 그 외 타입은 건너뜀(필수는 아님)
                }
            }

            // ══════════════════════════════════════════════════════════════
            //  4슬롯(X1·X2·Y1·Y2) 실행 — slot_layout=4 + dual 모드에서만. 기존 2슬롯 경로는 그대로.
            //  단계1(현재): slot_group X/Y는 슬롯 순차 호출, all(#6/#7)은 X 슬롯 순차(기존 단일보드 측정).
            //  단계2: all → 모듈 4보드 동시측정 / 단계3: X/Y 그룹 Task 병렬 (PLAN §5)
            // ══════════════════════════════════════════════════════════════
            bool slot4CommonFailed = false;   // 공통 항목(#0 통신·#3/#10 영점) FAIL → 4슬롯 전부 FAIL 취급

            object InvokeProc(object instance, MethodInfo method, object param, Action<string> log)
            {
                var ps = method.GetParameters();
                if (ps.Length == 3) return method.Invoke(instance, new object[] { libraries, param, log });
                if (ps.Length == 4) return method.Invoke(instance, new object[] { libraries, param, log, new Func<string, bool>(mainForm.ShowKeyboardConfirmationForm) });
                throw new InvalidOperationException($"지원되지 않는 메서드 시그니처: {method.Name} (파라미터 수: {ps.Length})");
            }
            bool IsProcSuccess(object res, out string retmsg)
            {
                retmsg = "";
                if (res is IDictionary<string, object> d)
                {
                    if (d.TryGetValue("retmsg", out var rm)) retmsg = rm?.ToString() ?? "";
                    return d.ContainsKey("success") && Convert.ToBoolean(d["success"]);
                }
                return false;
            }
            void SetSlotCellTag(int row, int slot, string rm)
            {
                try { mainForm.Invoke(new Action(() => mainForm.dataGridView1.Rows[row].Cells[2 + slot].Tag = rm)); } catch { }
            }

            // 반환 true = 검사 중단 (공통 항목 FAIL + fail_continue 아님)
            bool RunProcSlot4(int i, dynamic proc, object instance, MethodInfo method)
            {
                string group = MainForm.GetSlotGroup(proc);
                // 단계1: all(#6/#7)은 X 슬롯만 순차 측정 (Y 슬롯은 #14/#15에서 기존처럼 직접 측정)
                int[] slots = MainForm.SlotsOfGroup(group == "all" ? "X" : group);
                var swItem = System.Diagnostics.Stopwatch.StartNew();
                logAction($"▶ TEST_BEGIN: {proc.name} (ID: {proc.id})");
                if (proc.param != null)
                {
                    UpsertParamMeta(proc.param, "proc_id", proc.id);
                    UpsertParamMeta(proc.param, "proc_name", proc.name);
                }

                if (slots.Length == 0)
                {
                    // common: 모터·경사계 항목 1회 실행, 4칸 동일 표시
                    bool ok = false; string rm = ""; bool exc = false;
                    try { ok = IsProcSuccess(InvokeProc(instance, method, proc.param, logAction), out rm); }
                    catch (Exception ex) { exc = true; logAction($"실행 중 오류 발생: {ex.Message}"); mainForm.AddFailRecord(proc, null, $"예외: {ex.Message}"); }
                    for (int s = 0; s < 4; s++) { mainForm.UpdateSlotCell(i, s, ok ? "OK" : "FAIL", ok ? Color.Green : Color.Red); SetSlotCellTag(i, s, rm); }
                    logAction($"◀ TEST_FINISH: {proc.name} -> {(ok ? "OK" : "FAIL")} ({swItem.Elapsed.TotalSeconds:0.00}초)");
                    if (ok) { okCount++; return false; }
                    failCount++;
                    slot4CommonFailed = true;
                    if (!exc) mainForm.AddFailRecord(proc, rm, null);
                    // 공통 항목 FAIL = 지그/통신 문제 → fail_continue 아니면 중단 (기존 Y결합·무태그 FAIL 규칙과 동일)
                    if (!mainForm.checkBox_Process.Checked) { logAction($"테스트 {proc.name}에서 실패했습니다. 검사 종료."); return true; }
                    return false;
                }

                // 해당 없는 슬롯 열은 "—"
                for (int s = 0; s < 4; s++) if (Array.IndexOf(slots, s) < 0) mainForm.MarkSlotCellNA(i, s);

                bool anyFail = false, anyRun = false;
                foreach (int s in slots)
                {
                    string slotName = MainForm.SLOT_LABELS[s];
                    if (mainForm._slotSerial[s].Length == 0 || (mainForm._slotFailed[s] && !mainForm.checkBox_Process.Checked))
                    {
                        mainForm.UpdateSlotCell(i, s, "Skip", Color.LightBlue, Color.Black);
                        continue;
                    }
                    anyRun = true;
                    mainForm.UpdateSlotCell(i, s, "...", Color.Yellow, Color.Black);
                    var p = mainForm.CloneParamForSlot(proc, s);
                    Action<string> slog = m => logAction($"[{slotName}] {m}");
                    bool ok = false; string rm = ""; bool exc = false;
                    try { ok = IsProcSuccess(InvokeProc(instance, method, p, slog), out rm); }
                    catch (Exception ex) { exc = true; slog($"실행 중 오류 발생: {ex.Message}"); mainForm.AddFailRecordSlot(proc, null, $"예외: {ex.Message}", s); }
                    mainForm.UpdateSlotCell(i, s, ok ? "OK" : "FAIL", ok ? Color.Green : Color.Red);
                    SetSlotCellTag(i, s, rm);
                    if (ok) { okCount++; continue; }
                    failCount++; anyFail = true;
                    mainForm._slotFailed[s] = true;
                    if (!exc) mainForm.AddFailRecordSlot(proc, rm, null, s);
                    slog($"{proc.name} -> FAIL (슬롯 제외, 나머지 슬롯 진행)");
                }
                if (!anyRun) { skipCount++; logAction($"테스트 {proc.name} 스킵 (실행할 슬롯 없음)"); }
                logAction($"◀ TEST_FINISH: {proc.name} -> {(anyFail ? "FAIL" : (anyRun ? "OK" : "SKIP"))} ({swItem.Elapsed.TotalSeconds:0.00}초)");
                return false;
            }

            // 4슬롯 사이클 종료: 레인별(X1→Y1, X2→Y2) pending 저장·완성 업로드, 슬롯별 PASS/FAIL 요약 팝업
            void FinishSlot4()
            {
                string summary = mainForm.SlotSummaryText(slot4CommonFailed);
                logAction($"슬롯 결과: {summary}");
                mainForm.Invoke(() => mainForm.label_Serial.Text = summary.Replace(" · ", "\n"));

                var savedList = new List<string>();
                bool anyXPass = false;
                for (int lane = 0; lane < 2; lane++)
                {
                    int xs = lane == 0 ? MainForm.SLOT_X1 : MainForm.SLOT_X2;
                    int ys = lane == 0 ? MainForm.SLOT_Y1 : MainForm.SLOT_Y2;
                    string xn = MainForm.SLOT_LABELS[xs], yn = MainForm.SLOT_LABELS[ys];
                    string xSn = mainForm._slotSerial[xs], ySn = mainForm._slotSerial[ys];
                    string pendingPath = GetPendingSlotPath(lane);
                    bool xPass = xSn.Length > 0 && !mainForm._slotFailed[xs] && !slot4CommonFailed;
                    bool yPass = ySn.Length > 0 && !mainForm._slotFailed[ys] && !slot4CommonFailed;
                    if (xPass) anyXPass = true;

                    // 1) X 결과 → 레인 pending (다음 사이클 Y와 합산)
                    if (xPass)
                    {
                        var xRet = new JArray();
                        mainForm.Invoke(new Action(() => CollectRetmsgBySlot(mainForm, xs, xRet)));
                        var pendingData = new JObject
                        {
                            ["serial"] = xSn,
                            ["slot"] = xn,
                            ["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ["retmsg"] = xRet
                        };
                        File.WriteAllText(pendingPath, pendingData.ToString(), new System.Text.UTF8Encoding(false));
                        logAction($"[{xn}] X 결과 임시 저장: {xSn} ({xRet.Count}항목)");
                    }

                    // 2) Y 결과 + 이전 pending → 완성 저장
                    if (yPass)
                    {
                        var prev = _previousPendingSlot[lane];
                        string prevSn = prev?["serial"]?.ToString() ?? "";
                        if (prev != null && prevSn == ySn)
                        {
                            var yRet = new JArray();
                            mainForm.Invoke(new Action(() => CollectRetmsgBySlot(mainForm, ys, yRet)));
                            var combined = new JArray();
                            foreach (var it in prev["retmsg"] as JArray ?? new JArray()) combined.Add(it);
                            foreach (var it in yRet) combined.Add(it);
                            mainForm.SaveLogDirect(ySn, combined, logAction, $"{xn}->{yn}");
                            savedList.Add(ySn);
                            logAction($"[{yn}] 완성 업로드: {ySn} (X+Y {combined.Count}항목)");
                        }
                        else if (prev == null)
                            logAction($"[{yn}] Y 결과 있으나 이전 pending 없음 (첫 사이클)");
                        else
                            logAction($"[경고] [{yn}] pending 시리얼 불일치: pending={prevSn}, Y={ySn}");
                        _previousPendingSlot[lane] = null;
                    }

                    // 3) 마감(X 빈칸): 레인 pending 삭제 → 재투입 중복 완성 차단
                    if (xSn.Length == 0)
                    {
                        try { if (File.Exists(pendingPath)) File.Delete(pendingPath); } catch { }
                    }
                }
                if (savedList.Count > 0)
                    mainForm.Invoke(() => mainForm.label_passSaved.Text = "PASS SAVED: " + string.Join(", ", savedList));

                string totals = $"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}\n{summary}";
                if (failCount == 0)
                {
                    mainForm.ShowPassForm();
                    mainForm.passForm.SetResult(totals);
                }
                else
                {
                    // 시리얼 번호 롤백(재사용)은 X 슬롯이 하나도 통과 못했을 때만. 한 슬롯만 FAIL이면 번호 진행 유지(재검은 수동 입력)
                    if (!anyXPass) RollbackSerialAndMacIfFail(logAction);
                    mainForm.ShowFailForm();
                    mainForm.failForm.SetResult(totals);
                }
            }

            // repeat_all_count: 전체 항목 반복 횟수 (기본값 1)
            int repeatCount = 1;
            try { repeatCount = Math.Max(1, Convert.ToInt32(((IDictionary<string, object>)project).TryGetValue("repeat_all_count", out var _rc) ? _rc : 1)); } catch { }
            int repeatDelay = 1000;
            try { repeatDelay = Convert.ToInt32(((IDictionary<string, object>)project).TryGetValue("repeat_all_delay", out var _rd) ? _rd : 1000); } catch { }

            try
            {
                // test_mode 확인
                string curTestMode = "dual";
                try { int tmVal = Convert.ToInt32(workspace?.test_mode ?? 0); curTestMode = tmVal == 1 ? "single" : "dual"; } catch { }
                bool curSingleMode = curTestMode == "single";

                string snDisplay = string.IsNullOrEmpty(mainForm.serialNumberY)
                    ? $"S/N: {serialNumber}"
                    : $"X축: {serialNumber}, Y축: {mainForm.serialNumberY}";
                if (curSingleMode) snDisplay = $"S/N: {serialNumber} (단독 검사)";
                logAction($"검사 시작 - {project.name} [{snDisplay}]");
                var testStartTime = DateTime.Now;
                mainForm.Invoke(() => mainForm.label_passSaved.Text = "");

                // 밀어내기식: 이전 pending 파일 읽기 (single 모드에서는 pending 사용 안 함)
                string pendingPathInit = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pending_x_result.json");
                _previousPendingData = null;
                if (slot4)
                {
                    // 4슬롯: 레인별 pending (X1→Y1, X2→Y2)
                    for (int lane = 0; lane < 2; lane++)
                    {
                        _previousPendingSlot[lane] = null;
                        string pp = GetPendingSlotPath(lane);
                        if (!File.Exists(pp)) continue;
                        try
                        {
                            var loaded = JObject.Parse(File.ReadAllText(pp, new System.Text.UTF8Encoding(false)));
                            _previousPendingSlot[lane] = loaded;
                            logAction($"이전 pending 로드 (레인{lane + 1}): {loaded["serial"]} ({loaded["time"]})");
                        }
                        catch { logAction($"[경고] pending 파일 읽기 실패 (레인{lane + 1})"); }
                    }
                }
                else if (!curSingleMode && File.Exists(pendingPathInit))
                {
                    try
                    {
                        var loaded = JObject.Parse(File.ReadAllText(pendingPathInit, new System.Text.UTF8Encoding(false)));
                        if (loaded["serial"]?.ToString() == "Null")
                            logAction("[INFO] pending 무시: Null (마감 사이클 잔여)");
                        else
                        {
                            _previousPendingData = loaded;
                            logAction($"이전 pending 로드: {_previousPendingData["serial"]} ({_previousPendingData["time"]})");
                        }
                    }
                    catch { logAction("[경고] pending 파일 읽기 실패"); }
                }
                if (repeatCount > 1) logAction($"전체 반복 설정: {repeatCount}회");
                mainForm.Invoke(() => mainForm.label_cycle.Text = $"(0 / {repeatCount})");

                for (int round = 0; round < repeatCount; round++)
                {
                if (repeatCount > 1)
                    logAction($"========== 전체 반복 [{round + 1}/{repeatCount}] ==========");
                mainForm.Invoke(() => mainForm.label_cycle.Text = $"({round + 1} / {repeatCount})");


                // 라운드마다 카운터 리셋 (마지막 라운드 값만 최종 사용)
                if (repeatCount > 1 && round > 0)
                {
                    okCount = 0; failCount = 0; skipCount = 0;
                    mainForm._failRecords.Clear();   // 불량보기: 라운드 초기화 시 함께 비움
                    // DataGridView 결과 초기화
                    for (int ri = 0; ri < mainForm.dataGridView1.Rows.Count; ri++)
                    {
                        mainForm.UpdateDataGridView(ri, "", Color.White, Color.Black);
                    }
                }

                bool xGroupFailed = false;   // X결합 그룹 FAIL 시 남은 X 항목 스킵 (라운드마다 리셋)
                for (int s = 0; s < 4; s++) mainForm._slotFailed[s] = false;   // 4슬롯: 슬롯 단위 FAIL 제외 (라운드마다 리셋)
                slot4CommonFailed = false;
                for (int i = 0; i < project.procs.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        logAction("검사 중단 요청됨 (사용자 Stop)");
                        wasCanceled = true;
                        break;
                    }

                    var proc = project.procs[i];
                    var numberCell = mainForm.dataGridView1.Rows[i].Cells[0];

                    // 셀 초기화
                    mainForm.UpdateDataGridView(i, "", Color.White, Color.Black);
                    // 진행중 표시 ("...", 노란색/검정)
                    mainForm.UpdateDataGridView(i, "...", Color.Yellow, Color.Black);

                    // X결합 FAIL 후: 남은 X결합 항목 스킵하고 Y결합부터 진행 (시간 절약)
                    if (xGroupFailed && FailInfoForm.ExtractCoupling((string)(proc.name ?? "")) == "X결합")
                    {
                        mainForm.UpdateDataGridView(i, "Skip", Color.LightBlue, Color.Black);
                        logAction($"테스트 {proc.name} 스킵 (X결합 FAIL로 남은 X 항목 건너뜀 → Y 진행)");
                        skipCount++;
                        continue;
                    }

                    if (proc.param != null)
                    {
                        UpsertParamMeta(proc.param, "__proc_id", (string)proc.id);
                        UpsertParamMeta(proc.param, "__proc_name", (string)proc.name);
                        UpsertParamMeta(proc.param, "__repeat_round", round);
                        try { if (proc.unit != null) UpsertParamMeta(proc.param, "__proc_unit", (string)proc.unit); } catch { }
                        try { if (proc.scale != null) UpsertParamMeta(proc.param, "__proc_scale", proc.scale.ToString()); } catch { }
                        // 밀어내기식: Y축 proc은 Y시리얼 사용
                        string procSerial = mainForm.serialNumber ?? "";
                        try
                        {
                            var paramDict = proc.param as IDictionary<string, object>;
                            string uartId = paramDict != null && paramDict.ContainsKey("uart_id") ? paramDict["uart_id"]?.ToString() : "";
                            if ((uartId == "uart_232_y" || uartId == "uart_485_y") &&
                                !string.IsNullOrEmpty(mainForm.serialNumberY))
                            {
                                procSerial = mainForm.serialNumberY;
                            }
                        }
                        catch { }
                        UpsertParamMeta(proc.param, "__serial", procSerial);
                    }

                    // single 모드: 원래 방식으로 복원 (팝업 + uart_id 오버라이드)
                    if (curSingleMode && proc.param != null)
                    {
                        try
                        {
                            var pd = proc.param as IDictionary<string, object>;
                            string procId = (string)proc.id ?? "";
                            if (pd != null)
                            {
                                // 통신검사: test_mode 전달 (싱글이면 Y축 스킵)
                                if (procId == "COMM_485" || procId == "COMM_232")
                                {
                                    pd["test_mode"] = "single";
                                }
                                // uart_id 오버라이드: single은 제품 1개 (232: COM14→COM13, 485: COM12→COM11)
                                if (pd.ContainsKey("uart_id") && pd["uart_id"]?.ToString() == "uart_232_y")
                                {
                                    pd["uart_id"] = "uart_232";
                                }
                                if (pd.ContainsKey("uart_id") && pd["uart_id"]?.ToString() == "uart_485_y")
                                {
                                    pd["uart_id"] = "uart_485";
                                }
                                // #1 X축 영점: single용 메시지 (232/485 공통)
                                if ((procId == "MOTION_ZERO_232" || procId == "MOTION_ZERO_485")
                                    && pd.ContainsKey("gyro_dir") && pd["gyro_dir"]?.ToString() == "setdir6"
                                    && pd.ContainsKey("operator_prompt"))
                                {
                                    pd["operator_prompt"] = "제품을 X축 위치에 장착해주세요";
                                }
                                // #7 Y축 영점: 제품 이동 팝업 복원 (232/485 공통)
                                if ((procId == "MOTION_ZERO_232" || procId == "MOTION_ZERO_485")
                                    && pd.ContainsKey("gyro_dir") && pd["gyro_dir"]?.ToString() == "setdir5"
                                    && !pd.ContainsKey("operator_prompt"))
                                {
                                    pd["operator_prompt"] = "제품을 Y축 위치로 옮겨주세요";
                                }
                            }
                        }
                        catch { }
                    }

                    // 빈 축 시리얼 스킵: Y빈칸→Y축 proc 스킵, X빈칸→X축 proc 스킵 (밀어내기 모드 한정, 4슬롯은 RunProcSlot4에서 슬롯별 처리)
                    if (!curSingleMode && !slot4)
                    {
                        string procUartId2 = "";
                        string procName2 = (string)proc.name ?? "";
                        try { var pd2 = proc.param as IDictionary<string, object>; if (pd2 != null && pd2.ContainsKey("uart_id")) procUartId2 = pd2["uart_id"]?.ToString() ?? ""; } catch { }
                        // skip_coupling 명시 필드 최우선 (없으면 기존 휴리스틱 — 하위호환).
                        //  #7(X포트·Y이름=X결합)/#10(X포트·Y결합) 등 포트·이름으로 구분 불가한 항목 정확화.
                        string skipCoupling2 = "";
                        try { var pdc = proc.param as IDictionary<string, object>; if (pdc != null && pdc.ContainsKey("skip_coupling")) skipCoupling2 = pdc["skip_coupling"]?.ToString() ?? ""; } catch { }
                        bool isXProc2, isYProc2;
                        if (skipCoupling2 == "X")      { isXProc2 = true;  isYProc2 = false; }
                        else if (skipCoupling2 == "Y") { isXProc2 = false; isYProc2 = true;  }
                        else
                        {
                            isYProc2 = procUartId2.Contains("_y") || ((string)proc.id ?? "").Contains("_Y_") || procName2.Contains("Y축");
                            isXProc2 = !isYProc2 && procName2.Contains("X축");
                        }
                        bool isCommProc2 = ((string)proc.id ?? "") == "COMM_485" || ((string)proc.id ?? "") == "COMM_232";
                        bool emptySkip = (string.IsNullOrEmpty(mainForm.serialNumberY) && (isYProc2 || isCommProc2)) ||
                                         (mainForm.serialNumber == "Null"               && (isXProc2 || isCommProc2));
                        if (emptySkip)
                        {
                            mainForm.UpdateDataGridView(i, "Skip", Color.LightBlue, Color.Black);
                            logAction($"테스트 {proc.name} (ID: {proc.id}) **스킵됨** (빈 축 시리얼)");
                            skipCount++;
                            continue;
                        }
                    }

                    // 스킵 처리
                    if (numberCell.Style.BackColor == Color.Red)
                    {
                        mainForm.UpdateDataGridView(i, "Skip", Color.LightBlue, Color.Black);
                        logAction($"테스트 {proc.name} (ID: {proc.id}) **스킵됨**");
                        skipCount++;
                        continue;
                    }

                    logAction($"▶ TEST_BEGIN: {proc.name} (ID: {proc.id})");

                    var libraryDict = ((IEnumerable<dynamic>)workspace.libraries)
                        .FirstOrDefault(lib => lib.id == proc.libid) as IDictionary<string, object>;

                    if (libraryDict == null)
                    {
                        logAction($"오류: 프로세스 '{proc.name}'의 libid '{proc.libid}'에 해당하는 라이브러리를 찾을 수 없습니다.");
                        continue;
                    }

                    proc.library = libraryDict;
                    if (proc.library is ExpandoObject)
                        proc.library = (IDictionary<string, object>)proc.library;

                    if (proc.library is IDictionary<string, object> libraryDictFinal2 &&
                        libraryDictFinal2.ContainsKey("classtype") &&
                        libraryDictFinal2["classtype"] is Type classtype)
                    {
                        var method = classtype.GetMethod(proc.id);
                        if (method != null)
                        {
                            // ── 4슬롯: slot_group별 슬롯 실행 (기존 경로와 분리) ──
                            if (slot4)
                            {
                                if (RunProcSlot4(i, proc, libraryDictFinal2["instance"], method)) { wasCanceled = true; break; }
                                continue;
                            }

                            var swItem = System.Diagnostics.Stopwatch.StartNew();   // 항목별 걸린시간
                            try
                            {
                                // 이 단계 메타를 param에 주입 (TCPMSG의 retmsg 구성 등에 사용)
                                if (proc.param != null)
                                {
                                    UpsertParamMeta(proc.param, "proc_id", proc.id);
                                    UpsertParamMeta(proc.param, "proc_name", proc.name);
                                }

                                object res;
                                var parameters = method.GetParameters();

                                if (parameters.Length == 3)
                                {
                                    res = method.Invoke(libraryDictFinal2["instance"], new object[] { libraries, proc.param, logAction });
                                }
                                else if (parameters.Length == 4)
                                {
                                    res = method.Invoke(libraryDictFinal2["instance"], new object[] {
                                libraries,
                                proc.param,
                                logAction,
                                new Func<string, bool>(mainForm.ShowKeyboardConfirmationForm)
                            });
                                }
                                else
                                {
                                    logAction($"지원되지 않는 메서드 시그니처: {method.Name} (파라미터 수: {parameters.Length})");
                                    // 진행중 표시 되돌림
                                    mainForm.UpdateDataGridView(i, "FAIL", Color.Red);
                                    failCount++;
                                    mainForm.AddFailRecord(proc, null, "지원되지 않는 메서드 시그니처");
                                    continue;
                                }

                                // 실행 결과 확인
                                bool isSuccess = res is IDictionary<string, object> resultDict &&
                                                 resultDict.ContainsKey("success") &&
                                                 Convert.ToBoolean(resultDict["success"]);

                                // retmsg 있으면 결과셀 Tag에 저장(더블클릭 팝업 용)
                                string capturedRetmsg = "";
                                if (res is IDictionary<string, object> dictRes && dictRes.TryGetValue("retmsg", out var rmObj))
                                {
                                    string rm = rmObj?.ToString() ?? "";
                                    capturedRetmsg = rm;
                                    try
                                    {
                                        mainForm.Invoke(new Action(() =>
                                        {
                                            var resultCell = mainForm.dataGridView1.Rows[i].Cells[2];
                                            resultCell.Tag = rm;
                                        }));
                                    }
                                    catch { /* ignore */ }
                                }

                                mainForm.UpdateDataGridView(i, isSuccess ? "OK" : "FAIL", isSuccess ? Color.Green : Color.Red);
                                logAction($"◀ TEST_FINISH: {proc.name} -> {(isSuccess ? "OK" : "FAIL")} ({swItem.Elapsed.TotalSeconds:0.00}초)");

                                if (isSuccess) okCount++;
                                else { failCount++; mainForm.AddFailRecord(proc, capturedRetmsg, null); }

                                // X결합 FAIL → 남은 X 스킵하고 Y부터, Y결합/무태그 FAIL → 즉시 중단
                                bool isXCoupling = FailInfoForm.ExtractCoupling((string)(proc.name ?? "")) == "X결합";
                                if (!isSuccess && isXCoupling && !mainForm.checkBox_Process.Checked)
                                    xGroupFailed = true;   // 이후 X결합 항목 스킵 → 다음 Y결합부터

                                // 실패시 즉시 중단 옵션 (Y결합/무태그만)
                                if (!isSuccess && !mainForm.checkBox_Process.Checked && !isXCoupling)
                                {
                                    logAction($"테스트 {proc.name}에서 실패했습니다. 검사 종료.");
                                    throw new OperationCanceledException("Fail stop (중간 실패로 중단)");
                                }
                            }
                            catch (OperationCanceledException oce)
                            {
                                logAction($"실행 중단: {oce.Message}");
                                wasCanceled = true; // 팝업/저장 금지
                                break;
                            }
                            catch (Exception ex)
                            {
                                logAction($"실행 중 오류 발생: {ex.Message}");
                                // 실패 취급
                                mainForm.UpdateDataGridView(i, "FAIL", Color.Red);
                                failCount++;
                                mainForm.AddFailRecord(proc, null, $"예외: {ex.Message}");
                                // X결합 예외 → 남은 X 스킵하고 Y부터, Y결합/무태그 → 중단
                                bool isXCouplingEx = FailInfoForm.ExtractCoupling((string)(proc.name ?? "")) == "X결합";
                                if (isXCouplingEx && !mainForm.checkBox_Process.Checked)
                                    xGroupFailed = true;
                                if (!mainForm.checkBox_Process.Checked && !isXCouplingEx)
                                {
                                    wasCanceled = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            logAction($"오류: 메서드 {proc.id}를 {classtype.FullName}에서 찾을 수 없습니다.");
                            mainForm.UpdateDataGridView(i, "FAIL", Color.Red);
                            failCount++;
                            mainForm.AddFailRecord(proc, null, "메서드를 찾을 수 없습니다.");
                        }
                    }
                    else
                    {
                        logAction($"오류: 테스트 {proc.name}의 라이브러리 정보가 올바르지 않습니다.");
                        mainForm.UpdateDataGridView(i, "FAIL", Color.Red);
                        failCount++;
                        mainForm.AddFailRecord(proc, null, "라이브러리 정보 오류");
                    }
                }

                if (wasCanceled) break;

                // repeat_count 라운드 간 대기
                if (repeatCount > 1 && round < repeatCount - 1)
                {
                    logAction($"[전체 반복 {round + 1}/{repeatCount}] 결과: OK={okCount}, FAIL={failCount}, SKIP={skipCount}");
                    for (int _d = repeatDelay; _d > 0 && !token.IsCancellationRequested; _d -= 200)
                        System.Threading.Thread.Sleep(Math.Min(200, _d));
                }

                } // end repeat_count loop

                if (wasCanceled)
                {
                    RollbackSerialAndMacIfFail(logAction);
                    if (failCount > 0)
                    {
                        mainForm.ShowFailForm();
                        mainForm.failForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                    }
                    return;
                }

                logAction("모든 테스트 실행 완료");
                mainForm.Invoke(() => mainForm.label_cycle.Text = $"완료 ({repeatCount}회)");
                string finalResult = failCount == 0 ? "PASS" : "FAIL";
                var elapsed = DateTime.Now - testStartTime;
                string elapsedStr = elapsed.TotalMinutes >= 1
                    ? $"{(int)elapsed.TotalMinutes}m{elapsed.Seconds:D2}s"
                    : $"{elapsed.TotalSeconds:F1}s";
                logAction($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                string snSummary = string.IsNullOrEmpty(mainForm.serialNumberY)
                    ? $"[S/N: {mainForm.serialNumber}]"
                    : $"[X: {mainForm.serialNumber}, Y: {mainForm.serialNumberY}]";
                logAction($"test time: {elapsedStr} {snSummary}");
                if (!string.IsNullOrEmpty(mainForm.serialNumberY))
                    logAction($"X축: {mainForm.serialNumber} {finalResult}, Y축: {mainForm.serialNumberY} {finalResult}");

                // 최종 요약/팝업/저장
                if (!string.IsNullOrEmpty(mainForm.serialNumberY))
                {
                    mainForm.Invoke(() => mainForm.label_Serial.Text = $"X축: {mainForm.serialNumber} {finalResult}\nY축: {mainForm.serialNumberY} {finalResult}");
                }

                // 모션 결과 저장
                bool isMotion = !string.IsNullOrEmpty(mainForm.serialNumberY) ||
                    ((string)(workspace?.active_project ?? "")).Contains("motion", StringComparison.OrdinalIgnoreCase);
                string pendingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pending_x_result.json");

                // single 모드: pending 없이 기존 SaveLog로 바로 저장/업로드
                if (isMotion && curSingleMode && failCount == 0)
                {
                    mainForm.SaveLog(mainForm.serialNumber, mainForm.macAddress ?? "Not Use", null);
                    logAction($"단독 검사 저장: {mainForm.serialNumber}");
                    mainForm.Invoke(() => mainForm.label_passSaved.Text = $"PASS SAVED: {mainForm.serialNumber}");
                    mainForm.ShowPassForm();
                    mainForm.passForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                }
                else if (isMotion && curSingleMode && failCount > 0)
                {
                    RollbackSerialAndMacIfFail(logAction);
                    mainForm.ShowFailForm();
                    mainForm.failForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                }
                else if (isMotion && slot4)
                {
                    FinishSlot4();
                }
                else if (isMotion && failCount == 0)
                {
                    // X축 결과 수집 (uart_id 없는 proc = X축)
                    var xResult = new Dictionary<string, object>();
                    var yResult = new Dictionary<string, object>();
                    for (int i = 0; i < mainForm.dataGridView1.Rows.Count; i++)
                    {
                        var testName = mainForm.dataGridView1.Rows[i].Cells[1].Value?.ToString();
                        var testResult = mainForm.dataGridView1.Rows[i].Cells[2].Value?.ToString();
                        if (string.IsNullOrEmpty(testName) || string.IsNullOrEmpty(testResult)) continue;
                        string val = (testResult == "OK") ? "OK" : "FAIL";

                        // Y축 proc 구분: 이름에 "#0-Y", "#7", "#8", "#9", "#10", "#11", "#12" 포함
                        if (testName.StartsWith("#0-Y") || testName.StartsWith("#7 ") || testName.StartsWith("#8 ") ||
                            testName.StartsWith("#9 ") || testName.StartsWith("#10 ") || testName.StartsWith("#11 ") || testName.StartsWith("#12 "))
                            yResult[testName] = val;
                        else
                            xResult[testName] = val;
                    }

                    // 1) 이번 X축 retmsg 수집 (grid에서 X축 행만)
                    var xRetmsg = new JArray();
                    if (mainForm.InvokeRequired)
                        mainForm.Invoke(new Action(() => CollectRetmsgByAxis(mainForm, false, xRetmsg)));
                    else
                        CollectRetmsgByAxis(mainForm, false, xRetmsg);

                    // pending 파일에 X축 결과 + retmsg 저장 (마감 사이클 X=Null 제외)
                    if (mainForm.serialNumber != "Null")
                    {
                        var pendingData = new JObject
                        {
                            ["serial"] = mainForm.serialNumber,
                            ["time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            ["results"] = JObject.FromObject(xResult),
                            ["retmsg"] = xRetmsg
                        };
                        File.WriteAllText(pendingPath, pendingData.ToString(), new System.Text.UTF8Encoding(false));
                        logAction($"X축 결과 임시 저장: {mainForm.serialNumber} ({xResult.Count}항목)");
                        mainForm.Invoke(() => mainForm.label_Serial.Text += $"\n펜딩(X축완료): {mainForm.serialNumber}");
                    }

                    // 2) 이전 pending 있으면 → Y축 결과와 합쳐서 업로드
                    if (!string.IsNullOrEmpty(mainForm.serialNumberY) && yResult.Count > 0)
                    {
                        if (_previousPendingData != null)
                        {
                            string prevSerial = _previousPendingData["serial"]?.ToString() ?? "";
                            if (prevSerial == mainForm.serialNumberY)
                            {
                                // Y축 retmsg 수집 (현재 grid에서 Y축 행만)
                                var yRetmsg = new JArray();
                                if (mainForm.InvokeRequired)
                                    mainForm.Invoke(new Action(() => CollectRetmsgByAxis(mainForm, true, yRetmsg)));
                                else
                                    CollectRetmsgByAxis(mainForm, true, yRetmsg);

                                // 합산: pending X retmsg + 현재 Y retmsg
                                var prevRetmsg = _previousPendingData["retmsg"] as JArray ?? new JArray();
                                var combinedRetmsg = new JArray();
                                foreach (var item in prevRetmsg) combinedRetmsg.Add(item);
                                foreach (var item in yRetmsg) combinedRetmsg.Add(item);

                                // SaveLog 대신 직접 저장 (올바른 시리얼 + 합산 retmsg)
                                mainForm.SaveLogDirect(mainForm.serialNumberY, combinedRetmsg, logAction);
                                logAction($"완성 업로드: {mainForm.serialNumberY} (X+Y {combinedRetmsg.Count}항목)");
                                mainForm.Invoke(() => mainForm.label_passSaved.Text = $"PASS SAVED: {mainForm.serialNumberY}");
                            }
                            else
                            {
                                logAction($"[경고] pending 시리얼 불일치: pending={prevSerial}, Y축={mainForm.serialNumberY}");
                            }
                            _previousPendingData = null;
                        }
                        else
                        {
                            logAction($"Y축 결과 있으나 이전 pending 없음 (첫 사이클)");
                        }
                    }

                    // 마감 사이클(X=Null): 완성 후 pending 파일 삭제 → 같은 보드 재투입/시작사이클 누락 시 중복 완성 차단
                    // (중간 사이클은 위 X검사가 파일을 새 번호로 덮으므로 삭제 금지 — 마감은 X=Null이라 파일 stale → 삭제 안전)
                    if (mainForm.serialNumber == "Null")
                    {
                        try { if (File.Exists(pendingPath)) File.Delete(pendingPath); } catch { }
                        logAction("[INFO] 마감 사이클: pending 파일 삭제 (재투입 중복 차단)");
                    }

                    mainForm.ShowPassForm();
                    mainForm.passForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                }
                else if (isMotion && failCount > 0)
                {
                    RollbackSerialAndMacIfFail(logAction);
                    mainForm.ShowFailForm();
                    mainForm.failForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                }
                else if (failCount == 0)
                {
                    // 기존 방식 (비모션)
                    var result = new Dictionary<string, object>();
                    for (int i = 0; i < mainForm.dataGridView1.Rows.Count; i++)
                    {
                        var testName = mainForm.dataGridView1.Rows[i].Cells[1].Value?.ToString();
                        var testResult = mainForm.dataGridView1.Rows[i].Cells[2].Value?.ToString();
                        if (string.IsNullOrEmpty(testName) || string.IsNullOrEmpty(testResult)) continue;
                        result[testName] = (testResult == "OK") ? "OK" : "FAIL";
                    }
                    mainForm.SaveLog(serialNumber, macAddress, result);
                    mainForm.Invoke(() => mainForm.label_passSaved.Text = $"PASS SAVED: {mainForm.serialNumber}");
                    mainForm.ShowPassForm();
                    mainForm.passForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                }
                else
                {
                    RollbackSerialAndMacIfFail(logAction);
                    mainForm.ShowFailForm();
                    mainForm.failForm.SetResult($"총 결과: OK = {okCount}, FAIL = {failCount}, SKIP = {skipCount}");
                }
            }
            catch (Exception ex)
            {
                logAction($"실행 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                // 모션지그 수평 복귀 (FAIL/예외 시 기울어진 상태 방지)
                try
                {
                    // 수평 복귀는 MotionNewInternal에서 처리 (검사 완료 후 0도 복귀)
                    // finally에서는 추가 이동 불필요
                }
                catch { /* ignore */ }

                // 모든 통신 닫기
                try
                {
                    foreach (var kv in libraries)
                    {
                        if (kv.Value is IComm c)
                        {
                            try { c.Disconnect(); } catch { /* ignore */ }
                        }
                    }
                }
                catch { /* ignore */ }

                // IProcess End 호출
                try
                {
                    foreach (var iter in libraries)
                    {
                        if (iter.Value is IProcess process)
                        {
                            try { process.End(); } catch { /* ignore */ }
                        }
                    }
                }
                catch { /* ignore */ }

                // Stop/완료 후 Start 버튼 활성화
                try { mainForm.Invoke(() => { mainForm.button_start.Enabled = true; mainForm.buttonStop.Enabled = false; }); } catch { }
            }
        }






        public void InitializeCTSP(IDictionary<string, object> libraries)
        {
            // 로그를 통해 libraries가 제대로 로드되었는지 확인
            if (LogConfig.IsTest) Log($"현재 libraries의 키 목록: {string.Join(", ", libraries.Keys)}");

            // 'ctsp' 항목 확인
            if (libraries.ContainsKey("ctsp"))
            {
                // ctsp가 존재하면 IProtocol로 캐스팅
                ctsp_ = libraries["ctsp"] as IProtocol;

                // ctsp_가 null이라면 초기화가 되지 않았다는 의미
                if (ctsp_ == null)
                {
                    //Log("CTSP 인스턴스를 찾을 수 없거나 초기화되지 않았습니다.");
                    throw new InvalidOperationException("CTSP 인스턴스를 찾을 수 없거나 초기화되지 않았습니다.");
                }

                // PreRun을 호출하여 초기화
                try
                {
                    ctsp_.PreRun(libraries);
                    if (LogConfig.IsTest) Log("CTSP 라이브러리 초기화 완료.");
                }
                catch (Exception ex)
                {
                    //Log($"CTSP 라이브러리 초기화 중 오류 발생: {ex.Message}");
                    throw new InvalidOperationException("CTSP 라이브러리 초기화 중 오류 발생.", ex);
                }
            }
            else
            {
                // 예외를 던지기 전에 로그를 추가하여 어떤 상황인지 확인
                //Log("CTSP 라이브러리('ctsp')가 'libraries'에 존재하지 않습니다.");
                throw new KeyNotFoundException("CTSP 라이브러리('ctsp')가 'libraries'에 존재하지 않습니다.");
            }
        }






        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                Log("검사 중단 요청됨 (현재 항목 완료 후 종료)");

                // Start는 실제 종료(finally) 후 활성화
                buttonStop.Enabled = false;
            }
        }

        public DialogResult ShowUserConfirmDialog(string comment)
        {
            using (var form = new OKNGmsg(comment))
            {
                return form.ShowDialog(); // OK, Cancel, Retry 중 하나
            }
        }

        public bool ShowKeyboardConfirmationForm(string comment)
        {
            DialogResult result = DialogResult.OK;
            if (InvokeRequired)
                Invoke(new Action(() => { result = ShowLargeConfirmDialog(comment); }));
            else
                result = ShowLargeConfirmDialog(comment);
            return result == DialogResult.OK;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            listBox_Log.Items.Clear();


        }

        private void button_Copy_Click(object sender, EventArgs e)
        {
            PerformLogCopy(silent: false);
        }

        private void button_CopyAll_Click(object sender, EventArgs e)
        {
            if (listBox_Log.Items.Count == 0)
            {
                MessageBox.Show("로그가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                var sb = new StringBuilder();
                foreach (var item in listBox_Log.Items)
                    sb.AppendLine(item?.ToString() ?? "");
                Clipboard.SetText(sb.ToString());
                MessageBox.Show($"전체 로그 {listBox_Log.Items.Count}줄 복사 완료", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"복사 중 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowPassForm(string? message = null)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowPassForm(message)));
                return;
            }

            using (var dlg = new PassForm())
            {
                // 표시 위치/포커스 설정
                dlg.StartPosition = FormStartPosition.CenterParent; // Owner 기준 중앙
                dlg.TopMost = true;               // 항상 위
                dlg.ShowInTaskbar = false;        // 작업표시줄 숨김(선택)

                // 결과 텍스트가 있으면 전달(폼에 SetResult(string) 메서드가 있을 때)
                try { dlg.GetType().GetMethod("SetResult")?.Invoke(dlg, new object?[] { message }); } catch { /* 무시 */ }

                dlg.ShowDialog(this);             // ★ Owner 지정: this
            }
        }

        private void ShowFailForm(string? message = null)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowFailForm(message)));
                return;
            }

            using (var dlg = new FailForm())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.TopMost = true;
                dlg.ShowInTaskbar = false;

                try { dlg.GetType().GetMethod("SetResult")?.Invoke(dlg, new object?[] { message }); } catch { /* 무시 */ }

                dlg.ShowDialog(this);
            }

            // FailForm OK 후, fail_detail=true 이고 불량이 있으면 불량보기 자동 표시 (pSMC 이식)
            if (_failDetailPopup && _failRecords.Count > 0)
            {
                int idx = _autoFailInfoIdx >= 0 ? _autoFailInfoIdx : _failRecords.Count - 1;
                _autoFailInfoIdx = -1;
                ShowFailInfo(idx);
            }
        }

        // 불량 1건 누적 (검사 루프 FAIL 지점에서 호출) — pSMC 이식
        internal void AddFailRecord(dynamic proc, string retmsg, string errorMsg)
        {
            string item = "";
            try { item = (string)(proc?.name ?? ""); } catch { }

            // 결합 그룹당 첫 FAIL만 기록 — X결합/Y결합 연쇄 FAIL 노이즈 방지 (무태그=보드검사는 전부 기록)
            string coupling = FailInfoForm.ExtractCoupling(item);
            if (!string.IsNullOrEmpty(coupling) && _failRecords.Exists(fr => fr.Coupling == coupling))
                return;

            // 시리얼: Y결합 항목=Y위치 보드(serialNumberY), 그 외(X결합/무태그)=X(serialNumber)
            string recSerial = (coupling == "Y결합" && !string.IsNullOrEmpty(serialNumberY))
                ? serialNumberY : (serialNumber ?? "");

            // fail_tag: proc.param["fail_tag"] (선택적)
            string failTag = "";
            try
            {
                var pd = proc?.param as IDictionary<string, object>;
                if (pd != null && pd.TryGetValue("fail_tag", out var ft)) failTag = ft?.ToString() ?? "";
            }
            catch { }

            string summary, detail;
            if (!string.IsNullOrEmpty(errorMsg))
            {
                summary = string.IsNullOrWhiteSpace(failTag) ? errorMsg : failTag;
                detail = errorMsg;
            }
            else
            {
                FailInfoForm.BuildFailSummaryDetail(retmsg, failTag, out summary, out detail);
            }

            _failRecords.Add(new FailRecord
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Serial = recSerial,
                Coupling = coupling,  // 시료 2종 구분(X결합/Y결합)
                Item = item,
                Summary = summary,
                Detail = detail
            });
        }

        // 불량보기 창 표시 (자동/수동 공용) — pSMC 이식
        internal void ShowFailInfo(int highlightIdx = -1)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => ShowFailInfo(highlightIdx)));
                return;
            }
            if (_failRecords.Count == 0)
            {
                MessageBox.Show("불량 없음", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new FailInfoForm(_failRecords, highlightIdx))
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ShowDialog(this);
            }
        }

        private void button_expand_Click(object sender, EventArgs e)
        {
            ToggleWindowSize();
        }

        private void button_ChangeInspector_Click(object sender, EventArgs e)
        {
            try
            {
                // 현재 값 읽어 기본값으로 보여주기
                string current = IniRead("General", "Inspector", "").Trim();

                // 간단 입력창(하위폼 생성 없이)
                string input = Interaction.InputBox("새 검사자 이름을 입력하세요.", "검사자 변경", current);

                // 사용자가 취소했거나 공백만 입력한 경우: 아무 것도 하지 않음
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("변경이 취소되었습니다.", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 저장 + 라벨 갱신
                SaveInspectorToIni(input.Trim());

                // 확인 안내
                MessageBox.Show($"검사자가 '{input.Trim()}'(으)로 변경되었습니다.", "완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("검사자 변경 중 오류가 발생했습니다.\n" + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Fail일 경우 Serial Number와 Mac Address 텍스트 파일 마지막 줄을 지우는 함수
        private void RemoveLastLineFromFile(string path, Action<string> log)
        {
            try
            {
                if (!File.Exists(path))
                {
                    log($"롤백 스킵: '{Path.GetFileName(path)}' 가 없습니다.");
                    return;
                }

                var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                var lines = File.ReadAllLines(path, enc).ToList();
                if (lines.Count == 0)
                {
                    log($"롤백 스킵: '{Path.GetFileName(path)}' 가 비어 있습니다.");
                    return;
                }

                // 뒤에서부터 첫 “비어있지 않은” 줄을 제거, 그 위쪽의 공백 꼬리도 정리
                int idx = lines.Count - 1;
                while (idx >= 0 && string.IsNullOrWhiteSpace(lines[idx])) { lines.RemoveAt(idx); idx--; }
                if (idx >= 0)
                {
                    lines.RemoveAt(idx);
                    // 제거 후에 뒤쪽 공백 라인도 깔끔하게 정리
                    while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);
                    File.WriteAllLines(path, lines, enc);
                    log($"롤백 완료: '{Path.GetFileName(path)}' 마지막 번호를 되돌렸습니다.");
                }
                else
                {
                    log($"롤백 스킵: '{Path.GetFileName(path)}' 에 유효한 줄이 없습니다.");
                }
            }
            catch (Exception ex)
            {
                log($"롤백 실패: {Path.GetFileName(path)} - {ex.Message}");
            }
        }

        // FAIL일 때 두 파일 모두 롤백 + _lastSerialX 복원
        private string _prevLastSerialX = ""; // 롤백용 이전 _lastSerialX 백업
        private void RollbackSerialAndMacIfFail(Action<string> log)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string serialPath = Path.Combine(baseDir, GetSerialFileName());
            string macPath = Path.Combine(baseDir, "MacAddress.txt");

            RemoveLastLineFromFile(serialPath, log);
            if (IsMacWriteEnabledFromConfig())
                RemoveLastLineFromFile(macPath, log); // MacAddressWrite=False면 스킵

            // _lastSerialX 롤백 (중간 취소/FAIL 시 이전 값 복원)
            // v0.6.8: 빈 문자열이면 파일 생성/덮어쓰기 생략 (보드 single 검사에서 빈 파일 생성 방지)
            _lastSerialX = _prevLastSerialX;
            if (!string.IsNullOrEmpty(_lastSerialX))
                try { File.WriteAllText(Path.Combine(baseDir, GetLastSerialXFileName()), _lastSerialX, new System.Text.UTF8Encoding(false)); } catch { }
            // 4슬롯: X2 슬롯 기억값도 롤백
            if (_slot4)
            {
                _lastSerialX2 = _prevLastSerialX2;
                if (!string.IsNullOrEmpty(_lastSerialX2))
                    try { File.WriteAllText(Path.Combine(baseDir, GetLastSerialX2FileName()), _lastSerialX2, new System.Text.UTF8Encoding(false)); } catch { }
            }
        }

        private bool IsMacWriteEnabledFromConfig()
        {
            try
            {
                string cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "config.ini");
                return ReadIniBool(cfg, "General", "MacAddressWrite", true);
            }
            catch { return true; }
        }

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

        private void button_OpenWorkspace_Click(object sender, EventArgs e)
        {
            if (_isRunning) { Log("[INFO] 검사 중에는 워크스페이스를 열 수 없습니다."); return; } // F0(v05)
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "워크스페이스 열기";
                // 기본 필터: *workspace*.json만 표시 (deps/runtimeconfig/error_table 등 비워크스페이스 json 숨김)
                ofd.Filter = "Workspace (*workspace*.json)|*workspace*.json|All JSON (*.json)|*.json|All files (*.*)|*.*";
                ofd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    // 1) 기존 표시 지우기
                    dataGridView1.Rows.Clear();
                    label_CurrentTest.Text = "";

                    // 2) 스냅샷도 초기화
                    _libSnapshot = null;

                    // 3) 새 워크스페이스 로드
                    var newWs = MainLoadWorkspace(ofd.FileName, Log);
                    if (newWs != null)
                    {
                        workspace = newWs;
                        _currentWorkspacePath = ofd.FileName;   // ★ 새 경로 기억

                        try
                        {
                            // 라벨 추가
                            label_CurrentTest.Text = workspace.projects[0].name;
                            _logProjFolder = ((string)workspace.projects[0].name).Replace(" ", "");
                        }
                        catch
                        {
                            label_CurrentTest.Text = "(no project)";
                        }

                        // log_level 재설정
                        try
                        {
                            string ll = workspace.log_level;
                            if (!string.IsNullOrEmpty(ll)) Cantops.FlexFab.LogConfig.LogLevel = ll;
                        }
                        catch { }

                        // LastWorkspace 저장 (파일명만)
                        IniWrite("General", "LastWorkspace", Path.GetFileName(ofd.FileName));

                        Log($"[INFO] workspace 변경: {Path.GetFileName(ofd.FileName)}");
                    }
                    else
                    {
                        MessageBox.Show("새 워크스페이스 로드에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // v0.6.9: 우클릭 메뉴에서 호출 — 현재 워크스페이스 파일을 다이얼로그 없이 재로드
        private void ReloadCurrentWorkspace()
        {
            if (_isRunning) { Log("[INFO] 검사 중에는 워크스페이스를 재로드할 수 없습니다."); return; } // F0(v05)
            if (string.IsNullOrEmpty(_currentWorkspacePath) || !File.Exists(_currentWorkspacePath))
            {
                MessageBox.Show("재로드할 워크스페이스가 없습니다.\nOpen Workspace로 먼저 파일을 열어주세요.",
                    "재로드 불가", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 1) 기존 표시 지우기
            dataGridView1.Rows.Clear();
            label_CurrentTest.Text = "";

            // 2) 스냅샷도 초기화
            _libSnapshot = null;

            // 3) 동일 경로에서 재로드
            var newWs = MainLoadWorkspace(_currentWorkspacePath, Log);
            if (newWs != null)
            {
                workspace = newWs;
                // _currentWorkspacePath는 유지

                try
                {
                    label_CurrentTest.Text = workspace.projects[0].name;
                    _logProjFolder = ((string)workspace.projects[0].name).Replace(" ", "");
                }
                catch
                {
                    label_CurrentTest.Text = "(no project)";
                }

                try
                {
                    string ll = workspace.log_level;
                    if (!string.IsNullOrEmpty(ll)) Cantops.FlexFab.LogConfig.LogLevel = ll;
                }
                catch { }

                // 재로드 시 테스트설정 런타임 오버라이드 초기화 — 파일값으로 원복
                _settingsOverridden = false;
                ApplyWorkspaceCachedUI();
                label_overrides.Text = "";

                Log($"[INFO] workspace 재로드: {Path.GetFileName(_currentWorkspacePath)}");
            }
            else
            {
                MessageBox.Show("워크스페이스 재로드에 실패했습니다.\nJSON 문법 오류 가능성을 확인하세요.",
                    "재로드 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 로그창 OwnerDraw: FAIL 줄 빨강+굵게, TEST_BEGIN/FINISH 마커 굵게 (pSMC 이식)
        private void listBox_Log_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            string text = listBox_Log.Items[e.Index]?.ToString() ?? "";

            bool isFail =
                text.IndexOf("-> FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("[NG]", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("→ NG", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isTestMarker =
                text.IndexOf("TEST_BEGIN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("TEST_FINISH", StringComparison.OrdinalIgnoreCase) >= 0;

            e.DrawBackground(); // 선택 하이라이트 배경 유지

            Font font = e.Font;
            if (isFail || isTestMarker)
                font = _logBoldFont ??= new Font(e.Font, FontStyle.Bold);

            Color color = isFail ? Color.Red : e.ForeColor;

            TextRenderer.DrawText(e.Graphics, text, font, e.Bounds, color,
                                  TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            e.DrawFocusRectangle();
        }

        // ═══════════ 테스트 설정 (F2 런타임 오버라이드 + 저장) — pSMC 이식 ═══════════
        // 진입 게이트: 검사 동작 SETTING이라 여기서 후속 패스워드 확인을 추가할 예정.
        private void OpenTestSettings()
        {
            // F0(v05): 검사 중 설정 변경 금지 — 실행 중 test_mode 등이 바뀌면 판정 경로가 바뀜
            if (_isRunning) { Log("[INFO] 검사 중에는 테스트 설정을 열 수 없습니다."); return; }
            // 다이얼로그 진입은 자유(적용=런타임). 파일 [저장]만 패스워드 확인(ShowTestSettingsDialog 내부).
            ShowTestSettingsDialog();
        }

        // 패스워드 입력 다이얼로그(마스킹). 취소 시 null. 기대값: 워크스페이스 JSON test_save_password (기본 0906)
        private string PromptPassword()
        {
            using var dlg = new Form
            {
                Text = "설정 저장 — 패스워드",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(320, 160)
            };
            var lbl = new Label { Text = "저장하려면 패스워드를 입력하세요.", Location = new Point(16, 15), AutoSize = true };
            var txt = new TextBox { Location = new Point(16, 45), Width = 272, UseSystemPasswordChar = true };
            var ok = new Button { Text = "확인", Location = new Point(122, 82), Width = 80, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "취소", Location = new Point(208, 82), Width = 80, DialogResult = DialogResult.Cancel };
            dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;
            return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }

        // 런타임 오버라이드 다이얼로그 (적용=런타임 / 저장=파일). spec/proc.param은 대상 아님.
        private void ShowTestSettingsDialog()
        {
            if (workspace == null) { MessageBox.Show("워크스페이스가 로드되지 않았습니다."); return; }

            var fields = GetOverridableFields();
            // expected_fw_version 제외 — proc.param.tests[]에 중첩된 항목별 값이라 단일 오버라이드 불가(JSON에서 수정)
            fields.Remove("expected_fw_version");
            // fail_detail(불량보기 자동표시 on/off) 포함 — 목록에 없으면 fail_continue 다음에 추가
            if (!fields.Contains("fail_detail"))
            {
                int fi = fields.IndexOf("fail_continue");
                if (fi >= 0) fields.Insert(fi + 1, "fail_detail"); else fields.Add("fail_detail");
            }
            // serial_duplicate_check를 repeat_all_count 바로 위로 재배치 (표시 순서)
            if (fields.Contains("serial_duplicate_check") && fields.Contains("repeat_all_count"))
            {
                fields.Remove("serial_duplicate_check");
                fields.Insert(fields.IndexOf("repeat_all_count"), "serial_duplicate_check");
            }
            var editors = new Dictionary<string, Func<object>>();

            var dlg = new Form
            {
                Text = "테스트 설정 (적용=런타임 / 저장=파일)",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Size = new Size(470, 150 + fields.Count * 38)
            };

            int y = 15;
            foreach (var name in fields)
            {
                dlg.Controls.Add(new Label { Text = name, Location = new Point(16, y + 5), AutoSize = true });
                object cur = GetSettingValue(name);
                switch (name)
                {
                    case "log_level":
                        {
                            var cb = new ComboBox { Location = new Point(230, y), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
                            cb.Items.AddRange(new object[] { "MP_LOG", "TEST_LOG" });
                            cb.SelectedItem = (cur?.ToString() == "TEST_LOG") ? "TEST_LOG" : "MP_LOG";
                            dlg.Controls.Add(cb); editors[name] = () => cb.SelectedItem?.ToString() ?? "MP_LOG";
                            break;
                        }
                    case "test_mode":
                        {
                            // 0=dual(밀어내기 2개) / 1=single(1개, Y스킵)
                            var cb = new ComboBox { Location = new Point(230, y), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
                            cb.Items.AddRange(new object[] { "0  dual(2개)", "1  single(1개)" });
                            cb.SelectedIndex = (ToIntSafe(cur) == 1) ? 1 : 0;
                            dlg.Controls.Add(cb); editors[name] = () => (object)cb.SelectedIndex;
                            break;
                        }
                    case "serial_duplicate_check":
                    case "serial_prefix_check":
                        {
                            var cb = new ComboBox { Location = new Point(230, y), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
                            cb.Items.AddRange(new object[] { "0  안함", "1  경고팝업", "2  차단" });
                            cb.SelectedIndex = Math.Max(0, Math.Min(2, ToIntSafe(cur)));
                            dlg.Controls.Add(cb); editors[name] = () => (object)cb.SelectedIndex;
                            break;
                        }
                    case "repeat_all_count":
                        {
                            var nud = new NumericUpDown { Location = new Point(230, y), Width = 210, Minimum = 1, Maximum = 9999, Value = Math.Min(9999, Math.Max(1, ToIntSafe(cur))) };
                            dlg.Controls.Add(nud); editors[name] = () => (object)(int)nud.Value;
                            break;
                        }
                    case "serial_auto_increment":
                        {
                            var chk = new CheckBox { Location = new Point(230, y), AutoSize = true, Checked = ToBoolSafe(cur) };
                            dlg.Controls.Add(chk); editors[name] = () => (object)chk.Checked;
                            break;
                        }
                    case "fail_continue":
                    case "show_log":
                    case "fail_detail":
                    case "clear_log_on_run_start":
                        {
                            var chk = new CheckBox { Location = new Point(230, y), AutoSize = true, Checked = ToIntSafe(cur) != 0 };
                            dlg.Controls.Add(chk); editors[name] = () => (object)(chk.Checked ? 1 : 0);
                            break;
                        }
                    default: // 문자열 (expected_fw_version 등)
                        {
                            var tb = new TextBox { Location = new Point(230, y), Width = 210, Text = cur?.ToString() ?? "" };
                            dlg.Controls.Add(tb); editors[name] = () => (object)tb.Text;
                            break;
                        }
                }
                y += 38;
            }

            dlg.Controls.Add(new Label
            {
                Text = "※ [적용]=런타임만(재로드 시 초기화) / [저장]=현재 워크스페이스 파일에 영구 기록.\n※ spec(판정기준)은 JSON에서만 변경.",
                Location = new Point(16, y + 2),
                Size = new Size(430, 40),
                ForeColor = Color.DimGray
            });
            y += 48;

            var save = new Button { Text = "저장", Location = new Point(130, y), Width = 95 };
            var ok = new Button { Text = "적용", Location = new Point(235, y), Width = 95, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "취소", Location = new Point(340, y), Width = 95, DialogResult = DialogResult.Cancel };
            // [저장]: 패스워드 통과해야 다이얼로그 닫힘. 틀리면(또는 취소) 설정 화면 유지(재시도 가능)
            save.Click += (s, e) =>
            {
                string expected = "0906";
                try { string _p = (string)(workspace?.test_save_password ?? ""); if (!string.IsNullOrWhiteSpace(_p)) expected = _p.Trim(); } catch { }
                string entered = PromptPassword();
                if (entered == null) return;                       // 입력 취소 → 설정 화면 유지
                if (entered != expected)
                {
                    MessageBox.Show("패스워드가 일치하지 않습니다.\n다시 시도하세요.",
                        "저장 거부", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;                                         // 불일치 → 설정 화면 유지
                }
                dlg.DialogResult = DialogResult.Yes;               // 통과 → 닫고 저장
            };
            dlg.Controls.Add(save); dlg.Controls.Add(ok); dlg.Controls.Add(cancel);
            dlg.AcceptButton = ok; dlg.CancelButton = cancel;

            var result = dlg.ShowDialog(this);
            if (result != DialogResult.OK && result != DialogResult.Yes) return;

            // 적용·저장 모두 먼저 런타임 반영. 저장값을 모아 파일 기록에 재사용.
            var applied = new Dictionary<string, object>();
            foreach (var kv in editors)
            {
                try
                {
                    var val = kv.Value();
                    if (val is string s && string.IsNullOrWhiteSpace(s)) continue;
                    // F0(v05): 4슬롯 워크스페이스는 dual 전용 — test_mode=1 적용 거부
                    if (_slot4 && kv.Key == "test_mode" && ToIntSafe(val) == 1)
                    {
                        MessageBox.Show("4슬롯 지그는 dual 전용입니다.\ntest_mode=1(single)은 적용하지 않습니다.", "테스트 설정",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }
                    SetSettingValue(kv.Key, val);
                    applied[kv.Key] = val;
                }
                catch { }
            }

            _settingsOverridden = true;
            ApplyWorkspaceCachedUI();
            UpdateOverrideSummary(fields);

            if (result == DialogResult.Yes)
                SaveTestSettings(applied);           // 패스워드는 [저장] 버튼 클릭 시 이미 확인됨
            else
                Log("[테스트설정] 런타임 오버라이드 적용됨 (저장 안 됨)");
        }

        // 워크스페이스 캐시값을 실행 UI에 재적용 (MainForm_Load·재로드·테스트설정 적용에서 공유). VibeTilt 어댑트.
        private void ApplyWorkspaceCachedUI()
        {
            if (workspace == null) return;
            try { checkBox_Process.Checked = Convert.ToInt32(workspace.fail_continue) != 0; } catch { }
            try { string ll = (string)(workspace.log_level ?? ""); if (!string.IsNullOrEmpty(ll)) Cantops.FlexFab.LogConfig.LogLevel = ll; } catch { }
            try { _failDetailPopup = Convert.ToInt32(workspace.fail_detail) != 0; } catch { }
            try { _serialAutoIncrement = (bool)(workspace?.serial_auto_increment ?? false); } catch { }
            ApplySlotLayout(); // 4슬롯: slot_layout / slot_uarts / clear_log_on_run_start 재적용

            // show_log → 창 크기/로그창 (VibeTilt 값 1541/941 × 819)
            bool showLog = false;
            try { showLog = Convert.ToInt32(workspace.show_log) != 0; } catch { }
            if (showLog) { this.Size = new Size(1541, 819); listBox_Log.Visible = true; isWindowShrunk = false; }
            else { this.Size = new Size(941, 819); listBox_Log.Visible = false; isWindowShrunk = true; }

            // repeat_all_count → 회차 라벨
            try
            {
                int rc = Convert.ToInt32(((IDictionary<string, object>)workspace.projects[0]).TryGetValue("repeat_all_count", out var _rc) ? _rc : 1);
                label_cycle.Text = rc > 1 ? $"(repeat: {rc})" : "";
            }
            catch { label_cycle.Text = ""; }
        }

        // 편집 가능 필드 목록. workspace의 `_작업자_변경가능`(| 구분) 우선, 없으면 VibeTilt 기본셋.
        private List<string> GetOverridableFields()
        {
            var fields = new List<string>();
            try
            {
                var dict = (IDictionary<string, object>)workspace;
                if (dict.TryGetValue("_작업자_변경가능", out var raw) && raw != null)
                {
                    foreach (var f in raw.ToString().Split('|'))
                    {
                        var n = f.Trim();
                        if (n.Length > 0 && !fields.Contains(n)) fields.Add(n);
                    }
                }
            }
            catch { }

            if (fields.Count == 0)
            {
                fields.Add("log_level");
                fields.Add("show_log");
                fields.Add("fail_continue");
                fields.Add("fail_detail");
                fields.Add("serial_auto_increment");
                fields.Add("serial_duplicate_check");
                fields.Add("test_mode");
                fields.Add("repeat_all_count");
            }
            return fields;
        }

        private object GetSettingValue(string name)
        {
            try
            {
                if (name == "repeat_all_count")
                {
                    var proj = (IDictionary<string, object>)((IList<object>)workspace.projects)[0];
                    return proj.TryGetValue("repeat_all_count", out var v) ? v : 1;
                }
                var dict = (IDictionary<string, object>)workspace;
                return dict.TryGetValue(name, out var val) ? val : null;
            }
            catch { return null; }
        }

        private void SetSettingValue(string name, object value)
        {
            try
            {
                if (name == "repeat_all_count")
                {
                    var proj = (IDictionary<string, object>)((IList<object>)workspace.projects)[0];
                    proj["repeat_all_count"] = value;
                    return;
                }
                var dict = (IDictionary<string, object>)workspace;
                dict[name] = value;
            }
            catch { }
        }

        private void UpdateOverrideSummary(List<string> fields)
        {
            // 작게 한 줄 표시(오버라이드 적용 시에만). 상세는 로그 참조.
            try { label_overrides.Text = _settingsOverridden ? "⚙ 테스트설정 적용중(런타임)" : ""; }
            catch { }
        }

        private static int ToIntSafe(object o)
        {
            try { return Convert.ToInt32(o); } catch { return 0; }
        }
        private static bool ToBoolSafe(object o)
        {
            try { if (o is bool b) return b; return Convert.ToInt32(o) != 0; } catch { return false; }
        }

        // [저장]: 적용값을 현재 워크스페이스 JSON 파일에 영구 기록. 값 토큰만 치환(주석·포맷·타입 보존).
        //   ★spec/proc.param 제외. top-level 플래그 + repeat_all_count(첫 매치)만. 없는 키는 건너뜀.
        private void SaveTestSettings(Dictionary<string, object> values)
        {
            string wsPath = _currentWorkspacePath ?? "";
            if (string.IsNullOrEmpty(wsPath) || !File.Exists(wsPath))
            {
                MessageBox.Show($"워크스페이스 파일을 찾을 수 없습니다.\n{wsPath}", "설정 저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int updates = 0;
            var skipped = new List<string>();
            try
            {
                var enc = new System.Text.UTF8Encoding(false);
                string text = File.ReadAllText(wsPath, enc);

                foreach (var kv in values)
                {
                    string name = kv.Key;
                    var rx = new System.Text.RegularExpressions.Regex(
                        "(\"" + System.Text.RegularExpressions.Regex.Escape(name) +
                        "\"\\s*:\\s*)(true|false|-?\\d+(?:\\.\\d+)?|\"(?:[^\"\\\\]|\\\\.)*\")");
                    var m = rx.Match(text);
                    if (!m.Success) { skipped.Add(name); continue; }

                    var valGroup = m.Groups[2];
                    string oldTok = valGroup.Value;
                    string newTok = CoerceJsonValue(oldTok, kv.Value);
                    if (newTok == null || newTok == oldTok) continue;

                    text = text.Substring(0, valGroup.Index) + newTok + text.Substring(valGroup.Index + valGroup.Length);
                    updates++;
                    Log($"설정 저장: {name} {oldTok} → {newTok}");
                }

                if (updates == 0)
                {
                    string msg = "변경된 설정이 없어 저장하지 않았습니다.";
                    if (skipped.Count > 0) msg += $"\n(JSON에 없는 키는 건너뜀: {string.Join(", ", skipped)})";
                    MessageBox.Show(msg, "설정 저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                File.WriteAllText(wsPath, text, enc);
                string done = $"테스트 설정 저장 완료 ({updates}개) → {Path.GetFileName(wsPath)}";
                if (skipped.Count > 0) done += $"\n(JSON에 없어 건너뜀: {string.Join(", ", skipped)})";
                Log(done);
                MessageBox.Show(done, "설정 저장", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"설정 저장 실패: {ex.GetType().Name} - {ex.Message}");
                MessageBox.Show($"테스트 설정 저장 실패!\n\n{ex.Message}", "설정 저장 실패", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 기존 JSON 값 토큰의 타입(bool/숫자/문자열)을 유지하며 새 값을 JSON 토큰으로 변환. 불가/불필요면 null.
        private static string CoerceJsonValue(string oldTok, object newVal)
        {
            bool oldIsBool = oldTok == "true" || oldTok == "false";
            bool oldIsString = oldTok.Length >= 2 && oldTok[0] == '"';

            if (oldIsBool)
            {
                bool b = (newVal is bool bb) ? bb : (ToIntSafe(newVal) != 0);
                return b ? "true" : "false";
            }
            if (oldIsString)
            {
                string s = newVal?.ToString() ?? "";
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            }
            if (newVal is bool b2) return b2 ? "1" : "0";
            return ToIntSafe(newVal).ToString();
        }
    }
}
