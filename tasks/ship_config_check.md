# ShipConfigCheckTest (TITEMID=310)

## 목적
출하설정 초기화 이후,
장비의 현재 설정 출력(`config`)이
**workspace에 정의된 기대 기준과 일치하는지** 확인한다.

* 본 테스트는 **설정 dump 출력의 정상 여부 + 내용 검증**을 목적으로 한다.
* workspace 기반 기대 문자열 검증을 목적으로 한다.
---

## 기준
- ShipConfigCheckTest는 모든 명령과 검증을 workspace.json 기반으로만 수행해야 합니다.
- C# 코드에는 exit, #console, config, clrlog 등 어떤 명령도 하드코딩하지 않습니다.
- 엑셀 spec은 절차 정의용 *최소 기준*으로만 사용한다.
- **검증 항목은 workspace.json에 정의된 값만 사용한다.**
- 값 비교는 **문자열 기반(Contains)** 으로 수행한다.

---

## 절차 (MFC 기준)
1. `exit` 명령 전송  
   - 장비 상태 초기화 목적
2. `#console` 프롬프트 진입 확인
3. `config` 명령 전송
4. 설정 출력 로그 수신 및 누적

> 참고  
> MFC 로그 기준으로 `config` 출력은 **지연 발생 + 다중 라인**이며  
> 출력 종료 시점에 `>` 프롬프트가 다시 등장한다.

---

## 확인 항목 (문자열 포함 여부)
- 검증 문자열은 **workspace.json → tests 배열**에 정의된
  `expected_response` 값을 사용한다.
- 코드 내부에 검증 문자열을 **하드코딩하지 않는다.**
### 예시 (workspace.json)
```json
{
  "tests": [
    { "expected_response": "config_data ver = 1" },
    { "expected_response": "config_stat[0:load ok]" },
    { "expected_response": "baud = 0x9600" },
    { "expected_response": "DEV MODE[0]:SLAVE_ANA" },
    { "expected_response": "[FA=0000-000000]" },
    { "expected_response": "ch=0" },
    { "expected_response": "pwr=3" },
    { "expected_response": "rfbaud=1" },
    { "expected_response": "logic_filter_cnt = 10" },
    { "expected_response": "dbgmsg = 0" },
    { "expected_response": "tx_on_clk = 200" },
    { "expected_response": "rx_ref_lv[1],[0] = 48,51" },
    { "expected_response": "hf_filter = 1" },
    { "expected_response": "hf_sampling = 0" },
    { "expected_response": "[0]HOST_DATA_HW = 0" },
    { "expected_response": "baud=2" },
    { "expected_response": "termination=0" },
    { "expected_response": "E84_Ana_Exception=0" },
    { "expected_response": "Brightness=255" },
    { "expected_response": "mode=0" },
    { "expected_response": "flash break cnt = 0" },
    { "expected_response": "break_ptr = 0x0000" }
  ]
}
```

## 판정 기준
- workspace에 정의된 모든 expected_response 문자열이 수신 로그에 모두 포함되면 PASS
- 하나라도 누락되면 **FAIL**

---

## 구현 제약
- 숫자 파싱, 구조화된 비교 금지
- 문자열 `Contains` 방식만 사용
- 기존 통신 흐름 및 로그 출력 구조 변경 금지
- workspace 기반 테스트 정의를 우선 사용
---

## 수신 로직 주의사항 (중요)
- `>` 프롬프트는 **즉시 종료 조건으로 사용하지 않는다**
- 설정 본문을 **최소 1라인 이상 수신한 이후**에만
  `>`를 출력 종료 조건으로 인정한다
- 프롬프트만 단독 수신된 경우에는
  timeout 또는 빈 응답 조건까지 계속 대기한다

> 이는 MFC 기준 로그에서 확인된 실제 장비 동작을 반영한 것이다.

---

## 로그 규칙
- 수신된 `config` 전체 로그를 **원문 그대로 출력**
- 누락 항목 발생 시
[ConfigCheck] 누락 항목: <expected_response> 형식으로 로그 출력
---

## 참고 자료
- 기준 로그: `docs/log_20260130_140558.txt`
- 엑셀 spec:  
  `E84A 보드 검사용 출하검사 docs (v1.31).xlsx`

---

## 결론
- ShipConfigCheckTest는 workspace 기반 테스트 정의를 사용하는 검사 항목
- MFC 로그는 동작 기준, workspace는 검증 기준의 단일 진실 소스(Single Source of Truth) 로 사용한다.