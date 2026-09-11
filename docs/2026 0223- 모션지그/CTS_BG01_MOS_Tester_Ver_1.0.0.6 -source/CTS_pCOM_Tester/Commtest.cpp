// Commtest.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "Commtest.h"
#include "afxdialogex.h"

#define UM_UPDATE			WM_USER + 100
#define REFSELECT 49
#define DUTSELECT 57
enum
{
	pCOMWAIT = 0,
	pCOM_TEMP_ANGLE,
//	TARGET_RFIDCH_CHANGE,
//	TARGET_VHLID_CHANGE,
	MINE_RFIDCH_CHANGE,
//	MINE_VHLID_CHANGE,
	SWITCHCONSOLE_M_1,
	SWITCHCONSOLE_T_1,
//	CONFIGCLEAR_M,
//	CONFIGCLEAR_T,
	SWITCHNORMAL_M_1,	//10
	SWITCHNORMAL_T_1,	
	VERCHK,
//	DEBUG6_M,
	XPNS,
//	READYTORCVRSSI,
//	DEBUG0_M_1,			
	PORTCLEAR1,
	SWITCHCONSOLE_M_2,	
//	RAMTEST,
//	ROMTEST,
	SWITCHNORMAL_M_2,
	IO_PORT_CLEAR,
	MOSI_IOTEST_01,
	MOSI_IOTEST_02,
	MOSI_IOTEST_03,
	MOSI_IOTEST_04,		//20
	MOSI_IOTEST_05,		
	MOSI_IOTEST_06,
	MOSI_IOTEST_07,
	MOSI_IOTEST_08,
	MOSI_IOTEST_09,		
	MOSI_IOTEST_10,
	MOSI_IOTEST_11,
	MOSI_IOTEST_12,
	MOSI_IOTEST_13,
	MOSI_IOTEST_14,		//30
	MOSI_IOTEST_15,		
	MOSI_IOTEST_16,
	MISO_IOTEST_01,
	MISO_IOTEST_02,
	MISO_IOTEST_03,		
	MISO_IOTEST_04,
	MISO_IOTEST_05,
	MISO_IOTEST_06,
	MISO_IOTEST_07,
	MISO_IOTEST_08,		//40
	MISO_IOTEST_09,		
	MISO_IOTEST_10,
	MISO_IOTEST_11,
	MISO_IOTEST_12,
	MISO_IOTEST_13,		
	MISO_IOTEST_14,
	MISO_IOTEST_15,
	MISO_IOTEST_16,
	IO_TRIGGER,
#if 0
	MINE_RCV_DATA_CHECK,
	DEBUG0_T_1,				
	TARGET_RCV_DATA_CHECK,
	DEBUG0_M_2,
#endif
	TARGET_USERDATA_2000,			//50
	MINE_USERDATA_2000,			
	TARGET_USERDATA_100_CHECK,
	TARGET_USERDATA_300_CHECK,	
	TARGET_USERDATA_500_CHECK,
	TARGET_USERDATA_1000_CHECK,
	TARGET_USERDATA_2000_CHECK,
	MINE_USERDATA_100_CHECK,		
	MINE_USERDATA_300_CHECK,
	MINE_USERDATA_500_CHECK,
	MINE_USERDATA_1000_CHECK,		//60
	MINE_USERDATA_2000_CHECK,		

	AUTOGAINCONTROL_M_1,
//	AUTOGAINCONTROL_T_1,		

	SWITCHCONSOLE_M_3,
	DEBUG12_T_1,
	TARGET_RSSI,
	DEBUG0_T_2,			
	DEFAULT_MR0_M,
	SWITCHNORMAL_M_3,
#if 0
	SWITCHCONSOLE_T_2,				//70
	DEBUG12_M_1,				
	MINE_RSSI,
	DEBUG0_M_3,					
	DEFAULT_MR0_T,
	SWITCHNORMAL_T_2,
#endif
	AUTOGAINCHECK,
	GOOFFCHECK,
	TEMP_ANGLE_CHECK,
	DCLINECHECK,
#if 0
	AUTOGAINCONTROL_M_2,
	AUTOGAINCONTROL_T_2,		
	DEBUG14_M_1,
	MINEAUTOGAINCHECK,
	DEBUG0_M_4,						//80
#endif	
};

#define		STX				0x02
#define		ETX				0x03
#define YPOS_OFFSET			65
#define COMMAND_DELAY		25
#define RAMTEST_DELAY		14000
#define TIMER_CALL_DELAY	1000
#define PORT_RENEWAL		150
#define RESET_DELAY			100
#define DEFAULT_MR0			28
// CCommtest dialog

IMPLEMENT_DYNAMIC(CCommtest, CDialogEx)

CFont g_editFont_Commtest;
CFont g_editFont_CommtestRfid;
CFont g_editFont_CommtestVhlid;
CCTS_pCOM_TesterApp *g_pApp_Commtest;
CRect g_rcClientDecisionDlg_Commtest;

CCommtest::CCommtest(CWnd* pParent /*=NULL*/)
	: CDialogEx(CCommtest::IDD, pParent)
	, m_sCompVersion(_T("1.55"))
	, m_sCommTestRfid(_T("00009"))
	, m_sCommTestVhlid(_T("000009"))
	, m_sPreAngleX(_T("00.00"))
	, m_sPreAngleY(_T("00.00"))
	, m_sPreAngleZ(_T("000.0"))
	, m_sPreTemp(_T("00.00"))
	, m_sAngleX(_T("00.00"))
	, m_sAngleY(_T("00.00"))
	, m_sAngleZ(_T("000.0"))
	, m_sTemp(_T("00.00"))
	, m_sTemptime(_T("8000"))
	, m_sGooffAtt(_T("20"))
	, m_iBits(0)
	, m_iJigMode(0)
{
	g_pApp_Commtest = (CCTS_pCOM_TesterApp *)AfxGetApp();
}

CCommtest::~CCommtest()
{
}

void CCommtest::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_VER_CHECK, m_BtnVerChk);
	DDX_Control(pDX, IDC_CONF_CLEAR, m_BtnConf);
	DDX_Control(pDX, IDC_RESET, m_BtnReset);
	DDX_Control(pDX, IDC_AUTO_START, m_BtnAutoStart);
	DDX_Control(pDX, IDC_AUTO_STOP, m_BtnAutoStop);
	DDX_Control(pDX, IDC_CHECK1, m_cVerChk);
	DDX_Control(pDX, IDC_CHECK2, m_cConf);
	DDX_Control(pDX, IDC_CHECK3, m_cRcvWaitRssi);
	DDX_Control(pDX, IDC_CHECK7, m_cIoTest);
	DDX_Control(pDX, IDC_RCVSTAT_COMM, m_EditRcvStatComm);
	DDX_Control(pDX, IDC_COMP_VERSION, m_cCompVersion);
	DDX_Text(pDX, IDC_COMP_VERSION, m_sCompVersion);
	DDX_Control(pDX, IDC_TEST_RFID, m_cCommTestRfid);
	DDX_Text(pDX, IDC_TEST_RFID, m_sCommTestRfid);
	DDX_Control(pDX, IDC_TEST_VHLID, m_cCommTestVhlid);
	DDX_Text(pDX, IDC_TEST_VHLID, m_sCommTestVhlid);
	DDX_Control(pDX, IDC_CHECK8, m_cUserData);
	DDX_Control(pDX, IDC_CHECK9, m_cTargetRssi);
	DDX_Control(pDX, IDC_CHECK10, m_cMineRssi);
	DDX_Control(pDX, IDC_PROGRESS1, m_progress);
	DDX_Control(pDX, IDC_CHECK11, m_cAutoGainCon);
	DDX_Control(pDX, IDC_PRE_ANGLEX, m_editPreAngleX);
	DDX_Control(pDX, IDC_PRE_ANGLEY, m_editPreAngleY);
	DDX_Control(pDX, IDC_PRE_ANGLEZ, m_editPreAngleZ);
	DDX_Control(pDX, IDC_PRE_TEMP, m_editPreTemp);
	DDX_Control(pDX, IDC_ANGLEX, m_editAngleX);
	DDX_Control(pDX, IDC_ANGLEY, m_editAngleY);
	DDX_Control(pDX, IDC_ANGLEZ, m_editAngleZ);
	DDX_Control(pDX, IDC_TEMP, m_editTemp);
	DDX_Text(pDX, IDC_PRE_ANGLEX, m_sPreAngleX);
	DDX_Text(pDX, IDC_PRE_ANGLEY, m_sPreAngleY);
	DDX_Text(pDX, IDC_PRE_ANGLEZ, m_sPreAngleZ);
	DDX_Text(pDX, IDC_PRE_TEMP, m_sPreTemp);
	DDX_Text(pDX, IDC_ANGLEX, m_sAngleX);
	DDX_Text(pDX, IDC_ANGLEY, m_sAngleY);
	DDX_Text(pDX, IDC_ANGLEZ, m_sAngleZ);
	DDX_Text(pDX, IDC_TEMP, m_sTemp);
	DDX_Text(pDX, IDC_EDIT_TEMPTIME, m_sTemptime);
	DDX_Control(pDX, IDC_EDIT_TEMPTIME, m_editTemptime);
	DDX_Control(pDX, IDC_EDIT_GOOFF_ATT, m_editGooffAtt);
	DDX_Text(pDX, IDC_EDIT_GOOFF_ATT, m_sGooffAtt);
	DDX_Control(pDX, IDC_CHECK6, m_cDcline);
	DDX_Control(pDX, IDC_CHECK12, m_cAngleCorr);
	DDX_Control(pDX, IDC_CHECK13, m_cGooff);
	DDX_Radio(pDX, IDC_RADIO1, m_iBits);
	DDX_Radio(pDX, IDC_RADIO3, m_iJigMode);
}


BEGIN_MESSAGE_MAP(CCommtest, CDialogEx)
	ON_MESSAGE(UM_UPDATE, OnUpdateData)
	ON_BN_CLICKED(IDC_VER_CHECK, &CCommtest::OnBnClickedVerCheck)
	ON_BN_CLICKED(IDC_CONF_CLEAR, &CCommtest::OnBnClickedConfClear)
	ON_BN_CLICKED(IDC_RESET, &CCommtest::OnBnClickedReset)
	ON_BN_CLICKED(IDC_AUTO_START, &CCommtest::OnBnClickedAutoStart)
	ON_BN_CLICKED(IDC_AUTO_STOP, &CCommtest::OnBnClickedAutoStop)
	ON_BN_CLICKED(IDC_BUTTON6, &CCommtest::OnBnClickedButton6)
	ON_WM_CTLCOLOR()
	ON_WM_TIMER()
	ON_NOTIFY(NM_CUSTOMDRAW, IDC_PROGRESS1, &CCommtest::OnNMCustomdrawProgress1)
