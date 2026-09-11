# debug시 메인 json 파일 위치 폴더 
debug시 메인 작업폴더 : ..\flexfab_VibeTilt\output\Debug\net8.0-windows

# DPM 공용 + VibeTilt(485/232) 파트를 합성하여 workspace.json, workspace_232.json 생성
build_workspace.ps1 : 스크립트로 

workspace.json : rs485 기준
workspace_232.json : rs232 기준

# 공용 json 은 232와 485의 공용사용
공통 json 작업폴더 : ..\flexfab_VibeTilt\output\Debug\net8.0-windows\workspace_parts
공통 json 작업파일 : dpm_common.json, vibetilt_232.json, vibetilt_485.json
