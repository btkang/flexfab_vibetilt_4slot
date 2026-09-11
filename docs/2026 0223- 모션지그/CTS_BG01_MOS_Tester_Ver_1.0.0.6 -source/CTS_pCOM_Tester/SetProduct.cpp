// SetProduct.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "SetProduct.h"
#include "afxdialogex.h"

#define UM_UPDATE WM_USER + 1

#define REFSELECT 49
#define DUTSELECT 57

enum{
	VOLTAGE_INIT,
	SWITCHCONSOLE_M_1,
//	CONFCLR,
	CONFCHK_1,
	CONFMCHK_1,
	RESET,							//5

	VOLTAGEREVISION_INIT,			
	VOLTAGEREVISION_24,
	VOLTAGEREVISION_INIT_30,
	VOLTAGEREVISION_30,
	VOLTAGEREVISION_INIT_8,			//10
	VOLTAGEREVISION_8,			
	VOLTAGEREVISION,
	VOLTAGERETURN,

	VOLTAGEMININIT,

	TARGET_WAIT,
	MEASURE_FLOATING_RSSI,			//15
	CORRECT_FLOATING_RSSI,		
	CHK_FLOATING_RSSI,				

	RFIDSEARCH,
	RFID,							
	ESENABLE,						//20
	ESCHANGEENABLE,			
	TXSTOPENABLE,
	IOLOG,
	TRIGGERENABLE,					
	VERSION,						//25
	SWITCHCONSOLE_M_2,
	CONFCHK_2,						
	CONFMCHK_2,
	SAVE,							//29
};

#define		STX				0x02
#define		ETX				0x03
#define YPOS_OFFSET			65
#define COMMAND_DELAY		25
#define RESET_DELAY			100
#define TIMER_CALL_DELAY	1000
// CSetProduct dialog

IMPLEMENT_DYNAMIC(CSetProduct, CDialogEx)

CFont g_editFont_SetProduct;
CFont g_editFont_SetProductRfid;
CCTS_pCOM_TesterApp *g_pApp_SetProduct;
CRect g_rcClientDecisionDlg_SetProduct;

const char CMPSTRpCOM[] = \
"config_data ver = 1,config_stat[0:load ok],baud = 57600,[A=B96A-700000]ch=0,  hopping_ch=0, pwr=3, rfbaud=1\r\n"	\
"irfiltering = 0,logic_filter_cnt = 5,radio_filter_cnt = 2,dbgmsg = 0,plc_retry_cnt = 50, [O=------]\r\n"	\
"sig60_reg0 = 79, sig60_reg1 = e8, F1nF0 = 1, Hopping En = 0, nLoopback= 1,SIG60_Baud = 115200\r\n"	\
"mcp4142_r0 = 28, mcp4142_tcon = 0b, tr_en = 1, agc_en = 1, v_valid/v24/vhr/vlr = 0/0/0/0\r\n"	\
"v_drop_min/falling_val/min_thres/err_thres = 0/10/5/4"
"r_init_stat/val = 0/0\r\n"	\
"u_baud/parity/guarantee/length = 115200/0/1/500, flash_break_cnt = 0,break_ptr = 0x0000, iolog_mode =1\r\n"	\
"ram_break_cnt = 0,break_ptr = 0x0000, etc_log = 2";

const char CMPSTRMpCOM[] = \
"es_en = 0, es_change_en = 0, eseach_en = 0x7f, txstop_en = 0, log_stop_en = 0\r\n"	\
"motion_retry = 4, stop_move_cnt = 0x04, motion_init = 0\r\n"	\
"acX =0, acY =0, acZ =0, gyX =0, gyY =0, gyZ =0\r\n"	\
"tilt_angle =200, tilt_angle_x_offset =0, tilt_angle_y_offset =0\r\n"	\
"runout_angle =30, runout_angle_move_offset =20, runout_angle_x_offset =0, runout_angle_y_offset =0\r\n"	\
"runoutTime =1000, runout_oneside_angle =20, runout_oneside_Time =20\r\n"	\
"Gyro_angle_offset =20, impact_gravity =1000, ImpactTime =15\r\n"	\
"MoveStartAcc =70, MoveStopAcc =4, MoveSenseAcc =4\r\n"
"FastMoveStep1SenseAcc =20, FastMoveStep2SenseAcc =100, MoveStopAccTime =50, ExceptAccTime =1000\r\n"	\
"VibrationAcc =10, ChangeStopAccTime =1000, ChangeStopAcc_Possible =1, low_filter_cut_off =30\r\n"	\
"log4angle=100, log4anglez=4, log4tilt=2, log4runout=2, log4impact=50\r\n";
/*"ZeroG_OffsetAcX =0, ZeroG_OffsetAcY =0, ZeroG_OffsetAcZ =0\r\n";*/

