using Xunit;

namespace ff_tests;

// D3: slot_group 정규화·검증 (REVIEW_적대적검증결과_20260921 D3)
public class SlotGroupTests
{
    static readonly string INVALID = Priv.Field<string>("SLOT_GROUP_INVALID");

    static string G(System.Dynamic.ExpandoObject proc) => (string)Priv.Static("GetSlotGroup", proc)!;

    [Theory]
    [InlineData("common", "common")]
    [InlineData("X", "X")]
    [InlineData("Y", "Y")]
    [InlineData("all", "all")]
    public void 허용값은_그대로(string raw, string expected) => Assert.Equal(expected, G(Priv.ProcSg("#1", raw)));

    [Theory]
    [InlineData("x", "X")]
    [InlineData("y", "Y")]
    [InlineData(" Y ", "Y")]
    [InlineData("ALL", "all")]
    [InlineData("Common", "common")]
    public void 대소문자_공백은_정규화(string raw, string expected) => Assert.Equal(expected, G(Priv.ProcSg("#1", raw)));

    [Theory]
    [InlineData("YY")]
    [InlineData("")]
    [InlineData("z")]
    [InlineData("X1")]
    [InlineData("xy")]
    public void 허용값_외는_INVALID(string raw) => Assert.Equal(INVALID, G(Priv.ProcSg("#1", raw)));

    [Fact]
    public void slot_group_없고_skip_coupling_X면_X() =>
        Assert.Equal("X", G(Priv.Proc("#1", new Dictionary<string, object?> { ["skip_coupling"] = "X" })));

    [Fact]
    public void slot_group_없고_skip_coupling_소문자도_인식() =>
        Assert.Equal("Y", G(Priv.Proc("#1", new Dictionary<string, object?> { ["skip_coupling"] = "y" })));

    [Fact]
    public void slot_group_없고_skip_coupling_도_X_Y_아니면_common() =>
        Assert.Equal("common", G(Priv.Proc("#1", new Dictionary<string, object?> { ["skip_coupling"] = "Z" })));

    [Fact]
    public void slot_group_skip_coupling_둘다_없으면_common_기존동작() =>
        Assert.Equal("common", G(Priv.Proc("#1", new Dictionary<string, object?> { ["TIMEOUT"] = 5000 })));

    [Fact]
    public void slot_group_null이면_키없음과_같다() =>
        Assert.Equal("common", G(Priv.ProcSg("#1", null)));

    // 하위호환: D3 이전에는 param 멤버가 없는 proc도 common이었다(예외 → catch → common)
    [Fact]
    public void param_멤버가_없는_proc는_common_기존동작() =>
        Assert.Equal("common", G(Priv.Proc("#0", null)));

    // SlotsForRun은 허용값 외에 빈 배열(= common 경로)을 돌려준다 → RunProcSlot4 2차 방어선이 필요한 이유
    [Fact]
    public void SlotsForRun_INVALID는_빈배열_그래서_가드가_필요() =>
        Assert.Empty((int[])Priv.Static("SlotsForRun", INVALID)!);

    [Theory]
    [InlineData("X", new[] { 0, 1 })]
    [InlineData("Y", new[] { 2, 3 })]
    [InlineData("all", new[] { 0, 1 })]   // 단계1: all = X1·X2 (단계2에서 4슬롯으로 바뀌어야 함)
    [InlineData("common", new int[0])]
    public void SlotsForRun_단계1_규칙(string g, int[] expected) =>
        Assert.Equal(expected, (int[])Priv.Static("SlotsForRun", g)!);

    [Fact]
    public void ValidateSlotGroups_전부_정상이면_true()
    {
        var f = Priv.Bare();
        Priv.SetField(f, "workspace", Priv.Workspace(
            Priv.ProcSg("#0 통신", "common"), Priv.ProcSg("#1-X FW", "X"), Priv.ProcSg("#6 기울기", "all"), Priv.ProcSg("#9 UID", "y")));
        var args = new object?[] { null };
        Assert.True((bool)Priv.Call(f, "ValidateSlotGroups", args)!);
    }

    [Fact]
    public void ValidateSlotGroups_오타는_항목명과_값을_보고()
    {
        var f = Priv.Bare();
        Priv.SetField(f, "workspace", Priv.Workspace(
            Priv.ProcSg("#1-X FW", "X"), Priv.ProcSg("#9 UID", "YY"), Priv.ProcSg("#11 CAL", "Y")));
        var args = new object?[] { null };
        Assert.False((bool)Priv.Call(f, "ValidateSlotGroups", args)!);
        string msg = (string)args[0]!;
        Assert.Contains("#9 UID", msg);
        Assert.Contains("\"YY\"", msg);
        Assert.DoesNotContain("#11", msg);
    }

    [Fact]
    public void ValidateSlotGroups_param_없는_proc는_통과_기존동작()
    {
        var f = Priv.Bare();
        Priv.SetField(f, "workspace", Priv.Workspace(Priv.Proc("#작업자확인", null), Priv.ProcSg("#1", "X")));
        var args = new object?[] { null };
        Assert.True((bool)Priv.Call(f, "ValidateSlotGroups", args)!);
    }
}
