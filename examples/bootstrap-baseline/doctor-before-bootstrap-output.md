# Sample Output: doctor before bootstrap-baseline

fresh host 저장소에서 아직 baseline이 초기화되지 않은 경우 doctor는 아래처럼 bootstrap 안내를 함께 출력한다.

```text
WARN [missing-active-docs] .: No active SDLC docs were found under docs/. Workspace-specific checks were skipped.
INFO [bootstrap-required] .: Fresh host repos should run /bootstrap-baseline in Copilot Chat before the first real Discovery. Missing bootstrap files: docs/discovery/index.md, docs/srs/index.md, docs/batches/index.md, docs/releases/index.md, docs/project-structure.md, docs/runtime-flows.md
stagepilot-doctor: 0 error(s), 1 warning(s), 1 info message(s)
```