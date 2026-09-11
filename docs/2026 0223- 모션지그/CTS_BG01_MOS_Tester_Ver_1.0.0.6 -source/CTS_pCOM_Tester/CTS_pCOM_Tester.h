
// CTS_pCOM_Tester.h : main header file for the PROJECT_NAME application
//

#pragma once

#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CCTS_pCOM_TesterApp:
// See CTS_pCOM_Tester.cpp for the implementation of this class
//

class CCTS_pCOM_TesterApp : public CWinApp
{
public:
	CCTS_pCOM_TesterApp();

	void SendDataToEditControl(CString SendData, CCommThread *pComThread);
	void SendDataToEditControl2(CString SendData, CCommThread *pComThread);
	void SendDataToEditControl3(CString SendData, CCommThread *pComThread);
	void SendDataToEditControlMaster(CString SendData, CCommThread *pComThread);
	void SendDataToEditControl4(CString SendData, CCommThread *pComThread);

	void ReadDataToEditControl(int DelayTime, CCommThread *pComThread);
	void ReadDataToEditControl2(int DelayTime, CCommThread *pComThread);
	void ReadDataToEditControl3(int DelayTime, CCommThread *pComThread);
	void ReadDataToEditControlMaster(int DelayTime, CCommThread *pComThread);
	void ReadDataToEditControl4(int DelayTime, CCommThread *pComThread);
	void fReOpen(void);
	void DisplayToStepData(CString ViewData, unsigned char ucFailIndex);
	void DisplayToStepData_Limit(CString ViewData, unsigned char ucFailIndex);
	void DisplayToStepData_Limit_Type2(CString ViewData, unsigned char ucFailIndex);
	void DisplayToStepData_Each(CString ViewData, unsigned char ucFailIndex,  int iboard_recognize_index, unsigned char uc_board_insert_flag);
	void DisplayToStepSequence( unsigned char ucStepIndex );
	void DisplayToStepScreenClear(void);
	void DisplayEMIOSendPacket(CString SendData);

	CCommThread m_ComuPort, m_ComuPort2, m_ComuPort3, m_ComuPortMaster, m_ComuPort4;
	CMySocket * m_MySocket;

	//Master/Slave Maint
	BYTE RcvBuff[65536];
	BYTE RcvBuffMaster[65536];
	//Master/Slave User
	BYTE RcvBuff2[65536];
	BYTE RcvBuff3[65536];
	BYTE RcvBuff4[65536];

	BYTE InputPort[8];
	BYTE OutputPort[8];

	CString App_RcvPort;


	CFont g_editFont_View_Step;


// Overrides
public:
	virtual BOOL InitInstance();

// Implementation

	DECLARE_MESSAGE_MAP()
};

extern CCTS_pCOM_TesterApp theApp;