CSetProduct::CSetProduct(CWnd* pParent /*=NULL*/)
	: CDialogEx(CSetProduct::IDD, pParent)
	, m_sCompVersion(_T("1.55"))
	, m_sRfid(_T("00100"))
	, m_rdoEs(0)
	, m_rdoEce(0)
	, m_rdoTse(0)
	, m_rdoIl(1)
	, m_rdoTrm(1)
{
	g_pApp_SetProduct = (CCTS_pCOM_TesterApp *)AfxGetApp();
}

CSetProduct::~CSetProduct()
{
}

void CSetProduct::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_CHECK1, m_cConfClr);
	DDX_Control(pDX, IDC_CHECK2, m_cReset);
	DDX_Control(pDX, IDC_CHECK3, m_cConfChk);
	DDX_Control(pDX, IDC_CHECK4, m_cVerChk);
	DDX_Control(pDX, IDC_CONFIG_CLEAR, m_BtnConfClr);
	DDX_Control(pDX, IDC_RESET, m_BtnReset);
	DDX_Control(pDX, IDC_CONFIG_CHECK, m_BtnConfChk);
	DDX_Control(pDX, IDC_VERSION_CHECK, m_BtnVerChk);
	DDX_Control(pDX, IDC_AUTO_START, m_BtnAutoStart);
	DDX_Control(pDX, IDC_AUTO_STOP, m_BtnAutoStop);
	DDX_Control(pDX, IDC_RCVSTAT_SETPRODUCT, m_EditRcvStatSetProduct);
	DDX_Control(pDX, IDC_COMP_VERSION, m_cCompVersion);
	DDX_Text(pDX, IDC_COMP_VERSION, m_sCompVersion);
	DDX_Control(pDX, IDC_RFID, m_EditRfid);
	DDX_Text(pDX, IDC_RFID, m_sRfid);
	DDX_Control(pDX, IDC_CHECK5, m_cRfid);
	DDX_Control(pDX, IDC_CHECK6, m_cConfSave);
	DDX_Control(pDX, IDC_CHECK7, m_cCorrectVolt);
	DDX_Control(pDX, IDC_CHECK8, m_cOffsetRssi);
	DDX_Control(pDX, IDC_PROGRESS1, m_progress);
	DDX_Radio(pDX, IDC_RADIO1, m_rdoEs);
	DDX_Radio(pDX, IDC_RADIO3, m_rdoEce);
	DDX_Radio(pDX, IDC_RADIO5, m_rdoTse);
	DDX_Radio(pDX, IDC_RADIO7, m_rdoIl);
	DDX_Radio(pDX, IDC_RADIO11, m_rdoTrm);
}


BEGIN_MESSAGE_MAP(CSetProduct, CDialogEx)
	ON_BN_CLICKED(IDC_CONFIG_CLEAR, &CSetProduct::OnBnClickedConfigClear)
	ON_BN_CLICKED(IDC_RESET, &CSetProduct::OnBnClickedReset)
	ON_BN_CLICKED(IDC_CONFIG_CHECK, &CSetProduct::OnBnClickedConfigCheck)
	ON_BN_CLICKED(IDC_VERSION_CHECK, &CSetProduct::OnBnClickedVersionCheck)
	ON_BN_CLICKED(IDC_AUTO_START, &CSetProduct::OnBnClickedAutoStart)
	ON_BN_CLICKED(IDC_AUTO_STOP, &CSetProduct::OnBnClickedAutoStop)
	ON_BN_CLICKED(IDC_BUTTON7, &CSetProduct::OnBnClickedButton7)
	ON_WM_CTLCOLOR()
	ON_MESSAGE(UM_UPDATE, OnUpdateData)
END_MESSAGE_MAP()


// CSetProduct message handlers
BOOL CSetProduct::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	g_editFont_SetProduct.CreatePointFont(500, TEXT("굴림"));
	m_EditRcvStatSetProduct.SetFont(&g_editFont_SetProduct, TRUE);

	g_editFont_SetProductRfid.CreatePointFont(150, TEXT("굴림"));
	m_EditRfid.SetFont(&g_editFont_SetProductRfid, TRUE);

	m_cConfClr.EnableWindow(FALSE);
	m_cReset.EnableWindow(FALSE);
	m_cConfChk.EnableWindow(FALSE);
	m_cVerChk.EnableWindow(FALSE);
	m_cRfid.EnableWindow(FALSE);
	m_cConfSave.EnableWindow(FALSE);
	m_cOffsetRssi.EnableWindow(FALSE);
	m_cCorrectVolt.EnableWindow(FALSE);

	m_cConfClr.SetCheck(0);
	m_cReset.SetCheck(0);
	m_cConfChk.SetCheck(0);
	m_cVerChk.SetCheck(0);
	m_cRfid.SetCheck(0);
	m_cConfSave.SetCheck(0);
	m_cOffsetRssi.SetCheck(0);
	m_cCorrectVolt.SetCheck(0);

	m_EditRfid.SetLimitText(5);

	// added
	m_pDlgDecision = new CDecisionDlg();// decision
	m_pDlgDecision->Create(IDD_DECISION, this);// decision
	VERIFY(m_pDlgDecision);
	m_pDlgDecision->GetClientRect(g_rcClientDecisionDlg_SetProduct); // decision 최초 생성시 창 크기를 기억한다.

	return TRUE;  // return TRUE unless you set the focus to a control
	// 예외: OCX 속성 페이지는 FALSE를 반환해야 합니다.
}


