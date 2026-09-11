# CODE: RS-485 VIBE/TILT 스트리밍 → 폴링 방식 전환

- **날짜**: 2026-02-25
- **브랜치**: step4-motion-jig
- **수정 파일**: `ff_vibetilt/VibeTilt.cs`, `output/Debug/net8.0-windows/workspace.json`

---

## 배경

RS-485 VIBE/TILT는 기존에 `ATIM=10` + `START` → 10초 자동 스트리밍 방식이었음.

**문제점:**
- 종료 시 STOP을 2회 보내야 함 (이중 STOP)
- STOP 사이 sleep 대기 필요 (200ms + 500ms)
- workspace.json tests 배열이 복잡해짐

**실기기 확인:** RS-485도 `<GAC,1>` / `<AN,1>` 1회 폴링이 동작함.
→ RS-232와 동일하게 폴링 방식으로 전환하여 구조 단순화.

---

## 응답 포맷 차이 (중요)

| 방식 | GAC 응답 | AN 응답 |
|------|----------|---------|
| 스트리밍 | `[GAC,ID,X,Y,Z,RMS]` | `[AN,ID,X,Y]` |
| **폴링** | `[GAC,0,ID,X,Y,Z,RMS]` | `[AN,0,1,X,Y]` |

폴링 응답에는 **errcode(0) 필드가 앞에 추가**됨 → 파싱 offset 변경 필요.

---

## VibeTilt.cs 수정

### 1. `TryParseGacSample` — RS-485 폴링 포맷 처리

**위치:** ~line 293

```csharp
// 변경 전
if (isRs485)
{
    // RS-485 형식: [GAC,ID,X,Y,Z,RMS] (RV 없음)
    if (parts.Length >= 6 && TryParseGacAt(parts, 2, out x, out y, out z, out rms))
        return true;
    return false;
}

// 변경 후
if (isRs485)
{
    // RS-485 폴링: [GAC,0,ID,X,Y,Z,RMS] (errcode 포함, length=7, offset=3)
    if (parts.Length >= 7 && TryParseGacAt(parts, 3, out x, out y, out z, out rms))
        return true;
    // RS-485 스트리밍 호환: [GAC,ID,X,Y,Z,RMS] (length=6, offset=2)
    if (parts.Length >= 6 && TryParseGacAt(parts, 2, out x, out y, out z, out rms))
        return true;
    return false;
}
```

> 폴링 포맷 우선 시도, 실패 시 기존 스트리밍 포맷 폴백 (하위 호환).

---

### 2. `VIBE_Internal` — GAC 폴링 조건 (RS-232 제한 제거)

**위치:** ~line 1191

```csharp
// 변경 전
if (!isRs485 && !string.IsNullOrEmpty(gacCommand))
{
    // RS-232: GAC 폴링 방식 (workspace.json에서 읽은 명령 사용)
    SendCommand(uart, gacCommand, param, logAction);
}
// RS-485: START 후 자동 스트림 수신 (GAC 명령 불필요)

// 변경 후
if (!string.IsNullOrEmpty(gacCommand))
{
    // RS-232/RS-485 공통: GAC 폴링 방식
    SendCommand(uart, gacCommand, param, logAction);
}
```

> `!isRs485` 조건 제거. RS-485도 workspace.json에 `<GAC,1>` 항목이 있으면 폴링 명령 전송.

---

### 3. `VIBE_Internal` finally — GAC 마커 단순화

**위치:** ~line 1326

```csharp
// 변경 전
if (token == "GAC" || (isRs485 && token == "START"))
{
    afterGac = true;
    ...
}

// 변경 후
if (token == "GAC")
{
    afterGac = true;
    ...
}
```

> RS-485도 이제 GAC 항목이 workspace.json에 있으므로, START를 마커로 쓰는 분기 제거.

---

### 4. `TILT_Internal` — anCommand 변수 + 저장

**위치:** ~line 1488 (변수 선언), ~line 1508 (init 루프)

```csharp
// 변수 선언 추가 (stepList 선언 바로 아래)
var stepList = GetStepList(param).ToList();
string? anCommand = null;   // ← 추가

// AN 명령 미리 추출 (START 이후에 위치하더라도 찾을 수 있도록 전체 스캔)
anCommand = stepList
    .Select(t => GetTestString(t, "command"))
    .FirstOrDefault(cmd => NormalizeCommandToken(cmd) == "AN");

// init 루프: AN 마커 도달 시 break (anCommand는 이미 추출됨)
if (token == "AN")
    break; // 데이터 수집 마커, while 루프에서 처리
```

---

### 5. `TILT_Internal` — AN 폴링 루프 + 파싱

