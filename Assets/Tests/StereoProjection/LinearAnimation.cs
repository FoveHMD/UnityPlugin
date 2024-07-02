using UnityEngine;
using UnityEngine.UI;

namespace Assets.Tests
{
    public class LinearAnimation: MonoBehaviour
    {
        public Text CubeDepthText;
        public string DepthFormat = "{0:F1}";

        public Vector3 originPos;
        public Vector3 targetPos;
        public float originScale = 1;
        public float targetScale = 1;
        public float period = 5;
        public bool exponentialSpeed = false;

        private void Update()
        {
            var halfPeriod = period / 2;
            var modulo = Time.time % period;
            if (modulo > halfPeriod)
                modulo = period - modulo;

            var ratio = modulo / halfPeriod;
            if (exponentialSpeed)
                ratio *= ratio;

            transform.localPosition = Vector3.Lerp(originPos, targetPos, ratio);
            transform.localScale = Vector3.one * Mathf.Lerp(originScale, targetScale, ratio);

            CubeDepthText.text = string.Format(DepthFormat, transform.position.z);
        }
    }
}
