// BoardLevelMo.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "BoardLevelMo_BG01.h"
#include "afxdialogex.h"
#include <stdlib.h>
#include "CTS_pCOM_TesterDlg.h"

#define	STX_BG01		0x02
#define	ETX_BG01		0x03

#define DEVICE0_BG01 0x01
#define DEVICE1_BG01 0x02
#define DEVICE2_BG01 0x04

#define YPOS_OFFSET			65
#define CUSTOM_UPDATEDATA WM_USER + 2
enum {
	JIG_INIT, //0
	OPEN,
#if 0
	ORIGIN,
	WORKING,
#endif
	DEGREE_0_CHECK,
	ANGLE_p_10_5_0,
	MEASURE_ANGLE_p_10_5,
	ANGLE_m_10_5_0, //5
	ANGLE_INIT_0,
	pCOM_INIT,
	ANGLE_m_10_5_1,
	MEASURE_ANGLE_m_10_5,
	ANGLE_p_10_5_1, //10
	ANGLE_INIT_1,  // 11
	END,
};

CCTS_pCOM_TesterApp* g_pApp_BoardLevel_Motion_BG01;
CFont g_editFont_BoardLevel_Motion_BG01;
CRect g_rcCliDcsDlg_BMo_BG01;

// CBoardLevelMo dialog

IMPLEMENT_DYNAMIC(CBoardLevelMoBG01, CDialogEx)

CBoardLevelMoBG01::CBoardLevelMoBG01(CWnd* pParent /*=NULL*/)
	: CDialogEx(CBoardLevelMoBG01::IDD, pParent)
	, m_sMotorAngle_BG01(_T("1500"))
	, m_bRefSensorInit_BG01(FALSE)
	, m_sRefAngle_BG01(_T("000.000"))
	, m_bChkLongrun_BG01(FALSE)
	, m_iLongrunCurr_BG01(0)
	, m_iLongrunMax_BG01(0)
{
	g_pApp_BoardLevel_Motion_BG01 = (CCTS_pCOM_TesterApp*)AfxGetApp();
}

CBoardLevelMoBG01::~CBoardLevelMoBG01()
{
}

void CBoardLevelMoBG01::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT_MOTOR_ANGLE_BG01, m_editMotorAngle_BG01);
	DDX_Text(pDX, IDC_EDIT_MOTOR_ANGLE_BG01, m_sMotorAngle_BG01);
	DDX_Control(pDX, IDC_BUTTON_MOTOR_ANGLE_BG01, m_btnMotorAngle_BG01);
	DDX_Control(pDX, IDC_BUTTON_SENSOR_WORK_BG01, m_btnSensorWork_BG01);
	DDX_Control(pDX, IDC_BUTTON_ORIGIN_BG01, m_btnSensorOrigin_BG01);
	DDX_Control(pDX, IDC_BUTTON_START_BG01, m_btnTestStart_BG01);
	DDX_Control(pDX, IDC_BUTTON_STOP_BG01, m_btnTestStop_BG01);
	DDX_Control(pDX, IDC_TEST_RESULT_MOTION_BG01, m_EditDataMonitorMotion_BG01);
	DDX_Control(pDX, IDC_BUTTON_EMIO_START_BG01, m_btnEmioStart_BG01);
	DDX_Control(pDX, IDC_BUTTON_EMIO_STOP_BG01, m_btnEmioStop_BG01);
	DDX_Control(pDX, IDC_CHECK1_BG01, m_cDeviceOpen_BG01);
	DDX_Control(pDX, IDC_CHECK4_BG01, m_cDegree0Check_BG01);
	DDX_Control(pDX, IDC_CHECK5_BG01, m_cAngle_p_10_5_0_BG01);
	DDX_Control(pDX, IDC_CHECK6_BG01, m_cMeasureAngle0_BG01);
	DDX_Control(pDX, IDC_CHECK7_BG01, m_cAngle_m_10_5_1_BG01);
	DDX_Control(pDX, IDC_CHECK8_BG01, m_cRefAngleInit0_BG01);
	DDX_Control(pDX, IDC_CHECK9_BG01, m_cAngle_m_10_5_0_BG01);
	DDX_Control(pDX, IDC_CHECK10_BG01, m_cMeasureAngle1_BG01);
	DDX_Control(pDX, IDC_CHECK11_BG01, m_cAngle_p_10_5_1_BG01);
	DDX_Control(pDX, IDC_CHECK12_BG01, m_cRefAngleInit1_BG01);
	DDX_Control(pDX, IDC_CHECK13_BG01, m_cRefSensorInit_BG01);
	DDX_Check(pDX, IDC_CHECK13_BG01, m_bRefSensorInit_BG01);
	DDX_Control(pDX, IDC_EDIT_REF_ANGLE_BG01, m_editRefAngle_BG01);
	DDX_Text(pDX, IDC_EDIT_REF_ANGLE_BG01, m_sRefAngle_BG01);
	DDX_Control(pDX, IDC_PROGRESS1_BG01, m_progress_BG01);
	DDX_Control(pDX, IDC_CHECK2_BG01, m_cChkX_BG01);
	DDX_Control(pDX, IDC_CHECK3_BG01, m_cChkY_BG01);
	DDX_Control(pDX, IDC_CHECK14_BG01, m_cChkZ_BG01);
	DDX_Control(pDX, IDC_CHECK15_BG01, m_cChkLongrun_BG01);
	DDX_Check(pDX, IDC_CHECK15_BG01, m_bChkLongrun_BG01);
	DDX_Control(pDX, IDC_LONGRUN_MAX_BG01, m_editLongrunMax_BG01);
	DDX_Control(pDX, IDC_LONGRUN_CURR_BG01, m_editLongrunCurr_BG01);
	DDX_Text(pDX, IDC_LONGRUN_CURR_BG01, m_iLongrunCurr_BG01);
	DDX_Text(pDX, IDC_LONGRUN_MAX_BG01, m_iLongrunMax_BG01);
	DDX_Control(pDX, IDC_BUTTON_MOTOR_SET, m_btnMotorSet_BG01);
	DDX_Control(pDX, IDC_BUTTON3_BG01, m_btnClear_BG01);
}


BEGIN_MESSAGE_MAP(CBoardLevelMoBG01, CDialogEx)
	ON_WM_CTLCOLOR()
	ON_BN_CLICKED(IDC_BUTTON_MOTOR_ANGLE_BG01, &CBoardLevelMoBG01::OnBnClickedButtonMotorAngle)
	ON_BN_CLICKED(IDC_BUTTON_SENSOR_WORK_BG01, &CBoardLevelMoBG01::OnBnClickedButtonSensorWork)
	ON_BN_CLICKED(IDC_BUTTON_ORIGIN_BG01, &CBoardLevelMoBG01::OnBnClickedButtonOrigin)
	ON_BN_CLICKED(IDC_BUTTON_START_BG01, &CBoardLevelMoBG01::OnBnClickedButtonStart)
	ON_BN_CLICKED(IDC_BUTTON_STOP_BG01, &CBoardLevelMoBG01::OnBnClickedButtonStop)
	ON_BN_CLICKED(IDC_BUTTON_EMIO_START_BG01, &CBoardLevelMoBG01::OnBnClickedButtonEmioStart)
	ON_BN_CLICKED(IDC_BUTTON_EMIO_STOP_BG01, &CBoardLevelMoBG01::OnBnClickedButtonEmioStop)
	ON_MESSAGE(CUSTOM_UPDATEDATA, ForCustomMessageFromThread)
	ON_EN_CHANGE(IDC_EDIT_MOTOR_ANGLE_BG01, &CBoardLevelMoBG01::OnEnChangeEditMotorAngleBg01)
	ON_BN_CLICKED(IDC_BUTTON_MOTOR_SET, &CBoardLevelMoBG01::OnBnClickedButtonMotorSet)
END_MESSAGE_MAP()


// CBoardLevelMoBG01 message handlers

LRESULT CBoardLevelMoBG01::ForCustomMessageFromThread(WPARAM  wParam, LPARAM lParam)
{
	UpdateData(FALSE);
	return 0;
}


BOOL CBoardLevelMoBG01::OnInitDialog()
{
	CDialogEx::OnInitDialog();


	m_iTestSignIndex[0] = -1;
	m_iTestSignIndex[1] = 0;
	m_iTestSignIndex[2] = 0;
	m_iTestSignIndex[3] = -1;
	m_iTestSignIndex[4] = -1;
	m_iTestSignIndex[5] = 1;

	g_editFont_BoardLevel_Motion_BG01.CreatePointFont(500, TEXT("굴림"));
	m_EditDataMonitorMotion_BG01.SetFont(&g_editFont_BoardLevel_Motion_BG01, TRUE);

	m_cDeviceOpen_BG01.EnableWindow(FALSE);
	m_cDegree0Check_BG01.EnableWindow(FALSE);
	m_cAngle_p_10_5_0_BG01.EnableWindow(FALSE);
	m_cMeasureAngle0_BG01.EnableWindow(FALSE);
	m_cAngle_m_10_5_1_BG01.EnableWindow(FALSE);
	m_cRefAngleInit0_BG01.EnableWindow(FALSE);
	m_cAngle_m_10_5_0_BG01.EnableWindow(FALSE);
	m_cMeasureAngle1_BG01.EnableWindow(FALSE);
	m_cAngle_p_10_5_1_BG01.EnableWindow(FALSE);
	m_cRefAngleInit1_BG01.EnableWindow(FALSE);
	m_editRefAngle_BG01.EnableWindow(FALSE);
	m_cRefSensorInit_BG01.SetCheck(1);

	m_cChkX_BG01.EnableWindow(FALSE);
	m_cChkY_BG01.EnableWindow(FALSE);
	m_cChkZ_BG01.EnableWindow(FALSE);
	m_cChkLongrun_BG01.EnableWindow(TRUE);

	m_editLongrunMax_BG01.SetLimitText(7);

	m_iLongrunCurr_BG01 = 0;
	m_iLongrunMax_BG01 = 0;

	m_pDlgDecision_BG01 = new CDecisionDlg();// decision
	m_pDlgDecision_BG01->Create(IDD_DECISION, this);// decision
	VERIFY(m_pDlgDecision_BG01);
	m_pDlgDecision_BG01->GetClientRect(g_rcCliDcsDlg_BMo_BG01); // decision 최초 생성시 창 크기를 기억한다.

	CCTS_pCOM_TesterDlg dlg;

	return TRUE;  // return TRUE unless you set the focus to a control
}

