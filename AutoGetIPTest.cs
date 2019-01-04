using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System;
using System.Text;

public class AutoGetIPTest : MonoBehaviour {
    string askString = "wisdom udp connet ask:"; //编辑框文字
    public TextAsset configNet;
    string[] configList;

    //以下默认都是私有的成员
    Socket socket; //目标socket
    EndPoint serverEnd; //服务端
    IPEndPoint ipEnd; //服务端端口
    string recvStr; //接收的字符串
    string sendStr; //发送的字符串
    byte[] recvData = new byte[1024]; //接收的数据，必须为字节
    byte[] sendData = new byte[1024]; //发送的数据，必须为字节
    int recvLen; //接收的数据长度
    Thread connectThread; //连接线程
    string X01P1RT_DATA;
    static float heartBeat = 1f;
    int xintiao_timer = 1;
    int RT_TIME_CN = 20;
    string RemoteHost = ""; //服务器IP
    int RemotePort = 5110; //服务器的端口
    bool Udp_connet_state = false;
    bool receiveMessage = false;  //判断是否收到消息
    //初始化
    void InitSocket()
    {
        //定义连接的服务器ip和端口，可以是本机ip，局域网，互联网
        ipEnd = new IPEndPoint(IPAddress.Parse(RemoteHost), RemotePort);
        //定义套接字类型,在主线程中定义
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //定义服务端
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        serverEnd = (EndPoint)sender;

        //建立初始连接，这句非常重要，第一次连接初始化了serverEnd后面才能收到消息
        print("首次发送连接请求");
        SocketSend(askString);

        //开启一个线程连接，必须的，否则主线程卡死
        connectThread = new Thread(new ThreadStart(SocketReceive));
        connectThread.Start();
    }

    void SocketSend(string sendStr)
    {
        //清空发送缓存
        sendData = new byte[1024];
        //数据类型转换
        sendData = Encoding.ASCII.GetBytes(sendStr);
        //发送给指定服务端
        socket.SendTo(sendData, sendData.Length, SocketFlags.None, ipEnd);
    }

    //服务器接收
    void SocketReceive()
    {
        //进入接收循环
        while (true)
        {
            //对data清零
            recvData = new byte[1024];
            //获取客户端，获取服务端端数据，用引用给服务端赋值，实际上服务端已经定义好并不需要赋值
            recvLen = socket.ReceiveFrom(recvData, ref serverEnd);
            print("message from: " + serverEnd.ToString()); //打印服务端信息
            //输出接收到的数据
            recvStr = Encoding.ASCII.GetString(recvData, 0, recvLen);
            print(recvStr);
            if (recvStr.Contains(@"wisdom udp connet ok --->IP:"))
            {
                recvStr = recvStr.Replace("wisdom udp connet ok --->IP:", "");
                if (VerifyIPAddressAndPort(recvStr, RemotePort))
                {
                    RemoteHost = recvStr;
                    ipEnd = null;
                    ipEnd = new IPEndPoint(IPAddress.Parse(RemoteHost), RemotePort);
                    Udp_connet_ask = true;
                }
            }
            else if (Udp_connet_state)
            {
                receiveMessage = true;
            }
        }
    }

    //连接关闭
    void SocketQuit()
    {
        //关闭线程
        if (connectThread != null)
        {
            connectThread.Interrupt();
            connectThread.Abort();
        }
        //最后关闭socket
        if (socket != null)
            socket.Close();
        print("连接已关闭");
    }

    // Use this for initialization
    void Start () {
        print("程序入口");
        configList = configNet.text.Split('|');
        RemotePort = int.Parse(configList[0]);//端口
        askString = askString + configList[1];//服务器ID
        X01P1RT_DATA = configList[2];
        print(configList[0]);//端口
        print(configList[1]);//服务器ID
        print(configList[2]);//心跳消息
    }

    void OnGUI()
    {
        askString = GUI.TextField(new Rect(10, 10, 220, 20), askString);
        if (GUI.Button(new Rect(10, 40, 220, 20), "send"))
            SocketSend(askString);
    }

