# 역할 및 목적
- 응답은 한글/한국어로 한다.

# 규칙
- Plan 먼저 작성 및 검토한다.
- code 작성을 임의로 시작하지 말고 나에게 확인받아야 한다.

# 상황
- 모션검사는 EMIO 보드(이더넷연결)를 이용해서 setp 모터를 구동시킨다. 

- 기존프로젝트인 BG01이 있고 프로그램이 있다. 거기서 모션부분을 가지고 온다.
- 기존프로젝트인 PCOM이 있고 프로그램은 없고 구동한 LOG가 있다.

- BG01, PCOM , Vibetilt 모두 모션을 구동하는 EMIO 보드를 사용한다.
- c# 프로젝트는 루트폴더이며 ff_vibetilt이 프로젝트 폴더이며 파일이 있다. 

- 모션이 구동하고 센서 측정을 하는거야. 센서는 vl10(232), vl20(485) 가 있고 각각 x,y,z축을  측정해. 그리고 비교할 지그에 붙은 센서는 자이로 센서가 있어.
- ff_vibetilt에 적용된 진동검사, 기울기 검사는 보드레벨 검사야. 지금 추가할건 모션보드에서  진동검사,기울기 검사를 추가할거야


# 지그 구성
- 지그 자이로센서 시리얼연결, 38400 8N1, : docs\2026 0223- 모션지그\SOLAR-360-420_Inclinometer_Sensor_4-20mA_Output.pdf
- X축 : 시리얼, (232/485:115200,sampling :10ms)
- Y축 : 시리얼, (232/485:115200,sampling :10ms)
- Z축 : 시리얼, (232/485:115200,sampling :10ms)
- EMIO 보드 : 이더넷 : 100.100.100.70(예상)
- 

# json 파일
- 프로그램실행시 기본 json 파일(workspace.json)으로 로딩된다.
- 양산시는 기본 json 파일(workspace.json)에 빈 검사항목을 넣는다.
- 양산시는 open workspace 버튼누르고 파일 다이얼로그를 통해 json파일을 선택한다. (예. workspace_232.json) 이후 검사한다.

# PCOM 프로그램 docs\2026 0223- 모션지그
- PCOM 프로그램 화면 로그 : D:\03.Code\60.VL01\01.Flexfab_VL01\12.flexfab_VibeTilt\01.Flexfab_TEST\flexfab_VibeTilt\docs\2026 0223- 모션지그\pcom 지그 화면 로그 2026 0211

# BG01 프로그램 docs\2026 0223- 모션지그
- 소스코드 : D:\03.Code\60.VL01\01.Flexfab_VL01\12.flexfab_VibeTilt\01.Flexfab_TEST\flexfab_VibeTilt\docs\2026 0223- 모션지그\CTS_BG01_MOS_Tester_Ver_1.0.0.6
- BG01 log : BG01_Log__2026-02-11_09H_42M_18S.txt
- BG01 실행파일 : CTS_BG01_MOS_Tester_Ver_1.0.0.6.exe
- 루트 : CTS_BG01_MOS_Tester_Ver_1.0.0.6 폴더는 mfc/C++/VS2017로 만들어져있고 BG01 검사프로그램 소스코드가 있다.

# 검사항목
- 보드레벨 검사와 다르게 모션검사에서는 진동검사, 기울기 검사만 한다.



# 목적
- BG01, PCOM 이전프로젝트에서 모션구동 코드를 추출해서 c# 프로그램에 이식하려고한다.


# 4.1.3.2. Output Mode 2 : G scale Accel Data

## 1) ASCII 출력

### 가속도(G) 데이터 출력

| 항목 | Head1 | 응답 | SP | DATA1 | SP | DATA2 | SP | DATA3 | SP | DATA4 | Tail1 |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Size** | 1 | 3 | 1 | 가변 | 1 | 가변 | 1 | 가변 | 1 | 4 | 1 |
| **내용** | [ | GAC | , | Accel_X | , | Accel_Y | , | Accel_Z | , | Rms_3_axis | ] |

<br>

### 데이터 출력 예)

| 구분 | 내용 |
| :--- | :--- |
| **응답** | `[GAC,0.12,0.03,1.00,1.01]` |
| **비고** | ※ **Accel_X,Y,Z** : X,Y,Z 축 중력가속도(G) real-time 데이터<br>&nbsp;&nbsp;&nbsp;&nbsp;Range : -4.00 ~ 4.00G<br><br>※ **Rms_3_axis** : 3 축 rms real-time 데이터 = $\sqrt{\frac{x^2 + y^2 + z^2}{3}}$<br>&nbsp;&nbsp;&nbsp;&nbsp;Range : 0.00 ~ 4.00G |