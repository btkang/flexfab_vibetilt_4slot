// DecisionDlg.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "DecisionDlg.h"
#include "afxdialogex.h"


// CDecisionDlg dialog

IMPLEMENT_DYNAMIC(CDecisionDlg, CDialogEx)

CDecisionDlg::CDecisionDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(CDecisionDlg::IDD, pParent)
{
	m_pBitEnd = NULL;
	m_pBitFail = NULL;
}

CDecisionDlg::~CDecisionDlg()
{
	if (m_pBitEnd)
		delete m_pBitEnd;

	if (m_pBitFail)
		delete m_pBitFail;
}

void CDecisionDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_DECISION_SCREEN, m_wndDecisionScreen);
}


BEGIN_MESSAGE_MAP(CDecisionDlg, CDialogEx)
	ON_WM_TIMER()
END_MESSAGE_MAP()

void CDecisionDlg::SetDecision(bool bEnd)
{
	if (bEnd){
		m_wndDecisionScreen.SetBitmap(*m_pBitEnd);
	}
	else{
		m_wndDecisionScreen.SetBitmap(*m_pBitFail);
	}
}


void CDecisionDlg::AutoHide(int Delay)
{
	SetTimer(0, Delay, NULL);
}


void CDecisionDlg::OnTimer(UINT_PTR nIDEvent)
{
	this->ShowWindow(SW_HIDE);

	CDialogEx::OnTimer(nIDEvent);
}


BOOL CDecisionDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	m_pBitEnd = new CBitmap();
	m_pBitFail = new CBitmap();
	VERIFY(m_pBitEnd);
	VERIFY(m_pBitFail);

	m_pBitEnd->LoadBitmap(IDB_END);
	//m_pBitEnd->LoadBitmap(IDB_PASS);
	m_pBitFail->LoadBitmap(IDB_FAIL);

	return TRUE;  // return TRUE unless you set the focus to a control
	// EXCEPTION: OCX Property Pages should return FALSE
}

// CDecisionDlg message handlers