**위치:** ~line 1591 (while 루프), ~line 1607 (파싱)

```csharp
// 변경 전 while 루프
while (DateTime.UtcNow < deadline)
{
    // MODE,6 START 이후 장비가 AN 스트림을 밀어주는 패턴을 우선 사용
    ReadStep(...);

// 변경 후
while (DateTime.UtcNow < deadline)
{
    if (!string.IsNullOrEmpty(anCommand))
    {
        // RS-232/RS-485 공통: AN 폴링 방식
        SendCommand(uart, anCommand, param, logAction);
    }
    ReadStep(...);
```

```csharp
// 변경 전 파싱
int offset = isRs485 ? 2 : 1; // RS-485: [AN,ID,X,Y], RS-232: [AN,X,Y]
int requiredLength = isRs485 ? 4 : 3;

// 변경 후
// RS-485 폴링: [AN,0,ID,X,Y] (length=5, offset=3)
// RS-485 스트리밍 호환: [AN,ID,X,Y] (length=4, offset=2)
// RS-232: [AN,X,Y] (length=3, offset=1)
int offset, requiredLength;
if (!isRs485)
{
    offset = 1; requiredLength = 3;
}
else if (parts.Length >= 5)
{
    offset = 3; requiredLength = 5; // 폴링 포맷
}
else
{
    offset = 2; requiredLength = 4; // 스트리밍 호환
}
```

> `parts.Length >= 5`를 기준으로 폴링/스트리밍 포맷 자동 판별.

---

## workspace.json 수정

### VIBE_485 tests

```
변경 전: MODE,1,0 → ATIM,1,10 → SAM,1,10 → MODE,1,2 → START,1 → STOP,1 → MODE,1,0
변경 후: MODE,1,0 → ATIM,1,0  → SAM,1,10 → MODE,1,2 → START,1 → GAC,1 → STOP,1 → MODE,1,0
```

- `ATIM,1,10` → `ATIM,1,0`: 자동전송 비활성화
- `GAC,1` 추가 (START 다음): while 루프에서 매 회 `<GAC,1>` 폴링 명령으로 사용됨

### TILT_485 tests

```
변경 전 (복잡):
  ATIM,1,0 → STOP,1 → sleep 200 → STOP,1 → sleep 500
  → ATIM,1,10 → SAM,1,10 → MODE,1,6 → START,1
  → AN → STOP,1 → sleep 200 → STOP,1 → MODE,1,0

변경 후 (단순):
  ATIM,1,0 → STOP,1 → SAM,1,10 → MODE,1,6 → START,1
  → AN,1 → STOP,1 → MODE,1,0
```

- 이중 STOP + sleep 항목 제거
- `ATIM,1,10` 제거 (폴링이므로 자동전송 불필요)
- `<AN>` → `<AN,1>` (RS-485 폴링 명령)
- teardown 이중 STOP → 단일 STOP

---

## 실기기 검증 결과 (2026-02-25)

**총 결과: OK=8, FAIL=0** ✓

| 항목 | 결과 | 비고 |
|------|------|------|
| VIBE_485 `<GAC,1>` 폴링 | ✓ | `[GAC,0,1,X,Y,Z,RMS]` 수신 확인 |
| TILT_485 `<AN,1>` 폴링 | ✓ | `[AN,0,1,X,Y]` 수신 확인 |
| STOP 단일 1회 즉시 응답 | ✓ | 이중 STOP 불필요 확인 |

---

## 버그픽스 (실기기 1차 테스트 후)

### 증상
`<GAC,1>` / `<AN,1>` 폴링 명령이 전혀 전송되지 않고 TIMEOUT.

### 원인
`gacCommand` / `anCommand` 추출을 init 루프 내에서 수행했는데,
workspace.json 순서가 `START,1 → GAC,1` (GAC가 START 뒤)이므로
루프가 START를 먼저 만나 `break` → GAC에 도달하지 못해 변수가 `null` 유지.

### 수정
init 루프 시작 **전**에 LINQ로 stepList 전체를 스캔하여 명령 추출:

```csharp
// VIBE_Internal
string? gacCommand = stepList
    .Select(t => GetTestString(t, "command"))
    .FirstOrDefault(cmd => NormalizeCommandToken(cmd) == "GAC");

// TILT_Internal
anCommand = stepList
    .Select(t => GetTestString(t, "command"))
    .FirstOrDefault(cmd => NormalizeCommandToken(cmd) == "AN");
```

init 루프는 START까지 명령을 실행하는 역할만 유지.
GAC/AN 마커 도달 시 `break`하여 루프 종료 (명령 추출과 루프 역할 분리).
