using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
官方教程 Roll-a-ball 的浮块旋转类
*/
public class Rotator : MonoBehaviour {
	
	// Update is called once per frame, when before rendering each frame..
	void Update () {
		// Rotate the game object that this script is attached to by 15 in the X axis,
		// 30 in the Y axis and 45 in the Z axis, multiplied by deltaTime in order to make it per second
		// rather than per frame.
		transform.Rotate (new Vector3 (15, 30, 45) * Time.deltaTime);
	}
	
}
