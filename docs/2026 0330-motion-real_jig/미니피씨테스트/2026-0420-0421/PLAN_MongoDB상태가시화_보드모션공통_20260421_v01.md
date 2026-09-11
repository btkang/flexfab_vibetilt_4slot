# PLAN: MongoDB 업로드 상태 가시화 (보드/모션 공통)

> **Status:** TODO (deferred) — 2026-04-21 작성, 나중에 구현 예정.
> **범위:** 보드/모션 공통 (양쪽 검사 결과 저장 플로우에 동일하게 적용).
> **우선순위:** 낮음 (MongoDB가 옵션 기능이며 로컬 JSON 저장으로 데이터 유실 없음).

---

## Context (배경 / 목적)

현재 MongoDB 업로드는 **옵션 기능**(`config.ini` → `MongoDBUpload=True/False`)으로 이미 구현되어 있으나, 작업자가 다음 정보를 확인할 방법이 없음:

1. **프로그램 실행 시** — MongoDB가 활성/비활성인지, 활성이면 실제로 DB 서버에 연결되는지
2. **검사 완료 시** — 이번 검사 결과가 DB에 업로드되었는지 / 실패했는지 / 생략되었는지

현재 동작:
- 활성 + 연결 실패 → 4초 timeout 후 긴 스택트레이스가 로그에 출력 (가독성 저하)
- 비활성 → "MongoDBUpload=False → 로컬 JSON만 저장합니다." 한 줄만 출력
- 로딩 시점에는 아무 표시 없음

**목표:** 작업자가 로그 창 한 번만 봐도 DB 상태와 검사별 업로드 결과를 바로 알 수 있도록, 로딩 시점과 검사 완료 시점에 **명확한 한 줄 로그**를 노출.

---

## 변경 사양

### 1. 프로그램 시작 시 로그 추가 (MainForm_Load)
`FlexfabForm.cs:258` (현재 `log_level / test_mode / repeat` 로그) **바로 뒤에** MongoDB 상태 로그 추가.

**출력 패턴:**
```
# MongoDBUpload=False 일 때
[INFO] MongoDB 업로드: 비활성 (로컬 저장 전용)

# MongoDBUpload=True + 연결 성공
[INFO] MongoDB 업로드: 활성 | 연결 확인 중...
[INFO] MongoDB 연결: OK (192.168.10.10:45029)

# MongoDBUpload=True + 연결 실패
[INFO] MongoDB 업로드: 활성 | 연결 확인 중...
[WARN] MongoDB 연결 실패 — 검사 시 재시도됩니다 (TimeoutException)
```

### 2. 검사 완료 후 한 줄 요약 (업로드 결과)
**보드/모션 양쪽 공통**으로 업로드 시도 직후 아래 한 줄 출력 (기존 상세 로그는 유지 — 디버깅용).

**출력 패턴:**
```
# 성공
DB 업로드: OK

# 실패 (True인데 연결/업로드 실패)
DB 업로드: 실패 (로컬 저장됨)

# 생략 (False)
DB 업로드: 생략 (MongoDBUpload=False)
```

### 3. 새 헬퍼 메서드 `PingMongoConnection`
startup 체크 시 업로드 없이 ping만 시도하는 전용 메서드 추가.
기존 `TryUploadToMongo:1498`의 ping 로직(`client.GetDatabase("admin").RunCommand("ping")`)을 재사용.

---

## 수정 대상 파일

**1 파일만 수정**: `flexfab_mini/FlexfabForm.cs`

### 수정 위치별 상세

| # | 위치 | 변경 내용 |
|---|---|---|
| A | `FlexfabForm.cs:258` 다음 줄 | Startup MongoDB 상태 + ping 로그 블록 추가 (약 15줄) |
| B | `FlexfabForm.cs:1291~1301` | 보드검사 저장 분기 — 업로드 결과 한 줄 요약 추가 |
| C | `FlexfabForm.cs:1479~1480` | 모션검사 저장 분기 — `wantMongo` 체크 추가 + 한 줄 요약 추가 (현재 무조건 업로드 시도하는 버그 동시 수정) |
| D | `FlexfabForm.cs:1540` 이후 | 새 메서드 `PingMongoConnection(out string msg)` 추가 |