END_MESSAGE_MAP()

// CCommtest message handlers
BOOL CCommtest::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	g_editFont_Commtest.CreatePointFont(500, TEXT("굴림"));
	m_EditRcvStatComm.SetFont(&g_editFont_Commtest, TRUE);

	g_editFont_CommtestRfid.DeleteObject();
	g_editFont_CommtestRfid.CreatePointFont(150, TEXT("굴림"));
	m_cCommTestRfid.SetFont(&g_editFont_CommtestRfid, TRUE);

	g_editFont_CommtestVhlid.DeleteObject();
	g_editFont_CommtestVhlid.CreatePointFont(150, TEXT("굴림"));
	m_cCommTestVhlid.SetFont(&g_editFont_CommtestVhlid, TRUE);

	m_cVerChk.EnableWindow(FALSE);
	m_cConf.EnableWindow(FALSE);
	m_cRcvWaitRssi.EnableWindow(FALSE);
	m_cIoTest.EnableWindow(FALSE);
	m_cUserData.EnableWindow(FALSE);
	m_cTargetRssi.EnableWindow(FALSE);
	m_cMineRssi.EnableWindow(FALSE);
	m_cAutoGainCon.EnableWindow(FALSE);
	m_cDcline.EnableWindow(FALSE);
	m_cAngleCorr.EnableWindow(FALSE);
	m_cGooff.EnableWindow(FALSE);

	m_editPreAngleX.EnableWindow(FALSE);
	m_editPreAngleY.EnableWindow(FALSE);
	m_editPreAngleZ.EnableWindow(FALSE);
	m_editPreTemp.EnableWindow(FALSE);
	m_editAngleX.EnableWindow(FALSE);
	m_editAngleY.EnableWindow(FALSE);
	m_editAngleZ.EnableWindow(FALSE);
	m_editTemp.EnableWindow(FALSE);

	m_cVerChk.SetCheck(0);
	m_cConf.SetCheck(1);
	m_cRcvWaitRssi.SetCheck(0);
	m_cIoTest.SetCheck(0);
	m_cUserData.SetCheck(0);
	m_cTargetRssi.SetCheck(0);
	m_cMineRssi.SetCheck(1);
	m_cAutoGainCon.SetCheck(0);
	m_cDcline.SetCheck(0);
	m_cAngleCorr.SetCheck(0);
	m_cGooff.SetCheck(0);

	m_editGooffAtt.SetLimitText(2);

	m_sRightTrim = _T("]\r\n");
	m_sTmp = _T("");
	m_sTryValue = _T("");
	m_sFailValue = _T("");

	// added
	m_pDlgDecision = new CDecisionDlg();// decision
	m_pDlgDecision->Create(IDD_DECISION, this);// decision
	VERIFY(m_pDlgDecision);
	m_pDlgDecision->GetClientRect(g_rcClientDecisionDlg_Commtest); // decision 최초 생성시 창 크기를 기억한다.

	m_cCommTestRfid.SetLimitText(5);
	m_cCommTestVhlid.SetLimitText(6);

	return TRUE;  // return TRUE unless you set the focus to a control
	// 예외: OCX 속성 페이지는 FALSE를 반환해야 합니다.
}


LRESULT CCommtest::OnUpdateData(WPARAM wParam, LPARAM lParam)
{
	UpdateData(FALSE);

	return 0;
}


void CCommtest::SendData_to_test(CString SendCmd) //test pCOM send
{
	g_pApp_Commtest->SendDataToEditControl(SendCmd, &g_pApp_Commtest->m_ComuPort);
}

void CCommtest::SendData_to_ref(CString SendCmd) //ref pCOM send
{
	g_pApp_Commtest->SendDataToEditControlMaster(SendCmd, &g_pApp_Commtest->m_ComuPortMaster);
}

void CCommtest::SendData_to_u_test(CString SendCmd) //user test pCOM send
{
	g_pApp_Commtest->SendDataToEditControl2(SendCmd, &g_pApp_Commtest->m_ComuPort2);
}

void CCommtest::SendData_to_u_ref(CString SendCmd) //user ref pCOM send
{
	g_pApp_Commtest->SendDataToEditControl3(SendCmd, &g_pApp_Commtest->m_ComuPort3);
}

void CCommtest::SendData_to_supply(CString SendCmd) //power supply send
{
	g_pApp_Commtest->SendDataToEditControl4(SendCmd, &g_pApp_Commtest->m_ComuPort4);
}

void CCommtest::ReadData_to_test(int DelayTime)
{
	g_pApp_Commtest->ReadDataToEditControl(DelayTime, &g_pApp_Commtest->m_ComuPort);
}

void CCommtest::ReadData_to_ref(int DelayTime)
{
	g_pApp_Commtest->ReadDataToEditControlMaster(DelayTime, &g_pApp_Commtest->m_ComuPortMaster);
}

void CCommtest::ReadData_to_u_test(int DelayTime)
{
	g_pApp_Commtest->ReadDataToEditControl2(DelayTime, &g_pApp_Commtest->m_ComuPort2);
}

void CCommtest::ReadData_to_u_ref(int DelayTime)
{
	g_pApp_Commtest->ReadDataToEditControl3(DelayTime, &g_pApp_Commtest->m_ComuPort3);
}

void CCommtest::ReadData_to_supply(int DelayTime)
{
	g_pApp_Commtest->ReadDataToEditControl4(DelayTime, &g_pApp_Commtest->m_ComuPort4);
}

CString CCommtest::ConverToHex(CString data)
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

void CCommtest::processdelay(DWORD dat)
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

UINT ProgressThread_CommTest(LPVOID lParam)
{
	CCommtest *pCCommtest;
	pCCommtest = (CCommtest*)lParam;

	int ProgressVal = 0;
	switch (pCCommtest->m_iTestStep)
	{
	case pCOM_TEMP_ANGLE:               ProgressVal = 10000 / 20;			break;
//	case CONFIGCLEAR_M:					ProgressVal = 2800 / 20;			break;
//	case CONFIGCLEAR_T:					ProgressVal = 3600 / 20;			break;
	case TARGET_RSSI:					ProgressVal = 3000 / 20;			break;
//	case MINE_RSSI:						ProgressVal = 3000 / 20;			break;
//	case RAMTEST:						ProgressVal = 13000 / 20;			break;
//	case ROMTEST:						ProgressVal = 36000 / 20;			break;
	case TARGET_USERDATA_100_CHECK:		ProgressVal = 100 / 5 * 3 / 20;		break;
	case TARGET_USERDATA_300_CHECK:		ProgressVal = 300 / 5 * 3 / 20;		break;
	case TARGET_USERDATA_500_CHECK:		ProgressVal = 500 / 5 * 3 / 20;		break;
	case TARGET_USERDATA_1000_CHECK:	ProgressVal = 1000 / 5 * 3 / 20;	break;
	case TARGET_USERDATA_2000_CHECK:	ProgressVal = 2000 / 5 * 3 / 20;	break;
	case MINE_USERDATA_100_CHECK:		ProgressVal = 100 / 5 * 3 / 20;		break;
	case MINE_USERDATA_300_CHECK:		ProgressVal = 300 / 5 * 3 / 20;		break;
	case MINE_USERDATA_500_CHECK:		ProgressVal = 500 / 5 * 3 / 20;		break;
	case MINE_USERDATA_1000_CHECK:		ProgressVal = 1000 / 5 * 3 / 20;	break;
	case MINE_USERDATA_2000_CHECK:		ProgressVal = 2000 / 5 * 3 / 20;	break; 
	}

	pCCommtest->m_progress.SetRange(0, ProgressVal);

	for (int i = 0; i <= ProgressVal; i++)
	{
		pCCommtest->processdelay(20);
		pCCommtest->m_progress.SetPos(i);

		if (pCCommtest->m_pProgressThread == NULL)
		{
			pCCommtest->m_progress.SetPos(0);
			break;
		}
	}
	return 0;
}

int CCommtest::GetFindCharCount(CString param_string, char param_find_char)
{
	int length = param_string.GetLength(), find_count = 0;

	for (int i = 0; i < length; i++)
	{
		if (param_string[i] == param_find_char)
		{
			find_count++;
		}
	}
	return find_count;
}

void CCommtest::OnTimer(UINT_PTR nIDEvent)
{
#if 0
	if (m_iTimercnt >0)	m_iTimercnt--;
	switch (nIDEvent)
	{
	case RAMTEST:
		ReadData_to_test(1);
		if (strstr((const char*)g_pApp_Commtest->RcvBuff, "Test Success") != 0)
		{
			KillTimer(RAMTEST);
			TestResult(_T("PASS"));
			break;
		}
		else if (g_pApp_Commtest->RcvBuff[0] == '\0' && m_iTimercnt < 13)	//ramtest 명령어 시작 시 첫 응답 pCOM 내부 연산으로 느림, Timer cnt 초반은 응답이 없더라도 넘어간다.
		{
			KillTimer(RAMTEST);
			TestResult(_T("FAIL"));
			break;
		}

		if (!m_iTimercnt)
		{
			KillTimer(RAMTEST);
			if (strstr((const char*)g_pApp_Commtest->RcvBuff, "Test Success") != 0)		TestResult(_T("PASS"));
			else																		TestResult(_T("FAIL"));
		}
		break;
	case ROMTEST:
		ReadData_to_test(1);
		if (strstr((const char*)g_pApp_Commtest->RcvBuff, "Test Success") != 0)
		{
			KillTimer(ROMTEST);
			TestResult(_T("PASS"));
			break;
		}
		else if (g_pApp_Commtest->RcvBuff[0] == '\0' && m_iTimercnt < 25)	//ramtest 명령어 시작 시 첫 응답 pCOM 내부 연산으로 느림, Timer cnt 초반은 응답이 없더라도 넘어간다.
		{
			KillTimer(ROMTEST);
			TestResult(_T("FAIL"));
			break;
		}

		if (!m_iTimercnt)
		{
			KillTimer(ROMTEST);
			if (strstr((const char*)g_pApp_Commtest->RcvBuff, "Test Success") != 0)		TestResult(_T("PASS"));
			else																		TestResult(_T("FAIL"));
		}
		break;
	}

	CDialogEx::OnTimer(nIDEvent);
#endif
}

