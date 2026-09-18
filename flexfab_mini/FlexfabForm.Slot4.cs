using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace flexfab
{
    // ── 4슬롯(X1·X2·Y1·Y2) 확장 — PLAN_4슬롯_메인폼_병렬측정_구현계획_20260918_v01 §3 ──
    // 워크스페이스 최상위 slot_layout="4" 일 때만 활성. 없거나 "2"면 기존 2슬롯 동작 그대로.
    // 밀어내기 2레인: X1→Y1, X2→Y2. 슬롯 uart는 slot_uarts 표로 결정.
    public partial class MainForm
    {
        internal static readonly string[] SLOT_NAMES = { "X1", "X2", "Y1", "Y2" };            // 내부 키 (slot_uarts 매칭, __slot)
        internal static readonly string[] SLOT_LABELS = { "X1슬롯", "X2슬롯", "Y1슬롯", "Y2슬롯" }; // 표시명 (지그 표기와 동일: 그리드·로그·팝업·불량보기·결과 JSON)
        internal const int SLOT_X1 = 0, SLOT_X2 = 1, SLOT_Y1 = 2, SLOT_Y2 = 3;

        internal bool _slot4 = false;                       // slot_layout == "4"
        internal string[] _slotSerial = { "", "", "", "" }; // 슬롯별 시리얼 ("" = 빈 슬롯 → Skip)
        internal Dictionary<string, string> _slotUarts = new();   // "X1" → "uart_232" …
        internal bool[] _slotFailed = { false, false, false, false }; // 슬롯 단위 FAIL 제외
        private bool _clearLogOnRunStart = false;           // clear_log_on_run_start
        private static readonly JObject?[] _previousPendingSlot = { null, null }; // 레인별 pending (X1→Y1, X2→Y2)
        private static string _lastSerialX2 = "";           // X2 슬롯 이전 시리얼 (Y2 자동 채움)
        private string _prevLastSerialX2 = "";              // 롤백 백업

        // 4슬롯 실행 여부: 4슬롯 워크스페이스 + dual 모드. single 모드는 기존 X1/Y1 경로 사용
        internal bool Slot4Run => _slot4 && !IsSingleTestMode();

        private bool IsSingleTestMode()
        {
            try { return Convert.ToInt32(workspace?.test_mode ?? 0) == 1; } catch { return false; }
        }

        // 워크스페이스 로드/재로드/설정 적용 시 호출 — slot_layout·slot_uarts·clear_log 읽고 그리드 열 구성
        // ws: 읽을 워크스페이스 객체. MainLoadWorkspace는 지역변수를 넘김(멤버 workspace는 반환 후에야 갱신됨), 그 외는 멤버 사용
        private void ApplySlotLayout(dynamic ws = null)
        {
            ws ??= workspace;
            _slot4 = false;
            _slotUarts = new Dictionary<string, string>();
            try
            {
                var dict = (IDictionary<string, object>)ws;
                if (dict.TryGetValue("slot_layout", out var sl) && (sl?.ToString() ?? "") == "4") _slot4 = true;
                if (dict.TryGetValue("slot_uarts", out var su) && su is IDictionary<string, object> sud)
                    foreach (var kv in sud) _slotUarts[kv.Key] = kv.Value?.ToString() ?? "";
                _clearLogOnRunStart = dict.TryGetValue("clear_log_on_run_start", out var cl) && Convert.ToInt32(cl) != 0;
            }
            catch { }
            if (_slot4)
            {
                foreach (var s in SLOT_NAMES)
                    if (!_slotUarts.ContainsKey(s)) { Log($"[경고] slot_uarts에 {s} 없음 → 4슬롯 비활성"); _slot4 = false; }
            }
            ConfigureGridColumnsForSlots();
        }

        // 결과 열: 2슬롯 = "결과" 1열(Cells[2]) / 4슬롯 = X1·X2·Y1·Y2 4열(Cells[2..5]).
        // 그리드 폭(542)·위치·폼·오른쪽 컨트롤(검사자/시리얼/Mac/불량보기) 전부 불변 — 기존 폭 안에서 열만 재배분
        //  (행 머리 ▸ 열 숨김 41px + 번호 60→45 + 항목 300→235 → 슬롯 4열 × 60)
        private void ConfigureGridColumnsForSlots()
        {
            if (InvokeRequired) { Invoke(new Action(ConfigureGridColumnsForSlots)); return; }
            const int slotW = 60;
            bool hasExtra = dataGridView1.Columns.Contains("Slot_X2");
            if (_slot4 && !hasExtra)
            {
                dataGridView1.RowHeadersVisible = false;
                dataGridView1.Columns["Number"].Width = 45;
                dataGridView1.Columns["Name"].Width = 235;
                dataGridView1.Columns["Result"].HeaderText = SLOT_LABELS[SLOT_X1];
                dataGridView1.Columns["Result"].Width = slotW;
                foreach (var nm in new[] { "X2", "Y1", "Y2" })
                {
                    var col = new DataGridViewTextBoxColumn { Name = "Slot_" + nm, HeaderText = nm + "슬롯", Width = slotW, SortMode = DataGridViewColumnSortMode.NotSortable };
                    dataGridView1.Columns.Add(col);
                }
            }
            else if (!_slot4 && hasExtra)
            {
                foreach (var nm in new[] { "Slot_X2", "Slot_Y1", "Slot_Y2" })
                    if (dataGridView1.Columns.Contains(nm)) dataGridView1.Columns.Remove(nm);
                dataGridView1.RowHeadersVisible = true;
                dataGridView1.Columns["Number"].Width = 60;
                dataGridView1.Columns["Name"].Width = 300;
                dataGridView1.Columns["Result"].HeaderText = "결과";
                dataGridView1.Columns["Result"].Width = 100;
            }
            if (_slot4) SetSlotHeaders();
        }

        // 헤더에 슬롯명 + 시리얼 표시 (빈 슬롯은 슬롯명만)
        private void SetSlotHeaders()
        {
            if (InvokeRequired) { Invoke(new Action(SetSlotHeaders)); return; }
            if (!_slot4 || !dataGridView1.Columns.Contains("Slot_X2")) return;
            for (int s = 0; s < 4; s++)
            {
                var col = dataGridView1.Columns[2 + s];
                string sn = _slotSerial[s];
                col.HeaderText = string.IsNullOrEmpty(sn) ? SLOT_LABELS[s] : $"{SLOT_LABELS[s]}\n{sn}";
            }
        }

        // 슬롯 1칸 갱신 (4슬롯 전용). 번호셀 빨강(작업자 제외)이면 Skip
        public void UpdateSlotCell(int rowIndex, int slotIdx, string resultText, Color bgColor, Color? fgColor = null)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, int, string, Color, Color?>(UpdateSlotCell), rowIndex, slotIdx, resultText, bgColor, fgColor);
                return;
            }
            var numberCell = dataGridView1.Rows[rowIndex].Cells[0];
            var cell = dataGridView1.Rows[rowIndex].Cells[2 + slotIdx];
            if (numberCell.Style.BackColor == Color.Red)
            {
                cell.Value = "Skip"; cell.Style.BackColor = Color.LightBlue; cell.Style.ForeColor = Color.Black;
                return;
            }
            cell.Value = resultText;
            cell.Style.BackColor = bgColor;
            cell.Style.ForeColor = fgColor ?? Color.White;
        }

        // 해당 없음 표시 (X 항목의 Y열 등)
        public void MarkSlotCellNA(int rowIndex, int slotIdx)
        {
            if (InvokeRequired) { Invoke(new Action<int, int>(MarkSlotCellNA), rowIndex, slotIdx); return; }
            var cell = dataGridView1.Rows[rowIndex].Cells[2 + slotIdx];
            cell.Value = "—"; cell.Style.BackColor = Color.Gainsboro; cell.Style.ForeColor = Color.Gray;
        }

        // proc.param.slot_group → 실행 슬롯 인덱스. common=[] (1회), X=[0,1], Y=[2,3], all=[0,1,2,3]
        // slot_group 없으면 skip_coupling(X/Y)로 추정, 그것도 없으면 common
        internal static string GetSlotGroup(dynamic proc)
        {
            try
            {
                var pd = proc?.param as IDictionary<string, object>;
                if (pd != null)
                {
                    if (pd.TryGetValue("slot_group", out var sg) && sg != null) return sg.ToString();
                    if (pd.TryGetValue("skip_coupling", out var sc) && sc != null)
                    {
                        string c = sc.ToString().ToUpperInvariant();
                        if (c == "X" || c == "Y") return c;
                    }
                }
            }
            catch { }
            return "common";
        }

        internal static int[] SlotsOfGroup(string group) => group switch
        {
            "X" => new[] { SLOT_X1, SLOT_X2 },
            "Y" => new[] { SLOT_Y1, SLOT_Y2 },
            "all" => new[] { SLOT_X1, SLOT_X2, SLOT_Y1, SLOT_Y2 },
            _ => Array.Empty<int>(),
        };

        // 슬롯별 param 복제: uart_id·__serial·__slot 치환. 원본 param은 건드리지 않음
        internal IDictionary<string, object> CloneParamForSlot(dynamic proc, int slotIdx)
        {
            var src = proc?.param as IDictionary<string, object>;
            var dst = new Dictionary<string, object>();
            if (src != null) foreach (var kv in src) dst[kv.Key] = kv.Value;
            string slot = SLOT_NAMES[slotIdx];
            dst["uart_id"] = _slotUarts[slot];
            dst["__serial"] = _slotSerial[slotIdx];
            dst["__slot"] = slot;
            return dst;
        }

        private string GetLastSerialX2FileName() => GetLastSerialXFileName().Replace("last_serial_x_", "last_serial_x2_");
        private string GetPendingSlotPath(int lane) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"pending_x{lane + 1}_result.json");

        // 시리얼 입력 (4칸). X1·X2 신규 입력, Y1·Y2는 이전 사이클 X1·X2 자동 채움. 빈칸 = 그 슬롯 Skip
        private bool ShowQuadSerialDialog()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var serialPath = Path.Combine(baseDir, GetSerialFileName());

            string lastSerial = "00001";
            try
            {
                if (File.Exists(serialPath))
                {
                    var lines = File.ReadAllLines(serialPath, new System.Text.UTF8Encoding(false));
                    for (int i = lines.Length - 1; i >= 0; i--) { var t = lines[i].Trim(); if (t.Length > 0) { lastSerial = t; break; } }
                }
            }
            catch { }
            string serialPrefix = "";
            try { if (workspace != null) serialPrefix = Convert.ToString(workspace.serial_prefix) ?? ""; } catch { }
            if (!string.IsNullOrEmpty(serialPrefix) && !lastSerial.Contains("-")) lastSerial = serialPrefix + lastSerial;

            Func<string, int, string> shift = (val, delta) =>
            {
                string pfx = "", num = val; int d = val.LastIndexOf('-');
                if (d >= 0 && d < val.Length - 1) { pfx = val.Substring(0, d + 1); num = val.Substring(d + 1); }
                if (num.All(char.IsDigit) && long.TryParse(num, out long n)) { n = Math.Max(1, n + delta); return pfx + n.ToString().PadLeft(num.Length, '0'); }
                return val;
            };

            // Y 기본값: 이전 사이클 X1/X2 (프로젝트별 파일)
            _lastSerialX = ""; _lastSerialX2 = "";
            try { var p = Path.Combine(baseDir, GetLastSerialXFileName()); if (File.Exists(p)) _lastSerialX = File.ReadAllText(p, new System.Text.UTF8Encoding(false)).Trim(); } catch { }
            try { var p = Path.Combine(baseDir, GetLastSerialX2FileName()); if (File.Exists(p)) _lastSerialX2 = File.ReadAllText(p, new System.Text.UTF8Encoding(false)).Trim(); } catch { }

            var bigFont = new Font("맑은 고딕", 16);
            var midFont = new Font("맑은 고딕", 12);
            using var dlg = new Form
            {
                Text = "시리얼 입력 (4슬롯 밀어내기: X1슬롯→Y1슬롯, X2슬롯→Y2슬롯)",
                Size = new Size(620, 560), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false
            };
            var lblGuide = new Label
            {
                Text = "시작 사이클: Y1·Y2 비우기 → Y 스킵\r\n마감 사이클: X1·X2 비우기 → X 스킵\r\n정상 사이클: 4칸 입력 (Y는 이전 X 자동)\r\n빈칸 = 그 슬롯만 Skip",
                Location = new Point(20, 10), Size = new Size(330, 80), Font = new Font("맑은 고딕", 10),
                ForeColor = Color.DarkBlue, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.AliceBlue,
            };
            var txt = new TextBox[4];
            string[] defaults = { lastSerial, shift(lastSerial, 1), _lastSerialX, _lastSerialX2 };
            string[] labels = { "X1슬롯 시리얼", "X2슬롯 시리얼 (X1 + 1)", "Y1슬롯 시리얼 (이전 X1슬롯)", "Y2슬롯 시리얼 (이전 X2슬롯)" };
            var ctrls = new List<Control> { lblGuide };
            for (int s = 0; s < 4; s++)
            {
                int y = 100 + s * 78;
                ctrls.Add(new Label { Text = labels[s], Location = new Point(20, y), AutoSize = true, Font = midFont });
                var tb = new TextBox { Text = defaults[s], Location = new Point(20, y + 28), Width = 330, MaxLength = 11, Font = bigFont };
                tb.SelectionStart = tb.Text.Length;
                tb.GotFocus += (o, e) => { tb.SelectionStart = tb.Text.Length; };
                if (s >= 2 && string.IsNullOrEmpty(defaults[s])) tb.PlaceholderText = "(없음 - 시작 사이클)";
                var bm = new Button { Text = "-", Location = new Point(360, y + 26), Size = new Size(50, 40), Font = new Font("맑은 고딕", 16, FontStyle.Bold) };
                var bp = new Button { Text = "+", Location = new Point(418, y + 26), Size = new Size(50, 40), Font = new Font("맑은 고딕", 16, FontStyle.Bold) };
                bm.Click += (o, e) => { if (tb.Text.Trim().Length > 0) tb.Text = shift(tb.Text.Trim(), -1); };
                bp.Click += (o, e) => { if (tb.Text.Trim().Length > 0) tb.Text = shift(tb.Text.Trim(), 1); };
                txt[s] = tb; ctrls.Add(tb); ctrls.Add(bm); ctrls.Add(bp);
            }
            var btnPresetStart = new Button { Text = "시작 사이클 (Y 비우기)", Location = new Point(370, 10), Size = new Size(220, 30), Font = new Font("맑은 고딕", 10), ForeColor = Color.DarkRed };
            var btnPresetEnd = new Button { Text = "마감 사이클 (X 비우기)", Location = new Point(370, 50), Size = new Size(220, 30), Font = new Font("맑은 고딕", 10), ForeColor = Color.DarkRed };
            btnPresetStart.Click += (o, e) => { txt[SLOT_Y1].Text = ""; txt[SLOT_Y2].Text = ""; };
            btnPresetEnd.Click += (o, e) => { txt[SLOT_X1].Text = ""; txt[SLOT_X2].Text = ""; };
            var btnOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(180, 430), Size = new Size(160, 55), Font = bigFont };
            var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(360, 430), Size = new Size(160, 55), Font = bigFont };
            ctrls.AddRange(new Control[] { btnPresetStart, btnPresetEnd, btnOk, btnCancel });
            dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;
            dlg.Controls.AddRange(ctrls.ToArray());

            if (dlg.ShowDialog(this) != DialogResult.OK) return false;

            for (int s = 0; s < 4; s++) _slotSerial[s] = string.IsNullOrWhiteSpace(txt[s].Text) ? "" : txt[s].Text.Trim();
            // 기존 경로 호환: X1/Y1을 serialNumber/serialNumberY에도 반영 (로그·라벨·불량기록 폴백)
            serialNumber = string.IsNullOrEmpty(_slotSerial[SLOT_X1]) ? "Null" : _slotSerial[SLOT_X1];
            serialNumberY = _slotSerial[SLOT_Y1];
            macAddress = "Not Use";

            // 접두사 검증 (serial_prefix_check 0/1/2) — 기존과 동일 규칙, 4칸 전부
            string expectedPrefix = ""; int prefixCheck = 1;
            try { expectedPrefix = (string)(workspace?.serial_prefix ?? ""); } catch { }
            try { prefixCheck = (int)(workspace?.serial_prefix_check ?? 1); } catch { }
            if (!string.IsNullOrEmpty(expectedPrefix) && prefixCheck > 0)
            {
                var bad = SLOT_LABELS.Where((n, i) => _slotSerial[i].Length > 0 && !_slotSerial[i].StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
                if (bad.Count > 0)
                {
                    if (prefixCheck >= 2) { ShowLargeConfirmDialog($"시리얼 접두사를 확인하세요 ({string.Join(",", bad)})\n({expectedPrefix} 필요)"); return false; }
                    if (ShowLargeYesNoDialog($"시리얼 접두사를 확인하세요 ({string.Join(",", bad)})\n({expectedPrefix} 필요)\n\n계속 진행하시겠습니까?") == DialogResult.No) return false;
                }
            }
            // 숫자값 < 1 / 5자리 미만 차단 (FW 2.0.3)
            for (int s = 0; s < 4; s++)
            {
                if (_slotSerial[s].Length == 0) continue;
                string num = _slotSerial[s]; int d = num.LastIndexOf('-');
                if (d >= 0 && d < num.Length - 1) num = num.Substring(d + 1);
                if (num.All(char.IsDigit) && long.TryParse(num, out long v) && (v < 1 || num.Length < 5))
                { ShowLargeConfirmDialog($"시리얼 형식 오류 ({SLOT_LABELS[s]})\n• 숫자값 00001 이상\n• 5자리 이상 (예: VL2-00001)"); return false; }
            }
            // 전부 빈값 차단
            if (_slotSerial.All(string.IsNullOrEmpty)) { ShowLargeConfirmDialog("시리얼이 모두 비어있습니다.\n최소 한 슬롯에 시리얼을 입력하세요."); return false; }
            // 4칸 상호 중복 차단
            var dup = _slotSerial.Where(s => s.Length > 0).GroupBy(s => s.ToUpperInvariant()).FirstOrDefault(g => g.Count() > 1);
            if (dup != null) { ShowLargeConfirmDialog($"동일 시리얼이 두 슬롯에 입력되었습니다 ({dup.First()}).\n서로 다른 시리얼을 입력하세요."); return false; }

            label_Serial.Text = string.Join("\n", SLOT_LABELS.Select((n, i) => $"{n}: {(_slotSerial[i].Length == 0 ? "없음" : _slotSerial[i])}"));
            label_mac.Text = "";
            SetSlotHeaders();

            // 다음 사이클 Y 기본값용 X1/X2 기억 (빈 X는 저장 안 함 = 마감 사이클)
            _prevLastSerialX = _lastSerialX; _prevLastSerialX2 = _lastSerialX2;
            if (_slotSerial[SLOT_X1].Length > 0) { _lastSerialX = _slotSerial[SLOT_X1]; try { File.WriteAllText(Path.Combine(baseDir, GetLastSerialXFileName()), _lastSerialX, new System.Text.UTF8Encoding(false)); } catch { } }
            if (_slotSerial[SLOT_X2].Length > 0) { _lastSerialX2 = _slotSerial[SLOT_X2]; try { File.WriteAllText(Path.Combine(baseDir, GetLastSerialX2FileName()), _lastSerialX2, new System.Text.UTF8Encoding(false)); } catch { } }

            // 자동증가: X 슬롯 중 큰 번호 + 1을 serial 파일에 1줄 append (롤백은 기존처럼 마지막 1줄 제거)
            if (!_serialAutoIncrement) return true;
            try
            {
                string top = new[] { _slotSerial[SLOT_X1], _slotSerial[SLOT_X2] }.Where(s => s.Length > 0)
                    .OrderByDescending(s => { string n = s; int d = s.LastIndexOf('-'); if (d >= 0) n = s.Substring(d + 1); return long.TryParse(n, out var v) ? v : 0; }).FirstOrDefault();
                if (!string.IsNullOrEmpty(top))
                    File.AppendAllText(serialPath, shift(top, 1) + Environment.NewLine, new System.Text.UTF8Encoding(false));
            }
            catch { }
            return true;
        }

        // 슬롯 열 셀 Tag(retmsg) 수집 — 해당 슬롯이 실행된 행만 (Tag 있음)
        private static void CollectRetmsgBySlot(MainForm form, int slotIdx, JArray target)
        {
            foreach (DataGridViewRow row in form.dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                if (row.Cells.Count <= 2 + slotIdx) continue;
                var tagText = row.Cells[2 + slotIdx].Tag?.ToString();
                if (string.IsNullOrWhiteSpace(tagText)) continue;
                foreach (var raw in tagText.Replace("\r\n", "\n").Split('\n'))
                {
                    var t = raw.Trim(); if (t.Length == 0) continue;
                    if (t.StartsWith("{") && t.EndsWith("}")) { try { target.Add(JObject.Parse(t)); } catch { target.Add(t); } }
                    else target.Add(t);
                }
            }
        }

        // 슬롯별 첫 FAIL만 기록 (기존 '결합 그룹당 첫 FAIL'을 슬롯 단위로). Coupling 칸에 슬롯명
        internal void AddFailRecordSlot(dynamic proc, string retmsg, string errorMsg, int slotIdx)
        {
            string slot = SLOT_LABELS[slotIdx];
            if (_failRecords.Exists(fr => fr.Coupling == slot)) return;
            string item = ""; try { item = (string)(proc?.name ?? ""); } catch { }
            string failTag = "";
            try { var pd = proc?.param as IDictionary<string, object>; if (pd != null && pd.TryGetValue("fail_tag", out var ft)) failTag = ft?.ToString() ?? ""; } catch { }
            string summary, detail;
            if (!string.IsNullOrEmpty(errorMsg)) { summary = string.IsNullOrWhiteSpace(failTag) ? errorMsg : failTag; detail = errorMsg; }
            else FailInfoForm.BuildFailSummaryDetail(retmsg, failTag, out summary, out detail);
            _failRecords.Add(new FailRecord
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Serial = _slotSerial[slotIdx],
                Coupling = slot,
                Item = item,
                Summary = summary,
                Detail = detail
            });
        }

        // "X1 PASS · X2 PASS · Y1 PASS · Y2 FAIL" (빈 슬롯은 '-')
        internal string SlotSummaryText(bool commonFailed)
        {
            return string.Join(" · ", SLOT_LABELS.Select((n, i) =>
                _slotSerial[i].Length == 0 ? $"{n} -" : $"{n} {(commonFailed || _slotFailed[i] ? "FAIL" : "PASS")}"));
        }
    }
}
