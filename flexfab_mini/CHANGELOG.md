# flexfab_mini CHANGELOG

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
