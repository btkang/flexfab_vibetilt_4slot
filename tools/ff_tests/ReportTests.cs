using System.Dynamic;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ff_tests;

// 3단계: 검사성적서·검사이력 CSV·FAIL JSON (PLAN_4슬롯_성적서_이력CSV_FAIL저장_20260921_v01)
// 순수 로직(internal static)을 파일 I/O 없이 검증한다.
public class ReportLogicTests
{
    static readonly UTF8Encoding Enc = new(false);
    static readonly Type RowT = typeof(flexfab.MainForm).GetNestedType("ReportRow", System.Reflection.BindingFlags.NonPublic)!;

    static object Row(string proc, string grp, string num, string label)
    {
        var r = Activator.CreateInstance(RowT, true)!;
        RowT.GetField("Proc")!.SetValue(r, proc); RowT.GetField("Group")!.SetValue(r, grp);
        RowT.GetField("Num")!.SetValue(r, num); RowT.GetField("Label")!.SetValue(r, label);
        return r;
    }
    static System.Collections.IList Rows(params object[] rs)
    {
        var l = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(RowT))!;
        foreach (var r in rs) l.Add(r);
        return l;
    }
    static List<List<string>> NewTable(string name, System.Collections.IList rows, string? note = null) =>
        (List<List<string>>)Priv.Static("NewReportTable", name, rows, note)!;

    // ── CSV ──
    [Theory]
    [InlineData("VL1-00012")]
    [InlineData("a,b")]
    [InlineData("say \"hi\"")]
    [InlineData("FAIL(X 기록 없음: 같은 보드(A) 재투입)")]
    [InlineData("")]
    public void CSV_이스케이프_왕복(string v)
    {
        string line = string.Join(",", new[] { "x", v, "y" }.Select(s => (string)Priv.Static("CsvEsc", s)!));
        var cells = (List<string>)Priv.Static("CsvSplitLine", line)!;
        Assert.Equal(new[] { "x", v, "y" }, cells);
    }

    [Fact]
    public void CSV_BOM_있는_파일도_첫셀이_정확()
    {
        string p = Path.Combine(Priv.TempDir(), "t.csv");
        File.WriteAllText(p, "시리얼,검사일시\r\nA,B\r\n", new UTF8Encoding(true));
        var t = (List<List<string>>)Priv.Static("ReadCsvTableStrict", p)!;
        Assert.Equal("시리얼", t[0][0]);   // BOM이 첫 셀에 붙으면 헤더 인식 실패 → 매번 새 파일 분리
        Assert.Equal(2, t.Count);
    }

    // ── 파일명 ──
    [Theory]
    [InlineData("VL1-00012", "VL1-00012")]
    [InlineData("VL1-..\\x", "VL1-__x")]
    [InlineData("a/b:c", "a_b_c")]
    [InlineData("  ", "Null")]
    public void SafeFileName_경로문자_제거(string sn, string expected) =>
        Assert.Equal(expected, (string)Priv.Static("SafeFileName", sn)!);

    [Fact]
    public void PASS_DUPLICATE_같은초_충돌은_번호로_보존()
    {
        string d = Priv.TempDir();
        var t = new DateTime(2026, 9, 21, 10, 0, 0);
        string p1 = (string)Priv.Static("PassDuplicatePath", d, "VL1-00012", t)!;
        Assert.EndsWith(Path.Combine("PASS_DUPLICATE", "VL1-00012_20260921_100000.json"), p1);
        Directory.CreateDirectory(Path.GetDirectoryName(p1)!); File.WriteAllText(p1, "{}");
        string p2 = (string)Priv.Static("PassDuplicatePath", d, "VL1-00012", t)!;
        Assert.EndsWith("VL1-00012_20260921_100000_2.json", p2);
    }

    [Fact]
    public void FAIL_JSON_파일명에_단계와_시각()
    {
        string d = Priv.TempDir();
        var t = new DateTime(2026, 9, 21, 10, 5, 7);
        string p = (string)Priv.Static("FailJsonPath", d, "VL1-00012", "Y", t)!;
        Assert.EndsWith(Path.Combine("FAIL", "VL1-00012_Y_20260921_100507.json"), p);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!); File.WriteAllText(p, "{}");
        Assert.EndsWith("VL1-00012_Y_20260921_100507_2.json", (string)Priv.Static("FailJsonPath", d, "VL1-00012", "Y", t)!);
    }

    // ── 이력 ──
    [Fact]
    public void 이력_파일선택_분할본_있으면_가장_최신()
    {
        string d = Priv.TempDir(); var now = new DateTime(2026, 9, 21);
        var args = new object?[] { d, "P", now, null };
        Assert.EndsWith("이력_P_20260921.csv", (string)Priv.Static("PickHistoryPath", args)!);
        File.WriteAllText(Path.Combine(d, "이력_P_20260921.csv"), "x");
        File.WriteAllText(Path.Combine(d, "이력_P_20260921_02.csv"), "x");
        File.WriteAllText(Path.Combine(d, "이력_P_20260921_03.csv"), "x");
        Assert.EndsWith("이력_P_20260921_03.csv", (string)Priv.Static("PickHistoryPath", args)!);
        Assert.EndsWith("이력_P_20260921_04.csv", (string)Priv.Static("NextSplitPath", Path.Combine(d, "이력_P_20260921"))!);
    }

    [Theory]
    [InlineData("OK", "OK")]
    [InlineData("FAIL", "FAIL")]
    [InlineData("Skip", "-")]
    [InlineData("...", "-")]
    [InlineData("", "-")]
    [InlineData("—", "")]
    public void 이력_셀값_변환(string cell, string expected) => Assert.Equal(expected, (string)Priv.Static("HistCell", cell)!);

    [Fact]
    public void 이력_헤더비교()
    {
        var a = new List<string> { "시리얼", "#1" }; var b = new List<string> { "시리얼", "#1" };
        Assert.True((bool)Priv.Static("HeaderMatches", a, b)!);
        Assert.False((bool)Priv.Static("HeaderMatches", a, new List<string> { "시리얼", "#2" })!);
        Assert.False((bool)Priv.Static("HeaderMatches", null, b)!);
    }

    // ── 성적서 행·표 ──
    [Theory]
    [InlineData("#1-X [X슬롯] FW 버전 확인", "", "#1-X", "FW 버전 확인")]
    [InlineData("#9 [Y결합] 시리얼번호 설정", "", "#9", "시리얼번호 설정")]
    [InlineData("#5-1 X축 정지 검사(0도)", "0.98~1.02G", "#5-1", "X축 정지 검사(0도) (0.98~1.02G)")]
    [InlineData("통신검사", "", "", "통신검사")]
    public void 성적서_항목명_분리(string name, string spec, string num, string label)
    {
        var args = new object?[] { name, spec, null, null };
        Priv.Static("SplitReportName", args);
        Assert.Equal(num, args[2]); Assert.Equal(label, args[3]);
    }

    [Theory]
    [InlineData("common", "공통")]
    [InlineData("X", "X단계")]
    [InlineData("Y", "Y단계")]
    [InlineData("all", "모션")]
    [InlineData("__invalid", "")]
    public void 성적서_구분(string g, string label) => Assert.Equal(label, (string)Priv.Static("GroupLabelOf", g)!);

    static System.Collections.IList TwoRows() => Rows(Row("#1-X FW", "X단계", "#1-X", "FW"), Row("#9 UID", "Y단계", "#9", "UID"));

    [Fact]
    public void 성적서_새표_구조()
    {
        var t = NewTable("VL10 모션", TwoRows(), "※ 분리");
        Assert.Equal(new[] { "검사명", "구분", "번호", "검사번호", "세부 검사 항목" }, t[0]);
        Assert.Equal("검사일시", t[1][4]);
        Assert.Equal(new[] { "VL10 모션", "X단계", "1", "#1-X", "FW" }, t[2]);
        Assert.Equal(new[] { "VL10 모션", "Y단계", "2", "#9", "UID" }, t[3]);
        Assert.Equal("합격, 불합격 판정", t[4][4]);
        Assert.StartsWith("※", t[5][4]);
        Assert.True((bool)Priv.Static("IsReportTable", t)!);
    }

    [Fact]
    public void 성적서_행구성_비교는_각주_판정행_무시()
    {
        var t = NewTable("P", TwoRows(), "※ 분리");
        Assert.True((bool)Priv.Static("ReportRowsMatch", t, TwoRows())!);
        Assert.False((bool)Priv.Static("ReportRowsMatch", t, Rows(Row("#1-X FW", "X단계", "#1-X", "FW (새 기준)"), Row("#9 UID", "Y단계", "#9", "UID")))!);
        Assert.False((bool)Priv.Static("ReportRowsMatch", t, Rows(Row("#1-X FW", "X단계", "#1-X", "FW")))!);
    }

    [Fact]
    public void 성적서_열배치_새열_재검_가득()
    {
        var t = NewTable("P", TwoRows());
        Assert.Equal(-1, (int)Priv.Static("ReportColumnOf", t, "A", 2)!);            // 새 열
        int ca = (int)Priv.Static("FillReportColumn", t, -1, "A", "2026-09-21 10:00")!;
        Assert.Equal(5, ca);
        Assert.Equal(5, (int)Priv.Static("ReportColumnOf", t, "A", 2)!);             // 재검 = 기존 열
        Priv.Static("FillReportColumn", t, -1, "B", "2026-09-21 10:05");
        Assert.Equal(int.MinValue, (int)Priv.Static("ReportColumnOf", t, "C", 2)!);  // N=2 가득 → 다음 파일
        Assert.Equal(5, (int)Priv.Static("ReportColumnOf", t, "A", 2)!);             // 가득이어도 재검은 기존 열
    }

    [Fact]
    public void 성적서_열채우기_값()
    {
        var t = NewTable("P", TwoRows(), "※ 분리");
        int c = (int)Priv.Static("FillReportColumn", t, -1, "VL1-00012", "2026-09-21 10:00")!;
        Assert.Equal("VL1-00012", t[0][c]);
        Assert.Equal("2026-09-21 10:00", t[1][c]);
        Assert.Equal("OK", t[2][c]); Assert.Equal("OK", t[3][c]);
        Assert.Equal("합", t[4][c]);
        Assert.Equal("", t[5][c]);   // 각주 행은 비움
        Assert.All(t, r => Assert.Equal(t[0].Count, r.Count));
    }

    [Fact]
    public void 성적서_재검은_열을_늘리지_않고_검사일시만_갱신()
    {
        var t = NewTable("P", TwoRows());
        Priv.Static("FillReportColumn", t, -1, "A", "2026-09-21 10:00");
        int col = (int)Priv.Static("ReportColumnOf", t, "A", 10)!;
        Priv.Static("FillReportColumn", t, col, "A", "2026-09-21 11:00");
        Assert.Equal(6, t[0].Count);
        Assert.Equal("2026-09-21 11:00", t[1][5]);
    }

    [Fact]
    public void 성적서_파일선택_오늘_최신순번_어제파일_무시()
    {
        string d = Priv.TempDir(); var now = new DateTime(2026, 9, 21);
        Assert.EndsWith("성적서_P_20260921_01.csv", (string)Priv.Static("PickReportPath", d, "P", now, false)!);
        File.WriteAllText(Path.Combine(d, "성적서_P_20260920_05.csv"), "x");   // 어제
        File.WriteAllText(Path.Combine(d, "성적서_P_20260921_01.csv"), "x");
        File.WriteAllText(Path.Combine(d, "성적서_P_20260921_02.csv"), "x");
        Assert.EndsWith("성적서_P_20260921_02.csv", (string)Priv.Static("PickReportPath", d, "P", now, false)!);
        Assert.EndsWith("성적서_P_20260921_03.csv", (string)Priv.Static("PickReportPath", d, "P", now, true)!);
    }

    // ── 워크스페이스 JSON 부분 갱신 ──
    [Fact]
    public void UpsertTopLevelJson_있으면_값만_치환()
    {
        string j = "{\n  \"report_items\": [\"a\", \"b\"],\n  \"report_serial_count\": 10,\n  \"x\": 1\n}";
        j = (string)Priv.Static("UpsertTopLevelJson", j, "report_items", "[\"c\"]", "\"_c\": \"\"")!;
        j = (string)Priv.Static("UpsertTopLevelJson", j, "report_serial_count", "5", "\"_c\": \"\"")!;
        var o = JObject.Parse(j);
        Assert.Equal(new[] { "c" }, o["report_items"]!.ToObject<string[]>());
        Assert.Equal(5, (int)o["report_serial_count"]!);
        Assert.Equal(1, (int)o["x"]!);
    }

    [Fact]
    public void UpsertTopLevelJson_없으면_중복검사_주석_뒤에_주석과_삽입()
    {
        string j = "{\r\n  \"serial_duplicate_check\": 0,\r\n  \"_serial_duplicate_check_comment\": \"c\",\r\n  \"libraries\": []\r\n}";
        j = (string)Priv.Static("UpsertTopLevelJson", j, "report_serial_count", "10", "\"_report_serial_count_comment\": \"N\"")!;
        var o = JObject.Parse(j);
        Assert.Equal(10, (int)o["report_serial_count"]!);
        Assert.Equal("N", (string)o["_report_serial_count_comment"]!);
        Assert.True(j.IndexOf("_serial_duplicate_check_comment") < j.IndexOf("report_serial_count"));
        Assert.Contains("\r\n  \"report_serial_count\"", j);   // 원본 줄바꿈(CRLF) 유지
    }

    // ── 설정·행 정의 (워크스페이스) ──
    static flexfab.MainForm FormWith(ExpandoObject ws) { var f = Priv.Bare(); Priv.SetField(f, "workspace", ws); return f; }

    static ExpandoObject Ws(object? reportItems, long? n, params ExpandoObject[] procs)
    {
        var ws = Priv.Workspace(procs);
        var d = (IDictionary<string, object?>)ws;
        ((IDictionary<string, object?>)((List<object>)d["projects"]!)[0])["name"] = "VL10 모션검사 (RS-232)_TEST_4slot";
        if (reportItems != null) d["report_items"] = reportItems;
        if (n != null) d["report_serial_count"] = n;
        return ws;
    }

    [Fact]
    public void 설정_키없음은_전체_N기본10()
    {
        var f = FormWith(Ws(null, null, Priv.ProcSg("#1 A", "X")));
        dynamic r = Priv.Call(f, "ReadReportSettings")!;
        Assert.True((bool)r.Item3); Assert.Equal(10, (int)r.Item2);
        var args = new object?[] { null };
        var rows = (System.Collections.IList)Priv.Call(f, "BuildReportRows", args)!;
        Assert.True((bool)args[0]!); Assert.Equal(1, rows.Count);
    }

    [Fact]
    public void 설정_빈배열은_성적서_off()
    {
        var f = FormWith(Ws(new List<object>(), 5L, Priv.ProcSg("#1 A", "X")));
        var args = new object?[] { null };
        var rows = (System.Collections.IList)Priv.Call(f, "BuildReportRows", args)!;
        Assert.False((bool)args[0]!); Assert.Empty(rows);
    }

    [Fact]
    public void 행정의_report_items_필터_구분_기준()
    {
        var p3 = Priv.Proc("#3 영점", new Dictionary<string, object?> { ["slot_group"] = "common", ["report_spec"] = "±0.1,도", ["report_group"] = "준비" });
        var f = FormWith(Ws(new List<object> { "#9 [Y슬롯] UID", "#3 영점" }, 10L,
            Priv.ProcSg("#1-X [X슬롯] FW", "X"), Priv.ProcSg("#9 [Y슬롯] UID", "Y"), p3));
        var args = new object?[] { null };
        var rows = (System.Collections.IList)Priv.Call(f, "BuildReportRows", args)!;
        Assert.Equal(2, rows.Count);                                  // #1-X는 report_items에 없음
        Assert.Equal("#9", RowT.GetField("Num")!.GetValue(rows[0]));    // 워크스페이스 proc 순서
        Assert.Equal("UID", RowT.GetField("Label")!.GetValue(rows[0]));
        Assert.Equal("Y단계", RowT.GetField("Group")!.GetValue(rows[0]));
        Assert.Equal("준비", RowT.GetField("Group")!.GetValue(rows[1]));     // report_group 우선
        Assert.Equal("영점 (±0.1·도)", RowT.GetField("Label")!.GetValue(rows[1]));   // report_spec, 콤마 → 가운뎃점
    }
}

