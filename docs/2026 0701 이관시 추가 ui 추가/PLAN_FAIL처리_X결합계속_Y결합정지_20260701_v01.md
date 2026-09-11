# PLAN_ FAIL 처리 — X결합 FAIL은 계속, Y결합 FAIL은 정지

- 작성: Claude / 2026-07-01 / v01
- 브랜치: `feature/qmp-unified-autostart`
- 근거(NOTE_): `양산이관 추가 2026 0701.txt` — "x축 fail나도 y축 진행 / y축 fail나면 stop / 둘다 pass여야 최종 pass"
- 선행: ① 결합구분 표시(`FailInfoForm` Coupling/ExtractCoupling) 구현 완료 → **본 건이 그 ExtractCoupling 재사용**

---

## 0. 개정 이력

- **v01(초안):** X결합 FAIL 시 남은 X항목도 **계속 검사** 후 Y 진행.
- **v02(수정, 2026-07-01):** X결합 FAIL 시 **남은 X항목은 스킵하고 곧장 Y 첫 항목부터** 진행(시간 절약). ← 현재 구현. (사용자 피드백: X 이미 불량이면 남은 X 돌리는 건 시간낭비)

## 1. 요구사항 (확정 v02)

| 상황 | 동작 |
|------|------|
| **X결합** 항목에서 FAIL | **남은 X결합 항목 스킵 → Y결합 첫 항목(#8)부터** 진행 (시간 절약) |
| **Y결합** 항목에서 FAIL | **즉시 STOP** (남은 항목 스킵) |
| 최종 판정 | 기존 그대로: **X·Y 둘 다 PASS여야 최종 PASS** (`failCount==0`) |

> 구현: `xGroupFailed` 플래그 — X결합 FAIL 시 set → 루프 상단에서 남은 X결합 항목 Skip 처리. Y결합/무태그 FAIL은 기존 정지가드로 STOP. 불량보기는 결합 그룹당 첫 FAIL만(AddFailRecord dedup). 판정/pending 불변.

- 목적: X결합 시료가 떨어져도 **Y결합 시료 결과까지 한 번에 확인**. Y는 마지막 그룹이라 FAIL 시 이어갈 의미 없어 정지.
- **판정/저장/업로드(pending) 흐름은 변경 없음.** 정지 "지점"만 조정.

---

## 2. 현황 (코드 근거) — 정지가 일어나는 곳은 딱 2곳

`flexfab_mini/FlexfabForm.cs` 검사 루프 안:

**(1) 결과 FAIL 시 즉시중단 (2986~2991)**
```csharp
if (!isSuccess && !mainForm.checkBox_Process.Checked)   // 미체크 = 즉시중단(양산 기본)
{
    throw new OperationCanceledException("Fail stop ...");  // → catch(OCE) → break
}
```
**(2) 예외 발생 시 중단 (2999~3011)**
```csharp
catch (Exception ex) {
    ... failCount++; AddFailRecord(...);
    if (!mainForm.checkBox_Process.Checked) { wasCanceled = true; break; }
}
```

- `checkBox_Process.Checked` = `fail_continue` (체크=끝까지, 미체크=중단).
- 그 외 FAIL 지점(메서드 없음 3013 / 라이브러리오류 3021 / 시그니처 3950)은 **원래 break 안 하고 다음 항목 진행** → 변경 대상 아님.

---

## 3. 설계 (최소 변경 — 정지 조건에 "X결합이면 계속" 가드 추가)

### 결합 판별 (기존 헬퍼 재사용)
```csharp
bool isXCoupling = FailInfoForm.ExtractCoupling((string)(proc.name ?? "")) == "X결합";
```

### 정지 조건 변경
두 정지 지점의 조건을 **`... && !isXCoupling`** 으로:

**(1) 결과 FAIL:**
```csharp
if (!isSuccess && !mainForm.checkBox_Process.Checked && !isXCoupling)
{
    throw new OperationCanceledException("Fail stop (Y결합/무태그 실패로 중단)");
}
// X결합 FAIL → 위 조건 거짓 → throw 안 함 → 다음 항목(=Y결합)으로 계속
```

**(2) 예외:**
```csharp
if (!mainForm.checkBox_Process.Checked && !isXCoupling)
{
    wasCanceled = true; break;
}
// X결합 예외 → 계속
```

### 규칙 요약
| 항목 결합 | FAIL 시 |
|---|---|
| **X결합** | 계속 (정지 안 함) |
| **Y결합** | 정지 |
| **무태그(보드검사 등)** | 정지 (기존과 동일 — 회귀 없음) |

- `fail_continue=1`(체크)면 지금처럼 전부 계속 (본 변경 무관).

---

## 4. 판정·저장 영향 (변경 없음 확인)

- 끝까지 갔는데 X가 FAIL였으면 `failCount>0` → **FAIL 브랜치(3185)** → 롤백+FAIL팝업, **pending 저장/업로드 안 함** (기존과 동일).
- Y에서 정지하면 `wasCanceled` → **3042 브랜치** → 롤백+FAIL팝업, 저장 안 함 (기존과 동일).
- X·Y 모두 PASS → `failCount==0` → 기존 pending/업로드 그대로.
- → **production pending 파이프라인 무손상.** 순수하게 "정지 위치"만 이동.

---

## 5. 부수 효과 / 주의

- **장점:** X FAIL이어도 Y까지 검사 → FAIL 팝업·불량보기에 **X·Y 불량이 함께** 뜸(작업자 전체 파악).
- **주의(HW):** X결합 항목이 **모터/모션 오류(FINE_FAIL 등)** 로 FAIL인 경우, 지그가 비정상 상태에서 Y 모션을 이어가면 Y 결과 신뢰도가 낮을 수 있음. (통신/스펙 FAIL은 무관) → 실기 검증에서 X 모터고장 케이스 확인 권장. 필요 시 "X 모터오류는 정지" 세분화는 후속.
- 무태그 항목 정지 유지 → 보드검사/기존 워크스페이스 회귀 없음.

---

## 6. 수정 파일

| 파일 | 변경 |
|------|------|
| `flexfab_mini/FlexfabForm.cs` | 정지 2지점에 `isXCoupling` 가드 추가 (약 3~4줄) |

- **JSON 변경 없음. 판정 로직 불변. 헬퍼 신규 없음(ExtractCoupling 재사용).**

---

## 7. 검증

1. 빌드 성공.
2. **X결합 FAIL → Y 진행 확인**: X 항목 강제 FAIL 후 그리드가 Y결합 항목까지 실행되는지, 최종 FAIL 판정, 불량보기에 X·Y 불량 표시.
3. **Y결합 FAIL → 정지 확인**: Y 항목 FAIL 시 그 지점에서 멈추는지.
4. **무태그(보드검사) FAIL → 정지(회귀 없음)**.
5. **정상품**: X·Y 모두 PASS → 기존 pending/업로드/PASS팝업 그대로.
6. `fail_continue=1` 시 전부 계속(기존) 유지.

> 하드웨어(EMIO+485) 필요 — 지그 없이는 포트연결 단계에서 abort라 실행 안 됨. 실기 확보 시 검증.

---

## 8. 작업 순서

- [ ] 1. `FlexfabForm.cs` 정지 2지점에 `isXCoupling` 가드
- [ ] 2. Debug 빌드
- [ ] 3. (실기) §7 검증
- [ ] 4. `CODE_*.md` 정리
- [ ] 5. 사용자 커밋
