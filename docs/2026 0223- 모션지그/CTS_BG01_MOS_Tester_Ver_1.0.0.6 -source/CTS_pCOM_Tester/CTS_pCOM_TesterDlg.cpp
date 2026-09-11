
// CTS_pCOM_TesterDlg.cpp : implementation file
//

#include "stdafx.h"
#include "CTS_pCOM_Tester.h"
#include "CTS_pCOM_TesterDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

#define	STX		0x02
#define	ETX		0x03

#define HIMETRIC_INCH        2540

HWND hCommWnd;
CCTS_pCOM_TesterApp *g_pApp_dlg = (CCTS_pCOM_TesterApp *)AfxGetApp();
// CAboutDlg dialog used for App About

void WSAStart()
{
	WORD wVersionRequested;
	WSADATA wsaData;
	int err;

	wVersionRequested = MAKEWORD(2, 2);

	err = WSAStartup(wVersionRequested, &wsaData);
	if (err != 0) {
		/* Tell the user that we could not find a usable */
		/* WinSock DLL.                                  */
		return;
	}

	/* Confirm that the WinSock DLL supports 2.2.*/
	/* Note that if the DLL supports versions greater    */
	/* than 2.2 in addition to 2.2, it will still return */
	/* 2.2 in wVersion since that is the version we      */
	/* requested.                                        */

	if (LOBYTE(wsaData.wVersion) != 2 ||
		HIBYTE(wsaData.wVersion) != 2) {
		/* Tell the user that we could not find a usable */
		/* WinSock DLL.                                  */
		WSACleanup();
		return;
	}
}

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// Dialog Data
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// CCTS_pCOM_TesterDlg dialog



CCTS_pCOM_TesterDlg::CCTS_pCOM_TesterDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(CCTS_pCOM_TesterDlg::IDD, pParent)
	, m_iSerialPort(1)
	, m_iBaudRate(7)
	, m_iParity(0)
	, m_iSerialPort2(3)
	, m_iBaudRate2(4)
	, m_iParity2(0)
	, m_iSerialPortMaster(2)
	, m_iSerialPort3(4)
	///, m_iSerialPort4(0)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
	m_pwndShow = NULL;

	m_nPort = 2233; // TCP/IP Port 

	WSAStart();
	g_pApp_dlg->m_MySocket = new CMySocket;
	//  m_Display_Step = 0;
}

void CCTS_pCOM_TesterDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_RCV_DATA, m_EditReceiveData);
	DDX_Control(pDX, IDC_SERIAL_PORT, m_cSerialPort);
	DDX_CBIndex(pDX, IDC_SERIAL_PORT, m_iSerialPort);
	DDX_Control(pDX, IDC_BAUD_RATE, m_cBaudRate);
	DDX_CBIndex(pDX, IDC_BAUD_RATE, m_iBaudRate);
	DDX_Control(pDX, IDC_PARITY, m_cParity);
	DDX_CBIndex(pDX, IDC_PARITY, m_iParity);
	DDX_Control(pDX, IDC_IPADDRESS1, m_cIPAddr);
	DDX_Control(pDX, IDC_TAB1, m_Tab);
	DDX_Control(pDX, IDC_SERIAL_PORT2, m_cSerialPort2);
	DDX_CBIndex(pDX, IDC_SERIAL_PORT2, m_iSerialPort2);
	DDX_Control(pDX, IDC_BAUD_RATE2, m_cBaudRate2);
	DDX_CBIndex(pDX, IDC_BAUD_RATE2, m_iBaudRate2);
	DDX_Control(pDX, IDC_PARITY2, m_cParity2);
	DDX_CBIndex(pDX, IDC_PARITY2, m_iParity2);
	DDX_Control(pDX, IDC_ETHERNET_CMD, m_cEhernetCmd);
	DDX_Control(pDX, IDC_SERIAL_PORT_MASTER, m_cSerialPortMaster);
	DDX_CBIndex(pDX, IDC_SERIAL_PORT_MASTER, m_iSerialPortMaster);
	DDX_Control(pDX, IDC_ETHERNET_SEND, m_BtnEhternetSend);
	DDX_Control(pDX, IDC_SERIAL_PORT3, m_cSerialPort3);
	DDX_CBIndex(pDX, IDC_SERIAL_PORT3, m_iSerialPort3);
	DDX_Control(pDX, IDC_EDIT2_VIEW_STEP_DESCRIP, m_Edit_Receive_View_Step_Data);
}

