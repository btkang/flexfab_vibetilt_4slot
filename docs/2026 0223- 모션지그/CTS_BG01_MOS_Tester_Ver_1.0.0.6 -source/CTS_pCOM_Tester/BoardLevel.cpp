// BoardLevel.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "BoardLevel.h"
#include "afxdialogex.h"

enum{
	BITCONTROL,
	USERSERIAL,
	TXRSSI_MNTVOLTAGE,
	SRAM,
	FLASH,
	IDCHGTARGET,
	IDCHGMASTER,
	RF2GFULLPAYRX,
	RF2GFULLPAYTX,
	PLCREGISTER,
	MPU6050,
	BOARDINPUTPORT,
	IOCHK,
	INFLASH,
	PLC_10_5MHZ,
	PLC_13MHZ,
	LEDON,
	LEDOFF,
};

#define HIMETRIC_INCH       2540
#define YPOS_OFFSET			65
#define CMD_DELAY			25
#define SRAM_DELAY			3000
#define FLASH_DELAY			42000
#define INFLASH_DELAY		14000
// CBoardLevel dialog

CCTS_pCOM_TesterApp *g_pApp_BoardLevel;
CFont g_editFont_BoardLevel;
CFont g_editRfidFont;
CRect g_rcClientDecisionDlg_BoadrdLevel;

IMPLEMENT_DYNAMIC(CBoardLevel, CDialogEx)

CBoardLevel::CBoardLevel(CWnd* pParent /*=NULL*/)
	: CDialogEx(CBoardLevel::IDD, pParent)
	, m_b8bits(FALSE)
{
	g_pApp_BoardLevel = (CCTS_pCOM_TesterApp *)AfxGetApp();
}

CBoardLevel::~CBoardLevel()
{
}

void CBoardLevel::DoDataExchange(CDataExchange* pDX)
{
	// TODO: Add your specialized code here and/or call the base class

	CDialogEx::DoDataExchange(pDX);

	DDX_Control(pDX, IDC_CHECK1, m_cUserSerial);
	DDX_Control(pDX, IDC_CHECK2, m_cLedon);
	DDX_Control(pDX, IDC_CHECK3, m_cLedoff);
	DDX_Control(pDX, IDC_CHECK4, m_cIoTest);
	DDX_Control(pDX, IDC_CHECK5, m_cFlashTest);
	DDX_Control(pDX, IDC_CHECK10, m_c2gFullPayRx);
	DDX_Control(pDX, IDC_CHECK12, m_c2gFullPayTx);

	DDX_Control(pDX, IDC_TEST_RESULT, m_EditDataMonitor);
	DDX_Control(pDX, IDC_LEDON, m_BtnLedon);
	DDX_Control(pDX, IDC_LEDOFF, m_BtnLedoff);
	DDX_Control(pDX, IDC_IO_TEST, m_BtnIoTest);
	DDX_Control(pDX, IDC_FLASH_TEST, m_BtnFlashTest);
	DDX_Control(pDX, IDC_AUTO_START, m_BtnAutoStart);
	DDX_Control(pDX, IDC_AUTO_STOP, m_BtnAutoStop);
	DDX_Control(pDX, IDC_2GFULL_PAY, m_Btn2gFullPayRx);
	DDX_Control(pDX, IDC_2GFULL_PAY_TX, m_Btn2gFullPayTx);

	DDX_Control(pDX, IDC_USER_SERIAL, m_BtnUserSerial);
	DDX_Control(pDX, IDC_RSSI_VOLTAGE, m_BtnRssiVolt);
	DDX_Control(pDX, IDC_SRAM, m_BtnSram);
	DDX_Control(pDX, IDC_PLC_IC, m_BtnPlcIc);
	DDX_Control(pDX, IDC_MPU6050, m_BtnMpu6050);
	DDX_Control(pDX, IDC_INPUT_PORT, m_BtnInputPort);
	DDX_Control(pDX, IDC_CHECK6, m_cRssiVolt);
	DDX_Control(pDX, IDC_CHECK7, m_cPlcIc);
	DDX_Control(pDX, IDC_CHECK8, m_cMpu);
	DDX_Control(pDX, IDC_CHECK11, m_cInputPort);
	DDX_Control(pDX, IDC_PROGRESS1, m_progress);
	DDX_Control(pDX, IDC_CHECK13, m_cInFlashTest);
	DDX_Control(pDX, IDC_INFLASH_TEST, m_BtnInflashTest);
	DDX_Control(pDX, IDC_CHECK9, m_cSramTest);
	DDX_Control(pDX, IDC_CHECK14, m_cPlc10mhz);
	DDX_Control(pDX, IDC_CHECK15, m_cPlc13mhz);
	DDX_Control(pDX, IDC_PLC10MHZ, m_BtnPlc10mhz);
	DDX_Control(pDX, IDC_PLC13MHZ, m_BtnPlc13mhz);
	DDX_Control(pDX, IDC_MANUAL_PAYLOAD, m_btn32bytePayload);
	DDX_Check(pDX, IDC_CHECK16, m_b8bits);
	DDX_Control(pDX, IDC_CHECK16, m_chk8bits);
}


