# REF: 배포 시 JSON 설정 변경 가이드

- **목적:** 다른 PC/지그에 배포할 때 JSON에서 변경해야 할 항목 정리
- **대상 파일:** workspace_motion_232.json, longrun_motion_*.json

---

## 1. 반드시 확인/변경할 항목

### 1.1 COM 포트 (장치 관리자에서 확인)

검색 키워드: `"port"`

| 항목 | JSON 키 | 현재 값 | 설명 |
|------|---------|--------|------|
| VL 센서 X축 | uart_232 → port | COM13 | RS-232 X축 통신 |
| VL 센서 Y축 | uart_232_y → port | COM14 | RS-232 Y축 통신 |
| 자이로 | uart_gyro → port | COM15 | SOLAR-360-420 레퍼런스 |
| VL 센서 485 | uart_485 → port | COM13 | RS-485 통신 (컨버터 경유) |

### 1.2 EMIO 모터 제어 (TCP)

검색 키워드: `"ip"`

| 항목 | JSON 키 | 현재 값 | 설명 |
|------|---------|--------|------|
| EMIO IP | tcp_emio → ip | 100.100.100.70 | EMIO 보드 IP |
| EMIO 포트 | tcp_emio → port | 2233 | EMIO 보드 포트 |

### 1.3 MongoDB

검색 키워드: `"mongodb"`

| 항목 | JSON 키 | 현재 값 | 설명 |
|------|---------|--------|------|
| DB 주소 | mongodb_uri | mongodb://192.168.10.10:45029/... | 검사 결과 저장 DB |

---

## 2. 운영 모드 설정 (용도에 따라 변경)

검색 키워드: `"log_level"`, `"show_log"`, `"test_mode"`

| 항목 | JSON 키 | 양산 권장 | 테스트 | 설명 |
|------|---------|---------|-------|------|
| 로그 레벨 | log_level | MP_LOG | TEST_LOG | MP_LOG: 핵심만, TEST_LOG: 전체 |
| 로그창 표시 | show_log | false | true | 양산: 숨김 |
| 시리얼 중복 | serial_duplicate_check | 2 | 0 | 2:차단, 1:경고, 0:안함 |
| FAIL 시 | fail_continue | false | true | false:중단, true:끝까지 |
| 검사 모드 | test_mode | dual | single/dual | dual: X+Y 동시 |
| 반복 횟수 | repeat_all_count | 없음(1회) | N | 롱런 시 설정 |

---

## 3. 스펙/모션 파라미터 (일반적으로 변경 불필요)

변경 시 전 파일 수평전개 필요 (longrun_dual 기준)

| 항목 | JSON 키 | 현재 값 | 설명 |
|------|---------|--------|------|
| 기울기 ±20도 허용 | tilt_tolerance_20 | 0.2 | 사양서 기준 |
| 기울기 ±45도 허용 | tilt_tolerance_45 | 0.5 | 사양서 기준 |
| 진동 최소 | vibe_min | 0.99 | 사양서 기준 |
| 진동 최대 | vibe_max | 1.01 | 사양서 기준 |
| OFFSET 검증 범위 | verify_an_min / max | -0.05 / 0.05 | 사양서 기준 |
| 홈 센서 | home_sensor | in1 | in1/in2/auto |
| 자이로 방향 X축 | gyro_dir | setdir6 | X축 지그 |
| 자이로 방향 Y축 | gyro_dir | setdir5 | Y축 지그 |

---

## 4. 232 vs 485 전환 시 변경할 것

| 항목 | 232 | 485 |
|------|-----|-----|
| uart id | uart_232 | uart_485 |
| baudrate | 115200 | 115200 |
| vibetilt config | "uart_232": "uart_232" | "uart_485": "uart_485" |
| ctsp commlib | uart_232 | uart_485 |
| VER 명령 | `<VER>` | `<VER,1>` |
| OFFSET 명령 | `<OFFSET,1>` / `[OFFSET,0,1]` | `<OFFSET,1,1>` / `[OFFSET,0,1,1]` |
| APPCFG 명령 | `<APPCFG,SAVE>` / `[APPCFG,0,SAVE]` | `<APPCFG,1,SAVE>` / `[APPCFG,0,1,SAVE]` |
| UID | 미구현 | `<UID,1,VL2-{serial}>` |
| Y축 uart_id | uart_232_y 지정 | 제거 (단일포트) |

---

## 5. 빠른 변경 체크리스트

새 PC에 배포할 때:
- [ ] COM 포트 확인 (장치 관리자) → JSON 수정
- [ ] EMIO IP/포트 확인 → JSON 수정
- [ ] MongoDB 주소 확인 → JSON 수정
- [ ] 용도에 맞는 workspace 선택 (양산: workspace_motion_xxx, 테스트: longrun_motion_xxx)
- [ ] 실행 테스트 1회 → ALL PASS 확인
