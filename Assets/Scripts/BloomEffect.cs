using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Volume))]
public class BloomEffect : MonoBehaviour
{
    private Volume volume;

    [SerializeField] private float maxBlendDistance;
    [SerializeField] private float minBlendDistance;
    [SerializeField] private float timeToChange;
    [SerializeField] private float delayTime;

    private float timer;

    private void Awake()
    {
        volume = GetComponent<Volume>();
    }

    private void Start()
    {
        StartTheBloom();
    }

    public void StartTheBloom()
    {
        if (volume != null)
        {
            StartCoroutine(BlendDistanceChange());
        }
        else
        {
            Debug.LogWarning("Falta um componente Volume neste objeto!");
        }
    }

    private IEnumerator BlendDistanceChange()
    {
        while (true)
        {
            timer = 0f;

            while (timer < timeToChange)
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