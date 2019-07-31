using UnityEngine;

namespace Assets.Tests
{
	public class LinearAnimation: MonoBehaviour
	{
		public Vector3 originPos;
		public Vector3 targetPos;
		public float originScale = 1;
		public float targetScale = 1;
		public float period = 5;

		private void Update()
		{
			var halfPeriod = period / 2;
			var modulo = Time.time % period;
			if (modulo > halfPeriod)
				modulo = period - modulo;

			var ratio = modulo / halfPeriod;
			ratio *= ratio * ratio;

			transform.localPosition = Vector3.Lerp(originPos, targetPos, ratio);
			transform.localScale = Vector3.one * Mathf.Lerp(originScale, targetScale, ratio);
		}
	}
}