void CCommtest::entertheconsole(int which)
{
	m_bEnterTheTest = FALSE;

	if (!which)	SendData_to_test("#console\r");
	else		SendData_to_ref("#console\r");

	processdelay(25);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::enterthenormal(int which)
{
	m_bEnterTheTest = FALSE;

	if (!which)	SendData_to_test("exit\r");
	else		SendData_to_ref("exit\r");

	processdelay(50);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::debugmode(int which, int val)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("D=%02d", val);
	PortCmd.Format("<D=%02d%s>", val, ConverToHex(tmp));
	CompStr = PortCmd;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(50);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::debugmode_c(int which, int val)
{
	m_bEnterTheTest = FALSE;

	CString tmp1, tmp2, PortCmd;
	PortCmd.Format("debug %02d\r", val);
	tmp1.Format("D=%02d", val);
	tmp2.Format("[D=%02d%s]", val, ConverToHex(tmp1));
	CompStr = tmp2;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(50);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::rfidch(int which, CString str)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("B=B96A-7%s:%03d", str, 0);
	PortCmd.Format("<B=B96A-7%s:%03d%s>", str, 0, ConverToHex(tmp));
	CompStr = PortCmd;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::vhlid(int which, CString str)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("O=%s", str);
	PortCmd.Format("<O=%s%s>", str, ConverToHex(tmp));
	CompStr = PortCmd;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::datachangecheck(int which)
{
	m_bEnterTheTest = FALSE;

	modesel(REFSELECT, 1);	//MASTER SEL
	modesel(DUTSELECT, 1);	//SLAVE SEL
	debugmode(which, 3);
	processdelay(50);
	modesel(REFSELECT, 0);	//MASTER SEL
	modesel(DUTSELECT, 0);	//SLAVE SEL
	processdelay(50);
	rcv_debugmsg(which);
}

void CCommtest::UserLength(int which, int val)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("UL=%04d", val);
	PortCmd.Format("<UL=%04d%s>", val, ConverToHex(tmp));
	CompStr = PortCmd;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(200);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::UserDataCheck(int which, int length)
{
	m_bEnterTheTest = FALSE;

	CString tmp, userdata;
	BYTE user[2000];
	memset(&user, 1, 2000);

	int i;
	userdata = "";
#if 0
	for (i = 0; i < length; i++)
	{
		tmp.Format("%d", user[i]);
		userdata += tmp;

		Sleep(0);
	}
#endif
	for (i = 0; i < length; i++)
	{
		userdata += '1';
		Sleep(0);
	}

//	m_pProgressThread = AfxBeginThread(ProgressThread_CommTest, this);

	if (!which)	SendData_to_u_test(userdata);
	else		SendData_to_u_ref(userdata);

	processdelay(length / 5 * 3);

	if (!which)	ReadData_to_u_ref(1000);
	else		ReadData_to_u_test(1000);
}

void CCommtest::PortClear(int which)
{
	//디버그모드를 종료 할 경우 디바이스에서 수신 되어 있던 메시지는 모두 삭제 한다.
	if (!which)	PurgeComm(&g_pApp_Commtest->m_ComuPort, PURGE_TXABORT | PURGE_TXCLEAR | PURGE_RXABORT | PURGE_RXCLEAR);
	else		PurgeComm(&g_pApp_Commtest->m_ComuPortMaster, PURGE_TXABORT | PURGE_TXCLEAR | PURGE_RXABORT | PURGE_RXCLEAR);
}

void CCommtest::rcv_debugmsg(int which)
{
	m_bEnterTheTest = FALSE;

	processdelay(50);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::modesel(int which, int val)
{
	/*PIO 명령어 제어*/
	//TEST pCOM Master Mode Wait & REF pCOM Slave Mode Wait
	//PIO16000D000000000000
	//TEST pCOM Master Mode Active & REF pCOM Slave Mode Active
	//PIO160008000000000000

	/*PIW 명령어 제어*/
	//TEST pCOM Master Mode Wait & REF pCOM Slave Mode Wait
	//PIW03491
	//PIW03500
	//PIW03511
	//PIW03521
	//TEST pCOM Master Mode Active & REF pCOM Slave Mode Active
	//PIW03490
	//PIW03500
	//PIW03510
	//PIW03521

	//49 : Test pCOM Select - 0 : ACTIVE, 1 : WAIT
	//50 : Test pCOM M/S    - 0 : MASTER, 1 : SLAVE
	//51 : Ref. pCOM Select - 0 : ACTIVE, 1 : WAIT
	//52 : Ref. pCOM M/S    - 0 : MASTER, 1 : SLAVE

	/* TEST, REF. pCOM WAIT 모드 진입
	modesel(50, 0);
	modesel(52, 1);
	modesel(49, 1);
	modesel(51, 1);
	*/

	/* TEST, REF. pCOM MASTER/SLAVE 모드 진입
	modesel(50, 0);
	modesel(52, 1);
	modesel(49, 0);
	modesel(51, 0);
	*/
	m_bEnterTheTest = FALSE;

	CString PortCmd;
	PortCmd.Format("PIW03%02d%d", which, val);
	PortControl(PortCmd);
}

void CCommtest::RamTest(int which, int timercnt)
{
	m_bEnterTheTest = FALSE;
	
//	m_pProgressThread = AfxBeginThread(ProgressThread_CommTest, this);

	m_iTimercnt = timercnt;

	if (!which)	SendData_to_test("ramtest\r");
	else		SendData_to_ref("ramtest\r");

	SetTimer(m_iTestStep, TIMER_CALL_DELAY, NULL);
#if 0
	if (!which)	ReadData_to_test(RAMTEST_DELAY);
	else		ReadData_to_ref(RAMTEST_DELAY);
#endif
}

void CCommtest::RomTest(int which, int timercnt)
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread_CommTest, this);

	m_iTimercnt = timercnt;

	if (!which)	SendData_to_test("logromtest\r");
	else		SendData_to_ref("logromtest\r");

	SetTimer(m_iTestStep, TIMER_CALL_DELAY, NULL);
}

void CCommtest::AutoGainControl(int which, int val)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("AM=%d", val);
	PortCmd.Format("<AM=%d%s>", val, ConverToHex(tmp));
	CompStr = PortCmd;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::AutoGainControl_c(int which, int val)
{
	m_bEnterTheTest = FALSE;

	CString tmp1, tmp2, PortCmd;
	PortCmd.Format("agcm %01d\r", val);
	tmp1.Format("AM=%01d", val);
	tmp2.Format("[AM=%01d%s]", val, ConverToHex(tmp1));
	CompStr = tmp2;
	CompStr.Replace('<', '[');
	CompStr.Replace('>', ']');

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::AutoGainControlCheck(int which)
{
	m_bEnterTheTest = FALSE;

	/*
	pCOM JIG ATT IO
	1  1  1  1  1
	37 36 35 34 33
	*/

	processdelay(150);
	PortControl("PIW03331");
	PortControl("PIW03341");
	PortControl("PIW03351");
	processdelay(1000);
	PortControl("PIW03330");
	PortControl("PIW03340");
	PortControl("PIW03350");
	processdelay(1000);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);

	CString RcvBuff, strTok;
	CString tmpTok1, tmpTok2;
	int nv1 = 0, nv2 = 0, nv3 = 0, nv4 = 0, nv5 = 0, nv6 = 0;
	int sepCount = GetFindCharCount(RcvBuff, '\n');
	int cnt = 0;
	int result = FALSE;
	int lastbufidx = 0;

	CString* Arr_RcvBuff = new CString[sepCount + 1];
	BYTE* Arr_RssiStepBuff = new BYTE[sepCount + 1];
	BYTE* Arr_RssiBuff = new BYTE[sepCount + 1];

	int* iTargetrssi = new int[sepCount + 1];
	int* iMyrssi = new int[sepCount + 1];
	int* iMymr0 = new int[sepCount + 1];

	for (cnt = 1; cnt <= sepCount - 1; cnt++)
	{
		/*V0.95 기준 출력 메시지 달라짐*/
		//CompareStr = Arr_RcvBuff[cnt].Right(3); 
		//ex)[18167]1:1,2(0):3
		sscanf_s((const char*)Arr_RcvBuff[cnt], "[%d]%d:%d,%d(%d):%d\r", &nv1, &nv2, &nv3, &nv4, &nv5, &nv6);

		iTargetrssi[cnt-1] = nv4;
		iMyrssi[cnt-1] = nv6;
		iMymr0[cnt-1] = nv5;
#if 0
		if (Arr_RcvBuff[cnt].Find(',') == -1 || Arr_RcvBuff[cnt].Find('(') == -1 || Arr_RcvBuff[cnt].Find(')') == -1)
		{
			lastbufidx = cnt - 1;
			break;
		}
		else
		{
			Arr_RssiStepBuff[cnt - 1] = _ttoi(Arr_RcvBuff[cnt].Mid(Arr_RcvBuff[cnt].Find(',') + 1, 1));
			Arr_RssiBuff[cnt - 1] = _ttoi(Arr_RcvBuff[cnt].Mid(Arr_RcvBuff[cnt].Find('(') + 1, Arr_RcvBuff[cnt].Find(')') - Arr_RcvBuff[cnt].Find('(')));
			lastbufidx = cnt;
		}

		else if (Arr_RssiStepBuff[cnt] == 3)
		{
			if (Arr_RssiBuff[cnt] != 0)
			{
				if ((Arr_RssiBuff[cnt] + 10) != Arr_RssiBuff[cnt - 1])
				{
					result = FALSE;
					break;
				}
			}
		}
#endif
	}

	if (!which)
	{
		RcvBuff = (CString)g_pApp_Commtest->RcvBuffMaster;
		if (g_pApp_Commtest->RcvBuffMaster[0] == '\0')
		{
			TestResult("FAIL");
		}

		while (AfxExtractSubString(strTok, RcvBuff, cnt, '\n'))
		{
			Arr_RcvBuff[cnt++] = strTok;
		}

		for (cnt = 2; cnt <= sepCount - 1; cnt++)
		{
			//ex)[18167]1:1,2(0):3
			sscanf_s((const char*)Arr_RcvBuff[cnt], "[%d]%d:%d,%d(%d):%d\r", &nv1, &nv2, &nv3, &nv4, &nv5, &nv6);
		}

		delete[] Arr_RcvBuff;
	}
}

BOOL CCommtest::GoOffCheck(int which)
{
	m_bEnterTheTest = FALSE;
	BOOL returnval = FALSE;
	int i;
	/*
	pCOM JIG ATT IO
	1  1  1  1  1
	37 36 35 34 33
	*/

	PortControl("PIW03330");
	PortControl("PIW03340");
	PortControl("PIW03350");
	PortControl("PIW03360");
	PortControl("PIW03370");

	for (i = 1; i <= _ttoi(m_sGooffAtt); i++)
	{
		if ((i & 0x01) == 0x01)	PortControl("PIW03331");
		if ((i & 0x02) == 0x02)	PortControl("PIW03341");
		if ((i & 0x04) == 0x04)	PortControl("PIW03351");
		if ((i & 0x08) == 0x08)	PortControl("PIW03361");
		if ((i & 0x10) == 0x10)	PortControl("PIW03371");

		processdelay(550);
		if ((g_pApp_Commtest->InputPort[6] & 0x04) != 0x04)
		{
			break;
		}
		if (((g_pApp_Commtest->OutputPort[4] & 0x1F) & i) != i)
		{
			break;
		}

		if ((i & 0x01) == 0x01)	PortControl("PIW03330");
		if ((i & 0x02) == 0x02)	PortControl("PIW03340");
		if ((i & 0x04) == 0x04)	PortControl("PIW03350");
		if ((i & 0x08) == 0x08)	PortControl("PIW03360");
		if ((i & 0x10) == 0x10)	PortControl("PIW03370");
	}

	if ((i >= (_ttoi(m_sGooffAtt))) && ((g_pApp_Commtest->InputPort[6] & 0x04) != 0x04))		returnval = TRUE;
	else if ((i >= (_ttoi(m_sGooffAtt))) && ((g_pApp_Commtest->InputPort[6] & 0x04) == 0x04))	returnval = TRUE;

	PortControl("PIW03330");
	PortControl("PIW03340");
	PortControl("PIW03350");
	PortControl("PIW03360");
	PortControl("PIW03370");
	
	return returnval;
}

void CCommtest::Clearcom_c(int which)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	PortCmd.Format("clrcom\r");

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(50);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

BOOL CCommtest::DclineCheck(int which)
{
	m_bEnterTheTest = FALSE;
	BOOL returnval = FALSE;

	CString RcvBuff, strTok;
	int nv1 = 0, nv2 = 0, nv3 = 0, nv4 = 0, cnt = 0, sepCount = 0;

	entertheconsole(0);
	Clearcom_c(0);
	debugmode_c(0, 5);
	enterthenormal(0);
	modesel(38, 1);	//DC Line
	processdelay(4000);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);

#if 1
	if (!which)
	{
		RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
		if (g_pApp_Commtest->RcvBuff[0] == '\0')
		{
			;
		}

		sepCount = GetFindCharCount(RcvBuff, '\n');

		CString* Arr_RcvBuff = new CString[sepCount + 1];
		while (AfxExtractSubString(strTok, RcvBuff, cnt, '\n'))
		{
			Arr_RcvBuff[cnt++] = strTok;
		}

		for (cnt = 2; cnt <= sepCount - 1; cnt++)
		{
			//ex)22928[G_1, C_0, F_0]
			sscanf_s((const char*)Arr_RcvBuff[cnt], "%d[G_%d, C_%d, F_%d]", &nv1, &nv2, &nv3, &nv4);

			if (nv4 > 0)	break;
		}

		delete[] Arr_RcvBuff;
	}
	if (cnt >= (sepCount - 1))	returnval = TRUE;

	debugmode(0, 0);
#endif
	return returnval;
}

void CCommtest::AutogainCheck(int which)
{
	m_bEnterTheTest = FALSE;

	entertheconsole(0);
	AutoGainControl_c(0, 0);
	mr0(0, DEFAULT_MR0);
	AutoGainControl_c(0, 1);
	ConfigSave(0);
	PcomReset(0);
	processdelay(5000);
	entertheconsole(0);
	mr0_r(0);
}

void CCommtest::ConfigSave(int which)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	PortCmd.Format("save\r");
	CompStr = tmp;

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(50);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::PcomReset(int which)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	PortCmd.Format("reset\r");

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::mr0(int which, int val)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	PortCmd.Format("mr0 %d\r", val);
	tmp.Format("mr0=%d(%d)", val, val);
	CompStr = tmp;

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::mr0_r(int which)
{
	m_bEnterTheTest = FALSE;

	CString PortCmd;
	PortCmd.Format("mr0\r");

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::check_rssi(int which)
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread_CommTest, this);

	m_iRssiCheckDone = FALSE;
	CString cmd;
	CString RcvBuff, strTok;
	CString tmpTok1, tmpTok2;
	int nv1 = 0, nv2 = 0, nv3 = 0, nv4 = 0;
#if 0
	PortControl("PIW03331");
	PortControl("PIW03341");
	PortControl("PIW03351");
	PortControl("PIW03361");
	PortControl("PIW03371");
#endif
	for (int i = 0; i < 128; i += 127)
	{
		cmd.Format("mr0 %d\r", i);

		if (!which)	SendData_to_test(cmd);
		else		SendData_to_ref(cmd);

		Sleep(PORT_RENEWAL*2);//mr0 값을 변경하고 상대방 수신에 적용 대기 시간
		if (!which)	PortClear(1);
		else		PortClear(0);

		if (!which)	ReadData_to_ref(RESET_DELAY);
		else		ReadData_to_test(RESET_DELAY);

		if (!which)
		{
			RcvBuff = (CString)g_pApp_Commtest->RcvBuffMaster;
			if (g_pApp_Commtest->RcvBuffMaster[0] == '\0')
			{
				TestResult("FAIL");
				break;
			}

			int sepCount = GetFindCharCount(RcvBuff, '\n');
			int cnt = 0;
			int sum = 0;

			CString* Arr_RcvBuff = new CString[sepCount + 1];
			while (AfxExtractSubString(strTok, RcvBuff, cnt, '\n'))
			{
				Arr_RcvBuff[cnt++] = strTok;
			}

			for (cnt = 2; cnt <= sepCount - 1; cnt++)
			{
				//ex)[20762]RL(5)_466(432)\r
//				tmp = _ttoi(Arr_RcvBuff[cnt].Mid(Arr_RcvBuff[cnt].Find('_') + 1, Arr_RcvBuff[cnt].Find('\r') - Arr_RcvBuff[cnt].Find('_')));
				sscanf_s((const char*)Arr_RcvBuff[cnt], "[%d]RL(%d)_%d(%d)\r", &nv1, &nv2, &nv3, &nv4);
				sum += nv3;
			}
			m_iTargetRssi[i/127] = sum/(cnt - 2);

			delete[] Arr_RcvBuff;
		}
		else
		{
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			if (g_pApp_Commtest->RcvBuffMaster[0] == '\0')
			{
				TestResult("FAIL");
				break;
			}

			int sepCount = GetFindCharCount(RcvBuff, '\n');
			int cnt = 0;
			int sum = 0;

			CString* Arr_RcvBuff = new CString[sepCount + 1];
			while (AfxExtractSubString(strTok, RcvBuff, cnt, '\n'))
			{
				Arr_RcvBuff[cnt++] = strTok;
			}

			for (cnt = 2; cnt <= sepCount - 1; cnt++)
			{
//				sum += _ttoi(Arr_RcvBuff[cnt].Mid(Arr_RcvBuff[cnt].Find('_') + 1, Arr_RcvBuff[cnt].Find('\r') - Arr_RcvBuff[cnt].Find('_')));
				sscanf_s((const char*)Arr_RcvBuff[cnt], "[%d]RL(%d)_%d(%d)\r", &nv1, &nv2, &nv3, &nv4);
				sum += nv3;
			}
			m_iMineRssi[i/127] = sum / (cnt - 2);

			delete[] Arr_RcvBuff;
		}
	}
#if 0
	PortControl("PIW03330");
	PortControl("PIW03340");
	PortControl("PIW03350");
	PortControl("PIW03360");
	PortControl("PIW03370");
#endif
	m_iRssiCheckDone = TRUE;
}

