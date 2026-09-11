# build_workspace.ps1
# DPM 공용 + VibeTilt(485/232) + Motion(485/232) 파트를 합성하여
# workspace.json, workspace_232.json, workspace_motion_485.json, workspace_motion_232.json 생성
#
# 사용법: powershell -ExecutionPolicy Bypass -File build_workspace.ps1

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$partsDir = Join-Path $scriptDir "workspace_parts"

# 파트 파일 읽기 (UTF-8)
$dpmRaw = Get-Content (Join-Path $partsDir "dpm_common.json") -Raw -Encoding UTF8
$vt485Raw = Get-Content (Join-Path $partsDir "vibetilt_485.json") -Raw -Encoding UTF8
$vt232Raw = Get-Content (Join-Path $partsDir "vibetilt_232.json") -Raw -Encoding UTF8
$motion485Raw = Get-Content (Join-Path $partsDir "motion_485.json") -Raw -Encoding UTF8
$motion232Raw = Get-Content (Join-Path $partsDir "motion_232.json") -Raw -Encoding UTF8

$dpm = $dpmRaw | ConvertFrom-Json
$vt485 = $vt485Raw | ConvertFrom-Json
$vt232 = $vt232Raw | ConvertFrom-Json
$motion485 = $motion485Raw | ConvertFrom-Json
$motion232 = $motion232Raw | ConvertFrom-Json

function Assert-UniqueValues {
    param(
        [string]$Label,
        [array]$Values
    )

    $dups = $Values |
        Group-Object |
        Where-Object { $_.Count -gt 1 } |
        Select-Object -ExpandProperty Name

    if ($dups) {
        throw "$Label 중복 감지: $($dups -join ', ')"
    }
}

function Build-Workspace {
    param($vibetilt, $dpmPart)

    # libraries: vibetilt + dpm
    $libraries = New-Object System.Collections.ArrayList
    foreach ($lib in @($vibetilt.libraries)) { $null = $libraries.Add($lib) }
    foreach ($lib in @($dpmPart.libraries)) { $null = $libraries.Add($lib) }

    # library_depencency: vibetilt + dpm
    $deps = New-Object System.Collections.ArrayList
    foreach ($d in @($vibetilt.library_depencency)) { $null = $deps.Add($d) }
    foreach ($d in @($dpmPart.library_depencency)) { $null = $deps.Add($d) }

    # procs: DPM procs first, then vibetilt procs
    $procs = New-Object System.Collections.ArrayList
    foreach ($p in @($dpmPart.procs)) { $null = $procs.Add($p) }
    foreach ($p in @($vibetilt.procs)) { $null = $procs.Add($p) }

    # 안전검증: ID 중복 방지
    Assert-UniqueValues -Label "libraries.id" -Values @($libraries | ForEach-Object { $_.id })
    Assert-UniqueValues -Label "project.procs.id" -Values @($procs | ForEach-Object { $_.id })

    $workspace = [ordered]@{
        libraries = @($libraries.ToArray())
        projects = @(
            [ordered]@{
                id = $vibetilt.project.id
                name = $vibetilt.project.name
                library_depencency = @($deps.ToArray())
                procs = @($procs.ToArray())
            }
        )
        active_project = $vibetilt.project.id
    }

    return $workspace
}

function Find-ProcById {
    param(
        [array]$Procs,
        [string]$ProcId
    )

    return @($Procs | Where-Object { $_.id -eq $ProcId } | Select-Object -First 1)[0]
}

function Merge-Params {
    param(
        $BaseParam,
        $OverrideParam
    )

    $merged = [ordered]@{}

    if ($BaseParam -ne $null) {
        foreach ($p in $BaseParam.PSObject.Properties) {
            $merged[$p.Name] = $p.Value
        }
    }

    if ($OverrideParam -ne $null) {
        foreach ($p in $OverrideParam.PSObject.Properties) {
            $merged[$p.Name] = $p.Value
        }
    }

    return [pscustomobject]$merged
}