BEGIN_MESSAGE_MAP(CCTS_pCOM_TesterDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_MESSAGE(SOCKET_MSGID, OnSocketMsg)
	ON_BN_CLICKED(IDC_SERIAL_OPEN, &CCTS_pCOM_TesterDlg::OnBnClickedSerialOpen)
	ON_BN_CLICKED(IDC_SERIAL_CLOSE, &CCTS_pCOM_TesterDlg::OnBnClickedSerialClose)
	ON_BN_CLICKED(IDC_ETHERNET_CONNECT, &CCTS_pCOM_TesterDlg::OnBnClickedEthernetConnect)
	ON_BN_CLICKED(IDC_ETHERNET_DISCONNECT, &CCTS_pCOM_TesterDlg::OnBnClickedEthernetDisconnect)
	ON_NOTIFY(TCN_SELCHANGE, IDC_TAB1, &CCTS_pCOM_TesterDlg::OnTcnSelchangeTab1)
	ON_BN_CLICKED(IDC_BUTTON5, &CCTS_pCOM_TesterDlg::OnBnClickedButton5)
	ON_BN_CLICKED(IDC_ETHERNET_SEND, &CCTS_pCOM_TesterDlg::OnBnClickedEthernetSend)
	ON_WM_CTLCOLOR()
END_MESSAGE_MAP()


// CCTS_pCOM_TesterDlg message handlers

BOOL CCTS_pCOM_TesterDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	{
		HRSRC hRsrc = FindResource(NULL, MAKEINTRESOURCE(VS_VERSION_INFO), RT_VERSION);
		if (hRsrc != NULL)
		{
			HGLOBAL hGlobalMemory = LoadResource(NULL, hRsrc);
			if (hGlobalMemory != NULL)
			{
				void *pVersionResouece = LockResource(hGlobalMemory);
				void *pVersion;
				UINT uLength;

				// 아래줄에 041204B0는 리소스 파일(*.rc)에서 가져옴.
				// 프로젝트 리소스 파일을 참고하세요(어느부분 참고인지는 밑에 나와있음)

				/*
				190828 보드레벨 테스트 기능 변경
				flash 대기 시간 32초 -> 40초

				190830 보드레벨 테스트 기능 변경
				flash 대기 시간 40초 -> 42초

				V1.0.0.1
				New JIG Type 완료

				V1.0.0.1-1
				보드레벨 "test 2 fail" 추가

				V1.0.0.2
				기능검사(조립레벨) 200610 온도 보정 OFFSET 값 0.03 -> 0.07
				기능검사(조립레벨) 200610 온도 차이 수정 8.0도 이상
				출하준비 VFMV명령어 추가
				*/

				if (VerQueryValue(pVersionResouece, TEXT("StringFileInfo\\040904b0\\FileVersion"), &pVersion, &uLength) != 0)
				{
					CString ver;
					ver.Format(TEXT("Cantops BG01 Motion Sensor Tester (Version %s)"), (wchar_t*)pVersion);
					SetWindowText(ver);
				}
			}
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here
	hCommWnd = m_hWnd;
	
	//m_Tab.InsertItem(2, _T("기능 검사(보드 레벨)"));
	//m_Tab.InsertItem(3, _T("기능 검사(조립 레벨)"));
	//m_Tab.InsertItem(4, _T("출하 준비"));
	m_Tab.InsertItem(1, _T("BG 01 모션 검사"));
	//m_Tab.InsertItem(2, _T("모션 검사(보드 레벨)"));

	CRect Rect;
	m_Tab.GetClientRect(&Rect);

	// BG01 Add 
	m_BoardLevelMotion_BG01.Create(IDD_BOARDMO_BG01, &m_Tab);
	m_BoardLevelMotion_BG01.SetWindowPos(NULL, 5, 25, Rect.Width() - 12, Rect.Height() - 33, SWP_SHOWWINDOW | SWP_NOZORDER);

	m_pwndShow = &m_BoardLevelMotion_BG01;

	//m_BoardLevelMotion.Create(IDD_BOARDLEVELMO, &m_Tab);
	//m_BoardLevelMotion.SetWindowPos(NULL, 5, 25, Rect.Width() - 12, Rect.Height() - 33,  SWP_NOZORDER);

	

	// m_BoardLevel.Create(IDD_BOARD_LEVEL, &m_Tab);
	// m_BoardLevel.SetWindowPos(NULL, 5, 25, Rect.Width() - 12, Rect.Height() - 33,SWP_NOZORDER);
	// 
	// 
	// m_Commtest.Create(IDD_COMMTEST, &m_Tab);
	// m_Commtest.SetWindowPos(NULL, 5, 25, Rect.Width() - 12, Rect.Height() - 33, SWP_NOZORDER);
	// 
	// m_SetProduct.Create(IDD_SET_PRODUCT, &m_Tab);
	// m_SetProduct.SetWindowPos(NULL, 5, 25, Rect.Width() - 12, Rect.Height() - 33, SWP_NOZORDER);

	

	int tmp = SearchPort();

	// Init Port 
	m_cSerialPort.SetCurSel(1); // X
	m_cSerialPortMaster.SetCurSel(2); // Y 
	m_cSerialPort2.SetCurSel(3); // Z
	m_cSerialPort3.SetCurSel(4); // Slope Sensor
	//m_cSerialPort4.SetCurSel(tmp - 2);

	/// m_cIPAddr.SetAddress(192, 168, 1, 200);
	//m_cIPAddr.SetAddress(192, 168, 1, 101); // 192.168.1.200
	m_cIPAddr.SetAddress(100, 100, 100, 70); // 100.100.100.70
	// IP Setting Value 

	m_EditReceiveData.SetLimitText(0);

	g_pApp_dlg->m_MySocket->SetParentPtr(GetSafeHwnd(), SOCKET_MSGID);
	g_pApp_dlg->m_ComuPort.m_bConnected = FALSE;
	g_pApp_dlg->m_ComuPort2.m_bConnected = FALSE;
	g_pApp_dlg->m_ComuPort3.m_bConnected = FALSE;
	g_pApp_dlg->m_ComuPortMaster.m_bConnected = FALSE;
	g_pApp_dlg->m_ComuPort4.m_bConnected = FALSE;

	m_BtnEhternetSend.ShowWindow(SW_HIDE);
	m_cEhernetCmd.ShowWindow(SW_HIDE);

	GetDlgItem(IDC_ETHERNET_DISCONNECT)->EnableWindow(FALSE);
	GetDlgItem(IDC_SERIAL_CLOSE)->EnableWindow(FALSE);

	// BG01 Button View Setting 
	m_BoardLevelMotion_BG01.m_btnTestStart_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnTestStop_BG01.EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnSensorWork_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnSensorOrigin_BG01.EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnEmioStart_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnEmioStop_BG01.EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnMotorAngle_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnMotorSet_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnClear_BG01.EnableWindow(FALSE);


	m_editFont_View_Step_R.CreatePointFont(220, TEXT("굴림"));
	m_Edit_Receive_View_Step_Data.SetFont(&m_editFont_View_Step_R, TRUE); // Font Init 


	return TRUE;  // return TRUE  unless you set the focus to a control
}


void CCTS_pCOM_TesterDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CCTS_pCOM_TesterDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CCTS_pCOM_TesterDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}


DWORD CCTS_pCOM_TesterDlg::byIndexBaud(int xBaud)
{
	DWORD dwBaud = 0;
	switch (xBaud)
	{
	case 0:		dwBaud = CBR_4800;		break;
	case 1:		dwBaud = CBR_9600;		break;
	case 2:		dwBaud = CBR_14400;		break;
	case 3:		dwBaud = CBR_19200;		break;
	case 4:		dwBaud = CBR_38400;		break;
	case 5:		dwBaud = CBR_56000;		break;
	case 6:		dwBaud = CBR_57600;		break;
	case 7:		dwBaud = CBR_115200;	break;
	}

	return dwBaud;
}

BYTE CCTS_pCOM_TesterDlg::byIndexData(int xData)
{
	BYTE byData = 0;
	switch (xData)
	{
	case 0:	byData = 5;			break;
	case 1:	byData = 6;			break;
	case 2:	byData = 7;			break;
	case 3:	byData = 8;			break;
	}

	return byData;
}

BYTE CCTS_pCOM_TesterDlg::byIndexStop(int xStop)
{
	BYTE byStop = 0;
	if (xStop == 0)
	{
		byStop = ONESTOPBIT;
	}
	else
	{
		byStop = TWOSTOPBITS;
	}

	return byStop;
}

BYTE CCTS_pCOM_TesterDlg::byIndexParity(int xParity)
{
	BYTE byParity = 0;
	switch (xParity)
	{
	case 0:	byParity = NOPARITY;	break;
	case 1:	byParity = ODDPARITY;	break;
	case 2:	byParity = EVENPARITY;	break;
	}

	return byParity;
}
#if 0
long CCTS_pCOM_TesterDlg::OnCommunication(WPARAM wParam, LPARAM lParam)
{
	UpdateData(TRUE);
	CString tStr, strASCIIRX;
	BYTE aByte; //데이터를 저장할 변수

	int iSize = (g_pApp_dlg->m_ComuPort.m_QueueRead).GetSize(); //포트로 들어온 데이터 갯수
	int iSize2 = (g_pApp_dlg->m_ComuPort2.m_QueueRead).GetSize(); //포트로 들어온 데이터 갯수
	int iSize3 = (g_pApp_dlg->m_ComuPortMaster.m_QueueRead).GetSize(); //포트로 들어온 데이터 갯수
	if (iSize)
	{
		tStr = _T("");
		strASCIIRX = _T("");
		int cutlLen = m_EditReceiveData.GetWindowTextLength();
		if (cutlLen > 4192) m_EditReceiveData.SetWindowText(_T(""));

		for (int i = 0; i < iSize; i++)//들어온 갯수 만큼 데이터를 읽어 와 화면에 보여줌
		{
			(g_pApp_dlg->m_ComuPort.m_QueueRead).GetByte(&aByte);//큐에서 데이터 한개를 읽어옴
			if (aByte == NULL)	tStr = _T("\r\n");
			else				tStr.Format(_T("%c"), aByte);
			strASCIIRX += tStr;
		}
		int lLen = m_EditReceiveData.GetWindowTextLength();
		m_EditReceiveData.SetSel(lLen, lLen);
		m_EditReceiveData.ReplaceSel(strASCIIRX);

		if (m_BoardLevel.m_bThreadStatus == TRUE/* || m_BoardLevel.m_bThreadStatus == FALSE*/)
		{
			m_BoardLevel.RcvStr = strASCIIRX;
		}
		if (m_SetProduct.m_bThreadStatus == TRUE)
		{
			m_SetProduct.RcvStr = strASCIIRX;
		}
		if (m_Commtest.m_bThreadStatus == TRUE)
		{
			m_Commtest.RcvStr = strASCIIRX;
		}
	}
	if (iSize2)
	{
		tStr = _T("");
		strASCIIRX = _T("");
		int cutlLen = m_EditReceiveData.GetWindowTextLength();
		if (cutlLen > 4192) m_EditReceiveData.SetWindowText(_T(""));

		for (int i = 0; i < iSize2; i++)//들어온 갯수 만큼 데이터를 읽어 와 화면에 보여줌
		{
			(g_pApp_dlg->m_ComuPort2.m_QueueRead).GetByte(&aByte);//큐에서 데이터 한개를 읽어옴
			if (aByte == NULL)	tStr = _T("\r\n");
			else				tStr.Format(_T("%c"), aByte);
			strASCIIRX += tStr;
		}
		int lLen = m_EditReceiveData.GetWindowTextLength();
		m_EditReceiveData.SetSel(lLen, lLen);
		m_EditReceiveData.ReplaceSel(strASCIIRX);

		if (m_BoardLevel.m_bThreadStatus == TRUE)
		{
			m_BoardLevel.RcvStr = strASCIIRX;
		}
		if (m_SetProduct.m_bThreadStatus == TRUE)
		{
			m_SetProduct.RcvStr = strASCIIRX;
		}
		if (m_Commtest.m_bThreadStatus == TRUE)
		{
			m_Commtest.RcvStr = strASCIIRX;
		}
	}
	if (iSize3)
	{
		tStr = _T("");
		strASCIIRX = _T("");
		int cutlLen = m_EditReceiveData.GetWindowTextLength();
		if (cutlLen > 4192) m_EditReceiveData.SetWindowText(_T(""));

		for (int i = 0; i < iSize3; i++)//들어온 갯수 만큼 데이터를 읽어 와 화면에 보여줌
		{
			(g_pApp_dlg->m_ComuPortMaster.m_QueueRead).GetByte(&aByte);//큐에서 데이터 한개를 읽어옴
			if (aByte == NULL)	tStr = _T("\r\n");
			else				tStr.Format(_T("%c"), aByte);
			strASCIIRX += tStr;
		}
		int lLen = m_EditReceiveData.GetWindowTextLength();
		m_EditReceiveData.SetSel(lLen, lLen);
		m_EditReceiveData.ReplaceSel(_T("\r\nRcvMaster>") + strASCIIRX);

		if (m_BoardLevel.m_bThreadStatus == TRUE || m_BoardLevel.m_bThreadStatus == FALSE)
		{
			m_BoardLevel.RcvStrMaster = strASCIIRX;
		}
	}
	return 0;
}
#endif

void CCTS_pCOM_TesterDlg::PortControl(CString cmd)
{
	CString msg = _T("");

	msg += (TCHAR)STX;
	msg += cmd;
	msg += (TCHAR)ETX;

	g_pApp_dlg->m_MySocket->Send(msg, strlen(msg));

	Sleep(100);
}

UINT portremote_check(LPVOID lParam)
{
	CCTS_pCOM_TesterDlg *pCCTS_pCOM_TesterDlg;
	pCCTS_pCOM_TesterDlg = (CCTS_pCOM_TesterDlg*)lParam;

	while (pCCTS_pCOM_TesterDlg->m_bPortThread)
	{
		pCCTS_pCOM_TesterDlg->PortControl(_T("\x2PII00\x3"));

		Sleep(50);
	}

	return 0;
}

LRESULT CCTS_pCOM_TesterDlg::OnSocketMsg(WPARAM wParam, LPARAM lParam)
{
	switch (wParam)
	{
	case MSO_CONNECT:	// 접속이 완료 되었음.
		m_cIPAddr.EnableWindow(FALSE);
		GetDlgItem(IDC_ETHERNET_CONNECT)->EnableWindow(FALSE);
		GetDlgItem(IDC_ETHERNET_DISCONNECT)->EnableWindow(TRUE);
		SetDlgItemText(IDC_STATIC16, "포트리모트 연결 완료");
		m_bPortThread = 1;
		m_BoardLevelMotion_BG01.m_b_tcp_connection_status_BG01 = TRUE;
		AfxBeginThread(portremote_check, (LPVOID)this);
		break;

	case MSO_DISCONNECT:
		m_cIPAddr.EnableWindow(TRUE);
		GetDlgItem(IDC_ETHERNET_CONNECT)->EnableWindow(TRUE);
		if (g_pApp_dlg->m_MySocket != NULL)	g_pApp_dlg->m_MySocket->Close();
		SetDlgItemText(IDC_STATIC16, "포트리모트 연결을 확인하세요");
		m_bPortThread = 0;
		WaitForSingleObject(m_hPortThread, 200);

		if (m_BoardLevelMotion_BG01.m_bThreadStatus_BG01==TRUE) {
			m_BoardLevelMotion_BG01.m_b_tcp_connection_status_BG01 = FALSE;
			SetDlgItemText(IDC_STATIC16, "포트리모트 연결이 끊겼습니다.");

			//BYTE f0, f1, f2, f3;
			//m_cIPAddr.GetAddress(f0, f1, f2, f3);
			//g_pApp_dlg->m_MySocket->Create();
			//
			//BOOL bf = FALSE;
			//g_pApp_dlg->m_MySocket->SetSockOpt(SO_LINGER, &bf, sizeof(bf));
			//
			//if (g_pApp_dlg->m_MySocket->Connect(f0, f1, f2, f3, m_nPort) == FALSE)
			//{
			//	int err = GetLastError();
			//	if (err != WSAEWOULDBLOCK)
			//		AfxMessageBox("연결하지 못했습니다");
			//	Sleep(500);
			//}
		}
		

		break;

	case MSO_RECEIVE:
		OnRcv();
		break;
	}

	return 0;
}

int CCTS_pCOM_TesterDlg::SearchPort()
{
	int i;
	HKEY h_CommKey;
	LONG Reg_Ret;
	DWORD Size = MAX_PATH;
	char i_str[MAX_PATH];
	Reg_Ret = RegOpenKeyEx(HKEY_LOCAL_MACHINE, _T("HARDWARE\\DEVICEMAP\\SERIALCOMM"), 0, KEY_READ | KEY_QUERY_VALUE, &h_CommKey);
	//레지스트리..
	if (Reg_Ret == ERROR_SUCCESS)
	{
		for (i = 0; Reg_Ret == ERROR_SUCCESS; i++)
		{
			Reg_Ret = RegEnumValue(h_CommKey, i, i_str, &Size, NULL, NULL, NULL, NULL);
			if (Reg_Ret == ERROR_SUCCESS)
			{
				DWORD dwType, dwSize = MAX_PATH;
				char szBuffer[MAX_PATH];

				RegQueryValueEx(h_CommKey, i_str, 0, &dwType, (LPBYTE)szBuffer, &dwSize);

				m_cSerialPort.AddString((LPCTSTR)szBuffer);  // 리스트 박스에 레지스트리 내용 추가(여기서는 COM PORT)
				m_cSerialPort2.AddString((LPCTSTR)szBuffer);  // 리스트 박스에 레지스트리 내용 추가(여기서는 COM PORT)
				m_cSerialPortMaster.AddString((LPCTSTR)szBuffer);  // 리스트 박스에 레지스트리 내용 추가(여기서는 COM PORT)
				m_cSerialPort3.AddString((LPCTSTR)szBuffer);  // 리스트 박스에 레지스트리 내용 추가(여기서는 COM PORT)
				//m_cSerialPort4.AddString((LPCTSTR)szBuffer);  // 리스트 박스에 레지스트리 내용 추가(여기서는 COM PORT)
				//하위 레지스트리 값을 얻을 수 있음. 
			}
			Size = MAX_PATH;
		}
	}
	RegCloseKey(h_CommKey);

	return i;
}

unsigned char _hexchar2int(char c)
{
	if (c<'0')
		return 0;
	if (c <= '9')
		return (unsigned char)c - '0';
	if (c<'A')
		return 0;
	if (c <= 'F')
		return (unsigned char)c - 'A' + 10;
	if (c<'a')
		return 0;
	if (c <= 'f')
		return (unsigned char)c - 'a' + 10;
	return '0';
}

unsigned long _hexstr2int(char* str, int len)
{
	int i;
	unsigned long ret;

	ret = 0;

	i = 0;
	for (; i<len; i++)
	{
		ret += _hexchar2int(str[i]);
		if (i<(len - 1)) ret <<= 4;
	}
	return ret;
}

void CCTS_pCOM_TesterDlg::OnRcv()
{
	static int state = 0;
	static char szRcv[1000];
	static int nRcvOfs = 0;

	UCHAR str[100];

	int len;
	//while ((len = m_pSocket->Receive(str, 100)) > 0)
	while ((len = g_pApp_dlg->m_MySocket->Receive(str, 100)) > 0)
	{
		for (int i = 0; i < len; i++)
		{
			UCHAR uch = str[i];
			switch (state)
			{
			case 0:	// wait STX (0x02)
				szRcv[0] = uch;
				if (uch == STX)
				{
					state = 1;
					nRcvOfs = 1;
				}
				break;
			case 1:
				szRcv[nRcvOfs++] = uch;
				szRcv[nRcvOfs] = 0;
				if (uch == ETX)
				{
					//DoPacket((UCHAR *)szRcv, nRcvOfs);
					//m_Commtest.RcvStr = (UCHAR *)szRcv;
					nRcvOfs = 0;
					state = 0;
				}
				break;
			}
		}
		/*if (m_Commtest.m_bThreadStatus == TRUE)
		{
			m_Commtest.RcvStr = (UCHAR *)szRcv;
		}
		if (m_BoardLevelComm.m_bThreadStatus == TRUE)
		{
			m_BoardLevelComm.RcvStr = (UCHAR *)szRcv;
		}*/
		
		if (szRcv[2] == 'i' && szRcv[3] == 'i')
		{
#if 0
			int lLen = m_EditReceiveData.GetWindowTextLength();
			m_EditReceiveData.SetSel(lLen, lLen);
			m_EditReceiveData.ReplaceSel(szRcv);
#endif
		}
		else
		{
			CString strData;
			CTime cTimeT = CTime::GetCurrentTime();
			strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));
			
			int lLen = m_EditReceiveData.GetWindowTextLength();
			m_EditReceiveData.SetSel(lLen, lLen);
			m_EditReceiveData.ReplaceSel(_T("\r\n" + strData +" [E.RCV] > "));

			lLen = m_EditReceiveData.GetWindowTextLength();
			m_EditReceiveData.SetSel(lLen, lLen);
			m_EditReceiveData.ReplaceSel(szRcv);

			theApp.App_RcvPort.Format("%s", szRcv);
			
			CString strBufferTemp;
			strBufferTemp = (_T("\r\n" + strData + " [E.RCV] > ") + (CString)szRcv);			

			if (INVALID_HANDLE_VALUE == (HANDLE)m_p_Log_File_Save) {
			}else{
				m_p_Log_File_Save.Write((strBufferTemp), strBufferTemp.GetLength());

				ULONGLONG  fileLength = m_p_Log_File_Save.GetPosition();
				if (fileLength > 1000000) {
					m_p_Log_File_Save.Close();
					g_pApp_dlg->fReOpen();
				}
			}

		}

		if (szRcv[1] == 'p' && szRcv[2] == 'i' && szRcv[3] == 'i' && szRcv[4] == '0' && szRcv[5] == '3' && szRcv[6] == '2')
		{
			for (int i = 0; i < 8; i++)
			{
				g_pApp_dlg->InputPort[7 - i] = (unsigned char)_hexstr2int(szRcv + 7 + i * 2, 2);
				Sleep(0);
			}
			for (int i = 0; i < 8; i++)
			{
				g_pApp_dlg->OutputPort[7 - i] = (unsigned char)_hexstr2int(szRcv + 7 + 16 + i * 2, 2);
				Sleep(0);
			}
		}

		Sleep(0);
	}
}