LRESULT CSetProduct::OnUpdateData(WPARAM wParam, LPARAM lParam)
{
	UpdateData(FALSE);

	return 0;
}

void CSetProduct::processdelay(DWORD dat)
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

CString CSetProduct::ConverToHex(CString data)
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


void CSetProduct::SendData_to_test(CString SendCmd) //test pCOM send
{
	g_pApp_SetProduct->SendDataToEditControl(SendCmd, &g_pApp_SetProduct->m_ComuPort);
}

void CSetProduct::SendData_to_ref(CString SendCmd) //ref pCOM send
{
	g_pApp_SetProduct->SendDataToEditControlMaster(SendCmd, &g_pApp_SetProduct->m_ComuPortMaster);
}

void CSetProduct::SendData_to_supply(CString SendCmd) //power supply send
{
	g_pApp_SetProduct->SendDataToEditControl4(SendCmd, &g_pApp_SetProduct->m_ComuPort4);
}

void CSetProduct::ReadData_to_test(int DelayTime)
{
	g_pApp_SetProduct->ReadDataToEditControl(DelayTime, &g_pApp_SetProduct->m_ComuPort);
}

void CSetProduct::ReadData_to_ref(int DelayTime)
{
	g_pApp_SetProduct->ReadDataToEditControlMaster(DelayTime, &g_pApp_SetProduct->m_ComuPortMaster);
}

void CSetProduct::ReadData_to_supply(int DelayTime)
{
	g_pApp_SetProduct->ReadDataToEditControl4(DelayTime, &g_pApp_SetProduct->m_ComuPort4);
}

void CSetProduct::entertheconsole()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test("#console\r");
	processdelay(COMMAND_DELAY + COMMAND_DELAY);
	ReadData_to_test(1);
}

int CSetProduct::GetFindCharCount(CString param_string, char param_find_char)
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

void CSetProduct::OnBnClickedConfigClear()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test("config clear\r");
	processdelay(3500);
	ReadData_to_test(1);
}

void CSetProduct::OnBnClickedReset()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test(_T("reset\r"));
	processdelay(100);
	ReadData_to_test(1);
	processdelay(2500);
}


void CSetProduct::Rfid(CString str)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("A=%s", str);
	PortCmd.Format("<A=%s%s>", str, ConverToHex(tmp));
	tmp.Format("A=B96A-7%s", str);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(COMMAND_DELAY * 10);
	ReadData_to_test(1);
}

void CSetProduct::IncreaseRfid()
{
	CString tmp_rfid;
	int iRfid = 0;

	sscanf_s(m_sRfid, "%x", &iRfid);
	iRfid++;
	m_sRfid.Format("%05X", iRfid);

//	UpdateData(FALSE);
}

BYTE CSetProduct::DataSearch(CString Which)
{
#define MAX_COLS	50
	CString searchStr, Rfid;

	CString SearchTitle;
	SearchTitle.Format(_T("c:\\pCOM_DB\\%s.txt"), Which);
	char *pszStr = (LPSTR)(LPCTSTR)SearchTitle;
	FILE *fp;
	char readdata[MAX_COLS];

	fopen_s(&fp, (const char *)pszStr, "r+t");
	if (NULL == fp)
	{
		return 2;
	}

	while (fgets(readdata, MAX_COLS, fp) != NULL)
	{
		if (strstr((const char*)readdata, m_sRfid) != 0)
		{
			fclose(fp);
			return FALSE;
		}
	}

	fclose(fp);
	return TRUE;
}

void CSetProduct::SaveToDB(CString saveStr, CString Which)
{
	int length = saveStr.GetLength();
	BYTE *saveBuff;
	saveBuff = new BYTE[length + 3];
	memcpy(saveBuff, LPCTSTR(saveStr), length + 3);

	CString saveTitle;

	saveTitle.Format(_T("c:\\pCOM_DB\\%s.txt"), Which);
	char *pszStr = (LPSTR)(LPCTSTR)saveTitle;
	FILE *fp;

	fopen_s(&fp, (const char *)pszStr, "a+t");
	if (fp == NULL)
	{
		CreateDirectory(_T("c:\\pCOM_DB"), NULL);
		fopen_s(&fp, (const char *)pszStr, "a+t");
	}

	fwrite(saveBuff, sizeof(char), strlen((char*)saveBuff), fp);

	fclose(fp);
	delete[] saveBuff;
}

