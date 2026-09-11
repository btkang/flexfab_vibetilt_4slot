# Ethernet Motion IO (V2) 프로토콜 사양서 요약

## 1. 개요
*   **문서 제목:** Ethernet Motion IO(V2) 프로토콜 사양서 (V1.5)
*   **모델명:** CTS-EMIO-20EBxx
*   **작성일:** 2020. 2. 26
*   **통신 방식:** ASCII 통신 기반 (Ethernet / Serial)

## 2. 통신 프로토콜 구조

### 2.1 프레임 구조
*   **송신 (Host -> Board):** `<STX> ID TYPE CMD DATA1 ... DATAn <ETX>`
*   **수신 (Board -> Host):** `<STX> id type cmd ACK DATA1 ... DATAn <ETX>`

### 2.2 구성 요소 설명
*   **STX:** 0x02 (Start of Text)
*   **ETX:** 0x03 (End of Text)
*   **ID:** Board ID ('P': Base Board, Response 시 소문자 'p')
*   **TYPE:** Command Type
    *   'P': System Control
    *   'I' (또는 'T'): I/O Control
    *   'M': Motion Control
    *   Response 시 소문자('p', 'i', 'm')로 변경
*   **CMD:** 구체적인 명령 코드 (3글자, 예: PPP, PMS)
*   **ACK:** 응답 상태 (Response 시 포함)
    *   '0': 정상 (Acknowledgement)
    *   '1' ~ '9': 에러 코드 (Error Code)
*   **DATA:** 가변 길이 데이터

## 3. 명령어 종류

### 3.1 System 제어 명령 ('P' Type)
| 명령 | 기능 | 설명 |
| :--- | :--- | :--- |
| **PPP** | Request System Information | 펌웨어 버전, 보드 정보 확인 |
| **PPS** | Set System Parameter | 시스템 파라미터 설정 |
| **PPR** | Read System Parameter | 시스템 파라미터 확인 |
| **PPB** | System Initialize | 하드웨어 리셋 |
| **PPI** | Request System Status | 시스템 동작 및 에러 상태 확인 |
| **PPC** | Request Error Code & Clear | 에러 코드 확인 및 클리어 |
| **PPW** | Request Whole System Status | 전체 시스템 상태 요청 (PPI, PII, PML, PMI 등 통합) |
| **PPM** | Request Power Monitor | 전원 상태 확인 |
| **PPT** | Request Temperature Status | 온도 확인 |
| **PPD** | Set/Get System Date & Time | 시간 설정 및 확인 |

### 3.2 Digital I/O 제어 명령 ('I' Type)
| 명령 | 기능 | 설명 |
| :--- | :--- | :--- |
| **PII** | I/O Read Input & Output | 전체 입출력 상태 읽기 |
| **PIO** | I/O Output Control | 전체 출력 제어 |
| **PIR** | I/O Read Input Bit | 특정 입력 비트 읽기 |
| **PIW** | I/O Output Bit Control | 특정 출력 비트 제어 |
| **PIB** | I/O Read Output Bit | 특정 출력 비트 상태 읽기 |
| **PEI** | Extension I/O Read | 확장 보드 입출력 읽기 |
| **PEO** | Extension I/O Control | 확장 보드 출력 제어 |

### 3.3 Motor 제어 명령 ('M' Type)
| 명령 | 기능 | 설명 |
| :--- | :--- | :--- |
| **PMS** | Set Motion Parameter | 모터 파라미터 설정 |
| **PMR** | Read Motion Parameter | 모터 파라미터 확인 |
| **PMO** | Move Origin | 원점 복귀 |
| **PMT** | Auto Teaching | 자동 티칭 (센서 스캔) |
| **PMP** | Move Position | 지정된 위치(센서)로 이동 |
| **PMA** | Absolute Move | 절대 좌표 이동 (Pulse) |
| **PMD** | Relative Move | 상대 좌표 이동 (Pulse) |
| **PMV** | Velocity Move | 속도 모드 이동 |
| **PMB** | Stop | 감속 정지 |
| **PME** | E-Stop | 긴급 정지 |
| **PML** | Request Pulse Position | 현재 펄스 위치 확인 |
| **PMI** | Request Motion Status | 모터 상태 확인 (동작중, 에러 등) |
| **PMC** | Request Encoder Count | 엔코더 값 확인 |
| **PMW** | Write Encoder Count | 엔코더 값 쓰기 |
| **PMK** | Ext. Servo Dev. Clear | 서보 편차 카운트 클리어 |
| **PMU** | Ext. Servo Start-up | 서보 On/Off 제어 |
| **PMF** | On-the-fly Velocity Move | 이동 중 속도 변경 |

## 4. 에러 코드 (ACK Code & Motion Error)

### 4.1 ACK 코드 (프로토콜 응답)
*   **0:** 에러 없음 (성공)
*   **1:** 등록되지 않은 명령어
*   **2:** 프로토콜 포맷/길이 에러
*   **3:** 파라미터 접근 에러
*   **4:** 명령 수행 불가 (동작 중 등)
*   **5:** 이전 명령 에러 미해제 (PPC로 클리어 필요)
*   **6:** 명령 수행 중 에러 발생
*   **7:** Motor Driver Disable 상태
*   **9:** EMO(비상정지) 신호 감지됨
*   **A:** Motor Driver 심각한 이상
*   **B:** I/O Update 오류
*   **D:** FPGA Data Read/Write Error

### 4.2 주요 Motion 에러 코드 (상세)
*   **x01:** 외부 드라이버 Fault
*   **x02:** 보드 과열
*   **x03:** 통신 에러
*   **x04:** 모터 드라이버 과전류/보호동작
*   **x15:** 원점 복귀 중 이동거리 초과 (센서 감지 실패)
*   **x17:** 원점 복귀 파라미터 설정 에러
*   **x25:** Auto Teaching 이동거리 초과
*   **x29:** Move Origin 미수행 상태에서 Auto Teaching 시도

*(x는 축 번호를 의미)*

## 5. 통신 설정 (Serial / Network)

### 5.1 Serial (RS-232C)
*   **Baudrate:** 115,200 bps
*   **Data bits:** 8
*   **Stop bits:** 1
*   **Parity:** None
*   **Flow Control:** 없음

### 5.2 Ethernet (Default)
*   **IP Address:** 100.100.100.70
*   **Subnet Mask:** 255.255.255.0
*   **Gateway:** 192.168.1.1
*   **Port:** 2233
*   **설정 변경:** 시리얼 터미널에서 `localip`, `save` 등의 명령어로 변경 가능.
