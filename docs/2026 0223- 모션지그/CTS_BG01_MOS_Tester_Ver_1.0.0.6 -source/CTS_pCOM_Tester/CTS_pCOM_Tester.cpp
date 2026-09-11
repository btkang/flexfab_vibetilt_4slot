
// CTS_pCOM_Tester.cpp : Defines the class behaviors for the application.
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "CTS_pCOM_TesterDlg.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CCTS_pCOM_TesterApp

BEGIN_MESSAGE_MAP(CCTS_pCOM_TesterApp, CWinApp)
	ON_COMMAND(ID_HELP, &CWinApp::OnHelp)
END_MESSAGE_MAP()


// CCTS_pCOM_TesterApp construction

CCTS_pCOM_TesterApp::CCTS_pCOM_TesterApp()
{
	// support Restart Manager
	m_dwRestartManagerSupportFlags = AFX_RESTART_MANAGER_SUPPORT_RESTART;

	// TODO: add construction code here,
	// Place all significant initialization in InitInstance

	m_ComuPort.SetThreadId(1);
	m_ComuPort2.SetThreadId(2);
	m_ComuPortMaster.SetThreadId(3);
	m_ComuPort3.SetThreadId(4);
}


// The one and only CCTS_pCOM_TesterApp object

CCTS_pCOM_TesterApp theApp;


// CCTS_pCOM_TesterApp initialization

BOOL CCTS_pCOM_TesterApp::InitInstance()
{
	// InitCommonControlsEx() is required on Windows XP if an application
	// manifest specifies use of ComCtl32.dll version 6 or later to enable
	// visual styles.  Otherwise, any window creation will fail.
	INITCOMMONCONTROLSEX InitCtrls;
	InitCtrls.dwSize = sizeof(InitCtrls);
	// Set this to include all the common control classes you want to use
	// in your application.
	InitCtrls.dwICC = ICC_WIN95_CLASSES;
	InitCommonControlsEx(&InitCtrls);

	CWinApp::InitInstance();


	AfxEnableControlContainer();

	// Create the shell manager, in case the dialog contains
	// any shell tree view or shell list view controls.
	CShellManager *pShellManager = new CShellManager;

	// Activate "Windows Native" visual manager for enabling themes in MFC controls
	CMFCVisualManager::SetDefaultManager(RUNTIME_CLASS(CMFCVisualManagerWindows));

	// Standard initialization
	// If you are not using these features and wish to reduce the size
	// of your final executable, you should remove from the following
	// the specific initialization routines you do not need
	// Change the registry key under which our settings are stored
	// TODO: You should modify this string to be something appropriate
	// such as the name of your company or organization

	SetRegistryKey(_T("Local AppWizard-Generated Applications"));

	CCTS_pCOM_TesterDlg dlg;
	m_pMainWnd = &dlg;
	INT_PTR nResponse = dlg.DoModal();
	if (nResponse == IDOK)
	{
		// TODO: Place code here to handle when the dialog is
		//  dismissed with OK
	}
	else if (nResponse == IDCANCEL)
	{
		// TODO: Place code here to handle when the dialog is
		//  dismissed with Cancel
	}
	else if (nResponse == -1)
	{
		TRACE(traceAppMsg, 0, "Warning: dialog creation failed, so application is terminating unexpectedly.\n");
		TRACE(traceAppMsg, 0, "Warning: if you are using MFC controls on the dialog, you cannot #define _AFX_NO_MFC_CONTROLS_IN_DIALOGS.\n");
	}

	// Delete the shell manager created above.
	if (pShellManager != NULL)
	{
		delete pShellManager;
	}

	if (!AfxSocketInit())
	{
		AfxMessageBox(_T("AfxSocketInit È£Ãâ¿¡ ½ÇÆÐ ÇÏ¿´½À´Ï´Ù"));
		return FALSE;
	}
	// Since the dialog has been closed, return FALSE so that we exit the
	//  application, rather than start the application's message pump.
	return FALSE;
}

void CCTS_pCOM_TesterApp::SendDataToEditControl(CString SendData, CCommThread *pComThread)
{
	int nSize = SendData.GetLength() + 4;
	BYTE *Send_buff;

	CString strData;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	Send_buff = new BYTE[nSize];
	sprintf_s((char*)Send_buff, nSize, "%s", LPCTSTR(SendData));
	pComThread->WriteComm(Send_buff, nSize - 4);

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	if (lLen > 20000) pMainDlg->m_EditReceiveData.SetWindowText(_T(""));



	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData  +" [Snd Target]>") + SendData);
	delete[] Send_buff;
}