void CSetProduct::OnBnClickedConfigCheck()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test(_T("config\r"));
	processdelay(500);
	ReadData_to_test(1);
}


void CSetProduct::ConfigmChk()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test(_T("config m\r"));
	processdelay(500);
	ReadData_to_test(1);
}


void CSetProduct::OnBnClickedVersionCheck()
{
	SendData_to_test(_T("<V56>"));
	processdelay(100);
	ReadData_to_test(1);
}


void CSetProduct::VerChkInThread()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test(_T("<V56>"));
	processdelay(100);
	ReadData_to_test(1);

	CompStr = ConverToHex(_T("V=") + m_sCompVersion);
	CompStr = _T("[V=") + m_sCompVersion + CompStr + _T("]");
}

void CSetProduct::VoltageInit(int which, int data)
{
	m_bEnterTheTest = FALSE;

	Sleep(TIMER_CALL_DELAY);	//전압 보정 DELAY

	CString tmp, userdata;
	tmp.Format("<XVMS=%d>", data);

	if (!which)	SendData_to_test(tmp);
	else		SendData_to_ref(tmp);

	processdelay(COMMAND_DELAY * 5);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CSetProduct::VoltageRenewal(int which, int data)
{
	m_bEnterTheTest = FALSE;

	CString tmp, userdata;
	tmp.Format("<XVCS=%d>", data);

	if (!which)	SendData_to_test(tmp);
	else		SendData_to_ref(tmp);

	processdelay(RESET_DELAY);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CSetProduct::VoltageChg(int data)
{
	m_bEnterTheTest = FALSE;

	CString tmp;
	tmp.Format("Inst OUTP1\n");
	SendData_to_supply(tmp);
	processdelay(30);

	tmp.Format("Volt %d\n", data);
	SendData_to_supply(tmp);
	processdelay(TIMER_CALL_DELAY);

	SendData_to_supply("VOLT?\n");
	processdelay(200);
	ReadData_to_supply(1);
}

void CSetProduct::VoltageMinInit(int which, int data)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("VFMV=%d", data);
	PortCmd.Format("<VFMV=%d%s>", data, ConverToHex(tmp));
	tmp.Format("VFMV=%d", data);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(COMMAND_DELAY * 5);
	ReadData_to_test(1);
}

void CSetProduct::MeasureRssi(int which, int data)
{
	m_bEnterTheTest = FALSE;

	CString tmp, userdata;
	tmp.Format("<XRFV=%d>", data);

	if (!which)	SendData_to_test(tmp);
	else		SendData_to_ref(tmp);

	processdelay(TIMER_CALL_DELAY);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CSetProduct::ChkRssi(int which)
{
//	m_bEnterTheTest = FALSE;

	CString tmp, userdata;
	tmp = "<XRFV>";

	if (!which)	SendData_to_test(tmp);
	else		SendData_to_ref(tmp);

	processdelay(COMMAND_DELAY);

	if (!which)	ReadData_to_test(1);
	else		ReadData_to_ref(1);
}

void CSetProduct::ConfigSave()
{
	m_bEnterTheTest = FALSE;

	SendData_to_test("save\r");
	processdelay(RESET_DELAY);
	ReadData_to_test(1);
}

void CSetProduct::modesel(int which, int val)
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
	PortCmd.Format("PIW03%d%d", which, val);
	PortControl(PortCmd);
}

void CSetProduct::PortControl(CString cmd)
{
	CString msg = _T("");

	msg += (TCHAR)STX;
	msg += cmd;
	msg += (TCHAR)ETX;

	g_pApp_SetProduct->m_MySocket->Send(msg, strlen(msg));

	processdelay(10);
}

void CSetProduct::esenable(int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("EE=%d", dat);
	PortCmd.Format("<EE=%d%s>", dat, ConverToHex(tmp));
	tmp.Format("EE=%d", dat);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(RESET_DELAY);
	ReadData_to_test(1);
}


void CSetProduct::eschangeenable(int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("ECE=%d", dat);
	PortCmd.Format("<ECE=%d%s>", dat, ConverToHex(tmp));
	tmp.Format("ECE=%d", dat);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(RESET_DELAY);
	ReadData_to_test(1);
}


void CSetProduct::txstopenable(int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("TE=%d", dat);
	PortCmd.Format("<TE=%d%s>", dat, ConverToHex(tmp));
	tmp.Format("TE=%d", dat);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(RESET_DELAY);
	ReadData_to_test(1);
}


void CSetProduct::iolog(int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("IL=%d", dat);
	PortCmd.Format("<IL=%d%s>", dat, ConverToHex(tmp));
	tmp.Format("IL=%d", dat);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(RESET_DELAY);
	ReadData_to_test(1);
}


void CSetProduct::triggermode(int dat)
{
	m_bEnterTheTest = FALSE;

	CString tmp, PortCmd;
	tmp.Format("TM=%d", dat);
	PortCmd.Format("<TM=%d%s>", dat, ConverToHex(tmp));
	tmp.Format("TM=%d", dat);
	CompStr.Format("[%s%s]", tmp, ConverToHex(tmp));

	SendData_to_test(PortCmd);
	processdelay(RESET_DELAY);
	ReadData_to_test(1);
}

void CSetProduct::TestResult(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision->MoveWindow(rcParent.left + (rcParent.Width() - g_rcClientDecisionDlg_SetProduct.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcClientDecisionDlg_SetProduct.Height()) / 2 - YPOS_OFFSET, g_rcClientDecisionDlg_SetProduct.Width(), g_rcClientDecisionDlg_SetProduct.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_bEnterTheTest = TRUE;
		m_iTestResult = TRUE;
		GetDlgItem(IDC_RCVSTAT_SETPRODUCT)->Invalidate();
		m_EditRcvStatSetProduct.SetWindowText(_T(msg));

//		if (m_iTestStep == CONFCLR)				m_cConfClr.SetCheck(1);
		if (m_iTestStep == RESET)				m_cReset.SetCheck(1);
		if (m_iTestStep == CONFCHK_1)			m_cConfChk.SetCheck(1);
//		if (m_iTestStep == RFID)				m_cRfid.SetCheck(1);
		if (m_iTestStep == VERSION)				m_cVerChk.SetCheck(1);
//		if (m_iTestStep == VOLTAGEREVISION)		m_cCorrectVolt.SetCheck(1);
//		if (m_iTestStep == CHK_FLOATING_RSSI)	m_cOffsetRssi.SetCheck(1); //config 체크에서 최종 결정
		if (m_iTestStep == CONFCHK_2)			{ m_cOffsetRssi.SetCheck(1); m_cRfid.SetCheck(1); m_cCorrectVolt.SetCheck(1); }

		m_iTestStep++;
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_RCVSTAT_SETPRODUCT)->Invalidate();
		m_EditRcvStatSetProduct.SetWindowText(_T(msg));
		OnBnClickedAutoStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_iTestResult = TRUE;
		GetDlgItem(IDC_RCVSTAT_SETPRODUCT)->Invalidate();
		m_EditRcvStatSetProduct.SetWindowText(_T(msg));
		m_cConfSave.SetCheck(1);
		SaveToDB(m_sRfid + "\n", "pCOM_DB");
		IncreaseRfid();
		PostMessage(UM_UPDATE, 0 ,0);
		OnBnClickedAutoStop();

		m_pDlgDecision->SetDecision(TRUE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
	else
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_RCVSTAT_SETPRODUCT)->Invalidate();
		msg.Format("%s_%d", msg, m_iTestStep);
		m_EditRcvStatSetProduct.SetWindowText(_T(msg));
		OnBnClickedAutoStop();

		m_pDlgDecision->SetDecision(FALSE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
}

int CSetProduct::RcvMsgCnt(BYTE* msg)
{
	int i;

	for (i = 0; i <= (int)strlen((const char*)msg); i++)
	{
		if (msg[i] == '\0')	break;
	}

	return i;
}

UINT ThreadStatus_SetProduct(LPVOID lParam)
{
	CSetProduct *pCSetProduct;
	pCSetProduct = (CSetProduct*)lParam;

//	int RcvCnt, i;

	while (pCSetProduct->m_bThreadStatus)
	{
		int lLen = pCSetProduct->m_EditRcvStatSetProduct.GetWindowTextLength();
		pCSetProduct->m_EditRcvStatSetProduct.SetSel(lLen, lLen);

		if (pCSetProduct->m_iTestStep == VOLTAGE_INIT)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageChg(24);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff4, "24.0") != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
			}
			else
			{
				pCSetProduct->VoltageChg(24);
				pCSetProduct->TestResult(_T("FAIL"));
			}
		}
		else if (pCSetProduct->m_iTestStep == SWITCHCONSOLE_M_1)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->entertheconsole();
#if 0
			RcvCnt = pCSetProduct->RcvMsgCnt((BYTE*)g_pApp_SetProduct->RcvBuff);

			for (i = 0; i <= RcvCnt; i++)
			{
				if (g_pApp_SetProduct->RcvBuff[i] == '>')	break;
			}

			if (i > RcvCnt)	pCSetProduct->TestResult(_T("FAIL"));
			else			pCSetProduct->TestResult(_T("PASS"));
#endif
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, ">") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																pCSetProduct->TestResult(_T("FAIL"));
		}
