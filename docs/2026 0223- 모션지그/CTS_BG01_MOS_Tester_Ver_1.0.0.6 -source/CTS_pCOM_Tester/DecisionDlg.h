#pragma once
#include "afxwin.h"

// CDecisionDlg dialog

class CDecisionDlg : public CDialogEx
{
	DECLARE_DYNAMIC(CDecisionDlg)

public:
	CDecisionDlg(CWnd* pParent = NULL);   // standard constructor
	virtual ~CDecisionDlg();

// Dialog Data
	enum { IDD = IDD_DECISION };

	CBitmap *m_pBitEnd, *m_pBitFail;

protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

	DECLARE_MESSAGE_MAP()

public:
	CStatic m_wndDecisionScreen;
	void SetDecision(bool bEnd);
	void AutoHide(int Delay);
	virtual BOOL OnInitDialog();
	afx_msg void OnTimer(UINT_PTR nIDEvent);
};
