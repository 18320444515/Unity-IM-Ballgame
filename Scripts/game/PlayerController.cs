using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using UnityEngine.Networking;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using UnityEngine.SceneManagement;
using System;


/**
@author Tianyi
@time 2018.12.28 1:55 a.m. 

这是控制游戏小球的类，主要业务有：
0. 建立连接，使对战双方都能获取对手的信息
1. 计时并使对战双方轮流行动
2. 计分、显示分数、显示轮到谁、显示时间、显示提示性元素例如颜色
*/
// TODO 黑球失败业务
public class PlayerController : MonoBehaviour {

	// These are the public values between two players
	public float speed; // 影响速度的变量
	private Rigidbody rb; // 小球刚体
	public Text timeText; // 显示时间的Text

	// 用来存放刷新显示的当前计时
	private float time = 15.0f;
	// 用于存放刷新显示的等待计时
	private float wait = 0.0f;
	// 用于记录结点性的临时时间点
	private long zero = 0;

	// These are the values about players
	public Text countTextA; // 显示玩家A名称
	public Text countTextB; // 显示玩家B名称
	public Text turnText; // 显示轮到谁
	public Text winText; // 显示谁赢了
	private int[] count = new int[2]; // 记分
	private string[] players = new string[2]{"Hello","World"}; // 记录双方昵称
	private int turn; // 记录当前轮到谁了
	
	// These are the ball-to-collide objects
	public GameObject ballBlack;
	public GameObject ballRed;
	public GameObject ballPink;
	public GameObject ballPurple;
	public GameObject ballIndigo;
	public GameObject ballBlue;
	public GameObject ballTeal;
	public GameObject ballGreen;
	public GameObject ballYellow;
	public GameObject ballAmber;
	public GameObject ballOrange;
	public GameObject ballBrown;
	public GameObject ballGrey;

	// The persistent data object that won't be destory when the scene changed.
	private PersistentData pdScript;
	private int index = 0; // 记录玩家进入房间的序号
	private int hereId = 0; // 此端玩家的 id
	private int thereId = 0; // 彼端玩家的 id 用于获取对手的信息
	private int isFirst = 1; // 这里不要混淆，这个只是防止其他游戏房间的服务器消息带来的不良影响，见用法

	// 记录玩家是否是第一个行动的人
	private int isfirstTurn = 1;
	// 创建 1个客户端套接字
    Socket socketClient = null;
    // 线程对象，用于持续监听 socket 新信息
	Thread threadClient = null;

	// 这两个是会动态刷新的 水平、垂直 移动指数
    private float moveHorizontal;
    private float moveVertical;