#if 0
		else if (pCSetProduct->m_iTestStep == CONFCLR)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->OnBnClickedConfigClear();

			if ((strstr((const char*)g_pApp_SetProduct->RcvBuff, _T("Clear Log Success")) != 0) &&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, _T(">")) != 0))					pCSetProduct->TestResult(_T("PASS"));
			else																					pCSetProduct->TestResult(_T("FAIL"));
		}
#endif
		else if (pCSetProduct->m_iTestStep == CONFCHK_1)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->OnBnClickedConfigCheck();

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, CMPSTRpCOM) != 0)				pCSetProduct->TestResult(_T("PASS"));
			else																				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == CONFMCHK_1)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->ConfigmChk();

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, CMPSTRMpCOM) != 0)				pCSetProduct->TestResult(_T("PASS"));
			else																				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == RESET)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->OnBnClickedReset();

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, _T("after 100ms reset..")) != 0)	pCSetProduct->TestResult(_T("PASS"));
			else																					pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION_INIT)
		{
			pCSetProduct->modesel(38, 0);	//ATT Line (Commtest 완료 과정에서 ATT Line으로 바뀌지 않았을 경우를 대비)
			pCSetProduct->m_bEnterTheTest = TRUE;
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageInit(0, 0);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "[XVMS=1=0,0,0]") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																			pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION_24)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageInit(0, 1);
#if 0
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "[XVMS=3=0,0,0]") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																			pCSetProduct->TestResult(_T("FAIL"));
#else
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "ILL_SEQ[1]") != 0)		pCSetProduct->TestResult(_T("FAIL"));
			else																		pCSetProduct->TestResult(_T("PASS"));
