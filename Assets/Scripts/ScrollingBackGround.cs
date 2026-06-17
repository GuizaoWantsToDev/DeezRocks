using UnityEngine;
using UnityEngine.UI;

public class ScrollingBackGround : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private RawImage myRawImage;

    private void Update()
    {
        Rect rect = myRawImage.uvRect;
        rect.x += speed * Time.deltaTime;
        myRawImage.uvRect = rect;
    }
}