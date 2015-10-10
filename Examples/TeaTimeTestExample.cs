
// I use this script to test random things.

// 2015/09/15 12:47:29 PM


using UnityEngine;
using matnesis.TeaTime;


public class TeaTimeTestExample : MonoBehaviour
{
	TeaTime queue;


	void Start()
	{
		queue = this.tt().Pause().Add(1, () =>
		{
			Debug.Log("step 1 " + Time.time);
		})
		.Add(1, () =>
		{
			Debug.Log("step 2 " + Time.time);
		})
		.Add(1, (ttHandler t) =>
		{
			Debug.Log("step 3 " + Time.time);
			t.WaitFor(1);
		})
		.Add(() =>
		{
			Debug.Log("step 4 " + Time.time);
		})
		.Loop(1, (ttHandler t) =>
		{
			transform.position = Vector3.Lerp(transform.position, new Vector3(10, 10, 10), t.deltaTime);
		})
		.Add(() =>
		{
			Debug.Log("step 5 " + Time.time);
		})
		.Loop((ttHandler t) =>
		{
			transform.position = Vector3.Lerp(transform.position, Vector3.zero, t.deltaTime);

			if (t.timeSinceStart >= 1)
				t.Break();
		})
		.Add(() =>
		{
			Debug.Log("step 6 " + Time.time);
		})
		.Add(new WaitForSeconds(1), () =>
		{
			Debug.Log("step 7 " + Time.time);
		})
		.Loop(0, (ttHandler t) =>
		{
			// Ignorable loop
		})
		.Add(1, () =>
		{
			Debug.Log("step 8 " + Time.time);
		})
		.Wait();
	}


	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Y))
		{
			queue.Reset();
		}

		if (Input.GetKeyDown(KeyCode.U))
		{
			queue.Restart();
		}

		if (Input.GetKeyDown(KeyCode.I))
		{
			queue.Play();
		}

		if (Input.GetKeyDown(KeyCode.O))
		{
			queue.Pause();
		}

		if (Input.GetKeyDown(KeyCode.P))
		{
			queue.Stop();
		}
	}
}
