<#
.SYNOPSIS
  메인폼(flexfab_mini) 변경분을 GitLab flexfab_mini 기록 저장소의 모델 브랜치에 커밋·푸시.
  CLAUDE.md 1-1 "메인폼 기록 조건부 수정" 절차의 ②(기준점)·④(수정 후) 단계용.

.USAGE
  pwsh tools/sync_flexfab_mini.ps1 -Message "feat(flexfab): 4슬롯 단계1 — UI 4열·슬롯별 순차 실행"
  pwsh tools/sync_flexfab_mini.ps1 -Message "..." -Tag "4slot-stage1-260918"
  pwsh tools/sync_flexfab_mini.ps1 -DryRun     # 복사·diff만, 커밋/푸시 안 함

.NOTES
  - 대상 브랜치: vibetilt-4slot (본 프로젝트). 동기화 폴더: ../flexfab_mini_gitlab (메인 저장소 바깥)
  - 복사 대상: 메인 저장소 git이 추적하는 flexfab_mini/* (workspace.json 제외). bin/obj/output은 추적 안 되므로 제외됨
  - 커밋 메시지 끝에 메인 저장소 HEAD 해시를 자동으로 붙임
#>
param(
    [string]$Message = "",
    [string]$Tag = "",
    [string]$Branch = "vibetilt-4slot",
    [switch]$DryRun
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$sync = Join-Path (Split-Path -Parent $repo) "flexfab_mini_gitlab"
if (-not (Test-Path (Join-Path $sync ".git"))) { throw "동기화 폴더 없음: $sync (git clone http://192.168.10.2:30007/btkang/flexfab_mini.git 먼저)" }

Push-Location $repo
try {
    $head = (git rev-parse --short HEAD).Trim()
    $files = git ls-files flexfab_mini | Where-Object { $_ -ne "flexfab_mini/workspace.json" }
    # 아직 add 안 된 신규 파일도 포함 (예: FlexfabForm.Slot4.cs)
    $files += git ls-files --others --exclude-standard flexfab_mini | Where-Object { $_ -notmatch "/(bin|obj|output)/" }
    $files = $files | Sort-Object -Unique
    foreach ($f in $files) {
        $rel = $f.Substring("flexfab_mini/".Length)
        $dst = Join-Path $sync $rel
        New-Item -ItemType Directory -Force (Split-Path -Parent $dst) | Out-Null
        Copy-Item -Force $f $dst
    }
    Write-Host "복사 $($files.Count)개 → $sync"
}
finally { Pop-Location }

Push-Location $sync
try {
    git checkout -q $Branch
    git add -A
    $stat = git diff --cached --stat
    if (-not $stat) { Write-Host "변경 없음 (GitLab $Branch = 로컬 flexfab_mini)"; return }
    Write-Host $stat
    if ($DryRun) { Write-Host "[DryRun] 커밋/푸시 생략"; git reset -q; return }
    if (-not $Message) { throw "-Message 필요" }
    $full = "$Message`n`n- 출처: flexfab_vibetilt_4slot $head (flexfab_mini/, workspace.json 제외)"
    git commit -q -m $full
    if ($Tag) { git tag -a $Tag -m "$Message ($head)" }
    git push origin $Branch
    if ($Tag) { git push origin $Tag }
    Write-Host "푸시 완료: $Branch $(if ($Tag) { "+ tag $Tag" })"
}
finally { Pop-Location }
