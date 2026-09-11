# REF_ pSMC_Tester 양산 검사 프로그램 상세 분석

> 소스: `02.ref_IMU모델소스\pSMC_Tester - 1.5m\CTS_pCOM_Tester\`
> 실제 언어: **C++ / MFC** (Visual C++ v110/v120). C# 아님.

## 1. 프로그램 개요

| 항목 | 내용 |
|---|---|
| 대상 디바이스 | **pSMC** — 모터/서보 제어 모듈. FPGA, TMC(Trinamic) 모터드라이버, eFlash, **Motion Sensor(IMU: 기울기 각도 X/Y/Z + 온도)** 탑재 |
| 통신 | RS-232/UART 다중 COM + 일부 TCP(`CMySocket`) |
| DUT 속도 | **115200 bps** (`m_iBaudRate2=7`), 8-N-1 |
| 프로토콜 | 송신 `<명령+HEX체크섬>\r\n`, 응답 `[...]`. 콘솔모드 `1<fpgareg,...>`, `1GMD1,32000` 형태 |

### 포트 구성 (`Commtest.cpp:452~498`)
| 함수 | 포트 | 대상 |
|---|---|---|
| SendData_to_test | m_ComuPort2 | **DUT(pSMC)** 115200 |
| SendData_to_ref | m_ComuPort3 | ref pCOM 57600 |
| SendData_to_supply | m_ComuPort4 | SMPS (`volt`,`curr`) |
| SendData_to_dpm | m_ComuPort | DPM(파워미터) |
| SendData_to_CL | m_ComuPortCL | Current Loader (0xAA 바이너리) |

## 2. 검사 시퀀스 (활성 항목)

enum: `Commtest.cpp:20~155`, 디스패처: `ThreadStatus_Commtest` (`Commtest.cpp:4827~6491`). 구 PLC 항목 다수는 `#if 0` 비활성.

| 스텝 | 내용 | 판정 위치 |
|---|---|---|
| pCOMWAIT | JIG 릴레이/모드 초기화 | :4847 |
| CONFIGCLEAR_M | config clear | — |
| SWITCHCONSOLE/NORMAL_* | 콘솔/일반 모드 전환("exit" 확인) | :5040,5047 |
| VERCHK | FW 버전 (`<V56>` → `[V=1.022...]` 포함) | :5055 (`m_sCompVersion="1.022"` :180) |
| XPNS | RSSI 대기값 (PLC 잔재) | :5072 |
| DUT_OUTPUT_TEST | IO Port-Remote (psmcIOTest, 6bit) | :5180 |
| DCLINEVOLT/OVERCUR/CHECK | 현재 강제 PASS | :6111,6151,6182 |
| **SPITESTFPGA** | `1<fpgareg,X=0,ADDR=0x0F,...>` → 상위16bit==0x55AA | :6325 (로직 :3107~3134) |
| **SPITESTTMC** | `1<tmcreg,X=0,ADDR=0x04,...>` → 상위8bit==0x30 | :6332 (:3137~3164) |
| **SPITESTFLASH** | `<FIDD3>` → 제조사ID 0xEF | :6339 (:3168~3191) |
| **SPITESTMOTSEN** | **IMU 통신 확인** (아래 4절) | :6345 |
| **I2CTEST** | 온도센서 `<MTEMP>` (아래 4절) | :6357 |
| ADCTESTVOL | `1<getadc,CAL,VOL>` → 값>0 | :6368 (:3221~3235) |
| ADCTESTCUR | `1<getadc,CAL,CUR>` → ≤ m_sLimitCurr×1000mA | :6380 (:3238~3252) |
| DIAGPINTEST | TMC diag 핀 0x3000/0x2000 쓰고 FPGA reg 0x43 bit0 검증 | :6394 (:3258~3330) |
| MOTIONTEST | `1GMD1,±32000` 정역회전+육안 (보통 skip) | :6421 (:3338~3422) |
| VOLTCALITEST | 자동검사 제외(skip) | :6442 |
| CURRCALITEST | 자동검사에서 END 처리 | :6467 |

> 기울기 각도 검사 `TEMP_ANGLE_CHECK`는 enum 주석 처리(`:131`) + 실행부 `#if 0`(`:6061~6109`) — 비활성이나 로직 잔존.

