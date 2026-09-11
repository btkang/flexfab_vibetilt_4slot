// BoardLevelMo.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "BoardLevelMo.h"
#include "afxdialogex.h"
#include <stdlib.h>

#define	STX		0x02
#define	ETX		0x03

#define DEVICE0 0x01
#define DEVICE1 0x02
#define DEVICE2 0x04

#define YPOS_OFFSET			65
#define CUSTOM_UPDATEDATA WM_USER + 2
enum{
	JIG_INIT,
	OPEN,
#if 0
	ORIGIN,
	WORKING,
#endif
	DEGREE_0_CHECK,
	ANGLE_p_10_5_0,
	MEASURE_ANGLE_p_10_5,
	ANGLE_m_10_5_0,
	ANGLE_INIT_0,
	pCOM_INIT,
	ANGLE_m_10_5_1,
	MEASURE_ANGLE_m_10_5,
	ANGLE_p_10_5_1,
	ANGLE_INIT_1,
	END,
};

CCTS_pCOM_TesterApp *g_pApp_BoardLevel_Motion;
CFont g_editFont_BoardLevel_Motion;
CRect g_rcCliDcsDlg_BMo;

// CBoardLevelMo dialog

IMPLEMENT_DYNAMIC(CBoardLevelMo, CDialogEx)

CBoardLevelMo::CBoardLevelMo(CWnd* pParent /*=NULL*/)
	: CDialogEx(CBoardLevelMo::IDD, pParent)
	, m_sMotorAngle(_T("1500"))
	, m_bRefSensorInit(FALSE)
	, m_sRefAngle(_T("000.000"))
	, m_bChkLongrun(FALSE)
	, m_iLongrunCurr(0)
	, m_iLongrunMax(0)
{
	g_pApp_BoardLevel_Motion = (CCTS_pCOM_TesterApp *)AfxGetApp();
}

CBoardLevelMo::~CBoardLevelMo()
{
}

void CBoardLevelMo::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT_MOTOR_ANGLE, m_editMotorAngle);
	DDX_Text(pDX, IDC_EDIT_MOTOR_ANGLE, m_sMotorAngle);
	DDX_Control(pDX, IDC_BUTTON_MOTOR_ANGLE, m_btnMotorAngle);
	DDX_Control(pDX, IDC_BUTTON_SENSOR_WORK, m_btnSensorWork);
	DDX_Control(pDX, IDC_BUTTON_ORIGIN, m_btnSensorOrigin);
	DDX_Control(pDX, IDC_BUTTON_START, m_btnTestStart);
	DDX_Control(pDX, IDC_BUTTON_STOP, m_btnTestStop);
	DDX_Control(pDX, IDC_TEST_RESULT_MOTION, m_EditDataMonitorMotion);
	DDX_Control(pDX, IDC_BUTTON_EMIO_START, m_btnEmioStart);
	DDX_Control(pDX, IDC_BUTTON_EMIO_STOP, m_btnEmioStop);
	DDX_Control(pDX, IDC_CHECK1, m_cDeviceOpen);
	DDX_Control(pDX, IDC_CHECK4, m_cDegree0Check);
	DDX_Control(pDX, IDC_CHECK5, m_cAngle_p_10_5_0);
	DDX_Control(pDX, IDC_CHECK6, m_cMeasureAngle0);
	DDX_Control(pDX, IDC_CHECK7, m_cAngle_m_10_5_1);
	DDX_Control(pDX, IDC_CHECK8, m_cRefAngleInit0);
	DDX_Control(pDX, IDC_CHECK9, m_cAngle_m_10_5_0);
	DDX_Control(pDX, IDC_CHECK10, m_cMeasureAngle1);
	DDX_Control(pDX, IDC_CHECK11, m_cAngle_p_10_5_1);
	DDX_Control(pDX, IDC_CHECK12, m_cRefAngleInit1);
	DDX_Control(pDX, IDC_CHECK13, m_cRefSensorInit);
	DDX_Check(pDX, IDC_CHECK13, m_bRefSensorInit);
	DDX_Control(pDX, IDC_EDIT_REF_ANGLE, m_editRefAngle);
	DDX_Text(pDX, IDC_EDIT_REF_ANGLE, m_sRefAngle);
	DDX_Control(pDX, IDC_PROGRESS1, m_progress);
	DDX_Control(pDX, IDC_CHECK2, m_cChkX);
	DDX_Control(pDX, IDC_CHECK3, m_cChkY);
	DDX_Control(pDX, IDC_CHECK14, m_cChkZ);
	DDX_Control(pDX, IDC_CHECK15, m_cChkLongrun);
	DDX_Check(pDX, IDC_CHECK15, m_bChkLongrun);
	DDX_Control(pDX, IDC_LONGRUN_MAX, m_editLongrunMax);
	DDX_Control(pDX, IDC_LONGRUN_CURR, m_editLongrunCurr);
	DDX_Text(pDX, IDC_LONGRUN_CURR, m_iLongrunCurr);
	DDX_Text(pDX, IDC_LONGRUN_MAX, m_iLongrunMax);
}


