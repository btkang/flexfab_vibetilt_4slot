# STOP 응답 드레인 수정 계획

## Context
RS-485 STOP 5x 전송 후 응답을 읽지 않아 잔류 응답이 다음 검사 명령에 혼입되는 문제.
퇴근 롱런 226회 테스트에서 Y축 82회차부터 열화 → 130회차 통신 두절 확인.
VL20 개발팀 유의사항에 "STOP 여러 번 쏘라"는 있지만, 응답 처리 가이드는 없었음.
STOP 5x @ 10ms 패턴 자체는 유지, 응답 드레인만 보강.

## 수정 대상 파일
- `ff_vibetilt/MotionJig.cs` — StopVlStreaming, VlClear, rollback_mode0
- `ff_vibetilt/VibeTilt.cs` — ClearCommBuffer

## 변경사항

### 1. StopVlStreaming에 드레인 추가 (MotionJig.cs:1567-1587)
STOP 5x → Sleep(100) → **VlDrain 추가** → MODE → VlRecv

```
변경 전: STOP×5 → Sleep(100) → MODE → VlRecv
변경 후: STOP×5 → Sleep(100) → VlDrain(200ms) → MODE → VlRecv
```

VlDrain: 200ms 타임아웃 내에서 **연속 3회 빈 응답**이면 종료. 잔류 STOP 응답 + 스트리밍 꼬리 제거.

### 2. VlDrain 메서드 신규 (MotionJig.cs, VlClear 근처 ~1842)
```csharp
private static void VlDrain(IComm vl, Action<string> log, int timeoutMs = 200)
{
    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
    int emptyCount = 0;
    while (emptyCount < 3 && DateTime.UtcNow < deadline)
    {
        var resp = vl.Recv(new Dictionary<string, object?>
        {
            { "timeout", 50 },
            { "recv_mode", "line" }
        });
        if (resp == null || !resp.TryGetValue("value", out var v) || string.IsNullOrWhiteSpace(v?.ToString()))
            emptyCount++;
        else
            emptyCount = 0;  // 데이터 있으면 카운터 리셋
    }
}
```

### 3. VlClear 강화 (MotionJig.cs:1842-1854)
- 기존: 첫 빈 응답에 즉시 break
- 변경: **연속 2회 빈 응답**이면 break, maxAttempts 5→10

### 4. ClearCommBuffer 강화 (VibeTilt.cs:498-514)
- VlClear와 동일 패턴 적용: 연속 2회 빈 응답에 break, maxAttempts 5→10

### 5. rollback_mode0 수정 (MotionJig.cs:552-561)
- 기존: 수동 STOP 1회 + Sleep + MODE (VlRecv 없음)
- 변경: `StopVlStreaming(vl, isRs485, logAction)` 호출로 대체
  → 드레인 + MODE + VlRecv 모두 포함

## 적용 범위
- VlDrain, StopVlStreaming: **485만** (232는 단일 STOP이라 불필요)
- VlClear, ClearCommBuffer: **485+232 공용** (232 부작용 없음)
- rollback_mode0: **485+232** (StopVlStreaming이 내부 분기)

## 영향
- 테스트 1회 소요시간: ~150-200ms 증가 (STOP당, 총 2-3회/라운드)
- 232 모드: 변경 없음
- STOP 5x @ 10ms 패턴: 변경 없음 (개발팀 권장 유지)
- 양산 영향: 없음 (매 보드 전원 리셋, UART 누적 불가)

## 검증 방법
1. Debug 빌드 후 485 테스트 10회 반복 → 정상 확인
2. 퇴근 롱런 300회 재시도 → Y축 82회 이상 통신 유지 확인
