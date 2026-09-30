# VibeTilt PASS 조건 레벨별 설정 가이드

작성일: 2026-02-11
버전: 1.1 (실제 동작 기준 업데이트)

## 개요

VibeTilt 검사의 PASS 조건을 3가지 레벨(엄격, 보통, 완화)로 정의합니다.
**현재 설정은 엄격 레벨**이며, 실제 테스트 결과를 기준으로 작성되었습니다.

---

## 1. VIBE_232 (진동검사) - 레벨별 PASS 조건

| 항목 | 엄격 (현재) | 보통 | 완화 | 설명 |
|------|------------|------|------|------|
| **one_min**  | 0.9  | 0.85  | 0.8 | 1축(활성축) 최소값 |
| **one_max**  | 1.1  | 1.15  | 1.2 | 1축(활성축) 최대값 |
| **zero_min** | -0.1 | -0.15 | -0.2 | 0축(비활성축) 최소값 |
| **zero_max** | 0.1  | 0.15  | 0.2 | 0축(비활성축) 최대값 |
| **rms_min**  | 0.5  | 0.45  | 0.4 | RMS(진동강도) 최소값 |
| **rms_max**  | 0.65 | 0.7   | 0.75 | RMS(진동강도) 최대값 |
| **pass_consecutive_count** | 5회 | 3회 | 2회 | 연속 PASS 필요 횟수 |
| **실제 작업자 동작** | **3번 기울임** | 2번 기울임 | 1~2번 기울임 | 테스트 결과 |

### JSON 설정 예시

#### 엄격 (현재 설정)
```json
{
  "libid": "vibetilt",
  "id": "VIBE_232",
  "name": "진동검사",
  "param": {
    "TIMEOUT": 10000,
    "line_ending": "lf",
    "one_min": 0.9,
    "one_max": 1.1,
    "zero_min": -0.1,
    "zero_max": 0.1,
    "rms_min": 0.5,
    "rms_max": 0.65,
    "pass_consecutive_count": 5,
    "sampling_ms": 10,
    "operator_prompt": "지그를 앞/뒤 좌/우로 움직여주세요.\nPASS 조건: 1축 0.9~1.1, 0축 -0.1~0.1, RMS 0.5~0.65",
    "tests": [
      { "command": "<MODE,2>" },
      { "command": "<SAM,10>" },
      { "command": "<START>" },
      { "command": "<GAC>" },
      { "command": "<STOP>" }
    ]
  }
}
```

#### 보통
```json
{
  "libid": "vibetilt",
  "id": "VIBE_232",
  "name": "진동검사",
  "param": {
    "TIMEOUT": 10000,
    "line_ending": "lf",
    "one_min": 0.85,
    "one_max": 1.15,
    "zero_min": -0.15,
    "zero_max": 0.15,
    "rms_min": 0.45,
    "rms_max": 0.7,
    "pass_consecutive_count": 3,
    "sampling_ms": 10,
    "operator_prompt": "지그를 앞/뒤 좌/우로 움직여주세요.\nPASS 조건: 1축 0.85~1.15, 0축 -0.15~0.15, RMS 0.45~0.7",
    "tests": [
      { "command": "<MODE,2>" },
      { "command": "<SAM,10>" },
      { "command": "<START>" },
      { "command": "<GAC>" },
      { "command": "<STOP>" }
    ]
  }
}
```

#### 완화
```json
{
  "libid": "vibetilt",
  "id": "VIBE_232",
  "name": "진동검사",
  "param": {
    "TIMEOUT": 10000,
    "line_ending": "lf",
    "one_min": 0.8,
    "one_max": 1.2,
    "zero_min": -0.2,
    "zero_max": 0.2,
    "rms_min": 0.4,
    "rms_max": 0.75,
    "pass_consecutive_count": 2,
    "sampling_ms": 10,
    "operator_prompt": "지그를 앞/뒤 좌/우로 움직여주세요.\nPASS 조건: 1축 0.8~1.2, 0축 -0.2~0.2, RMS 0.4~0.75",
    "tests": [
      { "command": "<MODE,2>" },
      { "command": "<SAM,10>" },
      { "command": "<START>" },
      { "command": "<GAC>" },
      { "command": "<STOP>" }
    ]
  }
}
```

