#pragma once
#include "afxwin.h"
#include "DecisionDlg.h"
#include "afxcmn.h"

// CBoardLevel dialog

class CBoardLevel : public CDialogEx
{
private:
	HANDLE m_hThreadFinishEvent;

	DECLARE_DYNAMIC(CBoardLevel)

public:
	CBoardLevel(CWnd* pParent = NULL);   // standard constructor
	virtual ~CBoardLevel();

	CDecisionDlg	*m_pDlgDecision;

// Dialog Data
	enum { IDD = IDD_BOARD_LEVEL };

protected:
//	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	CWinThread *m_pThread;
	CWinThread *m_pProgressThread;
	enum ThreadWorkingType
	{
		THREAD_STOP = 0,
		THREAD_RUNNING,
		THREAD_PAUSE,
	};
	ThreadWorkingType m_eThreadWork;

	afx_msg void OnBnClickedAutoStart();
	afx_msg void OnBnClickedAutoStop();
	afx_msg void OnBnClickedUserSerial();
	afx_msg void OnBnClickedLedon();
	afx_msg void OnBnClickedLedoff();
	afx_msg void OnBnClickedIoTest();
	afx_msg void OnBnClickedFlashTest();
	afx_msg void OnBnClickedRssiVoltage();
	afx_msg void OnBnClickedSram();
	afx_msg void OnBnClickedPlcIc();
	afx_msg void OnBnClickedMpu6050();
	afx_msg void OnBnClicked2gfullPay();
	afx_msg void OnBnClickedInputPort();
	afx_msg void OnBnClicked2gfullPayTx();
	afx_msg void OnBnClickedInputPortTx();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);

	void SendData(CString SendCmd);
	void ReadData(int DelayTime);
	void SendDataMaster(CString SendCmd);
	void ReadDataMaster(int DelayTime);
	void SendDataUser(CString SendCmd);
	void ReadDataUser(int DelayTime);
	virtual void DoDataExchange(CDataExchange* pDX);
	virtual BOOL OnInitDialog();
	void TestResult(CString msg);
	void RfidSetTarget();
	void RfidSetMaster();
	void Thread2gfullPayRx();
	void Thread2gfullPayTx();
	CString ConverToHex(CString data);

	CButton m_cUserSerial;
	CButton m_cLedon;
	CButton m_cLedoff;
	CButton m_cIoTest;
	CButton m_cFlashTest;
	CButton m_c2gFullPayRx;
	CButton m_c2gFullPayTx;
	CButton m_cRssiVolt;
	CButton m_cPlcIc;
	CButton m_cMpu;
	CButton m_cInputPort;

	CButton m_BtnLedon;
	CButton m_BtnLedoff;
	CButton m_BtnIoTest;
	CButton m_BtnFlashTest;
	CButton m_BtnAutoStart;
	CButton m_BtnAutoStop;
	CButton m_Btn2gFullPayRx;
	CButton m_Btn2gFullPayTx;
	CButton m_BtnUserSerial;
	CButton m_BtnRssiVolt;
	CButton m_BtnSram;
	CButton m_BtnPlcIc;
	CButton m_BtnMpu6050;
	CButton m_BtnInputPort;

	BOOL m_bEnterTheTest;
	BOOL m_bThreadStatus;

	CString RcvStr;
	CString RcvStrMaster;
	CEdit m_EditDataMonitor;

	HBITMAP hbit;

	int m_iTestStep;
	int m_iTestResult;
	CEdit m_cTestRfid;
	CString m_sTestRfid;
	CProgressCtrl m_progress;
	CButton m_cInFlashTest;
	afx_msg void OnBnClickedInflashTest();
	CButton m_BtnInflashTest;
	CButton m_cSramTest;

	CButton m_cPlc10mhz;
	CButton m_cPlc13mhz;
	CButton m_BtnPlc10mhz;
	CButton m_BtnPlc13mhz;
	afx_msg void OnBnClickedPlc10mhz();
	afx_msg void OnBnClickedPlc13mhz();
	void processdelay(DWORD dat);
	CButton m_btn32bytePayload;
	afx_msg void OnBnClickedManualPayload();
	BOOL m_b8bits;
	CButton m_chk8bits;

	void bitcontrol(int which, int dat);

	CString m_sComparestr;
};
