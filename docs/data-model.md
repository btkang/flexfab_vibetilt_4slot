# flexfab VibeTilt 4-Slot Data Model

- Baseline Mode: observed
- Baseline Seed: .stage-pilot/bootstrap/baseline.yaml
- Status: draft
- Owner: (미정 — 사용자 결정)
- Last Updated (KST): 2026-09-30 10:55
- Source Discovery / Batch: bootstrap-baseline (2026-09-30, 저장소 관찰)

## Project Summary
- 진동기울기센서(VL10 RS-232 / VL20 RS-485) 양산 검사 프로그램 — 모션 지그 4슬롯 대응 flexfab 기반 데스크톱 앱

## Primary Domain
- 제조 양산 검사 (센서 보드 EOL 테스트, 모션 지그)

## Primary Persistence Backend
- 로컬 파일(JSON·CSV·텍스트) + MongoDB `ctsm.product`(선택, config.ini `MongoDBUpload`)

## Purpose
- This document captures the approved cross-cutting data model baseline for the project.
- It is a shared reference for REQ drafting, batch design, implementation, verification, migration planning, and handoff.

## Scope
- Covers core entities, relationships, states, persistence mappings, and shared consistency rules.
- Does not duplicate full functional acceptance criteria already owned by REQ documents.
- Does not replace ORM definitions, migration files, or generated schema artifacts.

## Model Summary
- 완성 결과 JSON :: X·Y 두 단계를 통과한 보드 1대의 검사 결과
- 대기 기록(pending) :: X 통과 후 Y를 기다리는 레인별 보드
- FAIL 결과 JSON :: 단계별 FAIL 보드 기록 (로컬만)
- 성적서 / 검사이력 :: 일자별 집계 (완성 보드 1열 / 시리얼 슬롯 1줄)
- 시리얼 채번 파일 :: 다음 번호 기본값·롤백 기준

## Entity: `완성 결과 JSON`
- Purpose: X·Y 두 단계를 모두 통과한 보드 1대의 검사 결과
- Source of Truth: `Result/{프로젝트}/{yyyy-MM-dd}/{sn}.json` (+ MongoDB `ctsm.product`)
- Lifecycle: Y 완성 시 생성 -> 같은 날 같은 시리얼 재완성 시 기존은 `PASS_DUPLICATE/{sn}_{시각}.json`으로 이관

### Key Fields
- `sn` :: string :: 보드 시리얼 (`VL1-`/`VL2-` + 5자리 이상, 최대 11자)
- `slot` :: string :: 레인 표기 `X1슬롯->Y1슬롯`
- `pass_fail` :: string :: `PASS` (4슬롯에서만 추가)
- `result` :: array :: 항목별 retmsg — X 단계(pending에서) + Y 단계
- `ver` :: string :: 검사 모듈 버전(`MODULE_VERSION`)
- `time` :: datetime :: 저장 시각(UTC)

### Relationships
- 대기 기록 :: Y 완성 시 같은 레인·같은 시리얼 pending을 소비해 X retmsg를 합침
- 성적서·검사이력 :: 완성 시 성적서 1열, 이력 1줄

### State Rules
- 레인 X 통과 + 같은 시리얼 Y 통과(양성 증거)일 때만 생성
- 저장 중 예외면 이번에 생긴 반쪽 파일만 삭제, 슬롯 FAIL(저장 실패)

## Entity: `대기 기록 (pending, 레인별)`
- Purpose: X 단계를 통과하고 Y 단계를 기다리는 보드
- Source of Truth: 실행 폴더 `pending_{워크스페이스파일명}_x{1|2}.json`
- Lifecycle: X 통과 시 생성 -> 사이클 시작 시 `.inuse` 잠금 -> Y 완성·폐기 시 삭제 / 공통 FAIL·중단 시 복원 / 무효는 `.invalid_*`, 잔존 `.inuse`는 `.stale_*`, 구 형식은 `.old_*`

### Key Fields
- `serial` :: string :: 보드 시리얼
- `slot` :: string :: X 슬롯 표기
- `ws` :: string :: 워크스페이스 파일명 (다른 워크스페이스 기록 차단)
- `x_result` :: string :: `PASS`
- `time` :: string :: `yyyy-MM-dd HH:mm:ss` (`pending_max_age_h` 기본 24h)
- `retmsg` :: array :: X 단계 결과

### Relationships
- 완성 결과 JSON :: Y 완성 시 소비
- 시리얼 입력 창 :: 유효 pending 시리얼이 Y 기본값

### State Rules
- 유효 = 파일·파싱·`ws` 일치·`x_result` PASS·유효기간 이내·`serial` 있음
- 같은 보드 재투입·레인 교차면 무효
- Y 칸이 비어 있는데 유효 pending이 있으면 경고 후 폐기(작업자 확인)
- 알려진 제한: 미래 `time`(시계 역행)은 무기한 유효

## Shared Consistency Rules
- 시리얼 채번(`serial_*.txt`, `last_serial_x*_*.txt`)은 번호를 실제 소모한 사이클만 롤백 (D2)
- pending 쓰기는 원자적(tmp → Replace/Move)
- 검사이력 CSV는 헤더가 다르면 `_NN` 분할, 잠긴 파일은 덮어쓰지 않고 건너뜀
- 성적서는 완성 보드 1대 = 1열, `report_serial_count`(기본 10) 열 롤오버, 재검은 기존 열 갱신

## Persistence / Integration Notes
- FAIL 결과 `{날짜}/FAIL/{sn}_{X|Y}_{시각}.json` — 로컬만(Mongo 업로드 안 함). 저장 실패·공통 FAIL은 만들지 않음
- 검사이력에는 공통 FAIL 중단 사이클도 `FAIL(공통 항목 #n)`으로 기록
- 로그 `Log/{프로젝트}/{yyyy-MM-dd}.log`
- SIL 실행 결과는 프로젝트명 `_SIM` 폴더로 분리, DB 업로드 차단

## Current Gaps / Planned Changes
- K1 FAIL JSON Mongo 업로드 여부 미정
- K2 품질모니터링이 FAIL·PASS_DUPLICATE까지 재귀 수집 → 집계 왜곡 (배포 전 결정)
- 완성 JSON `result`에 공통 항목(#0·#3·#10) retmsg 2회 수록 (D6-1)
- 성적서에 측정 수치 미기재(판정만) (K4)

## Update Triggers
- Update this document when a core entity, key field, or relationship changes.
- Update this document when lifecycle or persistence behavior changes.
- Update this document when a batch introduces a migration, backfill, or breaking data assumption.

## Change Log
- 2026-09-30 bootstrap-baseline 초안 (저장소 관찰, 0.2.2.6 / 메인툴 1.8.1.8 기준)
