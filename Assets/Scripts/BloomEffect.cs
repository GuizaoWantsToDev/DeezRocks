using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Isto obriga o Unity a adicionar um Volume caso te esqueças!
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
        // Awake corre MUITO antes do Start ou de qualquer clique na UI
        volume = GetComponent<Volume>();
    }

    private void Start()
    {
        StartTheBloom();
    }

    public void StartTheBloom()
    {
        // Prevenção: só começa a coroutine se o volume realmente existir
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
        while (true) // (true) é o mesmo que 1 + 1 == 2, mas mais bonito em código!
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