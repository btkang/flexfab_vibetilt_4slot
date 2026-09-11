# PLAN: 보드 VIBE/TILT judge_mode 추가 (방향별 검사)

## 배경

보드 검사에서 현재 값 변화만 확인(방향 무시). 불량 감지 강화를 위해 방향별 검사 옵션 추가.

## JSON 설정

```json
"judge_mode": 1,
"_judge_mode_comment": "1: 값 변화만 확인(방향무시) | 2: 방향별 각각 확인(축별+부호)"
```

- 기본값: 1 (없으면 현재 동작 유지, 하위 호환)
- VIBE, TILT_X, TILT_Y 각 항목 param에 추가

## judge_mode 비교

### 기울기 (TILT)

| 항목 | mode=1 (현재) | mode=2 (추가) |
|---|---|---|
| X축 | \|X\| 5~95도면 PASS | +방향 PASS → 팝업 → -방향 PASS |
| Y축 | \|Y\| 5~95도면 PASS | +방향 PASS → 팝업 → -방향 PASS |
| 부호 | 절대값 (무시) | +/- 구분 |
| 방향 | 아무 방향 | 양쪽 각각 |
| 라운드 | 1회 | 2회 (같은 항목 내) |

### 진동 (VIBE)

| 항목 | mode=1 (현재) | mode=2 (추가) |
|---|---|---|
| 판정 | 흔들면 PASS | X=1G, Y=1G, Z=1G 각각 확인 |
| 축 구분 | 안 함 | X/Y/Z 각각 |
| 라운드 | 1회 | 3회 (같은 항목 내) |
| 팝업 | "앞뒤나 좌우로 기울이세요" | "X방향" → "Y방향" → "수평 놓으세요" |

## 구현 (코드 수정)

### 파일: `ff_vibetilt/VibeTilt.cs`

#### TILT_Internal

```
judge_mode 읽기: int judgeMode = GetParamInt(param, "judge_mode", 1);

mode=1: 현재 코드 그대로 (Math.Abs)
mode=2:
  라운드1: "X축 +방향으로 기울이세요" → x >= x_min && x <= x_max (부호 그대로)
  라운드1 PASS → STOP → 버퍼클리어
  라운드2: "X축 -방향으로 기울이세요" → x >= -x_max && x <= -x_min
  라운드2 PASS → 최종 OK
```

#### VIBE_Internal

```
mode=1: 현재 코드 그대로 (모션 감지 + SPEC 범위)
mode=2:
  라운드1: "X방향으로 기울이세요" → X축 1G 확인 (one_min~one_max), Y,Z는 0축
  라운드1 PASS → STOP → 버퍼클리어
  라운드2: "Y방향으로 기울이세요" → Y축 1G 확인
  라운드2 PASS → STOP → 버퍼클리어
  라운드3: "수평으로 놓으세요" → Z축 1G 확인
  라운드3 PASS → 최종 OK
```

## JSON 수정: 보드 4파일

- workspace_board_232_TEST.json
- workspace_board_232_MP.json
- workspace_board_485_TEST.json
- workspace_board_485_MP.json

각 VIBE/TILT 항목 param에 `judge_mode` 추가.

## 검사 항목 수

- mode=1: 10개 (현재 그대로)
- mode=2: 10개 (같은 항목 내에서 라운드 추가, 항목 수 변경 없음)

## 영향 범위

- 보드 VIBE_232/485, TILT_X/Y_232/485만 해당
- 모션지그: MotionJig.cs 별도 코드 → 영향 없음
- judge_mode 없으면 기본 1 → 기존 동작 100% 유지

## 검증

1. judge_mode=1: 아무 방향 기울이면 PASS (기존 동작)
2. judge_mode=2 TILT: +방향 PASS → -방향 PASS → OK
3. judge_mode=2 TILT: +방향만 PASS → -방향 안 기울이면 TIMEOUT FAIL
4. judge_mode=2 VIBE: X=1G → Y=1G → Z=1G 순서대로 확인