void CCTS_pCOM_TesterDlg::OnBnClickedSerialOpen()
{
	UpdateData(TRUE);
	CString _portName, PortName, MonitorPort;
	_portName = _T("");
	PortName = _T("");
	MonitorPort = _T("");
	m_cSerialPort.GetWindowText(_portName); // X 
	PortName = _portName;
	_portName = _T("\\\\.\\") + PortName; // TEST Port 

	if (g_pApp_dlg->m_ComuPort.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	{
		if (g_pApp_dlg->m_ComuPort.OpenPort(_portName, byIndexBaud(m_iBaudRate), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity)) == TRUE)
		{
			GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(FALSE);
			m_cSerialPort.EnableWindow(FALSE);
			m_cBaudRate.EnableWindow(FALSE);
			m_cParity.EnableWindow(FALSE);
			m_Tab.EnableWindow(TRUE);
		}
	}
	else
	{
		CString msg;
		msg.Format(_T("시리얼을 연결하지 못했습니다"));
		AfxMessageBox(msg);
	}

	m_cSerialPortMaster.GetWindowText(_portName); // Y
	PortName = _portName;
	_portName = _T("\\\\.\\") + PortName; // REF PORT 

	if (g_pApp_dlg->m_ComuPortMaster.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	{
		if (g_pApp_dlg->m_ComuPortMaster.OpenPort(_portName, byIndexBaud(m_iBaudRate), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity)) == TRUE)
		{
			GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(FALSE);
			m_cSerialPortMaster.EnableWindow(FALSE);
			m_cBaudRate.EnableWindow(FALSE);
			m_cParity.EnableWindow(FALSE);
			m_Tab.EnableWindow(TRUE);
		}
	}
	else
	{
		CString msg;
		msg.Format(_T("시리얼을 연결하지 못했습니다"));
		AfxMessageBox(msg);
	}

	m_cSerialPort2.GetWindowText(_portName);  // Z
	PortName = _portName;
	_portName = _T("\\\\.\\") + PortName;

	if (g_pApp_dlg->m_ComuPort2.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	{
		if (g_pApp_dlg->m_ComuPort2.OpenPort(_portName, byIndexBaud(m_iBaudRate), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity)) == TRUE)
		{
			GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(FALSE);
			m_cSerialPort2.EnableWindow(FALSE);
			m_cBaudRate.EnableWindow(FALSE);
			m_cParity.EnableWindow(FALSE);
			m_Tab.EnableWindow(TRUE);
		}
	}
	else
	{
		CString msg;
		msg.Format(_T("시리얼을 연결하지 못했습니다"));
		AfxMessageBox(msg);
	}
	// int nIndex = m_Tab.GetCurSel();
	m_cSerialPort3.GetWindowText(_portName); // Slope 
	PortName = _portName;
	_portName = _T("\\\\.\\") + PortName; /// Slope Sensor BaudRate 38.4kbps
	if (g_pApp_dlg->m_ComuPort3.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	{
		//if (nIndex != 1)
		//{
		//	if (g_pApp_dlg->m_ComuPort3.OpenPort(_portName, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity)) == TRUE)
		//	{
		//		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(FALSE);
		//		m_cSerialPort3.EnableWindow(FALSE);
		//		m_cBaudRate2.EnableWindow(FALSE);
		//		m_cParity2.EnableWindow(FALSE);
		//		m_Tab.EnableWindow(TRUE);
		//	}
		//}
		//else
		{
			if (g_pApp_dlg->m_ComuPort3.OpenPort(_portName, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2)) == TRUE)
			{
				GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(FALSE);
				m_cSerialPort3.EnableWindow(FALSE);
				m_cBaudRate2.EnableWindow(FALSE);
				m_cParity2.EnableWindow(FALSE);
				m_Tab.EnableWindow(TRUE);
			}
		}
	}
	else
	{
		CString msg;
		msg.Format(_T("시리얼을 연결하지 못했습니다"));
		AfxMessageBox(msg);
	}


	GetDlgItem(IDC_SERIAL_CLOSE)->EnableWindow(TRUE);
	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(FALSE);

	UpdateData(FALSE);
}

