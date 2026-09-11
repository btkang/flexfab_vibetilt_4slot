# CHECK: v0.4.0.60408 빌드 검증 결과

- **날짜:** 2026-04-08
- **작성:** Claude
- **버전:** ff_vibetilt.dll v0.4.0.60408 / VibeTilt 0.3.0.60326 / MotionJig 0.1.0.60408

---

## 1. Debug 빌드 (VS2022)

- workspace: `longrun_motion_232_dual.json`
- 시간: 16:05:24 ~ 16:08:06 (**2분 42초**)
- 결과: **13항목 ALL PASS** (OK=13, FAIL=0, SKIP=0)
- X축: 00824 PASS, Y축: 00823 PASS

## 2. Release 빌드 (배포본)

- 실행 경로: `10.실행파일배포\260408_motion_232\0.4_60408-RELEASE-미포함\net8.0-windows\`
- workspace: `longrun_motion_232_dual.json`
- 시간: 16:10:32 ~ 16:13:14 (**2분 42초**)
- 결과: **13항목 ALL PASS** (OK=13, FAIL=0, SKIP=0)
- X축: 000032 PASS, Y축: 000031 PASS

## 3. 검증 요약

| 빌드 | 결과 | 소요시간 | 비고 |
|------|------|---------|------|
| Debug (VS2022) | 13항목 ALL PASS | 2분 42초 | |
| Release (배포본) | 13항목 ALL PASS | 2분 42초 | 배포본 정상 동작 확인 |

- Debug/Release 소요시간 동일
- 모든 측정값 SPEC 범위 내

## 4. 485 테스트

- 상태: **보류** (하드웨어 통신 이슈)
- 상세: `NOTE_485_통신이슈_20260408.md` 참조
- 하드웨어 담당자에게 확인 요청 완료
