# flexfab_mini CHANGELOG

## 2026-10-01 — 0.2.4.8(261001) / 메인툴 1.8.3.10: 화면 로그창 줄 수 상한 (stage-pilot bat-002) — **전역**
- 근거: `docs/discovery/dcy-003_…` → `docs/srs/Non-Functional/req-003` → `docs/batches/bat-002_20261001_log-line-limit/`. 레드마인 #4901 (밤샘 SIL 2차 메모리)
- **FlexfabForm.cs** — `TrimLogLines()` 신설, `AddLogMessage`에서 추가 후 호출. 화면 로그가 상한을 넘으면 오래된 줄을 상한의 10%만큼 일괄 삭제
  - 사유: `listBox_Log`가 지우지 않고 누적 → 밤샘 232 203사이클 79만 줄, Private 37→296MB (사이클당 ~1.3MB)
- **FlexfabForm.Slot4.cs** — `ApplySlotLayout`에서 워크스페이스 `log_max_lines` 읽기 (없으면 20000, 0=무제한). 워크스페이스 JSON 무변경
- 영향: 화면 표시만. 파일 로그(`Log/{proj}/날짜.log`)는 전부 유지. "전체 로그 복사"는 화면에 남은 줄만
- 하위호환: 키 없으면 기본 20000 — 2슬롯 워크스페이스도 같은 동작(안정성 수정, 전역)

## 2026-09-30 — 0.2.3.7(260930) / 메인툴 1.8.2.9: FAIL·재완성 기록 보관 경로 분리 (stage-pilot bat-001)
- 근거: stage-pilot `docs/discovery/dcy-001_…`(K1·K2) → `docs/srs/Data/req-001`, `Documentation/req-002` → `docs/batches/bat-001_20260930_fail-archive-path/`. 레드마인 #4900
- **FlexfabForm.Report.cs** — `ArchiveRootDir()` = `Result_보관/{프로젝트}` 신설. FAIL 결과·재완성 이전 결과(PASS_DUPLICATE)를 품질모니터링 수집 경로(`Result/`) 밖으로
  - 사유: 품질모니터링이 상위 `Result/`를 재귀 수집 → FAIL·중복이 섞여 시리얼 판정·수율 왜곡(K2)
  - 이력 CSV `결과파일` 열: FAIL은 작업 폴더 기준 `Result_보관/…`, 완성은 기존대로
  - 우클릭 "FAIL 결과 폴더 열기 (오늘)" → 새 경로
- K1: FAIL 결과는 DB에 올리지 않음(현행 유지) — 기준 문서에 명시
- 2슬롯 경로 무변경 (FAIL·PASS_DUPLICATE는 4슬롯 전용)
- 검증: 빌드 0, 단위 106(105+건너뜀1, 기대 경로 갱신), SIL 5사이클 CHECK OK, 품질모니터링(무수정) 파서로 시리얼 4개 PASS 확인

