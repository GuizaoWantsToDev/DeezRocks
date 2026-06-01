using UnityEngine;

public class ScrollingBackGround : UnityEngine.MonoBehaviour
{
    [SerializeField]
    private float speed;

    [SerializeField]
    private Renderer myRenderer;

    public void Update()
    {
        myRenderer.material.mainTextureOffset += new Vector2(speed * Time.deltaTime, 0f);
    }
}
