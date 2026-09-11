# Quality Monitor (모션 MP 품질 모니터링)

Result JSON을 스캔해서 TILT/VIBE 측정값의 추이·분포·FAIL 패턴을 집계/시각화.
검사 프로그램(flexfab.exe) **무수정**. JSON을 읽기만 함.

## 설치
```
pip install -r requirements.txt
```

## Level 1: CLI 집계 (`monitor.py`)

```
python monitor.py --result-dir "<Result 경로>"
python monitor.py --result-dir "<Result 경로>" --days 30
python monitor.py --result-dir "<Result 경로>" --out csv --csv-path events.csv
```

**출력:**
- 총 시리얼, PASS/FAIL, 불량률
- 항목별 FAIL 카운트
- TILT 각도별 오차 요약 (median/std/min/max)
- VIBE 축별 1G 값 요약

**CSV:** 한 행 = 한 측정 이벤트 (TILT 8 + VIBE 3 = 시리얼당 11행). 엑셀에서 피벗/차트/수동 관리도 작성 가능.

## Level 2: Streamlit 대시보드 (`app.py`)

```
streamlit run app.py -- --result-dir "<Result 경로>"
```

**접속:**
- 같은 PC: <http://localhost:8501>
- 같은 네트워크 타 PC: `http://<이 PC의 IP>:8501`
- 방화벽에서 TCP 8501 인바운드 허용 필요

**탭:**
1. **오늘** — 오늘 PASS/FAIL, 작업자별, 시리얼 검색
2. **TILT 추이** — 각도별 오차 시계열 + 스펙선(±0.2/±0.5)
3. **TILT 분포** — 히스토그램/박스 + 통계 요약
4. **VIBE** — 축별 시계열/히스토그램 + X/Y/Z 축간 비교 (norm)
5. **TILT 삼중값** — value/gyro/vl 3중 필드 비교 (센서 개별 이상 분리 관찰)
6. **FAIL** — FAIL 건 전체 조회

**사이드바:**
- Result 경로 직접 입력 가능 (`--result-dir` 대신)
- 기간 필터 (전체/1/7/30/90일)
- 캐시 새로고침 버튼

## 운영 시나리오

### 시나리오 A: 검사 PC + 모니터링 PC 분리
- 검사 PC에서 Result 폴더 SMB 공유 (`\\검사PC\Result`)
- 모니터링 PC에서 `--result-dir "\\검사PC\Result\모션지그검사(RS-485)_MP"`

### 시나리오 B: 미니피씨 단독
- 미니피씨 자체에서 streamlit 실행 → `localhost:8501`
- WiFi 되면 같은 네트워크 PC/태블릿에서 `http://<IP>:8501`
- WiFi 안 돼도 미니피씨 자체는 항상 뷰 가능

### 시나리오 C: 오프라인 분석 (개발 PC)
- 미니피씨 Result 폴더 주기 복사 (USB/공유/zip)
- 개발 PC에서 같은 명령으로 실행, `--result-dir`만 복사본 경로 지정

## 폴더 구조 가정
```
Result/
  모션지그검사(RS-485)_MP/
    2026-04-13/
      VL2-00001.json
      VL2-00002.json
      ...
    2026-04-14/
      ...
```
재귀 스캔하므로 중첩 구조도 OK. 시리얼당 JSON 파일 1개.

## 인식하는 JSON id
- `MOTION_TILT_X_485`, `MOTION_TILT_Y_485` — 4각도 data 배열
- `MOTION_VIBE_X_485`, `MOTION_VIBE_Y_485`, `MOTION_VIBE_Z_485` — 축별 1G

다른 id는 무시 (COMM/FWVER/UID/OFFSET 등).

## 문제 해결

- **콘솔 한글 깨짐:** Windows cmd 코드페이지 문제. `chcp 65001` 실행 또는 PowerShell 사용. CSV/Streamlit은 UTF-8 정상.
- **방화벽 차단으로 타 PC 접속 불가:** Windows Defender 방화벽 고급 → 인바운드 규칙 → TCP 8501 허용 (사설/공용 프로파일 모두).
- **IP 변경으로 접속 실패 (무선):** hostname 접속(`http://MINIPC-01:8501`) 또는 라우터 DHCP 예약.
- **경로에 한글/공백:** 따옴표로 감싸기 `"..."`.