// 구현 검증(Fable 3개, 2026-09-21) 지적 수정분 고정
public class ReportReviewFixTests
{
    static string Up(string j, string key, string val) =>
        (string)Priv.Static("UpsertTopLevelJson", j, key, val, $"\"_{key}_comment\": \"c\"")!;

    // [치명] 항목명 안의 ']'([X슬롯])에서 정규식이 끊겨 2번째 저장부터 워크스페이스 JSON이 깨지던 결함
    [Fact]
    public void Upsert_대괄호_포함_항목명_2회_저장해도_JSON_유효()
    {
        string j = "{\r\n  \"serial_duplicate_check\": 0,\r\n  \"libraries\": []\r\n}";
        var first = new[] { "#1-X [X슬롯] FW 버전 확인", "#9 [Y슬롯] 시리얼번호 설정 (Y축)" };
        var second = new[] { "#8 [Y슬롯] FW 버전 확인 (Y축)" };
        j = Up(j, "report_items", Newtonsoft.Json.JsonConvert.SerializeObject(first));
        j = Up(j, "report_items", Newtonsoft.Json.JsonConvert.SerializeObject(second));   // 종전 결함: 여기서 파괴
        j = Up(j, "report_items", Newtonsoft.Json.JsonConvert.SerializeObject(first));
        var o = JObject.Parse(j);
        Assert.Equal(first, o["report_items"]!.ToObject<string[]>());
    }