HBRUSH CBoardLevelMoBG01::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialogEx::OnCtlColor(pDC, pWnd, nCtlColor);

	int nRet = pWnd->GetDlgCtrlID();

	switch (m_iTestResult_BG01)
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

void CBoardLevelMoBG01::processdelay(DWORD dat)
{
	DWORD tick;
	int ProgressVal = dat;

	m_progress_BG01.SetRange(0, ProgressVal);

	tick = GetTickCount();
	while (GetTickCount() - tick <= dat)
	{
		m_progress_BG01.SetPos(GetTickCount() - tick);
		Sleep(0);
	}
	m_progress_BG01.SetPos(dat);
}

void CBoardLevelMoBG01::PortControl(CString cmd, int delay)
{
	int lfflag = 0;
	CString msg = _T("");

	if (cmd == "run")	lfflag = 1;

	if (!lfflag)	msg += (TCHAR)STX_BG01;
	else			msg += (TCHAR)0x0A;
	msg += cmd;
	if (!lfflag)	msg += (TCHAR)ETX_BG01;
	else			msg += (TCHAR)0x0A;

	g_pApp_BoardLevel_Motion_BG01->DisplayEMIOSendPacket(msg);  //->m_EditReceiveData.GetWindowTextLength();
	g_pApp_BoardLevel_Motion_BG01->m_MySocket->Send(msg, strlen(msg));		

	processdelay(delay);
}

void CBoardLevelMoBG01::OnBnClickedButtonMotorAngle()
{
	// TODO: Add your control notification handler code here

	UpdateData(TRUE);

	CString msg = _T("");
	CString cmd = _T("");

	m_editMotorAngle_BG01.GetWindowText(cmd);
	msg += (TCHAR)STX_BG01;
	msg += "PMD071LL" + cmd;
	msg += (TCHAR)ETX_BG01;

	g_pApp_BoardLevel_Motion_BG01->m_MySocket->Send(msg, strlen(msg));

	processdelay(100);
}


void CBoardLevelMoBG01::OnBnClickedButtonSensorWork()
{
	// TODO: Add your control notification handler code here

	// PortControl("PMP041LM1", 10); // Position 1 
}


void CBoardLevelMoBG01::OnBnClickedButtonOrigin()
{
	// TODO: Add your control notification handler code here

	PortControl("PMO011", 10);
}


void CBoardLevelMoBG01::deviceclose(int which)
{
	m_bEnterTheTest_BG01 = FALSE;

	switch (which)  // OLD <MCLOSEC3>
	{
	case 0: // X
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msclose>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPort);
		break;
	case 1: // Y
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msclose>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster);
		break;
	case 2: // Z
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort2.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msclose>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPort2);
		break;
	case 'Z':
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort2.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msclosez>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPort2);
		break;
	}
}


void CBoardLevelMoBG01::deviceopen(int which)
{
	m_bEnterTheTest_BG01 = FALSE;

	switch (which) // <MOPEN7F>
	{
	case 0:
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msopen>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPort);
		break;
	case 1:
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msopen>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster);
		break;
	case 2:
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort2.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msopen>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPort2);
		break;
	case 'Z':
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort2.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl("<msopenz>\r\n", &g_pApp_BoardLevel_Motion_BG01->m_ComuPort2);
		break;
	}
}


void CBoardLevelMoBG01::device_send_all(CString SendCmd)
{
	if ((deviceconnected_BG01 & DEVICE0_BG01) == DEVICE0_BG01)	devicesend(0, "<mstest>\r\n"); // X Roll Temporarily  <MTEST8D>
	if ((deviceconnected_BG01 & DEVICE1_BG01) == DEVICE1_BG01)	devicesend(1, "<mstest>\r\n"); // Y Pitch
	if ((deviceconnected_BG01 & DEVICE2_BG01) == DEVICE2_BG01)	devicesend(2, "<mstest>\r\n"); // Z Yaw
}


void CBoardLevelMoBG01::device_read_all()
{
	if ((deviceconnected_BG01 & DEVICE0_BG01) == DEVICE0_BG01)	deviceread(0); // X Roll
	if ((deviceconnected_BG01 & DEVICE1_BG01) == DEVICE1_BG01)	deviceread(1); // Y Pitch
	if ((deviceconnected_BG01 & DEVICE2_BG01) == DEVICE2_BG01)	deviceread(2); // Z Yaw
}


float CBoardLevelMoBG01::atofval(int dot, CString dat)
{
	float val;
	int i, j = 1;

	for (i = 0; i < dot; i++)	j *= 10;
	val = (float)atof(dat);

	val *= j;
	/*
	반올림 필요없음 양수 단위로 비교 검사 진행
	val = floor(val + 0.5);
	*/
	return val;
}

