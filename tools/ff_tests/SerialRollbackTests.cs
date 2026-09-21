using System.Text;
using Xunit;

namespace ff_tests;

// D2: 시리얼 롤백 가드 (REVIEW_적대적검증결과_20260921 D2)
// serial 파일 마지막 줄 = 다음에 쓸 번호. 롤백은 "이번 사이클이 추가한 줄"만 지워야 한다.
public class SerialRollbackTests
{
    static readonly UTF8Encoding Enc = new(false);

    static string NewFile(params string[] lines)
    {
        string p = Path.Combine(Priv.TempDir(), "serial.txt");
        File.WriteAllLines(p, lines, Enc);
        return p;
    }

    static int Count(string path) => (int)Priv.Static("CountNonEmptyLines", path)!;

    static List<string> Log(out Action<string> log) { var l = new List<string>(); log = l.Add; return l; }

    static void Rollback(string path, Action<string> log, int baseline) =>
        Priv.Call(Priv.Bare(), "RemoveLastLineFromFile", path, log, baseline);

    static string[] NonEmpty(string path) => File.ReadAllLines(path, Enc).Where(l => l.Trim().Length > 0).ToArray();

    [Fact]
    public void CountNonEmptyLines_없는_파일은_0() =>
        Assert.Equal(0, Count(Path.Combine(Priv.TempDir(), "none.txt")));

    [Fact]
    public void CountNonEmptyLines_빈줄은_세지_않음() =>
        Assert.Equal(2, Count(NewFile("VL1-00012", "", "  ", "VL1-00013", "")));

    // ── 결함 재현 시나리오: 마감 사이클(X 빈칸 → append 없음)에서 FAIL/STOP 롤백 ──
    [Fact]
    public void 마감사이클_번호_미소모면_롤백하지_않음()
    {
        string p = NewFile("VL1-00012", "VL1-00013", "VL1-00014");   // 00014 = 다음에 쓸 번호
        int baseline = Count(p);                                      // 사이클 시작 기준선 = 3
        // 마감 사이클: X1·X2 빈칸이라 append 없음 → Y FAIL → 롤백
        var log = Log(out var a);
        Rollback(p, a, baseline);
        Assert.Equal(new[] { "VL1-00012", "VL1-00013", "VL1-00014" }, NonEmpty(p));   // D2 이전: 00014 삭제 → 00013 재사용
        Assert.Contains(log, m => m.Contains("번호를 소모하지 않았습니다"));
    }

    [Fact]
    public void 정상사이클_번호_소모했으면_1줄_롤백()
    {
        string p = NewFile("VL1-00012", "VL1-00013", "VL1-00014");
        int baseline = Count(p);
        File.AppendAllText(p, "VL1-00016" + Environment.NewLine, Enc);   // X1=00014, X2=00015 → 00016 append
        var log = Log(out var a);
        Rollback(p, a, baseline);
        Assert.Equal(new[] { "VL1-00012", "VL1-00013", "VL1-00014" }, NonEmpty(p));
        Assert.Contains(log, m => m.Contains("롤백 완료"));
    }

    [Fact]
    public void 같은_사이클에서_롤백이_두번_불려도_추가분_1줄만_제거()
    {
        // 롤백 호출 지점이 10곳이라 한 사이클에 중복 호출될 수 있다 — 두 번째는 기준선에 걸려 스킵돼야 함
        string p = NewFile("VL1-00012", "VL1-00013");
        int baseline = Count(p);
        File.AppendAllText(p, "VL1-00014" + Environment.NewLine, Enc);
        var log = Log(out var a);
        Rollback(p, a, baseline);
        Rollback(p, a, baseline);
        Assert.Equal(new[] { "VL1-00012", "VL1-00013" }, NonEmpty(p));
    }

    [Fact]
    public void 기준선_미설정_minus1은_기존동작_무조건_1줄_제거()
    {
        string p = NewFile("VL1-00012", "VL1-00013");
        var log = Log(out var a);
        Rollback(p, a, -1);
        Assert.Equal(new[] { "VL1-00012" }, NonEmpty(p));
    }

    [Fact]
    public void 끝의_빈줄은_건너뛰고_마지막_번호를_제거()
    {
        string p = NewFile("VL1-00012", "VL1-00013");
        int baseline = Count(p);
        File.AppendAllText(p, "VL1-00014" + Environment.NewLine + Environment.NewLine + "   " + Environment.NewLine, Enc);
        var log = Log(out var a);
        Rollback(p, a, baseline);
        Assert.Equal(new[] { "VL1-00012", "VL1-00013" }, NonEmpty(p));
    }

    [Fact]
    public void 파일이_없으면_스킵()
    {
        string p = Path.Combine(Priv.TempDir(), "none.txt");
        var log = Log(out var a);
        Rollback(p, a, 0);
        Assert.False(File.Exists(p));
        Assert.Contains(log, m => m.Contains("없습니다"));
    }
}