---

## 2. TILT_232 (기울기검사) - 레벨별 PASS 조건

| 항목 | 엄격 (현재) | 보통 | 완화 | 설명 |
|------|------------|------|------|------|
| **alive_min_delta_x** | 40° | 30° | 20° | X축 변화폭 최소값 |
| **alive_min_delta_y** | 25° | 20° | 15° | Y축 변화폭 최소값 |
| **x_min**         | -60.0 | -70.0 | -80.0 | X축 절대 최소값 |
| **x_max**           | 60.0 | 70.0 | 80.0 | X축 절대 최대값 |
| **y_min**         | -60.0 | -70.0 | -80.0 | Y축 절대 최소값 |
| **y_max**           | 60.0 | 70.0 | 80.0 | Y축 절대 최대값 |
| **실제 작업자 동작** | **4번 기울임** | 2~3번 기울임 | 1~2번 기울임 | 테스트 결과 |

### JSON 설정 예시

#### 엄격 (현재 설정)
```json
{
  "libid": "vibetilt",
  "id": "TILT_232",
  "name": "기울기검사",
  "param": {
    "TIMEOUT": 10000,
    "DURATION": 3000,
    "line_ending": "lf",
    "alive_check": true,
    "alive_min_delta_x": 40,
    "alive_min_delta_y": 25,
    "x_min": -60.0,
    "x_max": 60.0,
    "y_min": -60.0,
    "y_max": 60.0,
    "sampling_ms": 10,
    "tilt_baud": 115200,
    "normal_baud": 115200,
    "tests": [
      { "command": "<MODE,6>" },
      { "command": "<START>" },
      { "command": "<AN>" },
      { "command": "<STOP>" }
    ]
  }
}
```

#### 보통
```json
{
  "libid": "vibetilt",
  "id": "TILT_232",
  "name": "기울기검사",
  "param": {
    "TIMEOUT": 10000,
    "DURATION": 3000,
    "line_ending": "lf",
    "alive_check": true,
    "alive_min_delta_x": 30,
    "alive_min_delta_y": 20,
    "x_min": -70.0,
    "x_max": 70.0,
    "y_min": -70.0,
    "y_max": 70.0,
    "sampling_ms": 10,
    "tilt_baud": 115200,
    "normal_baud": 115200,
    "tests": [
      { "command": "<MODE,6>" },
      { "command": "<START>" },
      { "command": "<AN>" },
      { "command": "<STOP>" }
    ]
  }
}
```

#### 완화
```json
{
  "libid": "vibetilt",
  "id": "TILT_232",
  "name": "기울기검사",
  "param": {
    "TIMEOUT": 10000,
    "DURATION": 3000,
    "line_ending": "lf",
    "alive_check": true,
    "alive_min_delta_x": 20,
    "alive_min_delta_y": 15,
    "x_min": -80.0,
    "x_max": 80.0,
    "y_min": -80.0,
    "y_max": 80.0,
    "sampling_ms": 10,
    "tilt_baud": 115200,
    "normal_baud": 115200,
    "tests": [
      { "command": "<MODE,6>" },
      { "command": "<START>" },
      { "command": "<AN>" },
      { "command": "<STOP>" }
    ]
  }
}
```

---

## 3. 레벨별 특징 비교

### 엄격 (현재 설정) ⭐
- **용도**: 최종 양산 검사, 높은 품질 요구 환경
- **특징**:
  - 좁은 허용 범위 (1축 ±0.1, 0축 ±0.1, RMS 0.5~0.65)
  - 5회 연속 PASS 필요 (진동검사)
  - 큰 기울기 변화폭 필요 (ΔX≥40°, ΔY≥25°)
- **실제 작업자 동작**:
  - **진동검사**: 3번 기울임 → PASS
  - **기울기검사**: 4번 기울임 → PASS