int CBoardLevelMoBG01::angle_measureEach(int signX, int signY, int signZ)
{
	// Spec  ##############################################################
	// Spec  ##############################################################

	// Spec  // Angle +/-0.3  Accel +/-5 Gyro +/-100 	

	// Spec  ##############################################################
	// Spec  ##############################################################

	// Angle Cross Detect Comport Cross Check 
	int returnval;
	CString x4angle, y4angle, z4angle, tmp, strTok;
	CString angle, angle_x, angle_y, angle_z, accel, accel_x, accel_y, accel_z, temp, g_accel, g_accel_x, g_accel_y, g_accel_z;

	returnval = 0;
	uc_board_insert_confirm_flag = 0x00;

	if ((deviceconnected_BG01 & DEVICE0_BG01) == DEVICE0_BG01) // X ////////////////////////////////////////////////////////////////
	{
		x4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff;
		//		x4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = x4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(angle_z, angle, 2, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');

		m_angle_x_BG01 = atofval(2, angle_x);
		m_angle_y_BG01 = atofval(2, angle_y);
		m_angle_z_BG01 = atofval(2, angle_z);
		m_accel_x_BG01 = atofval(3, accel_x);
		m_accel_y_BG01 = atofval(3, accel_y);
		m_accel_z_BG01 = atofval(3, accel_z);
		m_g_accel_x_BG01 = atofval(2, g_accel_x);
		m_g_accel_y_BG01 = atofval(2, g_accel_y);
		m_g_accel_z_BG01 = atofval(2, g_accel_z);

		if (signX == -1)  /// Negative  Axis X Value 
		{
			if (( (m_angle_x_BG01 >= (-1080.0) && m_angle_x_BG01 <= (-1020.0 )) &&				// -1054 
				(m_angle_y_BG01 >= (-30.0) && m_angle_y_BG01 <= (30.0)) &&						// -11
				(m_angle_z_BG01 >= (18000-30)  && m_angle_z_BG01 <= (18000+30) ) &&				// 18000
				(m_accel_x_BG01 >= -5.0 && m_accel_x_BG01 <= 5.0) &&							// 2
				(m_accel_y_BG01 >= -188.0 && m_accel_y_BG01 <= -175.0) &&						// -181
				(m_accel_z_BG01 >= (978.0-8.0) && m_accel_z_BG01 <= (978.0 + 8.0)) )			// 978 
				&&
				(m_bChkLongrun_BG01
					||
					(m_g_accel_x_BG01 >= -1150.0 && m_g_accel_x_BG01 <= -950.0) &&			 // Gyro -1055
					(m_g_accel_y_BG01 >= -100.0 && m_g_accel_y_BG01 <= 100.0) &&			// 0
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))) {			// 0
						{returnval |= DEVICE0_BG01;
						uc_board_insert_confirm_flag |= DEVICE0_BG01; }
			}
			else
			{
				DEVICE0_BG01;
			}
		}
		else /// Positive Axis X Value 
		{
			if (((m_angle_x_BG01 >= (1020.0 ) && m_angle_x_BG01 <= (1080.0 )) &&			// Angle 	1040
				(m_angle_y_BG01 >= -30.0 && m_angle_y_BG01 <= 30.0) &&						// Angle    5
				(m_angle_z_BG01 >= (18000 - 30.0) && m_angle_z_BG01 <= (18000 + 30.0)) &&	// Angle    18000
				(m_accel_x_BG01 >= -5.0 && m_accel_x_BG01 <= 5.0) &&						// Accel	0 			
				(m_accel_y_BG01 >= 175.0 && m_accel_y_BG01 <= 188.0) &&						// Accel 	181			
				(m_accel_z_BG01 >= (987.0 -8.0) && m_accel_z_BG01 <= (987.0 + 8.0)))		// Accel    988
				&&
				(m_bChkLongrun_BG01
					||					
					(m_g_accel_x_BG01 >= 950.0 && m_g_accel_x_BG01 <= 1150.0) &&           	// 1038
					(m_g_accel_y_BG01 >= -100.0 && m_g_accel_y_BG01 <= 100.0) &&			//	Gyro 0 
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))) {			//	Gyro 0 
						{returnval |= DEVICE0_BG01;
						uc_board_insert_confirm_flag |= DEVICE0_BG01; }
			}
			else {
				DEVICE0_BG01;
			}
		}
	}
	if ((deviceconnected_BG01 & DEVICE1_BG01) == DEVICE1_BG01) // Y ////////////////////////////////////////////////////////////////
	{
		y4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuffMaster;
		//		y4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		
		tmp = y4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(angle_z, angle, 2, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');
#if 0
		m_angle_x = (float)atof(angle_x);
		m_angle_y = (float)atof(angle_y);
		m_angle_z = (float)atof(angle_z);
		m_accel_x = (float)atof(accel_x);
		m_accel_y = (float)atof(accel_y);
		m_accel_z = (float)atof(accel_z);
		m_g_accel_x = (float)atof(g_accel_x);
		m_g_accel_y = (float)atof(g_accel_y);
		m_g_accel_z = (float)atof(g_accel_z);
#else
		m_angle_x_BG01 = atofval(2, angle_x);
		m_angle_y_BG01 = atofval(2, angle_y);
		m_angle_z_BG01 = atofval(2, angle_z);
		m_accel_x_BG01 = atofval(3, accel_x);
		m_accel_y_BG01 = atofval(3, accel_y);
		m_accel_z_BG01 = atofval(3, accel_z);
		m_g_accel_x_BG01 = atofval(2, g_accel_x);
		m_g_accel_y_BG01 = atofval(2, g_accel_y);
		m_g_accel_z_BG01 = atofval(2, g_accel_z);
#endif
		if (signY == -1)
		{
			if (((m_angle_x_BG01 >= -30.0 && m_angle_x_BG01 <= 30.0) &&							// 12
				(m_angle_y_BG01 >= (-1080.0) && m_angle_y_BG01 <= (-1020.0)) &&				// -1057
				(m_angle_z_BG01 >= (18000 - 30.0) && m_angle_z_BG01 <= (18000 + 30.0)) &&		// 18000
				(m_accel_x_BG01 >= 175.0 && m_accel_x_BG01 <= 188.0) &&							// 182
				(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&							// 0
				(m_accel_z_BG01 >= (979.0 - 8.0) && m_accel_z_BG01 <= (979.0 + 8.0)))			// 980
				&&
				(m_bChkLongrun_BG01
					||
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&				// 0
					(m_g_accel_y_BG01 >= -1150.0 && m_g_accel_y_BG01 <= -950.0) &&			// -1060
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0)))
			{
				returnval |= DEVICE1_BG01;	// 0
				uc_board_insert_confirm_flag |= DEVICE1_BG01;
			}
		}
		else
		{
			if (((m_angle_x_BG01 >= -30.0 && m_angle_x_BG01 <= 30.0) &&							// -7
				(m_angle_y_BG01 >= (1020.0) && m_angle_y_BG01 <= (1080.0)) &&					//1042
				(m_angle_z_BG01 >= (18000 - 30.0) && m_angle_z_BG01 <= (18000 + 30.0)) &&		// 18000	
				(m_accel_x_BG01 >= -188.0 && m_accel_x_BG01 <= -175.0) &&						// -181
				(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&							// 1
				(m_accel_z_BG01 >= (988.0 - 8.0) && m_accel_z_BG01 <= (988.0 + 8.0)))			// 988
				&&
				(m_bChkLongrun_BG01
					||
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&			// 0
					(m_g_accel_y_BG01 >= 950.0 && m_g_accel_y_BG01 <= 1150.0) &&			// 1043
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))) 
			{
				returnval |= DEVICE1_BG01;
				uc_board_insert_confirm_flag |= DEVICE1_BG01;
			}
		}
	}
	if ((deviceconnected_BG01 & DEVICE2_BG01) == DEVICE2_BG01) // Z //////////////////////////////////////////////////////////////// 
	{
		z4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff2;
		//		z4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = z4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(angle_z, angle, 2, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');
#if 0
		m_angle_x = (float)atof(angle_x);
		m_angle_y = (float)atof(angle_y);
		m_angle_z = (float)atof(angle_z);
		m_accel_x = (float)atof(accel_x);
		m_accel_y = (float)atof(accel_y);
		m_accel_z = (float)atof(accel_z);
		m_g_accel_x = (float)atof(g_accel_x);
		m_g_accel_y = (float)atof(g_accel_y);
		m_g_accel_z = (float)atof(g_accel_z);
#else
		m_angle_x_BG01 = atofval(2, angle_x);
		m_angle_y_BG01 = atofval(2, angle_y);
		m_angle_z_BG01 = atofval(2, angle_z);
		m_accel_x_BG01 = atofval(3, accel_x);
		m_accel_y_BG01 = atofval(3, accel_y);
		m_accel_z_BG01 = atofval(3, accel_z);
		m_g_accel_x_BG01 = atofval(2, g_accel_x);
		m_g_accel_y_BG01 = atofval(2, g_accel_y);
		m_g_accel_z_BG01 = atofval(2, g_accel_z);
#endif
		if (signZ == -1) /// Negative Value 
		{

			if ((m_angle_x_BG01 >= -30.0 && m_angle_x_BG01 <= +30) &&				// -120
				(m_angle_y_BG01 >= 1020.0 && m_angle_y_BG01 <= 1080.0) &&				// 1026
				(m_angle_z_BG01 >= (18000.0 - 30.0) && m_angle_z_BG01 <= (18000.0 + 30)) &&				// 16954
				(m_accel_x_BG01 >= -188.0 && m_accel_x_BG01 <= -175.0) &&				// -182
				(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&					//-12
				(m_accel_z_BG01 >= (986 - 8.0) && m_accel_z_BG01 <= (986 + 8.0)) &&	// 1005
				(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&			// 0
				(m_g_accel_y_BG01 >= 950.0 && m_g_accel_y_BG01 <= 1150.0) &&			// 0	
				(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0)) 
			{
				returnval |= DEVICE2_BG01; // -1045
				uc_board_insert_confirm_flag |= DEVICE2_BG01;
			}
			

		}
		else  /// Positive  Value 
		{			
			
				if ((m_angle_x_BG01 >= -30.0 && m_angle_x_BG01 <= 30.0) &&					// -7
					(m_angle_y_BG01 >= -1080.0 && m_angle_y_BG01 <= -1020.0) &&					// -1051					
					(m_angle_z_BG01 >= (18000.0 - 30) && m_angle_z_BG01 <= (18000.0 + 30.0)) &&					// 19053
					(m_accel_x_BG01 >= 175.0 && m_accel_x_BG01 <= 188.0) &&					// 181
					(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&					// -20
					(m_accel_z_BG01 >= (979.0 - 8.0) && m_accel_z_BG01 <= (979.0 + 8.0)) &&	// 1003
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&				// 0
					(m_g_accel_y_BG01 >= -11500.0 && m_g_accel_y_BG01 <= -950.0) &&				// 0
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0)) 
				{
					returnval |= DEVICE2_BG01;	// 1050 // Gyro Shift 
					uc_board_insert_confirm_flag |= DEVICE2_BG01;
				}
			
			
		}
	}

	return returnval;
}

int CBoardLevelMoBG01::angle_measure(int sign)
{
	int returnval;
	CString x4angle, y4angle, z4angle, tmp, strTok;
	CString angle, angle_x, angle_y, angle_z, accel, accel_x, accel_y, accel_z, temp, g_accel, g_accel_x, g_accel_y, g_accel_z;

	returnval = 0;

	if ((deviceconnected_BG01 & DEVICE0_BG01) == DEVICE0_BG01) // X ////////////////////////////////////////////////////////////////
	{
		x4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff;
		//		x4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = x4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(angle_z, angle, 2, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');

		m_angle_x_BG01 = atofval(2, angle_x);
		m_angle_y_BG01 = atofval(2, angle_y);
		m_angle_z_BG01 = atofval(2, angle_z);
		m_accel_x_BG01 = atofval(3, accel_x);
		m_accel_y_BG01 = atofval(3, accel_y);
		m_accel_z_BG01 = atofval(3, accel_z);
		m_g_accel_x_BG01 = atofval(2, g_accel_x);
		m_g_accel_y_BG01 = atofval(2, g_accel_y);
		m_g_accel_z_BG01 = atofval(2, g_accel_z);

		if (sign == -1)
		{
			//if (((m_angle_x_BG01 >=-1070.0 && m_angle_x_BG01 <=-1030.0) && // Original
			if (((m_angle_x_BG01 >= -1070.0 && m_angle_x_BG01 <= -900.0) &&
				//(m_angle_y_BG01 >=  -20.0 && m_angle_y_BG01 <=   20.0) &&  // Original
				(m_angle_y_BG01 >= -40.0 && m_angle_y_BG01 <= 60.0) &&
				//(m_angle_z_BG01 >=  -20.0 && m_angle_z_BG01 <=   20.0) &&  // Original
				//(m_accel_x_BG01 >=   -5.0 && m_accel_x_BG01 <=    5.0) && // Original
				(m_accel_x_BG01 >= -15.0 && m_accel_x_BG01 <= 15.0) &&
				//(m_accel_y_BG01 >= -188.0 && m_accel_y_BG01 <= -178.0) &&
				(m_accel_y_BG01 >= -188.0 && m_accel_y_BG01 <= -170.0) &&
				//(m_accel_z_BG01 >=  979.0 && m_accel_z_BG01 <=  989.0))  // Original
				(m_accel_z_BG01 >= 970.0 && m_accel_z_BG01 <= 1030.0))
				&&
				(m_bChkLongrun_BG01
					||
					(m_g_accel_x_BG01 >= 950.0 && m_g_accel_x_BG01 <= 1150.0) &&
					(m_g_accel_y_BG01 >= -100.0 && m_g_accel_y_BG01 <= 100.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))) {
				returnval |= DEVICE0_BG01;
			}
			else
			{
				DEVICE0_BG01;
			}
		}
		else
		{
			//if (((m_angle_x_BG01 >= 1030.0 && m_angle_x_BG01 <= 1070.0) && // Temporarily 
			if (((m_angle_x_BG01 >= 950.0 && m_angle_x_BG01 <= 1070.0) &&
				//(m_angle_y_BG01 >=  -20.0 && m_angle_y_BG01 <=   20.0) && // Temporarily
				(m_angle_y_BG01 >= -100.0 && m_angle_y_BG01 <= 100.0) &&
				//(m_angle_z_BG01 >=  -20.0 && m_angle_z_BG01 <=   20.0) && // Temporarily 
				//(m_accel_x_BG01 >=   -5.0 && m_accel_x_BG01 <=    5.0) && // Temporaily 
				(m_accel_x_BG01 >= -20.0 && m_accel_x_BG01 <= 20.0) && // 
				//(m_accel_y_BG01 >=  178.0 && m_accel_y_BG01 <=  188.0) && // Temporarily 
				(m_accel_y_BG01 >= 160.0 && m_accel_y_BG01 <= 188.0) &&
				//(m_accel_z_BG01 >=  979.0 && m_accel_z_BG01 <=  989.0))
				(m_accel_z_BG01 >= 970.0 && m_accel_z_BG01 <= 1050.0)) // Temporarily //
				&&
				(m_bChkLongrun_BG01
					||
					//(m_g_accel_x_BG01 >=  950.0 && m_g_accel_x_BG01 <= 1150.0) &&
					(m_g_accel_x_BG01 >= -1150.0 && m_g_accel_x_BG01 <= -950.0) &&
					(m_g_accel_y_BG01 >= -100.0 && m_g_accel_y_BG01 <= 100.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))) {
				returnval |= DEVICE0_BG01;
			}
			else {
				DEVICE0_BG01;
			}
		}
	}
	if ((deviceconnected_BG01 & DEVICE1_BG01) == DEVICE1_BG01) // Y ////////////////////////////////////////////////////////////////
	{
		y4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuffMaster;
		//		y4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = y4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(angle_z, angle, 2, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');
#if 0
		m_angle_x = (float)atof(angle_x);
		m_angle_y = (float)atof(angle_y);
		m_angle_z = (float)atof(angle_z);
		m_accel_x = (float)atof(accel_x);
		m_accel_y = (float)atof(accel_y);
		m_accel_z = (float)atof(accel_z);
		m_g_accel_x = (float)atof(g_accel_x);
		m_g_accel_y = (float)atof(g_accel_y);
		m_g_accel_z = (float)atof(g_accel_z);
#else
		m_angle_x_BG01 = atofval(2, angle_x);
		m_angle_y_BG01 = atofval(2, angle_y);
		m_angle_z_BG01 = atofval(2, angle_z);
		m_accel_x_BG01 = atofval(3, accel_x);
		m_accel_y_BG01 = atofval(3, accel_y);
		m_accel_z_BG01 = atofval(3, accel_z);
		m_g_accel_x_BG01 = atofval(2, g_accel_x);
		m_g_accel_y_BG01 = atofval(2, g_accel_y);
		m_g_accel_z_BG01 = atofval(2, g_accel_z);
#endif
		if (sign == -1)
		{
			if (((m_angle_x_BG01 >= -20.0 && m_angle_x_BG01 <= 20.0) &&
				(m_angle_y_BG01 >= 1030.0 && m_angle_y_BG01 <= 1070.0) &&
				(m_angle_z_BG01 >= -20.0 && m_angle_z_BG01 <= 20.0) &&
				(m_accel_x_BG01 >= -188.0 && m_accel_x_BG01 <= -178.0) &&
				(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&
				(m_accel_z_BG01 >= 979.0 && m_accel_z_BG01 <= 989.0))
				&&
				(m_bChkLongrun_BG01
					||
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&
					(m_g_accel_y_BG01 >= 950.0 && m_g_accel_y_BG01 <= 1150.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0)))		returnval |= DEVICE1_BG01;
		}
		else
		{
			if (((m_angle_x_BG01 >= -20.0 && m_angle_x_BG01 <= 20.0) &&
				(m_angle_y_BG01 >= -1070.0 && m_angle_y_BG01 <= -1030.0) &&
				(m_angle_z_BG01 >= -20.0 && m_angle_z_BG01 <= 20.0) &&
				(m_accel_x_BG01 >= 178.0 && m_accel_x_BG01 <= 188.0) &&
				(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&
				(m_accel_z_BG01 >= 979.0 && m_accel_z_BG01 <= 989.0))
				&&
				(m_bChkLongrun_BG01
					||
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&
					(m_g_accel_y_BG01 >= -1150.0 && m_g_accel_y_BG01 <= -950.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0)))		returnval |= DEVICE1_BG01;
		}
	}
	if ((deviceconnected_BG01 & DEVICE2_BG01) == DEVICE2_BG01) // Z //////////////////////////////////////////////////////////////// 
	{
		z4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff2;
		//		z4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = z4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(angle_z, angle, 2, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');
#if 0
		m_angle_x = (float)atof(angle_x);
		m_angle_y = (float)atof(angle_y);
		m_angle_z = (float)atof(angle_z);
		m_accel_x = (float)atof(accel_x);
		m_accel_y = (float)atof(accel_y);
		m_accel_z = (float)atof(accel_z);
		m_g_accel_x = (float)atof(g_accel_x);
		m_g_accel_y = (float)atof(g_accel_y);
		m_g_accel_z = (float)atof(g_accel_z);
#else
		m_angle_x_BG01 = atofval(2, angle_x);
		m_angle_y_BG01 = atofval(2, angle_y);
		m_angle_z_BG01 = atofval(2, angle_z);
		m_accel_x_BG01 = atofval(3, accel_x);
		m_accel_y_BG01 = atofval(3, accel_y);
		m_accel_z_BG01 = atofval(3, accel_z);
		m_g_accel_x_BG01 = atofval(2, g_accel_x);
		m_g_accel_y_BG01 = atofval(2, g_accel_y);
		m_g_accel_z_BG01 = atofval(2, g_accel_z);
#endif
		if (sign == -1)
		{
			if (!m_bChkLongrun_BG01)
			{
				if ((m_angle_x_BG01 >= -20.0 && m_angle_x_BG01 <= 20.0) &&
					(m_angle_y_BG01 >= 1030.0 && m_angle_y_BG01 <= 1070.0) &&
					(m_angle_z_BG01 >= -20.0 && m_angle_z_BG01 <= 20.0) &&
					(m_accel_x_BG01 >= -188.0 && m_accel_x_BG01 <= -178.0) &&
					(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&
					(m_accel_z_BG01 >= 979.0 && m_accel_z_BG01 <= 989.0) &&
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&
					(m_g_accel_y_BG01 >= 950.0 && m_g_accel_y_BG01 <= 1150.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))		returnval |= DEVICE2_BG01;
			}
			else
			{
				if ((m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&
					(m_g_accel_y_BG01 >= 950.0 && m_g_accel_y_BG01 <= 1150.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))		returnval |= DEVICE2_BG01;
			}
		}
		else
		{
			if (!m_bChkLongrun_BG01)
			{
				if ((m_angle_x_BG01 >= -20.0 && m_angle_x_BG01 <= 20.0) &&
					(m_angle_y_BG01 >= -1070.0 && m_angle_y_BG01 <= -1030.0) &&
					(m_angle_z_BG01 >= -20.0 && m_angle_z_BG01 <= 20.0) &&
					(m_accel_x_BG01 >= 178.0 && m_accel_x_BG01 <= 188.0) &&
					(m_accel_y_BG01 >= -5.0 && m_accel_y_BG01 <= 5.0) &&
					(m_accel_z_BG01 >= 979.0 && m_accel_z_BG01 <= 989.0) &&
					(m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&
					(m_g_accel_y_BG01 >= -1150.0 && m_g_accel_y_BG01 <= -950.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))		returnval |= DEVICE2_BG01;
			}
			else
			{
				if ((m_g_accel_x_BG01 >= -100.0 && m_g_accel_x_BG01 <= 100.0) &&
					(m_g_accel_y_BG01 >= -1150.0 && m_g_accel_y_BG01 <= -950.0) &&
					(m_g_accel_z_BG01 >= -100.0 && m_g_accel_z_BG01 <= 100.0))		returnval |= DEVICE2_BG01;
			}
		}
	}

	return returnval;
}


void CBoardLevelMoBG01::devicesend(int which, CString SendCmd)
{
	switch (which)
	{
	case 0: // X
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl(SendCmd, &g_pApp_BoardLevel_Motion_BG01->m_ComuPort);
		break;
	case 1: // Y
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControlMaster(SendCmd, &g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster);
		break;
	case 2: // Z
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort2.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl2(SendCmd, &g_pApp_BoardLevel_Motion_BG01->m_ComuPort2);
		break;
	case 3: // 
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort3.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->SendDataToEditControl3(SendCmd, &g_pApp_BoardLevel_Motion_BG01->m_ComuPort3);
		break;
	}
}


void CBoardLevelMoBG01::deviceread(int which)
{
	switch (which)
	{
	case 0: // X
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->ReadDataToEditControl(1, &g_pApp_BoardLevel_Motion_BG01->m_ComuPort);
		break;
	case 1: // Y
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->ReadDataToEditControlMaster(1, &g_pApp_BoardLevel_Motion_BG01->m_ComuPortMaster);
		break;
	case 2: // Z
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort2.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->ReadDataToEditControl2(1, &g_pApp_BoardLevel_Motion_BG01->m_ComuPort2);
		break;
	case 3:
		if (g_pApp_BoardLevel_Motion_BG01->m_ComuPort3.m_bConnected == NULL) break;
		g_pApp_BoardLevel_Motion_BG01->ReadDataToEditControl3(1, &g_pApp_BoardLevel_Motion_BG01->m_ComuPort3);
		break;
	}
}


void CBoardLevelMoBG01::angle_temp_data()
{
	CString x4angle, y4angle, z4angle, tmp, strTok;
	CString angle, angle_x, angle_y, angle_z, accel, accel_x, accel_y, accel_z, temp, g_accel, g_accel_x, g_accel_y, g_accel_z;

	if ((deviceconnected_BG01 & DEVICE0_BG01) == DEVICE0_BG01)
	{
		x4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff;
		//		x4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = x4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 0, ',');
		AfxExtractSubString(accel_x, accel, 0, ',');
		AfxExtractSubString(g_accel_x, g_accel, 0, ',');
		AfxExtractSubString(temp, temp, 0, ',');

		m_angle_x_BG01 = (float)atof(angle_x);
		m_accel_x_BG01 = (float)atof(accel_x);
		m_g_accel_x_BG01 = (float)atof(g_accel_x);
		m_temp_BG01[0] = (float)atof(temp);
	}
	if ((deviceconnected_BG01 & DEVICE1_BG01) == DEVICE1_BG01)
	{
		y4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuffMaster;
		//		y4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = y4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_y, angle, 1, ',');
		AfxExtractSubString(accel_y, accel, 1, ',');
		AfxExtractSubString(g_accel_y, g_accel, 1, ',');
		AfxExtractSubString(temp, temp, 0, ',');

		m_angle_y_BG01 = (float)atof(angle_y);
		m_accel_y_BG01 = (float)atof(accel_y);
		m_g_accel_y_BG01 = (float)atof(g_accel_y);
		m_temp_BG01[1] = (float)atof(temp);
	}
	if ((deviceconnected_BG01 & DEVICE2_BG01) == DEVICE2_BG01)
	{
		z4angle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff2;
		//		z4angle = "A=0.33,-0.14,0.00,ACC=0.002,0.005,1.004,T=45.52,GA=0.00,0.00,0.00";
		tmp = z4angle;
		AfxExtractSubString(angle, tmp, 1, '=');
		AfxExtractSubString(accel, tmp, 2, '=');
		AfxExtractSubString(temp, tmp, 3, '=');
		AfxExtractSubString(g_accel, tmp, 4, '=');

		AfxExtractSubString(angle_x, angle, 2, ',');
		AfxExtractSubString(accel_z, accel, 2, ',');
		AfxExtractSubString(g_accel_z, g_accel, 2, ',');
		AfxExtractSubString(temp, temp, 0, ',');

		m_angle_z_BG01 = (float)atof(angle_z);
		m_accel_z_BG01 = (float)atof(accel_z);
		m_g_accel_z_BG01 = (float)atof(g_accel_z);
		m_temp_BG01[2] = (float)atof(temp);
	}
}

BYTE CBoardLevelMoBG01::angle_0_init()
{
	CString refangle, sRefangle;
	float f_refangle;
	BYTE cycle_count = 100;

	while (cycle_count)
	{
		devicesend(3, "get---x");
		processdelay(100);
		deviceread(3);

		//		memcpy(g_pApp_BoardLevel_Motion->RcvBuff3, "-006.244", 8);
		refangle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff3;
		sRefangle = refangle.Mid(1);

		if (g_pApp_BoardLevel_Motion_BG01->RcvBuff3[0] == '+')	f_refangle = (float)atof(sRefangle);
		else												f_refangle = (float)atof(sRefangle) * -1;

		

		//break; // Temporarily 
		// 1 Turn 
		// Trim 0 Level 
		if (m_Refangle_BG01 - f_refangle > 0.005)
		{
			PortControl("PMD041LL1", 10);
		}
		else if (m_Refangle_BG01 - f_refangle < -0.005)
		{
			PortControl("PMD051LL-1", 10);
		}
		else
		{
			break;
		}

		cycle_count--;
		Sleep(0);
	}
	return cycle_count;
}

BYTE CBoardLevelMoBG01::angle_0_init_Revision()
{
	CString refangle, sRefangle;
	float f_refangle;
	BYTE cycle_count = 5;

	//while (cycle_count)
	//{
		devicesend(3, "get---x");
		processdelay(100);
		deviceread(3);

		//		memcpy(g_pApp_BoardLevel_Motion->RcvBuff3, "-006.244", 8);
		refangle = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff3;
		sRefangle = refangle.Mid(1);

		if (g_pApp_BoardLevel_Motion_BG01->RcvBuff3[0] == '+')	f_refangle = (float)atof(sRefangle);
		else													f_refangle = (float)atof(sRefangle) * -1;

	

		//if (m_Refangle_BG01 - f_refangle > 0.005)
		//{
			devicesend(3, "setzcur"); // Slope Sensor Check 
			processdelay(100);
			deviceread(3);
		//}
		//else if (m_Refangle_BG01 - f_refangle < -0.005)
		//{
			//devicesend(3, "setzcur"); // Slope Sensor Check 
			//processdelay(100);
			//deviceread(3);
		//}
		//else {
			//break;
		//}
		//cycle_count--;
	//}
	return cycle_count;
}


void CBoardLevelMoBG01::TestResult(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision_BG01->MoveWindow(rcParent.left + (rcParent.Width() - g_rcCliDcsDlg_BMo_BG01.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcCliDcsDlg_BMo_BG01.Height()) / 2 - YPOS_OFFSET, g_rcCliDcsDlg_BMo_BG01.Width(), g_rcCliDcsDlg_BMo_BG01.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_iTestStep_BG01++;
		m_bEnterTheTest_BG01 = TRUE;
		m_iTestResult_BG01 = TRUE;
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		if (m_iTestStep_BG01 == OPEN + 1)					m_cDeviceOpen_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == DEGREE_0_CHECK + 1)			m_cDegree0Check_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_0 + 1)			m_cAngle_p_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_p_10_5 + 1)	m_cMeasureAngle0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_0 + 1)			m_cAngle_m_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_0 + 1)			m_cRefAngleInit0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_1 + 1)			m_cAngle_m_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_m_10_5 + 1)	m_cMeasureAngle1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_1 + 1)			m_cAngle_p_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_1 + 1)			m_cRefAngleInit1_BG01.SetCheck(1);

		// Message 
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_cRefAngleInit1_BG01.SetCheck(1);
		m_iTestResult_BG01 = TRUE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(TRUE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);
	}
	else
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();

		msg.Format("%s_%d", msg, m_iTestStep_BG01); // Fail Step  /////////////////////////////////////// Display 

		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(FALSE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);

		// Display
		g_pApp_BoardLevel_Motion_BG01->DisplayToStepData(_T(msg), m_iTestStep_BG01);
	}
	g_pApp_BoardLevel_Motion_BG01->DisplayToStepSequence(m_iTestStep_BG01); // Step Display 
}

