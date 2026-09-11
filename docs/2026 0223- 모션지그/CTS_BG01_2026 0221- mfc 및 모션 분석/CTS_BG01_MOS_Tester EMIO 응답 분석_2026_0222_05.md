EMIO 응답 분석

CTS_BG01_MOS_Tester — pcom 지그 화면 로그 기반

실측 EMIO TCP 응답 패턴 확인 및 ff_motion 구현 반영

작성일: 2026-02-22 | Rev. 05

목차

분석 대상 로그

로그 구조 해석

확인된 EMIO 응답 목록

핵심 발견 — pmd5011 패턴

핵심 발견 — 원점복귀 응답 (pmo + pmp)

PPP00 응답 형식 확인

PMS 응답 미확인 — 처리 방법

ff_motion 구현 반영 사항

_04 계획 문서 수정 사항

## 1 분석 대상 로그

pcom 지그 화면 로그/ 폴더에 4개 파일이 존재한다. 이 파일들은 MFC 프로그램(CTS_BG01_MOS_Tester)이 실장비에서 PASS 검사를 수행한 결과 로그다.

| 파일명 | 내용 | 센서 구성 |
| --- | --- | --- |
| x.txt | Target 센서 1개만 있는 구성의 검사 로그 | Target only |
| y.txt | Ref. 센서 포함 구성의 검사 로그 | Ref. 포함 |
| z.txt | U.M 센서 포함 구성의 검사 로그 | U.M 포함 |
| x y z.txt | Target + Master + U.M 전체 센서 구성 | 전체 3채널 |

로그 캡처 시점: 로그는 INIT(PMS 파라미터 설정 + 원점복귀) 완료 직후부터 시작된다. PMS 명령 응답은 이 로그에 없다.

## 2 로그 구조 해석

로그의 각 태그가 의미하는 통신 채널:

| 태그 | 방향 | 대상 | 프로토콜 |
| --- | --- | --- | --- |
| [E.RCV] | 수신 | EMIO 스텝모터 컨트롤러 | TCP/IP (포트 2233) |
| [Snd Target] | 송신 | Target VibeTilt 센서 | RS-485 UART |
| [Rcv Target] | 수신 | Target VibeTilt 센서 | RS-485 UART |
| [Snd Ref.] | 송신 | Reference VibeTilt 센서 | RS-485 UART |
| [Rcv Master] | 수신 | Master VibeTilt 센서 | RS-485 UART |
| [Snd U.M] | 송신 | Under/Main VibeTilt 센서 | RS-485 UART |
| [Snd U.T] | 송신 | Under Test (선형 센서) | RS-485 UART |
| [Rcv U.T] | 수신 | Under Test (선형 센서) | RS-485 UART |

### 로그 예시 해석

// ── EMIO 응답 (TCP) ────────────────────────────────── [E.RCV] > pmo0011 ← 원점복귀 완료 [E.RCV] > pmp0011 ← 원점복귀 후 자동 알림 // ── VibeTilt UART (RS-485) ──────────────────────────── [Snd Target]><MCLOSEC3> ← MCLOSE 명령 송신 [Rcv Target]>[MCLOSE=OK9A][MOPEN=OK56] ← 상태 응답 [Snd Target]><MTEST8D> ← 각도 측정 명령 [Rcv Target]>A=10.43,-0.04,0.00,ACC=0.001,0.180,0.979,T=34.43,GA=10.57,0.00,0.00 // ── EMIO 이동 중 응답 패턴 ─────────────────────────── [E.RCV] > pmd5011 ← 소프트 리밋 오류 (정상 복구됨) [E.RCV] > ppc606138000 ← 오류 후 자동 HW 정보 응답 [E.RCV] > pmd0011 ← 이동 완료 (정상)

## 3 확인된 EMIO 응답 목록

