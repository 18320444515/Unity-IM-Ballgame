using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
相机移动类，让相机保持与主角小球的相对静止
*/
public class CameraController : MonoBehaviour {

	// 设置主角小球
	public GameObject player;

	// 存放相对角度
	private Vector3 offset;

	// Use this for initialization
	void Start () {
		// 初始化相对角度
		offset = transform.position - player.transform.position;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		// 保持相对角度
		transform.position = player.transform.position + offset;
	}
}
