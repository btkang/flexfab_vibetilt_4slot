#nullable enable
namespace MotionAngleApp;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private System.Windows.Forms.TableLayoutPanel _root;
    private System.Windows.Forms.TableLayoutPanel _statusPanel;
    private System.Windows.Forms.TableLayoutPanel _controlPanel;
    private System.Windows.Forms.Panel _logPanel;
    private System.Windows.Forms.RichTextBox _rtbLog;

    private System.Windows.Forms.Button _btnStartX;
    private System.Windows.Forms.Button _btnStartY;
    private System.Windows.Forms.Button _btnStop;
    private System.Windows.Forms.NumericUpDown _numMeasureSeconds;

    private System.Windows.Forms.Label _lblCurrentAngle;
    private System.Windows.Forms.Label _lblStatus;
    private System.Windows.Forms.Label _lblResolution;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _root = new TableLayoutPanel();
        _portPanel = new TableLayoutPanel();
        sensor1Group = new GroupBox();
        sensor1Layout = new TableLayoutPanel();
        _btnSensor1Send = new Button();
        _cbSensor1Port = new ComboBox();
        sensor1PortLabel = new Label();
        _tbSensor1Baud = new TextBox();
        sensor1BaudLabel = new Label();
        _btnSensor1Open = new Button();
        _tbSensor1Manual = new TextBox();
        sensor2Group = new GroupBox();
        sensor2Layout = new TableLayoutPanel();
        _cbSensor2Port = new ComboBox();
        sensor2PortLabel = new Label();
        _tbSensor2Baud = new TextBox();
        sensor2BaudLabel = new Label();
        _cbSensor2Mode = new ComboBox();
        sensor2ModeLabel = new Label();
        _btnSensor2Open = new Button();
        _tbSensor2Manual = new TextBox();
        _btnSensor2Send = new Button();
        motorGroup = new GroupBox();
        motorLayout = new TableLayoutPanel();
        _tbMotorIp = new TextBox();
        motorIpLabel = new Label();
        _tbMotorPort = new TextBox();
        motorPortLabel = new Label();
        _btnMotorOpen = new Button();
        _tbMotorManual = new TextBox();
        _btnMotorSend = new Button();
        motorQuickPanel = new TableLayoutPanel();
        _btnMotorErrorClear = new Button();
        _btnMotorZero = new Button();
        motorMoveStepPanel = new TableLayoutPanel();
        _btnMotorMoveStep = new Button();
        _tbMotorStep = new TextBox();
        _btnMotorOrigin = new Button();
        _btnRefreshPorts = new Button();
        _statusPanel = new TableLayoutPanel();
        anglePanel = new TableLayoutPanel();
        angleTitleLabel = new Label();
        _lblCurrentAngle = new Label();
        statusPanel = new TableLayoutPanel();
        statusTitleLabel = new Label();
        _lblStatus = new Label();
        resolutionPanel = new TableLayoutPanel();
        resolutionTitleLabel = new Label();
        _lblResolution = new Label();
        _controlPanel = new TableLayoutPanel();
        _btnStartX = new Button();
        _btnStartY = new Button();
        _btnStop = new Button();
        durationPanel = new FlowLayoutPanel();
        durationTitleLabel = new Label();
        _numMeasureSeconds = new NumericUpDown();
        _logPanel = new Panel();
        _rtbLog = new RichTextBox();
        _root.SuspendLayout();
        _portPanel.SuspendLayout();
        sensor1Group.SuspendLayout();
        sensor1Layout.SuspendLayout();
        sensor2Group.SuspendLayout();
        sensor2Layout.SuspendLayout();
        motorGroup.SuspendLayout();
        motorLayout.SuspendLayout();
        motorQuickPanel.SuspendLayout();
        motorMoveStepPanel.SuspendLayout();
        _statusPanel.SuspendLayout();
        anglePanel.SuspendLayout();
        statusPanel.SuspendLayout();
        resolutionPanel.SuspendLayout();
        _controlPanel.SuspendLayout();
        durationPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_numMeasureSeconds).BeginInit();
        _logPanel.SuspendLayout();
        SuspendLayout();
        // 
        // _root
        // 
        _root.ColumnCount = 1;
        _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _root.Controls.Add(_portPanel, 0, 0);
        _root.Controls.Add(_statusPanel, 0, 1);
        _root.Controls.Add(_controlPanel, 0, 2);
        _root.Controls.Add(_logPanel, 0, 3);
        _root.Dock = DockStyle.Fill;
        _root.Location = new Point(0, 0);
        _root.Name = "_root";
        _root.Padding = new Padding(8);
        _root.RowCount = 4;
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 360F));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90F));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _root.Size = new Size(1235, 714);
        _root.TabIndex = 0;
        // 
        // _portPanel
        // 
        _portPanel.ColumnCount = 3;
        _portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        _portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        _portPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        _portPanel.Controls.Add(sensor1Group, 0, 1);
        _portPanel.Controls.Add(sensor2Group, 1, 1);
        _portPanel.Controls.Add(motorGroup, 2, 1);
        _portPanel.Controls.Add(_btnRefreshPorts, 0, 2);
        _portPanel.Dock = DockStyle.Fill;
        _portPanel.Location = new Point(11, 11);
        _portPanel.Name = "_portPanel";
        _portPanel.RowCount = 3;
        _portPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
        _portPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _portPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        _portPanel.Size = new Size(1213, 354);
        _portPanel.TabIndex = 0;
        _portPanel.Paint += _portPanel_Paint;
        // 
        // sensor1Group
        // 
        sensor1Group.Controls.Add(sensor1Layout);
        sensor1Group.Dock = DockStyle.Fill;
        sensor1Group.Location = new Point(4, 12);
        sensor1Group.Margin = new Padding(4);
        sensor1Group.Name = "sensor1Group";
        sensor1Group.Size = new Size(392, 300);
        sensor1Group.TabIndex = 1;
        sensor1Group.TabStop = false;
        sensor1Group.Text = "상용센서";
        // 
        // sensor1Layout
        // 
        sensor1Layout.ColumnCount = 2;
        sensor1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 267F));
        sensor1Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sensor1Layout.Controls.Add(_btnSensor1Send, 1, 3);
        sensor1Layout.Controls.Add(_cbSensor1Port, 1, 0);
        sensor1Layout.Controls.Add(sensor1PortLabel, 0, 0);
        sensor1Layout.Controls.Add(_tbSensor1Baud, 1, 1);
        sensor1Layout.Controls.Add(sensor1BaudLabel, 0, 1);
        sensor1Layout.Controls.Add(_btnSensor1Open, 1, 2);
        sensor1Layout.Controls.Add(_tbSensor1Manual, 0, 3);
        sensor1Layout.Dock = DockStyle.Fill;
        sensor1Layout.Location = new Point(3, 22);
        sensor1Layout.Name = "sensor1Layout";
        sensor1Layout.Padding = new Padding(6);
        sensor1Layout.RowCount = 4;
        sensor1Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        sensor1Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        sensor1Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        sensor1Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sensor1Layout.Size = new Size(386, 275);
        sensor1Layout.TabIndex = 0;
        // 
        // _btnSensor1Send
        // 
        _btnSensor1Send.Location = new Point(276, 109);
        _btnSensor1Send.Name = "_btnSensor1Send";
        _btnSensor1Send.Size = new Size(101, 46);
        _btnSensor1Send.TabIndex = 6;
        _btnSensor1Send.Text = "Send";
        // 
        // _cbSensor1Port
        // 
        _cbSensor1Port.DropDownStyle = ComboBoxStyle.DropDownList;
        _cbSensor1Port.Location = new Point(276, 9);
        _cbSensor1Port.Name = "_cbSensor1Port";
        _cbSensor1Port.Size = new Size(101, 27);
        _cbSensor1Port.TabIndex = 1;
        // 
        // sensor1PortLabel
        // 
        sensor1PortLabel.Anchor = AnchorStyles.Left;
        sensor1PortLabel.Location = new Point(9, 6);
        sensor1PortLabel.Name = "sensor1PortLabel";
        sensor1PortLabel.Size = new Size(64, 28);
        sensor1PortLabel.TabIndex = 2;
        sensor1PortLabel.Text = "포트";
        sensor1PortLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _tbSensor1Baud
        // 
        _tbSensor1Baud.Location = new Point(276, 37);
        _tbSensor1Baud.Name = "_tbSensor1Baud";
        _tbSensor1Baud.Size = new Size(101, 26);
        _tbSensor1Baud.TabIndex = 3;
        // 
        // sensor1BaudLabel
        // 
        sensor1BaudLabel.Anchor = AnchorStyles.Left;
        sensor1BaudLabel.Location = new Point(9, 34);
        sensor1BaudLabel.Name = "sensor1BaudLabel";
        sensor1BaudLabel.Size = new Size(64, 28);
        sensor1BaudLabel.TabIndex = 4;
        sensor1BaudLabel.Text = "Baud";
        sensor1BaudLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _btnSensor1Open
        // 
        _btnSensor1Open.Location = new Point(276, 65);
        _btnSensor1Open.Name = "_btnSensor1Open";
        _btnSensor1Open.Size = new Size(101, 38);
        _btnSensor1Open.TabIndex = 4;
        _btnSensor1Open.Text = "Open";
        // 
        // _tbSensor1Manual
        // 
        _tbSensor1Manual.Location = new Point(9, 109);
        _tbSensor1Manual.Name = "_tbSensor1Manual";
        _tbSensor1Manual.Size = new Size(261, 26);
        _tbSensor1Manual.TabIndex = 5;
        // 
        // sensor2Group
        // 
        sensor2Group.Controls.Add(sensor2Layout);
        sensor2Group.Dock = DockStyle.Fill;
        sensor2Group.Location = new Point(404, 12);
        sensor2Group.Margin = new Padding(4);
        sensor2Group.Name = "sensor2Group";
        sensor2Group.Size = new Size(392, 300);
        sensor2Group.TabIndex = 2;
        sensor2Group.TabStop = false;
        sensor2Group.Text = "진동기울기센서";
        // 
        // sensor2Layout
        // 
        sensor2Layout.ColumnCount = 2;
        sensor2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 266F));
        sensor2Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        sensor2Layout.Controls.Add(_cbSensor2Port, 1, 0);
        sensor2Layout.Controls.Add(sensor2PortLabel, 0, 0);
        sensor2Layout.Controls.Add(_tbSensor2Baud, 1, 1);
        sensor2Layout.Controls.Add(sensor2BaudLabel, 0, 1);
        sensor2Layout.Controls.Add(_cbSensor2Mode, 1, 2);
        sensor2Layout.Controls.Add(sensor2ModeLabel, 0, 2);
        sensor2Layout.Controls.Add(_btnSensor2Open, 1, 3);
        sensor2Layout.Controls.Add(_tbSensor2Manual, 0, 4);
        sensor2Layout.Controls.Add(_btnSensor2Send, 1, 4);
        sensor2Layout.Dock = DockStyle.Fill;
        sensor2Layout.Location = new Point(3, 22);
        sensor2Layout.Name = "sensor2Layout";
        sensor2Layout.Padding = new Padding(6);
        sensor2Layout.RowCount = 5;
        sensor2Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        sensor2Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        sensor2Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        sensor2Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
        sensor2Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sensor2Layout.Size = new Size(386, 275);
        sensor2Layout.TabIndex = 0;
        // 
        // _cbSensor2Port
        // 
        _cbSensor2Port.DropDownStyle = ComboBoxStyle.DropDownList;
        _cbSensor2Port.Location = new Point(275, 9);
        _cbSensor2Port.Name = "_cbSensor2Port";
        _cbSensor2Port.Size = new Size(102, 27);
        _cbSensor2Port.TabIndex = 1;
        // 
        // sensor2PortLabel
        // 
        sensor2PortLabel.Anchor = AnchorStyles.Right;
        sensor2PortLabel.Location = new Point(9, 6);
        sensor2PortLabel.Name = "sensor2PortLabel";
        sensor2PortLabel.Size = new Size(260, 28);
        sensor2PortLabel.TabIndex = 2;
        sensor2PortLabel.Text = "포트";
        sensor2PortLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _tbSensor2Baud
        // 
        _tbSensor2Baud.Location = new Point(275, 37);
        _tbSensor2Baud.Name = "_tbSensor2Baud";
        _tbSensor2Baud.Size = new Size(102, 26);
        _tbSensor2Baud.TabIndex = 3;
        // 
        // sensor2BaudLabel
        // 
        sensor2BaudLabel.Anchor = AnchorStyles.Right;
        sensor2BaudLabel.Location = new Point(9, 34);
        sensor2BaudLabel.Name = "sensor2BaudLabel";
        sensor2BaudLabel.Size = new Size(260, 28);
        sensor2BaudLabel.TabIndex = 4;
        sensor2BaudLabel.Text = "Baud";
        sensor2BaudLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _cbSensor2Mode
        // 
        _cbSensor2Mode.DropDownStyle = ComboBoxStyle.DropDownList;
        _cbSensor2Mode.Location = new Point(275, 65);
        _cbSensor2Mode.Name = "_cbSensor2Mode";
        _cbSensor2Mode.Size = new Size(102, 27);
        _cbSensor2Mode.TabIndex = 4;
        // 
        // sensor2ModeLabel
        // 
        sensor2ModeLabel.Anchor = AnchorStyles.Right;
        sensor2ModeLabel.Location = new Point(9, 62);
        sensor2ModeLabel.Name = "sensor2ModeLabel";
        sensor2ModeLabel.Size = new Size(260, 28);
        sensor2ModeLabel.TabIndex = 7;
        sensor2ModeLabel.Text = "Mode";
        sensor2ModeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _btnSensor2Open
        // 
        _btnSensor2Open.Location = new Point(275, 93);
        _btnSensor2Open.Name = "_btnSensor2Open";
        _btnSensor2Open.Size = new Size(102, 41);
        _btnSensor2Open.TabIndex = 5;
        _btnSensor2Open.Text = "Open";
        // 
        // _tbSensor2Manual
        // 
        _tbSensor2Manual.Location = new Point(9, 140);
        _tbSensor2Manual.Name = "_tbSensor2Manual";
        _tbSensor2Manual.Size = new Size(260, 26);
        _tbSensor2Manual.TabIndex = 6;
        // 
        // _btnSensor2Send
        // 
        _btnSensor2Send.Location = new Point(275, 140);
        _btnSensor2Send.Name = "_btnSensor2Send";
        _btnSensor2Send.Size = new Size(102, 43);
        _btnSensor2Send.TabIndex = 7;
        _btnSensor2Send.Text = "Send";
        // 
        // motorGroup
        // 
        motorGroup.Controls.Add(motorLayout);
        motorGroup.Dock = DockStyle.Fill;
        motorGroup.Location = new Point(804, 12);
        motorGroup.Margin = new Padding(4);
        motorGroup.Name = "motorGroup";
        motorGroup.Size = new Size(405, 300);
        motorGroup.TabIndex = 3;
        motorGroup.TabStop = false;
        motorGroup.Text = "포트리모트";
        // 
        // motorLayout
        // 
        motorLayout.ColumnCount = 2;
        motorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248F));
        motorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        motorLayout.Controls.Add(_tbMotorIp, 1, 0);
        motorLayout.Controls.Add(motorIpLabel, 0, 0);
        motorLayout.Controls.Add(_tbMotorPort, 1, 1);
        motorLayout.Controls.Add(motorPortLabel, 0, 1);
        motorLayout.Controls.Add(_btnMotorOpen, 1, 2);
        motorLayout.Controls.Add(_tbMotorManual, 0, 3);
        motorLayout.Controls.Add(_btnMotorSend, 1, 3);
        motorLayout.Controls.Add(motorQuickPanel, 0, 4);
        motorLayout.Dock = DockStyle.Fill;
        motorLayout.Location = new Point(3, 22);
        motorLayout.Name = "motorLayout";
        motorLayout.Padding = new Padding(6);
        motorLayout.RowCount = 5;
        motorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        motorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        motorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        motorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        motorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        motorLayout.Size = new Size(399, 275);
        motorLayout.TabIndex = 0;
        // 
        // _tbMotorIp
        // 
        _tbMotorIp.Location = new Point(257, 9);
        _tbMotorIp.Name = "_tbMotorIp";
        _tbMotorIp.Size = new Size(133, 26);
        _tbMotorIp.TabIndex = 1;
        // 
        // motorIpLabel
        // 
        motorIpLabel.Anchor = AnchorStyles.Right;
        motorIpLabel.Location = new Point(9, 6);
        motorIpLabel.Name = "motorIpLabel";
        motorIpLabel.Size = new Size(242, 28);
        motorIpLabel.TabIndex = 2;
        motorIpLabel.Text = "IP";
        motorIpLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _tbMotorPort
        // 
        _tbMotorPort.Location = new Point(257, 37);
        _tbMotorPort.Name = "_tbMotorPort";
        _tbMotorPort.Size = new Size(133, 26);
        _tbMotorPort.TabIndex = 3;
        // 
        // motorPortLabel
        // 
        motorPortLabel.Anchor = AnchorStyles.Right;
        motorPortLabel.Location = new Point(9, 34);
        motorPortLabel.Name = "motorPortLabel";
        motorPortLabel.Size = new Size(242, 28);
        motorPortLabel.TabIndex = 4;
        motorPortLabel.Text = "Port";
        motorPortLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _btnMotorOpen
        // 
        _btnMotorOpen.Location = new Point(257, 65);
        _btnMotorOpen.Name = "_btnMotorOpen";
        _btnMotorOpen.Size = new Size(133, 42);
        _btnMotorOpen.TabIndex = 4;
        _btnMotorOpen.Text = "Connect";
        // 
        // _tbMotorManual
        // 
        _tbMotorManual.Location = new Point(9, 113);
        _tbMotorManual.Name = "_tbMotorManual";
        _tbMotorManual.Size = new Size(242, 26);
        _tbMotorManual.TabIndex = 5;
        // 
        // _btnMotorSend
        // 
        _btnMotorSend.Location = new Point(257, 113);
        _btnMotorSend.Name = "_btnMotorSend";
        _btnMotorSend.Size = new Size(133, 30);
        _btnMotorSend.TabIndex = 6;
        _btnMotorSend.Text = "Send";
        // 
        // motorQuickPanel
        // 
        motorQuickPanel.ColumnCount = 2;
        motorLayout.SetColumnSpan(motorQuickPanel, 2);
        motorQuickPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46.719162F));
        motorQuickPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53.280838F));
        motorQuickPanel.Controls.Add(_btnMotorErrorClear, 0, 0);
        motorQuickPanel.Controls.Add(_btnMotorZero, 0, 1);
        motorQuickPanel.Controls.Add(motorMoveStepPanel, 1, 1);
        motorQuickPanel.Controls.Add(_btnMotorOrigin, 1, 0);
        motorQuickPanel.Dock = DockStyle.Fill;
        motorQuickPanel.Location = new Point(9, 149);
        motorQuickPanel.Name = "motorQuickPanel";
        motorQuickPanel.RowCount = 2;
        motorQuickPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 44.3396225F));
        motorQuickPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55.6603775F));
        motorQuickPanel.Size = new Size(381, 117);
        motorQuickPanel.TabIndex = 7;
        // 
        // _btnMotorErrorClear
        // 
        _btnMotorErrorClear.Location = new Point(3, 3);
        _btnMotorErrorClear.Name = "_btnMotorErrorClear";
        _btnMotorErrorClear.Size = new Size(161, 41);
        _btnMotorErrorClear.TabIndex = 0;
        _btnMotorErrorClear.Text = "Error Clear";
        // 
        // _btnMotorZero
        // 
        _btnMotorZero.Location = new Point(3, 54);
        _btnMotorZero.Name = "_btnMotorZero";
        _btnMotorZero.Size = new Size(161, 43);
        _btnMotorZero.TabIndex = 2;
        _btnMotorZero.Text = "Move 0 Degree";
        // 
        // motorMoveStepPanel
        // 
        motorMoveStepPanel.ColumnCount = 2;
        motorMoveStepPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
        motorMoveStepPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        motorMoveStepPanel.Controls.Add(_btnMotorMoveStep, 1, 0);
        motorMoveStepPanel.Controls.Add(_tbMotorStep, 0, 0);
        motorMoveStepPanel.Dock = DockStyle.Fill;
        motorMoveStepPanel.Location = new Point(181, 54);
        motorMoveStepPanel.Name = "motorMoveStepPanel";
        motorMoveStepPanel.RowCount = 1;
        motorMoveStepPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        motorMoveStepPanel.Size = new Size(197, 60);
        motorMoveStepPanel.TabIndex = 3;
        // 
        // _btnMotorMoveStep
        // 
        _btnMotorMoveStep.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnMotorMoveStep.Location = new Point(91, 3);
        _btnMotorMoveStep.Name = "_btnMotorMoveStep";
        _btnMotorMoveStep.Size = new Size(103, 37);
        _btnMotorMoveStep.TabIndex = 4;
        _btnMotorMoveStep.Text = "Move Step";
        // 
        // _tbMotorStep
        // 
        _tbMotorStep.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
        _tbMotorStep.Location = new Point(3, 3);
        _tbMotorStep.Margin = new Padding(3, 3, 6, 3);
        _tbMotorStep.Name = "_tbMotorStep";
        _tbMotorStep.Size = new Size(79, 29);
        _tbMotorStep.TabIndex = 3;
        // 
        // _btnMotorOrigin
        // 
        _btnMotorOrigin.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnMotorOrigin.Location = new Point(272, 3);
        _btnMotorOrigin.Name = "_btnMotorOrigin";
        _btnMotorOrigin.Size = new Size(106, 41);
        _btnMotorOrigin.TabIndex = 1;
        _btnMotorOrigin.Text = "Move Origin";
        // 
        // _btnRefreshPorts
        // 
        _btnRefreshPorts.AutoSize = true;
        _btnRefreshPorts.Dock = DockStyle.Left;
        _btnRefreshPorts.Location = new Point(3, 319);
        _btnRefreshPorts.Name = "_btnRefreshPorts";
        _btnRefreshPorts.Size = new Size(120, 32);
        _btnRefreshPorts.TabIndex = 4;
        _btnRefreshPorts.Text = "포트 새로고침";
        // 
        // _statusPanel
        // 
        _statusPanel.ColumnCount = 3;
        _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
        _statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        _statusPanel.Controls.Add(anglePanel, 0, 0);
        _statusPanel.Controls.Add(statusPanel, 1, 0);
        _statusPanel.Controls.Add(resolutionPanel, 2, 0);
        _statusPanel.Dock = DockStyle.Fill;
        _statusPanel.Location = new Point(11, 371);
        _statusPanel.Name = "_statusPanel";
        _statusPanel.RowCount = 1;
        _statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _statusPanel.Size = new Size(1213, 84);
        _statusPanel.TabIndex = 1;
        // 
        // anglePanel
        // 
        anglePanel.ColumnCount = 1;
        anglePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        anglePanel.Controls.Add(angleTitleLabel, 0, 0);
        anglePanel.Controls.Add(_lblCurrentAngle, 0, 1);
        anglePanel.Dock = DockStyle.Fill;
        anglePanel.Location = new Point(3, 3);
        anglePanel.Name = "anglePanel";
        anglePanel.RowCount = 2;
        anglePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        anglePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        anglePanel.Size = new Size(394, 78);
        anglePanel.TabIndex = 0;
        // 
        // angleTitleLabel
        // 
        angleTitleLabel.Location = new Point(3, 0);
        angleTitleLabel.Name = "angleTitleLabel";
        angleTitleLabel.Size = new Size(388, 22);
        angleTitleLabel.TabIndex = 0;
        angleTitleLabel.Text = "현재 각도";
        angleTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _lblCurrentAngle
        // 
        _lblCurrentAngle.Font = new Font("맑은 고딕", 20F, FontStyle.Bold);
        _lblCurrentAngle.Location = new Point(3, 22);
        _lblCurrentAngle.Name = "_lblCurrentAngle";
        _lblCurrentAngle.Size = new Size(388, 56);
        _lblCurrentAngle.TabIndex = 1;
        // 
        // statusPanel
        // 
        statusPanel.ColumnCount = 1;
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        statusPanel.Controls.Add(statusTitleLabel, 0, 0);
        statusPanel.Controls.Add(_lblStatus, 0, 1);
        statusPanel.Dock = DockStyle.Fill;
        statusPanel.Location = new Point(403, 3);
        statusPanel.Name = "statusPanel";
        statusPanel.RowCount = 2;
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        statusPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        statusPanel.Size = new Size(394, 78);
        statusPanel.TabIndex = 1;
        // 
        // statusTitleLabel
        // 
        statusTitleLabel.Location = new Point(3, 0);
        statusTitleLabel.Name = "statusTitleLabel";
        statusTitleLabel.Size = new Size(388, 22);
        statusTitleLabel.TabIndex = 0;
        statusTitleLabel.Text = "현재 작업 상태";
        statusTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _lblStatus
        // 
        _lblStatus.Font = new Font("맑은 고딕", 20F, FontStyle.Bold);
        _lblStatus.Location = new Point(3, 22);
        _lblStatus.Name = "_lblStatus";
        _lblStatus.Size = new Size(388, 56);
        _lblStatus.TabIndex = 1;
        // 
        // resolutionPanel
        // 
        resolutionPanel.ColumnCount = 1;
        resolutionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        resolutionPanel.Controls.Add(resolutionTitleLabel, 0, 0);
        resolutionPanel.Controls.Add(_lblResolution, 0, 1);
        resolutionPanel.Dock = DockStyle.Fill;
        resolutionPanel.Location = new Point(803, 3);
        resolutionPanel.Name = "resolutionPanel";
        resolutionPanel.RowCount = 2;
        resolutionPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
        resolutionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        resolutionPanel.Size = new Size(407, 78);
        resolutionPanel.TabIndex = 2;
        // 
        // resolutionTitleLabel
        // 
        resolutionTitleLabel.Location = new Point(3, 0);
        resolutionTitleLabel.Name = "resolutionTitleLabel";
        resolutionTitleLabel.Size = new Size(401, 22);
        resolutionTitleLabel.TabIndex = 0;
        resolutionTitleLabel.Text = "모터 분해능";
        resolutionTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _lblResolution
        // 
        _lblResolution.Font = new Font("맑은 고딕", 20F, FontStyle.Bold);
        _lblResolution.Location = new Point(3, 22);
        _lblResolution.Name = "_lblResolution";
        _lblResolution.Size = new Size(401, 56);
        _lblResolution.TabIndex = 1;
        // 
        // _controlPanel
        // 
        _controlPanel.ColumnCount = 5;
        _controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        _controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245F));
        _controlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _controlPanel.Controls.Add(_btnStartX, 0, 0);
        _controlPanel.Controls.Add(_btnStartY, 1, 0);
        _controlPanel.Controls.Add(_btnStop, 2, 0);
        _controlPanel.Controls.Add(durationPanel, 3, 0);
        _controlPanel.Dock = DockStyle.Fill;
        _controlPanel.Location = new Point(11, 461);
        _controlPanel.Name = "_controlPanel";
        _controlPanel.RowCount = 1;
        _controlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _controlPanel.Size = new Size(1213, 84);
        _controlPanel.TabIndex = 2;
        // 
        // _btnStartX
        // 
        _btnStartX.Location = new Point(4, 4);
        _btnStartX.Margin = new Padding(4);
        _btnStartX.Name = "_btnStartX";
        _btnStartX.Size = new Size(112, 76);
        _btnStartX.TabIndex = 0;
        _btnStartX.Text = "X TEST START";
        // 
        // _btnStartY
        // 
        _btnStartY.Location = new Point(124, 4);
        _btnStartY.Margin = new Padding(4);
        _btnStartY.Name = "_btnStartY";
        _btnStartY.Size = new Size(112, 76);
        _btnStartY.TabIndex = 1;
        _btnStartY.Text = "Y TEST START";
        // 
        // _btnStop
        // 
        _btnStop.Location = new Point(244, 4);
        _btnStop.Margin = new Padding(4);
        _btnStop.Name = "_btnStop";
        _btnStop.Size = new Size(112, 76);
        _btnStop.TabIndex = 2;
        _btnStop.Text = "STOP";
        // 
        // durationPanel
        // 
        durationPanel.Controls.Add(durationTitleLabel);
        durationPanel.Controls.Add(_numMeasureSeconds);
        durationPanel.Location = new Point(363, 3);
        durationPanel.Name = "durationPanel";
        durationPanel.Size = new Size(239, 78);
        durationPanel.TabIndex = 3;
        durationPanel.WrapContents = false;
        // 
        // durationTitleLabel
        // 
        durationTitleLabel.AutoSize = true;
        durationTitleLabel.Location = new Point(3, 0);
        durationTitleLabel.Name = "durationTitleLabel";
        durationTitleLabel.Size = new Size(92, 19);
        durationTitleLabel.TabIndex = 0;
        durationTitleLabel.Text = "측정 시간(초)";
        durationTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _numMeasureSeconds
        // 
        _numMeasureSeconds.Location = new Point(101, 3);
        _numMeasureSeconds.Name = "_numMeasureSeconds";
        _numMeasureSeconds.Size = new Size(120, 26);
        _numMeasureSeconds.TabIndex = 1;
        // 
        // _logPanel
        // 
        _logPanel.Controls.Add(_rtbLog);
        _logPanel.Dock = DockStyle.Fill;
        _logPanel.Location = new Point(11, 551);
        _logPanel.Name = "_logPanel";
        _logPanel.Size = new Size(1213, 152);
        _logPanel.TabIndex = 3;
        // 
        // _rtbLog
        // 
        _rtbLog.Dock = DockStyle.Fill;
        _rtbLog.Location = new Point(0, 0);
        _rtbLog.Name = "_rtbLog";
        _rtbLog.ReadOnly = true;
        _rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical;
        _rtbLog.Size = new Size(1213, 152);
        _rtbLog.TabIndex = 0;
        _rtbLog.Text = "";
        _rtbLog.WordWrap = false;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(8F, 19F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1235, 714);
        Controls.Add(_root);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Motion Angle Test";
        _root.ResumeLayout(false);
        _portPanel.ResumeLayout(false);
        _portPanel.PerformLayout();
        sensor1Group.ResumeLayout(false);
        sensor1Layout.ResumeLayout(false);
        sensor1Layout.PerformLayout();
        sensor2Group.ResumeLayout(false);
        sensor2Layout.ResumeLayout(false);
        sensor2Layout.PerformLayout();
        motorGroup.ResumeLayout(false);
        motorLayout.ResumeLayout(false);
        motorLayout.PerformLayout();
        motorQuickPanel.ResumeLayout(false);
        motorMoveStepPanel.ResumeLayout(false);
        motorMoveStepPanel.PerformLayout();
        _statusPanel.ResumeLayout(false);
        anglePanel.ResumeLayout(false);
        statusPanel.ResumeLayout(false);
        resolutionPanel.ResumeLayout(false);
        _controlPanel.ResumeLayout(false);
        durationPanel.ResumeLayout(false);
        durationPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_numMeasureSeconds).EndInit();
        _logPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private TableLayoutPanel anglePanel;
    private TableLayoutPanel statusPanel;
    private TableLayoutPanel resolutionPanel;
    private FlowLayoutPanel durationPanel;
    private Label angleTitleLabel;
    private Label statusTitleLabel;
    private Label resolutionTitleLabel;
    private Label durationTitleLabel;
    private TableLayoutPanel _portPanel;
    private GroupBox sensor1Group;
    private TableLayoutPanel sensor1Layout;
    private Button _btnSensor1Send;
    private ComboBox _cbSensor1Port;
    private Label sensor1PortLabel;
    private TextBox _tbSensor1Baud;
    private Label sensor1BaudLabel;
    private Button _btnSensor1Open;
    private TextBox _tbSensor1Manual;
    private GroupBox sensor2Group;
    private TableLayoutPanel sensor2Layout;
    private ComboBox _cbSensor2Port;
    private Label sensor2PortLabel;
    private TextBox _tbSensor2Baud;
    private Label sensor2BaudLabel;
    private ComboBox _cbSensor2Mode;
    private Label sensor2ModeLabel;
    private Button _btnSensor2Open;
    private TextBox _tbSensor2Manual;
    private Button _btnSensor2Send;
    private GroupBox motorGroup;
    private TableLayoutPanel motorLayout;
    private TextBox _tbMotorIp;
    private Label motorIpLabel;
    private TextBox _tbMotorPort;
    private Label motorPortLabel;
    private Button _btnMotorOpen;
    private TextBox _tbMotorManual;
    private Button _btnMotorSend;
    private TableLayoutPanel motorQuickPanel;
    private TableLayoutPanel motorMoveStepPanel;
    private Button _btnMotorErrorClear;
    private Button _btnMotorOrigin;
    private Button _btnMotorZero;
    private TextBox _tbMotorStep;
    private Button _btnMotorMoveStep;
    private Button _btnRefreshPorts;
}
#nullable restore

