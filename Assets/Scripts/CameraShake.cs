using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin;
    private float shakeTimer;
    private Animator cameraAnimator;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        cameraAnimator = GetComponent<Animator>();
           // cinemachineBasicMultiChannelPerlin = GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void ShakeCamera(/*float intensity, float time*/)
    {
        //cinemachineBasicMultiChannelPerlin.AmplitudeGain = intensity;
        //shakeTimer = time;

        cameraAnimator.SetTrigger("shake");
    }

    private void Update()
    {
        if(shakeTimer > 0)
        {
            shakeTimer-= Time.deltaTime;
            if (shakeTimer <= 0f)
            {
                transform.localPosition = Vector3.zero;
                //cinemachineBasicMultiChannelPerlin.AmplitudeGain = 0f;
            }
        }
    }
}
