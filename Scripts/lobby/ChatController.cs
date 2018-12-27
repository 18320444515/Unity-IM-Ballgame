using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Net;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Threading;

/**
@author Tianyi
@time 2018.12.28 1:55 a.m.

聊天室的功能控制类，安装在"发送消息的按钮上"
*/
public class ChatController : MonoBehaviour {

    // 显示聊天记录、获取玩家输入、显示玩家信息 的Texts
	public Text messageShow;
	public Text messageInput;
	public Text userList;

    // 记录玩家信息
	private string USERNAME;
	private int USERID;

    // 存放 PersistentData
	private PersistentData pdScript;

	//private int isFirst = 1;
	private string refresh_msg;

    // 记录上一次刷新时 总消息记录的长度（用于减少不必要的刷新计算量，只有新消息到来的时候才刷新）
    private int oldLen = 0;

	//创建 1个客户端套接字 和1个负责监听服务端请求的线程
    public Socket socketClient = null;
    Thread threadClient = null;

	// Use this for initialization
	void Start () {
        // 获取 PersistentData 并提取相关数据
		pdScript = GameObject.Find("data").GetComponent<PersistentData>();
		USERNAME = pdScript.userName;
		USERID = pdScript.id;

        // 显示当前用户的昵称
		userList.text += "\n"+USERNAME;

		// 添加事件，发送聊天信息
		Button btn = gameObject.GetComponent<Button>();
        btn.onClick.AddListener(send);

        // 初始化当前的总聊天信息
        refresh_msg = messageShow.text;

		// 定义一个套字节监听  包含3个参数(IP4寻址协议,流式连接,TCP协议)
        socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // 需要获取文本框中的IP地址
        IPAddress ipaddress = IPAddress.Parse(pdScript.host);
        // 将获取的ip地址和端口号绑定到网络节点endpoint上
        IPEndPoint endpoint = new IPEndPoint(ipaddress, pdScript.ChatPort);
        // 这里客户端套接字连接到网络节点(服务端)用的方法是Connect 而不是Bind
        socketClient.Connect(endpoint);

        // 创建一个线程 用于监听服务端发来的消息
        threadClient = new Thread(RecMsg);
        // 将窗体线程设置为与后台同步
        threadClient.IsBackground = true;
        // 启动线程
        threadClient.Start();
	}
	
	/// <summary>
    /// 接收服务端发来信息的方法
    /// </summary>
    private void RecMsg()
    {
        while (true)//持续监听服务端发来的消息
        {
            // 通过这一句可以看出这个 while 循环并不是一直在运作，而是等待 socket 的新信息
        	Debug.Log("waiting.");

            // 定义一个 200k 的内存缓冲区 用于临时性存储接收到的信息
		    byte[] arrRecMsg = new byte[1024*200];

		    // 将客户端套接字接收到的数据存入内存缓冲区, 并获取其长度
		    int length = socketClient.Receive(arrRecMsg);

		    // 将套接字获取到的字节数组转换为人可以看懂的字符串
		    string strRecMsg = Encoding.UTF8.GetString(arrRecMsg, 0, length);

	    	// 打印
	    	Debug.Log(strRecMsg);
		    string[] rt = dealDTO(strRecMsg); // 处理 DTO (Data Transfer Object)

            // 刷新当前的总聊天信息：将新消息补接上
		    refresh_msg += "\n"+rt[0]+": "+GetCurrentTime()+"\n"+rt[2];

            // 打印标书结束了本次新消息的处理
		    Debug.Log("--");
        }
    }

    /// <summary>
    /// 发送字符串信息到服务端的方法
    /// </summary>
    void send()
    {
		// 获取输入框的内容，并装配 json 格式
        string toSend = "{\"senderName\":\""+USERNAME+"\",\"message\":\""+messageInput.text+"\",\"senderId\":"+USERID+"}\n";
        Debug.Log("sending..."+toSend);

        // 转化并发送
        byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(toSend);
        socketClient.Send(arrClientSendMsg);

    }

    /// <summary>
    /// 获取当前系统时间的方法
    /// </summary>
    /// <returns>当前时间</returns>
    private DateTime GetCurrentTime()
    {
        DateTime currentTime = new DateTime();
        currentTime = DateTime.Now;
        return currentTime;
    }

	// Update is called once per frame
	void FixedUpdate () {

        // 只有聊天记录发送了改动的时候，才更新显示出来的聊天记录
        if(refresh_msg.Length > oldLen){
            oldLen = refresh_msg.Length;
            messageShow.text = refresh_msg;
        }
		
	}

    // 解析 DTO 
	private string[] dealDTO(string stra){
		// string[] rt = new string[3];

		string str_stringLast = "\"";
        string str_intLast = "}";

        string str_message = "\"message\":\"";
        int IndexofE = stra.IndexOf(str_message);
        int IndexofF = stra.IndexOf(str_stringLast, IndexofE + 11);
        string message = stra.Substring(IndexofE + 11, IndexofF - IndexofE - 11);

        string ste_name = "\"senderName\":\"";
        int IndexofA = stra.IndexOf(ste_name);
        int IndexofB = stra.IndexOf(str_stringLast, IndexofA + 14);
        string sname = stra.Substring(IndexofA + 14, IndexofB - IndexofA - 14);

        string str_id = "\"senderId\":";
        int IndexofC = stra.IndexOf(str_id);
        int IndexofD = stra.IndexOf(str_intLast, IndexofC + 11);
        string sid = stra.Substring(IndexofC + 11, IndexofD - IndexofC - 11);

        return new string[] { sname, sid ,message};
	}
}