    /**
    Start 生命周期
    作用：
    1. 匹配对手，原理是按进入匹配模式的序号奇偶进行匹配，
       奇数用户需要等待序号为 自己序号+1 的偶数用户"上钩"，上钩后可以获取到对方的id，
       偶数玩家可以直接获取正在等待自己的序号为 自己序号-1 的用户的id，并双方正式开始游戏
    2. 初始化对战双方的信息并同步对战信息
    3. 启动线程持续监听 socket 新消息，并刷新移动量（注意：玩家无法直接控制小球，直接控制权完全交给 socket 服务器）
    */
	void Start()
	{
		// this is the prevent scene dataObject.
		pdScript = GameObject.Find("data").GetComponent<PersistentData>();
		hereId = pdScript.id; // get this sind player's id
		zero = GetTimestamp(); // initial the zero time point
		// Assign the Rigidbody component to our private rb variable
		rb = GetComponent<Rigidbody>();


		// 定义一个套字节监听  包含3个参数(IP4寻址协议,流式连接,TCP协议)
        socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // 从配置获取 socket 服务主机的 ip
        IPAddress ipaddress = IPAddress.Parse(pdScript.host);

        // 从配置获取 socket 服务的端口
        IPEndPoint endpoint = new IPEndPoint(ipaddress, pdScript.FightPort);

        // 这里客户端套接字连接到网络节点(服务端)用的方法是Connect 而不是Bind TODO 这有什么区别？
        socketClient.Connect(endpoint);

        while(true){ // 其实这个循环可以可无，因为只有一次就被强制跳出了

        	// 定义一个 200bit 的内存缓冲区 用于临时性存储接收到的信息
		    byte[] arrRecMsg = new byte[200];

		    // 将客户端套接字接收到的数据存入内存缓冲区, 并获取其长度
		    int length = socketClient.Receive(arrRecMsg);

		    // 将套接字获取到的字节数组转换为人可以看懂的字符串
		    string strRecMsg = Encoding.UTF8.GetString(arrRecMsg, 0, length);

		    // 打印测试
	    	Debug.Log(strRecMsg);
	    	Debug.Log("--1");

	    	// 原本为了防止防止其他游戏房间发来的 以0开头 的信息造成混乱才设置的 isFirst，后来break了，可删去 isFirst
	    	if(isFirst == 1 && strRecMsg.Split('_')[0].Equals("0")){
	    		isFirst = 0;
	    		
	    		// 获取当前进入房间的序号，将用于匹配对手以及对战信息的获取
	    		index = int.Parse(strRecMsg.Split('_')[1]);

	    		// 判断进入房间序号的奇偶，选用不同规则匹配对手
	    		if(index % 2 != 0){
	    			isfirstTurn = 1 // 奇数用户先手行动

	    			// 带上自己的 账号id 回应服务器
	    			send(hereId+"", index+"", "againWithId");

	    			// 不断监听，直至匹配成功，成功后正式启动游戏对战系统
	    			while(true){

	    				// 定义一个1M的内存缓冲区 用于临时性存储接收到的信息
					    byte[] arrRecMsg2 = new byte[200];

					    // 将客户端套接字接收到的数据存入内存缓冲区, 并获取其长度
					    int length2 = socketClient.Receive(arrRecMsg2);

					    // 将套接字获取到的字节数组转换为人可以看懂的字符串
					    string strRecMsg2 = Encoding.UTF8.GetString(arrRecMsg2, 0, length2);

				    	// 打印测试
				    	Debug.Log(strRecMsg2);
				    	Debug.Log("--2");

				    	// 匹配成功的条件是：第一个数字为房间 id 即奇数用户的序号
				    	if(int.Parse(strRecMsg2.Split('_')[0])==index){
				    		thereId = int.Parse(strRecMsg2.Split('_')[2]); // 记录对手的 id
				    		break;
				    	}

	    			}

	    		}else{
	    			isfirstTurn = 0; // 偶数用户后手行动

	    			// 带上自己的 账号id 回应服务器
	    			send(hereId+"", index+"", "againWithId");
	    			index = index - 1;
	    			while(true){
	    				// 定义一个1M的内存缓冲区 用于临时性存储接收到的信息
					    byte[] arrRecMsg2 = new byte[200];

					    // 将客户端套接字接收到的数据存入内存缓冲区, 并获取其长度
					    int length2 = socketClient.Receive(arrRecMsg2);

					    // 将套接字获取到的字节数组转换为人可以看懂的字符串
					    string strRecMsg2 = Encoding.UTF8.GetString(arrRecMsg2, 0, length2);
				    	
				    	// 打印测试
				    	Debug.Log(strRecMsg2);
				    	Debug.Log("--22");

				    	// 匹配成功的条件是：第一个数字为房间 id 即奇数用户的序号
				    	if(int.Parse(strRecMsg2.Split('_')[0])==index){
				    		thereId = int.Parse(strRecMsg2.Split('_')[1]); // 记录对手的 id
				    		break; // 匹配完成
				    	}
	    			}
	    		}
	    	}

	    	break; // 跳出并正式启动对战模式

        }

        players[1-isfirstTurn] = pdScript.userName; // 更新本端玩家的名称

		// Set the count to zero 
		count[0] = 0;
		count[1] = 0;
		// 从第一用户开始，开始前假装是第二个用户刚结束（为了加入开始前的提示）
		turn = 1;
		
		// Set the text property of our Win Text UI to an empty string, making the 'You Win' (game over message) blank
		winText.text = "";
		PostURL("https://zengtianyi.top/ball/player/one",thereId+""); // 获取对手的信息

		// 创建一个线程 用于监听服务端发来的消息
        threadClient = new Thread(Fighting);

        // 将窗体线程设置为与后台同步
        threadClient.IsBackground = true;

        // 启动线程
        threadClient.Start();
	}