void CBoardLevelMoBG01::TestResult_Limit(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision_BG01->MoveWindow(rcParent.left + (rcParent.Width() - g_rcCliDcsDlg_BMo_BG01.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcCliDcsDlg_BMo_BG01.Height()) / 2 - YPOS_OFFSET, g_rcCliDcsDlg_BMo_BG01.Width(), g_rcCliDcsDlg_BMo_BG01.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_iTestStep_BG01++;
		m_bEnterTheTest_BG01 = TRUE;
		m_iTestResult_BG01 = TRUE;
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		if (m_iTestStep_BG01 == OPEN + 1)					m_cDeviceOpen_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == DEGREE_0_CHECK + 1)			m_cDegree0Check_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_0 + 1)			m_cAngle_p_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_p_10_5 + 1)	m_cMeasureAngle0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_0 + 1)			m_cAngle_m_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_0 + 1)			m_cRefAngleInit0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_1 + 1)			m_cAngle_m_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_m_10_5 + 1)	m_cMeasureAngle1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_1 + 1)			m_cAngle_p_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_1 + 1)			m_cRefAngleInit1_BG01.SetCheck(1);

		// Message 
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_cRefAngleInit1_BG01.SetCheck(1);
		m_iTestResult_BG01 = TRUE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(TRUE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);
	}
	else
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();

		msg.Format("%s_%d", msg, m_iTestStep_BG01); // Fail Step  /////////////////////////////////////// Display 

		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(FALSE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);

		// Display  // DisplayToStepData_Limit
		g_pApp_BoardLevel_Motion_BG01->DisplayToStepData_Limit(_T(msg), m_iTestStep_BG01);
	}
	g_pApp_BoardLevel_Motion_BG01->DisplayToStepSequence(m_iTestStep_BG01); // Step Display 
}

