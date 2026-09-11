using System.Dynamic;
using System.Reflection;
using Newtonsoft.Json;
using Cantops.FlexFab;
using System.Diagnostics;
using flexfab;
using System.Windows.Forms;

namespace Cantops.FlexFab.Mini
{
    class App
    {
        public void MyLogAction(string message)
        {
            Console.WriteLine(message); // Form 추가후 사용되지 않음
        }


        public dynamic LoadWorkspace(string workspace_filename, Action<string> logAction)
        {
            dynamic workspace = null;

            try
            {
                if (!File.Exists(workspace_filename))
                {
                    logAction($"Error: '{workspace_filename}' 파일이 존재하지 않습니다.");
                    return null;
                }

                string jsonString = File.ReadAllText(workspace_filename);
                workspace = JsonConvert.DeserializeObject<ExpandoObject>(jsonString);

                foreach (var iter in workspace.libraries)
                {
                    if (LogConfig.IsTest) logAction($"라이브러리 로드 시도: ID={iter.id}, 파일={iter.filename}, 클래스={iter.classname}");

                    try
                    {
                        var assembly = Assembly.LoadFrom(iter.filename);
                        var type = assembly.GetType(iter.classname);

                        if (type == null)
                        {
                            throw new Exception($"클래스 '{iter.classname}'를 찾을 수 없습니다.");
                        }

                        iter.classtype = type;
                        iter.instance = Activator.CreateInstance(type);
                        if (LogConfig.IsTest) logAction($"Library '{iter.id}' 인스턴스 생성 성공.");

                        if (iter.instance is IModule module)
                        {
                            var config = (IDictionary<string, object>)iter.config;
                            config["log_action"] = logAction; // log_action 추가
                            module.SetConfig(config);
                            //logAction($"Library '{iter.id}'에 설정 적용 완료.");
                        }
                    }
                    catch (Exception ex)
                    {
                        logAction($"Library '{iter.id}' 로드 중 오류 발생: {ex.Message}");
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



        public void DoProject(dynamic workspace, string project_id, Action<string> logAction, MainForm mainForm)
        {
            logAction($"DoProject() 실행 시작: project_id = {project_id}");

            if (workspace == null)
            {
                logAction("오류: workspace가 null입니다.");
                return;
            }

            var project = ((IEnumerable<dynamic>)workspace.projects).FirstOrDefault(p => p.id == project_id);
            if (project == null)
            {
                logAction($"오류: project_id '{project_id}'에 해당하는 프로젝트를 찾을 수 없습니다.");
                return;
            }

            logAction($"프로젝트 이름: {project.name}");
            logAction($"프로젝트에 등록된 라이브러리: {JsonConvert.SerializeObject(project.library_depencency)}");

            var libraries = new Dictionary<string, object>();

            // 프로젝트에 필요한 라이브러리 로드
            foreach (var lib_dep in project.library_depencency)
            {
                var library = ((IEnumerable<dynamic>)workspace.libraries)
                    .FirstOrDefault(lib => lib.id == lib_dep) as IDictionary<string, object>;

                if (library != null && library.ContainsKey("instance"))
                {
                    libraries[lib_dep] = library["instance"];
                    logAction($"라이브러리 추가 완료: {lib_dep}");
                }
                else
                {
                    logAction($"오류: 라이브러리 '{lib_dep}'를 찾을 수 없습니다.");
                }
            }

            // 현재 라이브러리 키 목록 확인
            logAction($"현재 libraries 키 목록: {string.Join(", ", libraries.Keys)}");

            // 장치 및 프로세스 초기화
            foreach (var iter in libraries)
            {
                if (iter.Value is IDevice device)
                {
                    device.Initialize(libraries);
                    logAction($"디바이스 초기화 완료: {iter.Key}");
                }

                if (iter.Value is IProcess process)
                {
                    if (process is Uart uartProcess)
                    {
                        uartProcess.SetTimeout(3000); // UART 타임아웃 3초로 설정
                    }

                    process.Begin();
                    logAction($"프로세스 시작: {iter.Key}");
                }
            }

            try
            {
                logAction($"검사 시작 - {project.name}");

                for (int i = 0; i < project.procs.Count; i++)
                {
                    var proc = project.procs[i];
                    var numberCell = mainForm.dataGridView1.Rows[i].Cells[0]; // 1열(번호)

                    // 배경색이 빨간색이면 "Skip" 처리
                    if (numberCell.Style.BackColor == Color.Red)
                    {
                        mainForm.UpdateDataGridView(i, "Skip", Color.LightBlue);
                        logAction($"테스트 {proc.name} (ID: {proc.id}) **스킵됨**");
                        continue;
                    }

                    logAction($"테스트 실행 중: {proc.name} (ID: {proc.id})");

                    // proc.libid를 기반으로 라이브러리 찾기
                    var libraryDict = ((IEnumerable<dynamic>)workspace.libraries)
                        .FirstOrDefault(lib => lib.id == proc.libid) as IDictionary<string, object>;

                    if (libraryDict == null)
                    {
                        logAction($"오류: 프로세스 '{proc.name}'의 libid '{proc.libid}'에 해당하는 라이브러리를 찾을 수 없습니다.");
                        continue;
                    }

                    proc.library = libraryDict;

                    // ExpandoObject 변환 시도
                    if (proc.library is ExpandoObject)
                    {
                        //logAction($"proc.library가 ExpandoObject입니다. 변환을 시도합니다.");
                        proc.library = (IDictionary<string, object>)proc.library;
                    }
                    else if (proc.library is IDictionary<string, object> libraryDictFinal)
                    {
                        logAction($"proc.library가 이미 IDictionary<string, object> 형식입니다.");
                    }
                    else
                    {
                        logAction($"오류: proc.library가 ExpandoObject도 아니고 IDictionary도 아닙니다. 타입 = {proc.library?.GetType().FullName ?? "null"}");
                        continue;
                    }

                    // 클래스 타입 확인 및 메서드 실행
                    if (proc.library is IDictionary<string, object> libraryDictFinal2 &&
                        libraryDictFinal2.ContainsKey("classtype") &&
                        libraryDictFinal2["classtype"] is Type classtype)
                    {
                        var method = classtype.GetMethod(proc.id);
                        if (method != null)
                        {
                            logAction($"실행할 메서드: {proc.id} (클래스: {classtype.FullName})");

                            try
                            {
                                // log_action을 PIM_DPM 함수로 전달
                                var res = method.Invoke(libraryDictFinal2["instance"], new object[] { libraries, proc.param, logAction });

                                bool isSuccess = res is IDictionary<string, object> resultDict &&
                                                 resultDict.ContainsKey("success") &&
                                                 Convert.ToBoolean(resultDict["success"]);

                                mainForm.UpdateDataGridView(i, isSuccess ? "OK" : "FAIL", isSuccess ? Color.Green : Color.Red);
                                logAction($"테스트 결과: {proc.name} -> {(isSuccess ? "OK" : "FAIL")}");

                                // 만약 실패하면 남은 검사를 중지하고 종료
                                if (!isSuccess)
                                {
                                    logAction($"테스트 {proc.name}에서 실패했습니다. 검사 종료.");
                                    return;  // 여기서 바로 중지
                                }
                            }
                            catch (Exception ex)
                            {
                                logAction($"실행 중 오류 발생: {ex.Message}");
                            }
                        }
                        else
                        {
                            logAction($"오류: 메서드 {proc.id}를 {classtype.FullName}에서 찾을 수 없습니다.");
                        }
                    }
                    else
                    {
                        logAction($"오류: 테스트 {proc.name}의 라이브러리 정보가 올바르지 않습니다.");
                    }
                }

                logAction("모든 테스트 실행 완료");
            }
            catch (Exception ex)
            {
                logAction($"실행 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }

            // 모든 프로세스 종료
            foreach (var iter in libraries)
            {
                if (iter.Value is IProcess process)
                {
                    process.End();
                    //logAction($"프로세스 종료: {iter.Key}");
                }
            }
        }


        private static string ReadIniValue(string path, string section, string key, string defaultValue)
        {
            try
            {
                if (!File.Exists(path)) return defaultValue;
                string curSec = "";
                foreach (var line in File.ReadAllLines(path))
                {
                    var t = line.Trim();
                    if (t.Length == 0 || t.StartsWith(";")) continue;
                    if (t.StartsWith("[") && t.EndsWith("]")) { curSec = t.Substring(1, t.Length - 2).Trim(); continue; }
                    if (!curSec.Equals(section, StringComparison.OrdinalIgnoreCase)) continue;
                    int eq = t.IndexOf('=');
                    if (eq <= 0) continue;
                    if (t.Substring(0, eq).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                        return t.Substring(eq + 1).Trim();
                }
            }
            catch { }
            return defaultValue;
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // workspace 로드는 FlexfabForm 내부에서 config.ini LastWorkspace 기반으로 처리
            MainForm mainForm = new MainForm(null);
            Application.Run(mainForm);
        }
    }
}