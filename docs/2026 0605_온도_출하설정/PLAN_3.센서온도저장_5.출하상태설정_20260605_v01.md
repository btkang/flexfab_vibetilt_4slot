# PLAN_ 센서온도 저장(REFTEMP) + 출하상태 설정(RCONF) 기능추가

- 날짜: 2026-06-05
- 근거(Truth Source): `docs/20260602_양산용 지그 제작 요청서_진동기울기센서 - 3.검사 항목 양식(기능검사).xlsx`
- 원 요청: `NOTE_3.센서온도저장_5.출하상태설정_기능추가_20260605.md`
- 대상 버전: **0.7.3** (현재 0.7.2.60605의 다음)
- 상태: **계획 확정. 구현은 다음 세션** (사용자가 이 문서 보고 진행)

---

## 0. 핵심 결정 사항 (확정)

| 항목 | 결정 | 사유 |
|------|------|------|
| 삽입 phase | **Y결합에만** (X결합 안 건드림) | X결합→Y결합 순서 → Y가 최종 phase. 저장/설정류는 최종 상태에서 1회 |
| REFTEMP 위치 | Y결합, **UID 직후 (Y-Cal 직전)** | 온도는 축 무관 1회면 충분. 엑셀 #3(Cal 직전) 관계 보존 |
| RCONF 위치 | Y결합, **Y-Cal 직후 / Y-SAVE 직전** | X cal+Y cal 모두 확정된 최종상태 저장 → 출하상태에 양축 cal 포함 |
| 버전 | 0.7.3 (csproj+VibeTilt+MotionJig 통일) | — |
| REFTEMP 명령 | JSON `{temp}` 치환 방식 (UID의 {serial}과 동일) | 하드코딩 금지 원칙 부합 — **구현 시 최종 확정 필요** |
| FW 버전 불일치 | **이번 작업 제외** | NOTE에 요청 없음. 별건 (아래 6번) |

> ⚠️ **왜 X결합이 아니라 Y결합인가**: RCONF 스펙 = "UID+센서온도+**현재 cal 값**을 출하상태로 저장". X결합 #5 위치(엑셀 그대로)에 두면 Y cal(#11)이 아직 안 됐으므로 **Y축 cal이 출하상태에서 누락**. RCONF가 호출시점 cal을 스냅샷한다는 전제 하에 Y결합 맨끝이 안전. (RCONF가 단순 플래그면 위치 무관하지만, 안전하게 최종 phase 채택)

---

## 1. 범위 확정 (엑셀 기능검사 시트 1~13 대조)

엑셀 "기능검사" = **모션지그 검사 전체 플로우**. 13개 중 **신규는 2개뿐**.

| 엑셀# | 항목 | 명령(232 / 485) | 구현 상태 |
|-------|------|-----------------|-----------|
| 1 | FW version 확인 | `<VER>` / `<VER,1>` | 기존 FWVER |
| 2 | 시리얼번호 설정 | `<UID,..>` / `<UID,1,..>` | 기존 UID (변경 없음) |
| **3** | **센서 온도 저장** | `<SENTEMP>`+`<REFTEMP,XX.XX>` / `<SENTEMP,1>`+`<REFTEMP,1,XX.XX>` | **🆕 신규** |
| 4 | Calibration | `<OFFSET,1>` / `<OFFSET,1,1>` | 기존 OFFSET |
| **5** | **출하 상태 설정** | `<RCONF,1>` / `<RCONF,1,1>` | **🆕 신규** |
| 6 | 파라미터 저장 | `<APPCFG,SAVE>` / `<APPCFG,1,SAVE>` | 기존 APPCFG_SAVE |
| 7~13 | 기울기·가속도·cal·APPCFG확인 | — | 기존 모션검사 |

> 엑셀은 단일 선형 플로우. 실제 JSON은 **X결합 / Y결합 2회 장착**으로 분리되어 UID·OFFSET·APPCFG_SAVE가 두 phase에 중복. (UID=방어적 중복 / OFFSET·SAVE=축별 필수)

---

## 2. 삽입 후 Y결합 phase 최종 순서

현재 Y결합: `#8 FWVER → #9 UID → #10 Y영점 → #11 Y-Cal(OFFSET) → #12 Y-SAVE`

신규 삽입 후:
```
#8  FWVER_xxx
#9  UID_xxx               (시리얼)
🆕  REFTEMP_xxx           ← UID 직후 (온도저장)
#10 MOTION_ZERO_xxx       (Y영점 초기화)
#11 OFFSET_xxx            (Y-Calibration)
🆕  RCONF_xxx             ← Y-Cal 직후 / SAVE 직전 (출하설정)
#12 APPCFG_SAVE_xxx       (Y-SAVE)
```
→ 엑셀 상대순서(UID→온도저장→…→Cal→출하설정→SAVE)를 Y phase 안에서 보존.
→ **X결합 phase는 변경 없음.**

---

## 3. 신규 기능 A — 센서 온도 저장 (REFTEMP)

### 동작
1. `SENTEMP` 송신 → 응답에서 온도 문자열 `XX.XX` 추출
   - 232: `[SENTEMP,0,XX.XX]` (index 2)
   - 485: `[SENTEMP,0,1,XX.XX]` (index 3)
2. 추출한 온도 문자열을 그대로 `REFTEMP`로 송신
   - 232: `<REFTEMP,XX.XX>` → `[REFTEMP,0,XX.XX]`
   - 485: `<REFTEMP,1,XX.XX>` → `[REFTEMP,0,1,XX.XX]`
3. **판정**: REFTEMP 응답 온도값이 SENTEMP에서 읽은 값과 **일치하면 PASS** (엑셀 "응답이 위와 일치하는지 확인")

