#pragma once
#include "afxwin.h"
#include "DecisionDlg.h"
#include "afxcmn.h"

// CBoardLevelMo dialog

class CBoardLevelMo : public CDialogEx
{
	DECLARE_DYNAMIC(CBoardLevelMo)

public:
	CBoardLevelMo(CWnd* pParent = NULL);   // standard constructor
	virtual ~CBoardLevelMo();

	CDecisionDlg	*m_pDlgDecision;
	afx_msg LRESULT ForCustomMessageFromThread(WPARAM  wParam, LPARAM lParam);
// Dialog Data
	enum { IDD = IDD_BOARDLEVELMO };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	virtual BOOL OnInitDialog();

	CEdit m_editMotorAngle;
	CString m_sMotorAngle;
	CButton m_btnMotorAngle;
	CButton m_btnSensorWork;
	CButton m_btnSensorOrigin;
	CButton m_btnTestStart;
	CButton m_btnTestStop;
	afx_msg void OnBnClickedButtonMotorAngle();
	afx_msg void OnBnClickedButtonSensorWork();
	afx_msg void OnBnClickedButtonOrigin();
	afx_msg void OnBnClickedButtonStart();
	afx_msg void OnBnClickedButtonStop();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	CEdit m_EditDataMonitorMotion;

	int m_iTestStep;
	int m_iTestResult;

	void PortControl(CString cmd, int delay);
	CButton m_btnEmioStart;
	CButton m_btnEmioStop;
	afx_msg void OnBnClickedButtonEmioStart();
	afx_msg void OnBnClickedButtonEmioStop();

	BOOL m_bEnterTheTest;
	BOOL m_bThreadStatus;

	void deviceopen(int which);
	void deviceclose(int which);
	int deviceconnected;
	float m_angle_x;
	float m_angle_y;
	float m_angle_z;
	float m_accel_x;
	float m_accel_y;
	float m_accel_z;
	float m_g_accel_x;
	float m_g_accel_y;
	float m_g_accel_z;
	float m_temp[3];
	float m_Refangle;

	void device0_send(CString SendCmd);
	void device0_read(void);
	void device1_send(CString SendCmd);
	void device1_read(void);
	void device2_send(CString SendCmd);
	void device2_read(void);

	void devicesend(int which, CString SendCmd);
	void deviceread(int which);
	void TestResult(CString msg);
	CButton m_cDeviceOpen;
	CButton m_cDegree0Check;
	CButton m_cAngle_p_10_5_0;
	CButton m_cMeasureAngle0;
	CButton m_cAngle_m_10_5_1;
	CButton m_cRefAngleInit0;
	CButton m_cAngle_m_10_5_0;
	CButton m_cMeasureAngle1;
	CButton m_cAngle_p_10_5_1;
	CButton m_cRefAngleInit1;

	void processdelay(DWORD dat);
	void device_send_all(CString SendCmd);
	void device_read_all();

	void angle_temp_data();
	int angle_measure(int sign);
	BYTE angle_0_init();
	CButton m_cRefSensorInit;
	BOOL m_bRefSensorInit;
	CEdit m_editRefAngle;
	CString m_sRefAngle;
	CProgressCtrl m_progress;
	CButton m_cChkX;
	CButton m_cChkY;
	CButton m_cChkZ;

	float atofval(int dot, CString dat);
	CButton m_cChkLongrun;
	BOOL m_bChkLongrun;
	CEdit m_editLongrunMax;
	CEdit m_editLongrunCurr;
	int m_iLongrunCurr;
	int m_iLongrunMax;
};
