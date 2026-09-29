using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace flexfab
{
    /// <summary>
    /// ★성적서 인쇄본 xlsx 자동 생성(2026-08-26 사용자 결정 "CSV는 유지, xlsx는 CSV 전체로 매번 새로").
    ///   - 입력 = AppendReportCsv가 방금 기록한 표(List&lt;List&lt;string&gt;&gt;, 행=항목/열=시리얼). CSV가 정본이고 xlsx는 그 인쇄용 사본.
    ///   - 외부 패키지·파이썬·Excel 설치 불필요: xlsx = zip + OOXML 몇 개라 .NET 내장 ZipArchive로 직접 쓴다.
    ///   - 서식 = tools/성적서_인쇄.py와 동일: 테두리·헤더/합격행 음영·열폭(라벨 42·시리얼 13)·긴 값 축소맞춤·A4 가로 1장(fit-to-page)·
    ///     전 셀 텍스트(inlineStr + numFmt '@') — Excel의 '6/6'→날짜 둔갑, '3.300'→'3.3' 끝자리 탈락 차단(2026-08-25 실증).
    ///   - 임시파일에 쓴 뒤 교체 → 도중에 깨진 파일이 남지 않음. 작업자가 xlsx를 Excel로 열어 둔 상태(잠김)면 예외 → 호출측이 로그 1줄,
    ///     다음 PASS 때 CSV 전체로 다시 생성되므로 열 누락 없음. 검사·CSV 기록에는 영향 없음(부가 산출물).
    /// </summary>
    internal static class ReportXlsxWriter
    {
        private const int LBL = 5;   // 라벨 열 수: 검사명·구분·번호·검사번호·세부 검사 항목 (AppendReportCsv 양식 고정)

        /// <summary>★2026-08-31 인쇄본 상단 정보 블록(사내 양식 'CIS AX 검사성적서 양식_최종.xlsx' 상단부와 동일 구성 — 사용자 지시).
        /// 제목 / 개정이력(수기) / 검사일·모델명·S/N 범위·FIRMWARE·검사자·승인. CSV에는 넣지 않는다(정본은 표만). null이면 종전대로 표만.</summary>
        public sealed class Header
        {
            public string Title = "", Date = "", Model = "", SnRange = "", Firmware = "", Inspector = "";
            public string FormNo = "";      // 양식번호(수기 양식 좌하단 'CTS-D-201-01' 자리) — 번호 확정 전엔 빈칸
            public string MacAddress = "None";
            // VibeTilt 이식(2026-09-21): 개정이력 0행을 필드로 분리 — pSMC 원본은 "2026-08-31 / 신규 성적서 개정"이 하드코딩되어 있었다.
            //   문서 개정이력은 품질 문서관리 사항이라 기본은 빈칸(수기 칸). 값을 넣으면 그대로 표시.
            public string RevNo = "0", RevDate = "", RevNote = "";
        }
        private const int HDR = 7;   // 상단 블록 행 수 — CIS AX 검사성적서 양식(사내 표준)과 같은 배치: 제목·개정이력·검사자/승인 박스(1~4행) · 빈 행(5) · 제조일/모델명/SN/Mac(6) · FIRMWARE/검사일/검사자(7)

        /// <summary>표를 xlsx로 저장. 실패 시 예외(호출측에서 로그).</summary>
        public static void Write(string xlsxPath, List<List<string>> table) => Write(xlsxPath, table, null);
        public static void Write(string xlsxPath, List<List<string>> table, Header? header)
        {
            if (table == null || table.Count == 0) throw new ArgumentException("표가 비어 있음");
            int ncol = 0; foreach (var r in table) ncol = Math.Max(ncol, r.Count);
            // 표 영역 = ※ 각주 행 제외(각주는 테두리 없이 아래)
            int tableEnd = table.Count;
            for (int i = 0; i < table.Count; i++)
                if (table[i].Count >= LBL && (table[i][LBL - 1] ?? "").StartsWith("※")) { tableEnd = i; break; }

            string tmp = xlsxPath + ".tmp";
            try
            {
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    Add(zip, "[Content_Types].xml", ContentTypes());
                    Add(zip, "_rels/.rels", Rels());
                    Add(zip, "xl/workbook.xml", Workbook());
                    Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
                    Add(zip, "xl/styles.xml", Styles());
                    // ★2026-08-31 인쇄본은 '검사번호(#16-2.1 …)' 열을 뺀다 — CIS 양식(검사명·구분·번호·세부 검사 항목)과 같게(사용자 지시). CSV는 그대로(엑셀 항목번호 추적용).
                    var t2 = new List<List<string>>();
                    foreach (var r in table)
                    {
                        var c2 = new List<string>(r);
                        if (c2.Count > 3) c2.RemoveAt(3);
                        t2.Add(c2);
                    }
                    Add(zip, "xl/worksheets/sheet1.xml", Sheet(t2, ncol - 1, tableEnd, header, LBL - 1));
                }
                if (File.Exists(xlsxPath)) File.Replace(tmp, xlsxPath, null); else File.Move(tmp, xlsxPath);
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static void Add(ZipArchive zip, string name, string xml)
        {
            var e = zip.CreateEntry(name, CompressionLevel.Optimal);
            using var s = e.Open();
            var b = new UTF8Encoding(false).GetBytes(xml);
            s.Write(b, 0, b.Length);
        }

        private static string X(string s) => System.Security.SecurityElement.Escape(s ?? "") ?? "";

        private static string ContentTypes() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
            "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "</Types>";

        private static string Rels() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
            "</Relationships>";

        private static string Workbook() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<sheets><sheet name=\"성적서\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

        private static string WorkbookRels() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
            "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
            "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
            "</Relationships>";

        // cellXfs 인덱스: 0 기본 / 1 본문 가운데+테두리+축소 / 2 라벨 왼쪽+테두리+축소 / 3 헤더(굵게·음영·가운데·테두리)
        //                4 합격행 값(굵게·음영·가운데·테두리) / 5 합격행 라벨(굵게·음영·왼쪽·테두리) / 6 각주(회색 8pt·왼쪽·테두리 없음) / 7 본문 가운데+테두리+줄바꿈(긴 값)
        private static string Styles() =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<fonts count=\"4\">" +
            "<font><sz val=\"9\"/><name val=\"맑은 고딕\"/></font>" +
            "<font><b/><sz val=\"9\"/><name val=\"맑은 고딕\"/></font>" +
            "<font><sz val=\"8\"/><color rgb=\"FF808080\"/><name val=\"맑은 고딕\"/></font>" +
            "<font><b/><sz val=\"14\"/><name val=\"맑은 고딕\"/></font>" +
            "</fonts>" +
            "<fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
            "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFDDEBF7\"/></patternFill></fill>" +
            "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE2EFDA\"/></patternFill></fill></fills>" +
            "<borders count=\"2\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
            "<border><left style=\"thin\"><color rgb=\"FF808080\"/></left><right style=\"thin\"><color rgb=\"FF808080\"/></right>" +
            "<top style=\"thin\"><color rgb=\"FF808080\"/></top><bottom style=\"thin\"><color rgb=\"FF808080\"/></bottom><diagonal/></border></borders>" +
            "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
            "<cellXfs count=\"14\">" +
            "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"0\" applyNumberFormat=\"1\"/>" +
            "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" shrinkToFit=\"1\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\" shrinkToFit=\"1\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"2\" borderId=\"1\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"3\" borderId=\"1\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"3\" borderId=\"1\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"2\" fillId=\"0\" borderId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\"/></xf>" +
            // 7 본문 가운데+테두리+★줄바꿈(2026-08-31 — 시리얼 열 긴 셀: 축소하면 2pt대로 인쇄 불가, 줄바꿈 3줄이면 읽힘. 모션 2.4/2.9 실측)
            "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
            // 8 제목(굵게 14pt·왼쪽·테두리 없음) / 9 상단 정보 값(가운데·테두리 — 2026-08-31 사용자 "웬만한 건 가운데") / 10 상단 정보 라벨(굵게·음영·가운데·테두리) — 2026-08-31 상단 블록
            "<xf numFmtId=\"49\" fontId=\"3\" fillId=\"0\" borderId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"left\" vertical=\"center\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" shrinkToFit=\"1\"/></xf>" +
            "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"2\" borderId=\"1\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            // 11 세로 라벨('개정이력', CIS 양식 J2:J13 세로쓰기)
            "<xf numFmtId=\"49\" fontId=\"1\" fillId=\"2\" borderId=\"1\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" textRotation=\"255\"/></xf>" +
            // 12 제목(굵게 14pt·가운데·테두리) — 제목 박스
            "<xf numFmtId=\"49\" fontId=\"3\" fillId=\"0\" borderId=\"1\" applyNumberFormat=\"1\" applyFont=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\"/></xf>" +
            // 13 세부 검사 항목 2줄(이름 ↵ (spec)) — 가운데·테두리·줄바꿈
            "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"1\" applyNumberFormat=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
            "</cellXfs>" +
            "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
            "</styleSheet>";

        private static string ColName(int idx1)   // 1 → A, 27 → AA
        {
            var sb = new StringBuilder();
            for (int n = idx1; n > 0; n = (n - 1) / 26) sb.Insert(0, (char)('A' + (n - 1) % 26));
            return sb.ToString();
        }

        private static string Sheet(List<List<string>> table, int ncol, int tableEnd, Header? header, int lbl)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\" showGridLines=\"0\"/></sheetViews>");
            sb.Append("<sheetFormatPr defaultRowHeight=\"18\" customHeight=\"1\"/>");
            // 열폭: 라벨 9·9·5·9·42, 시리얼 13 (tools/성적서_인쇄.py와 동일)
            double[] lblW = { 9, 9, 5, 42 };   // 검사명·구분·번호·세부 검사 항목(검사번호 열은 인쇄본에서 제외)
            sb.Append("<cols>");
            for (int j = 1; j <= Math.Max(ncol, lbl); j++)
            {
                double w = j <= lbl ? lblW[j - 1] : 13;
                sb.Append($"<col min=\"{j}\" max=\"{j}\" width=\"{w}\" customWidth=\"1\"/>");
            }
            sb.Append("</cols><sheetData>");
            int off = 0;                       // 표 시작 행 오프셋(상단 블록이 있으면 HDR)
            var merges = new List<string>();
            if (header != null)
            {
                off = HDR;
                // 셀 사전: (row,col) → (style,text). 박스(병합)는 범위 안 모든 셀을 테두리 스타일로 채워야 선이 끊기지 않는다(OOXML은 셀 단위 테두리).
                var cellMap = new Dictionary<(int r, int c), (int st, string v)>();
                void Box(int c1, int r1, int c2, int r2, int st, string v)
                {
                    for (int r = r1; r <= r2; r++)
                        for (int c = c1; c <= c2; c++)
                            cellMap[(r, c)] = (st, (r == r1 && c == c1) ? v : "");
                    if (c2 > c1 || r2 > r1) merges.Add($"{ColName(c1)}{r1}:{ColName(c2)}{r2}");
                }
                // ★사내 양식 'CIS AX 검사성적서'(=제조팀 수기 VL20 양식)와 같은 상단 배치 — 2026-08-31 사용자 지시
                //   ("위쪽 행과 열을 CIS 엑셀과 같게", "제목·개정이력·검사자/승인은 각각 별도 박스, 제조일 행 위에 빈 행").
                //   1~4행: [제목 A:D] (E 빈열) [개정이력 F 세로 | G No | H 개정일 | I:K 개정 내용 ×3칸] (L 빈열) [M 검사자 | N:O 승인]
                //   5행 빈 행 / 6행 제조일(수기)·모델명·SN·Mac Address / 7행 사양(수기)·우측 빈 칸 / 8행부터 표
                //   14열(A~N) 기준: [제목 A:D] (E 빈열) [개정이력 F 세로 | G No | H 개정일 | I:K 개정 내용] (L 빈열) [M 검사자 | N 승인]
                Box(1, 1, 4, 4, 12, header.Title);
                Box(6, 1, 6, 4, 11, "개정이력");
                Box(7, 1, 7, 1, 10, "No"); Box(8, 1, 8, 1, 10, "개정일"); Box(9, 1, 11, 1, 10, "개정 내용");
                Box(7, 2, 7, 2, 9, header.RevNo); Box(8, 2, 8, 2, 9, header.RevDate); Box(9, 2, 11, 2, 9, header.RevNote);
                Box(7, 3, 7, 3, 9, "1"); Box(8, 3, 8, 3, 9, ""); Box(9, 3, 11, 3, 9, "");
                Box(7, 4, 7, 4, 9, "2"); Box(8, 4, 8, 4, 9, ""); Box(9, 4, 11, 4, 9, "");
                Box(13, 1, 13, 1, 10, "검사자"); Box(14, 1, 14, 1, 10, "승인");   // CIS와 같게 검사자·승인 각 1열(사용자 08-31)
                Box(13, 2, 13, 4, 9, header.Inspector); Box(14, 2, 14, 4, 9, "");   // 검사자=메인폼 라벨 자동, 승인=수기
                string mfg = "제조일 :  202   년    월    일";
                if (DateTime.TryParse(header.Date, out var dd)) mfg = $"제조일 : {dd:yyyy}년 {dd:MM}월 {dd:dd}일";   // 검사일로 자동 기입(사용자 08-31 "여기에 2026년08월31일 오면 돼")
                Box(1, 6, 2, 6, 2, mfg); Box(3, 6, 4, 6, 2, $"모델명 : {header.Model}");   // 왼쪽 정렬 예외 = 제조일·모델명·세부 검사 항목(사용자 08-31)
                Box(5, 6, 5, 6, 10, "SN"); Box(6, 6, 9, 6, 9, header.SnRange);
                Box(10, 6, 11, 6, 10, "Mac Address"); Box(12, 6, 14, 6, 9, header.MacAddress);
                Box(1, 7, 4, 7, 2, "사양 : ");   // VL20 수기 양식의 '사양 :' 행 그대로(수기 칸, 사용자 08-31). FW는 표 #00 셀에 있음
                Box(5, 7, 14, 7, 9, "");   // 7행 우측 빈 칸(사용자 08-31 "S/N 지워" — SN은 6행에 이미 있음). 검사일시는 양식에 없음 — 표의 '검사일시' 행이 담당
                double[] hts = { 15, 15, 15, 15, 6, 18, 18 };
                for (int r = 1; r <= HDR; r++)
                {
                    sb.Append($"<row r=\"{r}\" ht=\"{hts[r - 1]}\" customHeight=\"1\">");
                    for (int c = 1; c <= 14; c++)
                        if (cellMap.TryGetValue((r, c), out var cv))
                            sb.Append($"<c r=\"{ColName(c)}{r}\" s=\"{cv.st}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{X(cv.v)}</t></is></c>");
                    sb.Append("</row>");
                }
            }
            // ★2026-08-31 검사명(A)·구분(B) 열 — 연속 같은 값은 세로 병합(CIS 양식 B18:B41·C18:C30처럼, 사용자 지시 "A,B열 각각 합치기").
            //   대상 = 데이터 행(머리 2행·합격 행·각주 제외). 병합 범위 안 첫 칸만 값, 나머지는 빈칸(스타일·테두리는 유지).
            var blank = new HashSet<(int r, int c)>();
            for (int col = 0; col < Math.Min(2, lbl); col++)
            {
                int runStart = -1; string runVal = null;
                void Flush(int endExclusive)
                {
                    if (runStart >= 0 && endExclusive - runStart > 1)
                    {
                        merges.Add($"{ColName(col + 1)}{runStart + 1 + off}:{ColName(col + 1)}{endExclusive + off}");
                        for (int k = runStart + 1; k < endExclusive; k++) blank.Add((k, col));
                    }
                    runStart = -1; runVal = null;
                }
                for (int i = 0; i < table.Count; i++)
                {
                    var r0 = table[i];
                    bool isPass0 = i < tableEnd && r0.Count >= lbl && (r0[lbl - 1] ?? "").Contains("합격");
                    bool data0 = i >= 2 && i < tableEnd && !isPass0;
                    string v0 = data0 && r0.Count > col ? (r0[col] ?? "") : "";
                    if (!data0 || v0.Length == 0) { Flush(i); continue; }
                    if (runStart >= 0 && v0 == runVal) continue;
                    Flush(i); runStart = i; runVal = v0;
                }
                Flush(table.Count);
            }
            for (int i = 0; i < table.Count; i++)
            {
                var row = table[i];
                bool isHeader = i == 0;
                bool isPassRow = i < tableEnd && row.Count >= lbl && (row[lbl - 1] ?? "").Contains("합격");
                bool isNote = i >= tableEnd;
                int cells = isNote ? row.Count : Math.Max(ncol, row.Count);
                // ★2026-08-31 긴 값은 줄바꿈(스타일 7) + 행 높이 = 줄 수 × 18. 줄 수는 시리얼 열폭 13 기준 보수 추정(11자/줄, 최대 5줄).
                //   shrinkToFit(스타일 1)만으로는 37자 셀이 인쇄에서 2pt대(A4 1장 맞춤 70% × 13/37)라 읽히지 않았다(모션 2.4/2.9 실측).
                //   짧은 값(BOARD/SHIP 대부분)은 1줄이라 종전과 동일.
                bool dataRow = !isHeader && !isNote && !isPassRow && i >= 2;
                int maxLines = 1;
                // ★2026-08-31 세부 검사 항목이 열 폭(42)을 넘으면 "이름 ↵ (spec)" 2줄로(사용자 "이렇게 줄바꿈 되나?"). 마지막 " (" 앞에서 분리.
                string labelWrapped = null;
                if (dataRow && row.Count >= lbl)
                {
                    string lv = row[lbl - 1] ?? "";
                    // 끝의 괄호 묶음 시작 위치(역추적, 중첩 괄호 포함 — 2.4 spec "(10.5±0.2 (10.3~10.7…)…)" 대응)
                    int cut = -1;
                    if (lv.EndsWith(")"))
                    {
                        int depth = 0;
                        for (int k = lv.Length - 1; k >= 0; k--)
                        {
                            if (lv[k] == ')') depth++;
                            else if (lv[k] == '(') { depth--; if (depth == 0) { cut = k; break; } }
                        }
                    }
                    if (lv.Length > 40 && cut > 1 && lv[cut - 1] == ' ') { labelWrapped = lv.Substring(0, cut - 1) + "\n" + lv.Substring(cut); maxLines = 2; }
                }
                if (dataRow)
                    for (int j = lbl; j < cells; j++)
                    {
                        string v0 = j < row.Count ? row[j] ?? "" : "";
                        if (v0.Length > 13) maxLines = Math.Max(maxLines, Math.Min(5, (v0.Length + 10) / 11));
                    }
                sb.Append($"<row r=\"{i + 1 + off}\" ht=\"{18 * maxLines}\" customHeight=\"1\">");
                // ★2026-08-31 합격 행 = CIS 양식과 동일: 라벨 열 A~(lbl) 병합·가운데·굵기/음영 없음, 값도 가운데(사용자 지시 "정렬·행합치기")
                if (isPassRow) merges.Add($"A{i + 1 + off}:{ColName(lbl)}{i + 1 + off}");
                for (int j = 0; j < cells; j++)
                {
                    string v = j < row.Count ? row[j] ?? "" : "";
                    if (isPassRow && j < lbl) v = j == 0 ? (row.Count >= lbl ? row[lbl - 1] ?? "" : "") : "";   // 병합 첫 칸에 라벨
                    if (blank.Contains((i, j))) v = "";   // 검사명·구분 세로 병합의 하위 칸
                    bool lblWrap = dataRow && j == lbl - 1 && labelWrapped != null;
                    if (lblWrap) v = labelWrapped;
                    int st = isNote ? 6
                           : isHeader ? 3
                           : isPassRow ? 1
                           : lblWrap ? 13
                           : (dataRow && j >= lbl && v.Length > 13 ? 7 : 1);   // 세부 검사 항목 열도 가운데(사용자 08-31 — 왼쪽 예외는 제조일·모델명·사양만)
                    if (isNote && v.Length == 0) continue;
                    sb.Append($"<c r=\"{ColName(j + 1)}{i + 1 + off}\" s=\"{st}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{X(v)}</t></is></c>");
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");
            if (merges.Count > 0)
            {
                sb.Append($"<mergeCells count=\"{merges.Count}\">");
                foreach (var m in merges) sb.Append($"<mergeCell ref=\"{m}\"/>");
                sb.Append("</mergeCells>");
            }
            sb.Append("<pageMargins left=\"0.4\" right=\"0.4\" top=\"0.5\" bottom=\"0.5\" header=\"0.3\" footer=\"0.3\"/>");
            sb.Append("<pageSetup paperSize=\"9\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"1\"/>");   // A4 가로 1장
            if (header != null)   // 수기 양식 바닥글: 좌 양식번호 / 중 (주)캔탑스 / 우 제조팀
            {
                // 양식번호가 비면 &L 구간을 아예 생략 — "&L&C…"처럼 빈 구간이 붙으면 파서(Excel/openpyxl)가 좌측 텍스트로 오독함(08-31 _03 실물 확인)
                string left = string.IsNullOrWhiteSpace(header.FormNo) ? "" : $"&amp;L{X(header.FormNo)}";
                sb.Append($"<headerFooter><oddFooter>{left}&amp;C(주)캔탑스&amp;R제조팀</oddFooter></headerFooter>");
            }
            sb.Append("</worksheet>");
            return sb.ToString();
        }
    }
}