#endif
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION_INIT_30)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageChg(30);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff4, "30.0") != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
			}
			else
			{
				pCSetProduct->VoltageChg(24);
				pCSetProduct->TestResult(_T("FAIL"));
			}
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION_30)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageInit(0, 2);
#if 0
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "[XVMS=7=0,0,0]") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																			pCSetProduct->TestResult(_T("FAIL"));
#else
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "ILL_SEQ[2]") != 0)		pCSetProduct->TestResult(_T("FAIL"));
			else																		pCSetProduct->TestResult(_T("PASS"));
#endif
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION_INIT_8)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageChg(8);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff4, "8.0") != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
			}
			else
			{
				pCSetProduct->VoltageChg(24);
				pCSetProduct->TestResult(_T("FAIL"));
			}
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION_8)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageInit(0, 3);
#if 0
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "[XVMS=F=0,0,0]") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																			pCSetProduct->TestResult(_T("FAIL"));
#else
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "ILL_SEQ[3]") != 0)
			{
				pCSetProduct->TestResult(_T("FAIL"));
			}
			else
			{
				pCSetProduct->VoltageChg(24);
				pCSetProduct->TestResult(_T("PASS"));
			}
#endif
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEREVISION)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageRenewal(0, 1);

			if ((strstr((const char*)g_pApp_SetProduct->RcvBuff, "INV_PARAM[0]") != 0)		||
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, "INV_PARAM[1-24]") != 0)	||
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, "INV_PARAM[2-VH]") != 0)	||
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, "INV_PARAM[3-VL]") != 0))		pCSetProduct->TestResult(_T("FAIL"));
			else
			{
				CString RcvBuff, strTok_valid, strTok_24v, strTok_vhratio, strTok_vlratio, CompareStr;
				RcvBuff = (CString)g_pApp_SetProduct->RcvBuff;

				CompareStr = RcvBuff.Mid(RcvBuff.Find('=') + 1, RcvBuff.Find(']') - RcvBuff.Find('=') - 1);

				AfxExtractSubString(strTok_valid, CompareStr, 0, ',');
				AfxExtractSubString(strTok_24v, CompareStr, 1, ',');
				AfxExtractSubString(strTok_vhratio, CompareStr, 2, ',');
				AfxExtractSubString(strTok_vlratio, CompareStr, 3, ',');

				pCSetProduct->m_sFixedVoltage.Format("v_valid/v24/vhr/vlr = %s/%s/%s/%s", strTok_valid, strTok_24v, strTok_vhratio, strTok_vlratio);

				pCSetProduct->TestResult(_T("PASS"));
			}
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGERETURN)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageChg(24);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff4, "24.0") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																	pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == VOLTAGEMININIT)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VoltageMinInit(0, 0);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)	pCSetProduct->TestResult(_T("PASS"));
			else																				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == TARGET_WAIT)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->modesel(REFSELECT, 1);	//REF. SEL ON

			pCSetProduct->m_bEnterTheTest = TRUE;
			Sleep(100);
			pCSetProduct->m_iTestStep++;

		}
		else if (pCSetProduct->m_iTestStep == MEASURE_FLOATING_RSSI)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->MeasureRssi(0, 1);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "[XRFV=1,") != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
			}
			else
			{
				pCSetProduct->modesel(REFSELECT, 0);	//REF. SEL OFF
				pCSetProduct->MeasureRssi(0, 0);
				pCSetProduct->TestResult(_T("FAIL"));
			}
		}
		else if (pCSetProduct->m_iTestStep == CORRECT_FLOATING_RSSI)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->MeasureRssi(0, 0);

			pCSetProduct->modesel(REFSELECT, 0);	//REF. SEL OFF

			pCSetProduct->m_bEnterTheTest = TRUE; //다른 함수에서 같이 쓰이고 있어서 쓰레드에서 FLAG를 ON 시켜준다.
			pCSetProduct->m_iTestStep++;
		}
		else if (pCSetProduct->m_iTestStep == CHK_FLOATING_RSSI)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->ChkRssi(0);

			CString RcvBuff, CompareStrm, nv1, nv2;
			RcvBuff = (CString)g_pApp_SetProduct->RcvBuff;

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "[XRFV=0,") != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));

				CString RcvBuff = (CString)g_pApp_SetProduct->RcvBuff;

				RcvBuff.Delete(0, RcvBuff.Find("=") + 1);
				RcvBuff.Remove(']');
				RcvBuff.Remove('\r');
				RcvBuff.Remove('\n');
				
				AfxExtractSubString(nv1, RcvBuff, 0, ',');
				AfxExtractSubString(nv2, RcvBuff, 1, ',');

				pCSetProduct->m_sFloatingRssi.Format("r_init_stat/val = %s/%s", nv1, nv2);
			}
			else
			{
				pCSetProduct->TestResult(_T("FAIL"));
			}
		}
		else if (pCSetProduct->m_iTestStep == RFIDSEARCH)
		{
			if (pCSetProduct->m_bEnterTheTest)
			{
				int result = pCSetProduct->DataSearch("pCOM_DB");

				if (result == 1)		pCSetProduct->TestResult(_T("PASS"));
				else if (result == 2)	pCSetProduct->TestResult(_T("NoDB"));
				else					pCSetProduct->TestResult(_T("USED"));
			}
		}
		else if (pCSetProduct->m_iTestStep == RFID)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->Rfid(pCSetProduct->m_sRfid);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
				pCSetProduct->m_sFixedAddress.Format("[A=B96A-7%s]", pCSetProduct->m_sRfid);
			}
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == ESENABLE)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->esenable(pCSetProduct->m_rdoEs);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
				pCSetProduct->m_sEsenable.Format("es_en = %d", pCSetProduct->m_rdoEs);
			}
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == ESCHANGEENABLE)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->eschangeenable(pCSetProduct->m_rdoEce);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
				pCSetProduct->m_sEsChangeenable.Format("es_change_en = %d", pCSetProduct->m_rdoEce);
			}
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == TXSTOPENABLE)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->txstopenable(pCSetProduct->m_rdoTse);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
				pCSetProduct->m_sTxstop.Format("txstop_en = %d", pCSetProduct->m_rdoTse);
			}
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == IOLOG)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->iolog(pCSetProduct->m_rdoIl);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
				pCSetProduct->m_sIolog.Format("iolog_mode =%d", pCSetProduct->m_rdoIl);
			}
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == TRIGGERENABLE)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->triggermode(pCSetProduct->m_rdoTrm);

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
			{
				pCSetProduct->TestResult(_T("PASS"));
				pCSetProduct->m_sTriggerenable.Format("tr_en = %d", pCSetProduct->m_rdoTrm);
			}
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == VERSION)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->VerChkInThread();

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->CompStr) != 0)
				pCSetProduct->TestResult(_T("PASS"));
			else
				pCSetProduct->TestResult(_T("FAIL"));
		}
		else if(pCSetProduct->m_iTestStep == SWITCHCONSOLE_M_2)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->entertheconsole();

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, ">") != 0)		pCSetProduct->TestResult(_T("PASS"));
			else																pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == CONFCHK_2)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->OnBnClickedConfigCheck();
