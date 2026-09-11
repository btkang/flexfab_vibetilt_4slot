# CODE_setzcur 제거 (파라미터화, 기본 false) — R1b1

- 작성: Claude (2026-07-20)
- 근거 PLAN: `docs/2026 0715-모션센서(imu) 검사방법 지그 전반 검토/PLAN_setzcur제거_20260715_v01.md`
- 결정 사항 (사용자 확정, 2026-07-20):
  - **파라미터화 + 기본 false** (빌드하면 즉시 제거 동작, JSON으로만 복원)
  - **메인폼 UI 없음** — 옵션은 workspace JSON에만. 추후 "살리자" 결정 시 그때 UI 검토
  - 버전 체계 **R 전환**: R{n}=공식배포 / b{m}=내부베타. R0 = 260701_0.7.10 배포본(구표기 1.0.0), 본 변경 = **R1b1**(R1로 가는 첫 검증본)
- 배경: 계보 실증(BG01 이식 잔재) + τ=+0.090° 실측(7/16) + 담당자 확인(7/20, 요청서에 setzcur 없음)

---

## 1. 변경 내용

### 1.1 코드 — `ff_vibetilt/MotionJig.cs` (영점 초기화 #3/#10 공용 경로, 1곳)

기존 871-877의 무조건 setzcur 블록을 조건부로 변경:

```csharp
// 6. 자이로 기준점 설정 (setzcur) — 기본 미실행
// 지그요청서에 없는 명령(BG01 이식 잔재, 2026-02-24). FineAdjust가 공장영점 기준으로
// 수평을 만들므로 setzcur는 잔차만 비휘발 영점에 누적시킴 (τ=+0.090° 실측, 2026-07-16).
// 제거 적용 전 센서에 setzfac(공장영점 복원) 1회 선행 필수.
// 되돌리기: JSON #3/#10 param에 "gyro_setzcur": true
if (GetParamBool(param, "gyro_setzcur", false))
{
    if (!GyroSetZero(gyro, log)) { ...기존 FAIL 처리... }
}
```

- `GyroSetZero()` 메서드(1993행~)는 **삭제하지 않고 유지** — 복원 옵션 + 향후 setzfac 자동화 재사용
- FAIL 재영점 경로(#4/#6 내 재영점)는 measure 항목 param을 쓰므로 기본 false 동일 적용

### 1.2 버전 — R 체계 전환 (R1b1)

| 파일 | 변경 |
|---|---|
| `ff_vibetilt/VibeTilt.cs:13` | `MODULE_VERSION = "1.0.0"` → `"R1b1"` |
| `ff_vibetilt/MotionJig.cs:14` | 동일 |
| `ff_vibetilt/ff_vibetilt.csproj` | `<Version>1.0.0</Version>` (목표 R번호=Major) / `<InformationalVersion>R1b1</InformationalVersion>` |
| `RELEASE_NOTE_ff_vl.txt` | 규정 R 전환 명시, 1.0.0 → **R0** 개칭, **R1b1(작업중)** 항목 추가 |
| `버전변경이력_ff_vl_README.txt` | 규정 R 전환, 1.0.0 → R0 개칭 |
| 규정 문서 | `docs/2026 0720-setzcur제거_R2/검사프로그램 버전 관리 참고자료_R체계.txt` 신규 |

### 1.3 JSON — 모션 워크스페이스 9파일 × #3/#10 (2곳씩, 총 18곳)

Debug 4 + Release 5 (`TEST_..._지그없음` 포함), MOTION_ZERO param에 삽입:

```json
"gyro_setzcur": false,
"_gyro_setzcur_comment": "레퍼런스(자이로) 영점 tare 실행 여부. 지그요청서에 없는 BG01 이식 잔재라 기본 제거(false)=공장영점 유지 | true=기존동작(매 사이클 tare) 복원 | 적용 전 센서 setzfac 1회 선행 필수 (R2, 2026-07-20)"
```

- 기본값이 false라 키가 없어도 동작은 동일 — **상태 문서화 목적의 명시**
- CRLF/BOM 보존 확인, 9파일 전부 json parse OK

## 2. 검증

- `dotnet build ff_vibetilt.csproj -c Debug` — **오류 0** (경고 4는 기존)
- JSON 9파일 파싱 전부 OK
- 실기 검증은 미실시 (아래 남은 작업)

## 3. 운용 절차 (배포 전 필수)

```
1. 빌드 (R1b1 검증본)
2. 센서에 setzfac 1회  ← 필수. 안 하면 마지막 tare(τ≈0.09)가 영구 동결됨
   - 이때 X결합 τ 측정 겸행 권장: #3 완주 → r1 판독(5~6회) → setzfac → r2 판독 → τx = r2 − r1
3. 검사 시작 — 이후 FineAdjust가 공장영점 기준으로 매 사이클 수평 확립
```

## 4. 남은 작업

- [ ] 실기 회귀: TEST 워크스페이스로 ±20/±45 실측 (목표 |error| ≤ 0.05, spec ±0.2 여유 확인)
- [ ] X결합 τ 측정 (위 3-2 겸행) → 13p 회의자료 실측 근거 업데이트
- [ ] 회의(레퍼런스 영점 유지/제거 재논의) 결과 반영 — 유지 결정 시 JSON true 한 줄로 복원
- [ ] 검증·확정 후 R1 승격 → 릴리즈: 배포폴더(`진동기울기검사_YYMMDD_R1_ff_vl`) + 제조팀 문서 (베타 R1b1은 양산 미배포)
- [ ] 커밋 (사용자)
