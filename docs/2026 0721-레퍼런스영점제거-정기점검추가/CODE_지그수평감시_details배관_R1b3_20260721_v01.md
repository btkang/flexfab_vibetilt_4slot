# CODE_지그수평감시_details배관_R1b3_20260721_v01

- 작성: Claude (2026-07-21, 회사)
- 배경: 라이트판(R1b2)에서 잘랐던 details 배관의 1단계 복원 — "dev 추세가 로그(raw)에만
  갇혀 있다, FAIL·NG는 구조적 저장소에 있어야 한다"(사용자) → 1단계만 진행 결정
- 상태: **구현 완료 — Debug/Release 빌드 오류 0, 실기 확인 전**
- 범위: `ff_vibetilt/MotionJig.cs`만. **workspace JSON 변경 없음** (R1b2 확정 스펙 그대로)
- 버전: **R1b3** (MODULE_VERSION ×2, csproj, Build.props, 릴리즈노트)

---

## 1. 변경 내용

`JigHealthCheck`에 `out Dictionary<string, object?>? detail` 추가 → `MotionZeroInternal`이
`MakeResult(pass, procId, procName, **details**, status)`로 전달 (기존 null 자리).

### 결과 JSON에 기록되는 필드 (#3/#10 영점 항목의 data)

| 상황 | 기록 |
|---|---|
| `jig_health_check: false` (Y축 #10 등) | **미기록** (data=null, 기존과 동일) |
| 홈 생략 사이클 | `jig_zero_axis`, `jig_homed: false` |
| 판독 실패 | `jig_zero_axis`, `jig_homed: true`, `jig_zero_result: "NO_READ"` |
| 정상 판정 | `jig_zero_axis/read/base/dev/result(OK·WARN·NG)`, `jig_homed`, `jig_ng_streak`, `_comment` |
| 팝업 발생 시 추가 | `jig_check_prompted: true`, `jig_check_result: "confirmed"/"cancelled"` |

- 측정항목 내장영점 호출부(:533 경로)는 `out _` — 판정은 하되 details는 영점 항목에서만
- 영점 FAIL 시에는 JigHealthCheck 미호출(기존 로직) → 기록 없음 (FAIL 자체가 기록)

## 2. 저장 경로 정리 (R1b3 이후)

| 저장소 | 내용 |
|---|---|
| 로그 파일 | [JIG] 전체 (사람이 읽는 용) — 기존 유지 |
| **결과 JSON** (시리얼별) | **신규: 위 필드** + 취소 FAIL 상태문자열(기존) — 영구 보존, 소급 파싱 가능 |
| DB (QMP) | 결과 JSON 수집 경로로 유입 — **화면/컬럼 연동은 2단계(별도)**, 미착수 |

## 3. 검증

- Debug/Release 빌드 오류 0, ProductVersion=R1b3 확인
- 실기 확인 항목 (다음 가동 시): #3 PASS 후 `Result\...\VL*.json`의 MOTION_ZERO 항목 data에
  `jig_zero_read` 등 필드 존재 + 값이 로그 [JIG] 줄과 일치

## 4. 남은 것

1. 실기 1사이클 → 결과 JSON 필드 확인
2. R1b3 배포폴더 생성 (JSON 무변경이라 exe/dll만 교체해도 됨)
3. QMP 2단계 (새 필드 파싱·추세 화면) — 필요 시 별도 PLAN
4. 커밋 (사용자 지시 대기)
