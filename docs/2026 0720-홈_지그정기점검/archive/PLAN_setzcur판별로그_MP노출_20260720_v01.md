# PLAN_setzcur판별로그_MP노출_20260720_v01

- 작성: Claude (2026-07-20, 집)
- 상태: **제안 — 사용자 확인 전 (코드 미구현)**
- 선행: R1b1 (커밋 5679f66) — `gyro_setzcur` 파라미터화 완료
- 범위: `ff_vibetilt/MotionJig.cs` 1개 파일, **JSON 변경 없음**

---

## 1. 문제

`MotionJig.cs:876`이 `if (GetParamBool(param, "gyro_setzcur", false))` 로 블록을 건너뛰므로,
**미실행과 실행성공이 로그상 동일하게 침묵**한다.

| 상황 | MP 로그 |
|---|---|
| 미실행 (R1b1 기본) | **없음** |
| 실행 성공 | **없음** (`[GYRO] RX:` 는 IsTest 게이트, `MotionJig.cs:2026`) |
| 실행 실패 | `[ZERO] FAIL: setzcur 실패` |

→ **로그만 봐서는 setzcur가 적용된 장비인지 아닌지 구분 불가.**

`MODULE_VERSION="R1b1"`은 `GetInfo`의 `ver`(`MotionJig.cs:73`)로만 나가고,
JSON으로 `gyro_setzcur: true`를 복원하면 버전으로도 구분되지 않는다.
**버전이 아니라 실효 상태를 찍어야 한다.**

## 2. 왜 필요한가

앞으로 영점 데이터를 축적할 때(지그 정기점검 등), 나중에
*"이 구간은 setzcur가 살아있던 데이터 아닌가?"* 를 판별할 근거가 없으면 데이터 전체가 흔들린다.

2026-07-20 *"공장초기화 전후 비교를 0.7.9로 돌려 무효"* 와 같은 계열의 문제 —
**로그가 자기 조건을 증명하지 못하는 것.**

---

## 3. 수정 — `MotionJig.cs:876`

```csharp
if (GetParamBool(param, "gyro_setzcur", false))
{
    if (!GyroSetZero(gyro, log))
    {
        log("[ZERO] FAIL: setzcur 실패");          // 기존 유지
        LogErrorInfo("GYRO_SETZCUR", log);
        return false;
    }
    log("[ZERO] setzcur 실행함 (gyro_setzcur=true)");                        // 신규
}
else
{
    log("[ZERO] setzcur 실행안함 (gyro_setzcur=false, 경사계 공장영점 기준)");  // 신규
}
```

- `IsTest` 게이트 **밖** → MP/TEST 공통
- 사이클당 1줄, 기존 `[ZERO]` 태그·스타일 유지
- **어느 경로로도 반드시 한 줄이 남는다** (침묵 경로 제거)

## 4. 판별 문자열

프로젝트 규칙(판정은 로그 문자열 Contains)에 맞춰, **서로의 부분문자열이 되지 않게** 선정.

| 판별 대상 | Contains |
|---|---|
| 제거 적용됨 (R1b1 기본) | `setzcur 실행안함` |
| setzcur 살아있음 | `setzcur 실행함` |
| 구버전 (R0 이하) | **둘 다 없음** → 그 자체가 구버전 식별자 |

> `실행함`은 `실행안함`의 부분문자열이 **아니다**(사이에 `안`). 검사 순서 무관하게 안전.
> `실행` / `미실행` 조합은 `미실행`이 `실행`을 포함하므로 **사용 불가.**

## 5. 결과 details (선택)

로그는 사람이, details는 품질모니터링이 읽는다. 추세 분석용으로 값도 남긴다.

```
"gyro_setzcur": false
```

---

## 6. 확인 필요

1. 로그 문구 `실행함` / `실행안함` 확정 — 기존 로그 파서·검색 스크립트가 `[ZERO]` 라인을 읽고 있다면 영향 확인
2. details 기록(§5) 포함 여부

## 7. 작업 순서

```
1. MotionJig.cs:876 양쪽 분기 로그 추가
2. MP 워크스페이스 1사이클 — [ZERO] setzcur 실행안함 확인
3. 커밋 (JSON 변경 없음 → 단독 커밋)
```

회사 lane (실기 1사이클 필요). 로직 변경 없음 — 로그만 추가.