BEGIN_MESSAGE_MAP(CBoardLevel, CDialogEx)
	ON_BN_CLICKED(IDC_AUTO_START, &CBoardLevel::OnBnClickedAutoStart)
	ON_BN_CLICKED(IDC_AUTO_STOP, &CBoardLevel::OnBnClickedAutoStop)
	ON_BN_CLICKED(IDC_LEDON, &CBoardLevel::OnBnClickedLedon)
	ON_BN_CLICKED(IDC_LEDOFF, &CBoardLevel::OnBnClickedLedoff)
	ON_BN_CLICKED(IDC_IO_TEST, &CBoardLevel::OnBnClickedIoTest)
	ON_BN_CLICKED(IDC_FLASH_TEST, &CBoardLevel::OnBnClickedFlashTest)
	ON_BN_CLICKED(IDC_2GFULL_PAY, &CBoardLevel::OnBnClicked2gfullPay)
	ON_BN_CLICKED(IDC_2GFULL_PAY_TX, &CBoardLevel::OnBnClicked2gfullPayTx)
	ON_WM_CTLCOLOR()
	ON_BN_CLICKED(IDC_USER_SERIAL, &CBoardLevel::OnBnClickedUserSerial)
	ON_BN_CLICKED(IDC_RSSI_VOLTAGE, &CBoardLevel::OnBnClickedRssiVoltage)
	ON_BN_CLICKED(IDC_SRAM, &CBoardLevel::OnBnClickedSram)
	ON_BN_CLICKED(IDC_PLC_IC, &CBoardLevel::OnBnClickedPlcIc)
	ON_BN_CLICKED(IDC_MPU6050, &CBoardLevel::OnBnClickedMpu6050)
	ON_BN_CLICKED(IDC_INPUT_PORT, &CBoardLevel::OnBnClickedInputPort)
	ON_BN_CLICKED(IDC_INFLASH_TEST, &CBoardLevel::OnBnClickedInflashTest)
	ON_BN_CLICKED(IDC_PLC10MHZ, &CBoardLevel::OnBnClickedPlc10mhz)
	ON_BN_CLICKED(IDC_PLC13MHZ, &CBoardLevel::OnBnClickedPlc13mhz)
	ON_BN_CLICKED(IDC_MANUAL_PAYLOAD, &CBoardLevel::OnBnClickedManualPayload)
END_MESSAGE_MAP()


