using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateFPS : MonoBehaviour
{
    public string format = "FPS: {0:F1}";

    private const int bufferSize = 70; // ~1s
    private const float updateFrequency = 2;

    private float timeSinceLastUpdate;
    private int deltaTimeIndex;
    private List<float> deltaTimes = new List<float>();

    private Text fpsText;

    private void Start()
    {
        fpsText = GetComponent<Text>();
    }

    private void OnEnable()
    {
        timeSinceLastUpdate = 0;
        deltaTimeIndex = 0;
        deltaTimes.Clear();
    }

    private void RecordDeltaTime(float delta)
    {
        if (deltaTimes.Count >= bufferSize)
        {
            deltaTimes[deltaTimeIndex] = delta;
            deltaTimeIndex = (deltaTimeIndex + 1) % bufferSize;
        }
        else
        {
            deltaTimes.Add(delta);
            deltaTimeIndex = 0;
        }
    }

    private float ComputeFps()
    {
        var sum = 0f;
        foreach (var time in deltaTimes)
            sum += time;

        return deltaTimes.Count / sum;
    }

    // Update is called once per frame
    void Update()
    {
        var delta = Time.unscaledDeltaTime;

        RecordDeltaTime(delta);

        timeSinceLastUpdate += delta;
        if(timeSinceLastUpdate > 1 / updateFrequency)
        {
            var fpsVal = ComputeFps();
            fpsText.text = string.Format(format, fpsVal);
            timeSinceLastUpdate = 0;
        }
    }
}