void CCommtest::IoTest(int val)
{
	m_bEnterTheTest = FALSE;

	switch (val)
	{
		/*TEST pCOM LOGIC INPUT TEST*/
	case 1:
		PortControl("PIW03171");
		break;
	case 2:
		PortControl("PIW03170");
		PortControl("PIW03181");
		break;
	case 3:
		PortControl("PIW03180");
		PortControl("PIW03191");
		break;
	case 4:
		PortControl("PIW03190");
		PortControl("PIW03201");
		break;
	case 5:
		PortControl("PIW03200");
		PortControl("PIW03211");
		break;
	case 6:
		PortControl("PIW03210");
		PortControl("PIW03221");
		break;
	case 7:
		PortControl("PIW03220");
		PortControl("PIW03231");
		break;
	case 8:
		PortControl("PIW03230");
		PortControl("PIW03241");
		break;
	case 9:
		PortControl("PIW03240");
		PortControl("PIW03251");
		break;
	case 10:
		PortControl("PIW03250");
		PortControl("PIW03261");
		break;
	case 11:
		PortControl("PIW03260");
		PortControl("PIW03271");
		break;
	case 12:
		PortControl("PIW03270");
		PortControl("PIW03281");
		break;
	case 13:
		PortControl("PIW03280");
		PortControl("PIW03291");
		break;
	case 14:
		PortControl("PIW03290");
		PortControl("PIW03301");
		break;
	case 15:
		PortControl("PIW03300");
		PortControl("PIW03311");
		break;
	case 16:
		PortControl("PIW03310");
		PortControl("PIW03321");
		break;
		/*TEST pCOM LOGIC OUTPUT TEST*/
	case 17:
		PortControl("PIW03320");
		PortControl("PIW03011");
		break;
	case 18:
		PortControl("PIW03010");
		PortControl("PIW03021");
		break;
	case 19:
		PortControl("PIW03020");
		PortControl("PIW03031");
		break;
	case 20:
		PortControl("PIW03030");
		PortControl("PIW03041");
		break;
	case 21:
		PortControl("PIW03040");
		PortControl("PIW03051");
		break;
	case 22:
		PortControl("PIW03050");
		PortControl("PIW03061");
		break;
	case 23:
		PortControl("PIW03060");
		PortControl("PIW03071");
		break;
	case 24:
		PortControl("PIW03070");
		PortControl("PIW03081");
		break;
	case 25:
		PortControl("PIW03080");
		PortControl("PIW03091");
		break;
	case 26:
		PortControl("PIW03090");
		PortControl("PIW03101");
		break;
	case 27:
		PortControl("PIW03100");
		PortControl("PIW03111");
		break;
	case 28:
		PortControl("PIW03110");
		PortControl("PIW03121");
		break;
	case 29:
		PortControl("PIW03120");
		PortControl("PIW03131");
		break;
	case 30:
		PortControl("PIW03130");
		PortControl("PIW03141");
		break;
	case 31:
		PortControl("PIW03140");
		PortControl("PIW03151");
		break;
	case 32:
		PortControl("PIW03150");
		PortControl("PIW03161");
		break;
	}

	processdelay(150);
}