| 응답 | 의미 | 상태 | 출처 |
| --- | --- | --- | --- |
| pmo0011 | 원점복귀(PMO011) 완료 | 확인 | 로그 4개 파일 전체 |
| pmp0011 | 원점복귀 직후 자동 발신 (위치 맵 완료 추정) | 신규 발견 | 로그 4개 파일 전체, pmo0011 바로 다음 |
| pmd0011 | 이동(PMD) 완료 — 정상 | 확인 | 로그 전체에서 반복 수신 |
| pmd5011 | 이동 오류 — 소프트 리밋 충돌 (에러코드 5) | 오류 확인 | 로그 4개 파일 전체 (첫 이동 시) |
| ppc606138000 | PPP00 하드웨어 정보 응답 (형식: ppc60 + 6138000) | 신규 확인 | pmd5011 직후 자동 수신 |
| pms... | PMS 파라미터 설정 응답 | 미확인 | 로그에 없음 (INIT 완료 후 캡처 시작) |
| pml005XXXXX | 위치 읽기(PML011) 응답 | MFC 코드로만 확인 | 로그에 없음 |

## 4 핵심 발견 — pmd5011 패턴

4-1. 관측된 패턴

로그 4개 파일 전부에서 동일한 순서로 발생한다:

PMD 이동 명령 송신

→

pmd5011 (소프트 리밋 오류)

→

ppc606138000 (자동 HW 정보)

→

pmd0011 (다음 이동 완료)

// 로그 원문 (x.txt, y.txt, z.txt, "x y z.txt" 공통) [E.RCV] > pmd5011 ← 소프트 리밋 충돌 [E.RCV] > ppc606138000 ← 컨트롤러가 자동으로 HW 정보 송신 [E.RCV] > pmd0011 ← 그 이후 이동은 정상 // 이후: pmd0011이 반복적으로 수신됨 (정상 동작) [E.RCV] > pmd0011 [E.RCV] > pmd0011 [E.RCV] > pmd0011 ...

### 4-2. 해석

원점복귀 직후 모터가 처음 이동할 때 소프트웨어 리밋에 걸린다. 이때 EMIO 컨트롤러가 pmd5011을 먼저 보내고, 자동으로 HW 정보(ppc606138000)를 추가로 송신한다. MFC 코드는 이 오류를 무시하고 다음 이동을 계속 진행한다.

중요: pmd5011은 최종 실패가 아니다.

운용 중 첫 이동 시 정상적으로 발생하는 패턴으로, MFC에서도 이를 무시하고 계속 진행한다.

ff_motion 구현 시 pmd5011를 받으면 즉시 FAIL 처리하면 안 된다.

### 4-3. ff_motion 처리 방안

// VIBE_MOTION: PMD 명령 후 응답을 별도 스레드로 수신 // pmd5011이 와도 계속 대기 → 최종적으로 pmd0011 수신 여부로 판단 private string EmioRecvUntil(string expected, int timeoutMs) { // 수신 루프: expected 응답이 올 때까지 대기 // pmd5011, ppc60... 등 중간 응답은 로그만 남기고 계속 대기 while (ElapsedMs < timeoutMs) { string resp = EmioRecvOnce(); logAction($"[EMIO] {resp}"); if (resp.Contains(expected)) return resp; // 원하는 응답 도착 // pmd5011, ppc60... → 무시하고 계속 } return ""; // timeout }

## 5 핵심 발견 — 원점복귀 응답 (pmo + pmp)

5-1. 관측된 패턴

원점복귀(PMO011) 명령 후 항상 2개의 응답이 연속 수신된다:

PMO011 송신

→

pmo0011 (원점복귀 완료)

→

pmp0011 (위치 맵 완료 자동 알림)

// 로그 원문 (4개 파일 공통) [E.RCV] > pmo0011 ← PMO011 완료 응답 [E.RCV] > pmp0011 ← 자동 알림 (PMP = Position Map?)

### 5-2. pmp0011 해석