void CBoardLevelMoBG01::TestResult_Limit_Type2(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision_BG01->MoveWindow(rcParent.left + (rcParent.Width() - g_rcCliDcsDlg_BMo_BG01.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcCliDcsDlg_BMo_BG01.Height()) / 2 - YPOS_OFFSET, g_rcCliDcsDlg_BMo_BG01.Width(), g_rcCliDcsDlg_BMo_BG01.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_iTestStep_BG01++;
		m_bEnterTheTest_BG01 = TRUE;
		m_iTestResult_BG01 = TRUE;
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		if (m_iTestStep_BG01 == OPEN + 1)					m_cDeviceOpen_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == DEGREE_0_CHECK + 1)			m_cDegree0Check_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_0 + 1)			m_cAngle_p_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_p_10_5 + 1)	m_cMeasureAngle0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_0 + 1)			m_cAngle_m_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_0 + 1)			m_cRefAngleInit0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_1 + 1)			m_cAngle_m_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_m_10_5 + 1)	m_cMeasureAngle1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_1 + 1)			m_cAngle_p_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_1 + 1)			m_cRefAngleInit1_BG01.SetCheck(1);

		// Message 
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_cRefAngleInit1_BG01.SetCheck(1);
		m_iTestResult_BG01 = TRUE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(TRUE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);
	}
	else
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();

		msg.Format("%s_%d", msg, m_iTestStep_BG01); // Fail Step  /////////////////////////////////////// Display 

		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(FALSE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);

		// Display  // DisplayToStepData_Limit
		g_pApp_BoardLevel_Motion_BG01->DisplayToStepData_Limit_Type2(_T(msg), m_iTestStep_BG01);
	}
	g_pApp_BoardLevel_Motion_BG01->DisplayToStepSequence(m_iTestStep_BG01); // Step Display 
}



