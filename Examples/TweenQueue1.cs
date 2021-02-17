// A queue of tweens.

// 2015/10/05 05:54:55 PM

using UnityEngine;

public class TweenQueue1 : MonoBehaviour
{
	public Transform cube;
	public Renderer cubeRen;

	TeaTime queue;

	void Start()
	{
		queue = new TeaTime(this);
	}

	public void RandomColor()
	{
		Color randomColor = new Color(Random.value, Random.value, Random.value, Random.value);

		// Adds a one second callback loop that lerps to a random color.
		queue.Loop(1f, (ttHandler t) =>
		{
			cubeRen.material.color = Color.Lerp(
				cubeRen.material.color,
				randomColor,
				t.deltaTime); // t.deltaTime is a custom delta that represents the loop duration
		});
	}

	public void RandomScale()
	{
		Vector3 randomScale = new Vector3(Random.Range(0.5f, 2), Random.Range(0.5f, 2), Random.Range(0.5f, 2));

		// Adds a one second callback loop that lerps to a random scale.
		queue.Loop(1f, (ttHandler t) =>
		{
			cubeRen.transform.localScale = Vector3.Lerp(
				cube.localScale,
				randomScale,
				t.deltaTime);
		});
	}
}