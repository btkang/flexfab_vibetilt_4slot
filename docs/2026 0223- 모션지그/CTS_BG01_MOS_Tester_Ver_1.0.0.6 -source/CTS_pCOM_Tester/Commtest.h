#pragma once
#include "afxwin.h"
#include "DecisionDlg.h"
#include "afxcmn.h"

// CCommtest dialog

class CCommtest : public CDialogEx
{
private:
	DECLARE_DYNAMIC(CCommtest)

public:
	CCommtest(CWnd* pParent = NULL);   // standard constructor
	virtual ~CCommtest();

	CDecisionDlg	*m_pDlgDecision;
	afx_msg LRESULT ForCustomMessageFromThread(WPARAM  wParam, LPARAM lParam);
// Dialog Data
	enum { IDD = IDD_COMMTEST };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	CButton m_BtnVerChk;
	CButton m_BtnConf;
	CButton m_BtnReset;
	CButton m_BtnAutoStart;
	CButton m_BtnAutoStop;
	CButton m_cVerChk;
	CButton m_cConf;
	CButton m_cRcvWaitRssi;
	CButton m_cIoTest;
	CButton m_cUserData;

	BOOL m_bThreadStatus;
	CWinThread *m_pThread;
	CWinThread *m_pProgressThread;
	enum ThreadWorkingType
	{
		THREAD_STOP = 0,
		THREAD_RUNNING,
		THREAD_PAUSE,
	};
	ThreadWorkingType m_eThreadWork;
	BOOL m_bEnterTheTest;

	int m_iTestStep;
	int m_iTempIrTryValue;
	int m_iTempRfTryValue;
	int m_iTempIrFailValue;
	int m_iTestResult;
	int m_iTimercnt;

	CEdit m_EditRcvStatComm;
	CEdit m_cCompVersion;

	afx_msg void OnBnClickedVerCheck();
	afx_msg void OnBnClickedConfClear();
	afx_msg void OnBnClickedReset();
	afx_msg void OnBnClickedAutoStart();
	afx_msg void OnBnClickedAutoStop();
	afx_msg void OnBnClickedButton6();
	afx_msg void OnBnClickedButton1();
	afx_msg void OnBnClickedButton2();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	LRESULT OnUpdateData(WPARAM wParam, LPARAM lParam);

	CString ConverToHex(CString data);
	int GetFindCharCount(CString param_string, char param_find_char);
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	virtual BOOL OnInitDialog();

	void SendData_to_test(CString SendCmd);
	void SendData_to_ref(CString SendCmd);
	void SendData_to_u_test(CString SendCmd);
	void SendData_to_u_ref(CString SendCmd);
	void SendData_to_supply(CString SendCmd);

	void ReadData_to_test(int DelayTime);
	void ReadData_to_ref(int DelayTime);
	void ReadData_to_u_test(int DelayTime);
	void ReadData_to_u_ref(int DelayTime);
	void ReadData_to_supply(int DelayTime);

	void TestResult(CString msg);
	void Cidr2gMode();
	void Cidr5gMode();
	void CidrId();
	void IrLane1();
	void IrLane2();
	void Rf24g();
	void XMRF0();
	void Rf5g();
	void XM2();
	void XM3();
	void VerChkInThread();
	void IrReduce();
	void CidrConfClear();
	void TEMP();

	void PortControl(CString cmd);
	void entertheconsole(int which);
	void enterthenormal(int which);
	void debugmode(int which, int val);
	void debugmode_c(int which, int val);
	void modesel(int mode, int sel);
	void check_rssi(int which);
	void AutoGainControl(int which, int val);
	void AutoGainControl_c(int which, int val);
	void AutoGainControlCheck(int which);
	BOOL GoOffCheck(int which);
	BOOL DclineCheck(int which);
	void AutogainCheck(int which);
	void ConfigSave(int which);
	void PcomReset(int which);
	void PortClear(int which);
	void rcv_debugmsg(int which);
	void rfidch(int which, CString str);
	void vhlid(int which, CString str);
	void datachangecheck(int which);
	void ConfClear(int which);
	void UserLength(int which, int val);
	void UserDataCheck(int which, int length);

	void sigfs(int val);
	void mr0(int which, int val);
	void mr0_r(int which);

	void RamTest(int which, int timercnt);
	void RomTest(int which, int timercnt);
	void IoTest(int val);
	void IoTrigger();

	void Xpns(int which, int val);

	void Clearcom_c(int which);

	CString CompStr;
	CString RcvStr;
	CString m_sCompVersion;

	CString m_sRightTrim;
	CString m_sTmp;
	CString m_sTryValue;
	CString m_sFailValue;
	BYTE m_aRcvData[24];
	CEdit m_cCommTestRfid;
	CString m_sCommTestRfid;
	CEdit m_cCommTestVhlid;
	CString m_sCommTestVhlid;
	int m_iTargetRssi[2];
	int m_iMineRssi[128];
	BOOL m_iRssiCheckDone;

	int m_iRetry_ReadPort;
	CButton m_cTargetRssi;
	CButton m_cMineRssi;
	CProgressCtrl m_progress;
	CButton m_cAutoGainCon;

	float m_fAngleX[2];
	float m_fAngleY[2];
	float m_fAngleZ[2];
	float m_fTemp[2];

	void mbase(int which, int dat);
	void minfo(int which, int dat);
	void mtemp(int which);
	void processdelay(DWORD dat);
	void pcomioport(int on);
	CEdit m_editPreAngleX;
	CEdit m_editPreAngleY;
	CEdit m_editPreAngleZ;
	CEdit m_editPreTemp;
	CEdit m_editAngleX;
	CEdit m_editAngleY;
	CEdit m_editAngleZ;
	CEdit m_editTemp;
	CString m_sPreAngleX;
	CString m_sPreAngleY;
	CString m_sPreAngleZ;
	CString m_sPreTemp;
	CString m_sAngleX;
	CString m_sAngleY;
	CString m_sAngleZ;
	CString m_sTemp;

	void tempaging(DWORD dat);
	CString m_sTemptime;
	CEdit m_editTemptime;
	CEdit m_editGooffAtt;
	CString m_sGooffAtt;
	CButton m_cDcline;
	CButton m_cAngleCorr;
	CButton m_cGooff;
	int m_iBits;
	int m_iJigMode;
	afx_msg void OnNMCustomdrawProgress1(NMHDR* pNMHDR, LRESULT* pResult);
};
