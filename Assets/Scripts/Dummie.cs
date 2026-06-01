using System.Collections;
using UnityEngine;

public class Dummie : MonoBehaviour, IDamageable
{
    public Rigidbody2D myRigidBody2D;
    private Vector2 startPosition;
    private SpriteRenderer mySpriteRenderer;
    private Color originalColor;
    private Coroutine knockedCoroutine;

    public bool isKnocked;
    private float knockedTimer;

    [Header("--- SETTINGS ---")]
    [SerializeField] private float returnSpeed = 3f;
    [SerializeField] private float damageBlinkTime = 1f;
    [SerializeField] private Color hitColor = Color.red;
    

    private void Start()
    {
        myRigidBody2D = GetComponent<Rigidbody2D>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();

        originalColor = mySpriteRenderer.color;
        startPosition = transform.position;
        knockedTimer = MobilityAndCombatStats.Instance.knockedTimer;
    }

    private void FixedUpdate()
    {
        if (isKnocked) 
            return;

        // MoveTowards never overshoots — stops exactly at the target
        Vector2 nextPosition = Vector2.MoveTowards(myRigidBody2D.position, startPosition, returnSpeed * Time.fixedDeltaTime);
        myRigidBody2D.MovePosition(nextPosition);

        myRigidBody2D.linearVelocity = Vector2.zero;
    }

    #region KNOCKED_STAGE
    public void StartKnockedStage()
    {
        if (knockedCoroutine == null)
            knockedCoroutine = StartCoroutine(KnockedStage());
    }

    private IEnumerator KnockedStage()
    {
        isKnocked = true;
        transform.rotation = Quaternion.Euler(0, 0, 90);
        myRigidBody2D.sharedMaterial.bounciness = 1f;

        yield return new WaitForSeconds(knockedTimer);

        isKnocked = false;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        myRigidBody2D.sharedMaterial.bounciness = 0f;
        knockedCoroutine = null;
    }
    #endregion

    public void Damage(float amount)
    {
        Debug.Log($"<color=orange>DUMMY TOOK {amount} DAMAGE!</color>");

        mySpriteRenderer.color = hitColor;
        StartCoroutine(ResetColor());    
    }

    private IEnumerator ResetColor()
    {
        yield return new WaitForSeconds(damageBlinkTime);   
        mySpriteRenderer.color = originalColor;
    }
}