    [Fact]
    public void Upsert_실제_4슬롯_워크스페이스로_3회_왕복()
    {
        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string ws = Path.Combine(repo, "output", "Debug", "net8.0-windows", "MP_workspace_motion_232_VL10_4slot.json");
        Assert.True(File.Exists(ws), ws);
        string orig = File.ReadAllText(ws);
        var names = JObject.Parse(orig)["projects"]![0]!["procs"]!.Select(p => (string)p["name"]!).ToList();
        Assert.Contains(names, n => n.Contains(']'));   // 실제 항목명이 ']'를 포함 — 결함 트리거 조건
        string j = orig;
        for (int k = 0; k < 3; k++)
        {
            j = Up(j, "report_items", Newtonsoft.Json.JsonConvert.SerializeObject(names.Skip(k).ToList()));
            j = Up(j, "report_serial_count", (10 + k).ToString());
        }
        var o = JObject.Parse(j);
        Assert.Equal(names.Skip(2).ToList(), o["report_items"]!.ToObject<List<string>>());
        Assert.Equal(12, (int)o["report_serial_count"]!);
        var oo = JObject.Parse(orig);   // 나머지 키 불변
        foreach (var p in oo.Properties()) Assert.True(JToken.DeepEquals(p.Value, o[p.Name]), p.Name);
    }

