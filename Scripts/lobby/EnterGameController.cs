using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Net.Sockets;

/**
@author Tianyi
@time 2018.12.28 1:55 a.m.

进入匹配模式的控制类
*/
public class EnterGameController : MonoBehaviour {

	public Text findText;

	// Use this for initialization
	void Start () {
		// 初始化匹配提示的 Text UI
		findText.text = "";

		// 为按钮添加触击事件
		Button btn = gameObject.GetComponent<Button>();
        btn.onClick.AddListener(go);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	// 跳转到游戏场景 gameBody scene
	void go(){
		GameObject.Find("btn_send").GetComponent<ChatController>().socketClient.Close();
		findText.text = "匹配对手中..";
		SceneManager.LoadScene("gameBody");
	}
}
