using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using flexfab;

namespace ff_tests;

// 검사 대상(flexfab.MainForm)을 수정하지 않고 internal/private 멤버를 호출하는 리플렉션 헬퍼
internal static class Priv
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

    static MethodInfo M(string name) =>
        typeof(MainForm).GetMethod(name, All) ?? throw new MissingMethodException(nameof(MainForm), name);

    // 주의: 배열 인자 하나만 넘길 때는 (object)arr 로 감싼다 — params object[]로 펼쳐지는 것 방지
    public static object? Static(string name, params object?[] args) => M(name).Invoke(null, args);
    public static object? Call(MainForm f, string name, params object?[] args) => M(name).Invoke(f, args);

    public static T Field<T>(string name) =>
        (T)(typeof(MainForm).GetField(name, All) ?? throw new MissingFieldException(nameof(MainForm), name)).GetValue(null)!;

    // 생성자를 거치지 않은 MainForm — 폼 생성·설정 로드 없이 순수 로직만 호출할 때 사용.
    // 필드 초기화식도 실행되지 않는다. Component 종료자가 반쯤 빈 객체를 Dispose하지 않게 막는다.
    public static MainForm Bare()
    {
        var f = (MainForm)RuntimeHelpers.GetUninitializedObject(typeof(MainForm));
        GC.SuppressFinalize(f);
        return f;
    }

    public static void SetField(MainForm f, string name, object? v) =>
        (typeof(MainForm).GetField(name, All) ?? throw new MissingFieldException(nameof(MainForm), name)).SetValue(f, v);

    // 워크스페이스 proc — 실제 로드와 같은 ExpandoObject 모양(JsonConvert.DeserializeObject<ExpandoObject>)
    // param == null 이면 param 멤버 자체가 없는 proc
    public static ExpandoObject Proc(string name, IDictionary<string, object?>? param)
    {
        var p = new ExpandoObject();
        var pd = (IDictionary<string, object?>)p;
        pd["name"] = name;
        if (param != null)
        {
            var e = new ExpandoObject();
            var ed = (IDictionary<string, object?>)e;
            foreach (var kv in param) ed[kv.Key] = kv.Value;
            pd["param"] = e;
        }
        return p;
    }

    public static ExpandoObject ProcSg(string name, object? slotGroup) =>
        Proc(name, new Dictionary<string, object?> { ["slot_group"] = slotGroup });

    public static ExpandoObject Workspace(params ExpandoObject[] procs)
    {
        var prj = new ExpandoObject();
        ((IDictionary<string, object?>)prj)["procs"] = procs.Cast<object>().ToList();
        var ws = new ExpandoObject();
        ((IDictionary<string, object?>)ws)["projects"] = new List<object> { prj };
        return ws;
    }

    public static string TempDir()
    {
        string d = Path.Combine(Path.GetTempPath(), "ff_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }
}
