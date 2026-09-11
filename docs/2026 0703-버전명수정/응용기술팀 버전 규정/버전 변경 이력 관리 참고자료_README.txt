V4.7 : 2019.1.4(세메스 송진호 프로 요청)
    1) End 센서 공동 사용시에는 End Limit Error 감지 하지 않도록 수정
    2) Home 센서 공동 사용시에는 Home Limit Error 감지 하지 않도록 수정
    3) Error Check Array 갯수 변경 : 2 -> 4

V4.6 : 2018.4.25, 수정 내역 (한화기계 Tower Lifter 노이즈 문제 해결)
    1) FPGA read/write 함수의 데이터 확인 기능 보완
    2) FPGA read/write시 interrupt enable/disable 추가
    3) MoveStatus 확인 기능 보완

V4.5 : 임시 현장 테스트 버전(천안 L5)

V4.4 : AC03 대상 한정 수정 반영
    - 아래 동일

V4.3 : 2018.2.5, AA04/AB03/AD03 대상 수정 내역
    1) 네크웍 성능 향상 목적의 Queue Buffer Size 상향 변경
        - NUMBIGBUFS  8 -> 10
        - NUMLILBUFS  6 -> 14
        - NUM_RXBDS  2 -> 유지
        - NUM_TXBDS  2 -> 4
    2) 에러처리 함수(dtrap) 호출시 Board Reset 부분 삭제
        - 에러 메시지 출력후 호출지점으로 복귀
    3) ARP 응답 3회 이상 실패시 소켓 Close
        - 호스트 장비에서의 일방적인 접속 차단시 대응
    4) 현장 패치 작업 위한 boot loader 반영 

V4.2 : 2016.08.19
    1) 전원 감시 기능 추가 : 'PPM' 명령어 추가 
        . MTR_POWER_FAIL, IO_IN_PWR_FAIL, IO_OUT_PWR_FAIL
    2) 네트웍 통신 전송 에러시 보드 리셋 기능 삭제 
    3) ARP 명령어 메모리 할당 오류시 보드 리셋 기능 삭제

V4.1 : 2012.08.14