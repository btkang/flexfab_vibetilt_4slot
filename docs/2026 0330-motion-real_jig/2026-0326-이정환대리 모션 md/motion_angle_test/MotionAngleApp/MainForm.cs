using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionAngleApp;

#pragma warning disable CS8618, CS8602
public sealed partial class MainForm : Form
{
    private SerialPort? _sensor1;
    private SerialPort? _sensor2;
    private TcpClient? _motorClient;
    private NetworkStream? _motorStream;

    private readonly SemaphoreSlim _sensor1Lock = new(1, 1);
    private readonly SemaphoreSlim _sensor2Lock = new(1, 1);
    private readonly SemaphoreSlim _motorLock = new(1, 1);

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _sensor1RxCts;
    private CancellationTokenSource? _sensor2RxCts;
    private CancellationTokenSource? _motorRxCts;
    private CancellationTokenSource? _motorRxTimeoutCts;
    private int _motorTimeoutId;
    private string? _preferredSensor1Port;
    private string? _preferredSensor2Port;
    private TaskCompletionSource<string?>? _sensor1Pending;
    private TaskCompletionSource<string?>? _sensor2Pending;
    private TaskCompletionSource<string?>? _motorPending;
    private readonly object _sensor1PendingLock = new();
    private readonly object _sensor2PendingLock = new();
    private readonly object _motorPendingLock = new();
    private readonly StringBuilder _sensor1RxBuffer = new();
    private readonly StringBuilder _sensor2RxBuffer = new();
    private readonly object _sensor1RxBufferLock = new();
    private readonly object _sensor2RxBufferLock = new();
    private readonly object _sensor2LatestLock = new();
    private bool _isRunning;
    private bool _isManualMotorActionRunning;
    private double _motorResolution = FixedMotorResolution;
    private string? _currentTestOutputDir;
    private string? _latestSensor2Packet;
    private DateTime _latestSensor2PacketAtUtc;
    private readonly ConcurrentQueue<(DateTime Time, double Value)> _sensor2MeasureQueue = new();
    private volatile bool _sensor2Collecting;
    private Axis _sensor2CollectAxis;
    private string _sensor2CollectMode = Sensor2Mode232;

    private const string Sensor1Name = "상용센서";
    private const string Sensor2Name = "진동기울기센서";
    private const string Sensor2Mode232 = "232";
    private const string Sensor2Mode485 = "485";
    private const double FixedMotorResolution = 0.1125;
    private const string MotorErrorClearCommand = "PPC00";
    private const string MotorOriginCommand = "PMO011";
    private const int CommandTimeoutMs = 3000;
    private const int Sensor1MeasureIntervalMs = 500;
    private const int MaxCsvRowsPerFile = 1_000_000;
    private const int Sensor2StreamingWarmupMs = 500;
    private const int Sensor2LatestValueTimeoutMs = 3000;
    private const int StepPer10Deg = 88;
    private const int StepsToZeroFrom90 = 800;
    private const int SensorDiffErrorThreshold = 10;

    private enum Axis
    {
        X,
        Y
    }

    public MainForm()
    {
        InitializeComponent();
        MaximizeBox = true;

        _btnStartX.Click += async (_, _) => await StartTestAsync(Axis.X);
        _btnStartY.Click += async (_, _) => await StartTestAsync(Axis.Y);
        _btnStop.Click += (_, _) => StopTest();
        _btnRefreshPorts.Click += (_, _) => RefreshPorts();

        _btnSensor1Open.Click += (_, _) => ToggleSensor1();
        _btnSensor2Open.Click += (_, _) => ToggleSensor2();
        _btnMotorOpen.Click += (_, _) => ToggleMotor();

        _btnSensor1Send.Click += (_, _) => ManualSendSensor(
            _sensor1,
            Sensor1Name,
            _tbSensor1Manual.Text,
            _sensor1Lock,
            _sensor1PendingLock,
            () => _sensor1Pending,
            v => _sensor1Pending = v);
        _btnSensor2Send.Click += (_, _) => ManualSendSensor(
            _sensor2,
            Sensor2Name,
            _tbSensor2Manual.Text,
            _sensor2Lock,
            _sensor2PendingLock,
            () => _sensor2Pending,
            v => _sensor2Pending = v);
        _btnMotorSend.Click += (_, _) => ManualSendMotor(_tbMotorManual.Text);
        _btnMotorErrorClear.Click += async (_, _) => await ExecuteMotorQuickActionAsync("Error Clear", async ct =>
        {
            await SendMotorCommandAsync(MotorErrorClearCommand, ct);
        });
        _btnMotorOrigin.Click += async (_, _) => await ExecuteMotorQuickActionAsync("Move Origin", async ct =>
        {
            await SendMotorCommandAsync(MotorOriginCommand, ct);
        });
        _btnMotorZero.Click += async (_, _) => await ExecuteMotorQuickActionAsync("Move 0 Degree", async ct =>
        {
            await MoveStepsAsync(1250, ct);
        });
        _btnMotorMoveStep.Click += async (_, _) => await ExecuteMotorQuickActionAsync("Move Step", async ct =>
        {
            if (!int.TryParse(_tbMotorStep.Text, out var steps))
            {
                throw new InvalidOperationException("Move Step 값이 올바르지 않습니다.");
            }

            await MoveStepsAsync(steps, ct);
        });

        FormClosing += (_, _) =>
        {
            SaveSettings();
            Cleanup();
        };

        InitializeDefaults();
        LoadSettings();
        RefreshPorts();
        UpdatePortSettingsState();
        UpdateManualSendState();
        UpdateStatus("작업 준비");
        UpdateTestButtonsState();
        UpdateCurrentAngle(double.NaN);
        UpdateResolutionLabel();
        AppendLog("앱 시작");
    }

    private void InitializeDefaults()
    {
        _tbSensor1Baud.Text = "38400";
        _tbSensor2Baud.Text = "115200";
        _cbSensor2Mode.Items.Clear();
        _cbSensor2Mode.Items.AddRange(new object[] { Sensor2Mode232, Sensor2Mode485 });
        _cbSensor2Mode.SelectedItem = Sensor2Mode232;

        _tbMotorIp.Text = "100.100.100.70";
        _tbMotorPort.Text = "2233";
        _tbMotorStep.Text = "0";

        _numMeasureSeconds.Minimum = 1;
        _numMeasureSeconds.Maximum = (decimal)uint.MaxValue;
        _numMeasureSeconds.Value = 10;
    }

    private void RefreshPorts()
    {
        var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        _cbSensor1Port.Items.Clear();
        _cbSensor2Port.Items.Clear();
        _cbSensor1Port.Items.AddRange(ports);
        _cbSensor2Port.Items.AddRange(ports);

        if (ports.Length > 0)
        {
            var sensor1 = _preferredSensor1Port;
            var sensor2 = _preferredSensor2Port;
            _cbSensor1Port.SelectedItem = !string.IsNullOrWhiteSpace(sensor1) && ports.Contains(sensor1)
                ? sensor1
                : (ports.Contains("COM11") ? "COM11" : ports[0]);
            _cbSensor2Port.SelectedItem = !string.IsNullOrWhiteSpace(sensor2) && ports.Contains(sensor2)
                ? sensor2
                : (ports.Contains("COM4") ? "COM4" : ports[Math.Min(1, ports.Length - 1)]);
        }

        AppendLog($"포트 새로고침: {ports.Length}개");
        UpdatePortSettingsState();
    }