BOOL CBoardLevel::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	g_editFont_BoardLevel.CreatePointFont(500, TEXT("굴림"));
	m_EditDataMonitor.SetFont(&g_editFont_BoardLevel, TRUE);

	m_cUserSerial.EnableWindow(FALSE);
	m_cLedon.EnableWindow(FALSE);
	m_cLedoff.EnableWindow(FALSE);
	m_cIoTest.EnableWindow(FALSE);
	m_cFlashTest.EnableWindow(FALSE);
	m_c2gFullPayRx.EnableWindow(FALSE);
	m_c2gFullPayTx.EnableWindow(FALSE);
	m_cRssiVolt.EnableWindow(FALSE);
	m_cPlcIc.EnableWindow(FALSE);
	m_cMpu.EnableWindow(FALSE);
	m_cInputPort.EnableWindow(FALSE);
	m_cInFlashTest.EnableWindow(FALSE);
	m_cSramTest.EnableWindow(FALSE);
	m_cPlc10mhz.EnableWindow(FALSE);
	m_cPlc13mhz.EnableWindow(FALSE);

	m_Btn2gFullPayRx.EnableWindow(FALSE);
	m_Btn2gFullPayTx.EnableWindow(FALSE); 

	m_btn32bytePayload.EnableWindow(FALSE);

	m_cUserSerial.SetCheck(0);
	m_cLedon.SetCheck(0);
	m_cLedoff.SetCheck(0);
	m_cIoTest.SetCheck(0);
	m_cFlashTest.SetCheck(0);
	m_c2gFullPayRx.SetCheck(1);
	m_c2gFullPayTx.SetCheck(1);
	m_cRssiVolt.SetCheck(0);
	m_cPlcIc.SetCheck(0);
	m_cMpu.SetCheck(0);
	m_cInputPort.SetCheck(0);
	m_cInFlashTest.SetCheck(0);
	m_cSramTest.SetCheck(0);
	m_cPlc10mhz.SetCheck(0);
	m_cPlc13mhz.SetCheck(0);
	m_chk8bits.SetCheck(1);
	// added
	m_pDlgDecision = new CDecisionDlg();// decision
	m_pDlgDecision->Create(IDD_DECISION, this);// decision
	VERIFY(m_pDlgDecision);
	m_pDlgDecision->GetClientRect(g_rcClientDecisionDlg_BoadrdLevel); // decision 최초 생성시 창 크기를 기억한다.

	//hbit = ::LoadBitmap(AfxGetInstanceHandle(), MAKEINTRESOURCE(IDB_BITMAPPASS));

	return TRUE;  // return TRUE unless you set the focus to a control
	// 예외: OCX 속성 페이지는 FALSE를 반환해야 합니다.
}


void CBoardLevel::SendData(CString SendCmd)
{
	g_pApp_BoardLevel->SendDataToEditControl(SendCmd, &g_pApp_BoardLevel->m_ComuPort);
}


void CBoardLevel::SendDataMaster(CString SendCmd)
{
	g_pApp_BoardLevel->SendDataToEditControlMaster(SendCmd, &g_pApp_BoardLevel->m_ComuPortMaster);
}


void CBoardLevel::ReadData(int DelayTime)
{
	g_pApp_BoardLevel->ReadDataToEditControl(DelayTime, &g_pApp_BoardLevel->m_ComuPort);
}


void CBoardLevel::ReadDataMaster(int DelayTime)
{
	g_pApp_BoardLevel->ReadDataToEditControlMaster(DelayTime, &g_pApp_BoardLevel->m_ComuPortMaster);
}


void CBoardLevel::SendDataUser(CString SendCmd)
{
	g_pApp_BoardLevel->SendDataToEditControl2(SendCmd, &g_pApp_BoardLevel->m_ComuPort2);
}


void CBoardLevel::ReadDataUser(int DelayTime)
{
	g_pApp_BoardLevel->ReadDataToEditControl2(DelayTime, &g_pApp_BoardLevel->m_ComuPort2);
}

void CBoardLevel::processdelay(DWORD dat)
{
	DWORD tick;
	int ProgressVal = dat;

	m_progress.SetRange(0, ProgressVal);

	tick = GetTickCount();
	while (GetTickCount() - tick <= dat)
	{
		m_progress.SetPos(GetTickCount() - tick);
		Sleep(0);
	}
	m_progress.SetPos(dat);
}


CString CBoardLevel::ConverToHex(CString data)
{
	unsigned char checkSum = 0;
	int value = 0;
	CString returnvalue;
	for (int x = 0; x < data.GetLength(); x++)
	{
		checkSum += (int)(data[x]);
	}
	returnvalue.Format("%02X", checkSum);
	return returnvalue;
}


