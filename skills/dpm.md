상황
workspace.json는 workspace_vibetilt_b4cc79e step2 485 complete.json와 같은 파일이다.




workspace.json 에 workspace_colorimeter_dpm.json 항목중 dpm 을 참고해서 추가한다.
24V, 3.3V, 전류 검사를 추가한다.
	
	
	24V:   id41,에러율:3% min 23.28 ~ max 24.72
    3.3V: id43, 에러율:3% min 2.0 ~ max 3.5
    전류:  id09, min -0.001 ~ max 0.5
	
전압은 pass 범위를 +- %로 설정한다.(기본 3%)
전류는 직접 범위를 입력한다.	



 앞으로 작업할 때:
  1. 코드나 파일을 수정할 때 주석/문서에 명확히 남기기
  2. "임시", "나중에 변경", "H/W 설정 필요" 같은 상태를 명시하기
  3. 정상값/계산식을 함께 기록하기
  4. 다른 사람(또는 나중에 나 자신)이 이 주석만 보고도 다음 작업을 진행할 수 있게 하기
  
  