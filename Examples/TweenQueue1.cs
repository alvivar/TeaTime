// A queue that continues adding and executing new callbacks.

using UnityEngine;

public class TweenQueue1 : MonoBehaviour
{
	public Transform t;
	public Renderer r;

	private TeaTime queue;

	public void Start()
	{
		queue = new TeaTime(this);
	}

	public void RandomColor()
	{
		Color randomColor = new Color(Random.value, Random.value, Random.value, Random.value);

		// Adds a one second callback loop that lerps to a random color.
		queue.Loop(1, (TeaHandler t) =>
		{
			r.material.color = Color.Lerp(
				r.material.color,
				randomColor,
				t.deltaTime); // t.deltaTime is a custom delta that represents the loop duration.
		});
	}

	public void RandomScale()
	{
		Vector3 randomScale = new Vector3(Random.Range(0.5f, 2), Random.Range(0.5f, 2), Random.Range(0.5f, 2));

		// Adds a one second callback loop that lerps to a random scale.
		queue.Loop(1, (TeaHandler t) =>
		{
			r.transform.localScale = Vector3.Lerp(
				this.t.localScale,
				randomScale,
				t.deltaTime);
		});
	}

	public void Update()
	{
		if (Input.GetKeyDown(KeyCode.U))
		{
			queue.Restart();
		}

		if (Input.GetKeyDown(KeyCode.I))
		{
			RandomColor();
		}

		if (Input.GetKeyDown(KeyCode.O))
		{
			RandomScale();
		}

		if (Input.GetKeyDown(KeyCode.P))
		{
			RandomColor();
			RandomScale();
		}
	}
}

// 2015/10/05 05:54:55 PM