## 3. IMU 검사 상세

### 명령 정의 (`Commtest.cpp:2898~2949`)
| 함수 | 명령 | 용도 |
|---|---|---|
| `mbase(which,dat)` | `<MBSET=n+HEX>` | 영점/기준 캘리브레이션, n=0,1,2 순차 |
| `minfo(which,dat)` | `<MINFO=n+HEX>` | n=3→AngleX, 4→AngleY, 5→AngleZ, 0→센서 통신/ID |
| `mtemp(which)` | `<MTEMP+HEX>` | 센서 온도 |

### SPITESTMOTSEN — IMU 통신 자가진단 (`:3195~3202, 6345~6355`)
- `minfo(0,0)` = `<MINFO=0...>` 송신
- 응답에 `=` 구분자 2개 이상(파싱 가능)이면 PASS. **수치 spec 판정 없음** — comm check 성격.

### I2CTEST — 온도 (`:3207~3215, 6357~6366`)
- `mtemp(0)` 송신, 응답 첫 `=` 뒤 값에 소수점 있으면 PASS.
- 주석 "Motion Sensor doesn't use I2C" — 실제 UART 명령으로 취득.

### TEMP_ANGLE_CHECK — 영점 안정성 (비활성, `:6061~6109`, 사전측정 `:4944~4988`)
1. `MBSET=0` → `MBSET=1`(1000ms) → `MBSET=2` (영점 세팅)
2. `MINFO=3/4/5` → AngleX/Y/Z[0], `MTEMP` → Temp[0] (파싱: `AfxExtractSubString(...,2,'=')` 후 끝 3자 제외 atof)
3. 동작 후 재측정 → [1]
4. 판정 (`Commtest.cpp:6100~6108`):
```
fTemp = Temp[1]-Temp[0]
ftolerance = 0.01*fTemp + 0.07
PASS (AND):
  |Temp[1]-Temp[0]| <= 8.0          // 전후 온도차 8°C 이내
  |ΔAngleX| <= ftolerance
  |ΔAngleY| <= ftolerance
  |ΔAngleZ| <= 0.1
```
- 의미: 동작 전후 IMU 기울기 각도가 영점에서 벗어나지 않는지(오프셋 안정성). 주석 "25-05-09: maximum temp diff under 8.0", "200610 온도 변화 OFFSET 0.03→0.07" (`:6097~6101`)
- **가속도/자이로 원시값·진동 스펙트럼 판정 없음.** IMU는 각도+온도 통합 형태로만 검사.

## 4. 캘리브레이션 spec (하드코딩)

### 전압 캘리 `voltcalitest()` (`Commtest.cpp:3475~3977`)
- 측정 포인트 `{10.0, 15.0, 20.0, 24.0}V` (`:3484`), 검증 `{17.0}V` (`:3486`)
- SMPS 인가 → DPM 기준 + DUT raw ADC 각 10회 평균 → `<setcal,VOL,raw/dpm/...>` 다점 선형 캘리 (`:3809~3819`) → 17V 검증 **최대오차 20mV**(`cond_err_value`, `:3480,3926`) → `<savecal>` (`:3952`)
- 포인트 유효범위: DPM 기대값 ±1000mV (`:3801~3806`)

### 전류 캘리 `curcalitest()` (`Commtest.cpp:3979~`)
- 포인트 `{0.0, 0.5, 1.5, 2.5}A` (`:3992`), Current Loader CC 인가, 검증 1.5A, 포인트 허용 ±400mA (`:4345~4346`), `<setcal,CUR,...>` (`:4353~4363`)

## 5. 설정/spec 위치
- `Config.txt`/`Config_출력문구 참조.txt`: 구 PLC(pCOM) 파라미터 — IMU 무관.
- 레지스트리 프로파일 SECTION `"PREFERENCES"` (`Commtest.cpp:167`, 저장/로드 `:390~397, 6665~6667`): `m_sCompVersion=1.022`, `m_sLimitCurr`(전류상한), `m_bUseBreak=1`, `m_bUserCable=0`
- 판정 수치는 전부 소스 하드코딩. 결과 표시: `DecisionDlg.cpp`, `TestResult()`
- 시리얼 설정: `CommThread.cpp:62~153`, `CTS_pCOM_TesterDlg.cpp:558~573, 1125`