void CCTS_pCOM_TesterDlg::OnBnClickedSerialClose()
{
	UpdateData(TRUE);
	CString _portName, PortName;
	_portName = _T("");
	PortName = _T("");

	m_cSerialPort.GetWindowText(_portName);
	if (g_pApp_dlg->m_ComuPort.m_bConnected == TRUE)
	{
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		m_cSerialPort.EnableWindow(TRUE);
		m_cBaudRate.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPort.ClosePort();
	}
	else
	{
		CString msg;
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
		AfxMessageBox(msg);
	}

	m_cSerialPortMaster.GetWindowText(_portName);
	if (g_pApp_dlg->m_ComuPortMaster.m_bConnected == TRUE)
	{
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		m_cSerialPortMaster.EnableWindow(TRUE);
		m_cBaudRate.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPortMaster.ClosePort();
	}
	else
	{
		CString msg;
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
		AfxMessageBox(msg);
	}

	/// <summary>
	/// Add Port 
	/// </summary>
	m_cSerialPort2.GetWindowText(_portName);
	if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	{
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		m_cSerialPort2.EnableWindow(TRUE);
		m_cBaudRate.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPort2.ClosePort();
	}
	else
	{
		CString msg;
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
		AfxMessageBox(msg);
	}

	 m_cSerialPort3.GetWindowText(_portName);
	 if (g_pApp_dlg->m_ComuPort3.m_bConnected == TRUE)
	 {
	 	//GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(TRUE);
	 	m_cSerialPort3.EnableWindow(TRUE);
	 	m_cBaudRate2.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
	 	g_pApp_dlg->m_ComuPort3.ClosePort();
	 }
	 else
	 {
	 	CString msg;
	 	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	 	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	 	AfxMessageBox(msg);
	 }

	 GetDlgItem(IDC_SERIAL_CLOSE)->EnableWindow(FALSE);
	 GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);

}