    [Fact]
    public void 잠김_판정은_공유위반만()
    {
        string p = Path.Combine(Priv.TempDir(), "lock.csv");
        File.WriteAllText(p, "x");
        using (new FileStream(p, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var ex = Assert.ThrowsAny<IOException>(() => File.WriteAllText(p, "y"));
            Assert.True((bool)Priv.Static("IsLockViolation", ex)!);
        }
        Assert.False((bool)Priv.Static("IsLockViolation", new IOException("disk full", unchecked((int)0x80070070)))!);
    }

    [Fact]
    public void 이어쓰기_끝개행_보정()
    {
        string d = Priv.TempDir();
        string a = Path.Combine(d, "a.csv"); File.WriteAllText(a, "h\r\nrow1");        // 끝 개행 없음
        string b = Path.Combine(d, "b.csv"); File.WriteAllText(b, "h\r\nrow1\r\n");
        Assert.Equal(Environment.NewLine + "row2", (string)Priv.Static("PrefixNewlineIfNeeded", a, "row2")!);
        Assert.Equal("row2", (string)Priv.Static("PrefixNewlineIfNeeded", b, "row2")!);
        Assert.Equal("row2", (string)Priv.Static("PrefixNewlineIfNeeded", Path.Combine(d, "none.csv"), "row2")!);
    }

    [Fact]
    public void 성적서_열매칭_대소문자_무시()
    {
        var t = new List<List<string>>
        {
            new() { "검사명", "구분", "번호", "검사번호", "세부 검사 항목", "VL2-00001" },
            new() { "", "", "", "", "검사일시", "x" },
            new() { "", "", "", "", "합격, 불합격 판정", "합" },
        };
        Assert.Equal(5, (int)Priv.Static("ReportColumnOf", t, "vl2-00001", 10)!);
    }

    [Fact]
    public void 판정행_없는_표는_성적서로_인정_안함()
    {
        var t = new List<List<string>>
        {
            new() { "검사명", "구분", "번호", "검사번호", "세부 검사 항목" },
            new() { "", "", "", "", "검사일시" },
            new() { "P", "X단계", "1", "#1", "FW" },   // 판정 행 직전에서 잘린 파일
        };
        Assert.False((bool)Priv.Static("IsReportTable", t)!);
    }
}

// 실제 파일을 쓰는 통합형 — Result 루트가 현재 작업 폴더 기준이라 병렬 실행을 막고 작업 폴더를 임시로 바꾼다
[CollectionDefinition("cwd", DisableParallelization = true)]
public class CwdCollection { }

[Collection("cwd")]
public class ReportFileTests : IDisposable
{
    readonly string _old = Environment.CurrentDirectory;
    readonly string _tmp = Priv.TempDir();
    readonly List<string> _log = new();
    public ReportFileTests() { Environment.CurrentDirectory = _tmp; }
    public void Dispose() { Environment.CurrentDirectory = _old; }