UINT ProgressThread(LPVOID lParam)
{
	CBoardLevel *pCBoardLevel;
	pCBoardLevel = (CBoardLevel*)lParam;

	int ProgressVal = 0;
	switch (pCBoardLevel->m_iTestStep)
	{
	case USERSERIAL:		ProgressVal = 20 / 20;				break;
	case TXRSSI_MNTVOLTAGE:	ProgressVal = CMD_DELAY * 6 / 20;	break;
	case SRAM:				ProgressVal = SRAM_DELAY / 20;		break;
	case FLASH:				ProgressVal = FLASH_DELAY / 20;		break;
	case RF2GFULLPAYRX:		ProgressVal = 5000 / 20;			break;
	case RF2GFULLPAYTX:		ProgressVal = 5000 / 20;			break;
	case PLCREGISTER:		ProgressVal = CMD_DELAY * 6 / 20;	break;
	case MPU6050:			ProgressVal = CMD_DELAY * 14 / 20;	break;
	case BOARDINPUTPORT:	ProgressVal = CMD_DELAY / 20;		break;
	case IOCHK:				ProgressVal = CMD_DELAY * 6 / 20;	break;
	case INFLASH:			ProgressVal = INFLASH_DELAY / 20;	break;
	case PLC_10_5MHZ:		ProgressVal = 1000 / 20;			break;
	case PLC_13MHZ:			ProgressVal = 1000 / 20;			break;
	}

	pCBoardLevel->m_progress.SetRange(0, ProgressVal);

	for (int i = 0; i <= ProgressVal; i++)
	{
		pCBoardLevel->processdelay(20);
		pCBoardLevel->m_progress.SetPos(i);
	}
	return 0;
}

void CBoardLevel::OnBnClickedLedon()
{
	m_bEnterTheTest = FALSE;

	SendData(_T("<TEST=10DE>"));
}


void CBoardLevel::OnBnClickedLedoff()
{
	m_bEnterTheTest = FALSE;

	SendData(_T("<TEST=11DF>"));
}


void CBoardLevel::OnBnClickedIoTest()
{
	//I/O TEST
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=9B6>"));
	processdelay(CMD_DELAY * 6);
	ReadData(1);
}


void CBoardLevel::OnBnClickedFlashTest()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=4B1>"));
	processdelay(FLASH_DELAY);
	ReadData(1);
}


void CBoardLevel::OnBnClickedSram()
{
	//SRAM
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=3B0>"));
	processdelay(SRAM_DELAY);
	ReadData(1);
}


void CBoardLevel::OnBnClickedRssiVoltage()
{
	//RSSI_VOLTAGE
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=2AF>")); // X,Y 
	processdelay(CMD_DELAY * 6);
	ReadData(1);
}


void CBoardLevel::OnBnClickedUserSerial()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);
#if 0
	SendData(_T("<TEST=1AE>"));
	ReadData(CMD_DELAY);
#endif
	SendDataUser(_T("test 1 ok")); /// User Serial 
	processdelay(20);
	ReadDataUser(1);
}


void CBoardLevel::OnBnClickedPlcIc()
{
	//SIG60 TEST
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=6B3>"));
	processdelay(CMD_DELAY * 6);
	ReadData(1);
}


void CBoardLevel::RfidSetTarget()
{
	int ch;
	CString tmp, chcmd;
	m_bEnterTheTest = FALSE;

	m_sTestRfid = "00009";

	tmp.Format("A=%s", m_sTestRfid);
	tmp = ConverToHex(tmp);
	SendData("<A=" + m_sTestRfid + tmp + ">");

	ch = _ttoi(m_sTestRfid) % 122;
	tmp.Format("C=%d", ch);
	tmp = ConverToHex(tmp);
	chcmd.Format("<C=%d%s>", ch, tmp);
	SendData(chcmd);
	processdelay(500);
	ReadData(1);
}


void CBoardLevel::RfidSetMaster()
{
	int ch;
	CString tmp, chcmd;
	m_bEnterTheTest = FALSE;
	
	m_sTestRfid = "00009";

	tmp.Format("A=%s", m_sTestRfid);
	tmp = ConverToHex(tmp);
	SendDataMaster("<A=" + m_sTestRfid + tmp + ">");

	ch = _ttoi(m_sTestRfid) % 122;
	tmp.Format("C=%d", ch);
	tmp = ConverToHex(tmp);
	chcmd.Format("<C=%d%s>", ch, tmp);
	SendDataMaster(chcmd);
	processdelay(500);
	ReadData(1);
}