    void Update ()
    {
        heartBeat -= Time.deltaTime;
        if (heartBeat < 0)
        {
            heartBeat = xintiao_timer;
            if (Udp_connet_state == false) Get_Udp_Ask();
            if (Udp_connet_state == true)
            {
                print("心跳数据->"+ RemoteHost+':'+ RemotePort.ToString());
                SocketSend(X01P1RT_DATA);
            }
        }
        transform.Rotate(0, 1, 0);
        if (receiveMessage)
        {
            receiveMessage = false;
            ProcessMessage(recvStr);
        }
    }

    void OnApplicationQuit()
    {
        SocketQuit();
    }

    #region Get_Udp_Ask
    /// <summary>
    /// 
    /// </summary>
    int Get_Udp_Ask_step = 0;
    bool Udp_connet_ask = false;
    public bool Get_Udp_Ask()
    {
        switch (Get_Udp_Ask_step)
        {
            case 0://获取本机IP信息
                print("获取本机IP地址中……");
                xintiao_timer = 1;
                RemoteHost = GetLocalIP();
                Get_Udp_Ask_step = 1;
                break;
            case 1://获取IP结果
                if (VerifyIPAddressAndPort(RemoteHost, RemotePort))
                {
                    print("获取本机IP地址："+ RemoteHost);
                    string[] myIPList = RemoteHost.Split('.');
                    RemoteHost = myIPList[0] + '.' + myIPList[1] + '.' + myIPList[2] + ".255";
                    Get_Udp_Ask_step = 2;
                }
                else
                {
                    print("获取本机IP地址失败！");
                    Get_Udp_Ask_step = 0;
                }
                break;
            case 2://初始化socket
                Udp_connet_ask = false;
                InitSocket(); //在这里初始化
                Get_Udp_Ask_step = 3;
                break;
            case 3://是否连接成功
                if (Udp_connet_ask)
                {
                    print("服务器IP:" + RemoteHost);
                    heartBeat = xintiao_timer = RT_TIME_CN;
                    Get_Udp_Ask_step = 0;
                    Udp_connet_state = true;
                    print("连接成功");
                    return true;
                }
                else
                {
                    print("连接失败");
                    Get_Udp_Ask_step = 4;
                }
                break;
            case 4://发送连接请求
                Udp_connet_ask = false;
                print("再发送连接请求中……");
                SocketSend(askString);
                Get_Udp_Ask_step = 3;
                break;
            default:
                Get_Udp_Ask_step = 0;
                return false;
        }
        return false;
    }
    #endregion //Get_Udp_Ask

    /// <summary>
    /// 获取本机IP地址
    /// </summary>
    /// <returns>本机IP地址</returns>
    string GetLocalIP()
    {
        try
        {
            string HostName = Dns.GetHostName(); //得到主机名
            IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
            for (int i = 0; i<IpEntry.AddressList.Length; i++)
            {
                //从IP地址列表中筛选出IPv4类型的IP地址
                //AddressFamily.InterNetwork表示此IP为IPv4,
                //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    return IpEntry.AddressList[i].ToString();
                }
            }
            return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    #region VerifyIPAddressAndPort
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool VerifyIPAddressAndPort(string check_ip, int check_port)
    {
        IPAddress ipaddress = null;
        UInt16 port = 0;

        bool b = false;
        b = IPAddress.TryParse(check_ip, out ipaddress);
        if (!b)
        {
            return false;
        }
        b = UInt16.TryParse(check_port.ToString(), out port);
        if (!b)
        {
            return false;
        }

        return true;
    }
    #endregion //VerifyIPAddressAndPort

    //处理消息，控制大楼模型操作
    public void ProcessMessage(string str)
    {
        string[] retStr = str.Split('_');
        int id = int.Parse(retStr[0]);

        //判断消息调用控制模型
        if (id == 111)
        {
            //控制模型动作脚本调用区域
            SphereCtr.Instance.SetTrue();
        }
        if (id == 222)
        {
            SphereCtr.Instance.SetFalse();
        }

    }
}
