using UnityEngine;

public class Rock : MonoBehaviour
{
    [Header("General Rock Stats")]
    [SerializeField] private LayerMask whatDestroysRock;

    [SerializeField] private float energyCostPerLevel;
    [SerializeField] private float damagePerLevel;
    [SerializeField] private float sizeIncreasePerLevel;

    [Header("Level 1 Rock Stats")]
    [SerializeField] private float level1RockSize;
    [SerializeField] private float level1RockSpeed;
    [SerializeField] private float level1RockGravity;
    [SerializeField] private float level1RockDamage;
    [SerializeField] public float level1EnergyCost;

    [Header("Level 2 Rock Stats")]
    [SerializeField] private float level2RockSize;
    [SerializeField] private float level2RockSpeed;
    [SerializeField] private float level2RockGravity;
    [SerializeField] private float level2RockDamage;
    [SerializeField] public float level2EnergyCost;

    [Header("Level 3 Rock Stats")]
    [SerializeField] private float level3RockSize;
    [SerializeField] private float level3RockSpeed;
    [SerializeField] private float level3RockGravity;
    [SerializeField] private float level3RockDamage;
    [SerializeField] public float level3EnergyCost;

    [Header("Level 4 Rock Stats")]
    [SerializeField] private float level4RockSize;
    [SerializeField] private float level4RockSpeed;
    [SerializeField] private float level4RockGravity;
    [SerializeField] private float level4RockDamage;
    [SerializeField] public float level4EnergyCost;

    [Header("Level 5 Rock Stats")]
    [SerializeField] private float level5RockSize;
    [SerializeField] private float level5RockSpeed;
    [SerializeField] private float level5RockGravity;
    [SerializeField] private float level5RockDamage;
    [SerializeField] public float level5EnergyCost;

    [Header("Other")]
    [SerializeField] private float destructionRadius = 1f;
    [SerializeField] private GameObject debris;

    private Rigidbody2D rockRB;
    private float rockDamage;
    public enum RockType
    {
        Level1,
        Level2,
        Level3,
        Level4,
        Level5
    }
    public RockType rockType;

    void Start()
    {
        rockRB = GetComponent<Rigidbody2D>();
    }

    public void InitializeRockStats()
    {
        if(rockType == RockType.Level1)
        {
            SetVelocity(level1RockSpeed);
            SetRBStats(level1RockGravity);
            SetDamage(level1RockDamage);
        }
        if(rockType == RockType.Level2)
        {
            SetVelocity(level2RockSpeed);
            SetRBStats(level2RockGravity);
            SetDamage(level2RockDamage);
        }
        if (rockType == RockType.Level3)
        {
            SetVelocity(level3RockSpeed);
            SetRBStats(level3RockGravity);
            SetDamage(level3RockDamage);
        }
        if(rockType == RockType.Level4)
        {
            SetVelocity(level4RockSpeed);
            SetRBStats(level4RockGravity);
            SetDamage(level4RockDamage);
        }
        if(rockType == RockType.Level5)
        {
            SetVelocity(level5RockSpeed);
            SetRBStats(level5RockGravity);
            SetDamage(level5RockDamage);
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
                   // Instantiate(debris, piece.transform.position, transform.rotation);
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
