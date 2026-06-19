using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            int fps = Mathf.RoundToInt(1f / Time.deltaTime);
            fpsText.text = fps + " FPS";
            timer = 0f;
        }
    }
}