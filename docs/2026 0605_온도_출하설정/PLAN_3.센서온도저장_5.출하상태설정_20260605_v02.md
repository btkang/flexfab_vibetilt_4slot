# PLAN_ 센서온도 저장(REFTEMP) + 출하상태 설정(RCONF) 기능추가 — v02

- 날짜: 2026-06-05 작성 / 2026-06-08 v02 갱신
- 근거(Truth Source): `docs/20260602_양산용 지그 제작 요청서_진동기울기센서 - 3.검사 항목 양식(기능검사).xlsx`
- 원 요청: `NOTE_3.센서온도저장_5.출하상태설정_기능추가_20260605.md`
- 대상 버전: **0.7.3** (현재 0.7.2.60605의 다음)
- 상태: **계획 확정 (v02 결정 반영 완료). 구현 착수 가능**

> **v01 → v02 변경점**
> 1. 검사항목 흐름을 실제 JSON(17개)로 재확인 — v01 다이어그램이 #12에서 잘려있던 것 정정
> 2. **REFTEMP 범위 가드 추가** (20~40 밖이면 저장 안 함, FAIL) ← 신규 결정
> 3. REFTEMP echo 판정 = **B안(값 일치)** 확정 (온도 2자리 고정 실측 확인)
> 4. **RCONF 위치 = A안(엑셀 순서, OFFSET 직후/SAVE 직전)** 확정 + 사유(펌웨어 저장 의존성)
> 5. 코드/버전 위치 실측 반영 (VibeTilt.cs:13, MotionJig.cs:14, csproj:7)

---

## 0. 핵심 결정 사항 (v02 확정)

| 항목 | 결정 | 사유 |
|------|------|------|
| 삽입 phase | **Y결합에만** (X결합 안 건드림) | X→Y 순서라 Y가 최종 phase. 저장/설정류는 최종 상태에서 1회 |
| REFTEMP 위치 | Y결합, **UID 직후** | 온도는 축 무관 1회 충분. 엑셀 #3(Cal 직전) 상대순서 보존 |
| **REFTEMP 범위 가드** | **20~40 밖이면 FAIL + 저장 안 함** | 저장값은 펌웨어 온도보정 기준값 → 비정상값 박으면 보정 baseline 오염 |
| REFTEMP echo 판정 | **B안: RV0 + 응답온도==보낸온도 (문자열)** | 온도 2자리 고정(실측 30.10/32.08) → 문자열 일치 안전 |
| RCONF 위치 | Y결합, **OFFSET 직후 / SAVE 직전 (A안)** | ①RCONF→SAVE 펌웨어 저장 의존성 가능 → 뒤에 SAVE 보장 ②엑셀 스펙 순서 |
| 버전 | 0.7.3 (csproj + VibeTilt + MotionJig 통일) | — |
| FW 버전 불일치 | **이번 작업 제외** | NOTE에 요청 없음. 별건 (6번) |