#if 0
			if ((strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sFixedVoltage) != 0)	&&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sFloatingRssi) != 0)	&&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sFixedAddress) != 0)	&&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sIolog) != 0)			&&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sTriggerenable) != 0))			pCSetProduct->TestResult(_T("PASS"));
			else																								pCSetProduct->TestResult(_T("FAIL"));
#else
			int re = 0;
			CString msg;
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sFixedVoltage) != 0)	re |= 1 << 0;
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sFloatingRssi) != 0)	re |= 1 << 1;
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sFixedAddress) != 0)	re |= 1 << 2;
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sIolog) != 0)			re |= 1 << 3;
			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sTriggerenable) != 0)	re |= 1 << 4;

			if (re == 31)	pCSetProduct->TestResult(_T("PASS"));
			else
			{
				msg.Format("FAIL(%d)", re);
				pCSetProduct->TestResult(msg);
			}
#endif
		}
		else if (pCSetProduct->m_iTestStep == CONFMCHK_2)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->ConfigmChk();

			if ((strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sEsenable) != 0) &&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sEsChangeenable) != 0) &&
				(strstr((const char*)g_pApp_SetProduct->RcvBuff, pCSetProduct->m_sTxstop) != 0))			pCSetProduct->TestResult(_T("PASS"));
			else																							pCSetProduct->TestResult(_T("FAIL"));
		}
		else if (pCSetProduct->m_iTestStep == SAVE)
		{
			if (pCSetProduct->m_bEnterTheTest)	pCSetProduct->ConfigSave();

			if (strstr((const char*)g_pApp_SetProduct->RcvBuff, "Save Success.") != 0)			pCSetProduct->TestResult(_T("END"));
			else																				pCSetProduct->TestResult(_T("FAIL"));
		}
		Sleep(0);
	}
	return 0;
}


