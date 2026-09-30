# Bootstrap Baseline Example

이 예시는 fresh host 저장소에 StagePilot을 설치한 뒤 `/bootstrap-baseline`을 실행했을 때 남아야 하는 최소 active baseline 상태를 보여 준다.

## Scenario

1. host 저장소에 StagePilot을 설치했다.
2. 아직 Discovery, REQ, Batch, Release 문서는 하나도 없었다.
3. doctor를 실행하면 `INFO [bootstrap-required]`로 `/bootstrap-baseline`이 필요하다는 안내가 나온다.
4. `/bootstrap-baseline`이 최소 질문 세트를 받고 `.stage-pilot/bootstrap/baseline.yaml`을 만든 뒤 baseline 문서와 active index를 초기화했다.
5. 첫 real Discovery는 아직 시작하지 않았고, 이 예시는 그 직후 상태를 보여 준다.

## Files to inspect

- `doctor-before-bootstrap-output.md`
  - bootstrap 이전 fresh host 저장소에서 기대하는 doctor 출력 예시다.
- `bootstrap-baseline-output.md`
  - `/bootstrap-baseline` 실행 결과 예시다.
- `.stage-pilot/bootstrap/baseline.yaml`
  - bootstrap 질문 답변을 저장한 seed file 예시다.
- `docs/project-structure.md`
  - bootstrap으로 생성된 구조 baseline 초안 예시다.
- `docs/runtime-flows.md`
  - bootstrap으로 생성된 runtime flow baseline 초안 예시다.
- `docs/interface-contract.md`
  - bootstrap으로 생성된 인터페이스 계약 baseline 초안 예시다.
- `docs/data-model.md`
  - bootstrap으로 생성된 데이터 모델 baseline 초안 예시다.
- `docs/discovery/index.md`, `docs/srs/index.md`, `docs/batches/index.md`, `docs/releases/index.md`
  - 첫 real Discovery 전까지 비어 있어도 되는 register 골격이다.

## Validation

이 예시 루트는 doctor로 독립 검증할 수 있다.

```bash
python3 .stage-pilot/tools/stagepilot-doctor.py examples/bootstrap-baseline
```