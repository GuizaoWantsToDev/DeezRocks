using System.Collections;
using System.Timers;
using UnityEngine;

public class ShockWaveManager : MonoBehaviour
{
    [SerializeField] private float shockWaveTime = 0.75f;

    private Coroutine shockWaveCoroutine;

    private Material material;

    private static int waveDistanceFromCenter = Shader.PropertyToID("_WaveDistanceFromCenter");

    private void Start()
    {
        material = GetComponent<SpriteRenderer>().material;
        CallShockWave();
    }

    public void CallShockWave()
    {
        shockWaveCoroutine = StartCoroutine(ShockWaveAction(-0.1f, 1f));
    }

    private IEnumerator ShockWaveAction(float startPosition, float endPosition)
    {
        material.SetFloat(waveDistanceFromCenter, startPosition);

        float lerpedAmount = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < shockWaveTime)
        {
            elapsedTime += Time.deltaTime;
            
            lerpedAmount= Mathf.Lerp(startPosition, endPosition, (elapsedTime/shockWaveTime));
            material.SetFloat(waveDistanceFromCenter, lerpedAmount);

            yield return null;
        }

        Destroy(gameObject);
    }
}