    private void ToggleSensor1()
    {
        if (_sensor1 is { IsOpen: true })
        {
            StopSensor1Receive();
            _sensor1.Close();
            _btnSensor1Open.Text = "Open";
            AppendLog($"{Sensor1Name} 포트 닫힘");
            UpdateManualSendState();
            UpdatePortSettingsState();
            return;
        }

        if (_cbSensor1Port.SelectedItem is not string port)
        {
            MessageBox.Show($"{Sensor1Name} 포트를 선택하세요.");
            return;
        }

        if (!int.TryParse(_tbSensor1Baud.Text, out var baud))
        {
            MessageBox.Show($"{Sensor1Name} Baud 값이 올바르지 않습니다.");
            return;
        }

        _sensor1 = new SerialPort(port, baud, Parity.None, 8, StopBits.One)
        {
            NewLine = "\r",
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
        try
        {
            _sensor1.Open();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            ShowPortInUseMessage($"{Sensor1Name} 포트", port, ex);
            return;
        }
        StartSensor1Receive();
        _btnSensor1Open.Text = "Close";
        AppendLog($"{Sensor1Name} 포트 열림: {port} / {baud}");
        UpdateManualSendState();
        UpdatePortSettingsState();
    }

    private void ToggleSensor2()
    {
        if (_sensor2 is { IsOpen: true })
        {
            _sensor2.Close();
            _btnSensor2Open.Text = "Open";
            AppendLog($"{Sensor2Name} 포트 닫힘");
            StopSensor2Receive();
            UpdateManualSendState();
            UpdatePortSettingsState();
            return;
        }

        if (_cbSensor2Port.SelectedItem is not string port)
        {
            MessageBox.Show($"{Sensor2Name} 포트를 선택하세요.");
            return;
        }

        if (!int.TryParse(_tbSensor2Baud.Text, out var baud))
        {
            MessageBox.Show($"{Sensor2Name} Baud 값이 올바르지 않습니다.");
            return;
        }

        _sensor2 = new SerialPort(port, baud, Parity.None, 8, StopBits.One)
        {
            NewLine = "\r",
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
        try
        {
            _sensor2.Open();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            ShowPortInUseMessage($"{Sensor2Name} 포트", port, ex);
            return;
        }
        StartSensor2Receive();
        _btnSensor2Open.Text = "Close";
        AppendLog($"{Sensor2Name} 포트 열림: {port} / {baud}");
        UpdateManualSendState();
        UpdatePortSettingsState();
    }

    private void ToggleMotor()
    {
        if (_motorClient is { Connected: true })
        {
            StopMotorReceive();
            _motorStream?.Dispose();
            _motorClient.Close();
            _motorClient = null;
            _motorStream = null;
            _btnMotorOpen.Text = "Connect";
            AppendLog("모터 연결 해제");
            UpdateManualSendState();
            UpdatePortSettingsState();
            return;
        }

        if (!int.TryParse(_tbMotorPort.Text, out var port))
        {
            MessageBox.Show("모터 포트가 올바르지 않습니다.");
            return;
        }

        _motorClient = new TcpClient();
        _motorClient.NoDelay = true;
        try
        {
            _motorClient.Connect(_tbMotorIp.Text, port);
        }
        catch (SocketException ex)
        {
            ShowTcpPortInUseMessage(_tbMotorIp.Text, port, ex);
            _motorClient.Close();
            _motorClient = null;
            return;
        }
        _motorStream = _motorClient.GetStream();
        _motorStream.ReadTimeout = 3000;
        _motorStream.WriteTimeout = 3000;
        StartMotorReceive();
        _btnMotorOpen.Text = "Disconnect";
        AppendLog($"모터 연결: {_tbMotorIp.Text}:{port}");
        AppendLog($"모터 상태: Connected={_motorClient.Connected}, CanWrite={_motorStream.CanWrite}, CanRead={_motorStream.CanRead}");
        UpdateManualSendState();
        UpdatePortSettingsState();
    }

    private void ManualSendSensor(
        SerialPort? port,
        string name,
        string text,
        SemaphoreSlim gate,
        object pendingLock,
        Func<TaskCompletionSource<string?>?> getPending,
        Action<TaskCompletionSource<string?>?> setPending)
    {
        if (port is null || !port.IsOpen)
        {
            MessageBox.Show("포트가 열려있지 않습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("보낼 내용을 입력하세요.");
            return;
        }

        gate.Wait();
        try
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (pendingLock)
            {
                getPending()?.TrySetCanceled();
                setPending(tcs);
            }

            port.DiscardInBuffer();
            port.Write(text + "\r");
            AppendLog($"{name} 송신: {text}");

            _ = Task.Run(async () =>
            {
                using var timeoutCts = new CancellationTokenSource(CommandTimeoutMs);
                try
                {
                    var result = await tcs.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
                    if (result is null)
                    {
                        AppendLog($"{name} 수신 없음 타임아웃");
                        ShowTimeoutMessage($"{name} 수신 없음 타임아웃");
                    }
                }
                catch (OperationCanceledException)
                {
                    AppendLog($"{name} 수신 없음 타임아웃");
                    ShowTimeoutMessage($"{name} 수신 없음 타임아웃");
                }
            });
        }
        finally
        {
            gate.Release();
        }
    }

    private void ManualSendMotor(string text)
    {
        if (_motorStream is null)
        {
            MessageBox.Show("모터가 연결되어 있지 않습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("보낼 내용을 입력하세요.");
            return;
        }

        var packet = BuildMotorPacket(text);
        var payload = Encoding.ASCII.GetBytes(packet);
        try
        {
            StartMotorReceiveTimeout();
            _motorStream.Write(payload, 0, payload.Length);
            AppendLog($"모터 송신: {FormatMotorPacket(text)}");
        }
        catch (Exception ex)
        {
            AppendLog($"모터 송신 오류: {ex.Message}");
            throw;
        }
    }

    private async Task StartTestAsync(Axis axis)
    {
        if (_isRunning)
        {
            return;
        }

        if (_sensor1 is null || !_sensor1.IsOpen || _sensor2 is null || !_sensor2.IsOpen || _motorStream is null)
        {
            MessageBox.Show("모든 포트를 Open/Connect 한 후 시작할 수 있습니다.");
            return;
        }

        _cts = new CancellationTokenSource();
        _isRunning = true;
        _motorResolution = FixedMotorResolution;
        _currentTestOutputDir = PrepareTestOutputDirectory();
        var completed = false;
        UpdateStatus("작업 중");
        UpdateTestButtonsState();
        UpdateManualSendState();
        AppendLog(axis == Axis.X ? "X 테스트 시작" : "Y 테스트 시작");
        AppendLog($"CSV 폴더: {_currentTestOutputDir}");

        try
        {
            await RunTestFlowAsync(axis, _cts.Token);
            completed = true;
        }
        catch (OperationCanceledException)
        {
            AppendLog("테스트 취소");
        }
        catch (Exception ex)
        {
            AppendLog($"오류: {ex.Message}");
            MessageBox.Show($"오류: {ex.Message}");
        }
        finally
        {
            await TryStopSensor2StreamingAsync();
            _isRunning = false;
            _currentTestOutputDir = null;
            UpdateTestButtonsState();
            UpdateManualSendState();
            UpdateStatus("작업 준비");
            AppendLog("테스트 종료");
            if (completed)
            {
                var axisName = axis == Axis.X ? "X축" : "Y축";
                MessageBox.Show($"{axisName} 측정을 완료했습니다.");
            }
        }
    }

    private async Task ExecuteMotorQuickActionAsync(string actionName, Func<CancellationToken, Task> action)
    {
        if (_isRunning)
        {
            MessageBox.Show("테스트 실행 중에는 수동 제어를 사용할 수 없습니다.");
            return;
        }

        if (_motorStream is null || _motorClient is not { Connected: true })
        {
            MessageBox.Show("모터가 연결되어 있지 않습니다.");
            return;
        }

        _isManualMotorActionRunning = true;
        UpdateManualSendState();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            AppendLog($"모터 수동 실행: {actionName}");
            await action(cts.Token);
            AppendLog($"모터 수동 완료: {actionName}");
        }
        catch (OperationCanceledException)
        {
            AppendLog($"모터 수동 타임아웃: {actionName}");
            ShowTimeoutMessage($"모터 수동 타임아웃: {actionName}");
        }
        catch (Exception ex)
        {
            AppendLog($"모터 수동 오류({actionName}): {ex.Message}");
            MessageBox.Show($"모터 수동 오류({actionName}): {ex.Message}");
        }
        finally
        {
            _isManualMotorActionRunning = false;
            UpdateManualSendState();
        }
    }

    private void StopTest()
    {
        _cts?.Cancel();
        UpdateStatus("작업 준비");
    }

    private async Task RunTestFlowAsync(Axis axis, CancellationToken ct)
    {
        var axisStepSign = axis == Axis.X ? -1 : 1;
        await ConfigureSensorDirectionAsync(axis, ct);
        await SendMotorCommandAsync("PMO011", ct);
        await Task.Delay(10000, ct);
        await MoveStepsAsync(1250, ct);
        await Task.Delay(1000, ct);

        await FineAdjustToTargetAsync(0, axis, 250, ct);
        UpdateResolutionLabel();
        AppendLog($"분해능: {_motorResolution:F4}");

        // 0도 보정 완료 후 OFFSET 설정
        var offsetResp = await SendAndReceiveRawAsync(_sensor2, Sensor2Name, "<OFFSET,1,1>",
            _sensor2Lock, _sensor2PendingLock, () => _sensor2Pending, v => _sensor2Pending = v, ct);
        AppendLog($"{Sensor2Name} OFFSET 응답: {offsetResp?.Trim()}");

        // OFFSET 후 스트리밍 시작
        await StartSensor2StreamingAsync(ct);

        await MeasureAngleAsync(0, axis, ct);

        for (var angle = 10; angle <= 90; angle += 10)
        {
            await MoveStepsAsync(axisStepSign * StepPer10Deg, ct);
            await Task.Delay(1000, ct);
            await FineAdjustToTargetAsync(angle, axis, 200, ct);
            await MeasureAngleAsync(angle, axis, ct);
        }

        await MoveStepsAsync(-axisStepSign * StepsToZeroFrom90, ct);
        await Task.Delay(1000, ct);
        await FineAdjustToTargetAsync(0, axis, 250, ct);

        for (var angle = -10; angle >= -90; angle -= 10)
        {
            await MoveStepsAsync(-axisStepSign * StepPer10Deg, ct);
            await Task.Delay(1000, ct);
            await FineAdjustToTargetAsync(angle, axis, 200, ct);
            await MeasureAngleAsync(angle, axis, ct);
        }

        await MoveStepsAsync(axisStepSign * StepsToZeroFrom90, ct);
    }

    private async Task DetermineMotorResolutionAsync(CancellationToken ct)
    {
        var baseAngle = await ReadSensor1Async(ct);
        await MoveStepsAsync(1, ct);
        var plus = await ReadSensor1Async(ct);

        await MoveStepsAsync(-2, ct);
        var minus = await ReadSensor1Async(ct);

        await MoveStepsAsync(1, ct);

        var step1 = Math.Abs(plus - baseAngle);
        var step2 = Math.Abs(baseAngle - minus);
        _ = (step1 + step2) / 2.0;
    }

    private async Task RecalculateMotorResolutionOnceAsync(CancellationToken ct)
    {
        var baseAngle = await ReadSensor1Async(ct);
        await MoveStepsAsync(1, ct);
        var plus = await ReadSensor1Async(ct);
        await MoveStepsAsync(-1, ct);

        var step = Math.Abs(plus - baseAngle);
        _ = step;
    }

    private async Task FineAdjustToTargetAsync(double targetAngle, Axis axis, int maxSteps, CancellationToken ct)
    {
        var isZero = Math.Abs(targetAngle) < 0.001;
        var strictTolerance = isZero ? 0.03 : 0.05;
        var relaxedTolerance = isZero ? 0.05 : 0.1;
        const double reverseThreshold = 0.02;
        var strictDeadline = DateTime.UtcNow.AddSeconds(10);
        var relaxedLogged = false;
        var stepDirectionFactor = 1;
        var axisStepSign = axis == Axis.X ? -1 : 1;
        var currentAngle = await ReadSensor1Async(ct);
        UpdateCurrentAngle(currentAngle);

        for (var i = 0; i < maxSteps; i++)
        {
            ct.ThrowIfCancellationRequested();
            var tolerance = DateTime.UtcNow <= strictDeadline ? strictTolerance : relaxedTolerance;
            if (!relaxedLogged && tolerance == relaxedTolerance)
            {
                relaxedLogged = true;
                AppendLog($"기준각도 {targetAngle:F2}도: 10초 초과로 허용오차 {relaxedTolerance} 적용");
            }

            var delta = targetAngle - currentAngle;
            if (Math.Abs(delta) <= tolerance)
            {
                await Task.Delay(1000, ct);
                var confirmAngle = await ReadSensor1Async(ct);
                UpdateCurrentAngle(confirmAngle);
                if (Math.Abs(targetAngle - confirmAngle) <= tolerance)
                {
                    return;
                }

                currentAngle = confirmAngle;
                await Task.Delay(100, ct);
                continue;
            }

            var previousAbsDelta = Math.Abs(delta);
            var step = (delta > 0 ? 1 : -1) * stepDirectionFactor * axisStepSign;
            await MoveStepsAsync(step, ct);
            await Task.Delay(1000, ct);

            currentAngle = await ReadSensor1Async(ct);
            UpdateCurrentAngle(currentAngle);

            var newAbsDelta = Math.Abs(targetAngle - currentAngle);
            if ((newAbsDelta - previousAbsDelta) > reverseThreshold && previousAbsDelta > tolerance)
            {
                stepDirectionFactor *= -1;
                AppendLog($"기준각도 {targetAngle:F2}도: 오차 증가 감지로 스텝 방향 반전");
            }

            await Task.Delay(100, ct);
        }
    }

    private async Task MeasureAngleAsync(int angle, Axis axis, CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        AppendLog($"측정 시작: {angle}도");
        var baseDir = _currentTestOutputDir ?? GetCsvDirectory();
        var fileStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var axisLabel = axis == Axis.X ? "X" : "Y";
        var s1FileBase = $"{Sensor1Name}_{axisLabel}_{angle}도_{fileStamp}";
        var s2FileBase = $"{Sensor2Name}_{axisLabel}_{angle}도_{fileStamp}";

        var durationSeconds = decimal.ToDouble(_numMeasureSeconds.Value);

        // 목표 수집 개수
        var sensor1TargetCount = Math.Max(1, (int)(durationSeconds * 1000.0 / Sensor1MeasureIntervalMs));
        var sensor2TargetCount = (int)(durationSeconds * 1000);

        var s1RowCount = 0;
        var s1FileIndex = 1;
        var sw1 = CreateCsvWriter(baseDir, s1FileBase, s1FileIndex);

        var s2RowCount = 0;
        var s2FileIndex = 1;
        var sw2 = CreateCsvWriter(baseDir, s2FileBase, s2FileIndex);

        // 센서2 큐 수집 시작
        while (_sensor2MeasureQueue.TryDequeue(out _)) { }
        _sensor2CollectAxis = axis;
        _sensor2CollectMode = GetSensor2Mode();
        _sensor2Collecting = true;

        var loopStart = DateTime.UtcNow;
        var marginDeadline = loopStart.AddSeconds(durationSeconds + 2);

        try
        {
            while (s1RowCount < sensor1TargetCount || s2RowCount < sensor2TargetCount)
            {
                ct.ThrowIfCancellationRequested();
                if (DateTime.UtcNow > marginDeadline) break;

                // 센서1: 스케줄에 맞춰 읽기
                if (s1RowCount < sensor1TargetCount)
                {
                    var scheduled = loopStart.AddMilliseconds(s1RowCount * Sensor1MeasureIntervalMs);
                    var wait = scheduled - DateTime.UtcNow;
                    if (wait > TimeSpan.Zero)
                    {
                        await Task.Delay(wait, ct);
                    }

                    var time = DateTime.Now.ToString("yyyyMMdd HH:mm:ss:fff", CultureInfo.InvariantCulture);
                    var s1 = await ReadSensor1Async(ct);
                    UpdateCurrentAngle(s1);

                    if (s1RowCount >= MaxCsvRowsPerFile)
                    {
                        sw1.Dispose();
                        s1FileIndex++;
                        sw1 = CreateCsvWriter(baseDir, s1FileBase, s1FileIndex);
                    }
                    sw1.WriteLine($"{time},{s1:F2}");
                    s1RowCount++;

                    // 센서2 큐 drain → CSV 기록 (목표 개수까지만)
                    s2RowCount = DrainSensor2Queue(sw2, ref s2FileIndex, s2RowCount, baseDir, s2FileBase, out sw2, out var lastS2, sensor2TargetCount);

                    // 센서 차이 검사 (최신 센서2 값 기준)
                    if (lastS2.HasValue && Math.Abs(s1 - lastS2.Value) >= SensorDiffErrorThreshold)
                    {
                        AppendLog($"센서 차이 오류: {s1:F2}, {lastS2.Value:F2}");
                        throw new InvalidOperationException("센서 간 차이가 10도 이상입니다.");
                    }
                }
                else
                {
                    // 센서1 완료, 센서2 목표 미달 → 추가 수집
                    await Task.Delay(50, ct);
                    s2RowCount = DrainSensor2Queue(sw2, ref s2FileIndex, s2RowCount, baseDir, s2FileBase, out sw2, out _, sensor2TargetCount);
                }
            }

            // 수집 중단 후 잔여 큐 비우기 (CSV 기록 안 함)
            _sensor2Collecting = false;
            while (_sensor2MeasureQueue.TryDequeue(out _)) { }
        }
        finally
        {
            _sensor2Collecting = false;
            sw1.Dispose();
            sw2.Dispose();
        }

        AppendLog($"측정 완료: {angle}도");
    }

    private int DrainSensor2Queue(StreamWriter currentWriter, ref int fileIndex, int rowCount,
        string baseDir, string fileBase, out StreamWriter writer, out double? lastValue,
        int maxTotalRows = int.MaxValue)
    {
        writer = currentWriter;
        lastValue = null;
        while (rowCount < maxTotalRows && _sensor2MeasureQueue.TryDequeue(out var item))
        {
            if (rowCount >= MaxCsvRowsPerFile)
            {
                writer.Dispose();
                fileIndex++;
                writer = CreateCsvWriter(baseDir, fileBase, fileIndex);
                rowCount = 0;
            }
            var timeStr = item.Time.ToString("yyyyMMdd HH:mm:ss:fff", CultureInfo.InvariantCulture);
            writer.WriteLine($"{timeStr},{item.Value:F2}");
            rowCount++;
            lastValue = item.Value;
        }
        return rowCount;
    }

    private static StreamWriter CreateCsvWriter(string baseDir, string fileBase, int fileIndex)
    {
        var suffix = fileIndex <= 1 ? "" : $"_{fileIndex}";
        var path = Path.Combine(baseDir, $"{fileBase}{suffix}.csv");
        var sw = new StreamWriter(path, false, Encoding.UTF8);
        sw.WriteLine("시간,측정값");
        return sw;
    }

    private async Task<double> ReadSensor1Async(CancellationToken ct)
    {
        if (_sensor1 is null)
        {
            throw new InvalidOperationException($"{Sensor1Name} 포트가 열려있지 않습니다.");
        }

        var resp = await SendAndReceiveRawAsync(_sensor1, Sensor1Name, "get---x", _sensor1Lock, _sensor1PendingLock, () => _sensor1Pending, v => _sensor1Pending = v, ct);
        var value = ParseSensor1Value(resp);
        return value;
    }

    private async Task<double> GetLatestSensor2ValueAsync(Axis axis, CancellationToken ct)
    {
        if (_sensor2 is null || !_sensor2.IsOpen)
        {
            throw new InvalidOperationException($"{Sensor2Name} 포트가 열려있지 않습니다.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(Sensor2LatestValueTimeoutMs);

        try
        {
            while (!timeoutCts.IsCancellationRequested)
            {
                string? packet;
                DateTime receivedAtUtc;
                lock (_sensor2LatestLock)
                {
                    packet = _latestSensor2Packet;
                    receivedAtUtc = _latestSensor2PacketAtUtc;
                }

                if (!string.IsNullOrWhiteSpace(packet) &&
                    (DateTime.UtcNow - receivedAtUtc).TotalMilliseconds <= Sensor2LatestValueTimeoutMs)
                {
                    return ParseSensor2Value(packet, axis);
                }

                await Task.Delay(10, timeoutCts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // timeout
        }

        AppendLog($"{Sensor2Name} 최신값 대기 타임아웃");
        ShowTimeoutMessage($"{Sensor2Name} 최신값 대기 타임아웃");
        throw new InvalidOperationException($"{Sensor2Name} 최신값 없음");
    }

    private double ParseSensor1Value(string? resp)
    {
        if (resp is null)
        {
            AppendLog($"{Sensor1Name} 수신 없음 타임아웃");
            ShowTimeoutMessage($"{Sensor1Name} 수신 없음 타임아웃");
            throw new InvalidOperationException($"{Sensor1Name} 응답 없음");
        }

        var hash = resp.IndexOf('#');
        if (hash >= 0)
        {
            resp = resp[..hash];
        }
        resp = resp.Trim();

        if (double.TryParse(resp, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"{Sensor1Name} 응답 파싱 실패: {resp}");
    }

    private double ParseSensor2Value(string? resp, Axis axis)
    {
        if (resp is null)
        {
            AppendLog($"{Sensor2Name} 수신 없음 타임아웃");
            ShowTimeoutMessage($"{Sensor2Name} 수신 없음 타임아웃");
            throw new InvalidOperationException($"{Sensor2Name} 응답 없음");
        }

        resp = resp.Trim();
        if (resp.StartsWith("[") && resp.EndsWith("]"))
        {
            resp = resp[1..^1];
        }

        var parts = resp.Split(',').Select(p => p.Trim()).ToArray();
        var mode = GetSensor2Mode();
        int xIndex, yIndex;

        if (mode == Sensor2Mode485)
        {
            // 485: [AN,1,X,Y] → 4 parts
            xIndex = 2;
            yIndex = 3;
        }
        else
        {
            // 232: [AN,X,Y] → 3 parts
            xIndex = 1;
            yIndex = 2;
        }

        if (parts.Length > yIndex)
        {
            var xText = parts[xIndex];
            var yText = parts[yIndex];

            if (!double.TryParse(xText, NumberStyles.Float, CultureInfo.InvariantCulture, out var xValue))
            {
                throw new InvalidOperationException($"{Sensor2Name} X 파싱 실패: {xText}");
            }

            if (!double.TryParse(yText, NumberStyles.Float, CultureInfo.InvariantCulture, out var yValue))
            {
                throw new InvalidOperationException($"{Sensor2Name} Y 파싱 실패: {yText}");
            }

            return axis == Axis.X ? xValue : yValue;
        }

        throw new InvalidOperationException($"{Sensor2Name} 응답 파싱 실패: {resp}");
    }

    private static double ParseSensor2ValueDirect(string resp, Axis axis, string mode)
    {
        resp = resp.Trim();
        if (resp.StartsWith("[") && resp.EndsWith("]"))
        {
            resp = resp[1..^1];
        }

        var parts = resp.Split(',').Select(p => p.Trim()).ToArray();
        int xIndex, yIndex;

        if (mode == Sensor2Mode485)
        {
            xIndex = 2;
            yIndex = 3;
        }
        else
        {
            xIndex = 1;
            yIndex = 2;
        }

        if (parts.Length > yIndex &&
            double.TryParse(parts[xIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var xValue) &&
            double.TryParse(parts[yIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var yValue))
        {
            return axis == Axis.X ? xValue : yValue;
        }

        throw new InvalidOperationException($"센서2 파싱 실패: {resp}");
    }

    private string GetSensor2Mode()
    {
        if (InvokeRequired)
        {
            return (string)Invoke(new Func<string>(GetSensor2Mode));
        }

        return _cbSensor2Mode.SelectedItem as string ?? Sensor2Mode232;
    }

    private string GetSensor2ReadCommand()
    {
        return GetSensor2Mode() == Sensor2Mode485 ? "<AN,1>" : "<AN>";
    }

    private string GetSensor2StopCommand()
    {
        return GetSensor2Mode() == Sensor2Mode485 ? "<STOP,1>" : "<STOP>";
    }

    private async Task<string?> SendSensorCommandAsync(
        SerialPort? port,
        string name,
        string command,
        SemaphoreSlim gate,
        object pendingLock,
        Func<TaskCompletionSource<string?>?> getPending,
        Action<TaskCompletionSource<string?>?> setPending,
        CancellationToken ct)
    {
        if (port is null || !port.IsOpen)
        {
            throw new InvalidOperationException($"{name} 포트가 열려있지 않습니다.");
        }

        await gate.WaitAsync(ct);
        Task<string?> responseTask;
        try
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (pendingLock)
            {
                getPending()?.TrySetCanceled();
                setPending(tcs);
            }

            port.DiscardInBuffer();
            port.Write(command + "\r");
            AppendLog($"{name} 송신: {command}");
            responseTask = tcs.Task;
        }
        finally
        {
            gate.Release();
        }

        return await responseTask.ConfigureAwait(false);
    }

    private async Task<string?> AwaitSensorResponseAsync(Task<string?> responseTask, string name, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(CommandTimeoutMs);
        try
        {
            var resp = await responseTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            if (resp is null)
            {
                AppendLog($"{name} 수신 없음 타임아웃");
                ShowTimeoutMessage($"{name} 수신 없음 타임아웃");
            }
            return resp;
        }
        catch (OperationCanceledException)
        {
            AppendLog($"{name} 수신 없음 타임아웃");
            ShowTimeoutMessage($"{name} 수신 없음 타임아웃");
            return null;
        }
    }

    private async Task SendSensorWithoutResponseAsync(
        SerialPort? port,
        string name,
        string command,
        SemaphoreSlim gate,
        CancellationToken ct)
    {
        if (port is null || !port.IsOpen)
        {
            throw new InvalidOperationException($"{name} 포트가 열려있지 않습니다.");
        }

        await gate.WaitAsync(ct);
        try
        {
            port.Write(command + "\r");
            AppendLog($"{name} 송신: {command}");
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task MoveStepsAsync(int steps, CancellationToken ct)
    {
        if (_motorStream is null)
        {
            throw new InvalidOperationException("모터가 연결되어 있지 않습니다.");
        }

        Task<string?> responseTask;
        await _motorLock.WaitAsync(ct);
        try
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_motorPendingLock)
            {
                _motorPending?.TrySetCanceled();
                _motorPending = tcs;
            }

            var cmd = BuildMotorCommand(steps);
            var bytes = Encoding.ASCII.GetBytes(cmd);
            try
            {
                StartMotorReceiveTimeout();
                _motorStream.Write(bytes, 0, bytes.Length);
                AppendLog($"모터 송신: {FormatMotorPacket(cmd[1..^1])}");
                responseTask = tcs.Task;
            }
            catch (Exception ex)
            {
                AppendLog($"모터 송신 오류: {ex.Message}");
                throw;
            }
            AppendLog($"모터 이동: {steps} step");
        }
        finally
        {
            _motorLock.Release();
        }

        await AwaitMotorResponseAsync(responseTask, ct);
    }

    private void StartSensor1Receive()
    {
        StopSensor1Receive();
        if (_sensor1 is null)
        {
            return;
        }

        lock (_sensor1RxBufferLock)
        {
            _sensor1RxBuffer.Clear();
        }

        _sensor1RxCts = new CancellationTokenSource();
        _ = Task.Run(() => SensorReceiveLoopAsync(_sensor1, Sensor1Name, _sensor1Lock, _sensor1RxCts.Token));
        AppendLog($"{Sensor1Name} 수신 루프 시작");
    }

    private void StopSensor1Receive()
    {
        try
        {
            _sensor1RxCts?.Cancel();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _sensor1RxCts?.Dispose();
            _sensor1RxCts = null;
        }
        AppendLog($"{Sensor1Name} 수신 루프 중지");
    }

    private void StartSensor2Receive()
    {
        StopSensor2Receive();
        if (_sensor2 is null)
        {
            return;
        }

        lock (_sensor2RxBufferLock)
        {
            _sensor2RxBuffer.Clear();
        }

        _sensor2RxCts = new CancellationTokenSource();
        _ = Task.Run(() => Sensor2ReceiveLoopAsync(_sensor2, _sensor2RxCts.Token));
        AppendLog($"{Sensor2Name} 수신 루프 시작");
    }

    private void StopSensor2Receive()
    {
        try
        {
            _sensor2RxCts?.Cancel();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _sensor2RxCts?.Dispose();
            _sensor2RxCts = null;
        }
        AppendLog($"{Sensor2Name} 수신 루프 중지");
    }

    private async Task Sensor2ReceiveLoopAsync(SerialPort port, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && port.IsOpen)
        {
            try
            {
                if (port.BytesToRead == 0)
                {
                    await Task.Delay(1, ct);
                    continue;
                }

                var buffer = new byte[Math.Min(4096, port.BytesToRead)];
                var read = port.Read(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var raw = Encoding.ASCII.GetString(buffer, 0, read);
                    ProcessSensor2Chunk(raw);
                }
            }
            catch (TimeoutException)
            {
                await Task.Delay(1, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (IOException) { break; }
            catch (InvalidOperationException) { break; }
        }
    }

    private async Task SensorReceiveLoopAsync(SerialPort port, string name, SemaphoreSlim gate, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && port.IsOpen)
        {
            try
            {
                if (port.BytesToRead == 0)
                {
                    await Task.Delay(20, ct);
                    continue;
                }

                var buffer = new byte[Math.Min(1024, port.BytesToRead)];
                var read = port.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    await Task.Delay(20, ct);
                    continue;
                }

                var raw = Encoding.ASCII.GetString(buffer, 0, read);
                ProcessSensorChunk(name, raw);
            }
            catch (TimeoutException)
            {
                await Task.Delay(20, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch (InvalidOperationException)
            {
                break;
            }
        }
    }

    private void ProcessSensorChunk(string name, string raw)
    {
        if (name == Sensor1Name)
        {
            ProcessSensor1Chunk(raw);
            return;
        }

        if (name == Sensor2Name)
        {
            ProcessSensor2Chunk(raw);
        }
    }

    private void ProcessSensor1Chunk(string raw)
    {
        lock (_sensor1RxBufferLock)
        {
            _sensor1RxBuffer.Append(raw);
            while (true)
            {
                var end = IndexOf(_sensor1RxBuffer, '\r');
                if (end < 0)
                {
                    break;
                }

                var packet = _sensor1RxBuffer.ToString(0, end).Trim();
                _sensor1RxBuffer.Remove(0, end + 1);
                if (packet.Length > 0)
                {
                    AppendLog($"{Sensor1Name} 수신: {packet}");
                    TryResolvePending(Sensor1Name, packet);
                }
            }

            // Direction command may return plain "OK" without '\r'.
            var buffered = _sensor1RxBuffer.ToString().Trim();
            if (buffered.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                _sensor1RxBuffer.Clear();
                AppendLog($"{Sensor1Name} 수신: OK");
                TryResolvePending(Sensor1Name, "OK");
            }
        }
    }

    private void ProcessSensor2Chunk(string raw)
    {
        lock (_sensor2RxBufferLock)
        {
            var hasPending = HasSensor2Pending();
            var handledPacket = false;
            _sensor2RxBuffer.Append(raw);
            while (true)
            {
                var start = IndexOf(_sensor2RxBuffer, '[');
                if (start < 0)
                {
                    if (hasPending && TryExtractSensor2LinePacket(out var linePacket))
                    {
                        HandleSensor2Packet(linePacket);
                        continue;
                    }

                    if (hasPending && !handledPacket && !string.IsNullOrWhiteSpace(raw))
                    {
                        var escaped = raw.Replace("\r", "\\r").Replace("\n", "\\n");
                        AppendLog($"{Sensor2Name} 수신(raw): {escaped}");
                    }

                    if (_sensor2RxBuffer.Length > 4096)
                    {
                        _sensor2RxBuffer.Clear();
                    }
                    break;
                }

                if (start > 0)
                {
                    _sensor2RxBuffer.Remove(0, start);
                }

                var end = IndexOf(_sensor2RxBuffer, ']');
                if (end < 0)
                {
                    break;
                }

                var packet = _sensor2RxBuffer.ToString(0, end + 1).Trim();
                _sensor2RxBuffer.Remove(0, end + 1);
                if (packet.Length > 0)
                {
                    handledPacket = true;
                    HandleSensor2Packet(packet);
                }
            }
        }
    }

    private bool HasSensor2Pending()
    {
        lock (_sensor2PendingLock)
        {
            return _sensor2Pending is not null;
        }
    }

    private bool TryExtractSensor2LinePacket(out string packet)
    {
        for (var i = 0; i < _sensor2RxBuffer.Length; i++)
        {
            if (_sensor2RxBuffer[i] is '\r' or '\n')
            {
                packet = _sensor2RxBuffer.ToString(0, i).Trim();
                _sensor2RxBuffer.Remove(0, i + 1);
                return packet.Length > 0;
            }
        }

        packet = string.Empty;
        return false;
    }

    private void HandleSensor2Packet(string packet)
    {
        var shouldLog = !_isRunning && !packet.Contains("AN", StringComparison.OrdinalIgnoreCase);
        if (!shouldLog)
        {
            shouldLog = HasSensor2Pending();
        }

        if (shouldLog)
        {
            AppendLog($"{Sensor2Name} 수신: {packet}");
        }

        lock (_sensor2LatestLock)
        {
            _latestSensor2Packet = packet;
            _latestSensor2PacketAtUtc = DateTime.UtcNow;
        }
        TryResolvePending(Sensor2Name, packet);

        if (_sensor2Collecting)
        {
            try
            {
                var value = ParseSensor2ValueDirect(packet, _sensor2CollectAxis, _sensor2CollectMode);
                _sensor2MeasureQueue.Enqueue((DateTime.Now, value));
            }
            catch
            {
                // 파싱 실패 패킷 무시
            }
        }
    }

    private static int IndexOf(StringBuilder source, char ch)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == ch)
            {
                return i;
            }
        }

        return -1;
    }

    private void TryResolvePending(string name, string packet)
    {
        if (name == Sensor1Name)
        {
            lock (_sensor1PendingLock)
            {
                _sensor1Pending?.TrySetResult(packet);
                _sensor1Pending = null;
            }
        }
        else if (name == Sensor2Name)
        {
            lock (_sensor2PendingLock)
            {
                _sensor2Pending?.TrySetResult(packet);
                _sensor2Pending = null;
            }
        }
    }

    private async Task<string?> SendAndReceiveRawAsync(
        SerialPort port,
        string name,
        string command,
        SemaphoreSlim gate,
        object pendingLock,
        Func<TaskCompletionSource<string?>?> getPending,
        Action<TaskCompletionSource<string?>?> setPending,
        CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (pendingLock)
            {
                getPending()?.TrySetCanceled();
                setPending(tcs);
            }

            port.DiscardInBuffer();
            port.Write(command + "\r");
            AppendLog($"{name} 송신: {command}");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(CommandTimeoutMs);
            await using var _ = timeoutCts.Token.Register(() => tcs.TrySetResult(null));

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task ConfigureSensorDirectionAsync(Axis axis, CancellationToken ct)
    {
        if (_sensor1 is null)
        {
            throw new InvalidOperationException($"{Sensor1Name} 포트가 열려있지 않습니다.");
        }

        var cmd = axis == Axis.X ? "setdir6" : "setdir5";
        var resp = await SendAndReceiveRawAsync(_sensor1, Sensor1Name, cmd, _sensor1Lock, _sensor1PendingLock, () => _sensor1Pending, v => _sensor1Pending = v, ct);
        if (resp is null)
        {
            AppendLog($"{Sensor1Name} 방향 설정 응답 없음");
            ShowTimeoutMessage($"{Sensor1Name} 방향 설정 응답 없음");
            throw new InvalidOperationException($"{Sensor1Name} 방향 설정 응답 없음");
        }

        if (!resp.Contains("OK", StringComparison.OrdinalIgnoreCase))
        {
            AppendLog($"{Sensor1Name} 방향 설정 실패: {resp.Trim()}");
            ShowTimeoutMessage($"{Sensor1Name} 방향 설정 실패: {resp.Trim()}");
            throw new InvalidOperationException($"{Sensor1Name} 방향 설정 실패: {resp.Trim()}");
        }
    }

    private async Task StartSensor2StreamingAsync(CancellationToken ct)
    {
        if (_sensor2 is null || !_sensor2.IsOpen)
        {
            throw new InvalidOperationException($"{Sensor2Name} 포트가 열려있지 않습니다.");
        }

        lock (_sensor2LatestLock)
        {
            _latestSensor2Packet = null;
            _latestSensor2PacketAtUtc = default;
        }

        var is485 = GetSensor2Mode() == Sensor2Mode485;

        // MODE 설정
        var modeCmd = is485 ? "<MODE,1,6>" : "<MODE,6>";
        await SendSensorWithoutResponseAsync(_sensor2, Sensor2Name, modeCmd, _sensor2Lock, ct);
        await Task.Delay(100, ct);

        // SAM 설정
        var samSetCmd = is485 ? "<SAM,1,1>" : "<SAM,1>";
        await SendSensorWithoutResponseAsync(_sensor2, Sensor2Name, samSetCmd, _sensor2Lock, ct);
        await Task.Delay(100, ct);

        // ATIM (485 only)
        if (is485)
        {
            await SendSensorWithoutResponseAsync(_sensor2, Sensor2Name, "<ATIM,1,0>", _sensor2Lock, ct);
            await Task.Delay(100, ct);
        }

        // START
        var startCmd = is485 ? "<START,1>" : "<START>";
        await SendSensorWithoutResponseAsync(_sensor2, Sensor2Name, startCmd, _sensor2Lock, ct);

        await Task.Delay(Sensor2StreamingWarmupMs, ct);
        _ = await GetLatestSensor2ValueAsync(Axis.X, ct);
        AppendLog($"{Sensor2Name} 스트리밍 시작");
    }

    private async Task TryStopSensor2StreamingAsync()
    {
        if (_sensor2 is null || !_sensor2.IsOpen)
        {
            return;
        }

        var stopCmd = GetSensor2StopCommand();
        using var cts = new CancellationTokenSource(CommandTimeoutMs);

        try
        {
            while (!cts.IsCancellationRequested)
            {
                await SendSensorWithoutResponseAsync(_sensor2, Sensor2Name, stopCmd, _sensor2Lock, cts.Token);
                await Task.Delay(10, cts.Token);

                string? packet;
                lock (_sensor2LatestLock)
                {
                    packet = _latestSensor2Packet;
                }

                if (packet is not null &&
                    (packet.Contains("[STOP,0,1]") || packet.Contains("[STOP,2,1]")))
                {
                    AppendLog($"{Sensor2Name} 스트리밍 종료 확인: {packet}");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog($"{Sensor2Name} STOP 응답 대기 타임아웃");
        }
        catch (Exception ex)
        {
            AppendLog($"{Sensor2Name} STOP 송신 실패: {ex.Message}");
        }
    }

    private async Task SendMotorCommandAsync(string command, CancellationToken ct)
    {
        if (_motorStream is null)
        {
            throw new InvalidOperationException("모터가 연결되어 있지 않습니다.");
        }

        Task<string?> responseTask;
        await _motorLock.WaitAsync(ct);
        try
        {
            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_motorPendingLock)
            {
                _motorPending?.TrySetCanceled();
                _motorPending = tcs;
            }

            var packet = BuildMotorPacket(command);
            var bytes = Encoding.ASCII.GetBytes(packet);
            StartMotorReceiveTimeout();
            _motorStream.Write(bytes, 0, bytes.Length);
            AppendLog($"모터 송신: {FormatMotorPacket(command)}");
            responseTask = tcs.Task;
        }
        finally
        {
            _motorLock.Release();
        }

        await AwaitMotorResponseAsync(responseTask, ct);
    }

    private void StartMotorReceive()
    {
        StopMotorReceive();
        if (_motorStream is null)
        {
            return;
        }

        _motorRxCts = new CancellationTokenSource();
        _ = Task.Run(() => MotorReceiveLoopAsync(_motorStream, _motorRxCts.Token));
        AppendLog("모터 수신 루프 시작");
    }

    private void StopMotorReceive()
    {
        try
        {
            _motorRxCts?.Cancel();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _motorRxCts?.Dispose();
            _motorRxCts = null;
        }
        AppendLog("모터 수신 루프 중지");
    }

    private async Task MotorReceiveLoopAsync(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new byte[1024];
        var packet = new StringBuilder();
        var inFrame = false;

        while (!ct.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }

            if (read <= 0)
            {
                await Task.Delay(20, ct);
                continue;
            }

            CancelMotorReceiveTimeout();

            var hadFrame = false;
            for (var i = 0; i < read; i++)
            {
                var b = buffer[i];
                if (b == 0x02)
                {
                    packet.Clear();
                    inFrame = true;
                    continue;
                }

                if (b == 0x03)
                {
                    if (inFrame)
                    {
                        var payload = packet.ToString();
                        AppendLog($"모터 수신: {FormatMotorPacket(payload)}");
                        CancelMotorReceiveTimeout();
                        TryResolveMotorPending(payload);
                        hadFrame = true;
                    }
                    inFrame = false;
                    packet.Clear();
                    continue;
                }

                if (inFrame)
                {
                    packet.Append((char)b);
                }
            }

            if (!hadFrame)
            {
                var raw = Encoding.ASCII.GetString(buffer, 0, read).Replace("\r", "\\r").Replace("\n", "\\n");
                AppendLog($"모터 수신: {raw}");
                CancelMotorReceiveTimeout();
            }
        }
    }

    private void StartMotorReceiveTimeout()
    {
        CancelMotorReceiveTimeout();
        var timeoutId = Interlocked.Increment(ref _motorTimeoutId);
        _motorRxTimeoutCts = new CancellationTokenSource();
        var token = _motorRxTimeoutCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(CommandTimeoutMs, token);
                if (timeoutId == _motorTimeoutId && !token.IsCancellationRequested)
                {
                    AppendLog("모터 수신 없음 타임아웃");
                    ShowTimeoutMessage("모터 수신 없음 타임아웃");
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        });
    }

    private void TryResolveMotorPending(string payload)
    {
        lock (_motorPendingLock)
        {
            _motorPending?.TrySetResult(payload);
            _motorPending = null;
        }
    }

    private async Task<string?> AwaitMotorResponseAsync(Task<string?> responseTask, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(CommandTimeoutMs);
        try
        {
            var resp = await responseTask.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            if (resp is null)
            {
                AppendLog("모터 수신 없음 타임아웃");
                ShowTimeoutMessage("모터 수신 없음 타임아웃");
            }
            return resp;
        }
        catch (OperationCanceledException)
        {
            AppendLog("모터 수신 없음 타임아웃");
            ShowTimeoutMessage("모터 수신 없음 타임아웃");
            return null;
        }
    }

    private void CancelMotorReceiveTimeout()
    {
        Interlocked.Increment(ref _motorTimeoutId);
        try
        {
            _motorRxTimeoutCts?.Cancel();
        }
        catch
        {
            // ignore
        }
        finally
        {
            _motorRxTimeoutCts?.Dispose();
            _motorRxTimeoutCts = null;
        }
    }

    private static string BuildMotorPacket(string payload)
    {
        return ((char)0x02) + payload + (char)0x03;
    }

    private static string FormatMotorPacket(string payload)
    {
        return $"[02]{payload}[03]";
    }

    private static string BuildMotorCommand(int steps)
    {
        var stepText = steps.ToString(CultureInfo.InvariantCulture);
        var len = 3 + stepText.Length;
        return BuildMotorPacket($"PMD0{len}1ML{stepText}");
    }

    private void UpdateCurrentAngle(double angle)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateCurrentAngle(angle));
            return;
        }

        _lblCurrentAngle.Text = double.IsNaN(angle) ? "-" : angle.ToString("F3", CultureInfo.InvariantCulture);
    }

    private void UpdateStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateStatus(text));
            return;
        }

        _lblStatus.Text = text;
    }