pmp0011은 MFC 소스에서 명시적으로 전송하는 명령의 응답이 아니다. 원점복귀 완료 후 EMIO 컨트롤러가 자동으로 추가 송신하는 메시지다. pmp는 Position Map의 약자로 추정된다.

ff_motion 처리: PMO011 완료 판정은 pmo0011만 확인하면 충분하다. 이후 수신되는 pmp0011은 로그 출력 후 무시한다.

### 5-3. ff_motion 구현 방안

// HOME/INIT proc에서 PMO011 완료 판정 EmioSend("PMO011"); string resp = EmioRecvUntil("pmo0011", HOME_TIMEOUT_MS); // pmp0011이 이어서 오므로 수신 버퍼를 비워두거나 짧게 대기 Thread.Sleep(100); // pmp0011 수신 처리 여유시간 EmioFlushRecv(); // 잔여 응답 버리기 // 이후 다음 명령 송신

## 6 PPP00 응답 형식 확인

6-1. 실측 응답

// PPP00 명령 (HW 정보 요청) 응답 [E.RCV] > ppc606138000

### 6-2. 응답 파싱

| 전체 응답 | 파싱 | 의미 |
| --- | --- | --- |
| ppc606138000 | ppc = 명령 에코 <br> 6 = 채널 번호 <br> 0 = 상태 (0=정상) <br> 6138000 = HW 정보 데이터 | PPP00 HW 정보 정상 응답 |

HWINFO proc 확인 방법: 응답이 ppc로 시작하고 상태 바이트가 '0'이면 성공. HW 데이터 값(6138000)은 버전 정보로, ff_motion에서는 존재 여부만 확인해도 충분하다.

주의: ppc606138000은 PPP00 명령에 대한 응답 외에도, pmd5011 이후 EMIO 컨트롤러가 자동으로 송신하기도 한다. 이 경우는 HWINFO 결과가 아니므로 INIT 완료 확인에 혼용하지 않도록 주의.

## 7 PMS 응답 미확인 — 처리 방법

PMS 명령(모터 파라미터 설정)은 INIT 단계에서 실행된다. 로그는 INIT 완료 후부터 캡처되어 있으므로 PMS 응답은 확인 불가다.

### 7-1. MFC 코드에서 추정한 PMS 응답 패턴

| 명령 | 추정 응답 | 근거 |
| --- | --- | --- |
| PMS041480 | pms0041480 | pms + 004(에코) + 1480 또는 pms + 00 + 41480 |
| PMS0510550 | pms00510550 | EMIO 응답 패턴: 소문자 에코 + 상태 '0' |
| PMO011 | pmo0011 | 로그에서 실측 확인 ✅ |

정확한 PMS 응답 형식은 실기기 1회 연결로 확인 가능.

ff_motion INIT 구현 시 PMS 응답을 수신해서 로그로 출력하면, 첫 실행 시 실제 응답 패턴을 확인할 수 있다.

### 7-2. 실용적인 PMS 응답 처리 전략

엄격 모드 (권장하지 않음)

PMS 명령별로 정확한 응답 확인

응답 불일치 시 FAIL

PMS 응답 형식 확정 전까지 구현 불가

실용 모드 (권장)

PMS 응답이 pms로 시작하면 성공

상태 바이트 '0' 포함 여부만 확인

실기기 1회 연결 후 실제 응답을 로그로 기록하여 추후 보완

// PMS 명령 처리 — 실용 모드 private bool SendPms(string cmd, int timeoutMs) { EmioSend(cmd); string resp = EmioRecvUntil("pms", timeoutMs); logAction($"[EMIO] PMS resp: {resp}"); // "pms"로 시작하고 상태 '0' 포함 → 성공 return resp.StartsWith("pms") && resp.Contains('0'); }

## 8 ff_motion 구현 반영 사항

8-1. _04 계획 대비 추가/변경 사항