function Build-MotionWorkspace {
    param(
        $motionPart,
        $vibetiltPart,
        [string]$VibeSourceProcId,
        [string]$TiltSourceProcId
    )

    $libraries = New-Object System.Collections.ArrayList
    foreach ($lib in @($motionPart.libraries)) { $null = $libraries.Add($lib) }

    $deps = New-Object System.Collections.ArrayList
    foreach ($d in @($motionPart.library_depencency)) { $null = $deps.Add($d) }

    $vibeSource = Find-ProcById -Procs @($vibetiltPart.procs) -ProcId $VibeSourceProcId
    $tiltSource = Find-ProcById -Procs @($vibetiltPart.procs) -ProcId $TiltSourceProcId

    if ($null -eq $vibeSource) { throw "모션 공용화 실패: '$VibeSourceProcId'를 찾을 수 없습니다." }
    if ($null -eq $tiltSource) { throw "모션 공용화 실패: '$TiltSourceProcId'를 찾을 수 없습니다." }

    $procs = New-Object System.Collections.ArrayList
    foreach ($p in @($motionPart.procs)) {
        $procMap = [ordered]@{}
        foreach ($prop in $p.PSObject.Properties) {
            if ($prop.Name -ne "param") {
                $procMap[$prop.Name] = $prop.Value
            }
        }

        $sourceParam = $null
        if ($p.id -match "VIBE") {
            $sourceParam = $vibeSource.param
        }
        elseif ($p.id -match "TILT") {
            $sourceParam = $tiltSource.param
        }

        $procMap["param"] = Merge-Params -BaseParam $sourceParam -OverrideParam $p.param
        $null = $procs.Add([pscustomobject]$procMap)
    }

    Assert-UniqueValues -Label "motion.libraries.id" -Values @($libraries | ForEach-Object { $_.id })
    Assert-UniqueValues -Label "motion.project.procs.id" -Values @($procs | ForEach-Object { $_.id })

    return [ordered]@{
        libraries = @($libraries.ToArray())
        projects = @(
            [ordered]@{
                id = $motionPart.project.id
                name = $motionPart.project.name
                library_depencency = @($deps.ToArray())
                procs = @($procs.ToArray())
            }
        )
        active_project = $motionPart.project.id
    }
}

# 485 + DPM -> workspace.json
$ws485 = Build-Workspace -vibetilt $vt485 -dpmPart $dpm
$json485 = $ws485 | ConvertTo-Json -Depth 20
$outPath485 = Join-Path $scriptDir "workspace.json"
[System.IO.File]::WriteAllText($outPath485, $json485, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Generated: $outPath485"

# 232 + DPM -> workspace_232.json
$ws232 = Build-Workspace -vibetilt $vt232 -dpmPart $dpm
$json232 = $ws232 | ConvertTo-Json -Depth 20
$outPath232 = Join-Path $scriptDir "workspace_232.json"
[System.IO.File]::WriteAllText($outPath232, $json232, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Generated: $outPath232"

# Motion 485 + board VIBE/TILT 공용 파라미터 -> workspace_motion_485.json
$wsMotion485 = Build-MotionWorkspace -motionPart $motion485 -vibetiltPart $vt485 -VibeSourceProcId "VIBE_485" -TiltSourceProcId "TILT_485"
$jsonMotion485 = $wsMotion485 | ConvertTo-Json -Depth 20
$outPathMotion485 = Join-Path $scriptDir "workspace_motion_485.json"
[System.IO.File]::WriteAllText($outPathMotion485, $jsonMotion485, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Generated: $outPathMotion485"

# Motion 232 + board VIBE/TILT 공용 파라미터 -> workspace_motion_232.json
$wsMotion232 = Build-MotionWorkspace -motionPart $motion232 -vibetiltPart $vt232 -VibeSourceProcId "VIBE_232" -TiltSourceProcId "TILT_232"
$jsonMotion232 = $wsMotion232 | ConvertTo-Json -Depth 20
$outPathMotion232 = Join-Path $scriptDir "workspace_motion_232.json"
[System.IO.File]::WriteAllText($outPathMotion232, $jsonMotion232, (New-Object System.Text.UTF8Encoding $false))
Write-Host "Generated: $outPathMotion232"

Write-Host ""
Write-Host "Build complete!"