void CBoardLevelMoBG01::TestResult_Each(CString msg, int iresult_each_board, unsigned char uc_board_insert_flag)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision_BG01->MoveWindow(rcParent.left + (rcParent.Width() - g_rcCliDcsDlg_BMo_BG01.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcCliDcsDlg_BMo_BG01.Height()) / 2 - YPOS_OFFSET, g_rcCliDcsDlg_BMo_BG01.Width(), g_rcCliDcsDlg_BMo_BG01.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_iTestStep_BG01++;
		m_bEnterTheTest_BG01 = TRUE;
		m_iTestResult_BG01 = TRUE;
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		if (m_iTestStep_BG01 == OPEN + 1)					m_cDeviceOpen_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == DEGREE_0_CHECK + 1)			m_cDegree0Check_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_0 + 1)			m_cAngle_p_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_p_10_5 + 1)	m_cMeasureAngle0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_0 + 1)			m_cAngle_m_10_5_0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_0 + 1)			m_cRefAngleInit0_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_m_10_5_1 + 1)			m_cAngle_m_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == MEASURE_ANGLE_m_10_5 + 1)	m_cMeasureAngle1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_p_10_5_1 + 1)			m_cAngle_p_10_5_1_BG01.SetCheck(1);
		if (m_iTestStep_BG01 == ANGLE_INIT_1 + 1)			m_cRefAngleInit1_BG01.SetCheck(1);

		// Message 
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_cRefAngleInit1_BG01.SetCheck(1);
		m_iTestResult_BG01 = TRUE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();
		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));
		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(TRUE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);
	}
	else // Fail 
	{
		m_iTestResult_BG01 = FALSE;
		//GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		GetDlgItem(IDC_TEST_RESULT_MOTION_BG01)->Invalidate();

		msg.Format("%s_%d", msg, m_iTestStep_BG01); // Fail Step  /////////////////////////////////////// Display 

		m_EditDataMonitorMotion_BG01.SetWindowText(_T(msg));

		OnBnClickedButtonStop();

		m_pDlgDecision_BG01->SetDecision(FALSE);// decision
		m_pDlgDecision_BG01->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision_BG01->AutoHide(3000);

		// Display
		g_pApp_BoardLevel_Motion_BG01->DisplayToStepData_Each(_T(msg), m_iTestStep_BG01, iresult_each_board, uc_board_insert_flag); // Result Each Board 
	}
	g_pApp_BoardLevel_Motion_BG01->DisplayToStepSequence(m_iTestStep_BG01); // Step Display 
}


#define REPEAT_MEASURE_COUNT 3
#define REPEAT_MOVE_COUNT 2

