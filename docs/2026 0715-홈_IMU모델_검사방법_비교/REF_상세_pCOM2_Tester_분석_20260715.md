# REF_ pCOM2 양산 검사 프로그램 (CTS_pCOM_Tester V2.0.0.1/2019) 상세 분석

> 소스: `02.ref_IMU모델소스\CTS_pCOM2_Tester_V2.0.0.1_2019 (3) - 104kb\CTS_pCOM_Tester\`
> 실제 언어: **C++ / MFC** (VS2012~2013 v110/v120). C#/.NET 아님.

## 1. 프로그램 개요

| 항목 | 내용 |
|---|---|
| 대상 디바이스 | **pCOM2** — PLC(전력선통신) 모듈. 내부에 **MPU-6050 IMU**(3축 가속도+3축 자이로) 및 온도센서 탑재 |
| 프레임워크 | MFC 대화상자 기반, 멀티스레드(`AfxBeginThread`) |
| 펌웨어 버전 기준 | `m_sCompVersion = "1.55"` (VERCHK 비교), config 버전 v1.08 이상 분기 존재 |

### 통신
- UART/RS-232, `CreateFile`+`DCB` (`CommThread.cpp:62~153`)
- 보레이트표 (`CTS_pCOM_TesterDlg.cpp:528~544`): idx6=57600, idx7=115200. 기본: 포트1=57600, 포트2=115200 (`:92,95`)
- 디바이스 config: PLC baud=57600, user serial=115200 (`SetProduct.cpp:99,141`)
- 다중 포트 (`Commtest.h:74~86`): test(DUT), ref(기준기), user serial, 전원공급기(SCPI `VOLT?`/`MEAS:CURR?`), noise 발생기
- 프레임: `<명령+2자리HEX체크섬>` (`Commtest.cpp:2373`), 콘솔 진입/이탈 `#console\r`/`exit\r` (`BoardLevel.cpp:506,519`)
- 판정: 대부분 `strstr()` 문자열 포함 여부 ("test N ok", "PASS" 등)

## 2. 검사 모듈 구성 (3개 다이얼로그)

### (A) 보드 레벨 검사 — `CBoardLevel` (`BoardLevel.cpp`, enum :10~40)
| 순서 | 항목 | 내용 |
|---|---|---|
| 1 | USERSERIAL0~7 | 유저 시리얼 통신 |
| 2 | TXRSSI_MNTVOLTAGE | 송신 RSSI/모니터 전압 |
| 3 | SRAM | 외부 SRAM |
| 4 | FLASH | 플래시 (`"test 4 ok"`) |
| 5 | IDCHG* | ID 설정 (`#if 0` 비활성) |
| 6 | RF2GFULLPAY* | 2.4G RF (비활성) |
| 7 | PLCREGISTER | PLC IC 레지스터 (`"0x7F"`) |
| 8 | **MPU6050** | **IMU 자가진단 (`"test 7 ok"`)** |
| 9 | BOARDINPUTPORT | 입력 포트 (`"0x 2"`/485시 `"0x 0"`) |
| 10 | IOCHK | I/O (`"test 9 ok"`) |
| 11 | INFLASH | 내장 플래시 (`"test 12 ok"`) |
| 12~13 | PLC_LOW/HIGH_CH1~3 | PLC 저/고주파 채널 (`"test 13 ok"`) |
| 14 | LEDON/LEDOFF | LED |

### (B) 양산 프로그래밍/셋업 — `CSetProduct` (`SetProduct.cpp`, enum :21~65)
`VOLTAGE_INIT → 콘솔진입 → CONFCLR → CONFCHK_1 → CONFMCHK_1(모션config) → RESET → 전압보정(24/30/8단계) → FLOATING_RSSI 측정/보정 → RFID → CONFIG(설정기록) → VERSION → CONFCHK_2 → CONFMCHK_2(모션config 최종) → SAVE`

### (C) 통신/세트 검사 — `CCommtest` (`Commtest.cpp`, enum :18~139)
`pCOMWAIT → CONFIGCLEAR_M → ... → VERCHK → XPNS(수신RSSI 150~350) → IO 테스트 → USERDATA_2000 → 채널통신(CHCOMM/CHPWR/CHCHG) → GOOFF → DC라인 전압/과전류/체크`
> IMU 각도/온도 스텝(`pCOM_TEMP_ANGLE`, `TEMP_ANGLE_CHECK`)은 이 버전에서 주석/`#if 0` 비활성. 로직은 잔존.

## 3. 주요 판정 상세