void CSetProduct::OnBnClickedAutoStart()
{
	UpdateData(TRUE);

	m_sFixedVoltage = "";
	m_sFloatingRssi = "";
	m_sFixedAddress = "";

	m_cConfClr.SetCheck(0);
	m_cReset.SetCheck(0);
	m_cConfChk.SetCheck(0);
	m_cVerChk.SetCheck(0);
	m_cRfid.SetCheck(0);
	m_cConfSave.SetCheck(0);
	m_cOffsetRssi.SetCheck(0);
	m_cCorrectVolt.SetCheck(0);

	m_iTestStep = VOLTAGEREVISION_INIT;
	m_bThreadStatus = TRUE;
	m_bEnterTheTest = TRUE;

	m_BtnConfClr.EnableWindow(FALSE);
	m_BtnReset.EnableWindow(FALSE);
	m_BtnConfChk.EnableWindow(FALSE);
	m_BtnVerChk.EnableWindow(FALSE);
	m_BtnAutoStart.EnableWindow(FALSE);
	m_cCompVersion.EnableWindow(FALSE);
	m_EditRfid.EnableWindow(FALSE);

	GetDlgItem(IDC_RADIO1)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO2)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO3)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO4)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO5)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO6)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO7)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO8)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO9)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO10)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO11)->EnableWindow(FALSE);
	GetDlgItem(IDC_RADIO12)->EnableWindow(FALSE);

	UpdateData(FALSE);

	if (m_pThread == NULL)
	{
		m_pThread = AfxBeginThread(ThreadStatus_SetProduct, (LPVOID)this);
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


void CSetProduct::OnBnClickedAutoStop()
{
	m_iTestStep = VOLTAGEREVISION_INIT;
	m_bThreadStatus = FALSE;
	m_bEnterTheTest = FALSE;

	m_BtnConfClr.EnableWindow(TRUE);
	m_BtnReset.EnableWindow(TRUE);
	m_BtnConfChk.EnableWindow(TRUE);
	m_BtnVerChk.EnableWindow(TRUE);
	m_BtnAutoStart.EnableWindow(TRUE);
	m_cCompVersion.EnableWindow(TRUE);
	m_EditRfid.EnableWindow(TRUE);

	GetDlgItem(IDC_RADIO1)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO2)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO3)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO4)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO5)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO6)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO7)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO8)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO9)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO10)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO11)->EnableWindow(TRUE);
	GetDlgItem(IDC_RADIO12)->EnableWindow(TRUE);

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


BOOL CSetProduct::PreTranslateMessage(MSG* pMsg)
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


void CSetProduct::OnBnClickedButton7()
{
	m_EditRcvStatSetProduct.SetWindowText(_T(""));
}


HBRUSH CSetProduct::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialogEx::OnCtlColor(pDC, pWnd, nCtlColor);

	int nRet = pWnd->GetDlgCtrlID();

	switch (m_iTestResult)
	{
	case 0:
		if (nRet == IDC_RCVSTAT_SETPRODUCT)
			pDC->SetTextColor(RGB(255, 0, 0));
		break;
	case 1:
		if (nRet == IDC_RCVSTAT_SETPRODUCT)
			pDC->SetTextColor(RGB(0, 0, 255));
		break;
	}
	return hbr;
}