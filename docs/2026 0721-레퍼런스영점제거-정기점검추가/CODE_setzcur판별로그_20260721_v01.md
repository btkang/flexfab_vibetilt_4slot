# CODE_setzcur판별로그_20260721_v01

- 작성: Claude (2026-07-21, 회사)
- PLAN: `docs/2026 0720-홈_지그정기점검/PLAN_setzcur판별로그_MP노출_20260720_v02.md`
- 상태: **구현 완료 — 빌드 OK (Debug, 오류 0), 실기 1사이클 확인 전**
- 범위: `ff_vibetilt/MotionJig.cs` 1개 파일, **JSON 변경 없음** (단독 커밋 가능)

---

## 1. 변경 내용

`MotionJig.cs` `InitMotionZero` 내 setzcur 분기 (기존 `:876`) — 양쪽 경로에 로그 1줄씩 추가.
로직 변경 없음, 로그만 추가.

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

- `IsTest` 게이트 **밖** → MP/TEST 공통 출력
- 침묵 경로 제거: 영점 사이클이 돌면 어느 경로로도 반드시 한 줄이 남음
- 사이클당 2줄 — #3(X)·#10(Y) 영점 각각 실행 시

## 2. 판별 방법 (로그 문자열 Contains)

전제: `[ZERO] 영점 초기화 시작`이 존재하는 로그 구간에서만 판별 유효
(내장 영점은 최초 1사이클만 실행되므로 2사이클째부터는 신버전도 로그 없음).

| 로그 | 판별 |
|---|---|
| `setzcur 실행안함` | 제거 적용본 (R1b1 기본) |
| `setzcur 실행함` | setzcur 활성본 (JSON으로 복원한 경우) |
| 둘 다 없음 (영점 시작 로그는 있음) | 구버전 (R0 이하) |

`실행함`은 `실행안함`의 부분문자열이 아님 (사이에 `안`) — 검사 순서 무관 안전.

## 3. 검증

| 항목 | 결과 |
|---|---|
| 빌드 (dotnet build flexfab.sln -c Debug) | 오류 0 (경고 205개는 기존 flexfab_mini) |
| 실기 MP 워크스페이스 1사이클 | **미실시** — #3/#10 각각 `[ZERO] setzcur 실행안함` 확인 필요 |

## 4. 남은 것

1. 실기 1사이클 → `[ZERO] setzcur 실행안함` 2줄 확인 (MP 워크스페이스)
2. 커밋 (JSON 변경 없음 → 단독 커밋, 사용자 승인 후)
3. details 배관은 지그 수평감시 PLAN 2단계에서 (PLAN §5 결정대로 본건 미포함)