| 검사 | 명령 | 판정 | 위치 |
|---|---|---|---|
| PLC IC 레지스터 | — | `"0x7F"` 포함 | `BoardLevel.cpp:845~850` |
| **MPU6050 자가진단** | `<TEST=7B4>` (delay 350ms) | `"test 7 ok"` 포함 | `BoardLevel.cpp:438~448, 852~857` |
| 입력포트 | `<TEST=8B5>` | `"0x 2"`/`"0x 0"` | `BoardLevel.cpp:451~460, 859~872` |
| 플래시 | `<TEST=12E0>` | `"test 12 ok"` | `BoardLevel.cpp:491~500, 881~886` |
| PLC 통신 | `<TEST=0AD>`,`<TEST=13E1>` 등 | `"test 13 ok"` (2회 재시도) | `BoardLevel.cpp:546~565, 888~904` |
| 버전 | — | `"1.55"` 포함 | `Commtest.cpp:2764~2769` |
| 수신 RSSI | `Xpns(0,1)` | 150 ≤ 값 ≤ 350 | `Commtest.cpp:2780~2801` |
| DC라인 전압 | `VOLT?` ×5 | 5회 모두 > 20V | `Commtest.cpp:3691~3722` |
| DC라인 과전류 | `MEAS:CURR?` ×5 | 5회 모두 < 0.5A(`m_sLimitCurr`) | `Commtest.cpp:3724~3748` |
| CONFIG 기록 | Config.txt 라인별 전송 | echo 일치 | `SetProduct.cpp:577~757` |

## 4. IMU 검사 상세

### 4-1. MPU-6050 자가진단 (보드레벨)
`<TEST=7B4>` → `"test 7 ok"` 포함 시 PASS. 펌웨어 내부 I2C self-test(WHO_AM_I 등), PC는 문자열 판정만.

### 4-2. 각도/온도 영점 드리프트 검사 (세트레벨, 이 버전 비활성)
명령 (`Commtest.cpp:2367~2418`):
| 함수 | 명령 | 의미 |
|---|---|---|
| `mbase(0,n)` | `<MBSET=n>` | 모션베이스(영점) 세팅, n=0,1,2 순차 |
| `minfo(0,3/4/5)` | `<MINFO=3/4/5>` | Angle X/Y/Z 읽기 |
| `mtemp(0)` | `<MTEMP>` | 온도 읽기 |

흐름 (`Commtest.cpp:2671~2707` 기준측정 → `3646~3689` 재측정·판정):
1. `MBSET=0` → 100ms → `MBSET=1` → 1000ms → `MBSET=2`
2. Angle X/Y/Z + Temp 기준값 저장 (파싱: `=` 3번째 토큰, 뒤 3자 제거 후 atof; 표시 X,Y=`%2.2f`, Z=`%3.1f`)
3. `tempaging` 발열 대기 기본 **8000ms** (`Commtest.cpp:174`)
4. 재측정 후 판정 (`Commtest.cpp:3681~3688`):
```
fTemp = Temp[1] - Temp[0]
ftolerance = 0.01 * fTemp + 0.07
PASS (AND):
  (Temp[1]-Temp[0]) >= 8.0          // 8°C 이상 상승할 것
  |ΔAngleX| <= ftolerance
  |ΔAngleY| <= ftolerance
  |ΔAngleZ| <= 0.1                  // 고정
```
주석: "200610 온도 편차 OFFSET 0.03→0.07", "온도 편차 조건 8.0도 이상" (`:3678~3679`)

### 4-3. IMU/모션 config 기대값 `CMPSTRMpCOM` (`SetProduct.cpp:111~124`, 배열판 `:154~167`)
| 파라미터 | 값 | 의미 |
|---|---|---|
| acX/acY/acZ, gyX/gyY/gyZ | 0 | 가속도/자이로 오프셋(영점) |
| tilt_angle | 200 | 틸트 감지 각도 임계 |
| tilt_angle_x/y_offset | 0 | 틸트 축 오프셋 |
| runout_angle / move_offset | 30 / 20 | 런아웃 각도/이동 오프셋 |
| runoutTime | 1000 | 런아웃 판정 시간(ms) |
| runout_oneside_angle/_Time | 20 / 20 | 한쪽 런아웃 |
| Gyro_angle_offset | 20 | 자이로 각도 오프셋 |
| impact_gravity / ImpactTime | 1000 / 15 | 충격 임계(mg 추정)/시간 |
| MoveStart/Stop/SenseAcc | 70 / 4 / 4 | 이동 시작/정지/감지 가속도 |
| FastMoveStep1/2SenseAcc | 20 / 100 | 급이동 감지 |
| VibrationAcc | 10 | 진동 감지 가속도 임계 |
| low_filter_cut_off | 30 | LPF 컷오프 |
| ZeroG_OffsetAcX/Y/Z | 0 | 제로-G 오프셋 |
| E_RUNOUT_ACC / E_ELEVATION_ACC | 2000 / 2000 | 런아웃/고도 가속도 |

- `MOEN=1`(Motion Enable) → 응답 `"MOEN=ON,InnerFunction"` 확인 (`SetProduct.cpp:630~651`)

## 5. spec 위치
- 판정 수치: 소스 하드코딩. 디바이스 config: `Config.txt`(DC0=115, DVM=3, UB=2, MOEN=1 등 35라인) + 비교문자열.
- 공통 판정/결과: `TestResult` (`Commtest.cpp:2458~2560`)
