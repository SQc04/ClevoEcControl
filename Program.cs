// See https://aka.ms/new-console-template for more information
using ClevoEcControl;
using System.IO.Pipes;
using System.Text;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

class Program
{
    /*
    static Form form = new Form();

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;
    const int SW_SHOW = 5;

    static NotifyIcon notifyIcon;

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    static void ExitApplication()
    {
        notifyIcon.Dispose();
        Environment.Exit(0);  // 0 表示正常退出
    }
    */
    public static int ReadDataToEcdataClient(NamedPipeServerStream server)
    {
        server.WaitForConnection();
        // 读取 fan_id
        byte[] fanIdBytes = new byte[4];
        int bytesRead = server.Read(fanIdBytes, 0, fanIdBytes.Length);
        int fan_id = BitConverter.ToInt32(fanIdBytes, 0);
        return fan_id;
    }
    public static int ReadDataFanDutyClient(NamedPipeServerStream server)
    {
        // 读取 duty
        byte[] dutyBytes = new byte[4];
        int bytesRead = server.Read(dutyBytes, 0, dutyBytes.Length);
        int duty = BitConverter.ToInt32(dutyBytes, 0);
        return duty;
    }
    public static void SendDataToEcdataClient(NamedPipeServerStream server, ECData data,int fan_id)
    {
        byte[] remoteBytes = new byte[] { data.Remote };
        byte[] localBytes = new byte[] { data.Local };
        byte[] fanDutyBytes = new byte[] { data.FanDuty };
        byte[] reserveBytes = new byte[] { data.Reserve };
        server.Write(remoteBytes, 0, remoteBytes.Length);
        server.Write(localBytes, 0, localBytes.Length);
        server.Write(fanDutyBytes, 0, fanDutyBytes.Length);
        server.Write(reserveBytes, 0, reserveBytes.Length);
        Console.WriteLine("Fan" + fan_id.ToString() + "    " + $"Temp: {data.Remote}" + "    " + $"Local: {data.Local}" + "    " + $"FanDuty: {data.FanDuty}");
        server.Dispose();
    }
    public static void SendDataToClient(NamedPipeServerStream server, byte[] data)
    {
        server.WaitForConnection();
        server.Write(data, 0, data.Length);
        server.Dispose();
    }

    public static void SendFanRpmToClient(string pipeName, Func<int> getFanRpm)
    {
        using (var server = new NamedPipeServerStream(pipeName))
        {
            server.WaitForConnection();
            int fanRpm = getFanRpm();
            byte[] fanRpmBytes = BitConverter.GetBytes(fanRpm);
            SendDataToClient(server, fanRpmBytes);
        }
    }

