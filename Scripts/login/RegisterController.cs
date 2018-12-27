using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
@author Tianyi
@time 2018.12.28 1:55 a.m.

用户注册的控制类
*/
public class RegisterController : MonoBehaviour {

	public Text inputName;
	public Text inputPassword;

	void Start () {
		// 给按钮添加事件
		Button btn = gameObject.GetComponent<Button>();
        btn.onClick.AddListener(register);
	}

	void register()
    {
    	// Debug.Log("register, name="+inputName.text+", password="+inputPassword.text);

    	// 将输入框的值作为参数发送
        PostURL("https://zengtianyi.top/ball/player/add", inputName.text, inputPassword.text); 
    }
	
	// 调用协程发送 post 请求
	private void PostURL(string url, string name, string password)
    {
        //定义一个表单
        WWWForm form = new WWWForm();
        //给表单添加值
        form.AddField("name", name);
		form.AddField("password", password);
        WWW data = new WWW(url, form);
        StartCoroutine(Request(data));
    }
	
	// 协程
	private IEnumerator Request(WWW data)
    {
        yield return data;
        if (string.IsNullOrEmpty(data.error))
        {
			Debug.Log(data.text);

			string[] rt = dealDTO(data.text);

			// 若不成功
			if(rt[2] != "200"){
				// 打印错误信息
				Debug.LogError(data.text); 
	        }
        }
        else
        {
            Debug.LogError(data.error);
        }
	}

	// 解读 DTO
	private string[] dealDTO(string stra){
		// string[] rt = new string[3];
		try{
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
	    }catch(Exception){
	    	return new string[] { "Error", "somtiong error" ,"500"};
	    }
		
	}

}