BEGIN_MESSAGE_MAP(CBoardLevelMo, CDialogEx)
	ON_WM_CTLCOLOR()
	ON_BN_CLICKED(IDC_BUTTON_MOTOR_ANGLE, &CBoardLevelMo::OnBnClickedButtonMotorAngle)
	ON_BN_CLICKED(IDC_BUTTON_SENSOR_WORK, &CBoardLevelMo::OnBnClickedButtonSensorWork)
	ON_BN_CLICKED(IDC_BUTTON_ORIGIN, &CBoardLevelMo::OnBnClickedButtonOrigin)
	ON_BN_CLICKED(IDC_BUTTON_START, &CBoardLevelMo::OnBnClickedButtonStart)
	ON_BN_CLICKED(IDC_BUTTON_STOP, &CBoardLevelMo::OnBnClickedButtonStop)
	ON_BN_CLICKED(IDC_BUTTON_EMIO_START, &CBoardLevelMo::OnBnClickedButtonEmioStart)
	ON_BN_CLICKED(IDC_BUTTON_EMIO_STOP, &CBoardLevelMo::OnBnClickedButtonEmioStop)
	ON_MESSAGE(CUSTOM_UPDATEDATA, ForCustomMessageFromThread)
END_MESSAGE_MAP()


// CBoardLevelMo message handlers

LRESULT CBoardLevelMo::ForCustomMessageFromThread(WPARAM  wParam, LPARAM lParam)
{
	UpdateData(FALSE);
	return 0;
}


BOOL CBoardLevelMo::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	g_editFont_BoardLevel_Motion.CreatePointFont(500, TEXT("굴림"));
	m_EditDataMonitorMotion.SetFont(&g_editFont_BoardLevel_Motion, TRUE);

	m_cDeviceOpen.EnableWindow(FALSE);
	m_cDegree0Check.EnableWindow(FALSE);
	m_cAngle_p_10_5_0.EnableWindow(FALSE);
	m_cMeasureAngle0.EnableWindow(FALSE);
	m_cAngle_m_10_5_1.EnableWindow(FALSE);
	m_cRefAngleInit0.EnableWindow(FALSE);
	m_cAngle_m_10_5_0.EnableWindow(FALSE);
	m_cMeasureAngle1.EnableWindow(FALSE);
	m_cAngle_p_10_5_1.EnableWindow(FALSE);
	m_cRefAngleInit1.EnableWindow(FALSE);
	m_editRefAngle.EnableWindow(FALSE);
	m_cRefSensorInit.SetCheck(1);

	m_cChkX.EnableWindow(FALSE);
	m_cChkY.EnableWindow(FALSE);
	m_cChkZ.EnableWindow(FALSE);
	m_cChkLongrun.EnableWindow(TRUE);

	m_editLongrunMax.SetLimitText(7);

	m_iLongrunCurr = 0;
	m_iLongrunMax = 0;

	m_pDlgDecision = new CDecisionDlg();// decision
	m_pDlgDecision->Create(IDD_DECISION, this);// decision
	VERIFY(m_pDlgDecision);
	m_pDlgDecision->GetClientRect(g_rcCliDcsDlg_BMo); // decision 최초 생성시 창 크기를 기억한다.

	return TRUE;  // return TRUE unless you set the focus to a control
}

HBRUSH CBoardLevelMo::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
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

void CBoardLevelMo::processdelay(DWORD dat)
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

void CBoardLevelMo::PortControl(CString cmd, int delay)
{
	int lfflag = 0;
	CString msg = _T("");

	if (cmd == "run")	lfflag = 1;

	if (!lfflag)	msg += (TCHAR)STX;
	else			msg += (TCHAR)0x0A;
	msg += cmd;
	if (!lfflag)	msg += (TCHAR)ETX;
	else			msg += (TCHAR)0x0A;

	g_pApp_BoardLevel_Motion->m_MySocket->Send(msg, strlen(msg));

	processdelay(delay);
}

void CBoardLevelMo::OnBnClickedButtonMotorAngle()
{
	// TODO: Add your control notification handler code here

	UpdateData(TRUE);

	CString msg = _T("");
	CString cmd = _T("");

	m_editMotorAngle.GetWindowText(cmd);

	msg += (TCHAR)STX;
	msg += "PMD071LM" + cmd;
	msg += (TCHAR)ETX;

	g_pApp_BoardLevel_Motion->m_MySocket->Send(msg, strlen(msg));

	processdelay(100);
}


void CBoardLevelMo::OnBnClickedButtonSensorWork()
{
	// TODO: Add your control notification handler code here

	PortControl("PMP041LM1", 10);
}


void CBoardLevelMo::OnBnClickedButtonOrigin()
{
	// TODO: Add your control notification handler code here

	PortControl("PMO011", 10);
}


