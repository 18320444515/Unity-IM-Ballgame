#pragma strict

/*
探索 JavaScript 的时候写的脚本，然而很失望地发现了原来 Unity 的 JavaScript 是假的.. 
*/


private var rb:Rigidbody;
var speed:int;

function Start () {
	rb = GetComponent.<Rigidbody>();
}

function Update () {
	
}

function sayHello() {
	print('hello js');
}

// Each physics step..
function FixedUpdate ()
{
	var moveVertical:float = Input.GetAxis("Vertical");
    var moveHorizontal:float = Input.GetAxis("Horizontal");
    var movement:Vector3 = new Vector3 (moveHorizontal, 0.0, moveVertical);
    rb.AddForce(movement * speed);
    if( moveVertical != 0 || moveHorizontal != 0 )
	{
		// print("Horizontal:"+moveHorizontal+" vertical:"+moveVertical);
	}
}