# ShipLogClearTest (TITEMID=320)

## 목적

출하 전 장비 내부 로그가 정상적으로 삭제되는지 확인한다.

## 기준
*  모든 명령과 검증을 workspace.json 기반으로만 수행해야 합니다.
- C# 코드에는 exit, #console, config, clrlog 등 어떤 명령도 하드코딩하지 않습니다.
* 엑셀 spec + MFC 로그를 기준 동작으로 한다.
* 실제 검사 로직과 판정 기준은 workspace 정의를 절대 기준으로 한다.
* 코드 내 하드코딩된 문자열/절차는 허용하지 않는다.




## 절차 (기준 동작)
* 실제 실행 순서 및 명령은 workspace에 정의된 순서를 따른다.
1. `exit` 명령 전송
2. `#console` 프롬프트(`>`) 진입 확인
3. `clrlog` 명령 전송
4. 응답 문자열 수신

> ※ 프롬프트(`>`)만 수신된 경우 즉시 종료하지 않고,
> 실제 응답 문자열 수신을 우선한다.

## 판정 기준

* workspace에 정의된 tests[].expected_response 문자열이
* 수신 로그 전체(collected)에 포함되면 PASS
* 아래 중 하나라도 해당 시 FAIL
* expected_response 미포함
* 타임아웃 내 유효 응답 미수신
* workspace에 tests 정의 누락

## 구현 제약
* 판정 로직은 workspace 기반 동적 판정만 허용
* 문자열 비교는 **Contains 방식만 사용**
* 숫자 파싱, 구조화된 응답 처리 금지
* 기존 UART 통신 API 사용
* 기존 클래스 구조 및 아키텍처 변경 금지

## 로그 규칙

* 수신된 응답 문자열을 **가공 없이 그대로 로그 출력**
* PASS / FAIL 결과를 명확히 로그로 남길 것
* FAIL 시 수신된 전체 로그를 함께 출력

## 비고

* 본 항목은 **workspace 기반 자동 검사**를 전제로 한다.
* `ShipLogClearTest`는 `pim` 라이브러리에서 구현한다.

## TODO

* 실패 시 재시도 여부는 실제 양산 로그 분석 후 결정