void CBoardLevelMo::deviceclose(int which)
{
	m_bEnterTheTest = FALSE;

	switch (which)
	{
	case 0:
		g_pApp_BoardLevel_Motion->SendDataToEditControl("<MCLOSEC3>", &g_pApp_BoardLevel_Motion->m_ComuPort);
		break;
	case 1:
		g_pApp_BoardLevel_Motion->SendDataToEditControl("<MCLOSEC3>", &g_pApp_BoardLevel_Motion->m_ComuPortMaster);
		break;
	case 2:
		g_pApp_BoardLevel_Motion->SendDataToEditControl("<MCLOSEC3>", &g_pApp_BoardLevel_Motion->m_ComuPort2);
		break;
	}
}


void CBoardLevelMo::deviceopen(int which)
{
	m_bEnterTheTest = FALSE;

	switch (which)
	{
	case 0:
		g_pApp_BoardLevel_Motion->SendDataToEditControl("<MOPEN7F>", &g_pApp_BoardLevel_Motion->m_ComuPort);
		break;
	case 1:
		g_pApp_BoardLevel_Motion->SendDataToEditControl("<MOPEN7F>", &g_pApp_BoardLevel_Motion->m_ComuPortMaster);
		break;
	case 2:
		g_pApp_BoardLevel_Motion->SendDataToEditControl("<MOPEN7F>", &g_pApp_BoardLevel_Motion->m_ComuPort2);
		break;
	}
}


void CBoardLevelMo::device_send_all(CString SendCmd)
{
	if ((deviceconnected & DEVICE0) == DEVICE0)	devicesend(0, "<MTEST8D>");
	if ((deviceconnected & DEVICE1) == DEVICE1)	devicesend(1, "<MTEST8D>");
	if ((deviceconnected & DEVICE2) == DEVICE2)	devicesend(2, "<MTEST8D>");
}


void CBoardLevelMo::device_read_all()
{
	if ((deviceconnected & DEVICE0) == DEVICE0)	deviceread(0);
	if ((deviceconnected & DEVICE1) == DEVICE1)	deviceread(1);
	if ((deviceconnected & DEVICE2) == DEVICE2)	deviceread(2);
}


float CBoardLevelMo::atofval(int dot, CString dat)
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