    const string PROJ = "VL10 모션검사 (RS-232)_TEST_4slot";
    string Root => Path.Combine(_tmp, "Result", PROJ.Replace(" ", ""));
    // bat-001(REQ-001): FAIL·PASS_DUPLICATE 보관 루트 — 품질모니터링 수집 경로(Result/) 밖
    string Archive => Path.Combine(_tmp, "Result_보관", PROJ.Replace(" ", ""));

    flexfab.MainForm Form(long n = 10)
    {
        var ws = Priv.Workspace(Priv.ProcSg("#1-X [X슬롯] FW", "X"), Priv.ProcSg("#9 [Y슬롯] UID", "Y"));
        var d = (IDictionary<string, object?>)ws;
        ((IDictionary<string, object?>)((List<object>)d["projects"]!)[0])["name"] = PROJ;
        d["report_serial_count"] = n;
        var f = Priv.Bare(); Priv.SetField(f, "workspace", ws); return f;
    }

    void Board(flexfab.MainForm f, string sn) => Priv.Call(f, "AppendReportForBoard", sn, (Action<string>)_log.Add);

    string[] ReportCsvs() => Directory.GetFiles(Path.Combine(Root, "성적서"), "*.csv").Select(Path.GetFileName).OrderBy(x => x).ToArray()!;