void CBoardLevel::OnBnClickedMpu6050()
{
	//MPU-6050 TEST
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=7B4>"));
	processdelay(CMD_DELAY * 14);
	ReadData(1);
}


void CBoardLevel::OnBnClickedInputPort()
{
	//INPUT PORT
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=8B5>"));
	processdelay(CMD_DELAY + CMD_DELAY);
	ReadData(1);
}


void CBoardLevel::Thread2gfullPayRx()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<RX32=07C>"));
	SendDataMaster(_T("<TX32=07E>"));
	processdelay(5000);
	ReadData(1);
	ReadDataMaster(1);
}


void CBoardLevel::Thread2gfullPayTx()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendDataMaster(_T("<RX32=07C>"));
	SendData(_T("<TX32=07E>"));
	processdelay(5000);
	ReadData(1);
	ReadDataMaster(1);
}

void CBoardLevel::OnBnClickedInflashTest()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendData(_T("<TEST=12E0>"));
	processdelay(INFLASH_DELAY);
	ReadData(1);
}

void CBoardLevel::OnBnClickedPlc10mhz()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendDataMaster(_T("<TEST=14E2>"));
	SendData(_T("<TEST=13E1>"));
	processdelay(1000);
	ReadData(1);
	ReadDataMaster(1);
}


void CBoardLevel::OnBnClickedPlc13mhz()
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread, this);

	SendDataMaster(_T("<TEST=16E4>"));
	SendData(_T("<TEST=15E3>"));
	processdelay(1000);
	ReadData(1);
	ReadDataMaster(1);
}

void CBoardLevel::bitcontrol(int which, int dat)
{
#if 0
	im 0 8bit
	im 1 16bit
#endif
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("IM=%d", dat);
	PortCmd.Format("<%s%s>", tmp, ConverToHex(tmp));

	m_sComparestr = tmp;

	if (!which)	SendData(PortCmd);
	else		SendDataMaster(PortCmd);

	processdelay(100);

	if (!which)	ReadData(1);
	else		ReadDataMaster(1);
}