---

## 재사용 구성요소

| 구성요소 | 위치 | 용도 |
|---|---|---|
| `Log(string)` | `FlexfabForm.cs:1120` | 모든 상태 로그 출력 (타임스탬프 자동 부착) |
| `ReadBoolFromIni("General", "MongoDBUpload", ...)` | 기존 존재 | 플래그 읽기 — 그대로 재사용 |
| `workspace.mongodb_uri` | `TryUploadToMongo:1489` | URI 소스 — ping 헬퍼도 동일 경로 사용 |
| ping 로직 | `TryUploadToMongo:1498` | `PingMongoConnection`으로 추출 |
| `LogConfig.IsTest` | 기존 | 상세 로그 gating (유지) |

---

## 동작 시나리오 검증

### 케이스 1: 제조팀 (MongoDBUpload=False)
- 시작: `[INFO] MongoDB 업로드: 비활성 (로컬 저장 전용)` 1줄
- 검사 후: `DB 업로드: 생략 (MongoDBUpload=False)` 1줄
- **4초 timeout 없음**, 스택트레이스 없음

### 케이스 2: 개발/QA (MongoDBUpload=True, DB 연결됨)
- 시작: `활성 | 연결 확인 중...` → `MongoDB 연결: OK (...)`
- 검사 후: `DB 업로드: OK` + (기존 상세 `MongoDB 업로드 성공: ctsm.product _id=...` 유지)

### 케이스 3: 미니PC 현장 (MongoDBUpload=True, DB 미연결)
- 시작: `활성 | 연결 확인 중...` → `[WARN] MongoDB 연결 실패 — 검사 시 재시도됩니다`
- 검사 후: `DB 업로드: 실패 (로컬 저장됨)` + (기존 상세 로그 유지)
- ※ 여전히 매 검사마다 4초 timeout 발생 (이건 원래 옵션=True의 설계상 의도된 동작)

---

## 빌드/배포 대응

- `output/Debug/net8.0-windows/` + `output/Release/net8.0-windows/` **양쪽 workspace JSON에는 변경 없음** (코드만 수정)
- 빌드 후 미니PC 배포 시 `config.ini`는 기존 파일 유지 (자동 생성 시 기본값 `MongoDBUpload=True`)

---

## Verification (검증 방법)

### 로컬 Debug 환경
1. **케이스 1 재현:** `config.ini` 편집 → `MongoDBUpload=False` → 프로그램 재시작
   - 로딩 로그에 "비활성 (로컬 저장 전용)" 1줄 출력 확인
   - 보드검사 1회 실행 → "DB 업로드: 생략" 1줄 출력 확인
   - 4초 지연 없는지 체감 확인

2. **케이스 3 재현:** `MongoDBUpload=True` + DB 서버 미접속
   - 로딩 로그에 "연결 확인 중..." → "MongoDB 연결 실패" WARN 확인
   - 보드검사 1회 실행 → "DB 업로드: 실패 (로컬 저장됨)" 확인
   - 로컬 JSON(`Result/.../00001.json`) 정상 저장 확인

3. **케이스 2 재현 (선택):** 개발 DB 있으면 `mongodb_uri` 임시 변경 후 "OK" 경로 확인

### 회귀 방지 체크
- 기존 `MongoDB 업로드 성공/실패` 상세 로그가 여전히 출력되는지 (`LogConfig.IsTest` 조건)
- 모션검사 결과 저장 시 `MongoDBUpload=False`에도 로컬 JSON 정상 저장되는지 (버그 수정 효과)
- 보드검사 JSON(`Result/.../YYYY-MM-DD/00001.json`) 내용 변경 없는지 (코드 동작에만 영향)

---

## 관련 커밋/메모리

- 미니PC 테스트 리포트: `docs/2026 0330-motion-real_jig/미니피씨테스트/2026-0420/REPORT_미니PC보드검사결과_20260420_v01.md`
- MongoDB 관련 기존 동작은 `f6ceaab` (FlexfabForm 버그 수정) 커밋 참고
- 현재 상태: MongoDB 옵션 기능(제조팀 기준 사용 안 함), 로컬 JSON 저장으로 데이터 유실 없음