void CCommtest::IoTrigger()
{
	m_bEnterTheTest = FALSE;

	modesel(59, 0);	//SLAVE TRIGGER
}

UINT Thread(LPVOID lParam)
{
	CCommtest *pC;
	pC = (CCommtest*)lParam;
	
	BOOL returnval = FALSE;
	int i;

	pC->PortControl("PIW03330");
	pC->PortControl("PIW03340");
	pC->PortControl("PIW03350");
	pC->PortControl("PIW03360");
	pC->PortControl("PIW03370");

	while (pC->m_bThreadStatus)
	{
		for (i = 1; i <= 30; i++)
		{
			if ((i & 0x01) == 0x01)	pC->PortControl("PIW03331");
			if ((i & 0x02) == 0x02)	pC->PortControl("PIW03341");
			if ((i & 0x04) == 0x04)	pC->PortControl("PIW03351");
			if ((i & 0x08) == 0x08)	pC->PortControl("PIW03361");
			if ((i & 0x10) == 0x10)	pC->PortControl("PIW03371");

			pC->processdelay(550);
			if ((g_pApp_Commtest->InputPort[6] & 0x04) != 0x04)
			{
				break;
			}
			pC->processdelay(50);
			if (((g_pApp_Commtest->OutputPort[4] & 0x1F) & i) != i)
			{
				break;
			}

			if ((i & 0x01) == 0x01)	pC->PortControl("PIW03330");
			if ((i & 0x02) == 0x02)	pC->PortControl("PIW03340");
			if ((i & 0x04) == 0x04)	pC->PortControl("PIW03350");
			if ((i & 0x08) == 0x08)	pC->PortControl("PIW03360");
			if ((i & 0x10) == 0x10)	pC->PortControl("PIW03370");
		}

		if ((i >= (_ttoi(pC->m_sGooffAtt) - 1)) && ((g_pApp_Commtest->InputPort[6] & 0x04) != 0x04))	returnval = TRUE;
	}

	return 0;
}

void CCommtest::OnBnClickedVerCheck()
{
#if 1
	m_bEnterTheTest = FALSE;

	CompStr = ConverToHex(_T("V=") + m_sCompVersion);
	CompStr = _T("[V=") + m_sCompVersion + CompStr + _T("]");

	SendData_to_test(_T("<V56>"));
//	SendData_to_test(_T("ver\r"));
	processdelay(200);
	ReadData_to_test(1);
#else
	DclineCheck(0);
#endif
}

void CCommtest::ConfClear(int which)
{
	m_bEnterTheTest = FALSE;

//	m_pProgressThread = AfxBeginThread(ProgressThread_CommTest, this);

	if (!which)	SendData_to_test("config clear\r");
	else		SendData_to_ref("config clear\r");

	processdelay(3500);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CCommtest::OnBnClickedConfClear()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test(_T("config clear\r"));

	processdelay(3000);
	ReadData_to_test(1);
}

void CCommtest::OnBnClickedReset()
{
#if 0
	m_bEnterTheTest = FALSE;

	SendData_to_test(_T("\x2XRESET\x3"));
	ReadData_to_test(2000);
#endif
}

void CCommtest::Xpns(int which, int val)
{
	modesel(REFSELECT, 1);	//REF. SEL ON (Master Wait)

	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	PortCmd.Format("<XPNS=%d>", val);

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	Sleep(TIMER_CALL_DELAY);

	if (!which)	SendData_to_test("<XPNS=0>");
	else		SendData_to_ref("<XPNS=0>");

	if (!which)	ReadData_to_test(RESET_DELAY);
	else		ReadData_to_ref(RESET_DELAY);

	modesel(REFSELECT, 0);	//REF. SEL ON (Master Act)
}

void CCommtest::PortControl(CString cmd)
{
	CString msg = _T("");

	msg += (TCHAR)STX;
	msg += cmd;
	msg += (TCHAR)ETX;

	g_pApp_Commtest->m_MySocket->Send(msg, strlen(msg));

	Sleep(10);
}


void CCommtest::mbase(int which, int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("MBSET=%d", dat);
	PortCmd.Format("<%s%s>", tmp, ConverToHex(tmp));

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}


void CCommtest::minfo(int which, int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("MINFO=%d", dat);
	PortCmd.Format("<%s%s>", tmp, ConverToHex(tmp));

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}


void CCommtest::mtemp(int which)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("MTEMP");
	PortCmd.Format("<%s%s>", tmp, ConverToHex(tmp));

	if (!which)	SendData_to_test(PortCmd);
	else		SendData_to_ref(PortCmd);

	processdelay(100);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}


void CCommtest::pcomioport(int on)
{
	int i;
	CString Cmd;

	for (i = 1; i < 33; i++)
	{
		Cmd.Format("PIW03%02d%d", i, on);
		PortControl(Cmd);

		processdelay(150);
	}
}


void CCommtest::tempaging(DWORD dat)
{
//	m_pProgressThread = AfxBeginThread(ProgressThread_CommTest, this);
	processdelay(dat);
}