> 온도값은 double 파싱 없이 **문자열 그대로** 전달/비교 (25.30↔25.3 포맷 불일치 방지). RV(0)+대괄호 프레임 확인 포함.
> 온도 정상범위(20~40) 판정은 **기존 보드검사 SENTEMP가 담당** → 여기선 echo 일치만 (중복 판정 안 함).

### 코드 (ff_vibetilt/VibeTilt.cs)
- `REFTEMP_232` / `REFTEMP_485` (thin wrapper) → `REFTEMP_Internal(...)`
- `REFTEMP_Internal(uartKey, procId, procName)` 신규 (기존 `SENTEMP_Internal` 파싱 재사용 + REFTEMP write/echo검증)
- `info_.procs[]`에 `REFTEMP_232`, `REFTEMP_485` 등록
- **VER_Internal 재사용 불가 사유**: REFTEMP 인자가 런타임(SENTEMP 응답)에서 나옴 → param 치환 방식만으론 부족, 전용 핸들러 필요

### JSON proc (모션 232/485 Y결합, UID 직후)
```json
{
  "libid": "vibetilt",
  "id": "REFTEMP_485",
  "name": "센서 온도 저장",
  "_comment": "SENTEMP로 읽은 센서온도를 REFTEMP로 기준값 저장. echo 일치 PASS. 범위판정은 보드검사 SENTEMP 담당. Y결합 1회",
  "param": {
    "TIMEOUT": 5000,
    "tests": [ { "command": "<SENTEMP,1>" } ]
  }
}
```
> REFTEMP 명령은 코드가 런타임 조립(SENTEMP 응답값 사용). **구현 시 `{temp}` 치환 방식 vs 코드 분기 최종 확정.**

---

## 4. 신규 기능 B — 출하 상태 설정 (RCONF)

### 동작
- 232: `<RCONF,1>` → `[RCONF,0,1]`
- 485: `<RCONF,1,1>` → `[RCONF,0,1,1]`
- **판정**: 응답 프레임 완전일치 + RV(0) → PASS
- 의미: UID·센서온도·현재 cal 값을 출하상태로 저장 (펌웨어 처리). **Y-Cal 직후 실행해 양축 cal 확정 상태 저장**

### 코드 (ff_vibetilt/VibeTilt.cs) — 기존 재사용
- `RCONF_232` / `RCONF_485` (thin wrapper) → **기존 `VER_Internal` 그대로 호출**
  - RCONF은 VER/STOP 토큰이 아니므로 `else` 분기에서 RV(0) + `expected_response` 완전일치 검증 → **신규 내부 로직 불필요**
- `info_.procs[]`에 `RCONF_232`, `RCONF_485` 등록

### JSON proc (모션 232/485 Y결합, OFFSET 직후 / APPCFG_SAVE 직전)
```json
{
  "libid": "vibetilt",
  "id": "RCONF_485",
  "name": "출하 상태 설정",
  "_comment": "UID+센서온도+현재cal(양축 확정)을 출하상태값으로 저장. 응답 완전일치 PASS. Y-Cal 직후",
  "param": {
    "TIMEOUT": 5000,
    "tests": [ { "command": "<RCONF,1,1>", "expected_response": "[RCONF,0,1,1]" } ]
  }
}
```

---

## 5. 변경 파일 목록

### 코드
- `ff_vibetilt/VibeTilt.cs`
  - procs[] 4줄 추가 (REFTEMP_232/485, RCONF_232/485)
  - 핸들러 4개 + `REFTEMP_Internal` 1개 추가
  - `MODULE_VERSION` 0.7.2.60605 → **0.7.3.xxxxx**
- `ff_vibetilt/MotionJig.cs` MODULE_VERSION 통일 (표기만)
- `ff_vibetilt/ff_vibetilt.csproj` Version 0.7.3.xxxxx

### JSON (8개 = 모션 232/485 × MP/TEST × Debug/Release)
- `output/Debug/net8.0-windows/{MP,TEST}_workspace_motion_{232_VL10,485_VL20}.json`
- `output/Release/net8.0-windows/{MP,TEST}_workspace_motion_{232_VL10,485_VL20}.json`
- 각 파일: **Y결합 phase에만** REFTEMP(UID 뒤) + RCONF(OFFSET 뒤) 삽입
- `_comment` 주석 규칙 준수

---

## 6. 보류 / 별건

1. **REFTEMP 명령 조립 방식** — 구현 시작 시 최종 확정:
   - (권장) JSON `<REFTEMP,1,{temp}>` 템플릿 + 코드 `{temp}` 치환 (UID {serial} 방식과 일관)
   - (대안) 코드 232/485 분기 조립
2. **⚠️ FW 버전 불일치 (이번 작업 범위 아님)**
   - 엑셀: VL10 `1.0.5`, VL20 `2.0.3`
   - 현재 JSON/양산: VL20 `2.0.2` (메모리: 2.0.2.3로 되돌리지 말 것)
   - NOTE에 FW 변경 요청 없음 → 안 건드림. **별도 확인 안건**.

---

## 7. 작업 순서 (다음 세션, 승인 후)
1. REFTEMP 명령 방식(6-1) 확정 → VibeTilt.cs `REFTEMP_Internal` 구현
2. RCONF wrapper 추가 (VER_Internal 재사용)
3. procs[] 등록 + 버전 4곳 (0.7.3)
4. JSON 8개 Y결합에 삽입 (모션 232/485 MP/TEST Debug+Release)
5. 빌드 → 232/485 실기 검증
6. CODE_ 문서 작성 → 사용자 커밋
