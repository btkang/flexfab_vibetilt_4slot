# PIM Library for FlexFab

```
{
  "libraries": [
    {
      "filename": "ff_common.dll",
      "classname": "Cantops.FlexFab.Uart",
      "id": "uart_1",
      "config": {
        "port": "COM1",
        "baudrate": 115200
      }
    },
    {
      "filename": "ff_common.dll",
      "classname": "Cantops.FlexFab.Tcp",
      "id": "tcp_1",
      "config": {
        "ip": "100.100.100.70",
        "port":  12345
      }
    },
    {
      "filename": "ff_dpm.dll",
      "classname": "Cantops.FlexFab.Dpm",
      "id": "dpm",
      "config": {
        "uart": "uart_1"
      }
    },
    {
      "filename": "ff_portremote.dll",
      "classname": "Cantops.FlexFab.Portremote",
      "id": "portremote",
      "config": {
        "tcp": "tcp_1"
      }
    },
    {
      "filename": "ff_pim.dll",
      "classname": "Cantops.FlexFab.Pim",
      "id": "pim",
      "config": {
        "dpm": "dpm",
        "portremote": "portremote"
      }
    }
  ],
  "projects": [
    {
      "id": "prj-001",
      "name": "테스트 프로젝트",
      "library_depencency": [
        "uart_1",
        "tcp_1",
        "dpm",
        "portremote",
        "pim"
      ],
      "procs": [
        {
          "libid": "portremote",
          "id": "proc_02",
          "name": "5V 전원 검사용 릴레이 출력",
          "param": {
            "and": "0xFFFFFF0F",
            "or": "0x00000050"
          }
        },
        {
          "libid": "dpm",
          "id": "proc_01",
          "name": "5V 전원 검사",
          "param": {
            "id": 5,
            "min": 2.4,
            "max": 10.6
          }
        },
        {
          "libid": "dpm",
          "id": "proc_01",
          "name": "5V 전원 검사",
          "param": {
            "id": 5,
            "min": 2.4,
            "max": 10.6
          }
        },
        {
          "libid": "pim",
          "id": "pim_proc_01",
          "param": {
          }
        }
      ]
    }
  ],
  "active_project": "prj-001"
}
```


