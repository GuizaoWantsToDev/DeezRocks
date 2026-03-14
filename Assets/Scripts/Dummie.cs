using UnityEngine;

public class Dummie : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.AddPlayer(gameObject);
    }   
}
