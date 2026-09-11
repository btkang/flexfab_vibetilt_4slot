# PLAN: 요약결과데이터(summary) 추가

**날짜:** 2026-04-14  
**브랜치:** step6-motion-jig-test  
**상태:** 미착수  

---

## 1. 목적

- 기존 판정 로직 **변경 없이**, 결과 JSON에 요약값(summary)을 추가
- 생산 중 품질 추세를 파악하여 **PASS인데도 FAIL 날 징후를 사전 감지**
- 로컬 JSON 저장 → USB 복사 → 분석 프로그램으로 시각화
- 나중에 DB 연결 시 실시간 분석으로 확장

---

## 2. 요약값 항목 및 정상 범위

### 2-1. TILT(기울기) summary

| 요약값 | 타입 | 정상 범위 | 주의 | 위험 | 의미 |
|--------|------|----------|------|------|------|
| `median` | double | ±20도: ±0.15 이내 / ±45도: ±0.4 이내 | spec의 75% | spec 초과 | tail 10개 중앙값 — 측정 대표값 |
| `std` | double | < 0.01 | 0.01~0.03 | > 0.03 | tail 10개 표준편차 — 산포, 흔들림 |
| `p2p` | double | < 0.03 | 0.03~0.08 | > 0.08 | tail 10개 Max-Min — 노이즈 폭 |
| `sample_count` | int | 100~115 | 80~99 | < 80 | 수집된 총 샘플 수 — 통신 상태 |
| `tail_count` | int | 10 | - | - | 판정에 사용된 샘플 수 |
| `retry_used` | int | 0 | 1 | 2~3 | 실제 리트라이 횟수 |

### 2-2. VIBE(진동) summary

| 요약값 | 타입 | 정상 범위 | 주의 | 위험 | 의미 |
|--------|------|----------|------|------|------|
| `median` | double | 0.99~1.01 (Y축: 0.98~1.02) | spec 경계 90% | spec 초과 | tail 5개 GAC 중앙값 |
| `std` | double | < 0.005 | 0.005~0.01 | > 0.01 | tail 5개 표준편차 — GAC 안정성 |
| `p2p` | double | < 0.01 | 0.01~0.02 | > 0.02 | tail 5개 Max-Min — GAC 흔들림 |
| `norm` | double | 0.99~1.01 | 0.98~0.99 또는 1.01~1.02 | < 0.98 또는 > 1.02 | √(x²+y²+z²) — 3축 합력 |
| `non_target_max` | double | < 0.03 | 0.03~0.05 | > 0.05 | 비측정축 절대값 최대 — 축 정렬 |
| `sample_count` | int | 100~115 | 80~99 | < 80 | 수집된 총 샘플 수 |
| `tail_count` | int | 5 | - | - | 판정에 사용된 샘플 수 |
| `retry_used` | int | 0 | 1 | 2~3 | 실제 리트라이 횟수 |

---

## 3. 패턴 분석 가이드 (생산팀 인수인계용)

| 현상 | 원인 추정 | 조치 |
|------|----------|------|
| median이 서서히 한 방향으로 밀린다 | 지그 drift, 온도 변화, 자이로 drift | 지그 점검, 영점 재확인 |
| std가 특정 시간대에 커진다 | 지그 체결 풀림, 진동, 작업환경 변화 | 지그 볼트 체결 확인, 환경 점검 |
| p2p가 갑자기 커진다 | 케이블 접촉 불량, 보드 노이즈 | 케이블 교체, 커넥터 점검 |
| retry_used가 증가한다 | 경계값 제품 증가 or 지그 상태 악화 | LOT 확인, 지그 상태 점검 |
| sample_count가 줄어든다 | USB 통신 지연, 보드 응답 지연 | USB 허브/케이블 점검 |
| non_target_max가 커진다 | 지그 축 정렬 틀어짐, 조립 편차 | 지그 정렬 재확인 |
| norm이 1.00에서 벗어난다 | 센서 보정 문제, 기구 자세 틀어짐 | 센서 교정, 기구 점검 |

---

## 4. JSON 구조 (변경 부분)

### 4-1. TILT 결과 (기존 + summary 추가)

```json
{
  "item": "X축 기울기 -20도",
  "value": 0.13,
  "gyro": -19.97,
  "vl": -19.84,
  "min": -0.2,
  "max": 0.2,
  "judge_mode": "all_in_spec(엄격)",
  "result": "OK",
  "summary": {
    "median": 0.128,
    "std": 0.003,
    "p2p": 0.012,
    "sample_count": 110,
    "tail_count": 10,
    "retry_used": 0
  }
}
```

### 4-2. VIBE 결과 (기존 + summary 추가)

```json
{
  "item": "Y축 진동 (-90도)",
  "value": 0.98,
  "min": 0.98,
  "max": 1.02,
  "judge_mode": "all_in_spec(엄격)",
  "result": "OK",
  "summary": {
    "median": 0.98,
    "std": 0.0,
    "p2p": 0.01,
    "norm": 1.0003,
    "non_target_max": 0.02,
    "sample_count": 106,
    "tail_count": 5,
    "retry_used": 0
  }
}
```

---

## 5. 코드 수정 범위

| 파일 | 수정 내용 |
|------|----------|
| `MotionJig.cs` — RunTiltMeasure | tail 배열에서 median/std/p2p 계산, retry 카운터, summary 객체 생성 |
| `MotionJig.cs` — RunVibeMeasure | tail 배열에서 median/std/p2p/norm/non_target 계산, summary 객체 생성 |
| 워크스페이스 JSON | 변경 없음 |
| 판정 로직 | 변경 없음 |
| MongoDB 업로드 | 변경 없음 (JSON에 포함되면 자동 업로드) |

### 계산 방법

```
median = tail 배열 정렬 후 중앙값
std = √(Σ(xi - mean)² / N)
p2p = Max(tail) - Min(tail)
norm = √(gac_x² + gac_y² + gac_z²)   ← VIBE만
non_target_max = max(|비측정축1|, |비측정축2|)   ← VIBE만
retry_used = 실제 리트라이 발생 횟수 (0~3)
sample_count = 스트리밍으로 수집된 전체 샘플 수
```

---

## 6. 운영 계획

| 단계 | 내용 | 시기 |
|------|------|------|
| 1단계 | summary 추가 → 로컬 JSON 저장 | 지금 |
| 1-1 | 분석 프로그램 (Python) — USB 복사 JSON → 시각화 | 이후 |
| 2단계 | DB 연결 → 다른 PC에서 실시간 조회/시각화 | 네트워크 환경 갖춰진 후 |

---

## 7. 영향도

- 기존 판정 로직: **변경 없음**
- 검사 시간: **영향 없음** (이미 수집된 배열에서 계산만 추가)
- 결과 JSON 크기: 항목당 ~100바이트 증가 (무시할 수준)
- 기존 DB 데이터: **호환** (summary 없는 기존 문서와 공존 가능)
