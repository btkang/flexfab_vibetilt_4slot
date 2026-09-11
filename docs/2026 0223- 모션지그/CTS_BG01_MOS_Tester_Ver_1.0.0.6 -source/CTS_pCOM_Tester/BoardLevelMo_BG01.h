#pragma once
#include "afxwin.h"
#include "DecisionDlg.h"
#include "afxcmn.h"

// CBoardLevelMo dialog

class CBoardLevelMoBG01 : public CDialogEx
{
	DECLARE_DYNAMIC(CBoardLevelMoBG01)

public:
	CBoardLevelMoBG01(CWnd* pParent = NULL);   // standard constructor
	virtual ~CBoardLevelMoBG01();

	CDecisionDlg	*m_pDlgDecision_BG01;
	afx_msg LRESULT ForCustomMessageFromThread(WPARAM  wParam, LPARAM lParam);
// Dialog Data
	enum { IDD = IDD_BOARDMO_BG01 };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	virtual BOOL OnInitDialog();

	CEdit m_editMotorAngle_BG01;
	CString m_sMotorAngle_BG01;

	CButton m_btnMotorAngle_BG01;

	CButton m_btnSensorWork_BG01;
	CButton m_btnSensorOrigin_BG01;

	CButton m_btnEmioStart_BG01;
	CButton m_btnEmioStop_BG01;

	CButton m_btnTestStart_BG01;
	CButton m_btnTestStop_BG01;
	afx_msg void OnBnClickedButtonMotorAngle();
	afx_msg void OnBnClickedButtonSensorWork();
	afx_msg void OnBnClickedButtonOrigin();
	afx_msg void OnBnClickedButtonStart();
	afx_msg void OnBnClickedButtonStop();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	CEdit m_EditDataMonitorMotion_BG01;

	int m_iTestStep_BG01;
	int m_iTestResult_BG01;

	int m_iTestSignIndex[10];

	void PortControl(CString cmd, int delay);

	afx_msg void OnBnClickedButtonEmioStart();
	afx_msg void OnBnClickedButtonEmioStop();

	BOOL m_bEnterTheTest_BG01;
	BOOL m_bThreadStatus_BG01;
	BOOL m_b_tcp_connection_status_BG01;

	void deviceopen(int which);
	void deviceclose(int which);
	int deviceconnected_BG01;
	float m_angle_x_BG01;
	float m_angle_y_BG01;
	float m_angle_z_BG01;
	float m_accel_x_BG01;
	float m_accel_y_BG01;
	float m_accel_z_BG01;
	float m_g_accel_x_BG01;
	float m_g_accel_y_BG01;
	float m_g_accel_z_BG01;
	float m_temp_BG01[3];
	float m_Refangle_BG01;

	unsigned char uc_board_insert_confirm_flag;
	//unsigned char uc_board_insert_confirm_flag;
	//unsigned char uc_board_insert_confirm_flag;

	void device0_send(CString SendCmd);
	void device0_read(void);
	void device1_send(CString SendCmd);
	void device1_read(void);
	void device2_send(CString SendCmd);
	void device2_read(void);

	void devicesend(int which, CString SendCmd);
	void deviceread(int which);
	void TestResult(CString msg);
	void TestResult_Limit(CString msg);
	void TestResult_Limit_Type2(CString msg);
	void TestResult_Each(CString msg, int iresult_each_board, unsigned char uc_board_insert_flag);
	CButton m_cDeviceOpen_BG01;
	CButton m_cDegree0Check_BG01;
	CButton m_cAngle_p_10_5_0_BG01;
	CButton m_cMeasureAngle0_BG01;
	CButton m_cAngle_m_10_5_1_BG01;
	CButton m_cRefAngleInit0_BG01;
	CButton m_cAngle_m_10_5_0_BG01;
	CButton m_cMeasureAngle1_BG01;
	CButton m_cAngle_p_10_5_1_BG01;
	CButton m_cRefAngleInit1_BG01;

	void processdelay(DWORD dat);
	void device_send_all(CString SendCmd);
	void device_read_all();

	void angle_temp_data();
	int angle_measure(int sign);
	int angle_measureEach(int signX, int signY, int signZ);
	BYTE angle_0_init();
	BYTE angle_0_init_Revision(); // Temporarily 
	CButton m_cRefSensorInit_BG01;
	BOOL m_bRefSensorInit_BG01;
	CEdit m_editRefAngle_BG01;
	CString m_sRefAngle_BG01;
	CProgressCtrl m_progress_BG01;
	CButton m_cChkX_BG01;
	CButton m_cChkY_BG01;
	CButton m_cChkZ_BG01;

	float atofval(int dot, CString dat);
	CButton m_cChkLongrun_BG01;
	BOOL m_bChkLongrun_BG01;
	CEdit m_editLongrunMax_BG01;
	CEdit m_editLongrunCurr_BG01;
	int m_iLongrunCurr_BG01;
	int m_iLongrunMax_BG01;
	afx_msg void OnEnChangeEditMotorAngleBg01();
	afx_msg void OnBnClickedButtonMotorSet();
	CButton m_btnMotorSet_BG01;
	CButton m_btnClear_BG01;
	CWinThread* m_RThread_BG01;

};
