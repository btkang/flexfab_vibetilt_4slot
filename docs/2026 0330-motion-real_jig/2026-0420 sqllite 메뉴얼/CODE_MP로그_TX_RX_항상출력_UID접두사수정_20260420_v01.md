# MP 로그 TX/RX 항상 출력 + UID 접두사 수정

> 날짜: 2026-04-20
> 파일: `ff_vibetilt/VibeTilt.cs` (단일 파일, +16 / -15)

## 배경
- MP 모드(`log_level: MP_LOG`, `show_log: 0`) 실행 시 `[VibeTilt] TX/RX` 로그가 안 찍혀 FAIL 원인 파악이 어려움
- FW 1.0.4가 UID 명령 미구현 (RV=4 반환)인데, UID 실패 로그 접두사가 `[UID]`가 아닌 `[VER]`로 잘못 찍히는 버그 발견
  - 원인: `UID_232`/`UID_485`가 공용 `VER_Internal` 호출, 내부 로그 접두사 `[VER]` 하드코딩

## 변경 내용

### 1. TX/RX 로그 항상 출력 (2줄)
`SendCommand` / `ReadStep` 내부의 `if (LogConfig.IsTest)` 조건 제거:
```diff
- if (LogConfig.IsTest) logAction($"[VibeTilt] TX: {framed}");
+ logAction($"[VibeTilt] TX: {framed}");

- if (LogConfig.IsTest) logAction($"[VibeTilt] RX: {v}");
+ logAction($"[VibeTilt] RX: {v}");
```
→ TEST/MP 구분 없이 송수신 패킷이 항상 로그에 찍힘.

### 2. UID 접두사 버그 수정 (13줄)
`VER_Internal` 시그니처에 logTag 파라미터 추가:
```diff
  private IDictionary<string, object?> VER_Internal(
      IDictionary<string, object?> libraries,
      IDictionary<string, object?> param,
      Action<string> logAction,
-     string uartKey, string defaultProcId, string defaultProcName)
+     string uartKey, string defaultProcId, string defaultProcName,
+     string logTag = "VER")
```

내부 하드코딩 `[VER]` 10곳을 `[{logTag}]`로 변경:
```diff
- logAction($"[VER] RV 오류: {rv} ({GetRvDescription(rv)})");
+ logAction($"[{logTag}] RV 오류: {rv} ({GetRvDescription(rv)})");
```
(총 10개 로그 라인)

`UID_232`/`UID_485` 호출부에 `"UID"` 전달:
```diff
- return VER_Internal(..., "UID_232", "시리얼번호 설정 RS-232");
+ return VER_Internal(..., "UID_232", "시리얼번호 설정 RS-232", "UID");

- return VER_Internal(..., "UID_485", "시리얼번호 설정 RS-485");
+ return VER_Internal(..., "UID_485", "시리얼번호 설정 RS-485", "UID");
```
나머지 VER_Internal 호출자 (VER_232/485, COMM_232/485, FWVER_232/485, OFFSET_232/485, APPCFG_SAVE_232/485)는 default `"VER"` 유지 → 기존 로그 호환.

## 검증 결과

### MP 모드 실행 (2026-04-20 10:08)
```
[INFO] log_level: MP_LOG | show_log: false
[VibeTilt] TX: <VER>                            ← MP에 새로 보임
[VibeTilt] RX: [VER,0,F/W ver.1.0.4]
...
[VibeTilt] TX: <UID,VL1-00003>
[VibeTilt] RX: [UID,4,VL1-00003]
[UID] UID RV 오류: 4 (알 수 없는 명령어)        ← [VER]→[UID] 확인
```

### TEST 모드 실행 (2026-04-20 10:07)
- 기존과 동일하게 전체 verbose 로그 출력 (회귀 없음)
- UID 실패 시 접두사도 `[UID]`로 정상

## 동작 비교
| 로그 | 이전 MP | 현재 MP | TEST (전/후 동일) |
|------|:---:|:---:|:---:|
| `[VibeTilt] TX/RX` | ❌ 없음 | ✅ 있음 | ✅ |
| `[UID] RV 오류` | `[VER]`로 표기 | `[UID]`로 정상 | `[UID]` |
| `[COMM]` 블록 | 기존 | 기존 | 기존 |
| GAC/AN/finally 상세 | 기존 (없음) | 기존 (없음) | ✅ |

## 검토했으나 채택하지 않은 방안
- **StepLog 버퍼링 (FAIL 시에만 덤프)** — 복잡도 과함, side effect 여러 개 (로그 순서 왜곡, 예외 leak 등). 단순 TX/RX 항상 출력으로 결정.
- **MP workspace에 `show_log: 1`** — JSON 수정은 간단하지만 로그 전체가 verbose해짐. TX/RX만 원하는 경우 과도.

## 영향 범위
- `ff_vibetilt/VibeTilt.cs` 단일 파일 (+16 / -15)
- 공통 라이브러리 수정 없음 (`flexfab_interfaces.LogConfig` 등 그대로)
- workspace JSON 수정 없음
- UI/Form 수정 없음
- 결과 JSON / MongoDB 저장 로직 영향 없음

## 커밋
1. `[UID] 접두사 버그 수정: VER_Internal logTag 파라미터화`
2. `MP 모드에서 TX/RX 로그 항상 출력`

## 참고
- 이번 변경은 **FW UID 미구현 문제 자체는 해결하지 않음** — FW 1.0.4가 UID 명령을 지원하지 않는 상태는 FW 업데이트로 별도 해결 필요
- UID RV=4는 모든 명령 공통 "알 수 없는 명령어" 의미 (명령별 다르지 않음, 사용자 확인)