    public static void Main(string[] args)
    {
        //notifyIcon = new NotifyIcon();
        //notifyIcon.Icon = new Icon("FSFSoftH.ico");
        //notifyIcon.Visible = true;

        //var contextMenu = new ContextMenuStrip();
        //var exitMenuItem = new ToolStripMenuItem("退出");

        //exitMenuItem.Click += (sender, e) => ExitApplication();

        //contextMenu.Items.Add(exitMenuItem);

        //notifyIcon.ContextMenuStrip = contextMenu;
        /*
        Console.CancelKeyPress += (sender, e) =>
        {
            notifyIcon.Dispose();
        };
        */
        Process[] processes = Process.GetProcessesByName("ClevoEcControlWatchDog");
        if (processes.Length == 0)
        {
            // 如果 ClevoEcControl.exe 没有运行，那么启动它
            Process.Start("ClevoEcControlWatchDog.exe");
            //Process.Start("D:\\I\\FAIRING STUDIO\\FSSoftware\\FSGarvityTool\\ClevoEcControl\\bin\\Debug\\net6.0-windows10.0.22000.0\\ClevoEcControl.exe");
        }

        bool isInitialized = ClevoEcInfo.InitIo();

        if (!isInitialized)
        {
            Console.WriteLine("Failed to initialize IO.");
            return;
        }
        else
        {
            Console.WriteLine(isInitialized.ToString());
            Console.WriteLine("Server is running");
        }

        var handlers = new Dictionary<string, Action<string>>
        {
            { "ClevoEcPipeTestConnect", server =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("ClevoEcPipeTestConnect");
                    Console.ForegroundColor = ConsoleColor.White;

                }
            },
            { "ClevoEcPipeInitTo", server =>
                {
                    byte[] infoBytes = BitConverter.GetBytes(isInitialized);
                    Console.WriteLine("ClevoEcPipeInitTo: " + isInitialized.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, infoBytes);
                }
            },
            { "ClevoEcPipeVersion", server =>
                {
                    string version = ClevoEcInfo.GetECVersion();
                    byte[] versionBytes = Encoding.UTF8.GetBytes(version);
                    Console.WriteLine("ClevoEcPipeVersion: " + version.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, versionBytes);
                }
            },
            { "ClevoEcPipeFanNum", server =>
                {
                    int fanCount = ClevoEcInfo.GetFanCount();
                    byte[] fanCountBytes = BitConverter.GetBytes(fanCount);
                    Console.WriteLine("ClevoEcPipeVersion: " + fanCount.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, fanCountBytes);
                }
            },
            { "ClevoEcPipeCpuFanRpm", server =>
                {
                    int cpuFanRpm = ClevoEcInfo.GetCpuFanRpm();
                    byte[] cpuFanRpmBytes = BitConverter.GetBytes(cpuFanRpm);
                    Console.WriteLine("CpuFanRpm: " + cpuFanRpm.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, cpuFanRpmBytes);
                }
            },
            { "ClevoEcPipeGpuFanRpm", server =>
                {
                    int gpuFanRpm = ClevoEcInfo.GetGpuFanRpm();
                    byte[] gpuFanRpmBytes = BitConverter.GetBytes(gpuFanRpm);
                    Console.WriteLine("GpuFanRpm: " + gpuFanRpm.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, gpuFanRpmBytes);
                }
            },
            { "ClevoEcPipeGpu1FanRpm", server =>
                {
                    int gpu1FanRpm = ClevoEcInfo.GetGpu1FanRpm();
                    byte[] gpu1FanRpmBytes = BitConverter.GetBytes(gpu1FanRpm);
                    Console.WriteLine("GpuFanRpm: " + gpu1FanRpm.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, gpu1FanRpmBytes);
                }
            },
            { "ClevoEcPipeX72FanRpm", server =>
                {
                    int x72FanRpm = ClevoEcInfo.GetX72FanRpm();
                    byte[] x72FanRpmBytes = BitConverter.GetBytes(x72FanRpm);
                    Console.WriteLine("GpuFanRpm: " + x72FanRpm.ToString());
                    var server1 = new NamedPipeServerStream(server);
                    SendDataToClient(server1, x72FanRpmBytes);
                }
            },
            { "ClevoEcPipeTempFanDuty",server =>
                {
                    var server1 = new NamedPipeServerStream(server);
                    int fan_id = ReadDataToEcdataClient(server1);
                    ECData data = ClevoEcInfo.GetTempFanDuty(fan_id);
                    SendDataToEcdataClient(server1, data, fan_id);
                }
            },
            { "ClevoEcPiceSetFanDuty",server =>
                {
                    var server1 = new NamedPipeServerStream(server);
                    int fan_id = ReadDataToEcdataClient(server1);
                    int duty = ReadDataFanDutyClient(server1);
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("Fan" + fan_id.ToString()+ "RpmDuty: " + duty.ToString());
                    Console.ForegroundColor = ConsoleColor.White;
                    ClevoEcInfo.SetFanDuty(fan_id, duty);
                    server1.Dispose();
                }
            },
            { "ClevoEcPiceSetFanAuto",server =>
                {
                    var server1 = new NamedPipeServerStream(server);
                    int fan_id = ReadDataToEcdataClient(server1);
                    Console.WriteLine("SetFanAuto: Fan" + fan_id.ToString());
                    ClevoEcInfo.SetFANDutyAuto(fan_id);
                    server1.Dispose();
                }
            }
        };
        Console.WriteLine("Start Clevo Fan Control V1.0");
        while (isInitialized) // 死循环开始
        {
            using (var server = new NamedPipeServerStream("ClevoEcPipe"))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Wait request...");
                Console.ForegroundColor = ConsoleColor.White;
                server.WaitForConnection();
                // 读取请求类型
                byte[] requestTypeBytes = new byte[128];
                int bytesRead = server.Read(requestTypeBytes, 0, requestTypeBytes.Length);
                string requestType = Encoding.UTF8.GetString(requestTypeBytes, 0, bytesRead);
                Console.WriteLine("Request is " + requestType);
                // 处理请求
                if (handlers.TryGetValue(requestType, out var handler))
                {
                    handler(requestType);
                }
                else
                {
                    Console.WriteLine($"Unknown request type: {requestType}");
                }
            }
        }
        //notifyIcon.Dispose();
    }


}