void CCommtest::TestResult(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision->MoveWindow(rcParent.left + (rcParent.Width() - g_rcClientDecisionDlg_Commtest.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcClientDecisionDlg_Commtest.Height()) / 2 - YPOS_OFFSET, g_rcClientDecisionDlg_Commtest.Width(), g_rcClientDecisionDlg_Commtest.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		if (m_iTestStep == MISO_IOTEST_08)
		{
			if (!m_iBits)
			{
				m_cIoTest.SetCheck(1);
				m_iTestStep = IO_TRIGGER;
			}
		}
		else	m_iTestStep++;

		m_bEnterTheTest = TRUE;
		m_iTestResult = TRUE;
		GetDlgItem(IDC_RCVSTAT_COMM)->Invalidate();
		m_EditRcvStatComm.SetWindowText(_T(msg));
		m_iRetry_ReadPort = 10;

//		if (m_iTestStep == CONFIGCLEAR_T + 1)				m_cConf.SetCheck(1);
		if (m_iTestStep == VERCHK + 1)						m_cVerChk.SetCheck(1);
		if (m_iTestStep == XPNS + 1)						m_cRcvWaitRssi.SetCheck(1);
//		if (m_iTestStep == MISO_IOTEST_08 + 1)				if (m_b8bit)	m_cIoTest.SetCheck(1);
		if (m_iTestStep == MISO_IOTEST_16 + 1)				if (!m_iBits)	m_cIoTest.SetCheck(1);
		if (m_iTestStep == MINE_USERDATA_2000_CHECK + 1)	m_cUserData.SetCheck(1);
		if (m_iTestStep == TARGET_RSSI + 1)					m_cTargetRssi.SetCheck(1);
//		if (m_iTestStep == MINE_RSSI + 1)					m_cMineRssi.SetCheck(1);
		if (m_iTestStep == AUTOGAINCHECK + 1)				m_cAutoGainCon.SetCheck(1); 
		if (m_iTestStep == GOOFFCHECK + 1)					m_cGooff.SetCheck(1);
		if (m_iTestStep == TEMP_ANGLE_CHECK + 1)			m_cAngleCorr.SetCheck(1);

		if (m_pProgressThread != NULL)	m_pProgressThread = NULL;
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_RCVSTAT_COMM)->Invalidate();
		m_EditRcvStatComm.SetWindowText(_T(msg));
		OnBnClickedAutoStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_cDcline.SetCheck(1);

		m_iTestResult = TRUE;
		GetDlgItem(IDC_RCVSTAT_COMM)->Invalidate();
		m_EditRcvStatComm.SetWindowText(_T(msg));
		OnBnClickedAutoStop();

		m_pDlgDecision->SetDecision(TRUE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
	else
	{
		if (m_iTestStep >= MOSI_IOTEST_01 && m_iTestStep <= MISO_IOTEST_16)
		{
			if (m_iRetry_ReadPort > 0)
			{
				m_bEnterTheTest = TRUE;
				m_iRetry_ReadPort--;
			}
			else
			{
				m_iTestResult = FALSE;
				GetDlgItem(IDC_RCVSTAT_COMM)->Invalidate();
				m_EditRcvStatComm.SetWindowText(_T(msg));
				OnBnClickedAutoStop();

				m_pDlgDecision->SetDecision(FALSE);// decision
				m_pDlgDecision->ShowWindow(SW_SHOW);// decision
				m_pDlgDecision->AutoHide(3000);
			}
		}
		else
		{
			m_iTestResult = FALSE;
			GetDlgItem(IDC_RCVSTAT_COMM)->Invalidate();

			msg.Format("%s_%d", msg, m_iTestStep);

			m_EditRcvStatComm.SetWindowText(_T(msg));
			OnBnClickedAutoStop();

			m_pDlgDecision->SetDecision(FALSE);// decision
			m_pDlgDecision->ShowWindow(SW_SHOW);// decision
			m_pDlgDecision->AutoHide(3000);
		}
	}
}


UINT ThreadStatus_Commtest(LPVOID lParam)
{
	CCommtest *pCCommtest;
	pCCommtest = (CCommtest*)lParam;

	CString RcvBuff, tmp, strTok;
	BOOL result = 0;

	pCCommtest->m_fAngleX[0] = 0.0;
	pCCommtest->m_fAngleX[1] = 0.0;
	pCCommtest->m_fAngleY[0] = 0.0;
	pCCommtest->m_fAngleY[1] = 0.0;
	pCCommtest->m_fAngleZ[0] = 0.0;
	pCCommtest->m_fAngleZ[1] = 0.0;
	pCCommtest->m_fTemp[0] = 0.0;
	pCCommtest->m_fTemp[1] = 0.0;

	while (pCCommtest->m_bThreadStatus)
	{
		if (pCCommtest->m_iTestStep == pCOMWAIT)
		{
			if (pCCommtest->m_bEnterTheTest)
			{
				/*
				TEST pCOM -> SLAVE
				REF. pCOM -> MASTER
				*/
#if 0
				pCCommtest->modesel(REFSELECT, 1);
				pCCommtest->modesel(51, 1);
				pCCommtest->modesel(50, 1);
				pCCommtest->modesel(52, 0);
				pCCommtest->modesel(REFSELECT, 0);
				pCCommtest->modesel(51, 0);
#else
				pCCommtest->modesel(38, 0);	//ATT Line
				pCCommtest->modesel(REFSELECT, 1);	//REF. SEL ON
				pCCommtest->modesel(DUTSELECT, 1);	//DUT. SEL ON

				if (!pCCommtest->m_iJigMode)
				{
#if 0
					//dut pCOM Mode 제어 삭제
					pCCommtest->modesel(50, 1);	//DUT. MODE SLAVE 동작
#endif
					pCCommtest->modesel(39, 0);	//REF. MODE MASTER 동작
				}
				else
				{
#if 0
					//dut pCOM Mode 제어 삭제
					pCCommtest->modesel(50, 0);	//DUT. MODE MASTER 동작
#endif
					pCCommtest->modesel(39, 1);	//REF. MODE SLAVE 동작
				}

				pCCommtest->modesel(REFSELECT, 0); //REF. SEL OFF
				pCCommtest->modesel(DUTSELECT, 0);	//DUT. SEL OFF
				pCCommtest->modesel(59, 1);	//DUT. TRIGGER
#if 0
				//LATCH INIT 삭제
				pCCommtest->modesel(39, 1);	//LATCH INIT
				pCCommtest->modesel(39, 0);	//LATCH INIT
#endif
#endif
				pCCommtest->enterthenormal(0);
				pCCommtest->enterthenormal(1);

				pCCommtest->m_bEnterTheTest = TRUE;
			}
			pCCommtest->m_iTestStep++;
		}
		if (pCCommtest->m_iTestStep == pCOM_TEMP_ANGLE)
		{
			pCCommtest->processdelay(5000);	//pCOM Booting & Motion Init
			pCCommtest->mbase(0, 0); // Reset
			pCCommtest->processdelay(100);
			pCCommtest->mbase(0, 1); // cal 0
			pCCommtest->processdelay(1000);
			pCCommtest->mbase(0, 2); // cal 2 

			pCCommtest->minfo(0, 3);
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			AfxExtractSubString(strTok, RcvBuff, 2, '=');
			pCCommtest->m_fAngleX[0] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
			pCCommtest->m_sPreAngleX.Format("%2.2f", pCCommtest->m_fAngleX[0]);

			pCCommtest->minfo(0, 4);
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			AfxExtractSubString(strTok, RcvBuff, 2, '=');
			pCCommtest->m_fAngleY[0] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
			pCCommtest->m_sPreAngleY.Format("%2.2f", pCCommtest->m_fAngleY[0]);

			pCCommtest->minfo(0, 5);
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			AfxExtractSubString(strTok, RcvBuff, 2, '=');
			pCCommtest->m_fAngleZ[0] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
			pCCommtest->m_sPreAngleZ.Format("%3.1f", pCCommtest->m_fAngleZ[0]);

			pCCommtest->mtemp(0);
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			AfxExtractSubString(strTok, RcvBuff, 1, '=');
			pCCommtest->m_fTemp[0] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
			pCCommtest->m_sPreTemp.Format("%2.2f", pCCommtest->m_fTemp[0]);

			pCCommtest->pcomioport(1);
			pCCommtest->PostMessage(UM_UPDATE, 0, 0);
			pCCommtest->tempaging(_ttoi(pCCommtest->m_sTemptime));
			pCCommtest->TestResult(_T("PASS"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == TARGET_RFIDCH_CHANGE)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->rfidch(0, pCCommtest->m_sCommTestRfid);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_VHLID_CHANGE)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->vhlid(0, pCCommtest->m_sCommTestVhlid);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		else if (pCCommtest->m_iTestStep == MINE_RFIDCH_CHANGE)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->rfidch(0, pCCommtest->m_sCommTestRfid);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == MINE_VHLID_CHANGE)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->vhlid(1, pCCommtest->m_sCommTestVhlid);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		else if (pCCommtest->m_iTestStep == SWITCHCONSOLE_M_1 || pCCommtest->m_iTestStep == SWITCHCONSOLE_M_2 || pCCommtest->m_iTestStep == SWITCHCONSOLE_M_3)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->entertheconsole(0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, ">") != 0)		pCCommtest->TestResult(_T("PASS"));
			else																pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == SWITCHCONSOLE_T_1 /*|| pCCommtest->m_iTestStep == SWITCHCONSOLE_T_2*/)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->entertheconsole(1);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, ">") != 0)		pCCommtest->TestResult(_T("PASS"));
			else																	pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == CONFIGCLEAR_M)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->ConfClear(0);

			if ((strstr((const char*)g_pApp_Commtest->RcvBuff, "config clear") != 0) &&
				(strstr((const char*)g_pApp_Commtest->RcvBuff, "Clear Log Success") != 0))		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == CONFIGCLEAR_T)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->ConfClear(1);

			if ((strstr((const char*)g_pApp_Commtest->RcvBuffMaster, "config clear") != 0) &&
				(strstr((const char*)g_pApp_Commtest->RcvBuffMaster, "Clear Log Success") != 0))		pCCommtest->TestResult(_T("PASS"));
			else																						pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		else if (pCCommtest->m_iTestStep == SWITCHNORMAL_M_1 || pCCommtest->m_iTestStep == SWITCHNORMAL_M_2 || pCCommtest->m_iTestStep == SWITCHNORMAL_M_3)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->enterthenormal(0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, "exit") != 0)		pCCommtest->TestResult(_T("PASS"));
			else																pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == SWITCHNORMAL_T_1 /*|| pCCommtest->m_iTestStep == SWITCHNORMAL_T_2*/)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->enterthenormal(1);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, "exit") != 0)	pCCommtest->TestResult(_T("PASS"));
			else																	pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == VERCHK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->OnBnClickedVerCheck();

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)	pCCommtest->TestResult(_T("PASS"));
			else																			pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == DEBUG6_M)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(0, 6);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		else if (pCCommtest->m_iTestStep == XPNS)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->Xpns(0, 1);

			CString RcvBuff, tmp, strTok;
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			tmp = RcvBuff.Mid(RcvBuff.Find("XPNS=0"), strlen(RcvBuff));
			AfxExtractSubString(strTok, tmp, 0, ',');
			AfxExtractSubString(strTok, tmp, 1, ',');
			AfxExtractSubString(strTok, tmp, 2, ',');

			if (strlen(strTok) > 3)
			{
				AfxExtractSubString(strTok, strTok, 0, ']');
			}
			if (_ttoi(strTok) <= 300 && _ttoi(strTok) >= 200)	pCCommtest->TestResult(_T("PASS"));
			else												pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == READYTORCVRSSI)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->rcv_debugmsg(0);

			CString RcvBuff, strTok, CompareStr;
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			int sepCount = pCCommtest->GetFindCharCount(RcvBuff, '\n');
			int cnt = 0;
			int which = 0;
			
			CString* Arr_RcvBuff = new CString[sepCount + 1];
			while (AfxExtractSubString(strTok, RcvBuff, cnt, '\n'))
			{
				Arr_RcvBuff[cnt++] = strTok;
			}

			for (cnt = 1; cnt <= sepCount - 1; cnt++)
			{
				/*V0.95 기준 출력 메시지 달라짐*/
//				CompareStr = Arr_RcvBuff[cnt].Right(3); 
				which = Arr_RcvBuff[cnt].Find(':');
				CompareStr = Arr_RcvBuff[cnt].Mid(which + 1, sizeof(Arr_RcvBuff[cnt]) - 1);

				if (_ttoi(CompareStr) > 300 || _ttoi(CompareStr) < 200)	break;
			}

			if (cnt == sepCount)		pCCommtest->TestResult(_T("PASS"));
			else						pCCommtest->TestResult(_T("FAIL"));

			delete[] Arr_RcvBuff;
		}
#endif
		else if (pCCommtest->m_iTestStep == PORTCLEAR1)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->PortClear(0);

			pCCommtest->m_iTestStep++;
		}
#if 0
		else if (pCCommtest->m_iTestStep == RAMTEST)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->RamTest(0, 13);

			/*ramtest/romtest 명령어는 일정 시간 동안 계속 수신되는 메시지가 있기 때문에*/
			/*Timer로 일정 시간마다 수신되는 메시지를 갱신하여 최종 문자를 비교한다.*/
			/*스레드 내에서 처리하기 곤란하여 OnTimer 함수 이용*/
//			if (strstr((const char*)g_pApp_Commtest->RcvBuff, "Test Success") != 0)		pCCommtest->TestResult(_T("PASS"));
//			else																		pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == ROMTEST)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->RomTest(0, 32);
		}
#endif
		else if (pCCommtest->m_iTestStep == IO_PORT_CLEAR)
		{
			pCCommtest->pcomioport(0);
			pCCommtest->TestResult(_T("PASS"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_01)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(1);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x01 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x01 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_02)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(2);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x02 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x02 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_03)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(3);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x04 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x04 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_04)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(4);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x08 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x08 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_05)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(5);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x10 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x10 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_06)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(6);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x20 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x20 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_07)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(7);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x40 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x40 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_08)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(8);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[0] == 0x80 /*&& g_pApp_Commtest->InputPort[1] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));

				pCCommtest->PortControl("PIW03240");
				pCCommtest->m_iTestStep = MISO_IOTEST_01;
			}
			else
			{
				if (g_pApp_Commtest->InputPort[0] == 0x80 && g_pApp_Commtest->InputPort[1] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_09)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(9);

			if (g_pApp_Commtest->InputPort[1] == 0x01 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_10)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(10);

			if (g_pApp_Commtest->InputPort[1] == 0x02 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_11)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(11);

			if (g_pApp_Commtest->InputPort[1] == 0x04 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_12)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(12);

			if (g_pApp_Commtest->InputPort[1] == 0x08 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_13)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(13);

			if (g_pApp_Commtest->InputPort[1] == 0x10 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_14)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(14);

			if (g_pApp_Commtest->InputPort[1] == 0x20 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_15)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(15);

			if (g_pApp_Commtest->InputPort[1] == 0x40 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MOSI_IOTEST_16)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(16);

			if (g_pApp_Commtest->InputPort[1] == 0x80 && g_pApp_Commtest->InputPort[0] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_01)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(17);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x01 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x01 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_02)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(18);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x02 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x02 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_03)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(19);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x04 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x04 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_04)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(20);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x08 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x08 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_05)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(21);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x10 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x10 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_06)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(22);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x20 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x20 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_07)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(23);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x40 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x40 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_08)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(24);

			if (!pCCommtest->m_iBits)
			{
				if (g_pApp_Commtest->InputPort[2] == 0x80 /*&& g_pApp_Commtest->InputPort[3] == 0x00*/)	pCCommtest->TestResult(_T("PASS"));
				else																					pCCommtest->TestResult(_T("FAIL"));

				pCCommtest->PortControl("PIW03080");
			}
			else
			{
				if (g_pApp_Commtest->InputPort[2] == 0x80 && g_pApp_Commtest->InputPort[3] == 0x00)	pCCommtest->TestResult(_T("PASS"));
				else																				pCCommtest->TestResult(_T("FAIL"));
			}
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_09)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(25);

			if (g_pApp_Commtest->InputPort[3] == 0x01 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_10)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(26);

			if (g_pApp_Commtest->InputPort[3] == 0x02 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_11)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(27);

			if (g_pApp_Commtest->InputPort[3] == 0x04 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_12)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(28);

			if (g_pApp_Commtest->InputPort[3] == 0x08 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_13)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(29);

			if (g_pApp_Commtest->InputPort[3] == 0x10 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_14)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(30);

			if (g_pApp_Commtest->InputPort[3] == 0x20 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_15)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(31);

			if (g_pApp_Commtest->InputPort[3] == 0x40 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MISO_IOTEST_16)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTest(32);

			if (g_pApp_Commtest->InputPort[3] == 0x80 && g_pApp_Commtest->InputPort[2] == 0x00)	pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == IO_TRIGGER)
		{
			if (IDYES == AfxMessageBox(_T("iMAN Trigger 준비"), MB_YESNO))
			{
				if (pCCommtest->m_bEnterTheTest)	pCCommtest->IoTrigger();

				pCCommtest->processdelay(1000);

				if (IDYES == AfxMessageBox(_T("iMAN Trigger 확인"), MB_YESNO))
				{
//					pCCommtest->modesel(59, 0);	//SLAVE TRIGGER
					pCCommtest->pcomioport(1);
					pCCommtest->TestResult(_T("PASS"));
				}
				else if (IDNO)
				{
//					pCCommtest->modesel(59, 0);	//SLAVE TRIGGER
					pCCommtest->TestResult(_T("FAIL"));
				}
			}
			else if (IDNO)
			{
				pCCommtest->TestResult(_T("STOP"));
			}
		}