	// 在玩家后台运行的 socket 新信息监听线程
	void Fighting(){
		while(true){ // 不断监听

			// 打印测试表示又进入的监听状态，而不是出错
			Debug.Log("waiting..");

			//定义一个 200bit 的内存缓冲区 用于临时性存储接收到的信息
		    byte[] arrRecMsg = new byte[200];
		    //将客户端套接字接收到的数据存入内存缓冲区, 并获取其长度
		    int length = socketClient.Receive(arrRecMsg);
		    //将套接字获取到的字节数组转换为人可以看懂的字符串
		    string strRecMsg = Encoding.UTF8.GetString(arrRecMsg, 0, length).Split('\n')[0]+"\n";
			
			// 打印测试
			Debug.Log(strRecMsg);

			// 以房间号（奇数玩家进入房间的序号为筛选房间内信息的条件，避免其他）
			if(int.Parse(strRecMsg.Split('_')[0])==index){
				// 为了防止两边的用户信息不同步，唯一地由服务器来控制小球
				moveHorizontal = float.Parse(strRecMsg.Split('_')[1]);
				moveVertical = float.Parse(strRecMsg.Split('_')[2]);
				
			}

			// 打印测试，表示结束了本次的新消息
			Debug.Log("--");
		}
		
	}

	// 携程发送Post请求
	private void PostURL(string url, string id)
    {
        //定义一个表单
        WWWForm form = new WWWForm();
        //给表单添加值
        form.AddField("id", id);
        WWW data = new WWW(url, form);
        StartCoroutine(Request(data));
    }

	// 携程
	private IEnumerator Request(WWW data)
    {
        yield return data;
        if (string.IsNullOrEmpty(data.error))
        {
			Debug.Log(data.text);

	        string[] rt = dealDTO(data.text);

	        if(rt[2]=="200"){
	        	players[0+isfirstTurn] = rt[1];
	        	//Debug.Log(players[0+isfirstTurn]);
	        	// Run the SetCountText function to update the UI (see below)
				SetCountText ();
	        }else{
	        	Debug.LogError("Not 200");
	        }

			// foreach(string per in rt){
			// 	Debug.Log(per);
			// }
        }
        else
        {
            Debug.LogError(data.error);
        }
	}

	// 解析 DTO (Data Transfer Object)，返回 数组[3]
	private string[] dealDTO(string stra){
		// string[] rt = new string[3];

		string str_stringLast = "\"";
        string str_intLast = ",";

        string str_resultCode = "\"resultCode\":";
        int IndexofE = stra.IndexOf(str_resultCode);
        int IndexofF = stra.IndexOf(str_intLast, IndexofE + 13);
        string code = stra.Substring(IndexofE + 13, IndexofF - IndexofE - 13);

        string str_id = "\"id\":";
        int IndexofA = stra.IndexOf(str_id);
        int IndexofB = stra.IndexOf(str_intLast, IndexofA + 5);
        string id = stra.Substring(IndexofA + 5, IndexofB - IndexofA - 5);

        string str_name = "\"name\":\"";
        int IndexofC = stra.IndexOf(str_name);
        int IndexofD = stra.IndexOf(str_stringLast, IndexofC + 8);
        string player_name = stra.Substring(IndexofC + 8, IndexofD - IndexofC - 8);

        return new string[] { id, player_name ,code};
	}

	/// <summary>
    /// 发送内容到 socket 服务端的方法，匹配对手的时候会用到
    /// </summary>
    void send(string here, string idx, string c)
    {
		// 将输入的内容字符串转换为机器可以识别的字节数组
        string toSend = "{\"senderName\":\""+c+"\",\"message\":\""+idx+"\",\"senderId\":"+here+"}\n";

        Debug.Log("sending..."+toSend); // 打印测试确保无误

        byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(toSend);

        socketClient.Send(arrClientSendMsg);
    }


    /// <summary>
    /// 发送战斗数据
    /// </summary>
    void sendHV(string h, string v)
    {
		//将输入的内容字符串转换为机器可以识别的字节数组
        string toSend = index+"_"+h+"_"+v+"\n";

        Debug.Log("sending..."+toSend);

        byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(toSend);

        socketClient.Send(arrClientSendMsg);

    }

    void Update(){ // 使移动回归静止，当且仅当服务器有新消息的时候才更新、并移动
    	moveHorizontal = 0.0f;
		moveVertical = 0.0f;
    }

