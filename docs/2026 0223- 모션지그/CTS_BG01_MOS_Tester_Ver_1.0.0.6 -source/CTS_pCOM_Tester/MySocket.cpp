// MySocket.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "MySocket.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CMySocket

CMySocket::CMySocket()
{
	m_hParent = NULL;
	m_uiMsgID = 0;
}

CMySocket::~CMySocket()
{
}


// Do not edit the following lines, which are needed by ClassWizard.
#if 0
BEGIN_MESSAGE_MAP(CMySocket, CAsyncSocket)
	//{{AFX_MSG_MAP(CMySocket)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()
#endif	// 0

/////////////////////////////////////////////////////////////////////////////
// CMySocket member functions

void CMySocket::OnConnect(int nErrorCode)
{
	// TODO: Add your specialized code here and/or call the base class
	if (m_hParent) SendMessage(m_hParent, m_uiMsgID, MSO_CONNECT, 0);

	CAsyncSocket::OnConnect(nErrorCode);
}

void CMySocket::OnClose(int nErrorCode)
{
	// TODO: Add your specialized code here and/or call the base class
	if (m_hParent) SendMessage(m_hParent, m_uiMsgID, MSO_DISCONNECT, 0);

	CAsyncSocket::OnClose(nErrorCode);
}

void CMySocket::OnReceive(int nErrorCode)
{
	// TODO: Add your specialized code here and/or call the base class
	if (m_hParent) SendMessage(m_hParent, m_uiMsgID, MSO_RECEIVE, 0);

	CAsyncSocket::OnReceive(nErrorCode);
}


BOOL CMySocket::Connect(BYTE f0, BYTE f1, BYTE f2, BYTE f3, INT nport)
{
	CString cStr;
	cStr.Format(_T("%d.%d.%d.%d"), f0, f1, f2, f3);

	return CAsyncSocket::Connect((LPCTSTR)cStr, (UINT)nport);
}
