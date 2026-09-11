#pragma once
#include "afxwin.h"
#include "DecisionDlg.h"
#include "afxcmn.h"

// CSetProduct dialog

class CSetProduct : public CDialogEx
{
private:
	HANDLE m_hThreadFinishEvent;

	DECLARE_DYNAMIC(CSetProduct)

public:
	CSetProduct(CWnd* pParent = NULL);   // standard constructor
	virtual ~CSetProduct();

	CDecisionDlg	*m_pDlgDecision;

// Dialog Data
	enum { IDD = IDD_SET_PRODUCT };

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()
public:
	CWinThread *m_pThread;
	enum ThreadWorkingType
	{
		THREAD_STOP = 0,
		THREAD_RUNNING,
		THREAD_PAUSE,
	};
	ThreadWorkingType m_eThreadWork;

	CButton m_cConfClr;
	CButton m_cReset;
	CButton m_cConfChk;
	CButton m_cVerChk;
	CButton m_BtnConfClr;
	CButton m_BtnReset;
	CButton m_BtnConfChk;
	CButton m_BtnVerChk;
	CButton m_BtnAutoStart;
	CButton m_BtnAutoStop;

	CButton m_cConfSave;
	CButton m_cCorrectVolt;
	CButton m_cOffsetRssi;

	BOOL m_bThreadStatus;
	BOOL m_bEnterTheTest;
	int m_iTestStep;
	int m_iTestResult;
	CString RcvStr;
	CString CompStr;

	afx_msg void OnBnClickedConfigClear();
	afx_msg void OnBnClickedReset();
	afx_msg void OnBnClickedConfigCheck();
	afx_msg void OnBnClickedVersionCheck();
	afx_msg void OnBnClickedAutoStart();
	afx_msg void OnBnClickedAutoStop();
	afx_msg void OnBnClickedButton7();
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);

	virtual BOOL PreTranslateMessage(MSG* pMsg);
	virtual BOOL OnInitDialog();

	void SendData_to_test(CString SendCmd);
	void SendData_to_ref(CString SendCmd);
	void SendData_to_supply(CString SendCmd);

	void ReadData_to_test(int DelayTime);
	void ReadData_to_ref(int DelayTime);
	void ReadData_to_supply(int DelayTime);

	void Rfid(CString str);
	void IncreaseRfid();
	void SaveToDB(CString saveStr, CString Which);
	BYTE DataSearch(CString Which);
	void entertheconsole();

	void VerChkInThread();
	void TestResult(CString msg);
	CString ConverToHex(CString data);
	LRESULT OnUpdateData(WPARAM wParam, LPARAM lParam);

	CEdit m_EditRcvStatSetProduct;

	void ConfigChk();
	CEdit m_cCompVersion;
	CString m_sCompVersion;
	CEdit m_EditRfid;
	CString m_sRfid;
	CButton m_cRfid;

	void VoltageInit(int which, int data);
	void VoltageRenewal(int which, int data);
	void VoltageChg(int data);
	void VoltageMinInit(int which, int data);

	void MeasureRssi(int which, int data);
	void ChkRssi(int which);

	void ConfigSave();

	int RcvMsgCnt(BYTE* msg);

	int GetFindCharCount(CString param_string, char param_find_char);

	CString m_sFixedVoltage;
	CString m_sFloatingRssi;
	CString m_sFixedAddress;
	CString m_sEsenable;
	CString m_sEsChangeenable;
	CString m_sTxstop;
	CString m_sIolog;
	CString m_sTriggerenable;

	void ConfigmChk();
	void modesel(int which, int val);
	void PortControl(CString cmd);
	CProgressCtrl m_progress;

	void processdelay(DWORD dat);
	int m_rdoEs;
	int m_rdoEce;
	int m_rdoTse;
	int m_rdoIl;
	int m_rdoTrm;

	void esenable(int dat);
	void eschangeenable(int dat);
	void txstopenable(int dat);
	void iolog(int dat);
	void triggermode(int dat);
	CButton m_cFlotingRssi;
};