//pMainDlg->EMIOSendPacketDisplay(SendData);
void CCTS_pCOM_TesterDlg::EMIOSendPacketDisplay(CString SendData)
{
	CString strData;
	CTime cTimeT = CTime::GetCurrentTime();
	strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

	int lLen = m_EditReceiveData.GetWindowTextLength();
	m_EditReceiveData.SetSel(lLen, lLen);
	m_EditReceiveData.ReplaceSel(_T("\r\n" + strData + " " + SendData));

	CString strBufferTemp;
	strBufferTemp = ( _T("\r\n" + strData + " " + SendData) );

	if (INVALID_HANDLE_VALUE == (HANDLE)m_p_Log_File_Save) {
	}else{

		m_p_Log_File_Save.Write((strBufferTemp), strBufferTemp.GetLength());

		ULONGLONG  fileLength = m_p_Log_File_Save.GetPosition();
		if (fileLength > 1000000) {
			m_p_Log_File_Save.Close();
			g_pApp_dlg->fReOpen();
		}
	}

}

void CCTS_pCOM_TesterDlg::StepSequenceDisplay(unsigned char StepIndex)
{
	switch (StepIndex) {
		case 0:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 0"); break;
		case 1:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 1"); break;
		case 2:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 2"); break;
		case 3:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 3"); break;
		case 4:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 4"); break;
		case 5:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 5"); break;
		case 6:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 6"); break;
		case 7:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 7"); break;
		case 8:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 8"); break;
		case 9:  SetDlgItemText(IDC_STATIC_STEP_T, "STEP 9"); break;
		case 10: SetDlgItemText(IDC_STATIC_STEP_T, "STEP 10"); break;
		case 11: SetDlgItemText(IDC_STATIC_STEP_T, "STEP 11"); break;
		case 12: SetDlgItemText(IDC_STATIC_STEP_T, "STEP 12"); break;
		case 13: SetDlgItemText(IDC_STATIC_STEP_T, "STEP 13"); break;
		case 14: break;
		default: break;

	}
}



