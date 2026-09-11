using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace flexfab
{
    // 세션/회차 누적 불량 1건 (pSMC FailInfoForm 이식)
    internal sealed class FailRecord
    {
        public string Time = "";     // "HH:mm:ss"
        public string Serial = "";   // serialNumber (없으면 "")
        public string Coupling = ""; // "X결합"/"Y결합" (proc.name 태그, 없으면 "")
        public string Item = "";     // proc.name (검사 항목 이름)
        public string Summary = "";  // 한 줄 요약 (fail_tag 있으면 그 라벨)
        public string Detail = "";   // 여러 줄 상세 (NG 항목 측정값/기준)
    }

    public partial class FailInfoForm : Form
    {
        private readonly List<FailRecord> _records;
        private readonly int _highlightIdx; // 자동 선택할 행 인덱스(-1=없음)

        private DataGridView grid;
        private TextBox txtDetail;
        private Button btnCopy;
        private Button btnClear;
        private Button btnClose;

        internal FailInfoForm(List<FailRecord> records, int highlightIdx = -1)
        {
            _records = records;
            _highlightIdx = highlightIdx;
            BuildUI();
            PopulateGrid();
        }

        private void BuildUI()
        {
            Text = "불량 내용";
            Size = new Size(1150, 560);
            StartPosition = FormStartPosition.CenterParent;
            TopMost = true;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(820, 400);

            // --- DataGridView ---
            grid = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 240,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCoupling", HeaderText = "결합", FillWeight = 12 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime",   HeaderText = "시간",    FillWeight = 10 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSerial", HeaderText = "시리얼",  FillWeight = 18 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItem",   HeaderText = "항목",    FillWeight = 32 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSummary",HeaderText = "사유(요약)", FillWeight = 42 });
            // 결합구분 열: 시료 2종(X결합/Y결합) 구분을 최좌측에서 굵게 강조
            grid.Columns["colCoupling"].DefaultCellStyle.Font = new Font("Malgun Gothic", 11, FontStyle.Bold);
            grid.Columns["colCoupling"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            foreach (DataGridViewColumn col in grid.Columns)
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.SelectionChanged += Grid_SelectionChanged;

            // --- 상세 패널(굵은 큰 글씨) ---
            var lblDetail = new Label
            {
                Text = "상세",
                Dock = DockStyle.Top,
                Height = 22,
                Padding = new Padding(4, 4, 0, 0),
                AutoSize = false
            };
            txtDetail = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Malgun Gothic", 13, FontStyle.Bold),
                BackColor = Color.FromArgb(255, 255, 240)
            };

            // --- 버튼 ---
            btnCopy = new Button { Text = "복사", Size = new Size(90, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnClear = new Button { Text = "지우기", Size = new Size(90, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnClose = new Button { Text = "닫기", Size = new Size(90, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

            btnCopy.Click += BtnCopy_Click;
            btnClear.Click += BtnClear_Click;
            btnClose.Click += (_, __) => Close();

            // 버튼 패널 (한 줄: 복사 / 지우기 / 닫기) — Dock=Bottom으로 항상 하단 확보
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(6, 6, 6, 6),
                WrapContents = false
            };
            btnPanel.Controls.Add(btnCopy);
            btnPanel.Controls.Add(btnClear);
            btnPanel.Controls.Add(btnClose);

            // Dock 도킹 순서: Fill(txtDetail) 먼저 Add → 마지막에 남는 공간만 차지.
            // 이어서 lblDetail/grid(Top), btnPanel(Bottom)이 가장자리를 확보 → 상세창이 버튼을 덮지 않음.
            Controls.Add(txtDetail);
            Controls.Add(lblDetail);
            Controls.Add(grid);
            Controls.Add(btnPanel);
        }

        private void PopulateGrid()
        {
            grid.Rows.Clear();
            // 최신이 위에 오도록 역순
            for (int i = _records.Count - 1; i >= 0; i--)
            {
                var r = _records[i];
                grid.Rows.Add(r.Coupling, r.Time, r.Serial, r.Item, r.Summary);
                grid.Rows[grid.Rows.Count - 1].Tag = i; // 원본 인덱스 보관
            }

            // 자동 선택: _highlightIdx가 유효하면 해당 원본 인덱스의 행 선택
            if (_highlightIdx >= 0 && grid.Rows.Count > 0)
            {
                // 역순이라 원본 _highlightIdx를 역변환
                int gridRow = _records.Count - 1 - _highlightIdx;
                gridRow = Math.Max(0, Math.Min(gridRow, grid.Rows.Count - 1));
                grid.ClearSelection();
                grid.Rows[gridRow].Selected = true;
                grid.CurrentCell = grid.Rows[gridRow].Cells[0];
                ShowDetail(gridRow);
            }
            else if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                ShowDetail(0);
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0) { txtDetail.Text = ""; return; }
            ShowDetail(grid.SelectedRows[0].Index);
        }

        private void ShowDetail(int gridRowIdx)
        {
            if (gridRowIdx < 0 || gridRowIdx >= grid.Rows.Count) { txtDetail.Text = ""; return; }
            if (grid.Rows[gridRowIdx].Tag is int origIdx && origIdx < _records.Count)
            {
                var r = _records[origIdx];
                string cp = string.IsNullOrEmpty(r.Coupling) ? "" : "[" + r.Coupling + "]";
                // 상세내용 맨앞에 결합구분 헤더 (창 제목에는 넣지 않음 — 결합 열/상세로 구분)
                txtDetail.Text = string.IsNullOrEmpty(cp) ? r.Detail : cp + Environment.NewLine + r.Detail;
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            // 전체 불량(모든 행·모든 필드)을 한 번에 복사 — 붙여넣어 공유하기 편하게
            if (_records.Count == 0) { MessageBox.Show("복사할 불량 내용이 없습니다.", "알림"); return; }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== 불량 내용 (총 {_records.Count}건) ===");
            for (int i = 0; i < _records.Count; i++)
            {
                var r = _records[i];
                string cp = string.IsNullOrEmpty(r.Coupling) ? "" : $"[{r.Coupling}] ";
                sb.AppendLine();
                sb.AppendLine($"[{i + 1}] {r.Time} | 시리얼: {r.Serial} | {cp}{r.Item}");
                if (!string.IsNullOrWhiteSpace(r.Summary))
                    sb.AppendLine($"  사유: {r.Summary}");
                if (!string.IsNullOrWhiteSpace(r.Detail))
                {
                    sb.AppendLine("  상세:");
                    foreach (var line in r.Detail.Replace("\r\n", "\n").Split('\n'))
                        sb.AppendLine("    " + line);
                }
            }
            Clipboard.SetText(sb.ToString());
            MessageBox.Show($"불량 {_records.Count}건 전체를 클립보드에 복사했습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            var ans = MessageBox.Show("세션 불량 목록을 모두 지우겠습니까?", "지우기",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ans == DialogResult.Yes)
            {
                _records.Clear();
                grid.Rows.Clear();
                txtDetail.Text = "";
            }
        }

        // ─── 정적 헬퍼: proc.name → 결합구분("X결합"/"Y결합") ────────────────────
        // proc.name의 대괄호 태그 "[X결합]"/"[Y결합]" 추출. 없으면 "".
        // 시료 2종(X결합 위치/Y결합 위치)을 FAIL 시 제일 먼저 구분하기 위함.
        public static string ExtractCoupling(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            if (name.Contains("[X결합]")) return "X결합";
            if (name.Contains("[Y결합]")) return "Y결합";
            return "";
        }

        // ─── 정적 헬퍼: VibeTilt retmsg → (summary, detail) ───────────────────────
        // VibeTilt MakeResult 구조: {"Name":..,"id":..,"data":[{item|command, value, min?, max?, result}], "result":".."}
        //  → data[] 중 result==NG/FAIL 인 항목만 골라 "항목: 측정 value (기준 min~max)" 형식으로 표시.
        //  pSMC의 {Exp,Rsp}/{Item,Measure} 형식도 폴백 처리.
        public static void BuildFailSummaryDetail(
            string retmsg,
            string failTag,
            out string summary,
            out string detail)
        {
            summary = "";
            detail = "";

            if (string.IsNullOrWhiteSpace(retmsg))
            {
                summary = string.IsNullOrWhiteSpace(failTag) ? "상세 없음" : failTag;
                detail = summary;
                return;
            }

            var ngLines = new List<string>();
            try
            {
                var root = JToken.Parse(retmsg) as JObject;
                var data = root?["data"];

                if (data is JArray arr)
                {
                    foreach (var elem in arr)
                    {
                        if (elem is not JObject o) continue;
                        string res = (o["result"]?.ToString() ?? "").Trim().ToUpperInvariant();
                        if (res != "NG" && res != "FAIL") continue; // 불량(NG)만
                        ngLines.Add(FormatItem(o));
                    }

                    // NG 마커가 없는데 전체 result는 NG인 경우 → 전체 data 표시(폴백)
                    if (ngLines.Count == 0)
                    {
                        string topRes = (root?["result"]?.ToString() ?? "").Trim().ToUpperInvariant();
                        if (topRes == "NG" || topRes == "FAIL")
                            foreach (var elem in arr)
                                if (elem is JObject o2) ngLines.Add(FormatItem(o2));
                    }
                }
            }
            catch { /* JSON 파싱 실패 → 평문 폴백 */ }

            if (ngLines.Count == 0)
            {
                // 구조 파싱 실패/비어있음 → fail_tag 또는 원문 일부
                summary = string.IsNullOrWhiteSpace(failTag) ? "상세 없음" : failTag;
                detail = string.IsNullOrWhiteSpace(failTag) ? retmsg : failTag;
                return;
            }

            detail = string.Join(Environment.NewLine, ngLines);
            summary = !string.IsNullOrWhiteSpace(failTag) ? failTag : ngLines[0];
        }

        // data[] 항목 1건 → 표시 문자열
        private static string FormatItem(JObject o)
        {
            // 라벨: item | command | Item
            string label = o["item"]?.ToString();
            if (string.IsNullOrEmpty(label)) label = o["command"]?.ToString();
            if (string.IsNullOrEmpty(label)) label = o["Item"]?.ToString();
            if (string.IsNullOrEmpty(label)) label = "(항목)";

            // pSMC 호환: {Exp,Rsp}
            if (o["Exp"] != null || o["Rsp"] != null)
            {
                string exp = o["Exp"]?.ToString() ?? "";
                string rsp = o["Rsp"]?.ToString() ?? "";
                string rspDisp = string.IsNullOrEmpty(rsp) ? "응답없음" : rsp;
                return string.IsNullOrEmpty(exp) ? rspDisp : $"{label}: 기대={exp} / 실제={rspDisp}";
            }

            // 값/기준
            var valTok = o["value"] ?? o["Measure"];
            string value = (valTok == null || valTok.Type == JTokenType.Null) ? "응답없음" : valTok.ToString();
            string min = o["min"]?.ToString();
            string max = o["max"]?.ToString();

            if (!string.IsNullOrEmpty(min) && !string.IsNullOrEmpty(max))
                return $"{label}: 측정 {value} (기준 {min}~{max})";
            return $"{label}: {value}";
        }
    }
}