| 항목 | _04 계획 | _05 실측 반영 |
| --- | --- | --- |
| PMD 완료 응답 | pmd0011 확인 | 동일. 단, 중간에 pmd5011 + ppc60... 수신 가능 → 무시하고 계속 대기 |
| PMO011 완료 판정 | pmo0011 확인 | pmo0011 확인 후 pmp0011 자동 수신 → 무시 또는 버퍼 플러시 |
| PPP00 응답 형식 | 미확인 | ppc606138000 실측 확인. ppc 시작 → 성공 |
| PMS 응답 형식 | 미확인 | 여전히 미확인. 실용 모드로 처리 (pms 시작 + 상태 0 포함) |
| 수신 버퍼 처리 | 단순 수신 | 자동 알림 메시지 (pmp0011, ppc60...) 대비 → 목표 응답까지 대기하는 EmioRecvUntil() 필요 |

### 8-2. EmioRecvUntil() 설계

// 목표 키워드가 포함된 응답이 올 때까지 수신 반복 // 중간에 오는 pmd5011, pmp0011, ppc60... 등은 로그 출력 후 계속 대기 private string EmioRecvUntil(string expectedKeyword, int timeoutMs) { DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs); while (DateTime.UtcNow < deadline) { string line = ReadOneLine(_emioStream, 500); // 500ms 단위 수신 if (string.IsNullOrEmpty(line)) continue; logAction?.Invoke($"[E.RCV] {line}"); if (line.Contains(expectedKeyword)) return line; // 원하는 응답 도착 // 자동 알림 / 오류 응답 → 무시하고 계속 // pmd5011 → 소프트 리밋 (계속 대기) // pmp0011 → 위치 맵 알림 (무시) // ppc60.. → HW 정보 자동 응답 (무시) } return ""; // timeout }

### 8-3. VIBE_MOTION 흐름 재확인

// 1. EMIO 이동 명령 송신 (응답 대기 없이 즉시 다음 단계) EmioSend("PMD071LL1500"); // 2. 즉시 GAC 폴링 시작 (모터 이동 중) // pmd5011이 먼저 올 수 있으므로, EMIO 수신은 별도 처리 var emioRecvTask = Task.Run(() => EmioRecvUntil("pmd0011", TIMEOUT)); PollGac(uart, TIMEOUT, logAction); // GAC 수집 루프 // 3. 이동 완료 확인 string moveResult = await emioRecvTask; if (!moveResult.Contains("pmd0011")) → FAIL (이동 실패)

참고: VIBE_MOTION에서 EMIO 수신을 별도 Task로 실행하면, 메인 스레드가 GAC 폴링을 진행하는 동안 pmd5011 → ppc60... → pmd0011 수신을 백그라운드에서 처리할 수 있다. 이 방식이 동시성 처리에 가장 적합하다.

## 9 _04 계획 문서 수정 사항

_04 계획 문서(모션검사 계획)에서 "실기기 확인 필요"로 남겨뒀던 항목의 업데이트 결과:

| _04 미확인 항목 | _05 결론 |
| --- | --- |
| PMS 계열 응답 형식 (pms007121xxxx 등) | 여전히 미확인 — 실기기 첫 연결 시 로그로 확인 |
| PPP00(HWINFO) 응답 형식 | 확인 완료 — ppc606138000 (ppc60 + 데이터) |
| pmd4011(이동 오류) 응답 | pmd5011 확인 — 에러코드 4가 아닌 5 (소프트 리밋) |
| 원점복귀 응답 개수 | pmo0011 + pmp0011 — 2개 연속 수신 |
| 위치값 실측 기준 | 여전히 미확인 — 실기기에서 PML011 실측 후 확정 |

결론: 로그 분석으로 운용 단계의 EMIO 응답 패턴은 완전히 파악됐다. PMS 응답 형식과 위치 실측값은 실기기 첫 연결(월요일) 시 즉시 확인 가능하며, 이를 제외한 나머지 항목으로 ff_motion 구현을 시작할 수 있다.

CTS_BG01_MOS_Tester EMIO 응답 분석 | Rev. 05 | 2026-02-22