void CBoardLevel::TestResult(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision->MoveWindow(rcParent.left + (rcParent.Width() - g_rcClientDecisionDlg_BoadrdLevel.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcClientDecisionDlg_BoadrdLevel.Height()) / 2 - YPOS_OFFSET, g_rcClientDecisionDlg_BoadrdLevel.Width(), g_rcClientDecisionDlg_BoadrdLevel.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_bEnterTheTest = TRUE;
		m_iTestResult = TRUE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitor.SetWindowText(_T(msg));

		if (m_iTestStep == USERSERIAL)			m_cUserSerial.SetCheck(1);
		if (m_iTestStep == LEDON)				m_cLedon.SetCheck(1);
		if (m_iTestStep == IOCHK)				m_cIoTest.SetCheck(1);
		if (m_iTestStep == SRAM)				m_cSramTest.SetCheck(1);
		if (m_iTestStep == FLASH)				m_cFlashTest.SetCheck(1);
		if (m_iTestStep == TXRSSI_MNTVOLTAGE)	m_cRssiVolt.SetCheck(1);
		if (m_iTestStep == RF2GFULLPAYRX)		m_c2gFullPayRx.SetCheck(1);
		if (m_iTestStep == RF2GFULLPAYTX)		m_c2gFullPayTx.SetCheck(1);
		if (m_iTestStep == PLCREGISTER)			m_cPlcIc.SetCheck(1);
		if (m_iTestStep == MPU6050)				m_cMpu.SetCheck(1);
		if (m_iTestStep == BOARDINPUTPORT)		m_cInputPort.SetCheck(1);
		if (m_iTestStep == INFLASH)				m_cInFlashTest.SetCheck(1);
		if (m_iTestStep == PLC_10_5MHZ)			m_cPlc10mhz.SetCheck(1);
		if (m_iTestStep == PLC_13MHZ)			m_cPlc13mhz.SetCheck(1);

		m_iTestStep++;
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitor.SetWindowText(_T(msg));
		OnBnClickedAutoStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_iTestResult = TRUE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitor.SetWindowText(_T(msg));
		OnBnClickedAutoStop();
		m_cLedoff.SetCheck(1);

		m_pDlgDecision->SetDecision(TRUE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
	else
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitor.SetWindowText(_T(msg));
		OnBnClickedAutoStop();

		m_pDlgDecision->SetDecision(FALSE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
}


UINT ThreadStatus_BoardLevel(LPVOID lParam)
{
	CBoardLevel *pCBoardLevel;
	pCBoardLevel = (CBoardLevel*)lParam;

	while (pCBoardLevel->m_bThreadStatus)
	{
		if (pCBoardLevel->m_iTestStep == BITCONTROL)
		{
			if (pCBoardLevel->m_b8bits)	pCBoardLevel->bitcontrol(0, 0);
			else						pCBoardLevel->bitcontrol(0, 1);

			if (pCBoardLevel->m_b8bits)
			{
				if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "0") != 0)	pCBoardLevel->TestResult(_T("PASS"));
				else															pCBoardLevel->TestResult(_T("FAIL"));
			}
			else
			{
				if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "1") != 0)	pCBoardLevel->TestResult(_T("PASS"));
				else															pCBoardLevel->TestResult(_T("FAIL"));
			}
		}
		if (pCBoardLevel->m_iTestStep == USERSERIAL)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedUserSerial();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff2, "test 1 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == TXRSSI_MNTVOLTAGE)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedRssiVoltage();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 2 fail") != 0)	pCBoardLevel->TestResult(_T("FAIL"));
			else																		pCBoardLevel->TestResult(_T("PASS"));
		}
		else if (pCBoardLevel->m_iTestStep == SRAM)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedSram();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 3 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == FLASH)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedFlashTest();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 4 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == IDCHGTARGET)
		{
#if 0
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->RfidSetTarget();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "A=B96A-7" + pCBoardLevel->m_sTestRfid) != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																								pCBoardLevel->TestResult(_T("FAIL"));
#endif
			pCBoardLevel->m_iTestStep++;
		}
		else if (pCBoardLevel->m_iTestStep == IDCHGMASTER)
		{
#if 0
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->RfidSetMaster();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuffMaster, (const char*)"A=B96A-7" + pCBoardLevel->m_sTestRfid) != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																													pCBoardLevel->TestResult(_T("FAIL"));
#endif
			pCBoardLevel->m_iTestStep++;
		}
		else if (pCBoardLevel->m_iTestStep == RF2GFULLPAYRX)
		{
#if 0
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->Thread2gfullPayRx();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "- 32BYTE RF COMM TEST SUCCESS") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																						pCBoardLevel->TestResult(_T("FAIL"));
#endif
			pCBoardLevel->m_iTestStep++;
		}
		else if (pCBoardLevel->m_iTestStep == RF2GFULLPAYTX)
		{
#if 0
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->Thread2gfullPayTx();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuffMaster, "- 32BYTE RF COMM TEST SUCCESS") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																								pCBoardLevel->TestResult(_T("FAIL"));
#endif
			pCBoardLevel->m_iTestStep++;
		}
		else if (pCBoardLevel->m_iTestStep == PLCREGISTER)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedPlcIc();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "0x1F") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == MPU6050)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedMpu6050();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 7 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == BOARDINPUTPORT)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedInputPort();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "0x1F") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == IOCHK)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedIoTest();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 9 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == INFLASH)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedInflashTest();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 12 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == PLC_10_5MHZ)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedPlc10mhz();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 13 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == PLC_13MHZ)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedPlc13mhz();

			if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "test 15 ok") != 0)	pCBoardLevel->TestResult(_T("PASS"));
			else																	pCBoardLevel->TestResult(_T("FAIL"));
		}
		else if (pCBoardLevel->m_iTestStep == LEDON)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedLedon();

			if (IDYES == AfxMessageBox(_T("LED ON 확인"), MB_YESNO))
			{
				pCBoardLevel->TestResult(_T("PASS"));
			}
			else if (IDNO)
			{
				pCBoardLevel->TestResult(_T("STOP"));
			}
		}
		else if (pCBoardLevel->m_iTestStep == LEDOFF)
		{
			if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedLedoff();

			if (IDYES == AfxMessageBox(_T("LED OFF 확인\r\n전원 LED 제외"), MB_YESNO))
			{
				pCBoardLevel->TestResult(_T("END"));
			}
			else if (IDNO)
			{
				pCBoardLevel->TestResult(_T("STOP"));
			}
		}
