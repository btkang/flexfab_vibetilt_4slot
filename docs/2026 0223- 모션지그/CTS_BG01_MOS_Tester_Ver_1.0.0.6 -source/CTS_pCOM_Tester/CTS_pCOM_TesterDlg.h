
// CTS_pCOM_TesterDlg.h : header file
//

#pragma once
#include "afxwin.h"
#include "afxcmn.h"
#include "BoardLevel.h"
#include "BoardLevelMo.h"
#include "BoardLevelMo_BG01.h"
#include "Commtest.h"
#include "SetProduct.h"

// CCTS_pCOM_TesterDlg dialog
class CCTS_pCOM_TesterDlg : public CDialogEx
{
private:
	BYTE byIndexData(int xData);//데이터 비트 받기
	BYTE byIndexStop(int xStop);// 스톱비트 받기 
	BYTE byIndexParity(int xParity);// 펠리티 받기
	DWORD byIndexBaud(int xBaud);// 전송률 받기

	HANDLE m_hPortThread;
// Construction
public:
	CCTS_pCOM_TesterDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_CTS_pCOM_TESTER_DIALOG };
	CBoardLevelMo		m_BoardLevelMotion;
	CBoardLevel			m_BoardLevel;	
	CCommtest			m_Commtest;
	CSetProduct			m_SetProduct;
	CWnd*				m_pwndShow;
	CBoardLevelMoBG01	m_BoardLevelMotion_BG01;

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	virtual BOOL PreTranslateMessage(MSG* pMsg);

// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()

public:
	afx_msg	LRESULT OnSocketMsg(WPARAM wParam, LPARAM lParam);
	afx_msg void OnBnClickedSerialOpen();
	afx_msg void OnBnClickedSerialClose();
	afx_msg void OnBnClickedEthernetConnect();
	afx_msg void OnBnClickedEthernetDisconnect();
	afx_msg void OnTcnSelchangeTab1(NMHDR *pNMHDR, LRESULT *pResult);
	afx_msg void OnBnClickedButton5();
	afx_msg void OnBnClickedSerialOpen2();
	afx_msg void OnBnClickedSerialClose2();
	afx_msg void OnBnClickedEthernetSend();

	int SearchPort();
	void OnRcv();

	CEdit m_EditReceiveData;
	CEdit m_Edit_Receive_View_Step_Data;
	CFont m_editFont_View_Step_R;
	CFile m_p_Log_File_Save;

	CComboBox m_cSerialPort;
	CComboBox m_cBaudRate;
	CComboBox m_cParity;
	CComboBox m_cSerialPort2;
	CComboBox m_cBaudRate2;
	CComboBox m_cParity2;
	CComboBox m_cSerialPortMaster;

	int m_iSerialPort;
	int m_iBaudRate;
	int m_iParity;
	int m_iSerialPort2;
	int m_iBaudRate2;
	int m_iParity2;
	int m_iSerialPortMaster;
	
	CIPAddressCtrl m_cIPAddr;
	CTabCtrl m_Tab;
	UINT	m_nPort;
	CEdit m_cEhernetCmd;
	CButton m_BtnEhternetSend;
	CComboBox m_cSerialPort3;
	int m_iSerialPort3;

	BOOL m_bPortThread;
	void PortControl(CString cmd);
	CComboBox m_cSerialPort4;
	//int m_iSerialPort4;
	afx_msg void OnBnClickedSerialOpen3();
	afx_msg void OnBnClickedSerialClose3();
	virtual BOOL DestroyWindow();

	void StepSequenceDisplay(unsigned char StepIndex);
	void EMIOSendPacketDisplay(CString SendData);
	
//	int m_Display_Step;
	afx_msg HBRUSH OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor);
};
