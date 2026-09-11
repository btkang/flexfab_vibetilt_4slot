# flexfab VibeTilt 작업 매뉴얼
> 매 작업 세션 시작 시 참고하는 기준 문서입니다.
> 규칙이 바뀌면 이 문서와 `CLAUDE.md`를 함께 업데이트하세요.

---

## 1. 프로젝트 개요

- **목적**: VibeTilt 보드의 기능 검사 자동화 (보드레벨 + 모션지그)
- **언어/플랫폼**: C# .NET 8, Windows Forms
- **소스 루트**: `flexfab_VibeTilt/`
- **브랜치 전략**: 기능별 브랜치 → main 병합

---

## 2. 작업 사이클 (매 기능/태스크 반복)

```
[1] 사용자 아이디어/요구사항 메모
        ↓  NOTE_ 파일 작성 (사용자 직접)
[2] Claude가 계획서 작성
        ↓  PLAN_ 파일 생성
[3] Claude가 코드 구현
        ↓  소스 파일 수정/생성
[4] Claude가 코드 구현 문서 작성
        ↓  CODE_ 파일 생성
[5] 사용자 확인 → 커밋 (사용자 직접 또는 명시 요청 시 Claude)
[6] 사양/참조 문서 정리
        ↓  REF_ 파일 생성 또는 업데이트
```

---

## 3. 파일 네이밍 규칙

### 접두어 (Prefix)

| 접두어  | 작성자  | 내용                              | 예시                                       |
|---------|---------|-----------------------------------|--------------------------------------------|
| `NOTE_` | 사용자  | 아이디어, 메모, 질문, 요구사항    | `NOTE_motion_검사_20260225.md`             |
| `PLAN_` | Claude  | 작업 전 구현 계획서               | `PLAN_motion_jig_impl_20260224_v01.md`     |
| `CODE_` | Claude  | 코드 구현 완료 후 정리 문서       | `CODE_motion_jig_impl_20260224_v01.md`     |
| `REF_`  | Claude  | 외부 사양/프로토콜 분석 요약      | `REF_EMIO_V2_protocol_summary.md`          |
| `LOG_`  | 사용자  | 실기기 테스트 로그, 실측 결과     | `LOG_bg01_emio_test_20260222.txt`          |
| `CHECK_`| Claude  | 남은 작업 체크리스트              | `CHECK_motion_jig_todo.md`                 |

### 날짜/버전 형식

- 날짜: `YYYYMMDD` (하이픈·공백 없음)
- 버전: `_v01`, `_v02` … 뒤에 배치
- 예: `PLAN_motion_jig_impl_20260224_v01.md`

### 최신본 관리

- 최신 계획: `PLAN_[주제]_latest.md` → 항상 덮어쓰기
- 최신 구현: `CODE_[주제]_latest.md` → 항상 덮어쓰기
- 남은 할일: `CHECK_[주제]_todo.md` → 세션마다 갱신

### 아카이브

- 완료된 이전 버전은 `archive/` 하위로 이동
- 예: `docs/2026 0223- 모션지그/archive/20260224_PLAN_motion_jig_v01.md`

---

## 4. 폴더 구조

```
flexfab_VibeTilt/
├── CLAUDE.md              ← Claude 자동 읽기용 (짧은 규칙 요약)
├── WORK_MANUAL.md         ← 이 파일 (사람이 읽는 상세 매뉴얼)
│
├── docs/
│   ├── 2026 0223- 모션지그/
│   │   ├── NOTE_*.md      ← 사용자 메모
│   │   ├── PLAN_*.md      ← 계획서
│   │   ├── CODE_*.md      ← 코드 구현 정리
│   │   ├── REF_*.md       ← 사양 요약
│   │   ├── LOG_*.txt/md   ← 실측 로그
│   │   └── archive/       ← 구버전
│   ├── 2026 0219 review/
│   └── ...
│
└── ff_vibetilt/           ← C# 소스코드
```

---

## 5. Claude와 협업 규칙

### Claude가 해야 하는 것
- PLAN_ 파일 작성 → 사용자 확인 후 → 코드 구현
- 코드 구현 후 CODE_ 파일 작성
- 사양 분석 시 REF_ 파일 작성
- 세션 종료 전 CHECK_ 파일 갱신 (필요 시)

### Claude가 하면 안 되는 것
- **자동 커밋 금지** — 사용자가 "커밋해줘"라고 할 때만 실행
- NOTE_ 파일(사용자 메모) 수정 금지
- WORK_MANUAL.md / CLAUDE.md 임의 변경 금지

### 세션 시작 시 흐름
1. Claude가 `CLAUDE.md` 자동 읽음 → 규칙 적용
2. 사용자가 오늘 작업 목표 전달 (구두 또는 NOTE_ 파일)
3. Claude가 PLAN_ 작성 → 사용자 확인 → 구현 시작

---

## 6. Git 규칙

- 기능별 브랜치: `feature/기능명` 또는 `step숫자-설명`
- main에 직접 커밋 지양
- 커밋 메시지: `wip 설명` (작업 중) / `feat: 설명` (완료)
- **커밋은 사용자가 명시할 때만 실행**

---

## 7. 기존 파일 → 새 네이밍 매핑 (참고용)

| 기존 파일명 | 새 이름 |
|-------------|---------|
| `motion 검사.md` | `NOTE_motion_검사_20260225.md` |
| `모션지그_검사이식_Plan_2026_0224_01.md` | `PLAN_motion_jig_impl_20260224_v01.md` |
| `모션지그_검사이식_Code_생성_20260224_01.md` | `CODE_motion_jig_impl_20260224_v01.md` |
| `EMIO_V2_Protocol_Summary.md` | `REF_EMIO_V2_protocol_summary.md` |
| `보드레벨_json공용화_모션확장_Plan_20260224_06.md` | `PLAN_boardlevel_json_motion_20260224_v06.md` |
| `보드레벨_json공용화_모션확장_Code_구현_20260224_07.md` | `CODE_boardlevel_json_motion_20260224_v07.md` |

> 실제 이름 변경은 필요할 때 진행, 이 표는 참조용입니다.

---

_최종 수정: 2026-02-25_
