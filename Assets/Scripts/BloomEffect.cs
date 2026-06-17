using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BloomEffect : MonoBehaviour
{
    private Volume volume;
    [SerializeField] private float maxBlendDistance;
    [SerializeField] private float minBlendDistance;
    [SerializeField] private float timeToChange;
    [SerializeField] private float delayTime;
    private float timer;

    private void Start()
    {
        volume = GetComponent<Volume>();

        StartTheBloom();
    }
    public void StartTheBloom()
    {
        StartCoroutine(BlendDistanceChange());
    }
    private IEnumerator BlendDistanceChange()
    {
        while (1 + 1 == 2)
        {
            timer = 0f;
            while(timer < timeToChange)
            {
                timer += Time.deltaTime;
                volume.blendDistance = Mathf.Lerp(maxBlendDistance, minBlendDistance, timer / timeToChange);
                yield return null;
            }

            yield return new WaitForSeconds(delayTime);

            timer = 0f;
            while (timer < timeToChange)
            {
                timer += Time.deltaTime;
                volume.blendDistance = Mathf.Lerp(minBlendDistance, maxBlendDistance, timer / timeToChange);
                yield return null;
            }

            yield return new WaitForSeconds(delayTime);
        }
    }
}
