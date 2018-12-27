using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
@author Tianyi
@time 2018.12.28 1:55 a.m.

小球掉落控制类，小球掉落后会引发相应的效果（目前只有计分效果）
*/
// TODO 黑球失败业务
public class BallController : MonoBehaviour {

	// 主角小球，重点是它要设置有下文方法的脚本，才可以进行跨组件调用方法
	public GameObject Player;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		if(transform.position.y < -2){
			// Destory(gameObject);
			gameObject.SetActive (false); //物体不再更新
			// 跨组件调用方法
			Player.SendMessage("sayHello");
		}
	}
}