#if 0
		else if (pCCommtest->m_iTestStep == MINE_RCV_DATA_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->datachangecheck(1);

			CString vhlid, rfidch;
			vhlid.Format("%02x|%02x|%02x|%02x|%02x|%02x"
				, pCCommtest->m_sCommTestVhlid[0]
				, pCCommtest->m_sCommTestVhlid[1]
				, pCCommtest->m_sCommTestVhlid[2]
				, pCCommtest->m_sCommTestVhlid[3]
				, pCCommtest->m_sCommTestVhlid[4]
				, pCCommtest->m_sCommTestVhlid[5]);
			rfidch.Format("%02x|%02x|%02x|%02x|%02x|%02x"
				, (0x0b << 4) | (pCCommtest->m_sCommTestRfid[0] -'0')
				, (0x09 << 4) | (pCCommtest->m_sCommTestRfid[1] - '0')
				, (0x06 << 4) | (pCCommtest->m_sCommTestRfid[2] - '0')
				, (0x0a << 4) | (pCCommtest->m_sCommTestRfid[3] - '0')
				, (0x07 << 4) | (pCCommtest->m_sCommTestRfid[4] - '0')
				, _ttoi(pCCommtest->m_sCommTestRfid) % 122);
			if ((strstr((const char*)g_pApp_Commtest->RcvBuffMaster, vhlid) != 0) &&
				(strstr((const char*)g_pApp_Commtest->RcvBuffMaster, rfidch) != 0))
				pCCommtest->TestResult(_T("PASS"));
			else
			{
				pCCommtest->debugmode(1, 0);
				pCCommtest->TestResult(_T("FAIL"));
			}
		}
#endif
#if 0
		else if (pCCommtest->m_iTestStep == TARGET_RCV_DATA_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->datachangecheck(0);

			CString vhlid, rfidch;
			vhlid.Format("%02x|%02x|%02x|%02x|%02x|%02x"
				, pCCommtest->m_sCommTestVhlid[0]
				, pCCommtest->m_sCommTestVhlid[1]
				, pCCommtest->m_sCommTestVhlid[2]
				, pCCommtest->m_sCommTestVhlid[3]
				, pCCommtest->m_sCommTestVhlid[4]
				, pCCommtest->m_sCommTestVhlid[5]);
			rfidch.Format("%02x|%02x|%02x|%02x|%02x|%02x"
				, (0x0b << 4) | (pCCommtest->m_sCommTestRfid[0] - '0')
				, (0x09 << 4) | (pCCommtest->m_sCommTestRfid[1] - '0')
				, (0x06 << 4) | (pCCommtest->m_sCommTestRfid[2] - '0')
				, (0x0a << 4) | (pCCommtest->m_sCommTestRfid[3] - '0')
				, (0x07 << 4) | (pCCommtest->m_sCommTestRfid[4] - '0')
				, _ttoi(pCCommtest->m_sCommTestRfid) % 122);
			if ((strstr((const char*)g_pApp_Commtest->RcvBuff, vhlid) != 0) &&
				(strstr((const char*)g_pApp_Commtest->RcvBuff, rfidch) != 0))
				pCCommtest->TestResult(_T("PASS"));
			else
			{
				pCCommtest->debugmode(0, 0);
				pCCommtest->TestResult(_T("FAIL"));
			}
		}
#endif
		else if (pCCommtest->m_iTestStep == TARGET_USERDATA_2000)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserLength(1, 2000);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_USERDATA_2000)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserLength(0, 2000);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_USERDATA_100_CHECK)
		{
			//pCOM Boot Delay
			pCCommtest->processdelay(3000);
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(1, 100);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff2) == 100)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_USERDATA_300_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(1, 300);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff2) == 300)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_USERDATA_500_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(1, 500);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff2) == 500)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_USERDATA_1000_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(1, 1000);