> **왜 REFTEMP는 가드(저장 차단)인데 RCONF는 A안(측정 전 저장)인가 — 모순 아님**
> 막는 대상이 다름:
> - REFTEMP 가드 = **틀린 값**(범위 밖 온도)이 보정 baseline에 박히는 것 차단 (값 정합성, 심각도 높음)
> - RCONF 위치 = 출하 **플래그** 시점 문제. RCONF 입력(UID·온도·cal)은 A 위치에서 이미 전부 PASS 검증됨 → 내용물은 정상. 플래그가 측정 전 켜져도 불량은 작업자가 거름 (심각도 낮음)
> - 게다가 RCONF는 SAVE가 뒤따라야 영구저장될 수 있어(엑셀 #5→#6) **맨 끝(B)으로 옮기면 저장 안 될 위험** → A가 안전

---

## 1. 실제 검사항목 흐름 (모션 232/485, 17개 — 검증 완료)

8개 파일 전부 동일 구조 확인 (Debug==Release, 232↔suffix만 다르고 485와 순서 동일):

```
idx  X결합 phase                      idx  Y결합 phase
 0   COMM      (통신)                  8   FWVER     (FW Y)
 1   FWVER     (FW X)                  9   UID       (시리얼 Y)
 2   UID       (시리얼 X)             10   MOTION_ZERO (Y영점)
 3   MOTION_ZERO (X영점)             11   OFFSET    (Y-Cal)
 4   OFFSET    (X-Cal)               12   APPCFG_SAVE (Y-SAVE)
 5   APPCFG_SAVE (X-SAVE)            13   MOTION_VIBE_Z (Z진동 0도)
 6   MOTION_TILT_X (X기울기)         14   MOTION_TILT_Y (Y기울기)
 7   MOTION_VIBE_Y (Y진동 +90)       15   MOTION_VIBE_X (X진동 -90)
                                     16   APPCFG    (Config 확인)
```

`fail_continue = 0` (중간 FAIL 시 즉시 중단) — 앞 단계 실패 시 이후 저장 실행 안 됨.

---

## 2. 삽입 후 Y결합 phase 최종 순서 (A안)

```
#8  FWVER
#9  UID
🆕  REFTEMP          ← UID 직후 (온도저장, 범위가드 포함)
#10 MOTION_ZERO
#11 OFFSET (Y-Cal)
🆕  RCONF            ← Y-Cal 직후 / SAVE 직전 (출하설정)
#12 APPCFG_SAVE (Y-SAVE)
#13 MOTION_VIBE_Z
#14 MOTION_TILT_Y
#15 MOTION_VIBE_X
#16 APPCFG
```

- 엑셀 상대순서 `UID → 온도저장 → Cal → 출하설정 → SAVE` 를 Y phase 안에서 보존
- **X결합 phase 변경 없음**
- cal 캡처: RCONF는 OFFSET(#11) 직후라 양축(X #4 / Y #11) cal 모두 확정된 상태 저장

---

## 3. 신규 기능 A — 센서 온도 저장 (REFTEMP)

### 명령 / 응답
| 단계 | 232 | 485 |
|------|-----|-----|
| ① 읽기 | `<SENTEMP>` → `[SENTEMP,0,XX.XX]` (temp idx 2) | `<SENTEMP,1>` → `[SENTEMP,0,1,XX.XX]` (temp idx 3) |
| ② 저장 | `<REFTEMP,XX.XX>` → `[REFTEMP,0,XX.XX]` | `<REFTEMP,1,XX.XX>` → `[REFTEMP,0,1,XX.XX]` |

- 온도 형식: **소수 2자리 고정** (`30.10`, `32.08` — 485/232 실측 검증, trailing 0 유지)

### 동작 (PASS 조건)
```
① SENTEMP 송신 → 응답에서 온도 문자열 XX.XX 추출 (대괄호 + RV0 + 파싱성공)
② 범위 체크 (min=20, max=40)
     ├ 범위 밖 → FAIL, REFTEMP 송신 안 함  ★비정상값 저장 차단
     └ 범위 안 → ③
③ REFTEMP,<①에서 읽은 온도문자열> 송신 → 응답 RV0 AND echo온도 == 보낸온도
   → 모두 만족 PASS
```
> **PASS = ① 읽기 성공 AND ② 20~40 이내 AND ③ 저장 RV0 + echo 일치** (하나라도 실패 시 FAIL)
> 온도값은 double 파싱 없이 **읽은 문자열 그대로** REFTEMP에 전달/비교 (포맷 변형 방지).

### 코드 (ff_vibetilt/VibeTilt.cs)
- thin wrapper 신규 (UID/SENTEMP 패턴):
  ```csharp
  public IDictionary<string,object?> REFTEMP_232(...) =>
      REFTEMP_Internal(libraries, param, logAction, "uart_232", "REFTEMP_232", "센서온도 저장 RS-232");
  public IDictionary<string,object?> REFTEMP_485(...) =>
      REFTEMP_Internal(libraries, param, logAction, "uart_485", "REFTEMP_485", "센서온도 저장 RS-485");
  ```
- `REFTEMP_Internal(libraries, param, logAction, uartKey, procId, procName)` **신규**
  - ResolveUart → ClearCommBuffer
  - JSON tests[0].command (SENTEMP) 송신 → ReadStep → 프레임/RV0 확인 → temp 문자열 추출
    (idx: 485=3, 232=2 — `uartKey.Contains("485")`로 분기, SENTEMP_Internal 파싱 패턴 참고)
  - 범위 체크: `GetParamDouble(param,"min",20)` ~ `GetParamDouble(param,"max",40)`
    → 밖이면 로그 + NG detail 후 `MakeResult(false, ...)` 즉시 반환 (REFTEMP 미송신)
  - REFTEMP 명령 조립: JSON `<REFTEMP,1,{temp}>` 템플릿의 `{temp}`를 읽은 문자열로 치환
    (VER_Internal `{serial}` 치환 패턴과 동일 — 하드코딩 금지 원칙 부합)
  - REFTEMP 송신 → ReadStep → RV0 + 응답 temp == 보낸 temp(문자열) 비교
  - `MakeResult(ok, procId, procName, details, ...)`
- `info_.procs[]` 등록 2줄 추가 (REFTEMP_232, REFTEMP_485)
- **VER_Internal 재사용 불가 사유**: ② 인자가 런타임(① 응답)에서 나옴 + 범위 가드 + echo 비교 → 전용 핸들러 필요

### JSON proc (모션 232/485 Y결합, UID 직후)
```json
{
  "libid": "vibetilt",
  "id": "REFTEMP_485",
  "name": "센서 온도 저장",
  "_comment": "SENTEMP로 읽은 센서온도를 REFTEMP로 기준값 저장. 20~40 범위 밖이면 저장안함+FAIL(보정 baseline 오염 방지). echo 일치 PASS. Y결합 1회",
  "param": {
    "TIMEOUT": 5000,
    "min": 20,
    "max": 40,
    "_minmax_comment": "센서온도 정상범위(보수적). 이 범위 밖 = 저장 차단 + FAIL",
    "tests": [
      { "command": "<SENTEMP,1>" },
      { "command": "<REFTEMP,1,{temp}>" }
    ]
  }
}
```
> 232는 `<SENTEMP>` / `<REFTEMP,{temp}>`. `{temp}` 치환은 코드가 ① 응답값으로 처리.
> **구현 시 최종 확정**: `{temp}` 치환 방식 vs 코드 분기 조립 (권장: 치환).

---

## 4. 신규 기능 B — 출하 상태 설정 (RCONF)

### 명령 / 응답 / PASS
| | 232 | 485 |
|---|-----|-----|
| 명령 | `<RCONF,1>` | `<RCONF,1,1>` |
| 응답 | `[RCONF,0,1]` | `[RCONF,0,1,1]` |
| PASS | RV0 + 프레임 완전일치 | RV0 + 프레임 완전일치 |

- 의미: UID·센서온도·현재 cal 값을 출하상태로 저장 (펌웨어 처리). OFFSET(Y-Cal) 직후 실행해 양축 cal 확정 상태 저장.
- echo로 검증할 값 없음(응답에 저장내용 미포함) → "저장 명령 성공(RV0)" + 프레임 일치가 판정 전부.

### 코드 (ff_vibetilt/VibeTilt.cs) — 기존 재사용
- thin wrapper만 추가, **기존 `VER_Internal` 그대로 호출** (OFFSET 패턴과 동일):
  ```csharp
  public IDictionary<string,object?> RCONF_232(...) =>
      VER_Internal(libraries, param, logAction, "uart_232", "RCONF_232", "출하 상태 설정 RS-232");
  public IDictionary<string,object?> RCONF_485(...) =>
      VER_Internal(libraries, param, logAction, "uart_485", "RCONF_485", "출하 상태 설정 RS-485");
  ```
  - RCONF 토큰은 VER/STOP 아님 → VER_Internal else 분기에서 RV0 확인(line 1158) + `expected_response`가 `[`로 시작 시 프레임 완전일치(line 1172~1176) → 신규 내부 로직 불필요
- `info_.procs[]` 등록 2줄 추가 (RCONF_232, RCONF_485)

### JSON proc (모션 232/485 Y결합, OFFSET 직후 / APPCFG_SAVE 직전)
```json
{
  "libid": "vibetilt",
  "id": "RCONF_485",
  "name": "출하 상태 설정",
  "_comment": "UID+센서온도+현재cal(양축 확정)을 출하상태값으로 저장. 응답 완전일치 PASS. OFFSET 직후/SAVE 직전(펌웨어 저장 의존성)",
  "param": {
    "TIMEOUT": 5000,
    "tests": [
      { "command": "<RCONF,1,1>", "expected_response": "[RCONF,0,1,1]" }
    ]
  }
}
```
> 232: `<RCONF,1>` / `[RCONF,0,1]`.

---

## 5. 변경 파일 목록

### 코드 (ff_vibetilt)
- `VibeTilt.cs`
  - `info_.procs[]` 4줄 추가: REFTEMP_232/485, RCONF_232/485 (line 22~43 블록)
  - thin wrapper 4개 추가 (REFTEMP_232/485, RCONF_232/485)
  - `REFTEMP_Internal` 1개 신규
  - `MODULE_VERSION` (line 13) 0.7.2.60605 → **0.7.3.6MMDD**
- `MotionJig.cs` `MODULE_VERSION` (line 14) → 0.7.3.6MMDD 통일
- `ff_vibetilt.csproj` `<Version>` (line 7) → 0.7.3.6MMDD

### JSON (8개 = 모션 232/485 × MP/TEST × Debug/Release)
- `output/Debug/net8.0-windows/{MP,TEST}_workspace_motion_{232_VL10,485_VL20}.json`
- `output/Release/net8.0-windows/{MP,TEST}_workspace_motion_{232_VL10,485_VL20}.json`
- 각 파일 Y결합 phase에:
  - REFTEMP proc → UID 직후 (현 index 10 위치)
  - RCONF proc → OFFSET 직후 / APPCFG_SAVE 직전 (현 index 12 위치)
- `_comment` 주석 규칙 준수, 8개 동일 패턴 (파일별 예외 없음)

---

## 6. 보류 / 별건 / 실기 확인

1. **REFTEMP 응답 echo 포맷** — 신규 명령이라 로그에 없음 → **실기 1회 확인** (보드가 보낸 2자리 그대로 echo하는지). 다르면 echo 비교를 double 수치비교로 폴백.
2. **REFTEMP `{temp}` 조립 방식** — 구현 시작 시 확정 (권장: JSON 치환).
3. **RCONF 영구저장 모델** — RCONF 자체 영구저장 vs APPCFG_SAVE 필요. **A안이면 어느 쪽이든 안전**(뒤에 SAVE 보장) → 지금 막지 않아도 진행 가능. 개발팀/스펙 확인은 권장.
4. **⚠️ FW 버전 불일치 (이번 범위 아님)**: 엑셀 VL10 `1.0.5` / VL20 `2.0.3` vs 현재 양산 VL20 `2.0.2`(2.0.2.3로 되돌리지 말 것). NOTE에 변경요청 없음 → 안 건드림.

---

## 7. 작업 순서 (구현, 승인 후)

1. `REFTEMP_Internal` 구현 (읽기 → 범위가드 → 치환조립 → 저장 → echo비교)
2. RCONF wrapper 2개 추가 (VER_Internal 재사용)
3. REFTEMP wrapper 2개 + procs[] 4줄 등록
4. 버전 0.7.3 3곳 (VibeTilt.cs:13 / MotionJig.cs:14 / csproj:7)
5. JSON 8개 Y결합 삽입 (REFTEMP UID뒤 + RCONF OFFSET뒤)
6. 빌드 → 232/485 실기 검증 (echo 포맷 #6-1 확인 포함, 범위 밖 케이스 1회 FAIL 확인)
7. CODE_ 문서 작성 → 사용자 커밋 (코드+JSON 같은 커밋, Debug+Release 동시)