#if 0
		else if (pCBoardLevel->m_iTestStep == RF2G)
		{
			if (IDYES == AfxMessageBox(_T("스펙트럼 2.457 Ghz 셋팅 확인"), MB_YESNO))
			{
				if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedPlcIc();

				if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "2.4G RF TEST SUCCESS") != 0)
				{
					if (IDYES == AfxMessageBox(_T("스펙트럼 2.457 Ghz 레벨 확인"), MB_YESNO))
					{
						pCBoardLevel->TestResult(_T("PASS"));
					}
					else if (IDNO)
					{
						pCBoardLevel->TestResult(_T("FAIL"));
					}
				}
			}
			else if (IDNO)
			{
				pCBoardLevel->TestResult(_T("STOP"));
			}
		}
		else if (pCBoardLevel->m_iTestStep == RF5G)
		{
			if (IDYES == AfxMessageBox(_T("스펙트럼 5.775 Ghz 셋팅 확인"), MB_YESNO))
			{
				if (pCBoardLevel->m_bEnterTheTest)	pCBoardLevel->OnBnClickedMpu6050();

				if (strstr((const char*)g_pApp_BoardLevel->RcvBuff, "5G RF TEST SUCCESS") != 0)
				{
					if (IDYES == AfxMessageBox(_T("스펙트럼 5.775 Ghz 레벨 확인"), MB_YESNO))
					{
						pCBoardLevel->TestResult(_T("PASS"));
					}
					else if (IDNO)
					{
						pCBoardLevel->TestResult(_T("FAIL"));
					}
				}
			}
			else if (IDNO)
			{
				pCBoardLevel->TestResult(_T("STOP"));
			}
		}
#endif
		Sleep(0);
	}
	return 0;
}


void CBoardLevel::OnBnClicked2gfullPay()
{
	m_bThreadStatus = TRUE;
	m_bEnterTheTest = TRUE;
	m_iTestStep = RF2GFULLPAYRX;
	AfxBeginThread(ThreadStatus_BoardLevel, (LPVOID)this);
}


void CBoardLevel::OnBnClicked2gfullPayTx()
{
	m_bThreadStatus = TRUE;
	m_bEnterTheTest = TRUE;
	m_iTestStep = RF2GFULLPAYTX;
	AfxBeginThread(ThreadStatus_BoardLevel, (LPVOID)this);
}


void CBoardLevel::OnBnClickedAutoStart()
{
	UpdateData(TRUE);

	m_iTestStep = BITCONTROL;
	m_bThreadStatus = TRUE;
	m_bEnterTheTest = TRUE;

	m_BtnLedon.EnableWindow(FALSE);
	m_BtnLedoff.EnableWindow(FALSE);
	m_BtnIoTest.EnableWindow(FALSE);
	m_BtnFlashTest.EnableWindow(FALSE);
	m_BtnAutoStart.EnableWindow(FALSE);
	m_BtnUserSerial.EnableWindow(FALSE);
	m_BtnRssiVolt.EnableWindow(FALSE);
	m_BtnSram.EnableWindow(FALSE);
	m_BtnPlcIc.EnableWindow(FALSE);
	m_BtnMpu6050.EnableWindow(FALSE);
	m_BtnInputPort.EnableWindow(FALSE);
	m_BtnInflashTest.EnableWindow(FALSE);
	m_BtnPlc10mhz.EnableWindow(FALSE);
	m_BtnPlc13mhz.EnableWindow(FALSE);
	m_chk8bits.EnableWindow(FALSE);

	m_cUserSerial.SetCheck(0);
	m_cLedon.SetCheck(0);
	m_cLedoff.SetCheck(0);
	m_cIoTest.SetCheck(0);
	m_cSramTest.SetCheck(0);
	m_cFlashTest.SetCheck(0);
	m_cRssiVolt.SetCheck(0);
	m_cPlcIc.SetCheck(0);
	m_cMpu.SetCheck(0);
	m_cInputPort.SetCheck(0);
	m_cInFlashTest.SetCheck(0);
	m_cPlc10mhz.SetCheck(0);
	m_cPlc13mhz.SetCheck(0);

	if (m_pThread == NULL)
	{
		m_pThread = AfxBeginThread(ThreadStatus_BoardLevel, (LPVOID)this);
		if (m_pThread == NULL)
		{
			AfxMessageBox("자동 시작 실패");
		}
		m_pThread->m_bAutoDelete = FALSE;
		m_eThreadWork = THREAD_RUNNING;
	}
	else
	{
		if (m_eThreadWork == THREAD_PAUSE)
		{
			m_pThread->ResumeThread();
			m_eThreadWork = THREAD_RUNNING;
		}
	}
}