### 보통
- **용도**: 일반 검사, 개발/디버깅 단계
- **특징**:
  - 적당한 허용 범위 (1축 ±0.15, 0축 ±0.15, RMS 0.45~0.7)
  - 3회 연속 PASS 필요 (진동검사)
  - 중간 기울기 변화폭 (ΔX≥30°, ΔY≥20°)
- **예상 작업자 동작**:
  - **진동검사**: 2번 기울임 → PASS
  - **기울기검사**: 2~3번 기울임 → PASS

### 완화
- **용도**: 초기 테스트, 센서 동작 확인, 불량 센서 선별
- **특징**:
  - 넓은 허용 범위 (1축 ±0.2, 0축 ±0.2, RMS 0.4~0.75)
  - 2회 연속 PASS 필요 (진동검사)
  - 작은 기울기 변화폭 (ΔX≥20°, ΔY≥15°)
- **예상 작업자 동작**:
  - **진동검사**: 1~2번 기울임 → PASS
  - **기울기검사**: 1~2번 기울임 → PASS

---

## 4. JSON 수정 방법

### workspace.json 경로
```
D:\03.Code\60.VL01\01.Flexfab_VL01\12.flexfab_VibeTilt\01.Flexfab_TEST\flexfab\output\Debug\net8.0-windows\workspace.json
```

### 수정 절차
1. **백업**: workspace.json 복사 → workspace_backup_YYMMDD.json
2. workspace.json 파일 열기
3. `VIBE_232` 또는 `TILT_232` 항목의 `param` 찾기
4. 위 JSON 예시를 참고하여 값 수정
5. 파일 저장
6. 프로그램 재시작

### 빠른 변경: 엄격 → 보통

**VIBE_232 수정 (7개 항목)**:
```
one_min: 0.9 → 0.85
one_max: 1.1 → 1.15
zero_min: -0.1 → -0.15
zero_max: 0.1 → 0.15
rms_min: 0.5 → 0.45
rms_max: 0.65 → 0.7
pass_consecutive_count: 5 → 3
```

**TILT_232 수정 (6개 항목)**:
```
alive_min_delta_x: 40 → 30
alive_min_delta_y: 25 → 20
x_min: -60.0 → -70.0
x_max: 60.0 → 70.0
y_min: -60.0 → -70.0
y_max: 60.0 → 70.0
```

---

## 5. VibeTilt.cs 구현 상태

### pass_consecutive_count JSON 지원
- **구현 완료**: VibeTilt.cs:1047에서 이미 JSON 읽기 구현됨 ✅
  ```csharp
  int passConsecutiveCount = GetParamInt(param, "pass_consecutive_count", 5);
  ```
- **사용 방법**: workspace.json의 `VIBE_232` param에 `pass_consecutive_count` 추가
- **기본값**: JSON에 항목이 없으면 `5`로 동작
- **추가 수정 불필요**: VibeTilt.cs 코드 수정 없이 바로 사용 가능

---

## 6. 주의사항

1. **백업 필수**: workspace.json 수정 전 반드시 백업
   ```bash
   copy workspace.json workspace_backup_YYMMDD.json
   ```

2. **JSON 문법 검증**:
   - 마지막 항목에는 쉼표(,) 제거
   - 문자열은 큰따옴표(") 사용
   - 숫자는 따옴표 없이 작성

3. **프로그램 재시작**: JSON 수정 후 반드시 프로그램 재시작

4. **실제 테스트**: 수정 후 실제 센서로 테스트하여 PASS 동작 확인

5. **operator_prompt 수정 권장**: PASS 조건 변경 시 팝업 메시지도 함께 변경

---

## 7. 이력

| 날짜 | 버전 | 내용 |
|------|------|------|
| 2026-02-11 | 1.0 | 최초 작성: 엄격/보통/완화 3단계 정의 |
| 2026-02-11 | 1.1 | 실제 테스트 결과 반영: 엄격(진동 3번, 기울기 4번 기울임), JSON 3가지 모두 추가 |