	void FixedUpdate ()
	{
		// 若主角小球掉落出界，则本端玩家失败，对手胜利
		if(transform.position.y < -2){
			// Stop all ball
			ballBlack.GetComponent<Rigidbody>().Sleep();
			ballRed.GetComponent<Rigidbody>().Sleep();
			ballPink.GetComponent<Rigidbody>().Sleep();
			ballPurple.GetComponent<Rigidbody>().Sleep();
			ballIndigo.GetComponent<Rigidbody>().Sleep();
			ballBlue.GetComponent<Rigidbody>().Sleep();
			ballTeal.GetComponent<Rigidbody>().Sleep();
			ballGreen.GetComponent<Rigidbody>().Sleep();
			ballYellow.GetComponent<Rigidbody>().Sleep();
			ballAmber.GetComponent<Rigidbody>().Sleep();
			ballOrange.GetComponent<Rigidbody>().Sleep();
			ballBrown.GetComponent<Rigidbody>().Sleep();
			ballGrey.GetComponent<Rigidbody>().Sleep();
			gameObject.SetActive (false);

			// another player win the Game
			winText.text = players[1-turn] + " Win!";

			// back to the lobby
			socketClient.Close();
			SceneManager.LoadScene("lobby");
		}

		/**
		时间区间与业务如下：
		time [0 , 10] 轮到的玩家有权力控制小球的移动（有权力发送请求）
		time [10, 15] 轮到的玩家不能再控制小球了，但是会延长操控效果，持续5s
		wait [0 , 5 ] 更换行动玩家，缓冲5s，并UI提示
		*/
		if(time<10)
		{
			if(isfirstTurn!=turn){

				// 获取输入的移动量，准备发送到服务器
				float sendHorizontal = Input.GetAxis ("Horizontal");
				float sendVertical = Input.GetAxis ("Vertical");

				// 为了减少服务的负担，设置一些条件才进行发送
				if( ((int)10*time)%10==5 || sendVertical != 0 || sendHorizontal != 0 )
				{
					// 向服务器发送移数据
					sendHV(sendHorizontal+"",sendVertical+"");
				}
				
			}

			// 更新实时的小球数据（其中 #1 和 #3 仅由服务器的新消息控制改变，且会刷新为0）
			Vector3 movement = new Vector3 (moveHorizontal, 0.0f, moveVertical);

			// Add a physical force to our Player rigidbody using our 'movement' Vector3 above, 
			// multiplying it by 'speed' - our public player speed that appears in the inspector
			rb.AddForce (movement * speed);

			// 刷新计时器
			time = (float)( ( (DateTime.Now.Ticks / 10000) - zero ) / 1000.0f) + 0.001f;
			timeText.text = string.Format("{0:N3}",time).ToString();
		}
		else if(time<15) // [10，15] 是延长玩家行为效果的缓冲（无法控制，但可以保持运动状态）
		{
			// UI效果
			timeText.color = Color.red;

			// 刷新计时器
			time = (float)( ( (DateTime.Now.Ticks / 10000) - zero ) / 1000.0f) + 0.001f;
			timeText.text = string.Format("{0:N3}",time).ToString();
		}
		else
		{
			if(wait == 0.0f) // 下一轮开始前的缓冲
			{
				// 停止运动
				rb.Sleep();
				ballBlack.GetComponent<Rigidbody>().Sleep();
				ballRed.GetComponent<Rigidbody>().Sleep();
				ballPink.GetComponent<Rigidbody>().Sleep();
				ballPurple.GetComponent<Rigidbody>().Sleep();
				ballIndigo.GetComponent<Rigidbody>().Sleep();
				ballBlue.GetComponent<Rigidbody>().Sleep();
				ballTeal.GetComponent<Rigidbody>().Sleep();
				ballGreen.GetComponent<Rigidbody>().Sleep();
				ballYellow.GetComponent<Rigidbody>().Sleep();
				ballAmber.GetComponent<Rigidbody>().Sleep();
				ballOrange.GetComponent<Rigidbody>().Sleep();
				ballBrown.GetComponent<Rigidbody>().Sleep();
				ballGrey.GetComponent<Rigidbody>().Sleep();
				// 开始运动
				ballBlack.GetComponent<Rigidbody>().WakeUp();
				ballRed.GetComponent<Rigidbody>().WakeUp();
				ballPink.GetComponent<Rigidbody>().WakeUp();
				ballPurple.GetComponent<Rigidbody>().WakeUp();
				ballIndigo.GetComponent<Rigidbody>().WakeUp();
				ballBlue.GetComponent<Rigidbody>().WakeUp();
				ballTeal.GetComponent<Rigidbody>().WakeUp();
				ballGreen.GetComponent<Rigidbody>().WakeUp();
				ballYellow.GetComponent<Rigidbody>().WakeUp();
				ballAmber.GetComponent<Rigidbody>().WakeUp();
				ballOrange.GetComponent<Rigidbody>().WakeUp();
				ballBrown.GetComponent<Rigidbody>().WakeUp();
				ballGrey.GetComponent<Rigidbody>().WakeUp();
				rb.WakeUp();

				// 时间变颜色、提示下一个操作的用户做好准备
				timeText.color = Color.yellow;
				turnText.color = Color.yellow;
				turn = 1 - turn;
				zero = (long)(DateTime.Now.Ticks / 10000); // 刷新计时结点，为 wait 计时
				SetCountText();
			}

			// 刷新 wait 时间
			wait = (float)( ( (DateTime.Now.Ticks / 10000) - zero ) / 1000.0f) + 0.001f;
			timeText.text = string.Format("{0:0}",(int)(5-wait+1));

			if( wait > 5)    // 5秒静止缓冲结束
			{
				// 取消高亮UI
				turnText.color = Color.white;
				timeText.color = Color.white;
				wait = 0.0f; // 停止静止缓冲
				time = 0.0f; // 重新开始新一轮
				zero = (long)(DateTime.Now.Ticks / 10000); // 刷新计时结点，为 time 计时
			}
		}
		
	}


