using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace flexfab
{
    // ──────────────────────────────────────────────────────────────
    //  포트 장치 스캐너 (read-only) — 2026-06-11 추가
    //  목적: 포트 변경 다이얼로그에서 "이 COM이 어떤 장치인지"(칩/시리얼/USB위치)를
    //        바로 보여줘 장치관리자 왕복 제거 + FTDI 시리얼 기준 자동채움 지원.
    //  방식: 레지스트리 전용 (Microsoft.Win32.Registry = WindowsDesktop 프레임워크 내장 → 패키지 추가 없음).
    //  주의: 통신/검사 로직과 무관한 보조 기능. 실패해도 빈 목록 반환(예외 던지지 않음).
    // ──────────────────────────────────────────────────────────────

    public enum PortChipKind { Ftdi, Prolific, Other }

    public sealed class PortDeviceInfo
    {
        public string Com { get; init; } = "";        // 예: "COM17"
        public PortChipKind Kind { get; init; }
        public string? Serial { get; init; }           // FTDI 고유 시리얼 (Prolific 등은 null)
        public string? Location { get; init; }         // USB 허브 포트 위치 (시리얼 없는 칩 구분용)

        // 다이얼로그 한 줄 표시용
        public string Describe()
        {
            return Kind switch
            {
                PortChipKind.Ftdi     => $"{Com}  FTDI  {Serial}",
                PortChipKind.Prolific => $"{Com}  Prolific  {Location ?? "-"}",
                _                     => $"{Com}  {Location ?? "USB"}",
            };
        }
    }

    public static class PortDeviceScanner
    {
        private const string EnumRoot = @"SYSTEM\CurrentControlSet\Enum";

        // 현재 꽂힌 COM 포트 전체 스캔. 실패 시 빈 목록.
        public static List<PortDeviceInfo> Scan()
        {
            var result = new List<PortDeviceInfo>();
            try
            {
                var present = GetPresentComPorts();           // SERIALCOMM 기준 실제 존재 포트
                CollectFtdi(result, present);                 // FTDIBUS 서브트리 (시리얼 보유)
                CollectUsb(result, present);                  // USB 서브트리 (Prolific 등, 위치 보유)
            }
            catch { /* 보조 기능 — 조용히 빈 목록 */ }

            // COM 번호 기준 중복 제거 + 숫자 정렬
            return result
                .GroupBy(p => p.Com, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => ParseComNumber(p.Com))
                .ToList();
        }

        // FTDI 시리얼로 현재 매핑된 COM 찾기 (자동채움용). 못 찾으면 null.
        public static string? FindComByFtdiSerial(string? serial, IEnumerable<PortDeviceInfo>? scanned = null)
        {
            if (string.IsNullOrWhiteSpace(serial)) return null;
            var list = scanned ?? Scan();
            return list.FirstOrDefault(p =>
                       p.Kind == PortChipKind.Ftdi &&
                       string.Equals(p.Serial, serial, StringComparison.OrdinalIgnoreCase))
                   ?.Com;
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────

        // HKLM\HARDWARE\DEVICEMAP\SERIALCOMM → 현재 OS에 실제 존재하는 COM 집합
        private static HashSet<string> GetPresentComPorts()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");
                if (key != null)
                {
                    foreach (var name in key.GetValueNames())
                    {
                        if (key.GetValue(name) is string com && com.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                            set.Add(com);
                    }
                }
            }
            catch { }
            return set;
        }

        // FTDIBUS 서브트리: VID_0403+PID_6001+<SERIAL>\<inst>\Device Parameters\PortName
        private static void CollectFtdi(List<PortDeviceInfo> result, HashSet<string> present)
        {
            try
            {
                using var ftdi = Registry.LocalMachine.OpenSubKey($@"{EnumRoot}\FTDIBUS");
                if (ftdi == null) return;
                foreach (var devName in ftdi.GetSubKeyNames())   // 예: VID_0403+PID_6001+A93OWJK3A
                {
                    string? serial = ParseFtdiSerial(devName);
                    using var devKey = ftdi.OpenSubKey(devName);
                    if (devKey == null) continue;
                    foreach (var inst in devKey.GetSubKeyNames())  // 예: 0000
                    {
                        string? com = ReadPortName(devKey, inst);
                        if (com == null) continue;
                        if (present.Count > 0 && !present.Contains(com)) continue;
                        result.Add(new PortDeviceInfo
                        {
                            Com = com,
                            Kind = PortChipKind.Ftdi,
                            Serial = serial,
                            Location = ReadLocation(devKey, inst),
                        });
                    }
                }
            }
            catch { }
        }

        // USB 서브트리: VID_xxxx&PID_xxxx\<inst>\Device Parameters\PortName (PortName 있는 것만 = COM 장치)
        private static void CollectUsb(List<PortDeviceInfo> result, HashSet<string> present)
        {
            try
            {
                using var usb = Registry.LocalMachine.OpenSubKey($@"{EnumRoot}\USB");
                if (usb == null) return;
                foreach (var devName in usb.GetSubKeyNames())     // 예: VID_067B&PID_23A3
                {
                    using var devKey = usb.OpenSubKey(devName);
                    if (devKey == null) continue;
                    var kind = devName.IndexOf("VID_067B", StringComparison.OrdinalIgnoreCase) >= 0
                        ? PortChipKind.Prolific
                        : PortChipKind.Other;
                    foreach (var inst in devKey.GetSubKeyNames())
                    {
                        string? com = ReadPortName(devKey, inst);
                        if (com == null) continue;               // COM 포트 아닌 USB 장치는 스킵
                        if (present.Count > 0 && !present.Contains(com)) continue;
                        result.Add(new PortDeviceInfo
                        {
                            Com = com,
                            Kind = kind,
                            Serial = null,                       // Prolific PL2303 등은 칩 고유 시리얼 없음
                            Location = ReadLocation(devKey, inst),
                        });
                    }
                }
            }
            catch { }
        }

        private static string? ReadPortName(RegistryKey devKey, string inst)
        {
            try
            {
                using var dp = devKey.OpenSubKey($@"{inst}\Device Parameters");
                return dp?.GetValue("PortName") as string;
            }
            catch { return null; }
        }

        private static string? ReadLocation(RegistryKey devKey, string inst)
        {
            try
            {
                using var ik = devKey.OpenSubKey(inst);
                return ik?.GetValue("LocationInformation") as string;
            }
            catch { return null; }
        }

        // "VID_0403+PID_6001+A93OWJK3A" → "A93OWJK3A"
        private static string? ParseFtdiSerial(string devName)
        {
            int plus = devName.LastIndexOf('+');
            return (plus >= 0 && plus + 1 < devName.Length) ? devName.Substring(plus + 1) : null;
        }

        private static int ParseComNumber(string com)
        {
            return int.TryParse(new string(com.Where(char.IsDigit).ToArray()), out var n) ? n : int.MaxValue;
        }
    }
}