void CCTS_pCOM_TesterApp::DisplayToStepSequence(unsigned char ucStepIndex)
{
	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();
	pMainDlg->StepSequenceDisplay(ucStepIndex);

}

void CCTS_pCOM_TesterApp::DisplayEMIOSendPacket(CString SendData)
{
	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();
	pMainDlg->EMIOSendPacketDisplay(SendData);

}

void CCTS_pCOM_TesterApp::DisplayToStepScreenClear(void)
{
	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();
	pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization
}
// 

void CCTS_pCOM_TesterApp::DisplayToStepData_Each(CString ViewData, unsigned char ucFailIndex, int iboard_recognize_index, unsigned char uc_board_insert_flag)
{
	CString _Fail_Param_t[15][4] =
	{
		{ "0", "JIG_INIT",				"Communication Fail"},
		{ "1", "OPEN",					"Communication Fail"},
		{ "2", "DEGREE_0_CHECK",		"Communication Fail"},
		{ "3", "ANGLE_p_10_5_0",		"Move Fail"},
		{ "4", "MEASURE_ANGLE_p_10_5",	"Fail"},
		{ "5", "ANGLE_m_10_5_0",		"Move Fail"},
		{ "6", "ANGLE_INIT_0",			"Move Fail"},
		{ "7", "BG01_INIT",				"Communication Fail"},
		{ "8", "ANGLE_m_10_5_1",		"Move Fail"},
		{ "9", "MEASURE_ANGLE_m_10_5",	"Fail"},
		{ "10", "ANGLE_p_10_5_1",		"Move Fail"},
		{ "11", "ANGLE_INIT_1",			"Move Fail"},
		{ "12", "END",					"Communication Fail"},

	};


	CString _fail_board_index_t[5] =
	{
		{"X"},
		{"Y"},
		{"Z"},
		{" "},
	};


	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	unsigned char ucboard_fail_index_temp_x = 3;
	unsigned char ucboard_fail_index_temp_y = 3;
	unsigned char ucboard_fail_index_temp_z = 3;


	//############################################################################################
	//############################################################################################
	if (iboard_recognize_index & 0x01) { if (uc_board_insert_flag & 0x01) {} else { ucboard_fail_index_temp_x = 0; } }
	if (iboard_recognize_index & 0x02) { if (uc_board_insert_flag & 0x02) {} else { ucboard_fail_index_temp_y = 1; } }
	if (iboard_recognize_index & 0x04) { if (uc_board_insert_flag & 0x04) {} else { ucboard_fail_index_temp_z = 2; } }

	//############################################################################################
	//############################################################################################

	//strBufferTemp = (_T("\r\n" + strData + " Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);
	strBufferTemp = (_T("\r\n" + strData + " Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2] + " Axis " + _fail_board_index_t[ucboard_fail_index_temp_x] + " " + _fail_board_index_t[ucboard_fail_index_temp_y] + " " + _fail_board_index_t[ucboard_fail_index_temp_z] );


	int lLen = pMainDlg->m_Edit_Receive_View_Step_Data.GetWindowTextLength();
	
	pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization

	pMainDlg->m_Edit_Receive_View_Step_Data.SetSel(lLen, lLen);
	pMainDlg->m_Edit_Receive_View_Step_Data.ReplaceSel(_T(" Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2] + " Axis " + _fail_board_index_t[ucboard_fail_index_temp_x] + " " + _fail_board_index_t[ucboard_fail_index_temp_y] + " " + _fail_board_index_t[ucboard_fail_index_temp_z] );


	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) {
	}
	else {
		pMainDlg->m_p_Log_File_Save.Write((strBufferTemp), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}
}

void CCTS_pCOM_TesterApp::DisplayToStepData_Limit(CString ViewData, unsigned char ucFailIndex)
{
	CString _Fail_Param_t[15][4] =
	{
		{ "0", "JIG_INIT",				"Limit Sensor Fail"},
		{ "1", "OPEN",					"Communication Fail"},
		{ "2", "DEGREE_0_CHECK",		"Communication Fail"},
		{ "3", "ANGLE_p_10_5_0",		"Move Fail"},
		{ "4", "MEASURE_ANGLE_p_10_5",	"Measurement Fail"},
		{ "5", "ANGLE_m_10_5_0",		"Move Fail"},
		{ "6", "ANGLE_INIT_0",			"Move Fail"},
		{ "7", "BG01_INIT",				"Communication Fail"},
		{ "8", "ANGLE_m_10_5_1",		"Move Fail"},
		{ "9", "MEASURE_ANGLE_m_10_5",	"Measurement Fail"},
		{ "10", "ANGLE_p_10_5_1",		"Move Fail"},
		{ "11", "ANGLE_INIT_1",			"Move Fail"},
		{ "12", "END",					"Communication Fail"},

	};


	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	strBufferTemp = (_T("\r\n" + strData + " Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);


	// Font Setting 
	//pMainDlg->m_editFont_View_Step_R.CreatePointFont(220, TEXT("±¼¸²"));
	//pMainDlg->m_Edit_Receive_View_Step_Data.SetFont(&pMainDlg->m_editFont_View_Step_R, TRUE);

	int lLen = pMainDlg->m_Edit_Receive_View_Step_Data.GetWindowTextLength();
	//if (lLen > 20000) pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization
	pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization

	pMainDlg->m_Edit_Receive_View_Step_Data.SetSel(lLen, lLen);
	pMainDlg->m_Edit_Receive_View_Step_Data.ReplaceSel(_T(" Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);


	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) {
	}
	else {
		pMainDlg->m_p_Log_File_Save.Write((strBufferTemp), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}
}
void CCTS_pCOM_TesterApp::DisplayToStepData_Limit_Type2(CString ViewData, unsigned char ucFailIndex)
{
	CString _Fail_Param_t[15][4] =
	{
		{ "0", "JIG_INIT",				"Origin Position Fail"},
		{ "1", "OPEN",					"Communication Fail"},
		{ "2", "DEGREE_0_CHECK",		"Communication Fail"},
		{ "3", "ANGLE_p_10_5_0",		"Move Fail"},
		{ "4", "MEASURE_ANGLE_p_10_5",	"Measurement Fail"},
		{ "5", "ANGLE_m_10_5_0",		"Move Fail"},
		{ "6", "ANGLE_INIT_0",			"Move Fail"},
		{ "7", "BG01_INIT",				"Communication Fail"},
		{ "8", "ANGLE_m_10_5_1",		"Move Fail"},
		{ "9", "MEASURE_ANGLE_m_10_5",	"Measurement Fail"},
		{ "10", "ANGLE_p_10_5_1",		"Move Fail"},
		{ "11", "ANGLE_INIT_1",			"Move Fail"},
		{ "12", "END",					"Communication Fail"},

	};


	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	strBufferTemp = (_T("\r\n" + strData + " Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);


	// Font Setting 
	//pMainDlg->m_editFont_View_Step_R.CreatePointFont(220, TEXT("±¼¸²"));
	//pMainDlg->m_Edit_Receive_View_Step_Data.SetFont(&pMainDlg->m_editFont_View_Step_R, TRUE);

	int lLen = pMainDlg->m_Edit_Receive_View_Step_Data.GetWindowTextLength();
	//if (lLen > 20000) pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization
	pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization

	pMainDlg->m_Edit_Receive_View_Step_Data.SetSel(lLen, lLen);
	pMainDlg->m_Edit_Receive_View_Step_Data.ReplaceSel(_T(" Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);


	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) {
	}
	else {
		pMainDlg->m_p_Log_File_Save.Write((strBufferTemp), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}
}
void CCTS_pCOM_TesterApp::DisplayToStepData(CString ViewData , unsigned char ucFailIndex)
{
	CString _Fail_Param_t[15][4] =
	{
		{ "0", "JIG_INIT",				"Communication Fail"},
		{ "1", "OPEN",					"Communication Fail"},
		{ "2", "DEGREE_0_CHECK",		"Communication Fail"},
		{ "3", "ANGLE_p_10_5_0",		"Move Fail"},
		{ "4", "MEASURE_ANGLE_p_10_5",	"Measurement Fail"},
		{ "5", "ANGLE_m_10_5_0",		"Move Fail"},
		{ "6", "ANGLE_INIT_0",			"Move Fail"},
		{ "7", "BG01_INIT",				"Communication Fail"},
		{ "8", "ANGLE_m_10_5_1",		"Move Fail"},
		{ "9", "MEASURE_ANGLE_m_10_5",	"Measurement Fail"},
		{ "10", "ANGLE_p_10_5_1",		"Move Fail"},
		{ "11", "ANGLE_INIT_1",			"Move Fail"},
		{ "12", "END",					"Communication Fail"},

	};


	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	strBufferTemp = (_T("\r\n" + strData + " Step ") + ViewData + " " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);
	

	// Font Setting 
	//pMainDlg->m_editFont_View_Step_R.CreatePointFont(220, TEXT("±¼¸²"));
	//pMainDlg->m_Edit_Receive_View_Step_Data.SetFont(&pMainDlg->m_editFont_View_Step_R, TRUE);

	int lLen = pMainDlg->m_Edit_Receive_View_Step_Data.GetWindowTextLength();
	//if (lLen > 20000) pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization
	pMainDlg->m_Edit_Receive_View_Step_Data.SetWindowText(_T("")); // Initialization

	pMainDlg->m_Edit_Receive_View_Step_Data.SetSel(lLen, lLen);
	pMainDlg->m_Edit_Receive_View_Step_Data.ReplaceSel(_T(" Step ") + ViewData +" " + _Fail_Param_t[ucFailIndex][1] + " " + _Fail_Param_t[ucFailIndex][2]);
	 
	
	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) {
	}else{
		pMainDlg->m_p_Log_File_Save.Write((strBufferTemp), strBufferTemp.GetLength());
	 	ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
	 	if (fileLength > 1000000) {
	 		pMainDlg->m_p_Log_File_Save.Close();
	 		fReOpen();
	 	}
	 }
}

void CCTS_pCOM_TesterApp::SendDataToEditControlMaster(CString SendData, CCommThread *pComThread)
{
	int nSize = SendData.GetLength() + 4;
	BYTE *Send_buff;


	CString strData;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	Send_buff = new BYTE[nSize];
	sprintf_s((char*)Send_buff, nSize, "%s", LPCTSTR(SendData));
	pComThread->WriteComm(Send_buff, nSize - 4);

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	if (lLen > 20000) pMainDlg->m_EditReceiveData.SetWindowText(_T(""));
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData + " [Snd Ref.]>") + SendData);
	delete[] Send_buff;
}

void CCTS_pCOM_TesterApp::SendDataToEditControl2(CString SendData, CCommThread *pComThread)
{
	int nSize = SendData.GetLength() + 4;
	BYTE *Send_buff;

	Send_buff = new BYTE[nSize];
	sprintf_s((char*)Send_buff, nSize, "%s", LPCTSTR(SendData));
	pComThread->WriteComm(Send_buff, nSize - 4);

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	if (lLen > 20000) pMainDlg->m_EditReceiveData.SetWindowText(_T(""));
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n[Snd U.M]>") + SendData);
	delete[] Send_buff;
}

void CCTS_pCOM_TesterApp::SendDataToEditControl3(CString SendData, CCommThread *pComThread)
{
	int nSize = SendData.GetLength() + 4;
	BYTE *Send_buff;

	CString strData;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	Send_buff = new BYTE[nSize];
	sprintf_s((char*)Send_buff, nSize, "%s", LPCTSTR(SendData));
	int i = pComThread->WriteComm(Send_buff, nSize - 4);

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	if (lLen > 20000) pMainDlg->m_EditReceiveData.SetWindowText(_T(""));
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData + " [Snd U.T]>") + SendData);
	delete[] Send_buff;
}

void CCTS_pCOM_TesterApp::SendDataToEditControl4(CString SendData, CCommThread *pComThread)
{
	int nSize = SendData.GetLength() + 4;
	BYTE *Send_buff;

	Send_buff = new BYTE[nSize];
	sprintf_s((char*)Send_buff, nSize, "%s", LPCTSTR(SendData));
	pComThread->WriteComm(Send_buff, nSize - 4);

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	if (lLen > 20000) pMainDlg->m_EditReceiveData.SetWindowText(_T(""));
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n[Snd Supp]>") + SendData);
	delete[] Send_buff;
}

void CCTS_pCOM_TesterApp::ReadDataToEditControl(int DelayTime, CCommThread *pComThread)
{
	Sleep(DelayTime);

	memset(&RcvBuff, 0, sizeof(RcvBuff));

	pComThread->ReadComm(RcvBuff, sizeof(RcvBuff));

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData+ " [Rcv Target]>") + (CString)RcvBuff);
	strBufferTemp = (_T("\r\n" + strData + " [Rcv Target]>") + (CString)RcvBuff);
	
	
	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) { // Close
	}else{
		pMainDlg->m_p_Log_File_Save.Write((_T("\r\n" + strData + " [Rcv Target]>") + (CString)RcvBuff), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}
}
void CCTS_pCOM_TesterApp::fReOpen(void)
{
	CString LogfileName;
	CTime cTimeTSave = CTime::GetCurrentTime();
	CCTS_pCOM_TesterDlg* pMainDlg = (CCTS_pCOM_TesterDlg*)GetMainWnd();
	// File Name  
	LogfileName = "BG01_Log_";
	LogfileName += cTimeTSave.Format(_T("_%Y-%m-%d_%HH_%MM_%SS"));
	LogfileName += ".txt";

	// File Open CFile 
	if (!(pMainDlg->m_p_Log_File_Save.Open(LogfileName, CFile::modeCreate | CFile::modeReadWrite | CFile::shareDenyNone)))
	{		
	}
}
void CCTS_pCOM_TesterApp::ReadDataToEditControl2(int DelayTime, CCommThread *pComThread)
{
	Sleep(DelayTime);

	memset(&RcvBuff2, 0, sizeof(RcvBuff2));

	pComThread->ReadComm(RcvBuff2, sizeof(RcvBuff2));

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData +" [Rcv U.M]>") + (CString)RcvBuff2);

	strBufferTemp = (_T("\r\n" + strData + " [Rcv U.M]>") + (CString)RcvBuff2);

	
	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) { // Close
	}else{
		pMainDlg->m_p_Log_File_Save.Write((_T("\r\n" + strData + " [Rcv U.M]>") + (CString)RcvBuff2), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}
}

void CCTS_pCOM_TesterApp::ReadDataToEditControl3(int DelayTime, CCommThread *pComThread)
{
	Sleep(DelayTime);

	memset(&RcvBuff3, 0, sizeof(RcvBuff3));

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	pComThread->ReadComm(RcvBuff3, sizeof(RcvBuff3));

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData + " [Rcv U.T]>") + (CString)RcvBuff3);

	strBufferTemp = (_T("\r\n" + strData + " [Rcv U.T]>") + (CString)RcvBuff3);

	
	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save)
	{

	}
	else {
		pMainDlg->m_p_Log_File_Save.Write((_T("\r\n" + strData + " [Rcv U.T]>") + (CString)RcvBuff3), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}

}

void CCTS_pCOM_TesterApp::ReadDataToEditControlMaster(int DelayTime, CCommThread *pComThread)
{
	Sleep(DelayTime);

	memset(&RcvBuffMaster, 0, sizeof(RcvBuffMaster));

	pComThread->ReadComm(RcvBuffMaster, sizeof(RcvBuffMaster));

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	CString strData;
	CString strBufferTemp;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n" + strData + " [Rcv Master]>") + (CString)RcvBuffMaster);

	strBufferTemp = (_T("\r\n" + strData + " [Rcv Master]>") + (CString)RcvBuffMaster);


	
	if (INVALID_HANDLE_VALUE == (HANDLE)pMainDlg->m_p_Log_File_Save) {
	}else{
		pMainDlg->m_p_Log_File_Save.Write((_T("\r\n" + strData + " [Rcv Master]>") + (CString)RcvBuffMaster), strBufferTemp.GetLength());
		ULONGLONG  fileLength = pMainDlg->m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			pMainDlg->m_p_Log_File_Save.Close();
			fReOpen();
		}
	}
}

void CCTS_pCOM_TesterApp::ReadDataToEditControl4(int DelayTime, CCommThread *pComThread)
{
	Sleep(DelayTime);

	memset(&RcvBuff4, 0, sizeof(RcvBuff4));

	pComThread->ReadComm(RcvBuff4, sizeof(RcvBuff4));

	CCTS_pCOM_TesterDlg *pMainDlg = (CCTS_pCOM_TesterDlg *)GetMainWnd();

	int lLen = pMainDlg->m_EditReceiveData.GetWindowTextLength();
	pMainDlg->m_EditReceiveData.SetSel(lLen, lLen);
	pMainDlg->m_EditReceiveData.ReplaceSel(_T("\r\n[Rcv Supp]>") + (CString)RcvBuff4);
}