int CBoardLevelMo::angle_measure(int sign)
{
	int returnval;
	CString x4angle, y4angle, z4angle, tmp, strTok;
	CString angle, angle_x, angle_y, angle_z, accel, accel_x, accel_y, accel_z, temp, g_accel, g_accel_x, g_accel_y, g_accel_z;

	returnval = 0;

	if ((deviceconnected & DEVICE0) == DEVICE0)
	{
		x4angle = (CString)g_pApp_BoardLevel_Motion->RcvBuff;
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

		m_angle_x = atofval(2, angle_x);
		m_angle_y = atofval(2, angle_y);
		m_angle_z = atofval(2, angle_z);
		m_accel_x = atofval(3, accel_x);
		m_accel_y = atofval(3, accel_y);
		m_accel_z = atofval(3, accel_z);
		m_g_accel_x = atofval(2, g_accel_x);
		m_g_accel_y = atofval(2, g_accel_y);
		m_g_accel_z = atofval(2, g_accel_z);

		if (sign == -1)
		{
			if (((m_angle_x	 >=-1070.0 && m_angle_x   <=-1030.0) &&
				(m_angle_y	 >=  -20.0 && m_angle_y   <=   20.0) &&
				(m_angle_z	 >=  -20.0 && m_angle_z   <=   20.0) &&
				(m_accel_x	 >=   -5.0 && m_accel_x   <=    5.0) &&
				(m_accel_y	 >= -188.0 && m_accel_y   <= -178.0) &&
				(m_accel_z	 >=  979.0 && m_accel_z	  <=  989.0))
				&&
				(m_bChkLongrun
				||
				(m_g_accel_x >=-1150.0 && m_g_accel_x <= -950.0) &&
				(m_g_accel_y >= -100.0 && m_g_accel_y <=  100.0) &&
				(m_g_accel_z >= -100.0 && m_g_accel_z <=  100.0)))		returnval |= DEVICE0;
		}
		else
		{
			if (((m_angle_x	 >= 1030.0 && m_angle_x   <= 1070.0) &&
				(m_angle_y	 >=  -20.0 && m_angle_y   <=   20.0) &&
				(m_angle_z	 >=  -20.0 && m_angle_z   <=   20.0) &&
				(m_accel_x	 >=   -5.0 && m_accel_x   <=    5.0) &&
				(m_accel_y	 >=  178.0 && m_accel_y   <=  188.0) &&
				(m_accel_z	 >=  979.0 && m_accel_z   <=  989.0))
				&&
				(m_bChkLongrun
				||
				(m_g_accel_x >=  950.0 && m_g_accel_x <= 1150.0) &&
				(m_g_accel_y >= -100.0 && m_g_accel_y <=  100.0) &&
				(m_g_accel_z >= -100.0 && m_g_accel_z <=  100.0)))		returnval |= DEVICE0;
		}
	}
	if ((deviceconnected & DEVICE1) == DEVICE1)
	{
		y4angle = (CString)g_pApp_BoardLevel_Motion->RcvBuffMaster;
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
		m_angle_x = atofval(2, angle_x);
		m_angle_y = atofval(2, angle_y);
		m_angle_z = atofval(2, angle_z);
		m_accel_x = atofval(3, accel_x);
		m_accel_y = atofval(3, accel_y);
		m_accel_z = atofval(3, accel_z);
		m_g_accel_x = atofval(2, g_accel_x);
		m_g_accel_y = atofval(2, g_accel_y);
		m_g_accel_z = atofval(2, g_accel_z);
#endif
		if (sign == -1)
		{
			if (((m_angle_x	 >=  -20.0 && m_angle_x   <=   20.0) &&
				(m_angle_y	 >= 1030.0 && m_angle_y   <= 1070.0) &&
				(m_angle_z	 >=  -20.0 && m_angle_z   <=   20.0) &&
				(m_accel_x	 >= -188.0 && m_accel_x   <= -178.0) && 
				(m_accel_y	 >=   -5.0 && m_accel_y   <=    5.0) &&
				(m_accel_z	 >=  979.0 && m_accel_z   <=  989.0))
				&&
				(m_bChkLongrun
				||
				(m_g_accel_x >= -100.0 && m_g_accel_x <=  100.0) &&
				(m_g_accel_y >=  950.0 && m_g_accel_y <= 1150.0) &&
				(m_g_accel_z >= -100.0 && m_g_accel_z <=  100.0)))		returnval |= DEVICE1;
		}
		else
		{
			if (((m_angle_x	 >=  -20.0 && m_angle_x   <=   20.0) &&
				(m_angle_y	 >=-1070.0 && m_angle_y   <=-1030.0) &&
				(m_angle_z	 >=  -20.0 && m_angle_z   <=   20.0) &&
				(m_accel_x	 >=  178.0 && m_accel_x   <=  188.0) &&
				(m_accel_y	 >=   -5.0 && m_accel_y   <=    5.0) &&
				(m_accel_z	 >=  979.0 && m_accel_z   <=  989.0))
				&&
				(m_bChkLongrun
				||
				(m_g_accel_x >= -100.0 && m_g_accel_x <=  100.0) &&
				(m_g_accel_y >=-1150.0 && m_g_accel_y <= -950.0) &&
				(m_g_accel_z >= -100.0 && m_g_accel_z <=  100.0)))		returnval |= DEVICE1;
		}
	}
	if ((deviceconnected & DEVICE2) == DEVICE2)
	{
		z4angle = (CString)g_pApp_BoardLevel_Motion->RcvBuff2;
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
		m_angle_x = atofval(2, angle_x);
		m_angle_y = atofval(2, angle_y);
		m_angle_z = atofval(2, angle_z);
		m_accel_x = atofval(3, accel_x);
		m_accel_y = atofval(3, accel_y);
		m_accel_z = atofval(3, accel_z);
		m_g_accel_x = atofval(2, g_accel_x);
		m_g_accel_y = atofval(2, g_accel_y);
		m_g_accel_z = atofval(2, g_accel_z);
#endif
		if (sign == -1)
		{
			if (!m_bChkLongrun)
			{
				if ((m_angle_x   >= -20.0  && m_angle_x   <=   20.0) &&
					(m_angle_y   >= 1030.0 && m_angle_y   <= 1070.0) &&
					(m_angle_z   >= -20.0  && m_angle_z   <=   20.0) &&
					(m_accel_x   >= -188.0 && m_accel_x   <= -178.0) &&
					(m_accel_y   >= -5.0   && m_accel_y   <=    5.0) &&
					(m_accel_z   >= 979.0  && m_accel_z   <=  989.0) &&
					(m_g_accel_x >= -100.0 && m_g_accel_x <=  100.0) &&
					(m_g_accel_y >= 950.0  && m_g_accel_y <= 1150.0) &&
					(m_g_accel_z >= -100.0 && m_g_accel_z <=  100.0))		returnval |= DEVICE2;
			}
			else
			{
				if((m_g_accel_x >= -100.0 && m_g_accel_x <= 100.0) &&
				   (m_g_accel_y >=  950.0 && m_g_accel_y <= 1150.0)&&
				   (m_g_accel_z >= -100.0 && m_g_accel_z <= 100.0))		returnval |= DEVICE2;
			}
		}
		else
		{
			if (!m_bChkLongrun)
			{
				if ((m_angle_x   >= -20.0   && m_angle_x   <=    20.0) &&
					(m_angle_y   >= -1070.0 && m_angle_y   <= -1030.0) &&
					(m_angle_z   >= -20.0   && m_angle_z   <=    20.0) &&
					(m_accel_x   >= 178.0   && m_accel_x   <=   188.0) &&
					(m_accel_y   >= -5.0    && m_accel_y   <=     5.0) &&
					(m_accel_z   >= 979.0   && m_accel_z   <=   989.0) &&
					(m_g_accel_x >= -100.0  && m_g_accel_x <=   100.0) &&
					(m_g_accel_y >= -1150.0 && m_g_accel_y <=  -950.0) &&
					(m_g_accel_z >= -100.0  && m_g_accel_z <=  100.0))		returnval |= DEVICE2;
			}
			else
			{
				if((m_g_accel_x >= -100.0  && m_g_accel_x <=  100.0) &&
				   (m_g_accel_y >= -1150.0 && m_g_accel_y <= -950.0) &&
				   (m_g_accel_z >= -100.0  && m_g_accel_z <=  100.0))		returnval |= DEVICE2;
			}
		}
	}

	return returnval;
}