    [Fact]
    public void 성적서_완성2대_1파일_2열_xlsx_생성()
    {
        var f = Form();
        Board(f, "VL1-00012"); Board(f, "VL1-00013");
        var files = ReportCsvs();
        Assert.Single(files);
        var t = (List<List<string>>)Priv.Static("ReadCsvTableStrict", Path.Combine(Root, "성적서", files[0]))!;
        Assert.Equal(new[] { "VL1-00012", "VL1-00013" }, t[0].Skip(5));
        Assert.True(File.Exists(Path.ChangeExtension(Path.Combine(Root, "성적서", files[0]), ".xlsx")), string.Join("\n", _log));
    }

    [Fact]
    public void 성적서_N도달시_다음순번_재검은_기존열()
    {
        var f = Form(n: 2);
        Board(f, "A"); Board(f, "B"); Board(f, "A");   // A 재검 → 기존 열 갱신(새 열 아님)
        Assert.Single(ReportCsvs());
        Board(f, "C");                                // 가득 → _02
        var files = ReportCsvs();
        Assert.Equal(2, files.Length);
        Assert.EndsWith("_02.csv", files[1]);
    }

    [Fact]
    public void 성적서_항목변경시_기존파일_보존_새파일에_각주()
    {
        Board(Form(), "A");
        var ws2 = Priv.Workspace(Priv.ProcSg("#1-X [X슬롯] FW", "X"), Priv.ProcSg("#9 [Y슬롯] UID", "Y"), Priv.ProcSg("#11 CAL", "Y"));
        var d = (IDictionary<string, object?>)ws2;
        ((IDictionary<string, object?>)((List<object>)d["projects"]!)[0])["name"] = PROJ;
        var f2 = Priv.Bare(); Priv.SetField(f2, "workspace", ws2);
        Board(f2, "B");
        var files = ReportCsvs();
        Assert.Equal(2, files.Length);
        var t2 = (List<List<string>>)Priv.Static("ReadCsvTableStrict", Path.Combine(Root, "성적서", files[1]))!;
        Assert.Contains(t2, r => r.Count > 4 && r[4].StartsWith("※ 항목·기준 변경"));
        var t1 = (List<List<string>>)Priv.Static("ReadCsvTableStrict", Path.Combine(Root, "성적서", files[0]))!;
        Assert.Equal("A", t1[0][5]);   // 기존 파일 그대로
    }