    private void UpdateResolutionLabel()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdateResolutionLabel);
            return;
        }

        _lblResolution.Text = _motorResolution.ToString("F4", CultureInfo.InvariantCulture);
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        var line = $"[{DateTime.Now:yy-MM-dd HH:mm:ss:fff}] {message}";
        _rtbLog.AppendText(line + Environment.NewLine);
        var lines = _rtbLog.Lines;
        if (lines.Length > 500)
        {
            _rtbLog.Lines = lines[^500..];
        }
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }

    private void ShowPortInUseMessage(string portLabel, string portName, Exception ex)
    {
        var owner = TryFindSerialPortOwner(portName);
        var ownerLine = owner is null ? "외부 프로그램: 확인 불가" : $"외부 프로그램: {owner}";
        MessageBox.Show($"{portLabel} 사용 중입니다.\n포트: {portName}\n{ownerLine}\n오류: {ex.Message}");
        AppendLog($"{portLabel} 사용 중: {portName}");
    }

    private void ShowTcpPortInUseMessage(string ip, int port, Exception ex)
    {
        var owner = TryFindTcpOwner(ip, port);
        var ownerLine = owner is null ? "외부 프로그램: 확인 불가" : $"외부 프로그램: {owner}";
        MessageBox.Show($"모터 연결 실패.\n대상: {ip}:{port}\n{ownerLine}\n오류: {ex.Message}");
        AppendLog($"모터 연결 실패: {ip}:{port}");
    }

    private static string? TryFindTcpOwner(string ip, int port)
    {
        try
        {
            var info = new ProcessStartInfo("netstat", "-ano -p tcp")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(info);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            var target = $"{ip}:{port}";
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                {
                    continue;
                }

                var remote = parts[2];
                var state = parts[3];
                var pidText = parts[4];
                if (!remote.EndsWith(target, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!int.TryParse(pidText, out var pid))
                {
                    continue;
                }

                try
                {
                    var proc = Process.GetProcessById(pid);
                    return $"{proc.ProcessName} (PID {pid})";
                }
                catch
                {
                    return $"PID {pid}";
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string? TryFindSerialPortOwner(string portName)
    {
        return null;
    }

    private void UpdatePortSettingsState()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdatePortSettingsState);
            return;
        }

        var sensor1Open = _sensor1 is { IsOpen: true };
        var sensor2Open = _sensor2 is { IsOpen: true };
        var motorConnected = _motorClient is { Connected: true };
        var allConnected = sensor1Open && sensor2Open && motorConnected;

        _cbSensor1Port.Enabled = !sensor1Open;
        _tbSensor1Baud.Enabled = !sensor1Open;
        _cbSensor2Port.Enabled = !sensor2Open;
        _tbSensor2Baud.Enabled = !sensor2Open;
        _cbSensor2Mode.Enabled = !sensor2Open;
        _tbMotorIp.Enabled = !motorConnected;
        _tbMotorPort.Enabled = !motorConnected;
        _btnRefreshPorts.Enabled = !allConnected;
    }

    private void UpdateManualSendState()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdateManualSendState);
            return;
        }

        var sendEnable = !_isManualMotorActionRunning;
        var controlEnable = !_isRunning && !_isManualMotorActionRunning;
        _btnSensor1Send.Enabled = sendEnable && _sensor1 is { IsOpen: true };
        _btnSensor2Send.Enabled = sendEnable && _sensor2 is { IsOpen: true };
        _btnMotorSend.Enabled = sendEnable && _motorStream is not null;
        _btnMotorErrorClear.Enabled = controlEnable && _motorStream is not null;
        _btnMotorOrigin.Enabled = controlEnable && _motorStream is not null;
        _btnMotorZero.Enabled = controlEnable && _motorStream is not null;
        _btnMotorMoveStep.Enabled = controlEnable && _motorStream is not null;
        _tbMotorStep.Enabled = controlEnable && _motorStream is not null;
    }

    private sealed class AppSettingsData
    {
        public string? Sensor1Port { get; set; }
        public string? Sensor2Port { get; set; }
        public string? Sensor1Baud { get; set; }
        public string? Sensor2Baud { get; set; }
        public string? Sensor2Mode { get; set; }
        public string? MotorIp { get; set; }
        public string? MotorPort { get; set; }
    }

    private static string GetSettingsPath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MotionAngleApp");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    private void LoadSettings()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            var data = JsonSerializer.Deserialize<AppSettingsData>(json);
            if (data is null)
            {
                return;
            }

            _preferredSensor1Port = data.Sensor1Port;
            _preferredSensor2Port = data.Sensor2Port;
            if (!string.IsNullOrWhiteSpace(data.Sensor1Baud))
            {
                _tbSensor1Baud.Text = data.Sensor1Baud;
            }
            if (!string.IsNullOrWhiteSpace(data.Sensor2Baud))
            {
                _tbSensor2Baud.Text = data.Sensor2Baud;
            }
            if (!string.IsNullOrWhiteSpace(data.Sensor2Mode) &&
                (data.Sensor2Mode == Sensor2Mode232 || data.Sensor2Mode == Sensor2Mode485))
            {
                _cbSensor2Mode.SelectedItem = data.Sensor2Mode;
            }
            if (!string.IsNullOrWhiteSpace(data.MotorIp))
            {
                _tbMotorIp.Text = data.MotorIp;
            }
            if (!string.IsNullOrWhiteSpace(data.MotorPort))
            {
                _tbMotorPort.Text = data.MotorPort;
            }
        }
        catch (Exception ex)
        {
            AppendLog($"설정 불러오기 실패: {ex.Message}");
        }
    }

    private void SaveSettings()
    {
        try
        {
            var data = new AppSettingsData
            {
                Sensor1Port = _cbSensor1Port.SelectedItem as string,
                Sensor2Port = _cbSensor2Port.SelectedItem as string,
                Sensor1Baud = _tbSensor1Baud.Text.Trim(),
                Sensor2Baud = _tbSensor2Baud.Text.Trim(),
                Sensor2Mode = _cbSensor2Mode.SelectedItem as string,
                MotorIp = _tbMotorIp.Text.Trim(),
                MotorPort = _tbMotorPort.Text.Trim()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetSettingsPath(), json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            AppendLog($"설정 저장 실패: {ex.Message}");
        }
    }

    private void ShowTimeoutMessage(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowTimeoutMessage(message));
            return;
        }

        MessageBox.Show(message);
    }

    private void UpdateTestButtonsState()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdateTestButtonsState);
            return;
        }

        var enable = !_isRunning;
        _btnStartX.Enabled = enable;
        _btnStartY.Enabled = enable;
        _btnStop.Enabled = !enable;
    }

    private void Cleanup()
    {
        try
        {
            StopSensor1Receive();
            StopSensor2Receive();
            StopMotorReceive();
            CancelMotorReceiveTimeout();
            _sensor1?.Close();
            _sensor2?.Close();
            _motorStream?.Dispose();
            _motorClient?.Close();
        }
        catch
        {
            // ignore
        }
    }

    private void _root_Paint(object? sender, PaintEventArgs e)
    {

    }

    private static string GetCsvDirectory()
    {
        var csvDir = Path.Combine(AppContext.BaseDirectory, "csv");
        Directory.CreateDirectory(csvDir);
        return csvDir;
    }

    private static string PrepareTestOutputDirectory()
    {
        var root = GetCsvDirectory();
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var dir = Path.Combine(root, stamp);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            return dir;
        }

        var index = 1;
        while (true)
        {
            var candidate = Path.Combine(root, $"{stamp}_{index}");
            if (!Directory.Exists(candidate))
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            index++;
        }
    }

    private void _portPanel_Paint(object? sender, PaintEventArgs e)
    {

    }
}
#pragma warning restore CS8618, CS8602