## 2026-09-30 — 0.2.2.6(260930) / 메인툴 1.8.1.8: SIL 테스트 결정사항 Q1·M5·Q2·Q3 (**미커밋**)
- 근거: `02_분석_날짜/2026 0930-4슬롯_SIL결정사항_수정/PLAN_4슬롯_SIL결정사항_Q1M5Q2Q3_20260930_v01.md`, SIL 결과(레드마인 #4901). 사용자 결정 2026-09-30
- **Q1 — 공통 FAIL 중단 사이클 검사이력 기록** (`FlexfabForm.cs` 중단 경로)
  - 공통 항목 FAIL로 중단(`slot4CommonFailed`)된 경우 시리얼 있는 슬롯마다 이력 1줄, 판정 `FAIL(공통 항목 #0)` (`FirstCommonFailNo`)
  - FAIL 결과 JSON은 만들지 않음(지그·통신 문제 = 보드 불량 아님, 기존 저장 실패 원칙과 동일). 작업자 STOP은 기록 안 함
- **M5 — 작업자 항목 제외** (`FlexfabForm.Slot4.cs`, `FlexfabForm.cs`)
  - `SlotVerdict`: 번호셀 빨강(작업자 제외) 행은 판정 제외 → Skip 때문에 FAIL 나지 않음 (2슬롯과 같은 동작)
  - **쓰기 항목(`UID_`·`RCONF_`·`APPCFG_SAVE_`)은 제외 금지** (사용자 결정 2번): 번호셀 클릭 시 경고 창, 헤더 전체 제외에서도 흰색 유지. 사이클 시작 시 빨강인 쓰기 항목은 자동 해제(`LogSlot4Exclusions`), 판정에서도 제외 안 함(fail-closed)
  - 로그 `[4슬롯] 작업자 제외 항목 (판정 제외): #4` — 제외한 채 완성된 보드 추적
  - 2슬롯 경로(`_slot4` 아님)는 무변경
- **Q2 — STOP 후 버퍼 비우기 강화** (`ff_vibetilt/MotionJig.cs` `VlClear`) — **2슬롯 해당**
  - STOP 직후 호출(기울기·진동 2곳)만 `untilEmpty`: 줄 수 제한 없이 빈 응답 2회 연속까지, 최대 1.5초. 스트림 중 초기 데이터 버리기는 기존(10줄) 유지 — 시간 증가 없음
  - 사유: SIL 밤샘 1/387 — 호스트가 잠깐 멈춘 뒤 +45도 잔여 프레임이 -20도 측정에 섞여 센서 차이 65도 FAIL
  - 참고: `VibeTilt.cs` `ClearCommBuffer`(30줄 제한)도 같은 형태 — 이번 범위 밖, 기록만
- **Q3 — 최소 샘플 수** (`MotionJig.cs` `RunTiltMeasure`·`RunVibeMeasure`) — **2슬롯 해당**
  - 샘플 수 < 최소값이면 `샘플 부족 … — 재측정` 후 재시도, 끝까지 모자라면 FAIL(`TILT_SAMPLE_SHORT`/`VIBE_SAMPLE_SHORT` 로그). 기울기는 센서 차이 검사보다 먼저 확인
  - 최소값 기본 = 판정 tail 수(기울기 10, 진동 5). 워크스페이스 `tilt_judge_min_samples`·`vibe_judge_min_samples`로 조정 가능 — JSON 무변경
  - 사유: SIL 밤샘에서 샘플 2개로 기울기 PASS 1회
- 버전: ff_vibetilt 0.2.1.5 → **0.2.2.6 (260930)** (Directory.Build.props·ff_vibetilt.csproj·MODULE_VERSION), 메인툴 1.8.0.7 → **1.8.1.8 (260930)**
- 시뮬레이터: 고장 주입 `stream_every`(스트림 N개 중 1개만) 추가 — Q3 검증용
- 빌드 오류 0, 단위 테스트 106건(통과 105 / 건너뜀 1)

## 2026-09-29 — 0.2.1.5(260929): 버전 체계 적용, 테스트 항목명 잘림 완화, 타이틀 버전 표시 (**미커밋**)
- 버전: 캔탑스 버전·파일명 표준규칙 R1(2026-09-16) 적용. 4슬롯은 미배포 개발본(0.x). 09-18부터의 작업 단계에 버전을 소급 부여해 이어감(사용자 결정) — 이력표는 `RELEASE_NOTE_ff_vl.txt` 맨 위
  - 0.1.0.1(260918) 4슬롯 기능 → 0.1.1.2(260918) 잘못된 PASS 차단 → 0.1.2.3(260921) D2~D5 → 0.2.0.4(260921) 성적서·이력·FAIL 저장 → **0.2.1.5(260929) 본 항목**
  - 기존 표기 `R1b3 (1.0.0.0)`(2슬롯 분기 시 상속값) 폐기. `Directory.Build.props` Version 0.2.1 / FileVersion 0.2.1.5 / InformationalVersion `0.2.1.5 (260929)`, `ff_vibetilt.csproj` 동일, `MODULE_VERSION`(VibeTilt·MotionJig, 결과 JSON `ver`) `R1b3` → `0.2.1.5`
  - **메인툴(flexfab.exe) 버전 분리**: `flexfab.csproj`에서 별도 지정 → **1.8.0.7 (260929)**. 메인폼 계보(원본 v1.1=1.1.0.0 → pSMC 1.2.0.1~1.6.0.5 → VibeTilt 2슬롯 1.7.0.6) 다음 번호. ff_vibetilt(0.2.1.5)와 별개
  - 이후 규칙 7페이지대로 b=기능 추가, c=버그 수정, d=릴리즈마다 +1. **제조에 최초 배포할 때 1.0.0.0**, 배포 파일명 `_R1.0`(8페이지)
- 사유: 4슬롯은 결과 4열 때문에 항목 열이 235px로 줄어 긴 항목명(`#6 X축 기울기값 측정 (-20도, …)` 등)이 잘림 (사용자 요청 "최대한 보이게")
- **FlexfabForm.Slot4.cs** `ConfigureGridColumnsForSlots` — 4슬롯일 때만 그리드를 비어 있는 왼쪽 여백으로 확장(x 87→12, 폭 542→617, 오른쪽 끝 x=629 고정), 항목 열 235→310, 항목 열 줄바꿈 + 행 높이 자동(넘치는 이름만 2줄). 2슬롯 전환 시 위치·폭·줄바꿈 원복
- 영향: 오른쪽 컨트롤(검사자·SerialNo·Mac·불량보기)·폼 크기 불변. 2슬롯 화면 무변경. 판정·저장 무관
  - 보정: 행 높이 자동 조절이 1줄 행을 기본 높이보다 낮춰 줄간격이 좁아짐 → 최소 행 높이 = 기본 행 높이(`RowTemplate.Height`)로 고정. 2슬롯 전환 시 원복
- **워크스페이스 JSON(4슬롯 8개, Debug/Release)** #13 항목명 단축: `… Z축 진동값 측정 (0도) — CAL/저장 후 배치` → `… Z축 진동값 측정 (0도)`. 배치 이유는 기존 `_comment`("CAL/저장 후 모터 안 움직였으므로 0도 유지")에 있음. 이름은 표시·불량목록·성적서 행 이름에만 쓰이고 판정·결과 JSON 키(id)와 무관. 2슬롯 원본 워크스페이스는 무변경
- **FlexfabForm.cs** 타이틀 바 버전 표시(사용자 요청, 2슬롯 1.0.5.5와 동일 코드): 창 타이틀 `CanTops Flexfab v1.0` → `ff_vibetilt {버전}`(검사 모듈 버전만). 메인툴 버전은 시작 로그 1줄 추가, 워크스페이스는 기존대로 로그

## 2026-09-21 (3) — 4슬롯 검사성적서·검사이력 CSV·FAIL 결과 저장 (pSMC 수평전개, **미커밋 — 사용자 검토 대기**)
- 근거: `02_분석_날짜/2026 0921-4슬롯_결함수정_psmc수평전개/PLAN_4슬롯_성적서_이력CSV_FAIL저장_20260921_v01.md` (상위 PLAN §4 3단계, 사용자 "추천대로 진행")
- 참조: pSMC `flexfab_mini` `SaveLog`·`AppendResultCsv`·`AppendReportCsv`·성적서 설정·`ReportXlsxWriter.cs`
- **호출 범위: 4슬롯 정상 종료(`FinishSlot4`)만.** 2슬롯 경로는 연결하지 않음(2026-09-21 사용자 결정: 2슬롯 기능 추가 안 함). 비정상 종료(STOP·중단)는 기록 안 함(pSMC 동일)
- **신규 `ReportXlsxWriter.cs`** — pSMC 복사. 변경 3곳: 개정이력 0행 하드코딩("2026-08-31 / 신규 성적서 개정")을 `Header.RevNo/RevDate/RevNote` 필드로 분리(기본 빈칸 = 수기 칸), 주석 1곳. 의존성 `System.IO.Compression`만 — **NuGet·csproj 변경 없음**
- **신규 `FlexfabForm.Report.cs`** (partial)
  - C 저장 구조: `PreservePassDuplicate`(같은 날 같은 시리얼 재완성 → 기존 결과 `{날짜}/PASS_DUPLICATE/{sn}_{기존파일시각}.json` 이관), `SaveFailJsonSlot`(`{날짜}/FAIL/{sn}_{X|Y}_{시각}.json`, `pass_fail`·`stage`·`slot`·`fail_reason` 필드, **Mongo 업로드 안 함**), `SafeFileName`(경로 문자 치환)
  - B 검사이력: `AppendHistorySlot4` — `Result/{proj}/이력/이력_{proj}_{yyyyMMdd}.csv`, 시리얼 있는 슬롯마다 1줄(고정 10열 + 그리드 항목 열: OK/FAIL/-/빈칸). 헤더가 달라지거나 형식 인식 불가면 기존 보존 후 `_NN` 분할, 잠긴 파일 읽기 실패 시 기록 건너뜀(덮어쓰기 금지)
  - A 검사성적서: `AppendReportForBoard` — `Result/{proj}/성적서/성적서_{proj}_{yyyyMMdd}_{NN}.csv` + `.xlsx` 인쇄본. **완성 보드 1대 = 1열**, 값은 판정만(`OK`, 판정행 `합`) — 완성 = X·Y 양 단계 `SlotVerdict` 통과가 전제. 하루 1파일·N열(`report_serial_count`, 기본 10) 롤오버·재검은 기존 열 갱신·행 구성 변경 시 보존+각주
  - 성적서 설정: `report_items`(키 없음 = 전체, `[]` = 생성 안 함)·`report_serial_count`, `[성적서 설정]` 다이얼로그(검사 중 차단), 저장은 `UpsertTopLevelJson`으로 값 토큰만 치환/삽입(전체 재직렬화 금지, `_comment` 동반)
  - 순수 로직은 `internal static`으로 분리(단위 테스트)
  - 검사번호 정규식: pSMC `[a-z]?` → `[A-Za-z]?` — VibeTilt `#1-X`가 `#1-`로 잘리던 것을 단위 테스트가 검출
- **FlexfabForm.cs**
  - `FinishSlot4` ②: 완성 저장 직전 `PreservePassDuplicate` → 이관되면 `existedBefore=false`가 되어 저장 도중 예외 시 새 반쪽 파일만 지우고 기존 결과는 보존(L6 부분 해소). 완성 직후 `AppendReportForBoard`, 결과 상대경로 기록
  - `FinishSlot4` ⑤(신설): 판정 확정 후 FAIL 슬롯마다 `SaveFailJsonSlot`(Y FAIL은 같은 시리얼의 pending X retmsg를 앞에 붙임) → `AppendHistorySlot4`
  - `SaveLogDirect`: `slotLane`(4슬롯)일 때만 `pass_fail: "PASS"` 추가. 2슬롯 경로(slotLane 없음) 무변경
  - 결과 라벨 우클릭 메뉴(`passMenu`)에 `AddReportMenuItems` — 성적서 설정 / 성적서·이력·FAIL 폴더 / 최신 성적서(xlsx). **화면 배치 변경 없음**
- **배포 전 결정 필요** (PLAN §1)
  - K1 FAIL JSON Mongo 업로드: 현재 **안 함**. `TryUploadToMongo`가 `InsertOne`이라 양산 DB `ctsm.product`에 불량 문서가 쌓이기 때문
  - K2 **품질 모니터링 영향**: `tools/quality_monitor` `parser.py`·`etl.py`가 `rglob("*.json")` 재귀 수집 → `FAIL/`·`PASS_DUPLICATE/` JSON이 섞여 시리얼 판정·SPC 통계가 바뀐다. 모니터링 쪽 제외 처리 또는 경로 변경을 **배포 전 결정**
  - K4 성적서 측정 수치 미기재(판정만), K7 공통 항목 retmsg 2회 수록(D6-1) 미수정
- **구현 검증(Fable 3개 병렬) 반영 — 13건 수정** (`REVIEW_구현검증_성적서이력FAIL_20260921_v01.md`)
  - **치명** `UpsertTopLevelJson` 배열 정규식이 항목명 안의 `]`([X슬롯])에서 끊겨 [성적서 설정] 2번째 저장부터 **워크스페이스 JSON 파괴**(다음 실행 로드 불가) → 문자열 인식형 정규식 + 저장 전 `JObject.Parse` 검증(실패 시 파일 불변). **pSMC 원본에도 같은 결함**
  - **중대** 성적서 기록을 레인 처리 완료 후(⑤)로 이동 — 잠김 팝업이 레인2 완성 저장·pending 기록보다 먼저 워커를 막던 순서 결함
  - **중대** 4슬롯 완성 JSON 쓰기에 잠김 재시도(`slotLane` 있을 때만, 2슬롯 무변경)
  - 경미: 이관 후 저장 실패 시 기존 PASS 원위치(중복검사 유지), I/O 실패는 FAIL JSON 생략, 공유·잠금 위반만 잠김 팝업, CSV 전체 쓰기 원자적(tmp→교체), 이어쓰기 끝 개행 보정, 판정 행 없는 성적서 거부, 성적서 열 매칭 대소문자 무시, `report_items` 불일치 로그, 이력 분할 상한 제거
  - **결정 필요 12건** 이관(스펙 드리프트 성적서·품질 모니터링 집계·롤백 시리얼 재사용 등) — REVIEW §3
- 단위 테스트: `tools/ff_tests/ReportTests.cs` 신설 — 전체 106건(통과 105 / 건너뜀 1)
- 빌드: `dotnet build flexfab.sln` 오류 0

## 2026-09-21 (2) — D3 보정: `param` 없는 proc 하위호환 + 단위 테스트 도입
- **사유:** 단위 테스트(`tools/ff_tests`)가 D3 커밋(`2e7d580`)의 회귀를 검출. `param` 멤버가 없는 proc는 ExpandoObject 접근 시 예외가 나는데, D3에서 예외를 fail-closed(`SLOT_GROUP_INVALID`)로 바꾸면서 **기존 `common` 동작이 Start 거부로 바뀌었다**. CHANGELOG의 "키 자체가 없으면 기존 동작 유지" 서술과 불일치.
- **FlexfabForm.Slot4.cs** `GetSlotGroup` — `param` 접근 예외는 "키 없음"으로 보고 `common` 반환(기존 동작). 그 외 예외는 여전히 `SLOT_GROUP_INVALID`.
- **영향:** 현재 4슬롯 워크스페이스 4종은 19항목 전부 `param`이 있어 실사용 영향 없었음(잠재 회귀).
- **단위 테스트 신설:** `tools/ff_tests/` (xUnit, **`flexfab.sln` 밖** — 솔루션 구조 불변). 검사 대상 코드는 수정하지 않고 리플렉션으로 internal/private 멤버 호출. 실행 `dotnet test tools/ff_tests`. 52건(통과 51 / 건너뜀 1 = 알려진 제한 "미래 time pending 무기한 유효").
  - `SlotGroupTests` — D3 정규화·허용값·하위호환·`ValidateSlotGroups`·`SlotsForRun` 단계1 규칙
  - `SerialRollbackTests` — D2 마감 사이클 롤백 스킵·정상 사이클 1줄 롤백·중복 호출·기준선 미설정 기존 동작
  - `PendingTests` — `LoadValidPending` 6조건·`pending_max_age_h`·파일명 규칙·원자적 쓰기·요약 문구

## 2026-09-21 — 적대적 검증 지적 결함 수정 D2·D3·D4·D5 (GitLab `vibetilt-4slot`)
- 근거: `02_분석_날짜/2026 0921-4슬롯_결함수정_psmc수평전개/PLAN_4슬롯_결함수정_psmc수평전개_20260921_v01.md` §2, 결함 목록은 같은 폴더 `REVIEW_적대적검증결과_20260921_v01.md`
- 검증 방식: Fable 에이전트 3개 병렬 적대적 검증(명세 정확성 / 잘못된 PASS 공격 / 시뮬 계획 타당성) — 3건 모두 REJECT 판정에서 도출
- 빌드: `dotnet build flexfab.sln` 오류 0

### D2 — 마감 사이클·STOP 시 시리얼 무조건 롤백 (치명, **전역**)
- **사유:** 마감 사이클(X1·X2 빈칸)·STOP은 serial 파일에 append하지 않는데, `RollbackSerialAndMacIfFail`이 "이번 사이클이 번호를 소모했는지" 가드 없이 마지막 줄을 지웠다. 채번이 역행해 **이미 검사한 번호가 다음 사이클 기본값으로 제시**되고(결과 JSON 덮어쓰기), 반복되면 pending이 `같은 보드 재투입`으로 무효화되어 **양품의 X 기록이 파괴**된다. `serial_auto_increment=0`에서도 동일.
- **FlexfabForm.cs** — `CountNonEmptyLines`·`CaptureSerialBaselines` 신설, 필드 `_serialLinesBaseline`·`_macLinesBaseline`(-1 = 미설정). `button_start_Click`의 시리얼 입력 직전에 줄 수 기준선 기록. `RemoveLastLineFromFile(path, log, baseline)` — 기준선보다 줄이 늘지 않았으면 `롤백 스킵: … 이번 사이클은 번호를 소모하지 않았습니다` 로그 후 반환. MAC 파일도 동일.
- **영향:** 4슬롯·2슬롯 **양쪽 경로**(공유 함수). 롤백 호출 10곳 전부에 적용. 번호를 실제로 소모한 사이클의 롤백 동작은 기존과 동일.
- **하위호환:** 깨짐 없음(되돌릴 것이 없을 때 되돌리지 않는 방향). 파일 읽기 실패 시 기준선 -1로 기존 동작 유지.

### D3 — `slot_group` 값 미검증 (중대, 4슬롯 전용)
- **사유:** `GetSlotGroup`이 값을 검증 없이 반환해 오타(`"y"`)·공백(`"Y "`)·미지원 값이면 `SlotsForRun`이 빈 배열을 돌려주고 `RunProcSlot4`가 **common으로 취급** → 1회 실행 결과를 **4칸 모두에 OK로 기록**. 실행되지 않은 3개 슬롯이 PASS 증거를 얻었다. `#9 UID` 같은 쓰기 항목이면 한 보드에만 시리얼이 기록되고 나머지는 미기록 상태로 PASS.
- **FlexfabForm.Slot4.cs** — `SLOT_GROUP_INVALID`(`__invalid`)·`SLOT_GROUPS`(common/X/Y/all) 신설. `GetSlotGroup`은 trim + 대소문자 무시로 **정규화**(`"x"`→`"X"`) 후 허용값이 아니면 `SLOT_GROUP_INVALID` 반환(예외 시에도 동일, fail-closed). `slot_group` 키 자체가 없으면 기존 동작 유지(`skip_coupling` → 없으면 `common`). `ValidateSlotGroups(out msg)` 신설. `SlotVerdict`는 `SLOT_GROUP_INVALID`를 전 슬롯 판정 대상으로 취급.
- **FlexfabForm.cs** — `button_start_Click`에서 4슬롯 실행 전 `ValidateSlotGroups` 호출 → 위반 시 팝업·로그·**Start 거부**(하드웨어 동작 전 차단). `RunProcSlot4`에 2차 방어선(실행 안 하고 4칸 FAIL + 중단).
- **영향:** 워크스페이스 4종(Debug/Release 합 8파일) 모두 정상값이라 **동작 변화 없음**. 2슬롯 경로는 `Slot4Run` 밖이라 무영향.
- **하위호환:** 깨짐 없음. 대소문자 변형(`"x"`)은 기존에 잘못된 PASS를 내던 것이 정상 인식으로 개선.

### D4 — 공통 항목 FAIL 시 `fail_continue` 설정에 따른 비대칭 (중대, 4슬롯 전용)
- **사유:** `#10` 같은 공통 항목이 지그 글리치로 1회 FAIL할 때, `fail_continue` **켜짐**이면 `FinishSlot4`가 양 레인 pending을 폐기하고 **꺼짐**이면 중단 경로에서 복원했다. 설정값 하나로 **양품 2대의 X 기록 보존 여부**가 갈렸다. 공통 항목 FAIL은 지그·통신 문제이지 보드 결함이 아니므로 폐기는 과잉.
- **FlexfabForm.cs** `FinishSlot4` ③④ — `commonFailedEver`면 `.inuse`를 삭제하지 않고 **원래 이름으로 복원**(`공통 항목 FAIL — 대기 보드 기록 복원: … (다음 사이클 Y 재검 가능)`). 새 X를 기록했거나 이미 소비한 레인은 제외. ③의 `소비되지 않은 pending 폐기` 로그도 공통 FAIL일 때는 출력하지 않음.
- **영향:** 슬롯 개별 FAIL 시의 폐기 규칙은 **변경 없음**. `pending_max_age_h`(기본 24h)와 재투입 무효 규칙이 기존대로 방어.

### D5 — Y칸을 비우면 대기 보드 기록이 무음 소멸 (중대, 4슬롯 전용)
- **사유:** 사이클 시작 시 유효 pending은 **Y칸이 비어 있어도** `.inuse`로 잠그고 종료 때 무조건 삭제했다. 같은 레인 X칸을 채우면 새 pending 기록 분기로 빠져 폐기 로그도 남지 않아 **경고 없이 소멸**했다(시료 투입 조합 `X1,X2,Y1,·` / `X1,X2,·,Y2`).
- **FlexfabForm.Slot4.cs** `ShowQuadSerialDialog` — 4칸 중복 검사 직후, 레인별로 **유효 pending 있음 + Y칸 비어 있음**을 검사해 해당하면 예/아니오 확인(`… X 검사 기록이 삭제되어 X 단계부터 다시 검사해야 합니다. 계속하시겠습니까?`). **아니오** → Start 취소(기존 접두사 검증과 동일한 UX). 진행 시 로그 `대기 보드 폐기 예정 (작업자 확인)`.
- **FlexfabForm.cs** `FinishSlot4` ③ — 새 X pending 기록 후에도 옛 대기 보드가 남아 있으면 `대기 보드 폐기 (Y 미투입): {sn}` 로그 출력(무음 제거).
- **영향:** 의도적 폐기(보드 분실·불량)는 확인 후 그대로 진행 가능 — 차단이 아니라 경고.

### 미해결 (본 커밋 범위 밖)
- **D1(치명)** 레인 교차 배치 — 작업자가 X FAIL 보드를 다른 레인 Y에 꽂으면 pending 시리얼 자동 채움 때문에 **불량 보드가 다른 시리얼로 완성 출하**될 수 있다. 소프트웨어만으로 차단 불가 → Y단계 UID 읽기 대조 등 **검사 항목 변경 안건**으로 분리(제조팀·개발팀 협의).
- D6 경미 7건(공통 항목 retmsg 2회 수록, 비숫자 시리얼 우회, 단독실행 `OFFSET_`·`REFTEMP_` 미차단, 2인스턴스 `.inuse` 오폐기, `wsKey` 파일명 한정 등)은 백로그.
- **2슬롯 양산 브랜치(`vibetilt`)에는 본 수정 미반영** — 2026-09-21 사용자 결정(2슬롯 동결). D2는 2슬롯 경로에도 존재하는 결함이므로 배포본 `ff_vl_R1b3`에 남아 있다.

## 2026-09-18 (야간) — 잘못된 PASS 차단 결함 수정 (PLAN v05, GitLab `vibetilt-4slot`)
- 근거: `02_분석_날짜/2026 0918-4슬롯_구현계획/PLAN_4슬롯_잘못된PASS차단_결함수정_20260918_v05.md`, 게이트 기록 `REVIEW_PLAN게이트_20260918_v01.md`
- **전역(2슬롯에도 적용 — 안전·표시 결함):**
  - 검사 중 번호셀·헤더 토글, 모든 그리드 더블클릭, 테스트설정·워크스페이스 재로드·열기 차단(`_isRunning`). 실행 중 항목명 더블클릭 단독실행이 UI 스레드에서 모터·포트를 이중 구동하던 경로 제거.
  - 단독실행: `_isRunning`·Start 비활성, 번호셀 색 복원(1항목만 검사 후 PASS 방지), 이전 `__serial`·`__skip_uarts` 제거, 쓰기 항목(`UID_`·`RCONF_`·`APPCFG_SAVE_`) 차단(fail-closed, active_project 기준).
  - PASS/FAIL 팝업에 결과 문구 표시(9곳, `ShowPassForm(msg)`) — 기존엔 표시 안 되는 인스턴스에 설정돼 안 보였음.
  - Start Task try/finally — 예외여도 `_isRunning` 해제.
- **4슬롯 전용(`slot_layout=4`, 사이클 스냅샷 `slot4`):**
  - 양성 증거 판정 `SlotVerdict`: 슬롯 해당 항목 셀 전부 OK일 때만 PASS(`SlotsForRun` 공용 규칙). 라이브러리 미발견·메서드 없음·정보 오류 = 셀 FAIL + 전 슬롯 FAIL.
  - pending: `pending_{워크스페이스}_x{n}.json`(`ws`·`x_result` 필드), 사이클 시작 시 검증 후 `.inuse` 잠금, 남은 `.inuse` → `.stale_*`, 구 형식 → `.old_*`, 무효 → `.invalid_*`, 재투입·레인 교차 무효, 유효 X 기록 없는 Y 슬롯은 실행 금지(쓰기 항목 보호). 정상 종료: Y 완성 → X 원자적 기록 → `.inuse` 삭제. 비정상 종료: 안전 방향 정리.
  - Y 미완성 = FAIL, 시리얼 팝업 Y 기본값 = 유효 pending 시리얼.
  - 요약 `X통과(대기)`·`완성`·`FAIL(사유)`, 팝업은 슬롯 FAIL 하나라도 있으면 FAIL.
  - `repeat_all_count` 라운드 FAIL 누적(카운트·불량보기 유지), `fail_continue` 사이클 스냅샷.
  - 4슬롯 + single 모드 Start 거부, 테스트설정 `test_mode=1` 적용 거부.
  - #0 통신검사 빈 슬롯 포트 SKIP(`__skip_uarts`, ff_vibetilt MotionJig — 키 없으면 기존 동작).
  - 저장 예외 격리(레인별), 저장 도중 예외 시 반쪽 결과 JSON 삭제.
- 배포 전 4슬롯 지그 마감 사이클 실행 권장(구 pending 자동 `.old`).

## 2026-09-18 — 4슬롯 표시명 "X1슬롯" 통일, 겹침 수정
- **FlexfabForm.Slot4.cs** — 표시명 `SLOT_LABELS = {X1슬롯, X2슬롯, Y1슬롯, Y2슬롯}` 신설(지그 표기와 동일). 그리드 헤더·시리얼 팝업·라벨·로그 접두어·불량보기 슬롯란·요약·결과 JSON `slot`에 사용. 내부 키 `SLOT_NAMES`(X1~Y2)는 `slot_uarts` 매칭·`__slot`용으로 유지.
- **FailInfoForm.cs** — `ExtractCoupling`: `[X슬롯]`/`[Y슬롯]` 태그도 X/Y 그룹으로 인식 (4슬롯 워크스페이스를 single 모드로 쓸 때 기존 X그룹 스킵 로직 호환).
- 결과 4열 겹침: 그리드 폭 542 유지, 열 재배분(행머리 숨김·번호 45·항목 235·슬롯 60×4). `Log 표시/끄기` 버튼 높이 46→26.
- 동반 JSON: 4슬롯 워크스페이스 항목명 태그 `[X결합]/[Y결합]` → `[X슬롯]/[Y슬롯]` (생성기 반영). 2슬롯 JSON 무변경.

## 2026-09-18 — 4슬롯(X1·X2·Y1·Y2) 단계 1: UI 4열 + 슬롯별 순차 실행 (GitLab `vibetilt-4slot`)
- 근거: `02_분석_날짜/2026 0918-4슬롯_구현계획/PLAN_4슬롯_메인폼_병렬측정_구현계획_20260918_v01.md` §3 H1~H14 (H4 병렬은 단계 3, 지금은 순차)
- 게이트: 워크스페이스 최상위 `slot_layout: "4"` + `slot_uarts` + dual 모드일 때만 활성(`Slot4Run`). 2슬롯 워크스페이스는 코드 경로가 그대로라 **기존 동작 무변경**.
- **FlexfabForm.Slot4.cs (신규 partial)** — 4슬롯 전용 헬퍼. 필드(`_slot4`, `_slotSerial[4]`, `_slotUarts`, `_slotFailed[4]`, 레인 pending 2건), `ApplySlotLayout`(slot_layout/slot_uarts/clear_log 읽기+열 구성), `ConfigureGridColumnsForSlots`(결과 열 "결과"→X1 + X2·Y1·Y2 3열 추가, 그리드 폭 542→650, 폼·로그창·버튼 불변), `SetSlotHeaders`(헤더에 슬롯명+시리얼), `UpdateSlotCell`/`MarkSlotCellNA`, `GetSlotGroup`/`SlotsOfGroup`(param.slot_group: common/X/Y/all, 없으면 skip_coupling), `CloneParamForSlot`(uart_id·__serial·__slot 치환, 원본 불변), `ShowQuadSerialDialog`(에디트 4개, X2 기본=X1+1, Y1/Y2=이전 X1/X2 자동, 시작/마감 프리셋, prefix·형식·전부빈값·4칸 상호중복 검사, 자동증가는 X 큰 번호+1 1줄 append), `CollectRetmsgBySlot`, `AddFailRecordSlot`(슬롯당 첫 FAIL, Coupling=슬롯명), `SlotSummaryText`.
- **FlexfabForm.cs**
  - `MainLoadWorkspace` 그리드 구성 시 `ApplySlotLayout()` 호출, `ApplyWorkspaceCachedUI`에서도 재적용(재로드·F2 적용).
  - `UpdateDataGridView`: 4슬롯이면 결과 4칸 모두 갱신(초기화·공통 항목·작업자 제외 Skip). 2슬롯은 Cells[2]만(동일).
  - 결과 셀 더블클릭: 열 2 → 열 ≥2(슬롯 열마다 retmsg), 제목에 `[X1]` 표기.
  - `button_start_Click`: 4슬롯이면 `ShowQuadSerialDialog`, 시리얼 중복 체크를 X1·X2 각각, `clear_log_on_run_start`=1이면 시작 시 화면 로그 Clear(파일 로그는 append 그대로).
  - `MainDoProject`: 결과 셀 초기화를 결과 열 전부로(중복 루프 1개로 정리). 레인별 pending(`pending_x1_result.json`/`pending_x2_result.json`) 로드. 라운드마다 `_slotFailed` 리셋. 빈 축 스킵 블록은 4슬롯에서 우회(슬롯별 처리).
  - 로컬 함수 `RunProcSlot4`: common=1회 실행·4칸 동일(FAIL이면 `slot4CommonFailed`, fail_continue 아니면 중단) / X·Y=슬롯별 param 복제 후 **순차** 호출, 로그 `[X1]` 접두어, 빈 슬롯·FAIL 슬롯은 Skip, FAIL은 그 슬롯만 제외하고 계속 / all(#6/#7)=단계1에서는 X 슬롯 순차(기존 단일보드 측정).
  - 로컬 함수 `FinishSlot4`: 레인별 X pending 저장 → Y+pending 합산 `SaveLogDirect(…, slotLane:"X1->Y1")` → 마감(X 빈칸) 시 레인 pending 삭제. 팝업에 슬롯 요약("X1 PASS · X2 PASS · Y1 PASS · Y2 FAIL"). 시리얼 번호 롤백은 X 슬롯이 하나도 통과 못했을 때만.
  - `SaveLogDirect`: 선택 인자 `slotLane` → 결과 JSON `slot` 필드. `RollbackSerialAndMacIfFail`: X2 기억 파일도 롤백. 테스트 설정: `clear_log_on_run_start` 체크박스.
- 유지: 검사 항목 18개·판정·`_comment`, prefix/중복/자동증가/MaxLength 11, 시작·마감 사이클, test_mode single(4슬롯 워크스페이스라도 single이면 기존 X1/Y1 경로), fail_continue/fail_detail, 불량보기, 로그 파일·색·단축키, F2, 포트 변경(Uart 라이브러리 순회라 4포트 자동 표시), repeat, MongoDB 게이트, 재로드.
- 알려진 제한(단계 1): `#0 통신검사`는 모듈이 X1/Y1 포트만 확인(X2/Y2는 단계 2에서 `__slot_uarts`로 확장). `#6/#7`은 X 슬롯 순차 측정, `#14/#15`는 `judge_from` 무시하고 직접 측정(단계 2에서 동시측정 전환). 한 슬롯만 FAIL 시 시리얼 번호는 진행(재검은 수동 입력).
- 동반 JSON: `MP/TEST_workspace_motion_232_VL10_4slot.json`, `…_485_VL20_4slot.json` (Debug/Release, 생성기 `tools/gen_workspace_4slot.py`). 2슬롯 JSON 무변경.

## 2026-06-12
- **FlexfabForm.cs** — `TryUploadToMongo` 진입부에 공통 게이트 추가: `MongoDBUpload=False`면 **DB 연결 시도 자체를 건너뜀**(즉시 return false, 로컬 JSON만 저장).
  - 사유: 제조팀 DB 미연결(랜선 없음) 환경에서 매 검사마다 연결 타임아웃(serverSelectionTimeoutMS=4000) 4~5초 낭비. 보드검사(`SaveLog`)는 이미 wantMongo 체크가 있었으나 **모션 밀어내기(`SaveLogDirect`)는 체크 누락**으로 설정이 안 먹혔음.
  - 방식: 경로마다 체크하지 않고 **모든 업로드 길목인 `TryUploadToMongo` 한 곳**에 게이트 → 보드/모션/미래 경로 일괄 제어, 누락 재발 방지. `SaveLog`의 기존 체크(라인 1308)는 이중 안전으로 유지.
  - 영향 프로젝트: `SaveLogDirect`(밀어내기 dual) 사용 검사 = 현재 ff_vibetilt 모션. ff_pim/colorimeter/e84a는 본 리포 독립 fork라 직접 영향 없음.
  - 하위호환: 기본값 `True` → 기존 동작 그대로. `MongoDBUpload=False` 설정 PC에서만 DB 스킵. 깨짐 없음.

## 2026-06-11
- **PortDeviceScanner.cs (신규)** — 포트 장치 스캐너 (read-only). 레지스트리로 현재 꽂힌 COM의 칩종류/FTDI 시리얼/USB 위치 조회. 의존성 0(Microsoft.Win32.Registry = WindowsDesktop 프레임워크 내장, .csproj 무변경).
- **FlexfabForm.cs** — `ShowPortChangeDialog` 개선: ①상단에 "현재 꽂힌 포트" 스캔 목록 표시 ②각 입력행에 현재 COM의 장치 정체 즉시 표시(장치관리자 왕복 제거) ③[자동(FTDI)] 버튼 — `config.serial`(FTDI) 기준으로 COM 자동 채움(드리프트/스왑 차단).
  - 기존 −/+·적용·JSON 저장 플로우, `textBoxes` 타입, public API 무변경.
  - 영향 프로젝트: 본 리포 독립 fork (ff_pim/colorimeter/e84a 직접 영향 없음). 변경은 순수 추가(read-only 표시 + 버튼)라 하위호환 깨짐 없음.
  - 동반 JSON: 485 모션 4종(MP/TEST × Debug/Release)에 `serial` 필드 추가 + stale 포트(COM11/12→COM17/4) 교정. `serial`은 uart.cs가 무시(통신 영향 0).
- **FlexfabForm.cs** — [적용] 시 `serial` 자동 동기화: 지정 COM이 FTDI면 `port`와 함께 그 칩의 시리얼도 JSON에 저장. 새 컨버터 교체 시 JSON 손편집 불필요(창에서 COM 선택→적용으로 끝).
  - 가드1: `serial` 필드가 이미 있는 라이브러리만 갱신(485 X/Y로 blast radius 한정, Prolific/타 워크스페이스 자동생성 안 함).
  - 가드2: 적용 시점 재스캔 + 변경 시 `[INFO] serial 갱신: …` 로그(조용한 덮어쓰기 방지). COM 동일·시리얼만 변경된 경우도 저장되도록 changed 트리거.

## 2026-06-10
- **FlexfabForm.cs** — Open Workspace 다이얼로그 기본 필터 `*.json` → `*workspace*.json`
  - 사유: 실행 폴더에 비워크스페이스 json 13개(deps/runtimeconfig/error_table 등) — 워크스페이스 8개만 바로 보이게. 드롭다운에 All JSON/All files 유지.
  - 영향: UI 필터만, 로딩 로직 무변경. 하위호환 깨짐 없음.
- **FlexfabForm.cs** — 시리얼 입력 다이얼로그 `MaxLength 9 → 11` (txtX/txtY 2곳)
  - 사유: 출하검사 Spec(20260602 기능검사 양식 #2 비고) — 시리얼 5자리(00001~99999) 소진 후 6자리(100000~)·7자리(1000000~) 확장 대응. `VL1-` 접두사 4자 + 7자리 = 11자.
  - 채번/증가 로직(PadLeft 방식)은 자연 롤오버(99999→100000)로 이미 지원 — 변경 없음.
  - 영향 프로젝트: VibeTilt (본 리포는 독립 fork — ff_pim/ff_colorimeter/ff_e84a 영향 없음)
  - 하위호환: 깨짐 없음 (입력 상한 확대만)
