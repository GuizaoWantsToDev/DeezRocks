using UnityEngine;

public class Rock : MonoBehaviour
{
    [Header("General Rock Stats")]
    [SerializeField] private LayerMask whatDestroysRock;

    [Header("General Rock Stats")]
    [SerializeField] private float normalRockSpeed;
    [SerializeField] private float normalRockGravity;
    [SerializeField] private float normalRockDamage;
    [SerializeField] public float normalEnergyCost;

    [Header("General Rock Stats")]
    [SerializeField] private float boulderRockSpeed;
    [SerializeField] private float boulderRockGravity;
    [SerializeField] private float boulderRockDamage;
    [SerializeField] public float boulderEnergyCost;

    [Header("Other")]
    [SerializeField] private float destructionRadius = 1f;
    [SerializeField] private GameObject debris;

    private Rigidbody2D rockRB;
    private float rockDamage;
    public enum RockType
    {
        Normal,
        Boulder
    }
    public RockType rockType;

    void Start()
    {
        rockRB = GetComponent<Rigidbody2D>();

        InitializeRockStats();
    }

    private void InitializeRockStats()
    {
        if(rockType == RockType.Normal)
        {
            SetVelocity(normalRockSpeed);
            SetRBStats(normalRockGravity);
            SetDamage(normalRockDamage);
        }
        if(rockType == RockType.Boulder)
        {
            SetVelocity(boulderRockSpeed);
            SetRBStats(boulderRockGravity);
            SetDamage(boulderRockDamage);
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);

        //will check if the collision is within the layermask
        if ((whatDestroysRock.value & (1 << collision.gameObject.layer)) > 0)
        {
            //add particles, SFX, scrrenshake, damage, destroy the rock

            IDamageable iDamageable = collision.gameObject.GetComponent<IDamageable>();
            if (iDamageable != null)
            {
                iDamageable.Damage(rockDamage);
            }

            Collider2D[] pieces = Physics2D.OverlapCircleAll(contact.point, destructionRadius);

            foreach (Collider2D piece in pieces)
            {
                if (piece.gameObject.layer == LayerMask.NameToLayer("PlatformPiece"))
                {
                    piece.gameObject.SetActive(false);
                    Instantiate(debris, piece.transform.position, transform.rotation);
                }
            }

            Destroy(gameObject);
        } 
    }

    private void SetVelocity(float rockSpeed)
    {
        rockRB.linearVelocity = transform.right * rockSpeed;
    }

    private void SetRBStats(float RBStats)
    {
       rockRB.gravityScale = RBStats;
    }
    
    private void SetDamage(float Damage)
    {
        rockDamage = Damage;
    } 
}