//			int j = strlen((const char*)g_pApp_Commtest->RcvBuff2);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff2) == 1000)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_USERDATA_2000_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(1, 2000);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff2) == 2000)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_USERDATA_100_CHECK)
		{
			pCCommtest->processdelay(2000);
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(0, 100);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff3) == 100)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_USERDATA_300_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(0, 300);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff3) == 300)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_USERDATA_500_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(0, 500);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff3) == 500)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_USERDATA_1000_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(0, 1000);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff3) == 1000)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_USERDATA_2000_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->UserDataCheck(0, 2000);

			if (strlen((const char*)g_pApp_Commtest->RcvBuff3) == 2000)		pCCommtest->TestResult(_T("PASS"));
			else															pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == AUTOGAINCONTROL_M_1)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->AutoGainControl(0, 0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == AUTOGAINCONTROL_T_1)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->AutoGainControl(1, 0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		else if (pCCommtest->m_iTestStep == DEBUG12_T_1)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(1, 12);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TARGET_RSSI)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->check_rssi(0);

			if (pCCommtest->m_iRssiCheckDone)
			{
				if (pCCommtest->m_iTargetRssi[0] > pCCommtest->m_iTargetRssi[1] + 20)
				{
					pCCommtest->TestResult(_T("PASS"));
				}
				else
				{
					pCCommtest->enterthenormal(0);
					pCCommtest->enterthenormal(1);
					pCCommtest->debugmode(1, 0);
					pCCommtest->TestResult(_T("FAIL"));
					return 0;
				}
				pCCommtest->PortClear(1);
			}
		}
		else if (/*pCCommtest->m_iTestStep == DEBUG0_T_1 || pCCommtest->m_iTestStep == DEBUG0_T_1 || */pCCommtest->m_iTestStep == DEBUG0_T_2)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(1, 0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					{ pCCommtest->TestResult(_T("FAIL")); pCCommtest->debugmode(1, 0); pCCommtest->enterthenormal(0); pCCommtest->enterthenormal(1); }
		}
		else if (pCCommtest->m_iTestStep == DEFAULT_MR0_M)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->mr0(0, DEFAULT_MR0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == DEBUG12_M_1)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(0, 12);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINE_RSSI)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->check_rssi(1);

			if (pCCommtest->m_iRssiCheckDone)
			{
				int i;
				for (i = 0; i < 127; i+=10)
				{
					if (pCCommtest->m_iMineRssi[i] < pCCommtest->m_iMineRssi[i+1])
					{
						pCCommtest->enterthenormal(0);
						pCCommtest->enterthenormal(1);
						pCCommtest->debugmode(0, 0);
						pCCommtest->TestResult(_T("FAIL"));
						break;
					}
				}
				pCCommtest->TestResult(_T("PASS"));

				pCCommtest->PortClear(0);
			}
		}
		else if (/*pCCommtest->m_iTestStep == DEBUG0_M_1 || pCCommtest->m_iTestStep == DEBUG0_M_2 || */pCCommtest->m_iTestStep == DEBUG0_M_3)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(0, 0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				{ pCCommtest->TestResult(_T("FAIL")); pCCommtest->debugmode(0, 0); pCCommtest->enterthenormal(0); pCCommtest->enterthenormal(1); }
		}
		else if (pCCommtest->m_iTestStep == DEFAULT_MR0_T)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->mr0(1, DEFAULT_MR0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		else if (pCCommtest->m_iTestStep == AUTOGAINCHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->AutogainCheck(0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, "mr0=28(0)") != 0)	pCCommtest->TestResult(_T("PASS"));
			else																	pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == GOOFFCHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	result = pCCommtest->GoOffCheck(0);

			if (result == TRUE)	pCCommtest->TestResult(_T("PASS"));
			else				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == TEMP_ANGLE_CHECK)
		{
			if (pCCommtest->m_bEnterTheTest)
			{
				pCCommtest->enterthenormal(0);

				pCCommtest->minfo(0, 3);
				RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
				AfxExtractSubString(strTok, RcvBuff, 2, '=');
				pCCommtest->m_fAngleX[1] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
				pCCommtest->m_sAngleX.Format("%2.2f", pCCommtest->m_fAngleX[1]);

				pCCommtest->minfo(0, 4);
				RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
				AfxExtractSubString(strTok, RcvBuff, 2, '=');
				pCCommtest->m_fAngleY[1] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
				pCCommtest->m_sAngleY.Format("%2.2f", pCCommtest->m_fAngleY[1]);

				pCCommtest->minfo(0, 5);
				RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
				AfxExtractSubString(strTok, RcvBuff, 2, '=');
				pCCommtest->m_fAngleZ[1] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
				pCCommtest->m_sAngleZ.Format("%3.1f", pCCommtest->m_fAngleZ[1]);

				pCCommtest->mtemp(0);
				RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
				AfxExtractSubString(strTok, RcvBuff, 1, '=');
				pCCommtest->m_fTemp[1] = (float)atof(strTok.Mid(0, strlen(strTok) - 3));
				pCCommtest->m_sTemp.Format("%2.2f", pCCommtest->m_fTemp[1]);

				pCCommtest->PostMessage(UM_UPDATE, 0, 0);
			}
			/*200610 온도 보정 OFFSET 값 0.03 -> 0.07*/
			/*200610 온도 차이 수정 8.0도 이상*/

			float fTemp = pCCommtest->m_fTemp[1] - pCCommtest->m_fTemp[0];
			float ftolerance = (float)(0.01 * fTemp + 0.07);

			if ((pCCommtest->m_fTemp[1] - pCCommtest->m_fTemp[0] >= 8.0) &&
				((pCCommtest->m_fAngleX[1] - pCCommtest->m_fAngleX[0] <= ftolerance) && (pCCommtest->m_fAngleX[1] - pCCommtest->m_fAngleX[0] >= -(ftolerance))) &&
				((pCCommtest->m_fAngleY[1] - pCCommtest->m_fAngleY[0] <= ftolerance) && (pCCommtest->m_fAngleY[1] - pCCommtest->m_fAngleY[0] >= -(ftolerance))) &&
				((pCCommtest->m_fAngleZ[1] - pCCommtest->m_fAngleZ[0] <= 0.1) && (pCCommtest->m_fAngleZ[1] - pCCommtest->m_fAngleZ[0] >= -0.1)))	pCCommtest->TestResult(_T("PASS"));
			else																																	pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == DCLINECHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	result = pCCommtest->DclineCheck(0);

			pCCommtest->modesel(38, 0);	//ATT Line

			if (result == TRUE)	pCCommtest->TestResult(_T("END"));
			else				pCCommtest->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCCommtest->m_iTestStep == AUTOGAINCONTROL_M_2)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->AutoGainControl(0, 1);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == AUTOGAINCONTROL_T_2)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->AutoGainControl(1, 1);

			if (strstr((const char*)g_pApp_Commtest->RcvBuffMaster, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																					pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == DEBUG14_M_1)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(0, 14);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == MINEAUTOGAINCHECK)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->AutoGainControlCheck(0);

			CString RcvBuff, strTok, CompareStr;
			RcvBuff = (CString)g_pApp_Commtest->RcvBuff;
			int sepCount = pCCommtest->GetFindCharCount(RcvBuff, '\n');
			int cnt = 0;
			int result = TRUE;
			int lastbufidx = 0;

			CString* Arr_RcvBuff	= new CString[sepCount + 1];
			BYTE* Arr_RssiStepBuff	= new BYTE[sepCount + 1];
			BYTE* Arr_RssiBuff		= new BYTE[sepCount + 1];

			while (AfxExtractSubString(strTok, RcvBuff, cnt, '\n'))
			{
				Arr_RcvBuff[cnt++] = strTok;
			}
			/*
			기대 출력값

			[35480]1:1,0(28):0
			[35491]1:0,3(18):3
			[35502]1:0,3(8):3
			*/
			for (cnt = 1; cnt <= sepCount - 1; cnt++)
			{
				/*V0.95 기준 출력 메시지 달라짐*/
				//CompareStr = Arr_RcvBuff[cnt].Right(3); 
				//ex)[18167]1:1,2(0):3
				if (Arr_RcvBuff[cnt].Find(',') == -1 || Arr_RcvBuff[cnt].Find('(') == -1 || Arr_RcvBuff[cnt].Find(')') == -1)
				{
					lastbufidx = cnt - 1;
					break;
				}
				else
				{
					Arr_RssiStepBuff[cnt - 1] = _ttoi(Arr_RcvBuff[cnt].Mid(Arr_RcvBuff[cnt].Find(',') + 1, 1));
					Arr_RssiBuff[cnt - 1] = _ttoi(Arr_RcvBuff[cnt].Mid(Arr_RcvBuff[cnt].Find('(') + 1, Arr_RcvBuff[cnt].Find(')') - Arr_RcvBuff[cnt].Find('(')));
					lastbufidx = cnt;
				}
			}

			for (cnt = 1; cnt < lastbufidx; cnt++)
			{
				if (Arr_RssiStepBuff[cnt] == 2)
				{
					if (Arr_RssiBuff[cnt] != Arr_RssiBuff[cnt - 1])
					{
						result = FALSE;
						break;
					}
				}
				else if (Arr_RssiStepBuff[cnt] == 3)
				{
					if (Arr_RssiBuff[cnt] != 0)
					{
						if ((Arr_RssiBuff[cnt] + 10) != Arr_RssiBuff[cnt - 1])
						{
							result = FALSE;
							break;
						}
					}
				}
				else
				{
					if (Arr_RssiBuff[cnt] != 127)
					{
						if ((Arr_RssiBuff[cnt] - 10) != Arr_RssiBuff[cnt - 1])
						{
							result = FALSE;
							break;
						}
					}
				}

			}

			delete[] Arr_RcvBuff;
			delete[] Arr_RssiStepBuff;
			delete[] Arr_RssiBuff;

			if (result == TRUE)		pCCommtest->TestResult(_T("PASS"));
			else					pCCommtest->TestResult(_T("FAIL"));
		}
		else if (pCCommtest->m_iTestStep == DEBUG0_M_4)
		{
			if (pCCommtest->m_bEnterTheTest)	pCCommtest->debugmode(0, 0);

			if (strstr((const char*)g_pApp_Commtest->RcvBuff, pCCommtest->CompStr) != 0)		pCCommtest->TestResult(_T("PASS"));
			else																				pCCommtest->TestResult(_T("FAIL"));
		}
#endif
		Sleep(0);
	}
	return 0;
}


void CCommtest::OnBnClickedAutoStart() // TEST Start 
{
	UpdateData(TRUE);
	m_sPreAngleX = "00.00";
	m_sPreAngleY = "00.00";
	m_sPreAngleZ = "000.0";
	m_sPreTemp = "00.00";
	m_sAngleX = "00.00";
	m_sAngleY = "00.00";
	m_sAngleZ = "000.0";
	m_sTemp = "00.00";
	UpdateData(FALSE);
#if 0
	SendData_to_supply("OUTP OFF\n");
	Sleep(RESET_DELAY);
	SendData_to_supply("OUTP ON\n");
	Sleep(RESET_DELAY);
	SendData_to_supply("VOLT 24\n");
#endif
	m_iRetry_ReadPort = 10;
	PortControl("PIO160000000000000000"); // TCP/IP Communication 
#if 0
	modesel(REFSELECT, 1);	//MASTER SEL
	modesel(DUTSELECT, 1);	//SLAVE SEL

	modesel(50, 0);	//MASTER MODE
	modesel(58, 1);	//SLAVE MODE

	modesel(REFSELECT, 0);	//MASTER SEL
	modesel(DUTSELECT, 0);	//SLAVE SEL
#endif
	m_EditRcvStatComm.SetWindowText(_T(""));

	m_cVerChk.SetCheck(0);
//	m_cConf.SetCheck(0);
	m_cRcvWaitRssi.SetCheck(0);
	m_cIoTest.SetCheck(0);
	m_cUserData.SetCheck(0);
	m_cTargetRssi.SetCheck(0);
	m_cMineRssi.SetCheck(1);
	m_cAutoGainCon.SetCheck(0);
	m_cDcline.SetCheck(0);
	m_cAngleCorr.SetCheck(0);
	m_cGooff.SetCheck(0);

	m_iTestStep = pCOMWAIT;
	m_bEnterTheTest = TRUE;
	m_bThreadStatus = TRUE;

	m_BtnVerChk.EnableWindow(FALSE);
	m_BtnConf.EnableWindow(FALSE);
	m_BtnReset.EnableWindow(FALSE); 
	m_BtnAutoStart.EnableWindow(FALSE);
	m_cCompVersion.EnableWindow(FALSE);
	m_cCommTestRfid.EnableWindow(FALSE);
	m_cCommTestVhlid.EnableWindow(FALSE);
	m_editTemptime.EnableWindow(FALSE);
	m_editGooffAtt.EnableWindow(FALSE);

	GetDlgItem(IDC_RADIO1)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO2)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO3)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO4)->EnableWindow(FALSE);

	if (m_pThread == NULL)
	{
		m_pThread = AfxBeginThread(ThreadStatus_Commtest, (LPVOID)this);
		if (m_pThread == NULL)
		{
			AfxMessageBox("자동 시작 실패");
			return;
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


void CCommtest::OnBnClickedAutoStop()
{
	m_iTestStep = pCOMWAIT;
	m_bEnterTheTest = FALSE;
	m_bThreadStatus = FALSE;

	m_BtnVerChk.EnableWindow(TRUE);
	m_BtnConf.EnableWindow(TRUE);
	m_BtnReset.EnableWindow(TRUE);
	m_BtnAutoStart.EnableWindow(TRUE);
	m_cCompVersion.EnableWindow(TRUE);
	m_cCommTestRfid.EnableWindow(TRUE);
	m_cCommTestVhlid.EnableWindow(TRUE);
	m_editTemptime.EnableWindow(TRUE);
	m_editGooffAtt.EnableWindow(TRUE);

	GetDlgItem(IDC_RADIO1)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO2)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO3)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO4)->EnableWindow(TRUE);

	if (m_pProgressThread != NULL)
	{
		m_pProgressThread = NULL;
	}

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


BOOL CCommtest::PreTranslateMessage(MSG* pMsg)
{
	if (pMsg->message == WM_KEYDOWN || pMsg->message == WM_KEYUP)
	{
		if (pMsg->wParam == VK_ESCAPE || pMsg->wParam == VK_RETURN)
		{
			return true;
		}
	}
	return CDialogEx::PreTranslateMessage(pMsg);
}

void CCommtest::OnBnClickedButton6()
{
	m_EditRcvStatComm.SetWindowText(_T(""));
}

HBRUSH CCommtest::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialogEx::OnCtlColor(pDC, pWnd, nCtlColor);

	int nRet = pWnd->GetDlgCtrlID();

	switch (m_iTestResult)
	{
	case 0:
		if (nRet == IDC_RCVSTAT_COMM)
			pDC->SetTextColor(RGB(255, 0, 0));
		break;
	case 1:
		if (nRet == IDC_RCVSTAT_COMM)
			pDC->SetTextColor(RGB(0, 0, 255));
		break;
	}
	return hbr;
}


void CCommtest::OnNMCustomdrawProgress1(NMHDR* pNMHDR, LRESULT* pResult)
{
	LPNMCUSTOMDRAW pNMCD = reinterpret_cast<LPNMCUSTOMDRAW>(pNMHDR);
	// TODO: Add your control notification handler code here
	*pResult = 0;
}