UINT ThreadStatus_BoardLevelMoBG01(LPVOID lParam)
{
	CBoardLevelMoBG01* pCBLM;
	pCBLM = (CBoardLevelMoBG01*)lParam;

	pCBLM->deviceconnected_BG01 = 0;

	int i = 0;

	g_pApp_BoardLevel_Motion_BG01->DisplayToStepSequence(pCBLM->m_iTestStep_BG01); // Step Display 
	
	while (pCBLM->m_bThreadStatus_BG01)
	{
		if (pCBLM->m_iTestStep_BG01 == JIG_INIT) //0
		{
			// Temporarily 
			//pCBLM->m_iTestStep_BG01= OPEN; // For Debug 
			///continue;

			//############################################################################
			//############################################################################
			pCBLM->m_EditDataMonitorMotion_BG01.SetWindowText(_T("")); // Initialization 
			//############################################################################
			//############################################################################

			if (pCBLM->m_bEnterTheTest_BG01) // EMIO Home Search Debug 
			{
				pCBLM->PortControl("PPC00", 250);		// Move Step Motor Resolution // Rev  pms0041480 // Error Clear 


				// Initial State Check

				// Motor Setting 
				pCBLM->PortControl("PMS041480", 250);		// Move Step Motor Resolution // Rev  pms0041480 
				if (theApp.App_RcvPort.Find("pms0041480") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS0510550", 250);		// Move Torque		// Rev  
				if (theApp.App_RcvPort.Find("pms00510550") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS0510650", 250);		// Hold Torque // Rev   
				if (theApp.App_RcvPort.Find("pms00510650") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS071212000", 250);		// High Speed Set
				if (theApp.App_RcvPort.Find("pms0071212000") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS071222000", 250);		// Middle Speed Set
				if (theApp.App_RcvPort.Find("pms0071222000") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS071231000", 250);		// Low Speed Set 
				if (theApp.App_RcvPort.Find("pms0071231000") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS06124256", 250);		// High Acceleration Speed Set 
				if (theApp.App_RcvPort.Find("pms006124256") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS06125128", 250);		// Middle Acceleration Speed Set 
				if (theApp.App_RcvPort.Find("pms006125128") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS0512664", 250);		// Low Acceleration Speed Set 
				if (theApp.App_RcvPort.Find("pms00512664") != -1) {}
				else { pCBLM->TestResult(_T("FAIL"));  continue; }

				pCBLM->PortControl("PMS071121000", 250);	// Move Origin Max Speed // Rev  pms0071124000 
				if (theApp.App_RcvPort.Find("pms0071121000") != -1) {}
				else { pCBLM->TestResult(_T("FAIL")); continue; }

				pCBLM->PortControl("PMS06113500", 250);	// Move Origin Min Speed // Rev  pms0071124000 
				if (theApp.App_RcvPort.Find("pms006113500") != -1) {}
				else { pCBLM->TestResult(_T("FAIL")); continue; }

				pCBLM->PortControl("PMS06114128", 250);	// Move Origin Acceleration Speed // Rev  pms0071124000 
				if (theApp.App_RcvPort.Find("pms006114128") != -1) {}
				else { pCBLM->TestResult(_T("FAIL")); continue; }

				// Limit IO Detecting //////////////////////////////////////////////////////////////////////////////////////////
				// PII00 ///////////////////////////////////////////////////////////////////////////////////////////////////////

				 // ##############################################################################################################
				 // ##############################################################################################################
				 pCBLM->PortControl("PMS071288000", 250);	              // Software Limit Plus ////////////////////////////////
				 if (theApp.App_RcvPort.Find("pms0071288000") != -1) {}
				 else { pCBLM->TestResult(_T("FAIL")); continue; }         // Software Limit Plus ////////////////////////////////
				 // ##############################################################################################################
				 // ##############################################################################################################
				 
				 // ##############################################################################################################
				 // ##############################################################################################################
				 pCBLM->PortControl("PMS08129-8000", 250);
				 if (theApp.App_RcvPort.Find("pms008129-8000") != -1) {}
				 else { pCBLM->TestResult(_T("FAIL")); continue; }
				 // ##############################################################################################################
				 // ##############################################################################################################
				 // Software Limit Alarm +/- // Get Pulse Count
				 // Hardware Limit Alarm Gigu Control 

				// Origin Action // ##############################################################################################################
				pCBLM->PortControl("PMO011", 6500);			// Move Origin // Rev pmo0011
				if (theApp.App_RcvPort.Find("pmo0011") != -1) {}  // Receive Back // Communication Check
				else { pCBLM->TestResult(_T("FAIL")); continue; }
				
				// // Origin Sensor Value Check // IO Total
				// pCBLM->PortControl("PII00", 1000);
				// if (theApp.App_RcvPort.Find("pii01400000000010000") != -1) {	} else { 
				// 	pCBLM->PortControl("PMB011", 200);
				// 	pCBLM->PortControl("PMB011", 200);
				// 	//pCBLM->PortControl("PMB012", 500);
				// 	pCBLM->TestResult_Limit(_T("FAIL")); continue;
				// } // 

				// Origin Sensor Value Check // Pulse Value Check
				pCBLM->PortControl("PML011", 500);
				pCBLM->PortControl("PML011", 500);
				if (theApp.App_RcvPort.Find("pml0051-500") != -1) {} else { 
					pCBLM->PortControl("PMB011", 200);
					pCBLM->PortControl("PMB011", 200);
					//pCBLM->PortControl("PMB012", 500);
					pCBLM->TestResult_Limit_Type2(_T("FAIL")); continue;
				} //

				// Time Limit // ##################################################################################################################



				// Software Limit P/N
				pCBLM->PortControl("PMS0812812000", 250);	    // Software Limit Plus ////////////////////////////////////////////////////////////////
				if (theApp.App_RcvPort.Find("pms00812812000") != -1) {}
				else { pCBLM->TestResult(_T("FAIL")); continue; }

				pCBLM->PortControl("PMS08129-1000", 250);	    // Software Limit Minus //////////////////////////////////////////////////////////////
				if (theApp.App_RcvPort.Find("pms008129-1000") != -1) {}
				else { pCBLM->TestResult(_T("FAIL")); continue; }

				//pCBLM->PortControl("PMP041LM1", 3000);		// Move Position

				pCBLM->PortControl("PMD071LL6000",9000);	    // Move Relative PMD072LL1000   PMD082LL-1000 				

				if (theApp.App_RcvPort.Find("pmd0011") != -1) {}
				else { 
					if (theApp.App_RcvPort.Find("pmd4011") != -1) {
						pCBLM->PortControl("PMD071LL6000", 9000);	    // Move Relative PMD072LL1000   PMD082LL-1000 	
						if (theApp.App_RcvPort.Find("pmd0011") != -1) {}
						else{ pCBLM->TestResult(_T("FAIL")); continue; }
					}
					else {
						pCBLM->TestResult(_T("FAIL")); continue;
					}					
				}

				pCBLM->PortControl("PMD071LL500", 1000);	    // Move Relative PMD072LL1000   PMD082LL-1000 				

				if (theApp.App_RcvPort.Find("pmd0011") != -1) {}
				else {
					if (theApp.App_RcvPort.Find("pmd4011") != -1) {
						pCBLM->PortControl("PMD071LL500", 1000);	    // Move Relative PMD072LL1000   PMD082LL-1000 	
						if (theApp.App_RcvPort.Find("pmd0011") != -1) {}
						else { pCBLM->TestResult(_T("FAIL")); continue; }
					}
					else {
						pCBLM->TestResult(_T("FAIL")); continue;
					}
				}




#if 0
				pCBLM->devicesend(3, "setzcur");
				pCBLM->processdelay(20);
				pCBLM->deviceread(3);
#endif
				pCBLM->TestResult(_T("PASS"));
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == OPEN) /// Board Open  1
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{
				pCBLM->deviceclose(0); // X
				pCBLM->deviceclose(1); // Y
				//pCBLM->deviceclose('Z');
				pCBLM->deviceclose(2); // Z		// if device close skip 				
				pCBLM->processdelay(200);

				pCBLM->deviceopen(0); // X
				pCBLM->deviceopen(1); // Y
				pCBLM->deviceopen(2); // Z
				//pCBLM->processdelay(500);
				//pCBLM->deviceopen('Z'); // Z
				//pCBLM->processdelay(1500);

				pCBLM->processdelay(3000); //############################################################################################

				pCBLM->deviceread(0); // [MOPEN=OK56]
				if (strstr((const char*)g_pApp_BoardLevel_Motion_BG01->RcvBuff, "[msclose=OK]") != 0)		pCBLM->deviceconnected_BG01 |= DEVICE0_BG01;
				pCBLM->deviceread(1);
				if (strstr((const char*)g_pApp_BoardLevel_Motion_BG01->RcvBuffMaster, "[msclose=OK]") != 0)	pCBLM->deviceconnected_BG01 |= DEVICE1_BG01;
				pCBLM->deviceread(2);
				if (strstr((const char*)g_pApp_BoardLevel_Motion_BG01->RcvBuff2, "[msclose=OK]") != 0)		pCBLM->deviceconnected_BG01 |= DEVICE2_BG01;

				if (pCBLM->deviceconnected_BG01 != 0)		pCBLM->TestResult(_T("PASS")); // Incrase Level 
				else if (pCBLM->deviceconnected_BG01 == 0)	pCBLM->TestResult(_T("FAIL"));

				pCBLM->uc_board_insert_confirm_flag = 0x00;

				if ((pCBLM->deviceconnected_BG01 & DEVICE0_BG01) == DEVICE0_BG01) {	pCBLM->m_cChkX_BG01.SetCheck(1);  }// Temporariy 
				if ((pCBLM->deviceconnected_BG01 & DEVICE1_BG01) == DEVICE1_BG01) { pCBLM->m_cChkY_BG01.SetCheck(1);  }
				if ((pCBLM->deviceconnected_BG01 & DEVICE2_BG01) == DEVICE2_BG01) { pCBLM->m_cChkZ_BG01.SetCheck(1);  }

				// PopUp Window 
				// pCBLM->deviceconnected_BG01 = 0x05;// Debug

				CString msg;
				CString msg_temp;
				CString _fail_board_index_temp[5] =
				{
					{"X"},
					{"Y"},
					{"Z"},
					{" "},
				};	
				unsigned char ucboard_insert_index_temp_x = 3;
				unsigned char ucboard_insert_index_temp_y = 3;
				unsigned char ucboard_insert_index_temp_z = 3;

				if (pCBLM->deviceconnected_BG01 & 0x01) { ucboard_insert_index_temp_x = 0; }				else {  }
				if (pCBLM->deviceconnected_BG01 & 0x02) { ucboard_insert_index_temp_y = 1; }				else {  }
				if (pCBLM->deviceconnected_BG01 & 0x04) { ucboard_insert_index_temp_z = 2; }				else {}

				msg_temp = _T(" Axis " + _fail_board_index_temp[ucboard_insert_index_temp_x] + " " + _fail_board_index_temp[ucboard_insert_index_temp_y] + " " + _fail_board_index_temp[ucboard_insert_index_temp_z] );

				msg.Format("보드 실장 확인 %s", msg_temp);

				AfxMessageBox(msg, MB_OK);

			}
		}
		else if (pCBLM->m_iTestStep_BG01 == DEGREE_0_CHECK) // Slope Sensor Detect  2
		{
			CString refangle;

			if (pCBLM->m_bEnterTheTest_BG01 && pCBLM->m_bRefSensorInit_BG01)
			{
				//pCBLM->m_iTestStep_BG01++;
				//continue;


				pCBLM->devicesend(3, "setzcur"); // Slope Sensor Check 
				pCBLM->processdelay(100);
				pCBLM->deviceread(3);
				pCBLM->devicesend(3, "get---x");
				pCBLM->processdelay(100);
				pCBLM->deviceread(3);

				// Slope Sensor Skip


				if (g_pApp_BoardLevel_Motion_BG01->RcvBuff3[0] == '\0')	pCBLM->TestResult(_T("FAIL"));
				else
				{
					pCBLM->m_sRefAngle_BG01 = (CString)g_pApp_BoardLevel_Motion_BG01->RcvBuff3;
					refangle = pCBLM->m_sRefAngle_BG01.Mid(1);

					if (g_pApp_BoardLevel_Motion_BG01->RcvBuff3[0] == '+')		pCBLM->m_Refangle_BG01 = (float)atof(refangle);
					else if (g_pApp_BoardLevel_Motion_BG01->RcvBuff3[0] == '0')	pCBLM->m_Refangle_BG01 = (float)atof(refangle);
					else													pCBLM->m_Refangle_BG01 = (float)atof(refangle) * -1;
					pCBLM->PostMessage(CUSTOM_UPDATEDATA, 0, 0);
					pCBLM->TestResult(_T("PASS"));
				}
#if 0
				pCBLM->device_send_all("<MTEST8D>");
				pCBLM->processdelay(100);
				pCBLM->device_read_all();

				pCBLM->angle_temp_data();
#endif
			}
			else
				pCBLM->TestResult(_T("PASS"));
		}
		else if (pCBLM->m_iTestStep_BG01 == ANGLE_p_10_5_0) /// Move 3
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{

				pCBLM->PortControl("PMD071LL1500", 4000);

				for (i = 0; i < REPEAT_MOVE_COUNT; i++)
				{					
					pCBLM->PortControl("PML011", 500);
					if (theApp.App_RcvPort.Find("pml00517500") != -1) { break; }
					else if(theApp.App_RcvPort.Find("pml00516000") != -1){
						theApp.App_RcvPort = "";// Initialization
						pCBLM->PortControl("PMD071LL1500", 4000);
					}
					else {}				
				}
				pCBLM->PortControl("PML011", 500);
				pCBLM->PortControl("PML011", 500);
				if (theApp.App_RcvPort.Find("pml00517500") != -1) { pCBLM->TestResult(_T("PASS")); 
				} else {
					pCBLM->TestResult(_T("FAIL")); 
				}
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == MEASURE_ANGLE_p_10_5) // 4 Measure +10.5 5 /////////////////////////////////////////////////////////////
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{
				// <MTEST8D>
				pCBLM->devicesend(3, "get---x");
				pCBLM->processdelay(100);
				pCBLM->deviceread(3);
				pCBLM->device_send_all("<mstest>\r\n");
				pCBLM->processdelay(200);
				pCBLM->device_read_all();

				//if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measure(0))	pCBLM->TestResult(_T("PASS")); angle_measureEach
				/// For Debug 
				//pCBLM->deviceconnected_BG01 = 0x07; // For Debug
				//pCBLM->uc_board_insert_confirm_flag = 0x03;
				if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measureEach(-1, -1, 0))	pCBLM->TestResult_Each(_T("PASS"), pCBLM->deviceconnected_BG01 , pCBLM->uc_board_insert_confirm_flag);
				else
				{
					for (i = 0; i < REPEAT_MEASURE_COUNT; i++)
					{
						pCBLM->device_send_all("<mstest>\r\n");
						pCBLM->processdelay(3000);
						pCBLM->device_read_all();

						if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measureEach(-1, -1, 0))	break;
						else {};
						//if (i >= 4) {
						//	pCBLM->m_iTestStep_BG01 = JIG_INIT; //BG01 Init 
						//}
					}

					if (i < REPEAT_MEASURE_COUNT)	pCBLM->TestResult_Each(_T("PASS"), pCBLM->deviceconnected_BG01, pCBLM->uc_board_insert_confirm_flag);
					else		pCBLM->TestResult_Each(_T("FAIL"), pCBLM->deviceconnected_BG01, pCBLM->uc_board_insert_confirm_flag);
				}
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == ANGLE_m_10_5_0) // Move Zero 5
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{				
				pCBLM->PortControl("PMD081LL-1500", 4000);
			
					for (i = 0; i < REPEAT_MOVE_COUNT; i++)
					{
						
						pCBLM->PortControl("PML011", 500);
						if (theApp.App_RcvPort.Find("pml00516000") != -1) { break; }
						else if (theApp.App_RcvPort.Find("pml00517500") != -1) {
							theApp.App_RcvPort = "";// Initialization
							pCBLM->PortControl("PMD081LL-1500", 4000);
						}
						else {}
					}								

				pCBLM->PortControl("PML011", 500);
				pCBLM->PortControl("PML011", 500);
				if (theApp.App_RcvPort.Find("pml00516000") != -1) {
					pCBLM->TestResult(_T("PASS"));
				} else { 
					pCBLM->TestResult(_T("FAIL")); 
				}

			}
		}
		else if (pCBLM->m_iTestStep_BG01 == ANGLE_INIT_0) // 6 // Slope Sensor Angle Check 0 Degree
		{
			//pCBLM->m_iTestStep_BG01++;
			//continue;    // Skip Slope Sense 
			if (pCBLM->m_bEnterTheTest_BG01) // Temporarily 
			{
				if (pCBLM->angle_0_init_Revision() != 0)	pCBLM->TestResult(_T("PASS"));
				else							pCBLM->TestResult(_T("FAIL"));
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == pCOM_INIT) // Board Init  7
		{
			//if (pCBLM->m_bEnterTheTest_BG01 && !pCBLM->m_bChkLongrun_BG01)
			if (pCBLM->m_bEnterTheTest_BG01) // Temporarily 
			{
				pCBLM->deviceclose(0);
				pCBLM->deviceclose(1);
				//pCBLM->deviceclose('Z');
				pCBLM->deviceclose(2);				
				pCBLM->processdelay(200);

				pCBLM->deviceopen(0);
				pCBLM->deviceopen(1);
				pCBLM->deviceopen(2);
				//pCBLM->processdelay(500);
				//pCBLM->deviceopen('Z');

				//pCBLM->processdelay(1500);
				pCBLM->processdelay(3000); // ?
				//pCBLM->processdelay(5000); // ?

				pCBLM->deviceread(0);
				if (strstr((const char*)g_pApp_BoardLevel_Motion_BG01->RcvBuff, "[msclose=OK]") != 0)		pCBLM->deviceconnected_BG01 |= DEVICE0_BG01;
				pCBLM->deviceread(1);
				if (strstr((const char*)g_pApp_BoardLevel_Motion_BG01->RcvBuffMaster, "[msclose=OK]") != 0)	pCBLM->deviceconnected_BG01 |= DEVICE1_BG01;
				pCBLM->deviceread(2);
				if (strstr((const char*)g_pApp_BoardLevel_Motion_BG01->RcvBuff2, "[msclose=OK]") != 0)		pCBLM->deviceconnected_BG01 |= DEVICE2_BG01;

				if (pCBLM->deviceconnected_BG01 != 0)		pCBLM->TestResult(_T("PASS"));
				else if (pCBLM->deviceconnected_BG01 == 0)	pCBLM->TestResult(_T("FAIL"));
			}
			else if (pCBLM->m_bEnterTheTest_BG01 && pCBLM->m_bChkLongrun_BG01)	pCBLM->TestResult(_T("PASS"));
		}
		else if (pCBLM->m_iTestStep_BG01 == ANGLE_m_10_5_1) // Move -10 Degree  8
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{
				//pCBLM->PortControl("PMD081LM-1500", 3000);
				pCBLM->PortControl("PMD081LL-1500", 4000);

				for (i = 0; i < REPEAT_MOVE_COUNT; i++)
				{
					pCBLM->PortControl("PML011", 500);
					if (theApp.App_RcvPort.Find("pml00514500") != -1) { break; }
					else if (theApp.App_RcvPort.Find("pml00516000") != -1) {
						theApp.App_RcvPort = "";// Initialization
						pCBLM->PortControl("PMD081LL-1500", 4000);
					}
					else {}
				}

				pCBLM->PortControl("PML011", 500); 
				pCBLM->PortControl("PML011", 500);
				if (theApp.App_RcvPort.Find("pml00514500") != -1) { pCBLM->TestResult(_T("PASS")); }else { pCBLM->TestResult(_T("FAIL")); }
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == MEASURE_ANGLE_m_10_5) // 9 Measure -10.0 Degree /////////////////////////////////////////////////////
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{
				pCBLM->devicesend(3, "get---x");
				pCBLM->processdelay(100);
				pCBLM->deviceread(3);
				pCBLM->device_send_all("<mstest>\r\n");
				pCBLM->processdelay(200);
				pCBLM->device_read_all();

				///if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measure(0))		pCBLM->TestResult(_T("PASS"));
				if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measureEach(0, 0, -1))		pCBLM->TestResult_Each(_T("PASS"), pCBLM->deviceconnected_BG01, pCBLM->uc_board_insert_confirm_flag);
				else
				{
					for (i = 0; i < REPEAT_MEASURE_COUNT; i++)
					{
						pCBLM->device_send_all("<mstest>\r\n");
						pCBLM->processdelay(3000);
						pCBLM->device_read_all();

						///if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measure(1))	break;
						if (pCBLM->deviceconnected_BG01 == pCBLM->angle_measureEach(0, 0, -1))	break;
						else {};
						//if (i >= 4) {
						//	pCBLM->m_iTestStep_BG01 = JIG_INIT; //BG01 Init 
						//}
					}

					if (i < REPEAT_MEASURE_COUNT)	pCBLM->TestResult_Each(_T("PASS"), pCBLM->deviceconnected_BG01, pCBLM->uc_board_insert_confirm_flag);
					else		pCBLM->TestResult_Each(_T("FAIL"), pCBLM->deviceconnected_BG01, pCBLM->uc_board_insert_confirm_flag);
				}
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == ANGLE_p_10_5_1) //10  Move Original 
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{
				pCBLM->PortControl("PMD071LL1500", 3000);
				for (i = 0; i < REPEAT_MOVE_COUNT; i++)
				{
					pCBLM->PortControl("PML011", 500);
					if (theApp.App_RcvPort.Find("pml00516000") != -1) { break; }
					else if (theApp.App_RcvPort.Find("pml00514500") != -1) {
						theApp.App_RcvPort = "";// Initialization
						pCBLM->PortControl("PMD071LL1500", 4000);
					}
					else {}
				}


				pCBLM->PortControl("PML011", 500);
				pCBLM->PortControl("PML011", 500);
				if (theApp.App_RcvPort.Find("pml00516000") != -1) { pCBLM->TestResult(_T("PASS")); } else {
					pCBLM->TestResult(_T("FAIL")); 
				} /// 51 5455 
			}
		}
		else if (pCBLM->m_iTestStep_BG01 == ANGLE_INIT_1) // 11 Board Init 
		{
			if (pCBLM->m_bEnterTheTest_BG01)
			{
				if (pCBLM->angle_0_init_Revision() != 0)
				{
					if (pCBLM->m_bChkLongrun_BG01)
					{
						if (pCBLM->m_iLongrunCurr_BG01 < pCBLM->m_iLongrunMax_BG01)
						{
							// pCBLM->m_iTestStep_BG01 = DEGREE_0_CHECK; // Temporarily 
							pCBLM->m_iTestStep_BG01 = OPEN;
							pCBLM->m_iLongrunCurr_BG01++;
							pCBLM->PostMessage(CUSTOM_UPDATEDATA);
						}
						else
						{
							pCBLM->TestResult(_T("END"));
						}
					}
					else
					{
						pCBLM->TestResult(_T("END"));
					}
				}
				else
				{
					pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		Sleep(0);
	}

	return 0;
}


void CBoardLevelMoBG01::OnBnClickedButtonStart() // Motion Start BG01
{
	UpdateData(TRUE);
	m_bThreadStatus_BG01 = TRUE;
	m_bEnterTheTest_BG01 = TRUE;
	m_iTestStep_BG01 = JIG_INIT;

	m_btnTestStart_BG01.EnableWindow(FALSE);
	m_btnMotorAngle_BG01.EnableWindow(FALSE);
	m_btnEmioStart_BG01.EnableWindow(FALSE);
	m_btnEmioStop_BG01.EnableWindow(FALSE);
	m_btnSensorWork_BG01.EnableWindow(FALSE);
	m_btnSensorOrigin_BG01.EnableWindow(FALSE);
	m_editMotorAngle_BG01.EnableWindow(FALSE);
	m_cChkLongrun_BG01.EnableWindow(FALSE);
	m_btnMotorSet_BG01.EnableWindow(FALSE);


	m_cDeviceOpen_BG01.SetCheck(0);
	m_cDegree0Check_BG01.SetCheck(0);
	m_cAngle_p_10_5_0_BG01.SetCheck(0);
	m_cMeasureAngle0_BG01.SetCheck(0);
	m_cAngle_m_10_5_1_BG01.SetCheck(0);
	m_cRefAngleInit0_BG01.SetCheck(0);
	m_cAngle_m_10_5_0_BG01.SetCheck(0);
	m_cMeasureAngle1_BG01.SetCheck(0);
	m_cAngle_p_10_5_1_BG01.SetCheck(0);
	m_cRefAngleInit1_BG01.SetCheck(0);

	m_editLongrunCurr_BG01.EnableWindow(FALSE);
	m_editLongrunMax_BG01.EnableWindow(FALSE);

	m_iLongrunCurr_BG01 = 0;
	UpdateData(FALSE);

	m_cChkX_BG01.SetCheck(0);
	m_cChkY_BG01.SetCheck(0);
	m_cChkZ_BG01.SetCheck(0);

	g_pApp_BoardLevel_Motion_BG01->DisplayToStepScreenClear();

	m_RThread_BG01 = NULL;
	m_RThread_BG01 = AfxBeginThread(ThreadStatus_BoardLevelMoBG01, (LPVOID)this);

	if (m_RThread_BG01 == NULL)
	{
		AfxMessageBox(_T("Error Open Thread"));
	}

}


void CBoardLevelMoBG01::OnBnClickedButtonStop()
{
	// TODO: Add your control notification handler code here

	m_bThreadStatus_BG01 = FALSE;

	m_btnTestStart_BG01.EnableWindow(TRUE);
	m_btnMotorAngle_BG01.EnableWindow(TRUE);
	m_btnEmioStart_BG01.EnableWindow(TRUE);
	m_btnEmioStop_BG01.EnableWindow(TRUE);
	m_btnSensorWork_BG01.EnableWindow(TRUE);
	m_btnSensorOrigin_BG01.EnableWindow(TRUE);
	m_editMotorAngle_BG01.EnableWindow(TRUE);

	m_btnMotorSet_BG01.EnableWindow(TRUE);

	m_cChkLongrun_BG01.EnableWindow(TRUE);
	m_editLongrunCurr_BG01.EnableWindow(TRUE);
	m_editLongrunMax_BG01.EnableWindow(TRUE);

	// Stop Action 


	WaitForSingleObject(m_RThread_BG01->m_hThread, 2000); // KIll Thread 

}


void CBoardLevelMoBG01::OnBnClickedButtonEmioStart()
{

	//	PortControl("run", 100);
	PortControl("PPP00", 10); // Request Hardware Information 
	PortControl("PPR015", 10);
}


void CBoardLevelMoBG01::OnBnClickedButtonEmioStop()
{
	// TODO: Add your control notification handler code here

	PortControl("PST", 100);
}


void CBoardLevelMoBG01::OnEnChangeEditMotorAngleBg01()
{
	// TODO:  If this is a RICHEDIT control, the control will not
	// send this notification unless you override the CDialogEx::OnInitDialog()
	// function and call CRichEditCtrl().SetEventMask()
	// with the ENM_CHANGE flag ORed into the mask.

	// TODO:  Add your control notification handler code here
}


void CBoardLevelMoBG01::OnBnClickedButtonMotorSet()
{
	// TODO: Add your control notification handler code here
	UpdateData(TRUE);

	CString msg = _T("");
	CString cmd = _T("");

	//m_editMotorAngle.GetWindowText(cmd);

	msg += (TCHAR)STX_BG01;
	msg += "PMS041480"; // Microstep Setting  0 1.8 256 0.00703125 1500 10.54688
	msg += (TCHAR)ETX_BG01;

	g_pApp_BoardLevel_Motion_BG01->m_MySocket->Send(msg, strlen(msg));

	processdelay(100);
}