void CBoardLevelMo::devicesend(int which, CString SendCmd)
{
	switch (which)
	{
	case 0:
		g_pApp_BoardLevel_Motion->SendDataToEditControl(SendCmd, &g_pApp_BoardLevel_Motion->m_ComuPort);
		break;
	case 1:
		g_pApp_BoardLevel_Motion->SendDataToEditControlMaster(SendCmd, &g_pApp_BoardLevel_Motion->m_ComuPortMaster);
		break;
	case 2:
		g_pApp_BoardLevel_Motion->SendDataToEditControl2(SendCmd, &g_pApp_BoardLevel_Motion->m_ComuPort2);
		break;
	case 3:
		g_pApp_BoardLevel_Motion->SendDataToEditControl3(SendCmd, &g_pApp_BoardLevel_Motion->m_ComuPort3);
		break;
	}
}


void CBoardLevelMo::deviceread(int which)
{
	switch (which)
	{
	case 0:
		g_pApp_BoardLevel_Motion->ReadDataToEditControl(1, &g_pApp_BoardLevel_Motion->m_ComuPort);
		break;
	case 1:
		g_pApp_BoardLevel_Motion->ReadDataToEditControlMaster(1, &g_pApp_BoardLevel_Motion->m_ComuPortMaster);
		break;
	case 2:
		g_pApp_BoardLevel_Motion->ReadDataToEditControl2(1, &g_pApp_BoardLevel_Motion->m_ComuPort2);
		break;
	case 3:
		g_pApp_BoardLevel_Motion->ReadDataToEditControl3(1, &g_pApp_BoardLevel_Motion->m_ComuPort3);
		break;
	}
}


void CBoardLevelMo::angle_temp_data()
{
	CString x4angle, y4angle, z4angle, tmp, strTok;
	CString angle, angle_x, angle_y, angle_z, accel, accel_x, accel_y, accel_z, temp, g_accel, g_accel_x, g_accel_y, g_accel_z;

	if ((deviceconnected & DEVICE0) == DEVICE0)
	{
		x4angle = (CString)g_pApp_BoardLevel_Motion->RcvBuff;
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

		m_angle_x = (float)atof(angle_x);
		m_accel_x = (float)atof(accel_x);
		m_g_accel_x = (float)atof(g_accel_x);
		m_temp[0] = (float)atof(temp);
	}
	if ((deviceconnected & DEVICE1) == DEVICE1)
	{
		y4angle = (CString)g_pApp_BoardLevel_Motion->RcvBuffMaster;
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

		m_angle_y = (float)atof(angle_y);
		m_accel_y = (float)atof(accel_y);
		m_g_accel_y = (float)atof(g_accel_y);
		m_temp[1] = (float)atof(temp);
	}
	if ((deviceconnected & DEVICE2) == DEVICE2)
	{
		z4angle = (CString)g_pApp_BoardLevel_Motion->RcvBuff2;
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

		m_angle_z = (float)atof(angle_z);
		m_accel_z = (float)atof(accel_z);
		m_g_accel_z = (float)atof(g_accel_z);
		m_temp[2] = (float)atof(temp);
	}
}