void CCTS_pCOM_TesterDlg::OnBnClickedSerialOpen2()
{
	// UpdateData(TRUE);
	// CString _portName, PortName, MonitorPort;
	// _portName = _T("");
	// PortName = _T("");
	// MonitorPort = _T("");
	// m_cSerialPort2.GetWindowText(_portName);
	// PortName = _portName;
	// _portName = _T("\\\\.\\") + PortName;
	// 
	// if (g_pApp_dlg->m_ComuPort2.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	// {
	// 	if (g_pApp_dlg->m_ComuPort2.OpenPort(_portName, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2)) == TRUE)
	// 	{
	// 		GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(FALSE);
	// 		m_cSerialPort2.EnableWindow(FALSE);
	// 		m_cBaudRate2.EnableWindow(FALSE);
	// 		m_cParity2.EnableWindow(FALSE);
	// 		m_Tab.EnableWindow(TRUE);
	// 	}
	// }
	// else
	// {
	// 	CString msg;
	// 	msg.Format(_T("시리얼을 연결하지 못했습니다"));
	// 	AfxMessageBox(msg);
	// }
	// 
	// m_cSerialPort3.GetWindowText(_portName);
	// PortName = _portName;
	// _portName = _T("\\\\.\\") + PortName;
	// 
	// int nIndex = m_Tab.GetCurSel();
	// 
	// if (g_pApp_dlg->m_ComuPort3.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	// {
	// 	if (nIndex != 1)
	// 	{
	// 		if (g_pApp_dlg->m_ComuPort3.OpenPort(_portName, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity)) == TRUE)
	// 		{
	// 			GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(FALSE);
	// 			m_cSerialPort3.EnableWindow(FALSE);
	// 			m_cBaudRate2.EnableWindow(FALSE);
	// 			m_cParity2.EnableWindow(FALSE);
	// 			m_Tab.EnableWindow(TRUE);
	// 		}
	// 	}
	// 	else
	// 	{
	// 		if (g_pApp_dlg->m_ComuPort3.OpenPort(_portName, byIndexBaud(4), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity)) == TRUE)
	// 		{
	// 			GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(FALSE);
	// 			m_cSerialPort3.EnableWindow(FALSE);
	// 			m_cBaudRate2.EnableWindow(FALSE);
	// 			m_cParity2.EnableWindow(FALSE);
	// 			m_Tab.EnableWindow(TRUE);
	// 		}
	// 	}
	// }
	// else
	// {
	// 	CString msg;
	// 	msg.Format(_T("시리얼을 연결하지 못했습니다"));
	// 	AfxMessageBox(msg);
	// }
	// UpdateData(FALSE);
}


void CCTS_pCOM_TesterDlg::OnBnClickedSerialClose2()
{
	// UpdateData(TRUE);
	// CString _portName, PortName;
	// _portName = _T("");
	// PortName = _T("");
	// 
	// m_cSerialPort2.GetWindowText(_portName);
	// if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	// {
	// 	GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(TRUE);
	// 	m_cSerialPort2.EnableWindow(TRUE);
	// 	m_cBaudRate2.EnableWindow(TRUE);
	// 	m_cParity2.EnableWindow(TRUE);
	// 	g_pApp_dlg->m_ComuPort2.ClosePort();
	// }
	// else
	// {
	// 	CString msg;
	// 	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	// 	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	// 	AfxMessageBox(msg);
	// }
	// 
	// m_cSerialPort3.GetWindowText(_portName);
	// if (g_pApp_dlg->m_ComuPort3.m_bConnected == TRUE)
	// {
	// 	GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(TRUE);
	// 	m_cSerialPort3.EnableWindow(TRUE);
	// 	m_cBaudRate2.EnableWindow(TRUE);
	// 	m_cParity2.EnableWindow(TRUE);
	// 	g_pApp_dlg->m_ComuPort3.ClosePort();
	// }
	// else
	// {
	// 	CString msg;
	// 	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	// 	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	// 	AfxMessageBox(msg);
	// }
}

void CCTS_pCOM_TesterDlg::OnBnClickedSerialOpen3()
{
	// UpdateData(TRUE);
	// CString _portName, PortName, MonitorPort;
	// _portName = _T("");
	// PortName = _T("");
	// MonitorPort = _T("");
	// //m_cSerialPort4.GetWindowText(_portName);
	// PortName = _portName;
	// _portName = _T("\\\\.\\") + PortName;
	// 
	// if (g_pApp_dlg->m_ComuPort4.m_bConnected == FALSE)//포트가 닫혀 있을 경우에만 포트를 열기 위해
	// {
	// 	if (g_pApp_dlg->m_ComuPort4.OpenPort(_portName, byIndexBaud(3), byIndexData(3), byIndexStop(0), byIndexParity(0)) == TRUE)
	// 	{
	// 		//GetDlgItem(IDC_SERIAL_OPEN3)->EnableWindow(FALSE);
	// 		//m_cSerialPort4.EnableWindow(FALSE);
	// 		m_Tab.EnableWindow(TRUE);
	// 	}
	// }
	// else
	// {
	// 	CString msg;
	// 	msg.Format(_T("시리얼을 연결하지 못했습니다"));
	// 	AfxMessageBox(msg);
	// }
}

void CCTS_pCOM_TesterDlg::OnBnClickedSerialClose3()
{
	// UpdateData(TRUE);
	// CString _portName, PortName;
	// _portName = _T("");
	// PortName = _T("");
	// 
	// //m_cSerialPort4.GetWindowText(_portName);
	// if (g_pApp_dlg->m_ComuPort4.m_bConnected == TRUE)
	// {
	// 	//GetDlgItem(IDC_SERIAL_OPEN3)->EnableWindow(TRUE);
	// 	//m_cSerialPort4.EnableWindow(TRUE);
	// 	g_pApp_dlg->m_ComuPort4.ClosePort();
	// }
	// else
	// {
	// 	CString msg;
	// 	//GetDlgItem(IDC_SERIAL_OPEN3)->EnableWindow(TRUE);
	// 	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	// 	AfxMessageBox(msg);
	// }
}

