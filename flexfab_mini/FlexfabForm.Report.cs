using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace flexfab
{
    // ══════════ 검사성적서 · 검사이력 CSV · FAIL JSON (pSMC 수평전개) ══════════
    // PLAN_4슬롯_성적서_이력CSV_FAIL저장_20260921_v01 — 참조: pSMC flexfab_mini FlexfabForm.cs
    //   SaveLog(1726) / AppendResultCsv(2011) / AppendReportCsv(2905) / 성적서 설정(2467~2580) / ReportXlsxWriter.cs
    //
    // - 4슬롯 정상 종료(FinishSlot4)에서만 호출한다. 2슬롯 경로에는 연결하지 않는다(2026-09-21 사용자 결정: 2슬롯 기능 추가 안 함).
    // - 저장 루트 = 기존 결과 JSON 규칙(SaveLogDirect) Result/{프로젝트명(공백 제거)}/
    //     {날짜}/{sn}.json                        완성(PASS) — 기존과 동일
    //   bat-001(2026-09-30, REQ-001): FAIL·PASS_DUPLICATE는 품질모니터링 수집 경로(Result/) 밖 형제 폴더 Result_보관/{프로젝트}/ 로
    //     Result_보관/{proj}/{날짜}/PASS_DUPLICATE/{sn}_{시각}.json   같은 날 같은 시리얼 재완성 시 기존 파일 보존
    //     Result_보관/{proj}/{날짜}/FAIL/{sn}_{X|Y}_{시각}.json      FAIL — 로컬만(Mongo 업로드 안 함: dcy-001 K1)
    //     이력/이력_{proj}_{yyyyMMdd}[_NN].csv      슬롯(보드) 1장당 1줄, 사이클마다 누적
    //     성적서/성적서_{proj}_{yyyyMMdd}_{NN}.csv  완성 보드 1대 = 1열(가로 누적) + 같은 이름 .xlsx 인쇄본
    // - 성적서·이력·FAIL 저장은 부가 산출물: 실패해도 판정·완성 저장·pending에 영향을 주지 않는다(전부 내부 try/catch).
    public partial class MainForm
    {
        // ─────────────────────────── 경로 ───────────────────────────

        // SaveLogDirect·ResultJsonPath와 같은 규칙: projects[0].name 공백 제거
        internal string ResultProjFolder()
        {
            string projFolder = "";
            try { projFolder = ((string)workspace.projects[0].name).Replace(" ", ""); } catch { }
            return string.IsNullOrWhiteSpace(projFolder) ? "unknown" : projFolder;
        }

        internal string ResultRootDir() => Path.Combine(Directory.GetCurrentDirectory(), "Result", ResultProjFolder());

        // bat-001(REQ-001): FAIL·PASS_DUPLICATE 보관 루트 — 품질모니터링은 상위 Result/ 를 재귀 수집하므로 그 밖(형제 폴더)에 둔다
        internal const string ARCHIVE_ROOT_NAME = "Result_보관";
        internal string ArchiveRootDir() => Path.Combine(Directory.GetCurrentDirectory(), ARCHIVE_ROOT_NAME, ResultProjFolder());

        // 파일명에 쓸 수 없는 문자·경로 구분자를 '_'로 (시리얼 형식 검사를 우회한 값이 폴더를 벗어나지 않게)
        internal static string SafeFileName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "Null";
            var bad = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(s.Length);
            foreach (char c in s.Trim()) sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
            string r = sb.ToString().Replace("..", "_");
            return r.Length == 0 ? "Null" : r;
        }

        private string ReportInspectorName()
        {
            try
            {
                string raw = label_inspector?.Text ?? "";
                int idx = raw.IndexOf(':');
                return (idx >= 0) ? raw.Substring(idx + 1).Trim() : raw.Trim();
            }
            catch { return ""; }
        }

        private string ReportProjectName()
        {
            try { return (string)workspace.projects[0].name ?? ""; } catch { return ""; }
        }

        // ─────────────────────── 파일 잠김 재시도 ───────────────────────
        // pSMC 동일(2026-08-04): 엑셀 등이 파일을 잠그면 [다시 시도]/[취소]. 취소 = false(호출부가 경고 로그 후 건너뜀).
        //   잠김(IOException) 외 예외는 그대로 던진다. 검사 워커 스레드에서 불리므로 팝업은 Invoke.
        private bool RunFileOpWithLockRetry(Action op, string fileDesc)
        {
            while (true)
            {
                try { op(); return true; }
                catch (IOException ex) when (IsLockViolation(ex))
                {
                    DialogResult ans = DialogResult.Cancel;
                    Action show = () => ans = MessageBox.Show(this,
                        $"'{fileDesc}' 파일이 다른 프로그램(엑셀 등)에 열려 있어 저장할 수 없습니다.\n\n" +
                        "그 파일을 닫은 뒤 [다시 시도]를 누르세요.\n[취소]하면 이 기록은 저장을 건너뜁니다.\n\n" +
                        $"({ex.Message})",
                        "파일 잠김 — 저장 대기", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                    try { if (InvokeRequired) Invoke(show); else show(); } catch { }
                    if (ans != DialogResult.Retry) return false;
                }
            }
        }

        // 공유 위반(32)·잠금 위반(33)만 "다른 프로그램이 열어 둠"으로 본다. 디스크 가득·경로 길이 등 그 외 IOException은
        //   잘못된 안내로 재시도를 반복시키지 않도록 그대로 던진다(호출부 try/catch가 로그 후 건너뜀) — 구현 검증 지적
        internal static bool IsLockViolation(IOException ex)
        {
            int code = ex.HResult & 0xFFFF;
            return code == 32 || code == 33;
        }

        // 파일 끝이 개행이 아니면(엑셀·메모장이 마지막 개행 없이 저장) 이어쓸 텍스트 앞에 개행을 붙인다 — 이전 행과 한 줄로 붙는 것 방지
        internal static string PrefixNewlineIfNeeded(string path, string text)
        {
            if (!File.Exists(path)) return text;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length == 0) return text;
            fs.Seek(-1, SeekOrigin.End);
            return fs.ReadByte() == '\n' ? text : Environment.NewLine + text;
        }

        // ─────────────────────────── CSV ───────────────────────────

        // , " 개행 포함 시 큰따옴표로 감싸고 내부 " → ""
        internal static string CsvEsc(string v)
        {
            v ??= "";
            if (v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return v;
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }

        internal static List<string> CsvSplitLine(string line)
        {
            var cells = new List<string>(); var sb = new StringBuilder(); bool q = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (q)
                {
                    if (c == '"') { if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; } else q = false; }
                    else sb.Append(c);
                }
                else if (c == '"') q = true;
                else if (c == ',') { cells.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            cells.Add(sb.ToString());
            return cells;
        }

        // 엄격 읽기 — IOException을 삼키지 않는다(pSMC 2026-08-19: 잠긴 파일을 '없음'으로 오인해 덮어써 기록이 사라진 실사례).
        //   호출측은 RunFileOpWithLockRetry로 감싸고, 취소 시 기록을 건너뛴다. BOM은 ReadAllLines가 제거.
        internal static List<List<string>> ReadCsvTableStrict(string path)
        {
            var rows = new List<List<string>>();
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                if (line.Length > 0) rows.Add(CsvSplitLine(line));
            return rows.Count > 0 ? rows : null;
        }

        internal static string CsvJoin(IEnumerable<List<string>> rows)
        {
            var sb = new StringBuilder();
            foreach (var r in rows) sb.AppendLine(string.Join(",", r.Select(CsvEsc)));
            return sb.ToString();
        }

        // 같은 기준명에서 아직 없는 다음 분할 파일 (_02, _03 …)
        internal static string NextSplitPath(string baseNoExt)
        {
            int sfx = 2;
            while (File.Exists($"{baseNoExt}_{sfx:D2}.csv")) sfx++;
            return $"{baseNoExt}_{sfx:D2}.csv";
        }

        // ─────────────────── C. PASS 중복 보존 · FAIL JSON ───────────────────

        // PASS_DUPLICATE 경로 — 파일명 시각 = 기존 파일 최종수정시각(그 검사가 끝난 시각). 같은 초 충돌 시 _2,_3 (절대 덮지 않음)
        internal static string PassDuplicatePath(string saveDir, string sn, DateTime lastWrite)
        {
            string prevDir = Path.Combine(saveDir, "PASS_DUPLICATE");
            string stamp = lastWrite.ToString("yyyyMMdd_HHmmss");
            string safe = SafeFileName(sn);
            string p = Path.Combine(prevDir, $"{safe}_{stamp}.json");
            for (int i = 2; File.Exists(p); i++) p = Path.Combine(prevDir, $"{safe}_{stamp}_{i}.json");
            return p;
        }

        // 완성 저장 직전 호출: 같은 날 같은 시리얼 PASS가 이미 있으면 PASS_DUPLICATE/로 이관(덮어쓰기로 인한 유실 방지)
        // 반환 = 이관한 경로(저장 실패 시 RestorePassDuplicate로 되돌리기 위함), 이관 안 했으면 null
        internal string PreservePassDuplicate(string jsonPath, Action<string> log)
        {
            try
            {
                if (!File.Exists(jsonPath)) return null;
                string saveDir = Path.GetDirectoryName(jsonPath)!;
                string sn = Path.GetFileNameWithoutExtension(jsonPath);
                // bat-001: 완성 파일의 날짜 폴더명 그대로 보관 루트 아래로 (날짜 폴더가 아니면 오늘)
                string dayName = Path.GetFileName(saveDir);
                if (!DateTime.TryParseExact(dayName, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _)) dayName = DateTime.Now.ToString("yyyy-MM-dd");
                string prevPath = PassDuplicatePath(Path.Combine(ArchiveRootDir(), dayName), sn, File.GetLastWriteTime(jsonPath));
                Directory.CreateDirectory(Path.GetDirectoryName(prevPath)!);
                if (RunFileOpWithLockRetry(() => File.Move(jsonPath, prevPath), Path.GetFileName(jsonPath)))
                {
                    log($"[결과] 기존 PASS 결과 보존(PASS 중복) → {ARCHIVE_ROOT_NAME}\\…\\PASS_DUPLICATE\\{Path.GetFileName(prevPath)}");
                    return prevPath;
                }
                log("[결과] ★경고: 기존 PASS 결과 보존 취소(파일 잠김·사용자 취소) — 덮어쓰기 진행");
            }
            catch (Exception ex) { log($"[결과] ★경고: 기존 PASS 결과 보존 실패 — 덮어쓰기 발생 ({ex.Message})"); }
            return null;
        }

        // 이관 후 새 결과 저장이 실패하면 이관한 파일을 원위치로 — {sn}.json이 사라져 시리얼 중복검사가 무력화되는 것 방지(구현 검증 지적)
        internal void RestorePassDuplicate(string movedPath, string jsonPath, Action<string> log)
        {
            try
            {
                if (string.IsNullOrEmpty(movedPath) || !File.Exists(movedPath) || File.Exists(jsonPath)) return;
                File.Move(movedPath, jsonPath);
                log($"[결과] 저장 실패 — 기존 PASS 결과 원위치 복귀: {Path.GetFileName(jsonPath)}");
            }
            catch (Exception ex) { log($"[결과] ★경고: 기존 PASS 결과 원위치 복귀 실패 — PASS_DUPLICATE에 보존됨 ({ex.Message})"); }
        }

        internal static string FailJsonPath(string dateDir, string sn, string stage, DateTime now)
        {
            string failDir = Path.Combine(dateDir, "FAIL");
            string stem = $"{SafeFileName(sn)}_{stage}_{now:yyyyMMdd_HHmmss}";
            string p = Path.Combine(failDir, stem + ".json");
            for (int i = 2; File.Exists(p); i++) p = Path.Combine(failDir, $"{stem}_{i}.json");
            return p;
        }

        // FAIL 결과 JSON (로컬만). 반환 = Result/{proj}/ 기준 상대경로(이력 CSV '결과파일' 열), 실패 시 ""
        //   결과 JSON 필드는 SaveLogDirect와 같은 구성 + pass_fail/stage/slot/fail_reason (additive)
        internal string SaveFailJsonSlot(string sn, string stage, string slotLabel, string reason, JArray result, Action<string> log)
        {
            try
            {
                DateTime now = DateTime.Now;
                string dateStr = now.ToString("yyyy-MM-dd");
                string dateDir = Path.Combine(ArchiveRootDir(), dateStr);   // bat-001: Result/ 밖 보관
                string filePath = FailJsonPath(dateDir, sn, stage, now);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                string projName = ReportProjectName();
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
                var root = new JObject
                {
                    ["time"] = DateTime.UtcNow,
                    ["Ins"] = ReportInspectorName(),
                    ["Proj"] = projName,
                    ["project_type"] = projName.Contains("모션", StringComparison.OrdinalIgnoreCase) || projName.Contains("motion", StringComparison.OrdinalIgnoreCase) ? "motion" : "board",
                    ["comm_type"] = projName.Contains("485") ? "RS-485" : "RS-232",
                    ["lib"] = libLines,
                    ["sn"] = sn,
                    ["mac"] = "Not Use",
                    ["pass_fail"] = "FAIL",
                    ["stage"] = stage,
                    ["slot"] = slotLabel,
                    ["fail_reason"] = reason ?? "",
                    ["result"] = result ?? new JArray()
                };
                string json = root.ToString();
                if (!RunFileOpWithLockRetry(() => File.WriteAllText(filePath, json, new UTF8Encoding(false)), Path.GetFileName(filePath)))
                {
                    log($"[결과] ★경고: FAIL 결과 저장 건너뜀(파일 잠김·사용자 취소) — {slotLabel} {sn}");
                    return "";
                }
                log($"[{slotLabel}] FAIL 결과 저장(로컬): FAIL\\{Path.GetFileName(filePath)}");
                return $"{ARCHIVE_ROOT_NAME}/{ResultProjFolder()}/{dateStr}/FAIL/{Path.GetFileName(filePath)}";   // 작업 폴더 기준 (bat-001)
            }
            catch (Exception ex)
            {
                log($"[결과] FAIL 결과 저장 실패(판정에는 영향 없음): {slotLabel} {sn} — {ex.Message}");
                return "";
            }
        }

        // ─────────────────────── B. 검사이력 CSV ───────────────────────

        internal static readonly string[] HIST_FIXED = { "시리얼", "검사일시", "검사종류", "검사자", "슬롯", "단계", "판정", "FAIL수", "FAIL항목", "결과파일" };

        // 오늘 이력 파일: 기준명 또는 같은 날짜의 가장 최신 분할본(_02…)에 이어쓴다(pSMC 2026-08-19 결함 수정 규칙)
        internal static string PickHistoryPath(string dir, string key, DateTime now, out string baseNoExt)
        {
            baseNoExt = Path.Combine(dir, $"이력_{key}_{now:yyyyMMdd}");
            string p = baseNoExt + ".csv";
            for (int k = 2; ; k++)   // 상한 없음 — NextSplitPath와 같은 규칙(구현 검증 지적: 99 상한 불일치)
            {
                string cand = $"{baseNoExt}_{k:D2}.csv";
                if (File.Exists(cand)) p = cand; else break;
            }
            return p;
        }

        internal static bool HeaderMatches(List<string> actual, List<string> expected)
        {
            if (actual == null || actual.Count != expected.Count) return false;
            for (int i = 0; i < expected.Count; i++) if (actual[i] != expected[i]) return false;
            return true;
        }

        // 그리드 셀 → 이력 값: OK / FAIL / -(Skip·미실행) / 빈칸(해당 단계 아님 "—")
        internal static string HistCell(string cell)
        {
            cell = (cell ?? "").Trim();
            if (cell == "OK") return "OK";
            if (cell == "FAIL") return "FAIL";
            if (cell == "—") return "";
            return "-";
        }

        // 사이클 정상 종료 시: 시리얼이 있는 슬롯마다 1줄. state = 슬롯 요약 상태, relPath = 저장한 결과 JSON 상대경로
        internal void AppendHistorySlot4(string[] state, string[] relPath, Action<string> log)
        {
            try
            {
                // 그리드 수집(UI 스레드) — 항목 이름 + 슬롯별 셀 값
                var names = new List<string>();
                var cells = new List<string[]>();
                Action collect = () =>
                {
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        var row = dataGridView1.Rows[i];
                        if (row.IsNewRow) continue;
                        string nm = row.Cells[1].Value?.ToString() ?? "";
                        if (nm.Length == 0) continue;
                        var v = new string[4];
                        for (int s = 0; s < 4; s++) v[s] = row.Cells.Count > 2 + s ? row.Cells[2 + s].Value?.ToString() ?? "" : "";
                        names.Add(nm); cells.Add(v);
                    }
                };
                if (InvokeRequired) Invoke(collect); else collect();

                var header = new List<string>(HIST_FIXED);
                foreach (var nm in names) if (!header.Contains(nm)) header.Add(nm);

                string when = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string proj = ReportProjectName(), ins = ReportInspectorName();
                var newRows = new List<List<string>>();
                for (int s = 0; s < 4; s++)
                {
                    string sn = _slotSerial[s];
                    if (string.IsNullOrEmpty(sn)) continue;
                    var failNames = new List<string>();
                    for (int i = 0; i < names.Count; i++) if ((cells[i][s] ?? "").Trim() == "FAIL") failNames.Add(names[i]);
                    var row = new List<string>
                    {
                        sn, when, proj, ins, SLOT_LABELS[s], s <= SLOT_X2 ? "X" : "Y",
                        state?[s] ?? "", failNames.Count.ToString(), string.Join(";", failNames), relPath?[s] ?? ""
                    };
                    for (int c = HIST_FIXED.Length; c < header.Count; c++)
                    {
                        int i = names.IndexOf(header[c]);
                        row.Add(i >= 0 ? HistCell(cells[i][s]) : "");
                    }
                    newRows.Add(row);
                }
                if (newRows.Count == 0) return;

                string key = ResultProjFolder();
                string dir = Path.Combine(ResultRootDir(), "이력");
                Directory.CreateDirectory(dir);
                string csvPath = PickHistoryPath(dir, key, DateTime.Now, out string baseNoExt);

                List<List<string>> table = null;
                if (File.Exists(csvPath))
                {
                    List<List<string>> tmp = null;
                    if (!RunFileOpWithLockRetry(() => { tmp = ReadCsvTableStrict(csvPath); }, Path.GetFileName(csvPath)))
                    {
                        log($"[검사이력] ★기존 파일을 읽지 못해 이번 {newRows.Count}줄을 건너뜁니다(덮어쓰기 방지) — {Path.GetFileName(csvPath)}");
                        return;
                    }
                    table = tmp;
                    if (table != null && (table.Count == 0 || table[0].Count < HIST_FIXED.Length || table[0][0] != "시리얼"))
                    {
                        string np = NextSplitPath(baseNoExt);
                        log($"[검사이력] {Path.GetFileName(csvPath)} 형식 인식 불가 — 보존하고 {Path.GetFileName(np)}로 시작합니다");
                        csvPath = np; table = null;
                    }
                    else if (table != null && !HeaderMatches(table[0], header))
                    {
                        string np = NextSplitPath(baseNoExt);
                        log($"[검사이력] 항목 구성이 달라져 새 파일로 시작합니다 — {Path.GetFileName(csvPath)} 보존 → {Path.GetFileName(np)}");
                        csvPath = np; table = null;
                    }
                }

                bool full = table == null;
                string text = full ? CsvJoin(new[] { header }.Concat(newRows)) : CsvJoin(newRows);
                string target = csvPath;
                if (RunFileOpWithLockRetry(() =>
                    {
                        if (full) WriteAllTextAtomic(target, "﻿" + text);   // 새 파일 = BOM(엑셀 한글), tmp→교체(쓰는 도중 종료돼도 반쪽 파일 없음)
                        else File.AppendAllText(target, PrefixNewlineIfNeeded(target, text), new UTF8Encoding(false));
                    }, Path.GetFileName(target)))
                    log($"[검사이력] {Path.GetFileName(target)} ← {string.Join(", ", newRows.Select(r => $"{r[4]} {r[0]} {r[6]}"))}");
                else
                    log($"[검사이력] ★경고: 기록 건너뜀(파일 잠김·사용자 취소) — {Path.GetFileName(target)}");
            }
            catch (Exception ex)
            {
                log($"[검사이력] CSV 기록 실패(검사 결과에는 영향 없음): {ex.Message}");
            }
        }

        // ─────────────────────── A. 검사성적서 ───────────────────────

        internal const int REPORT_LBL = 5;                        // 검사명·구분·번호·검사번호·세부 검사 항목
        internal const string REPORT_PASS_ROW = "합격, 불합격 판정";
        internal const string REPORT_WHEN_ROW = "검사일시";

        internal sealed class ReportRow
        {
            public string Proc = "", Group = "", Num = "", Label = "";
        }

        // 검사번호: pSMC는 소문자 접미(#16-2.2a)만 허용했으나 VibeTilt는 #1-X 형식이라 대문자 접미도 허용(단위 테스트로 검출)
        static readonly Regex ReNum = new Regex(@"^(#[0-9][0-9\-~,./]*[A-Za-z]?)\s*");
        static readonly Regex ReSlotTag = new Regex(@"\s*\[(?:X|Y)(?:슬롯|결합)\]\s*");

        // 항목명 → (검사번호, 세부 라벨). 슬롯 태그([X슬롯] 등)는 떼고, 기준이 있으면 괄호로 붙인다
        internal static void SplitReportName(string procName, string spec, out string num, out string label)
        {
            var m = ReNum.Match(procName ?? "");
            num = m.Success ? m.Groups[1].Value : "";
            string rest = ReNum.Replace(procName ?? "", "");
            rest = ReSlotTag.Replace(rest, " ").Trim();
            label = rest + (string.IsNullOrWhiteSpace(spec) ? "" : $" ({spec})");
        }

        internal static string GroupLabelOf(string slotGroup) => slotGroup switch
        {
            "common" => "공통",
            "X" => "X단계",
            "Y" => "Y단계",
            "all" => "모션",
            _ => "",
        };

        // workspace: report_items(키 없음 = 전체 / [] = 생성 안 함) · report_serial_count(기본 10)
        internal (List<string> items, int n, bool unset) ReadReportSettings()
        {
            var items = new List<string>(); int n = 10; bool unset = true;
            try
            {
                var ws = (IDictionary<string, object>)workspace;
                if (ws.TryGetValue("report_items", out var ri) && ri is System.Collections.IEnumerable en && !(ri is string))
                {
                    unset = false;
                    foreach (var o in en) { var s = Convert.ToString(o); if (!string.IsNullOrWhiteSpace(s)) items.Add(s); }
                }
                if (ws.TryGetValue("report_serial_count", out var rc)) n = Math.Max(1, Convert.ToInt32(rc));
            }
            catch { }
            return (items, n, unset);
        }

        // 성적서 행 정의 — 워크스페이스 proc 순서, report_items 필터. enabled=false면 생성 안 함(전체 해제)
        internal List<ReportRow> BuildReportRows(out bool enabled)
        {
            var (items, _, unset) = ReadReportSettings();
            var rows = new List<ReportRow>();
            enabled = unset || items.Count > 0;
            if (!enabled) return rows;
            try
            {
                foreach (dynamic pr in (IEnumerable<object>)workspace.projects[0].procs)
                {
                    string nm = ""; try { nm = Convert.ToString(pr.name) ?? ""; } catch { }
                    if (nm.Length == 0) continue;
                    if (!unset && !items.Contains(nm)) continue;
                    if (rows.Any(r => r.Proc == nm)) continue;

                    string spec = "";
                    IDictionary<string, object> pd = null;
                    try { pd = pr.param as IDictionary<string, object>; } catch { }
                    if (pd != null)
                    {
                        if (pd.TryGetValue("report_spec", out var rs) && !string.IsNullOrWhiteSpace(Convert.ToString(rs)))
                            spec = Convert.ToString(rs).Replace(",", "·").Trim();   // CSV 열 보호
                        else if (pd.TryGetValue("tests", out var tl) && tl is System.Collections.IEnumerable te && !(tl is string))
                        {
                            string unit = pd.TryGetValue("unit", out var u) ? Convert.ToString(u) ?? "" : "";
                            var sp = new List<string>();
                            foreach (var t in te)
                                if (t is IDictionary<string, object> td && td.TryGetValue("min", out var mn) && td.TryGetValue("max", out var mx) && mn != null && mx != null)
                                    sp.Add($"{mn}~{mx}{unit}");
                            spec = string.Join(";", sp);
                        }
                    }
                    string grp = "";
                    if (pd != null && pd.TryGetValue("report_group", out var rg)) grp = Convert.ToString(rg) ?? "";
                    if (string.IsNullOrWhiteSpace(grp)) grp = GroupLabelOf(GetSlotGroup(pr));

                    SplitReportName(nm, spec, out string num, out string label);
                    rows.Add(new ReportRow { Proc = nm, Group = grp, Num = num, Label = label });
                }
            }
            catch { }
            return rows;
        }

        internal static List<List<string>> NewReportTable(string testName, IList<ReportRow> rows, string splitNote)
        {
            var t = new List<List<string>>
            {
                new List<string> { "검사명", "구분", "번호", "검사번호", "세부 검사 항목" },
                new List<string> { "", "", "", "", REPORT_WHEN_ROW }
            };
            int no = 0;
            foreach (var r in rows) t.Add(new List<string> { testName, r.Group, (++no).ToString(), r.Num, r.Label });
            t.Add(new List<string> { "", "", "", "", REPORT_PASS_ROW });
            if (!string.IsNullOrEmpty(splitNote)) t.Add(new List<string> { "", "", "", "", splitNote });
            return t;
        }

        // 기존 표의 항목 행(검사번호|세부)이 이번 행 정의와 같은가 — 다르면 기존 파일 보존하고 다음 순번
        internal static bool ReportRowsMatch(List<List<string>> table, IList<ReportRow> rows)
        {
            var actual = new List<string>();
            for (int r = 2; r < table.Count; r++)
            {
                if (table[r].Count < REPORT_LBL) continue;
                string l4 = table[r][4];
                if (l4 == REPORT_PASS_ROW || l4.StartsWith("※")) continue;
                actual.Add(table[r][3] + "|" + l4);
            }
            if (actual.Count != rows.Count) return false;
            for (int i = 0; i < rows.Count; i++) if (actual[i] != rows[i].Num + "|" + rows[i].Label) return false;
            return true;
        }

        // 판정 행까지 있어야 성적서로 인정 — 판정 행 직전에서 잘린 파일에 열을 계속 붙이지 않게(구현 검증 지적)
        internal static bool IsReportTable(List<List<string>> table) =>
            table != null && table.Count >= 3 && table[0].Count >= REPORT_LBL && table[0][0] == "검사명"
            && table.Any(r => r.Count > 4 && r[4] == REPORT_PASS_ROW);

        // 시리얼 열 위치: 기존 열(재검) 인덱스 / 새 열이면 -1 / 파일이 N열로 가득이면 int.MinValue
        internal static int ReportColumnOf(List<List<string>> table, string sn, int n)
        {
            // 시리얼 비교는 대소문자 무시 — pending 매칭·파일시스템과 같은 기준(구현 검증 지적)
            for (int c = REPORT_LBL; c < table[0].Count; c++)
                if (string.Equals(table[0][c], sn, StringComparison.OrdinalIgnoreCase)) return c;
            if (table[0].Count - REPORT_LBL >= n) return int.MinValue;
            return -1;
        }

        // 완성 보드 열 기록: 항목 = OK, 검사일시, 판정 = 합. col < 0 이면 새 열 추가. 반환 = 기록한 열
        internal static int FillReportColumn(List<List<string>> table, int col, string sn, string when)
        {
            if (col < 0) { foreach (var row in table) row.Add("-"); col = table[0].Count - 1; }
            table[0][col] = sn;
            for (int r = 1; r < table.Count; r++)
            {
                while (table[r].Count <= col) table[r].Add("-");   // 손상 파일 방어
                string l4 = table[r].Count > 4 ? table[r][4] : "";
                if (r == 1 || l4 == REPORT_WHEN_ROW) table[r][col] = when;
                else if (l4 == REPORT_PASS_ROW) table[r][col] = "합";
                else if (l4.StartsWith("※")) table[r][col] = "";
                else table[r][col] = "OK";
            }
            return col;
        }

        // 오늘 날짜 파일 중 가장 최신 순번(없으면 _01). forceNew = 다음 순번 새 파일
        internal static string PickReportPath(string dir, string key, DateTime now, bool forceNew)
        {
            string today = now.ToString("yyyyMMdd");
            int mx = 0; string best = null;
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir, $"성적서_{key}_{today}_*.csv"))
                {
                    var m = Regex.Match(Path.GetFileNameWithoutExtension(f), @"_(\d+)$");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int v) && v > mx) { mx = v; best = f; }
                }
            }
            if (!forceNew && best != null) return best;
            return Path.Combine(dir, $"성적서_{key}_{today}_{mx + 1:D2}.csv");
        }

        // 완성(Y 완성) 보드 1대 → 성적서 1열. 완성 = X·Y 양 단계 SlotVerdict 통과가 전제라 행은 전부 OK(측정 수치는 후속)
        internal void AppendReportForBoard(string sn, Action<string> log)
        {
            try
            {
                var rows = BuildReportRows(out bool enabled);
                if (!enabled) return;   // report_items = [] → 성적서 기능 off
                if (rows.Count == 0) { log($"[성적서] ★report_items와 일치하는 검사항목이 0개 — [성적서 설정] 확인 필요(이번 {sn} 기록 안 함)"); return; }
                int n = ReadReportSettings().n;
                string key = ResultProjFolder();
                string testName = ReportProjectName();
                string dir = Path.Combine(ResultRootDir(), "성적서");
                Directory.CreateDirectory(dir);

                DateTime now = DateTime.Now;
                string path = PickReportPath(dir, key, now, false);
                string splitNote = null;
                List<List<string>> table = null;
                if (File.Exists(path))
                {
                    List<List<string>> tmp = null;
                    if (!RunFileOpWithLockRetry(() => { tmp = ReadCsvTableStrict(path); }, Path.GetFileName(path)))
                    {
                        log($"[성적서] ★기존 파일을 읽지 못해 이번 기록을 건너뜁니다(덮어쓰기 방지) — {Path.GetFileName(path)} / {sn}");
                        return;
                    }
                    table = tmp;
                    if (!IsReportTable(table))
                    {
                        log($"[성적서] {Path.GetFileName(path)} 형식 인식 불가 — 보존하고 다음 순번 파일로");
                        path = PickReportPath(dir, key, now, true); table = null;
                    }
                    else if (!ReportRowsMatch(table, rows))
                    {
                        log($"[성적서] 항목·기준이 달라져 다음 순번 파일로 시작합니다 — {Path.GetFileName(path)} 보존");
                        splitNote = $"※ 항목·기준 변경으로 {Path.GetFileName(path)} 에서 분리 ({now:yyyy-MM-dd HH:mm})";
                        path = PickReportPath(dir, key, now, true); table = null;
                    }
                }

                int col = -1;
                if (table != null)
                {
                    col = ReportColumnOf(table, sn, n);
                    if (col == int.MinValue) { path = PickReportPath(dir, key, now, true); table = null; col = -1; }   // N 도달 → 다음 파일
                }
                if (table == null) table = NewReportTable(testName, rows, splitNote);
                bool recheck = col >= 0;
                FillReportColumn(table, col, sn, now.ToString("yyyy-MM-dd HH:mm"));

                string csv = CsvJoin(table);
                string target = path;
                if (!RunFileOpWithLockRetry(() => WriteAllTextAtomic(target, "﻿" + csv), Path.GetFileName(target)))   // BOM + tmp→교체
                {
                    log($"[성적서] ★경고: 기록 건너뜀(파일 잠김·사용자 취소) — {Path.GetFileName(target)} / {sn}");
                    return;
                }
                log($"[성적서] {Path.GetFileName(target)} ← {sn}{(recheck ? " (재검 — 기존 열 갱신)" : "")} ({table[0].Count - REPORT_LBL}/{n}열)");

                // 인쇄본 xlsx — CSV 전체로 매번 재생성(검사일시 행 제외). 실패해도 CSV·검사 영향 없음
                try
                {
                    var xt = table.Where(r => !(r.Count > 4 && r[4] == REPORT_WHEN_ROW)).Select(r => new List<string>(r)).ToList();
                    int last = table[0].Count - 1;
                    var mm = Regex.Match(testName ?? "", @"VL\d{2}", RegexOptions.IgnoreCase);
                    var hdr = new ReportXlsxWriter.Header
                    {
                        Title = (mm.Success ? mm.Value.ToUpperInvariant() + " " : "") + "검사 성적서",
                        Model = mm.Success ? mm.Value.ToUpperInvariant() : "",
                        SnRange = last >= REPORT_LBL ? (last == REPORT_LBL ? table[0][REPORT_LBL] : $"{table[0][REPORT_LBL]} ~ {table[0][last]}") : "",
                        Date = table.Count > 1 && table[1].Count > last ? (table[1][last] ?? "").Split(' ')[0] : "",
                        Inspector = ReportInspectorName(),
                        MacAddress = "None",
                    };
                    string xlsxPath = Path.ChangeExtension(target, ".xlsx");
                    ReportXlsxWriter.Write(xlsxPath, xt, hdr);
                    log($"[성적서] 인쇄본 {Path.GetFileName(xlsxPath)} 갱신");
                }
                catch (Exception xe) { log($"[성적서] 인쇄본 xlsx 갱신 보류(파일 열림/쓰기 실패) — 다음 완성 때 재생성: {xe.Message}"); }
            }
            catch (Exception ex)
            {
                log($"[성적서] 기록 실패(검사 결과에는 영향 없음): {sn} — {ex.Message}");
            }
        }

        // ─────────────────────── 성적서 설정 (UI) ───────────────────────

        private void ShowReportSettingsDialog()
        {
            if (_isRunning) { Log("[성적서] 검사 중에는 설정을 바꿀 수 없습니다."); return; }
            if (workspace == null) { MessageBox.Show(this, "워크스페이스가 로드되지 않았습니다."); return; }
            var names = new List<string>();
            try
            {
                foreach (dynamic p in (IEnumerable<object>)workspace.projects[0].procs)
                { try { string nm = Convert.ToString(p.name); if (!string.IsNullOrWhiteSpace(nm) && !names.Contains(nm)) names.Add(nm); } catch { } }
            }
            catch { }
            if (names.Count == 0) { MessageBox.Show(this, "검사 항목(procs)이 없습니다."); return; }
            var (items, n, unset) = ReadReportSettings();

            using var dlg = new Form
            {
                Text = "검사성적서 설정 — 완성 보드 1대가 '열' 하나 (새 보드는 오른쪽에 추가)",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                Size = new Size(600, 630)
            };
            var lblHead = new Label { Text = "성적서 표의 '행'이 될 검사항목 — 기본은 전체이고, 뺄 것만 체크를 해제하십시오.", Location = new Point(12, 12), AutoSize = true };
            var clb = new CheckedListBox { Location = new Point(12, 36), Size = new Size(560, 406), CheckOnClick = true, IntegralHeight = false };
            foreach (var nm in names) clb.Items.Add(nm, unset || items.Contains(nm));
            var btnAll = new Button { Text = "전체 선택", Location = new Point(12, 450), Size = new Size(90, 27) };
            var btnNone = new Button { Text = "전체 해제", Location = new Point(108, 450), Size = new Size(90, 27) };
            btnAll.Click += (s, e) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, true); };
            btnNone.Click += (s, e) => { for (int i = 0; i < clb.Items.Count; i++) clb.SetItemChecked(i, false); };
            var lblN = new Label { Text = "파일당 보드 수(N) — 열이 N개 차면 다음 순번 파일로:", Location = new Point(12, 490), AutoSize = true };
            var nud = new NumericUpDown { Location = new Point(400, 487), Width = 70, Minimum = 1, Maximum = 999, Value = Math.Min(999, Math.Max(1, n)) };
            var lblInfo = new Label
            {
                Text = "N=10 기본(A4 가로 인쇄 1장 기준).  체크 0개 = 성적서 생성 안 함.\r\n"
                     + "※ 검사이력 CSV(보드마다 1줄)와 FAIL 결과는 이 설정과 무관하게 항상 기록됩니다.",
                Location = new Point(12, 516), AutoSize = true, ForeColor = Color.DimGray
            };
            var ok = new Button { Text = "저장", DialogResult = DialogResult.OK, Location = new Point(400, 550), Size = new Size(80, 29) };
            var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(490, 550), Size = new Size(80, 29) };
            dlg.Controls.AddRange(new Control[] { lblHead, clb, btnAll, btnNone, lblN, nud, lblInfo, ok, cancel });
            dlg.AcceptButton = ok; dlg.CancelButton = cancel;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var sel = new List<string>();
            foreach (var o in clb.CheckedItems) sel.Add(o.ToString());
            SaveReportSettings(sel, (int)nud.Value);
        }

        // 메모리 + 로드된 워크스페이스 JSON에 저장 — 전체 재직렬화 금지, 값 토큰만 치환/삽입(주석 동반)
        //   ※ 로드된 실행본 1개만 갱신한다. Debug/Release 소스 JSON 동기는 커밋 시 확인(CLAUDE.md JSON 2벌 규칙).
        private void SaveReportSettings(List<string> items, int n)
        {
            n = Math.Max(1, n);
            try
            {
                var ws = (IDictionary<string, object>)workspace;
                ws["report_items"] = items.Cast<object>().ToList();
                ws["report_serial_count"] = (long)n;
            }
            catch { }
            try
            {
                string path = _currentWorkspacePath;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) { Log("[성적서] 설정 저장 실패: 로드된 워크스페이스 파일을 찾을 수 없음"); return; }
                var bytes = File.ReadAllBytes(path);
                bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                string text = new UTF8Encoding(false).GetString(bom ? bytes.Skip(3).ToArray() : bytes);
                text = UpsertTopLevelJson(text, "report_items", JsonConvert.SerializeObject(items),
                    "\"_report_items_comment\": \"★검사성적서(가로누적 CSV) 항목 — [성적서 설정]에서 체크(사용자 선택). 키 없음=전체, 빈 배열=성적서 생성 안 함\"");
                text = UpsertTopLevelJson(text, "report_serial_count", n.ToString(),
                    "\"_report_serial_count_comment\": \"성적서 파일당 보드(열) 수 N. N개 차면 다음 순번 파일\"");
                // 저장 전 검증: 결과가 JSON으로 읽히지 않으면 파일을 바꾸지 않는다(워크스페이스 파괴 = 다음 실행 로드 불가 → 양산 중단)
                try { JObject.Parse(text); }
                catch (Exception pe) { Log($"[성적서] ★설정 저장 중단 — 결과 JSON 검증 실패, 워크스페이스 파일은 변경하지 않았습니다 ({pe.Message})"); return; }
                var body = new UTF8Encoding(false).GetBytes(text);
                File.WriteAllBytes(path, bom ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(body).ToArray() : body);
                Log($"[성적서] 설정 저장: 항목 {items.Count}개, N={n} → {Path.GetFileName(path)} (※Debug/Release 소스 JSON 동기는 커밋 시 확인)");
            }
            catch (Exception ex) { Log($"[성적서] 설정 저장 실패: {ex.Message}"); }
        }

        // 최상위 키 upsert: 있으면 값 토큰만 치환, 없으면 serial_duplicate_check(및 그 주석) 줄 뒤에 주석과 함께 삽입 (pSMC 동일)
        internal static string UpsertTopLevelJson(string text, string key, string valueToken, string commentPair)
        {
            // 배열 대안은 문자열 리터럴을 통째로 건너뛴다. pSMC 원본 `\[[^\]]*\]`는 항목명 안의 ']'([X슬롯])에서 끊겨
            //   2번째 저장부터 워크스페이스 JSON을 깨뜨렸다(구현 검증 치명 지적 — pSMC 원본에도 같은 결함)
            var re = new Regex("(\"" + Regex.Escape(key) + "\"\\s*:\\s*)(\\[(?:\"(?:[^\"\\\\]|\\\\.)*\"|[^\\]\"])*\\]|\"(?:[^\"\\\\]|\\\\.)*\"|[^,\\r\\n}]+)");
            if (re.IsMatch(text)) return re.Replace(text, m => m.Groups[1].Value + valueToken, 1);

            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            string block = $"  \"{key}\": {valueToken},{nl}  {commentPair},{nl}";
            int anchor = text.IndexOf("\"serial_duplicate_check\"", StringComparison.Ordinal);
            if (anchor >= 0)
            {
                int lineEnd = text.IndexOf('\n', anchor);
                if (lineEnd >= 0)
                {
                    int nextEnd = text.IndexOf('\n', lineEnd + 1);
                    if (nextEnd > lineEnd && text.Substring(lineEnd + 1, nextEnd - lineEnd - 1).Contains("_serial_duplicate_check_comment"))
                        lineEnd = nextEnd;
                    return text.Insert(lineEnd + 1, block);
                }
            }
            int brace = text.IndexOf('{');
            if (brace >= 0)
            {
                int le = text.IndexOf('\n', brace);
                if (le >= 0) return text.Insert(le + 1, block);
            }
            return text;
        }

        // ─────────────────────── 메뉴 (결과 라벨 우클릭) ───────────────────────

        private void OpenFolderOrNotify(string dir, string what)
        {
            if (Directory.Exists(dir))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir) { UseShellExecute = true });
            else
                MessageBox.Show(this, $"{what} 폴더가 아직 없습니다.\n{dir}", "알림");
        }

        internal void AddReportMenuItems(ContextMenuStrip menu)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("성적서 설정", null, (s, e) => ShowReportSettingsDialog());
            menu.Items.Add("성적서 폴더 열기", null, (s, e) => OpenFolderOrNotify(Path.Combine(ResultRootDir(), "성적서"), "성적서"));
            menu.Items.Add("최신 성적서 열기 (xlsx)", null, (s, e) =>
            {
                string dir = Path.Combine(ResultRootDir(), "성적서");
                var f = Directory.Exists(dir) ? new DirectoryInfo(dir).GetFiles("성적서_*.xlsx").OrderByDescending(x => x.LastWriteTime).FirstOrDefault() : null;
                if (f != null) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(f.FullName) { UseShellExecute = true });
                else MessageBox.Show(this, $"성적서 인쇄본(xlsx)이 아직 없습니다.\n{dir}", "알림");
            });
            menu.Items.Add("이력 폴더 열기 (CSV)", null, (s, e) => OpenFolderOrNotify(Path.Combine(ResultRootDir(), "이력"), "이력"));
            menu.Items.Add("FAIL 결과 폴더 열기 (오늘)", null, (s, e) => OpenFolderOrNotify(Path.Combine(ArchiveRootDir(), DateTime.Now.ToString("yyyy-MM-dd"), "FAIL"), "FAIL"));
        }
    }
}
