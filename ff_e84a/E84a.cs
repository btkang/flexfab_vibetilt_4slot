using Cantops.FlexFab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cantops.FlexFab
{
    public class E84a : ModuleBase, IProcess
    {
        public const string MODULE_VERSION = "1.0";
        private IComm? _uart;

        public E84a()
        {
            info_ = new Dictionary<string, object?>
            {
                { "name", "e84a" },
                { "desc", "E84A ship test module" },
                { "ver",  MODULE_VERSION },
                { "procs", new List<Dictionary<string, object?>> {
                    new Dictionary<string, object?> { { "id", "ShipConfigClearTest" }, { "desc", "Ship Config Clear Test" } },
                    new Dictionary<string, object?> { { "id", "ShipConfigCheckTest" }, { "desc", "Ship Config Check Test" } },
                    new Dictionary<string, object?> { { "id", "ShipLogClearTest" }, { "desc", "Ship Log Clear Test" } },
                }}
            };
        }

        public void Begin()
        {
        }

        public void End()
        {
        }

        private IComm? ResolveUart(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            string? uartId = null;

            if (config_ != null &&
                config_.TryGetValue("uart", out var cfgUart) &&
                cfgUart is string cfgUartId &&
                !string.IsNullOrWhiteSpace(cfgUartId))
            {
                uartId = cfgUartId;
            }

            if (string.IsNullOrWhiteSpace(uartId) &&
                param.TryGetValue("uart", out var prmUart) &&
                prmUart != null &&
                !string.IsNullOrWhiteSpace(prmUart.ToString()))
            {
                uartId = prmUart.ToString();
            }

            if (string.IsNullOrWhiteSpace(uartId))
            {
                logAction("[E84A] FAIL : uart id not set");
                return null;
            }

            if (!libraries.TryGetValue(uartId, out var libObj) || libObj is not IComm comm)
            {
                logAction($"[E84A] FAIL : uart '{uartId}' not found or not IComm");
                return null;
            }

            _uart = comm;
            return comm;
        }

        private static string ExtractValue(IDictionary<string, object?>? resp)
        {
            if (resp == null) return "";
            if (resp.TryGetValue("value", out var v) && v != null)
                return v.ToString() ?? "";
            return "";
        }

        private static string GetLineEnding(IDictionary<string, object?> param)
        {
            if (param.TryGetValue("line_ending", out var le) && le != null)
            {
                var s = le.ToString()!.ToLowerInvariant();
                if (s == "crlf") return "\r\n";
                if (s == "cr") return "\r";
                if (s == "lf") return "\n";
                if (s == "none") return "";
            }
            return "\n";
        }

        private static object? GetParamValue(IDictionary<string, object?> param, string key)
        {
            return param.TryGetValue(key, out var v) ? v : null;
        }

        private static object? GetTestValue(object? test, string key)
        {
            if (test is IDictionary<string, object?> d1 && d1.TryGetValue(key, out var v1))
                return v1;
            if (test is IDictionary<string, object> d2 && d2.TryGetValue(key, out var v2))
                return v2;
            return null;
        }

        private static string GetTestString(object? test, string key)
        {
            var v = GetTestValue(test, key);
            return v?.ToString() ?? "";
        }

        private static int GetTestInt(object? test, string key, int defaultValue = 0)
        {
            var v = GetTestValue(test, key);
            if (v == null) return defaultValue;
            if (int.TryParse(v.ToString(), out var n)) return n;
            return defaultValue;
        }

        private static IEnumerable<object?> GetStepList(IDictionary<string, object?> param)
        {
            if (param.TryGetValue("steps", out var stepsObj) && stepsObj is System.Collections.IEnumerable stepsEnum)
                return stepsEnum.Cast<object?>();
            if (param.TryGetValue("tests", out var testsObj) && testsObj is System.Collections.IEnumerable testsEnum)
                return testsEnum.Cast<object?>();
            return Array.Empty<object?>();
        }

        private bool SendCommand(
            IComm comm,
            string command,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            string lineEnding = GetLineEnding(param);
            string body = command + lineEnding;

            try
            {
                comm.Send(new Dictionary<string, object?>
                {
                    { "body", body },
                    { "no_newline", true }
                });
                return true;
            }
            catch (Exception ex)
            {
                logAction($"[E84A] FAIL : send '{command}' error: {ex.Message}");
                return false;
            }
        }

        private void ReadStep(
            IComm comm,
            int readTimeoutMs,
            Action<string> logAction,
            out string stepCollected,
            out string stepLast)
        {
            stepCollected = "";
            stepLast = "";
            var end = DateTime.UtcNow.AddMilliseconds(readTimeoutMs);
            int emptyCount = 0;
            bool gotBody = false;

            while (DateTime.UtcNow < end)
            {
                var resp = comm.Recv(new Dictionary<string, object?>
                {
                    { "timeout", 500 },
                    { "recv_mode", "line" }
                });
                var v = ExtractValue(resp);
                if (!string.IsNullOrWhiteSpace(v))
                {
                    stepLast = v;
                    stepCollected += v + "\n";
                    logAction(v);
                    emptyCount = 0;
                    if (!gotBody && !v.Contains(">"))
                        gotBody = true;
                    if (v.Contains(">") && gotBody)
                        break;
                }
                else
                {
                    emptyCount++;
                    if (emptyCount >= 3) break;
                }
            }
        }

        private void ClearCommBuffer(IComm comm, Action<string> logAction, int maxAttempts = 5, int timeout = 100)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    var resp = comm.Recv(new Dictionary<string, object?>
                    {
                        { "timeout", timeout },
                        { "recv_mode", "line" }
                    });
                    var v = ExtractValue(resp);
                    if (string.IsNullOrWhiteSpace(v)) break;
                }
                catch
                {
                    break;
                }
            }
        }

        public IDictionary<string, object?> ShipConfigClearTest(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            var uart = ResolveUart(libraries, param, logAction);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            bool ok = true;
            var steps = new List<Dictionary<string, object?>>();

            string procId =
                (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? "ShipConfigClearTest";
            string procName =
                (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? "Ship Config Clear Test";

            int bootWaitMs = 2000;
            var waitObj = GetParamValue(param, "BOOT_WAIT");
            if (waitObj != null && int.TryParse(waitObj.ToString(), out var w) && w > 0)
                bootWaitMs = w;

            int readTimeoutMs = 5000;
            var tmoObj = GetParamValue(param, "TIMEOUT");
            if (tmoObj != null && int.TryParse(tmoObj.ToString(), out var tmo) && tmo > 0)
                readTimeoutMs = tmo;

            ClearCommBuffer(uart, logAction, 5, 120);

            var stepList = GetStepList(param).ToList();
            if (stepList.Count == 0)
            {
                logAction("[ConfigClear] steps/tests 배열이 비어있거나 존재하지 않습니다.");
                ok = false;
            }
            else
            {
                foreach (var t in stepList)
                {
                    int sleepMs = GetTestInt(t, "sleep", 0);
                    if (sleepMs <= 0)
                        sleepMs = GetTestInt(t, "delay", 0);
                    if (sleepMs > 0)
                    {
                        logAction($"Waiting {sleepMs} ms...");
                        System.Threading.Thread.Sleep(sleepMs);
                        continue;
                    }

                    string command = GetTestString(t, "command");
                    if (string.IsNullOrWhiteSpace(command))
                        command = GetTestString(t, "cmd");
                    string expected = GetTestString(t, "expected_response");
                    if (string.IsNullOrWhiteSpace(expected))
                        expected = GetTestString(t, "expect");

                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    logAction($"명령어 전송: {command}");
                    if (!SendCommand(uart, command, param, logAction))
                    {
                        ok = false;
                        steps.Add(new Dictionary<string, object?>
                        {
                            { "command", command },
                            { "expected", string.IsNullOrWhiteSpace(expected) ? null : expected },
                            { "value", null },
                            { "result", "NG" }
                        });
                        continue;
                    }

                    ReadStep(uart, readTimeoutMs, logAction, out var stepCollected, out var stepLast);

                    bool stepOk = true;
                    if (!string.IsNullOrWhiteSpace(expected) && !stepCollected.Contains(expected))
                    {
                        logAction($"[ConfigClear] 누락 항목: {expected}");
                        ok = false;
                        stepOk = false;
                    }

                    steps.Add(new Dictionary<string, object?>
                    {
                        { "command", command },
                        { "expected", string.IsNullOrWhiteSpace(expected) ? null : expected },
                        { "value", string.IsNullOrWhiteSpace(stepLast) ? null : stepLast },
                        { "result", stepOk ? "OK" : "NG" }
                    });
                }

                if (bootWaitMs > 0)
                {
                    logAction($"Waiting {bootWaitMs} ms for reboot...");
                    System.Threading.Thread.Sleep(bootWaitMs);
                }
            }

            var retObj = new Dictionary<string, object?>
            {
                { "Name", procName },
                { "id", procId },
                { "steps", steps },
                { "result", ok ? "OK" : "NG" }
            };

            return new Dictionary<string, object?>
            {
                { "success", ok },
                { "retmsg", JsonSerializer.Serialize(retObj) }
            };
        }

        public IDictionary<string, object?> ShipConfigCheckTest(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            var uart = ResolveUart(libraries, param, logAction);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            bool ok = true;
            var checks = new List<Dictionary<string, object?>>();

            string procId =
                (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? "ShipConfigCheckTest";
            string procName =
                (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? "Ship Config Check Test";

            int readTimeoutMs = 6000;
            var tmoObj = GetParamValue(param, "TIMEOUT");
            if (tmoObj != null && int.TryParse(tmoObj.ToString(), out var tmo) && tmo > 0)
                readTimeoutMs = tmo;

            string collected = "";

            var stepList = GetStepList(param).ToList();
            if (stepList.Count == 0)
            {
                logAction("[ConfigCheck] steps/tests 배열이 비어있거나 존재하지 않습니다.");
                ok = false;
            }
            else
            {
                foreach (var t in stepList)
                {
                    int sleepMs = GetTestInt(t, "sleep", 0);
                    if (sleepMs <= 0)
                        sleepMs = GetTestInt(t, "delay", 0);
                    if (sleepMs > 0)
                    {
                        logAction($"Waiting {sleepMs} ms...");
                        System.Threading.Thread.Sleep(sleepMs);
                        continue;
                    }

                    string command = GetTestString(t, "command");
                    if (string.IsNullOrWhiteSpace(command))
                        command = GetTestString(t, "cmd");
                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    logAction($"명령어 전송: {command}");
                    if (!SendCommand(uart, command, param, logAction))
                    {
                        ok = false;
                        continue;
                    }

                    ReadStep(uart, readTimeoutMs, logAction, out var stepCollected, out _);
                    if (!string.IsNullOrWhiteSpace(stepCollected))
                        collected += stepCollected;
                }

                foreach (var t in stepList)
                {
                    string expected = GetTestString(t, "expected_response");
                    if (string.IsNullOrWhiteSpace(expected))
                        expected = GetTestString(t, "expect");
                    if (string.IsNullOrWhiteSpace(expected))
                        continue;

                    string foundLine = "";
                    if (!string.IsNullOrWhiteSpace(collected))
                    {
                        var lines = collected.Split('\n');
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && line.Contains(expected))
                            {
                                foundLine = line.Trim();
                                break;
                            }
                        }
                    }

                    bool hit = !string.IsNullOrWhiteSpace(foundLine);
                    if (!hit)
                    {
                        logAction($"[ConfigCheck] 누락 항목: {expected}");
                        ok = false;
                    }

                    checks.Add(new Dictionary<string, object?>
                    {
                        { "expected", expected },
                        { "value", hit ? foundLine : null },
                        { "result", hit ? "OK" : "NG" }
                    });
                }
            }

            var retObj = new Dictionary<string, object?>
            {
                { "Name", procName },
                { "id", procId },
                { "checks", checks },
                { "result", ok ? "OK" : "NG" }
            };

            return new Dictionary<string, object?>
            {
                { "success", ok },
                { "retmsg", JsonSerializer.Serialize(retObj) }
            };
        }

        public IDictionary<string, object?> ShipLogClearTest(
            IDictionary<string, object?> libraries,
            IDictionary<string, object?> param,
            Action<string> logAction)
        {
            var uart = ResolveUart(libraries, param, logAction);
            if (uart == null)
                return new Dictionary<string, object?> { { "success", false }, { "error", "uart not resolved" } };

            bool ok = true;
            var steps = new List<Dictionary<string, object?>>();

            string procId =
                (GetParamValue(param, "__proc_id") ?? GetParamValue(param, "proc_id"))?.ToString() ?? "ShipLogClearTest";
            string procName =
                (GetParamValue(param, "__proc_name") ?? GetParamValue(param, "proc_name"))?.ToString() ?? "Ship Log Clear Test";

            int readTimeoutMs = 6000;
            var tmoObj = GetParamValue(param, "TIMEOUT");
            if (tmoObj != null && int.TryParse(tmoObj.ToString(), out var tmo) && tmo > 0)
                readTimeoutMs = tmo;

            var stepList = GetStepList(param).ToList();
            if (stepList.Count == 0)
            {
                logAction("[LogClear] steps/tests 배열이 비어있거나 존재하지 않습니다.");
                ok = false;
            }
            else
            {
                foreach (var t in stepList)
                {
                    int sleepMs = GetTestInt(t, "sleep", 0);
                    if (sleepMs <= 0)
                        sleepMs = GetTestInt(t, "delay", 0);
                    if (sleepMs > 0)
                    {
                        logAction($"Waiting {sleepMs} ms...");
                        System.Threading.Thread.Sleep(sleepMs);
                        continue;
                    }

                    string command = GetTestString(t, "command");
                    if (string.IsNullOrWhiteSpace(command))
                        command = GetTestString(t, "cmd");
                    string expected = GetTestString(t, "expected_response");
                    if (string.IsNullOrWhiteSpace(expected))
                        expected = GetTestString(t, "expect");

                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    logAction($"명령어 전송: {command}");
                    if (!SendCommand(uart, command, param, logAction))
                    {
                        ok = false;
                        steps.Add(new Dictionary<string, object?>
                        {
                            { "command", command },
                            { "expected", string.IsNullOrWhiteSpace(expected) ? null : expected },
                            { "value", null },
                            { "result", "NG" }
                        });
                        continue;
                    }

                    ReadStep(uart, readTimeoutMs, logAction, out var stepCollected, out var stepLast);

                    bool stepOk = true;
                    if (!string.IsNullOrWhiteSpace(expected) && !stepCollected.Contains(expected))
                    {
                        logAction($"[LogClear] 누락 항목: {expected}");
                        ok = false;
                        stepOk = false;
                    }

                    steps.Add(new Dictionary<string, object?>
                    {
                        { "command", command },
                        { "expected", string.IsNullOrWhiteSpace(expected) ? null : expected },
                        { "value", string.IsNullOrWhiteSpace(stepLast) ? null : stepLast },
                        { "result", stepOk ? "OK" : "NG" }
                    });
                }
            }

            var retObj = new Dictionary<string, object?>
            {
                { "Name", procName },
                { "id", procId },
                { "steps", steps },
                { "result", ok ? "OK" : "NG" }
            };

            return new Dictionary<string, object?>
            {
                { "success", ok },
                { "retmsg", JsonSerializer.Serialize(retObj) }
            };
        }

    }
}