BYTE CBoardLevelMo::angle_0_init()
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
		refangle = (CString)g_pApp_BoardLevel_Motion->RcvBuff3;
		sRefangle = refangle.Mid(1);

		if (g_pApp_BoardLevel_Motion->RcvBuff3[0] == '+')	f_refangle = (float)atof(sRefangle);
		else												f_refangle = (float)atof(sRefangle) * -1;

		if(m_Refangle - f_refangle > 0.005)
		{
			PortControl("PMD041LM1", 10);
		}
		else if(m_Refangle - f_refangle < -0.005)
		{
			PortControl("PMD051LM-1", 10);
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

void CBoardLevelMo::TestResult(CString msg)
{
	// decision
	CRect rcParent;
	GetWindowRect(rcParent);
	m_pDlgDecision->MoveWindow(rcParent.left + (rcParent.Width() - g_rcCliDcsDlg_BMo.Width()) / 2, rcParent.top + (rcParent.Height() - g_rcCliDcsDlg_BMo.Height()) / 2 - YPOS_OFFSET, g_rcCliDcsDlg_BMo.Width(), g_rcCliDcsDlg_BMo.Height());

	if (strcmp(msg, _T("PASS")) == 0)
	{
		m_iTestStep++;
		m_bEnterTheTest = TRUE;
		m_iTestResult = TRUE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion.SetWindowText(_T(msg));

		if (m_iTestStep == OPEN + 1)					m_cDeviceOpen.SetCheck(1);
		if (m_iTestStep == DEGREE_0_CHECK + 1)			m_cDegree0Check.SetCheck(1);
		if (m_iTestStep == ANGLE_p_10_5_0 + 1)			m_cAngle_p_10_5_0.SetCheck(1);
		if (m_iTestStep == MEASURE_ANGLE_p_10_5 + 1)	m_cMeasureAngle0.SetCheck(1);
		if (m_iTestStep == ANGLE_m_10_5_0 + 1)			m_cAngle_m_10_5_0.SetCheck(1);
		if (m_iTestStep == ANGLE_INIT_0 + 1)			m_cRefAngleInit0.SetCheck(1);
		if (m_iTestStep == ANGLE_m_10_5_1 + 1)			m_cAngle_m_10_5_1.SetCheck(1);
		if (m_iTestStep == MEASURE_ANGLE_m_10_5 + 1)	m_cMeasureAngle1.SetCheck(1);
		if (m_iTestStep == ANGLE_p_10_5_1 + 1)			m_cAngle_p_10_5_1.SetCheck(1);
		if (m_iTestStep == ANGLE_INIT_1 + 1)			m_cRefAngleInit1.SetCheck(1);
	}
	else if (strcmp(msg, _T("STOP")) == 0)
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion.SetWindowText(_T(msg));
		OnBnClickedButtonStop();
	}
	else if (strcmp(msg, _T("END")) == 0)
	{
		m_cRefAngleInit1.SetCheck(1);
		m_iTestResult = TRUE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();
		m_EditDataMonitorMotion.SetWindowText(_T(msg));
		OnBnClickedButtonStop();

		m_pDlgDecision->SetDecision(TRUE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
	else
	{
		m_iTestResult = FALSE;
		GetDlgItem(IDC_TEST_RESULT)->Invalidate();

		msg.Format("%s_%d", msg, m_iTestStep);

		m_EditDataMonitorMotion.SetWindowText(_T(msg));
		OnBnClickedButtonStop();

		m_pDlgDecision->SetDecision(FALSE);// decision
		m_pDlgDecision->ShowWindow(SW_SHOW);// decision
		m_pDlgDecision->AutoHide(3000);
	}
}


UINT ThreadStatus_BoardLevelMo(LPVOID lParam)
{
	CBoardLevelMo *pCBLM;
	pCBLM = (CBoardLevelMo*)lParam;

	pCBLM->deviceconnected = 0;

	int i = 0;

	while (pCBLM->m_bThreadStatus)
	{
		if (pCBLM->m_iTestStep == JIG_INIT)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->PortControl("PMO011", 5000);
				pCBLM->PortControl("PMP041LM1", 3000);
#if 0
				pCBLM->devicesend(3, "setzcur");
				pCBLM->processdelay(20);
				pCBLM->deviceread(3);
#endif
				pCBLM->TestResult(_T("PASS"));
			}
		}
		else if (pCBLM->m_iTestStep == OPEN)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->deviceclose(0);
				pCBLM->deviceclose(1);
				pCBLM->deviceclose(2);

				pCBLM->processdelay(150);

				pCBLM->deviceopen(0);
				pCBLM->deviceopen(1);
				pCBLM->deviceopen(2);

				pCBLM->processdelay(1500);

				pCBLM->deviceread(0);
				if (strstr((const char*)g_pApp_BoardLevel_Motion->RcvBuff, "[MOPEN=OK56]") != 0)		pCBLM->deviceconnected |= DEVICE0;
				pCBLM->deviceread(1);
				if (strstr((const char*)g_pApp_BoardLevel_Motion->RcvBuffMaster, "[MOPEN=OK56]") != 0)	pCBLM->deviceconnected |= DEVICE1;
				pCBLM->deviceread(2);
				if (strstr((const char*)g_pApp_BoardLevel_Motion->RcvBuff2, "[MOPEN=OK56]") != 0)		pCBLM->deviceconnected |= DEVICE2;

				if (pCBLM->deviceconnected != 0)		pCBLM->TestResult(_T("PASS")); // Incrase Level 
				else if (pCBLM->deviceconnected == 0)	pCBLM->TestResult(_T("FAIL"));

				if ((pCBLM->deviceconnected&DEVICE0) == DEVICE0)	pCBLM->m_cChkX.SetCheck(1);
				if ((pCBLM->deviceconnected&DEVICE1) == DEVICE1)	pCBLM->m_cChkY.SetCheck(1);
				if ((pCBLM->deviceconnected&DEVICE2) == DEVICE2)	pCBLM->m_cChkZ.SetCheck(1);
			}
		}
		else if (pCBLM->m_iTestStep == DEGREE_0_CHECK)
		{
			CString refangle;

			if (pCBLM->m_bEnterTheTest && pCBLM->m_bRefSensorInit)
			{
				pCBLM->devicesend(3, "setzcur");
				pCBLM->processdelay(100);
				pCBLM->deviceread(3);
				pCBLM->devicesend(3, "get---x");
				pCBLM->processdelay(100);
				pCBLM->deviceread(3);

				if (g_pApp_BoardLevel_Motion->RcvBuff3[0] == '\0')	pCBLM->TestResult(_T("FAIL"));
				else 
				{
					pCBLM->m_sRefAngle = (CString)g_pApp_BoardLevel_Motion->RcvBuff3;
					refangle = pCBLM->m_sRefAngle.Mid(1);

					if (g_pApp_BoardLevel_Motion->RcvBuff3[0] == '+')		pCBLM->m_Refangle = (float)atof(refangle);
					else if(g_pApp_BoardLevel_Motion->RcvBuff3[0] == '0')	pCBLM->m_Refangle = (float)atof(refangle);
					else													pCBLM->m_Refangle = (float)atof(refangle) * -1;
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
		else if (pCBLM->m_iTestStep == ANGLE_p_10_5_0)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->PortControl("PMD071LM1500", 3000);
				if (theApp.App_RcvPort.Find("pmd0011") != -1)	pCBLM->TestResult(_T("PASS"));
				else
				{
					for (i = 0; i < 10; i++)
					{
						pCBLM->PortControl("PMD071LM1500", 3000);
						if (theApp.App_RcvPort.Find("pmd0011") != -1)	break;
						else;
					}

					theApp.App_RcvPort = "";
					if (i < 10)	pCBLM->TestResult(_T("PASS"));
					else		pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		else if (pCBLM->m_iTestStep == MEASURE_ANGLE_p_10_5)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->device_send_all("<MTEST8D>");
				pCBLM->processdelay(100);
				pCBLM->device_read_all();

				if (pCBLM->deviceconnected == pCBLM->angle_measure(0))	pCBLM->TestResult(_T("PASS"));
				else
				{
					for (i = 0; i < 10; i++)
					{
						pCBLM->device_send_all("<MTEST8D>");
						pCBLM->processdelay(3000);
						pCBLM->device_read_all();

						if (pCBLM->deviceconnected == pCBLM->angle_measure(0))	break;
						else													;
					}

					if (i < 10)	pCBLM->TestResult(_T("PASS"));
					else		pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		else if (pCBLM->m_iTestStep == ANGLE_m_10_5_0)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->PortControl("PMD081LM-1500", 1000);
				if (theApp.App_RcvPort.Find("pmd0011") != -1)	pCBLM->TestResult(_T("PASS"));
				else
				{
					for (i = 0; i < 10; i++)
					{
						pCBLM->PortControl("PMD081LM-1500", 1000);
						if (theApp.App_RcvPort.Find("pmd0011") != -1)	break;
						else;
					}

					theApp.App_RcvPort = "";
					if (i < 10)	pCBLM->TestResult(_T("PASS"));
					else		pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		else if (pCBLM->m_iTestStep == ANGLE_INIT_0)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				if (pCBLM->angle_0_init() != 0)	pCBLM->TestResult(_T("PASS"));
				else							pCBLM->TestResult(_T("FAIL"));
			}
		}
		else if (pCBLM->m_iTestStep == pCOM_INIT)
		{
			if (pCBLM->m_bEnterTheTest && !pCBLM->m_bChkLongrun)
			{
				pCBLM->deviceclose(0);
				pCBLM->deviceclose(1);
				pCBLM->deviceclose(2);

				pCBLM->processdelay(150);

				pCBLM->deviceopen(0);
				pCBLM->deviceopen(1);
				pCBLM->deviceopen(2);

				pCBLM->processdelay(1500);

				pCBLM->deviceread(0);
				if (strstr((const char*)g_pApp_BoardLevel_Motion->RcvBuff, "[MOPEN=OK56]") != 0)		pCBLM->deviceconnected |= DEVICE0;
				pCBLM->deviceread(1);
				if (strstr((const char*)g_pApp_BoardLevel_Motion->RcvBuffMaster, "[MOPEN=OK56]") != 0)	pCBLM->deviceconnected |= DEVICE1;
				pCBLM->deviceread(2);
				if (strstr((const char*)g_pApp_BoardLevel_Motion->RcvBuff2, "[MOPEN=OK56]") != 0)		pCBLM->deviceconnected |= DEVICE2;

				if (pCBLM->deviceconnected != 0)		pCBLM->TestResult(_T("PASS"));
				else if (pCBLM->deviceconnected == 0)	pCBLM->TestResult(_T("FAIL"));
			}
			else if (pCBLM->m_bEnterTheTest && pCBLM->m_bChkLongrun)	pCBLM->TestResult(_T("PASS"));
		}
		else if (pCBLM->m_iTestStep == ANGLE_m_10_5_1)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->PortControl("PMD081LM-1500", 3000);
				if (theApp.App_RcvPort.Find("pmd0011") != -1)	pCBLM->TestResult(_T("PASS"));
				else
				{
					for (i = 0; i < 10; i++)
					{
						pCBLM->PortControl("PMD081LM-1500", 3000);
						if (theApp.App_RcvPort.Find("pmd0011") != -1)	break;
						else;
					}

					theApp.App_RcvPort = "";
					if (i < 10)	pCBLM->TestResult(_T("PASS"));
					else		pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		else if (pCBLM->m_iTestStep == MEASURE_ANGLE_m_10_5)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->device_send_all("<MTEST8D>");
				pCBLM->processdelay(100);
				pCBLM->device_read_all();

				if (pCBLM->deviceconnected == pCBLM->angle_measure(-1))		pCBLM->TestResult(_T("PASS"));
				else
				{
					for (i = 0; i < 10; i++)
					{
						pCBLM->device_send_all("<MTEST8D>");
						pCBLM->processdelay(3000);
						pCBLM->device_read_all();

						if (pCBLM->deviceconnected == pCBLM->angle_measure(-1))	break;
						else;
					}

					if (i < 10)	pCBLM->TestResult(_T("PASS"));
					else		pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		else if (pCBLM->m_iTestStep == ANGLE_p_10_5_1)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				pCBLM->PortControl("PMD071LM1500", 1000);
				if (theApp.App_RcvPort.Find("pmd0011") != -1)	pCBLM->TestResult(_T("PASS"));
				else
				{
					for (i = 0; i < 10; i++)
					{
						pCBLM->PortControl("PMD071LM1500", 1000);
						if (theApp.App_RcvPort.Find("pmd0011") != -1)	break;
						else;
					}

					theApp.App_RcvPort = "";
					if (i < 10)	pCBLM->TestResult(_T("PASS"));
					else		pCBLM->TestResult(_T("FAIL"));
				}
			}
		}
		else if (pCBLM->m_iTestStep == ANGLE_INIT_1)
		{
			if (pCBLM->m_bEnterTheTest)
			{
				if (pCBLM->angle_0_init() != 0)
				{
					if (pCBLM->m_bChkLongrun)
					{
						if (pCBLM->m_iLongrunCurr < pCBLM->m_iLongrunMax)
						{
							pCBLM->m_iTestStep = DEGREE_0_CHECK;
							pCBLM->m_iLongrunCurr++;
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


void CBoardLevelMo::OnBnClickedButtonStart() // Motion Start 
{
	UpdateData(TRUE);
	m_bThreadStatus = TRUE;
	m_bEnterTheTest = TRUE;
	m_iTestStep = JIG_INIT;

	m_btnTestStart.EnableWindow(FALSE);
	m_btnMotorAngle.EnableWindow(FALSE);
	m_btnEmioStart.EnableWindow(FALSE);
	m_btnEmioStop.EnableWindow(FALSE);
	m_btnSensorWork.EnableWindow(FALSE);
	m_btnSensorOrigin.EnableWindow(FALSE);
	m_editMotorAngle.EnableWindow(FALSE);
	m_cChkLongrun.EnableWindow(FALSE);

	m_cDeviceOpen.SetCheck(0);
	m_cDegree0Check.SetCheck(0);
	m_cAngle_p_10_5_0.SetCheck(0);
	m_cMeasureAngle0.SetCheck(0);
	m_cAngle_m_10_5_1.SetCheck(0);
	m_cRefAngleInit0.SetCheck(0);
	m_cAngle_m_10_5_0.SetCheck(0);
	m_cMeasureAngle1.SetCheck(0);
	m_cAngle_p_10_5_1.SetCheck(0);
	m_cRefAngleInit1.SetCheck(0);

	m_editLongrunCurr.EnableWindow(FALSE);
	m_editLongrunMax.EnableWindow(FALSE);

	m_iLongrunCurr = 0;
	UpdateData(FALSE);

	m_cChkX.SetCheck(0);
	m_cChkY.SetCheck(0);
	m_cChkZ.SetCheck(0);

	AfxBeginThread(ThreadStatus_BoardLevelMo, (LPVOID)this);
}


void CBoardLevelMo::OnBnClickedButtonStop()
{
	// TODO: Add your control notification handler code here

	m_bThreadStatus = FALSE;

	m_btnTestStart.EnableWindow(TRUE);
	m_btnMotorAngle.EnableWindow(TRUE);
	m_btnEmioStart.EnableWindow(TRUE);
	m_btnEmioStop.EnableWindow(TRUE);
	m_btnSensorWork.EnableWindow(TRUE);
	m_btnSensorOrigin.EnableWindow(TRUE);
	m_editMotorAngle.EnableWindow(TRUE);

	m_cChkLongrun.EnableWindow(TRUE);
	m_editLongrunCurr.EnableWindow(TRUE);
	m_editLongrunMax.EnableWindow(TRUE);
}


void CBoardLevelMo::OnBnClickedButtonEmioStart()
{
	// TODO: Add your control notification handler code here

	PortControl("run", 100);
	PortControl("PPP00", 100);
	PortControl("PPR015", 100);
}


void CBoardLevelMo::OnBnClickedButtonEmioStop()
{
	// TODO: Add your control notification handler code here

	PortControl("PST", 100);
}