void CBoardLevel::OnBnClickedAutoStop()
{
	m_iTestStep = BITCONTROL;
	m_bThreadStatus = FALSE;
	m_bEnterTheTest = FALSE;

	m_BtnLedon.EnableWindow(TRUE);
	m_BtnLedoff.EnableWindow(TRUE);
	m_BtnIoTest.EnableWindow(TRUE);
	m_BtnFlashTest.EnableWindow(TRUE);
	m_BtnAutoStart.EnableWindow(TRUE);
	m_BtnUserSerial.EnableWindow(TRUE);
	m_BtnRssiVolt.EnableWindow(TRUE);
	m_BtnSram.EnableWindow(TRUE);
	m_BtnPlcIc.EnableWindow(TRUE);
	m_BtnMpu6050.EnableWindow(TRUE);
	m_BtnInputPort.EnableWindow(TRUE);
	m_BtnInflashTest.EnableWindow(TRUE);
	m_BtnPlc10mhz.EnableWindow(TRUE);
	m_BtnPlc13mhz.EnableWindow(TRUE);
	m_chk8bits.EnableWindow(TRUE);

	if (m_pThread == NULL)
	{
		AfxMessageBox("자동 테스트 모드가 아닙니다.");
	}
	else
	{
#if 0
		쓰레드 종료의 경우 AfxBeginThread 로 쓰레드 생성했을 때는 위처럼 사용하지 않는다.
			CreateThread로 한 경우만 위처럼 delete해준다.
			AfxBeginThread를 사용한경우 ThreadFunction의 While문이 정지되게 만들어서 return 0 되게 되면 알아서 종료처리 된다.
			단, While문 안의 처리가 모두 될 수 있도록 Sleep을 적절히 사용하여 정지하도록 하자.

			m_pThread->SuspendThread();

		DWORD dwResult;
		::GetExitCodeThread(m_pThread->m_hThread, &dwResult);

		delete m_pThread;
		m_pThread = NULL;
		/*
		스레드 일시 정지
		if(m_pThread == NULL)
		{
		AfxMessageBox("자동 시작 중이 아닙니다");
		}
		else
		{
		m_pThread->SuspendThread();
		m_eThreadWork = THREAD_PAUSE;
		}
		*/
#endif	
		m_pThread = NULL;

		m_eThreadWork = THREAD_STOP;
	}
}

HBRUSH CBoardLevel::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialogEx::OnCtlColor(pDC, pWnd, nCtlColor);

	int nRet = pWnd->GetDlgCtrlID();

	switch (m_iTestResult)
	{
	case 0:
		if (nRet == IDC_TEST_RESULT)
			pDC->SetTextColor(RGB(255, 0, 0));
		break;
	case 1:
		if (nRet == IDC_TEST_RESULT)
			pDC->SetTextColor(RGB(0, 0, 255));
		break;
	}
	return hbr;
}


void CBoardLevel::OnBnClickedManualPayload()
{
	RfidSetTarget();
	RfidSetMaster();
	Thread2gfullPayRx();
	Thread2gfullPayTx();
}