void CCTS_pCOM_TesterDlg::OnBnClickedEthernetConnect()
{
	GetDlgItem(IDC_ETHERNET_CONNECT)->EnableWindow(FALSE);
	GetDlgItem(IDC_ETHERNET_DISCONNECT)->EnableWindow(TRUE);

	m_BoardLevelMotion_BG01.m_btnTestStart_BG01.EnableWindow(TRUE);
	m_BoardLevelMotion_BG01.m_btnTestStop_BG01.EnableWindow(TRUE);

	m_BoardLevelMotion_BG01.m_btnSensorWork_BG01.EnableWindow(TRUE);
	m_BoardLevelMotion_BG01.m_btnSensorOrigin_BG01.EnableWindow(TRUE);

	m_BoardLevelMotion_BG01.m_btnEmioStart_BG01.EnableWindow(TRUE);
	m_BoardLevelMotion_BG01.m_btnEmioStop_BG01.EnableWindow(TRUE);

	m_BoardLevelMotion_BG01.m_btnMotorAngle_BG01.EnableWindow(TRUE);
	m_BoardLevelMotion_BG01.m_btnMotorSet_BG01.EnableWindow(TRUE);
	m_BoardLevelMotion_BG01.m_btnClear_BG01.EnableWindow(TRUE);


	BYTE f0, f1, f2, f3;
	UpdateData(TRUE);

	m_cIPAddr.GetAddress(f0, f1, f2, f3);

	AfxSocketInit();

	g_pApp_dlg->m_MySocket->Create();

	BOOL bf = FALSE;
	g_pApp_dlg->m_MySocket->SetSockOpt(SO_LINGER, &bf, sizeof(bf));

	if (g_pApp_dlg->m_MySocket->Connect(f0, f1, f2, f3, m_nPort) == FALSE)
	{
		int err = GetLastError();
		if (err != WSAEWOULDBLOCK)
			AfxMessageBox("연결하지 못했습니다");
		Sleep(500);
	}
	
	// Fil Open
	CString LogfileName;
	CTime cTimeTSave = CTime::GetCurrentTime();

	// File Name  
	LogfileName = "BG01_Log_";
	LogfileName += cTimeTSave.Format(_T("_%Y-%m-%d_%HH_%MM_%SS"));
	LogfileName += ".txt";

	// File Open CFile 
	
	if (INVALID_HANDLE_VALUE == (HANDLE)m_p_Log_File_Save) {
		if (!(m_p_Log_File_Save.Open(LogfileName, CFile::modeCreate | CFile::modeReadWrite | CFile::shareDenyNone)))
		{
			
		}
		else {
			CString strData;
			CString strBufferTemp;
			CTime cTimeT = CTime::GetCurrentTime();
			strData = cTimeT.Format(_T("%Y-%m-%d %H:%M:%S"));

			strBufferTemp = _T("## BG01 MOTION SENSOR JIG LOG START ## " + strData);			
			m_p_Log_File_Save.Write(strBufferTemp, strBufferTemp.GetLength());
			
		}
	}
		

	
	

	
	//else { // If connected 
		
	//}
	UpdateData(FALSE);
}


void CCTS_pCOM_TesterDlg::OnBnClickedEthernetDisconnect()
{

	GetDlgItem(IDC_ETHERNET_CONNECT)->EnableWindow(TRUE);
	GetDlgItem(IDC_ETHERNET_DISCONNECT)->EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnTestStart_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnTestStop_BG01.EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnSensorWork_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnSensorOrigin_BG01.EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnEmioStart_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnEmioStop_BG01.EnableWindow(FALSE);

	m_BoardLevelMotion_BG01.m_btnMotorAngle_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnMotorSet_BG01.EnableWindow(FALSE);
	m_BoardLevelMotion_BG01.m_btnClear_BG01.EnableWindow(FALSE);


	g_pApp_dlg->m_MySocket->Close();
	SendMessage(SOCKET_MSGID, MSO_DISCONNECT, 0);

	if (INVALID_HANDLE_VALUE == (HANDLE)m_p_Log_File_Save) {
	}
	else{	m_p_Log_File_Save.Close(); // Log Fil Close 
	}
	m_cIPAddr.EnableWindow(TRUE);
}


