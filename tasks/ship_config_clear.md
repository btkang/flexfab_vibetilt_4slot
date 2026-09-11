# ShipConfigClearTest (TITEMID=300)

## 목적
출하 전 장비의 설정(Config)을 초기화(clear)하고,
정상적으로 저장 및 재부팅이 수행되는지 확인한다.

---

## 기준
- 모든 명령과 검증을 workspace.json 기반으로만 수행해야 합니다.
- C# 코드에는 exit, #console, config, clrlog 등 어떤 명령도 하드코딩하지 않습니다.
- MFC 구현 및 기준 로그를 따른다.
- **명령 순서 및 대기 조건은 workspace.json에 정의된 절차를 기준으로 한다.**
- C# 코드에서는 절차를 하드코딩하지 않고 workspace의 정의를 실행한다.

---

## 절차 정의 방식 (Workspace 기반)

ShipConfigClearTest의 절차는 workspace.json의 `param.steps` 항목으로 정의한다.

각 step은 다음 중 하나의 형태를 가진다.

- `cmd` : 전송할 명령 문자열
- `wait` : 응답 대기 조건 (`prompt`)
- `expect` : 응답 문자열 포함 여부 검사
- `sleep` : 지정 시간(ms) 대기

C# 구현에서는 steps를 순차적으로 실행하며,
각 단계 실패 시 테스트를 FAIL 처리한다.

---

## 절차 (MFC 기준 → Workspace 표현)

### MFC 기준 절차
1. `exit`
2. `#console` 진입 확인
3. `config clear`
4. `save` → `"Save Success"` 확인
5. `reset`
6. 재부팅 대기
7. `#console` 재진입 확인

### Workspace.json 예시


"param": {
  "port": "uart_e84a",
  "TIMEOUT": 6000,
  "bootWaitMs": 2000,
  "steps": [
    { "cmd": "exit", "wait": "prompt" },
    { "cmd": "#console", "wait": "prompt" },
    { "cmd": "config clear", "wait": "prompt" },
    { "sleep": 500 },
    { "cmd": "save", "expect": "Save Success" },
    { "cmd": "reset", "wait": "prompt" },
    { "sleep": 2000 },
    { "cmd": "#console", "wait": "prompt" }
  ]
}


## 판정 기준
- 모든 step이 정상적으로 수행되면 PASS

- 다음 중 하나라도 발생 시 FAIL

- 명령 전송 실패

- wait / expect 조건 불만족

- TIMEOUT 발생

## 구현 제약
- 통신 API(SendCmd, WaitForPrompt, WaitForContains)는 기존 구현을 사용한다.

- 절차 제어 로직은 C#에 유지한다.

- 명령 문자열 및 순서는 하드코딩하지 않는다.

- 구조/아키텍처 변경 금지.

## 로그 규칙
- 각 step의 송신/수신 로그를 그대로 출력한다.

- FAIL 발생 시 실패한 step과 사유를 명확히 로그로 남긴다.

## TODO
- reset 이후 대기 시간(bootWaitMs)은 기준 로그와 비교하여 조정 가능하다.

- 장비 펌웨어 변경 시 workspace의 steps만 수정하면 된다.