	/**
	* 这是 Roll-a-ball 的代码，注释写得很好
	*/
	// When this game object intersects a collider with 'is trigger' checked, 
	// store a reference to that collider in a variable named 'other'..
	// void OnTriggerEnter(Collider other) 
	// {
	// 	rb.AddForce (new Vector3 (0.0f, 0.0f, 0.0f));
	// 	// ..and if the game object we intersect has the tag 'Pick Up' assigned to it..
	// 	if (other.gameObject.CompareTag ("PickUp"))
	// 	{
	// 		// Make the other game object (the pick up) inactive, to make it disappear
	// 		other.gameObject.SetActive (false);

	// 		// Add one to the score variable 'count'
	// 		count[turn] += 1;

	// 		// Run the 'SetCountText()' function (see below)
	// 		SetCountText ();
	// 	}
	// }


	// 刷新计分显示，并判断是否已经有一方胜出
	// Create a standalone function that can update the 'countText' UI and check if the required amount to win has been achieved
	void SetCountText()
	{
		// 更新计分显示
		// Update the text field of our 'countText' variable
		countTextA.text = "Player " + players[0] + ": " + count[0].ToString ();
		countTextB.text = "Player " + players[1] + ": " + count[1].ToString ();
		turnText.text = "Turn to: " + players[turn];

		// 判断是否已经有一方胜出
		// Check if our 'count' is equal to or exceeded 7
		if (count[turn] >= 7) 
		{
			// 停止运动
			rb.Sleep();
			ballBlack.GetComponent<Rigidbody>().Sleep();
			ballRed.GetComponent<Rigidbody>().Sleep();
			ballPink.GetComponent<Rigidbody>().Sleep();
			ballPurple.GetComponent<Rigidbody>().Sleep();
			ballIndigo.GetComponent<Rigidbody>().Sleep();
			ballBlue.GetComponent<Rigidbody>().Sleep();
			ballTeal.GetComponent<Rigidbody>().Sleep();
			ballGreen.GetComponent<Rigidbody>().Sleep();
			ballYellow.GetComponent<Rigidbody>().Sleep();
			ballAmber.GetComponent<Rigidbody>().Sleep();
			ballOrange.GetComponent<Rigidbody>().Sleep();
			ballBrown.GetComponent<Rigidbody>().Sleep();
			ballGrey.GetComponent<Rigidbody>().Sleep();
			// 轮到的玩家胜出
			winText.text = players[turn] + " Win!";

			// 返回 lobby
			socketClient.Close();
			SceneManager.LoadScene("lobby");
		}
	}

	// 提供给小球脚本调用，更新计分数据
	void sayHello(){
		// Debug.Log("hello");
		count[turn] += 1;
		SetCountText ();
	}

	/// <summary>
    /// 获取时间戳
    /// </summary>
    /// <returns></returns>
    long GetTimestamp()
    {

        // System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
        // long ts = (DateTime.UtcNow.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
        // return ts;

        return (long)(DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
    }

}
