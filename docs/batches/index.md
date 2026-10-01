# Batch Index

## 목적
- 이 문서는 batch delivery 단위의 전역 인덱스다.
- 각 batch는 `docs/batches/<BAT_ID>/` 아래에서 profile(`standard` | `batch-lite`)에 맞는 산출물을 관리한다.

## Batch Register

| BAT ID | Profile | Status | Included REQ | Discovery | Folder |
| --- | --- | --- | --- | --- | --- |
| bat-001 | standard | release-candidate | REQ-001, REQ-002 | dcy-001 | [bat-001_20260930_fail-archive-path](./bat-001_20260930_fail-archive-path/index.md) |
| bat-002 | batch-lite | release-candidate | REQ-003 | dcy-003 | [bat-002_20261001_log-line-limit](./bat-002_20261001_log-line-limit/index.md) |
