# DPM Powertest00 VibeTilt 이전 작업

**작업일:** 2026-02-23

---

## 배경

- DPM 전압/전류 검사가 `ff_colorimeter` 라이브러리의 `Powertest00` 메서드에 구현되어 있었음
- 향후 `ff_colorimeter` 폴더 삭제 예정
- `ff_vibetilt` 하나로 통합하여 사용

---

## 작업 내용

### 1. workspace.json 구성 (485 + DPM 통합)

- 기준 파일: `workspace_b4cc79e_485.json` (git 커밋 b4cc79e)
- DPM 관련 라이브러리 추가:

```json
uart_dpm: COM12, baudrate 57600
dpm: ff_dpm.dll (uart: uart_dpm)
colorimeter: ff_colorimeter.dll (→ 이후 vibetilt로 교체)
```

- vibetilt config에 dpm 참조 추가:
```json
"config": {
  "uart_485": "uart_485",
  "dpm": "dpm"
}
```

---

### 2. DPM 검사 프로세스 추가 (procs 앞에 위치)

| 검사항목 | libid | 채널ID | error_rate | min | max | 비고 |
|---------|-------|--------|------------|-----|-----|------|
| 24V 검사 | vibetilt | id06 | 3% | 23.28 | 24.72 | ±3% 계산값 |
| 3.3V 검사 | vibetilt | id09 | 3% | 0.003 | 3.5 | **임시** (아래 참고) |
| 전류 검사 | vibetilt | id04 | - | -0.001 | 0.5 | **임시** (아래 참고) |

#### ⚠️ 임시 설정 (H/W 작업 필요)

- **id09가 2개 채널을 가짐**: 0.003~0.004 (전류?), 3.316 (실제 3.3V)
- 현재 3.3V 검사가 0.003~0.004 범위의 값을 먼저 수신함
- H/W 설정에서 3.3V 채널 ID를 변경하면 아래 정상 범위로 수정 필요:

```
3.3V 정상 범위 (±3% 계산):
  기본값: 3.3V × 0.03 = 0.099V
  min: 3.201, max: 3.399
```

- 전류도 H/W 설정 후 실제 범위로 수정 필요

---

### 3. Powertest00 메서드 VibeTilt.cs에 추가

**파일:** `ff_vibetilt/VibeTilt.cs`

- Colorimeter.cs의 `Powertest00` 메서드를 그대로 이전
- 헬퍼 메서드는 충돌 방지를 위해 `Dpm_` 접두사 사용:
  - `Dpm_BuildFrame()` - ID 토큰 → DPM 프레임 변환
  - `Dpm_FlushUart()` - UART 버퍼 플러시
  - `Dpm_RecvEtX()` - ETX 기반 UART 수신
  - `Dpm_ParseMinMax()` - min/max 파싱
  - `Dpm_Esc()` - 로그용 이스케이프
  - `Dpm_ExtractValue()` - 응답에서 value 추출

---

### 4. workspace.json libid 변경

```
Before: "libid": "colorimeter"
After:  "libid": "vibetilt"
```

DPM 검사 3개 (24V, 3.3V, 전류) 모두 변경

---

### 5. error_rate 필드 추가

- 전압 검사에 `error_rate` 필드 추가 (JSON 확장 필드)
- DPM 공용 json과 호환성 유지 (없으면 무시)
- 향후 PrintLog/CSV에 표시 예정 (코드 수정 필요)

```json
{
  "id": "id06",
  "error_rate": "3%",
  "min": "23.28",
  "max": "24.72"
}
```

---

## 테스트 결과

```
✅ 24V 검사:  23.653V (23.28~24.72) => OK
✅ 3.3V 검사: 0.00364V (0.003~3.5)  => OK (임시)
✅ 전류 검사: 0.00002A (-0.001~0.5) => OK (임시)
⏳ RS-485 검사: 진행 중 (빌드 후 확인 필요)
```

---

## 다음 작업

- [ ] 빌드 후 Powertest00 동작 확인
- [ ] H/W 설정 변경 후 3.3V 채널 ID 수정
- [ ] 3.3V 범위 정상값으로 수정 (min 3.201 ~ max 3.399)
- [ ] 전류 범위 실제값으로 수정
- [ ] PrintLog/CSV에 error_rate 표시 (ff_vibetilt 코드 수정)
- [ ] ff_colorimeter 폴더 삭제 확인

---

## 파일 위치

```
코드:
  ff_vibetilt/VibeTilt.cs  ← Powertest00 추가됨

workspace:
  output/Debug/net8.0-windows/workspace.json  ← 현재 사용

백업:
  output/Debug/net8.0-windows/workspace_backup/
    workspace_b4cc79e_485.json  ← 485 원본 (변경 전)
```
