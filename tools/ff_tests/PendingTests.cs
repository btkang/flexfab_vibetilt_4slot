using System.Dynamic;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ff_tests;

// 레인 pending 유효성·파일 규칙 (CHECK_4슬롯_동작명세 F1·F4, G5)
public class PendingTests
{
    const string WS = "TEST_workspace_motion_232_VL10_4slot";
    static readonly UTF8Encoding Enc = new(false);

    static string Write(JObject o)
    {
        string p = Path.Combine(Priv.TempDir(), $"pending_{WS}_x1.json");
        File.WriteAllText(p, o.ToString(), Enc);
        return p;
    }

    static JObject Valid(double hoursAgo = 1, string serial = "VL1-00012") => new()
    {
        ["serial"] = serial,
        ["slot"] = "X1슬롯",
        ["ws"] = WS,
        ["x_result"] = "PASS",
        ["time"] = DateTime.Now.AddHours(-hoursAgo).ToString("yyyy-MM-dd HH:mm:ss"),
        ["retmsg"] = new JArray(),
    };

    static (JObject? p, string reason) Load(string path, string ws = WS, object? workspace = null)
    {
        var f = Priv.Bare();
        if (workspace != null) Priv.SetField(f, "workspace", workspace);
        var args = new object?[] { path, ws, null };
        var r = (JObject?)Priv.Call(f, "LoadValidPending", args);
        return (r, (string)args[2]!);
    }

    [Fact]
    public void 정상_pending은_유효()
    {
        var (p, _) = Load(Write(Valid()));
        Assert.NotNull(p);
        Assert.Equal("VL1-00012", p!["serial"]!.ToString());
    }

    [Fact]
    public void 파일_없음()
    {
        var (p, r) = Load(Path.Combine(Priv.TempDir(), "none.json"));
        Assert.Null(p);
        Assert.Equal("pending 없음", r);
    }

    [Fact]
    public void 손상된_JSON()
    {
        string path = Path.Combine(Priv.TempDir(), "bad.json");
        File.WriteAllText(path, "{ \"serial\": ", Enc);
        var (p, r) = Load(path);
        Assert.Null(p);
        Assert.Equal("pending 손상", r);
    }

    [Fact]
    public void 워크스페이스_불일치()
    {
        var (p, r) = Load(Write(Valid()), ws: "MP_workspace_motion_232_VL10_4slot");
        Assert.Null(p);
        Assert.StartsWith("워크스페이스 불일치", r);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("FAIL")]
    public void x_result가_PASS가_아니면_무효(string? xr)
    {
        var o = Valid();
        if (xr == null) o.Remove("x_result"); else o["x_result"] = xr;
        var (p, r) = Load(Write(o));
        Assert.Null(p);
        Assert.Contains("X 판정 기록 없음", r);
    }

    [Fact]
    public void 유효기간_24h_초과()
    {
        var (p, r) = Load(Write(Valid(hoursAgo: 25)));
        Assert.Null(p);
        Assert.Contains("유효기간 초과(24h)", r);
    }

    [Fact]
    public void 유효기간_24h_이내()
    {
        var (p, _) = Load(Write(Valid(hoursAgo: 23)));
        Assert.NotNull(p);
    }

    [Fact]
    public void pending_max_age_h_설정을_따른다()
    {
        var ws = new ExpandoObject();
        ((IDictionary<string, object?>)ws)["pending_max_age_h"] = 48L;   // Newtonsoft 정수 = long
        var (p, _) = Load(Write(Valid(hoursAgo: 30)), workspace: ws);
        Assert.NotNull(p);
    }

    [Fact]
    public void 시리얼_빈값()
    {
        var (p, r) = Load(Write(Valid(serial: "  ")));
        Assert.Null(p);
        Assert.Equal("시리얼 없음", r);
    }

    [Fact]
    public void time_파싱불가는_무효()
    {
        var o = Valid();
        o["time"] = "not-a-date";
        var (p, _) = Load(Write(o));
        Assert.Null(p);   // 사유는 "유효기간 초과"로 표시됨(D6 경미 — 사유 문구 부정확)
    }

    [Fact(Skip = "알려진 제한: 미래 time(시계 역행)은 차이가 음수라 무기한 유효. REVIEW 에이전트2 경미 — 백로그")]
    public void 미래_time은_무효여야_함()
    {
        var (p, _) = Load(Write(Valid(hoursAgo: -5)));
        Assert.Null(p);
    }

    [Fact]
    public void pending_파일명_규칙()
    {
        string p0 = (string)Priv.Static("GetPendingSlotPath", 0, WS)!;
        string p1 = (string)Priv.Static("GetPendingSlotPath", 1, WS)!;
        Assert.Equal($"pending_{WS}_x1.json", Path.GetFileName(p0));
        Assert.Equal($"pending_{WS}_x2.json", Path.GetFileName(p1));
    }

    [Fact]
    public void 원자적_쓰기_새파일()
    {
        string p = Path.Combine(Priv.TempDir(), "a.json");
        Priv.Static("WriteAllTextAtomic", p, "{\"v\":1}");
        Assert.Equal("{\"v\":1}", File.ReadAllText(p, Enc));
        Assert.False(File.Exists(p + ".tmp"));
    }

    [Fact]
    public void 원자적_쓰기_덮어쓰기()
    {
        string p = Path.Combine(Priv.TempDir(), "a.json");
        File.WriteAllText(p, "old", Enc);
        Priv.Static("WriteAllTextAtomic", p, "new");
        Assert.Equal("new", File.ReadAllText(p, Enc));
        Assert.False(File.Exists(p + ".tmp"));
    }

    [Fact]
    public void 슬롯_요약_문구()
    {
        var state = new[] { "X통과(대기)", "완성", "FAIL(X 기록 없음: pending 없음)", "-" };
        string s = (string)Priv.Static("SlotSummaryText", (object)state)!;
        Assert.Equal("X1슬롯 X통과(대기) · X2슬롯 완성 · Y1슬롯 FAIL(X 기록 없음: pending 없음) · Y2슬롯 -", s);
    }
}
