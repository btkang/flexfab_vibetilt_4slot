# PLAN: 반복테스트 기능 추가

## 날짜: 2026-03-26

---

## 1. 개요

DPM 검사 항목에 반복 측정 기능 추가. JSON 설정으로 제어.

## 2. 두 가지 반복 모드

### 2-1. 단일 항목 반복 (`test_count`)
- **위치**: proc의 param 레벨
- **동작**: 해당 검사 항목 1개를 N회 반복 측정
- **용도**: DPM 24V만 100회 측정 → 편차 확인

```json
{
  "id": "DPM_24V",
  "name": "24V 검사 (10회 반복)",
  "param": {
    "test_count": 10,
    "test_count_delay": 500,
    "delay": 1000,
    "tests": [...]
  }
}
```

- `test_count`: 반복 횟수 (기본값 1, 생략 시 기존 동작)
- `test_count_delay`: 반복 간 대기시간 ms (기본값 500)

### 2-2. 전체 항목 반복 (`repeat_count`)
- **위치**: project 레벨
- **동작**: Skip 아닌 모든 검사 항목을 N회 반복
- **용도**: DPM 24V + 3.3V + 전류 전체를 5라운드 반복

```json
{
  "id": "prj-vl10",
  "name": "VL10 검사 (RS-232)",
  "repeat_count": 5,
  "repeat_delay": 1000,
  "procs": [...]
}
```

- `repeat_count`: 전체 반복 횟수 (기본값 1, 생략 시 기존 동작)
- `repeat_delay`: 라운드 간 대기시간 ms (기본값 1000)

## 3. 수정 파일

| 파일 | 수정 내용 |
|------|----------|
| `ff_vibetilt/VibeTilt.cs` | `Powertest00` 메서드에 `test_count` 반복 루프 추가 |
| `flexfab_mini/FlexfabForm.cs` | `MainDoProject`에 `repeat_count` 반복 루프 추가 |

## 4. 로그 출력 형식

### test_count (단일 항목)
```
[반복 1/10] Powertest00 범위 판정: min=4.800, max=5.200, value=4.985V => OK
[반복 2/10] Powertest00 범위 판정: min=4.800, max=5.200, value=4.991V => OK
...
[반복 10/10] 완료: OK 10/10
```

### repeat_count (전체 항목)
```
========== 전체 반복 [1/5] ==========
테스트 실행 중: 24V 검사 → OK
테스트 실행 중: 3.3V 검사 → OK
테스트 실행 중: 전류 검사 → OK
========== 전체 반복 [2/5] ==========
...
```

## 5. 판정 기준

### test_count
- **모든 회차 PASS** → success: true
- **1회라도 FAIL** → success: false
- retmsg에 회차별 측정값 포함

### repeat_count
- 각 라운드마다 기존과 동일한 판정
- DataGridView: 라운드마다 결과 갱신 (마지막 라운드 결과 표시)
- 1회라도 FAIL 시 중단 여부: 기존 `checkBox_Process` 옵션 따름

## 6. Stop 버튼
- 두 모드 모두 CancellationToken으로 즉시 중단 가능

## 7. 기존 동작 호환
- `test_count` 생략 또는 1 → 기존과 동일
- `repeat_count` 생략 또는 1 → 기존과 동일
- JSON에 필드 없으면 기본값 1 → 변경 없음
