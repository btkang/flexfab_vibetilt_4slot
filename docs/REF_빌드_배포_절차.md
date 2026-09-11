# 빌드 및 배포 절차

## 1. 버전 변경

| 파일 | 위치 |
|---|---|
| `ff_vibetilt/ff_vibetilt.csproj` | `<Version>`, `<InformationalVersion>` |
| `RELEASE_NOTE.txt` | 맨 위에 새 버전 항목 추가 |

버전 형식: `X.Y.Z.6MMDD` (예: `0.6.1.60415`)

## 2. 빌드

```bash
# Debug 빌드
dotnet build ff_vibetilt/ff_vibetilt.csproj -c Debug

# Release 빌드
dotnet build ff_vibetilt/ff_vibetilt.csproj -c Release

# flexfab_mini (공통 exe) Release 빌드
dotnet build flexfab_mini/flexfab.csproj -c Release

# publish (닷넷 런타임 포함, 독립 실행)
dotnet publish flexfab_mini/flexfab.csproj -c Release -r win-x64 --self-contained true
```

빌드 결과:
- Debug: `output/Debug/net8.0-windows/`
- Release: `output/Release/net8.0-windows/`
- publish: `flexfab_mini/output/Release/net8.0-windows/win-x64/publish/`

## 3. JSON 수정 시 적용 경로 (3곳)

| 경로 | 용도 |
|---|---|
| `output/Debug/net8.0-windows/` | 개발/디버그 |
| `output/Release/net8.0-windows/` | Release 빌드 |
| `flexfab_mini/output/.../publish/` | 독립 실행 배포 |

**주의:** JSON은 빌드로 자동 복사되지 않음. 3곳 모두 수동 수정 필요.

## 4. 배포 폴더 생성

경로: `D:\03.Code\60.VL01\01.Flexfab_VL01\12.flexfab_VibeTilt\10.실행파일배포\`

폴더 네이밍: `26MMDD_X.Y.Z_motion_232_485`

```
260415_0.6.1_motion_232_485/
├── 0.6.1_60415-RELEASE-미포함/
│   └── Release/net8.0-windows/    ← output/Release/net8.0-windows/ 복사
└── 0.6.1_60415-RELEASE-닷넨런타임포함/
    └── publish/                    ← flexfab_mini/.../publish/ 복사
```

- **RELEASE-미포함**: .NET 런타임 설치된 PC용 (파일 작음)
- **RELEASE-닷넨런타임포함**: 깡통 PC용 독립 실행 (런타임 포함)

### 배포 시 제외 목록 (복사 후 삭제 확인)

| 제외 대상 | 이유 |
|---|---|
| `Log\`, `csv_tilt\` | 테스트 로그/기울기 CSV 흔적 |
| `Result\` | 검사 결과 |
| `serial_*.txt`, `last_serial_*.txt`, `pending_x_result.json` | 시리얼/페딩 상태 (테스트 번호 유출 방지) |
| `TEMP\`, `workspace_backup\`, `qmp_*\`, `win-x64\`(Release 루트 잔재) | 개발 잡폴더 |

남기는 것: `Config\`, `runtimes\`, dll/exe, workspace JSON 전체, error_table.json

## 5. 별도 배포 폴더에도 JSON 적용

배포 폴더의 workspace JSON도 수정해야 함 (빌드와 별개).

## 6. 커밋

- 코드 + JSON 함께 커밋 (분리 금지)
- 버전 변경 + 릴리즈 노트도 같은 커밋에 포함
