# PLAN: error_table.json 코드 연동

## 날짜: 2026-04-07
## 상태: 검토 대기

## 목적
- FAIL 발생 시 error_table.json의 에러 정보(message, check)를 로그에 출력
- 양산 작업자가 FAIL 원인과 확인사항을 즉시 파악 가능

## 대상 파일
- `ff_vibetilt/MotionJig.cs` (코드 추가)
- `output/Debug/net8.0-windows/error_table.json` (이미 생성 완료)

## 구현 내용

### 1. 에러 테이블 로딩 (정적 캐시)
```csharp
private static Dictionary<string, JsonElement>? _errorTableCommon;
private static Dictionary<string, JsonElement>? _errorTableProject;
private static bool _errorTableLoaded = false;
```
- 최초 1회 `error_table.json` 로딩 → 정적 필드 캐시
- 파일 없거나 파싱 실패 시 무시 (검사 중단 안 함)

### 2. LogErrorInfo() 메서드
```
private void LogErrorInfo(string errorKey, Action<string> log)
```
- `errorKey`로 common → project 순서로 조회
- 찾으면 로그 출력:
  ```
  [ERROR_TABLE] COMM_TCP: TCP 연결 실패 (tcp_emio)
  [ERROR_TABLE] 확인: 1. LAN 케이블 연결 확인 / 2. EMIO 보드 전원 확인 / ...
  ```

### 3. FAIL 문자열 → 에러코드 매핑

| FAIL 문자열 (Contains) | error_table 키 | 위치 |
|------------------------|---------------|------|
| `tcp_emio not found` | `COMM_TCP` | MotionNewInternal, MotionZeroInternal |
| `uart_gyro not found` | `COMM_GYRO` | MotionNewInternal, MotionZeroInternal |
| `not found` (기타 uart) | `COMM_UART` | MotionNewInternal |
| `과회전 감지` | `OVERROTATION` | MotionNewInternal, MotionZeroInternal |
| `영점 초기화 실패` | (내부에서 상세 로그 이미 출력) | MotionNewInternal |

### 4. 내부 FAIL 로그에 에러코드 추가

InitMotionZero 내부:
| FAIL 로그 | error_table 키 |
|-----------|---------------|
| `[ZERO] FAIL: PMO011 timeout` | `ZERO_HOME_FAIL` |
| `[ZERO] FAIL: 0도 이동 실패` | `ZERO_MOVE_FAIL` |
| `[ZERO] FAIL: 0도 미세조정 실패` | `ZERO_FINE_FAIL` |
| `[ZERO] FAIL: setzcur 실패` | `GYRO_SETZCUR` |

RunTiltMeasure 내부:
| FAIL 로그 | error_table 키 |
|-----------|---------------|
| `[TILT] FAIL: 이동 실패` | `TILT_MOVE_FAIL` |
| `[TILT] FAIL: 미세조정 실패` | `TILT_FINE_FAIL` |
| `[TILT] FAIL: AN 파싱 실패` | `TILT_AN_PARSE_FAIL` |
| `[TILT] ERROR: 센서 차이` | `TILT_SENSOR_DIFF` |
| SPEC NG (anglePass=false) | `TILT_SPEC_OUT` |

RunVibeMeasure 내부:
| FAIL 로그 | error_table 키 |
|-----------|---------------|
| `[VIBE] FAIL: 이동 실패` | `VIBE_MOVE_FAIL` |
| `[VIBE] FAIL: 미세조정 실패` | `VIBE_MOVE_FAIL` |
| GAC timeout (전체 timeout) | `VIBE_GAC_FAIL` |

### 5. 적용 방식
- 각 FAIL 로그 직후에 `LogErrorInfo(errorKey, log)` 1줄 추가
- 기존 로그/로직 변경 없음 (추가만)
- error_table.json 로딩 실패 시 조용히 무시

## 변경 범위
- MotionJig.cs: ~50줄 추가 (로딩 + 조회 + 각 FAIL 지점 1줄씩)
- 기존 로직 변경: 없음
- 새 파일: 없음

## 롤백
- `git checkout 23ac515 -- ff_vibetilt/MotionJig.cs` 로 원복 가능