void CCTS_pCOM_TesterDlg::OnTcnSelchangeTab1(NMHDR *pNMHDR, LRESULT *pResult)
{
	//Port2, 3 baudrate 셋팅
	/*
			기능검사(보드레벨)  모션검사  기능검사(조립레벨)    출하준비
	port 2       115200          57600       115200           115200          
	port 3       115200          38400       115200           115200
	*/
	UpdateData(TRUE);
	CString _portName, PortName, port2, port3;
	m_cSerialPort2.GetWindowText(_portName);
	PortName = _portName;
	port2 = _T("\\\\.\\") + PortName;

	m_cSerialPort3.GetWindowText(_portName);
	PortName = _portName;
	port3 = _T("\\\\.\\") + PortName;


	if (m_pwndShow != NULL)
	{
		m_pwndShow->ShowWindow(SW_HIDE);
		m_pwndShow = NULL;
	}

	int nIndex = m_Tab.GetCurSel();
	switch (nIndex)
	{
	case 0: // BG01
		m_BoardLevelMotion_BG01.ShowWindow(SW_SHOW);
		m_pwndShow = &m_BoardLevelMotion_BG01;
		if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
		{
			if (m_iBaudRate2 != 6)
			{
				g_pApp_dlg->m_ComuPort2.ClosePort();
				g_pApp_dlg->m_ComuPort3.ClosePort();
				m_iBaudRate2 = 6;	//57600bps
				g_pApp_dlg->m_ComuPort2.OpenPort(port2, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
				//모션 검사에서 Comport3은 38400bps(Ref.Gyro Sensor)
				g_pApp_dlg->m_ComuPort3.OpenPort(port3, byIndexBaud(m_iBaudRate2 - 2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
			}
		}
		else
			if (m_iBaudRate2 != 6)	m_iBaudRate2 = 6;	//57600bps
		break;
	//case 1:
	//	m_BoardLevel.ShowWindow(SW_SHOW);
	//	m_pwndShow = &m_BoardLevel;
	//	if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	//	{
	//		if (m_iBaudRate2 != 7)
	//		{
	//			g_pApp_dlg->m_ComuPort2.ClosePort();
	//			g_pApp_dlg->m_ComuPort3.ClosePort();
	//			m_iBaudRate2 = 7;	//115200bps
	//			g_pApp_dlg->m_ComuPort2.OpenPort(port2, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//			g_pApp_dlg->m_ComuPort3.OpenPort(port3, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//		}
	//	}
	//	else
	//		if (m_iBaudRate2 != 7)	m_iBaudRate2 = 7;	//115200bps
	//	break;
	//case 1:
	//	m_BoardLevelMotion.ShowWindow(SW_SHOW);
	//	m_pwndShow = &m_BoardLevelMotion;
	//	if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	//	{
	//		if (m_iBaudRate2 != 6)
	//		{
	//			g_pApp_dlg->m_ComuPort2.ClosePort();
	//			g_pApp_dlg->m_ComuPort3.ClosePort();
	//			m_iBaudRate2 = 6;	//57600bps
	//			g_pApp_dlg->m_ComuPort2.OpenPort(port2, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//			//모션 검사에서 Comport3은 38400bps(Ref.Gyro Sensor)
	//			g_pApp_dlg->m_ComuPort3.OpenPort(port3, byIndexBaud(m_iBaudRate2 - 2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//		}
	//	}
	//	else
	//		if (m_iBaudRate2 != 6)	m_iBaudRate2 = 6;	//57600bps
	//	break;
	//case 2:
	//	m_Commtest.ShowWindow(SW_SHOW);
	//	m_pwndShow = &m_Commtest;
	//	if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	//	{
	//		if (m_iBaudRate2 != 7)
	//		{
	//			g_pApp_dlg->m_ComuPort2.ClosePort();
	//			g_pApp_dlg->m_ComuPort3.ClosePort();
	//			m_iBaudRate2 = 7;	//115200bps
	//			g_pApp_dlg->m_ComuPort2.OpenPort(port2, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//			g_pApp_dlg->m_ComuPort3.OpenPort(port3, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//		}
	//	}
	//	else
	//		if (m_iBaudRate2 != 7)	m_iBaudRate2 = 7;	//115200bps
	//	break;
	//case 3:
	//	m_SetProduct.ShowWindow(SW_SHOW);
	//	m_pwndShow = &m_SetProduct;
	//	if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	//	{
	//		if (m_iBaudRate2 != 7)
	//		{
	//			g_pApp_dlg->m_ComuPort2.ClosePort();
	//			g_pApp_dlg->m_ComuPort3.ClosePort();
	//			m_iBaudRate2 = 7;	//115200bps
	//			g_pApp_dlg->m_ComuPort2.OpenPort(port2, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//			g_pApp_dlg->m_ComuPort3.OpenPort(port3, byIndexBaud(m_iBaudRate2), byIndexData(3), byIndexStop(0), byIndexParity(m_iParity2));
	//		}
	//	}
	//	else
	//		if (m_iBaudRate2 != 7)	m_iBaudRate2 = 7;	//115200bps
	//	break;
	default: break;
	}
	UpdateData(FALSE);
	*pResult = 0;
}

BOOL CCTS_pCOM_TesterDlg::PreTranslateMessage(MSG* pMsg)
{
	if (pMsg->message == WM_KEYDOWN || pMsg->message == WM_KEYUP)
	{
		if (pMsg->wParam == VK_ESCAPE || pMsg->wParam == VK_RETURN)
		{
			return true;
		}
	}
	return CDialogEx::PreTranslateMessage(pMsg);
}

void CCTS_pCOM_TesterDlg::OnBnClickedButton5()
{
	m_EditReceiveData.SetWindowText(_T(""));
}


void CCTS_pCOM_TesterDlg::OnBnClickedEthernetSend()
{
	UpdateData(TRUE);

	CString msg = _T("");
	CString cmd = _T("");

	m_cEhernetCmd.GetWindowText(cmd);

	msg += (TCHAR)STX;
	msg += cmd;
	msg += (TCHAR)ETX;

	g_pApp_dlg->m_MySocket->Send(msg, strlen(msg));

	Sleep(100);
}


BOOL CCTS_pCOM_TesterDlg::DestroyWindow()
{
	// TODO: 여기에 특수화된 코드를 추가 및/또는 기본 클래스를 호출합니다.

	// TCP 
	g_pApp_dlg->m_MySocket->Close();
	SendMessage(SOCKET_MSGID, MSO_DISCONNECT, 0);

	m_cIPAddr.EnableWindow(TRUE);


	// Rs232 Close 
	UpdateData(TRUE);
	CString _portName, PortName;
	_portName = _T("");
	PortName = _T("");

	m_cSerialPort.GetWindowText(_portName);  // X
	if (g_pApp_dlg->m_ComuPort.m_bConnected == TRUE)
	{
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		m_cSerialPort.EnableWindow(TRUE);
		m_cBaudRate.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPort.ClosePort();
	}
	// else
	// {
	// 	CString msg;
	// 	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	// 	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	// 	AfxMessageBox(msg);
	// }

	m_cSerialPortMaster.GetWindowText(_portName); // Y
	if (g_pApp_dlg->m_ComuPortMaster.m_bConnected == TRUE)
	{
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		m_cSerialPortMaster.EnableWindow(TRUE);
		m_cBaudRate.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPortMaster.ClosePort();
	}
	//else
	//{
	//	CString msg;
	//	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	//	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	//	AfxMessageBox(msg);
	//}

	/// <summary>
	/// Add Port 
	/// </summary>
	m_cSerialPort2.GetWindowText(_portName); // Z
	if (g_pApp_dlg->m_ComuPort2.m_bConnected == TRUE)
	{
		GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
		m_cSerialPort2.EnableWindow(TRUE);
		m_cBaudRate.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPort2.ClosePort();
	}
	//else
	//{
	//	CString msg;
	//	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	//	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	//	AfxMessageBox(msg);
	//}

	m_cSerialPort3.GetWindowText(_portName); // Slope Sensor 
	if (g_pApp_dlg->m_ComuPort3.m_bConnected == TRUE)
	{
		//GetDlgItem(IDC_SERIAL_OPEN2)->EnableWindow(TRUE);
		m_cSerialPort3.EnableWindow(TRUE);
		m_cBaudRate2.EnableWindow(TRUE);
		m_cParity.EnableWindow(TRUE);
		g_pApp_dlg->m_ComuPort3.ClosePort();
	}
	//else
	//{
	//	CString msg;
	//	GetDlgItem(IDC_SERIAL_OPEN)->EnableWindow(TRUE);
	//	PortName.Format(_T("%s 열리지 않았습니다\r\n"), _portName);
	//	AfxMessageBox(msg);
	//}


	if (!(m_BoardLevelMotion_BG01.m_RThread_BG01 == NULL)) {
		WaitForSingleObject(m_BoardLevelMotion_BG01.m_RThread_BG01->m_hThread, 2000); // KIll Thread 
	}
	g_pApp_dlg->m_MySocket->Close();
	SendMessage(SOCKET_MSGID, MSO_DISCONNECT, 0);

	if (INVALID_HANDLE_VALUE == (HANDLE)m_p_Log_File_Save) { // Close
	}
	else{ // Open	
		m_p_Log_File_Save.Close(); // Log Fil Close 
	}
	// WaitForSingleObject(m_BoardLevelMotion_BG01.m_RThread_BG01->m_hThread, 2000); // KIll Thread 	

	return CDialogEx::DestroyWindow();
}


HBRUSH CCTS_pCOM_TesterDlg::OnCtlColor(CDC* pDC, CWnd* pWnd, UINT nCtlColor)
{
	HBRUSH hbr = CDialogEx::OnCtlColor(pDC, pWnd, nCtlColor);

	// TODO:  여기서 DC의 특성을 변경합니다.

	if (pWnd->GetDlgCtrlID() == IDC_EDIT2_VIEW_STEP_DESCRIP)
	{
		pDC->SetTextColor(RGB(255, 0, 0));
	}

	// TODO:  기본값이 적당하지 않으면 다른 브러시를 반환합니다.
	return hbr;
}