    [Fact]
    public void FAIL_JSON_로컬저장_필드()
    {
        var f = Form();
        var ret = new JArray { new JObject { ["Name"] = "#1-X FW", ["result"] = "FAIL" } };
        string rel = (string)Priv.Call(f, "SaveFailJsonSlot", "VL1-00012", "X", "X1슬롯", "FAIL(FAIL #1-X)", ret, (Action<string>)_log.Add)!;
        Assert.StartsWith($"Result_보관/{PROJ.Replace(" ", "")}/{DateTime.Now:yyyy-MM-dd}/FAIL/VL1-00012_X_", rel);
        Assert.False(Directory.Exists(Path.Combine(Root, DateTime.Now.ToString("yyyy-MM-dd"), "FAIL")));   // Result/ 아래엔 없음
        var o = JObject.Parse(File.ReadAllText(Path.Combine(_tmp, rel)));
        Assert.Equal("FAIL", (string)o["pass_fail"]!);
        Assert.Equal("X", (string)o["stage"]!);
        Assert.Equal("X1슬롯", (string)o["slot"]!);
        Assert.Equal("FAIL(FAIL #1-X)", (string)o["fail_reason"]!);
        Assert.Single((JArray)o["result"]!);
    }

    [Fact]
    public void PASS_중복은_기존파일을_PASS_DUPLICATE로_이관()
    {
        var f = Form();
        string day = Path.Combine(Root, DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(day);
        string json = Path.Combine(day, "VL1-00012.json");
        File.WriteAllText(json, "{\"old\":1}");
        Priv.Call(f, "PreservePassDuplicate", json, (Action<string>)_log.Add);
        Assert.False(File.Exists(json));
        string dupDir = Path.Combine(Archive, DateTime.Now.ToString("yyyy-MM-dd"), "PASS_DUPLICATE");   // bat-001: Result/ 밖
        var moved = Directory.GetFiles(dupDir);
        Assert.Single(moved);
        Assert.Equal("{\"old\":1}", File.ReadAllText(moved[0]));
        Assert.False(Directory.Exists(Path.Combine(day, "PASS_DUPLICATE")));
        Priv.Call(f, "PreservePassDuplicate", json, (Action<string>)_log.Add);   // 없으면 아무 일 없음
        Assert.Single(Directory.GetFiles(dupDir));
    }

    [Fact]
    public void 이관후_저장실패면_원위치_복귀()
    {
        var f = Form();
        string day = Path.Combine(Root, DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(day);
        string json = Path.Combine(day, "VL1-00012.json");
        File.WriteAllText(json, "{\"old\":1}");
        string moved = (string)Priv.Call(f, "PreservePassDuplicate", json, (Action<string>)_log.Add)!;
        Assert.False(File.Exists(json));
        Priv.Call(f, "RestorePassDuplicate", moved, json, (Action<string>)_log.Add);
        Assert.Equal("{\"old\":1}", File.ReadAllText(json));   // {sn}.json 복귀 → 시리얼 중복검사 유지
        Assert.False(File.Exists(moved));
    }

    [Fact]
    public void report_items_불일치면_기록안함_로그남김()
    {
        var ws = Priv.Workspace(Priv.ProcSg("#1-X [X슬롯] FW", "X"));
        var d = (IDictionary<string, object?>)ws;
        ((IDictionary<string, object?>)((List<object>)d["projects"]!)[0])["name"] = PROJ;
        d["report_items"] = new List<object> { "#99 없는 항목" };
        var f = Priv.Bare(); Priv.SetField(f, "workspace", ws);
        Board(f, "A");
        Assert.False(Directory.Exists(Path.Combine(Root, "성적서")) && Directory.GetFiles(Path.Combine(Root, "성적서"), "*.csv").Length > 0);
        Assert.Contains(_log, m => m.Contains("일치하는 검사항목이 0개"));
    }

    [Fact]
    public void 성적서_CSV는_BOM으로_시작_tmp_잔존없음()
    {
        Board(Form(), "A");
        string csv = Directory.GetFiles(Path.Combine(Root, "성적서"), "*.csv").Single();
        var b = File.ReadAllBytes(csv);
        Assert.True(b.Length > 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF);
        Assert.Empty(Directory.GetFiles(Path.Combine(Root, "성적서"), "*.tmp"));
    }
}
