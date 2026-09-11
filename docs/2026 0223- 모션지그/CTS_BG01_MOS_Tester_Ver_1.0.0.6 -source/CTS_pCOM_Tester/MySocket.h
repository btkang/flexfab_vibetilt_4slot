#if !defined(AFX_MYSOCKET_H__5F32119E_12AA_4208_8DFA_3CE764A25AE0__INCLUDED_)
#define AFX_MYSOCKET_H__5F32119E_12AA_4208_8DFA_3CE764A25AE0__INCLUDED_

#if _MSC_VER > 1000
#pragma once
#endif // _MSC_VER > 1000
// MySocket.h : header file
//


enum {
	MSO_CONNECT = 1,
	MSO_DISCONNECT = 2,
	MSO_RECEIVE = 3
};


/////////////////////////////////////////////////////////////////////////////
// CMySocket command target

class CMySocket : public CAsyncSocket
{
	// Attributes
public:
	HWND m_hParent;
	UINT m_uiMsgID;
	void SetParentPtr(HWND hParent, UINT uiMsgID) { m_hParent = hParent; m_uiMsgID = uiMsgID; }

	// Operations
public:
	CMySocket();
	virtual ~CMySocket();

	// Overrides
public:
	BOOL Connect(BYTE f0, BYTE f1, BYTE f2, BYTE f3, INT nport);

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CMySocket)
public:
	virtual void OnConnect(int nErrorCode);
	virtual void OnClose(int nErrorCode);
	virtual void OnReceive(int nErrorCode);
	//}}AFX_VIRTUAL

	// Generated message map functions
	//{{AFX_MSG(CMySocket)
	// NOTE - the ClassWizard will add and remove member functions here.
	//}}AFX_MSG


	// Implementation
protected:
};

/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

#endif // !defined(AFX_MYSOCKET_H__5F32119E_12AA_4208_8DFA_3CE764A25AE0__INCLUDED_)
