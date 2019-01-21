using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tealistTest : MonoBehaviour
{
	public bool flip = false;

	tlist t = new tlist();

	void Start()
	{
		t
			.Add(1, 0, () => Debug.Log("1 0 Add " + Time.time))
			.Add(2, 2, () => Debug.Log("2 2 Add " + Time.time))
			.Add(1, 0, () => Debug.Log("1 0 Add " + Time.time));
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			flip = !flip;
			Debug.Log("FLIP " + Time.time);
		}

		if (flip)
			t.Execute(-Time.deltaTime);
		else
			t.Execute(Time.deltaTime);

	}
}