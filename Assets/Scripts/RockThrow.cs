using UnityEngine;
using UnityEngine.InputSystem;

public class RockThrow : MonoBehaviour
{
    private PlayerController player;

    [SerializeField]
    private GameObject throwPoint;
    [SerializeField] 
    private Transform throwDirection;
    [SerializeField]
    private GameObject rock;
    private GameObject rockInst;
    private float energyCost = 2f;


    private void Start()
    {
        player = GetComponent<PlayerController>();
    }
    private void Update()
    {
        HandleRockDirection();
    }

    private void HandleRockDirection()
    {
       throwPoint.transform.right = MouseDirection.Instance.direction;
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        if (context.performed && EnergyManager.Instance.currentEnergy > 0f)
        {
            rockInst = Instantiate(rock, throwDirection.position, throwPoint.transform.rotation);
            EnergyManager.Instance.UseEnergy(energyCost);
        }
    } 
}
