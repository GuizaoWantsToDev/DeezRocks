using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private float shakeTimer;
    private Animator cameraAnimator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        cameraAnimator = GetComponent<Animator>();
    }

    public void ShakeCamera()
    {
        cameraAnimator.SetTrigger("shake");
    }
}