using UnityEngine;

public class Customer : Appearable
{
    enum BehaviorState
    {
        None,
        Normal, // In normal state all customers jump randomly, the more angry they are the more they jump
        Angry, // In angry state all customers jump in unison
    }

    public enum EmojiType
    {
        Happy,
        Angry,
    }

    [SerializeField] private GameObject emojiPrefab;
    [SerializeField] private Transform transformToOffsetFrom;
    [SerializeField] private Sprite customerHappySprite;
    [SerializeField] private Sprite customerAngrySprite;
    [SerializeField] private Collider physicsCollider;
    [SerializeField] private float jumpForce = 3.5f;

    [HideInInspector] public CustomerManager customerManager;
    [HideInInspector] public new Rigidbody rigidbody;

    private GameObject emojiObject = null;
    private Emoji emoji = null;
    private float minimalDistanceToGround = 0.0f;
    private float jumperTickTimer = 0.0f;
    private float emojiTickTimer = 0.0f;

    private const float jumperTickInterval = 3.0f;
    private const float emojiTickInterval = 3.0f;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        Appear();

        rigidbody = GetComponent<Rigidbody>();

        minimalDistanceToGround = physicsCollider.bounds.extents.y;
    }

    private void Update()
    {
        AppearOrDisappearTick();

        JumpTick();

        EmojiTick();
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -Vector3.up, minimalDistanceToGround + 0.1f);
    }

    private void JumpTick()
    {
        jumperTickTimer += Time.deltaTime;

        if (jumperTickTimer < jumperTickInterval)
        {
            return;
        }

        jumperTickTimer = 0.0f;

        if (IsGrounded())
        {
            if (Random.Range(0.0f, 1.0f) > customerManager.satisfaction)
            {
                rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }
    }

    private void EmojiTick()
    {
        emojiTickTimer += Time.deltaTime;

        if (emojiTickTimer < emojiTickInterval)
        {
            return;
        }

        emojiTickTimer = 0.0f;

        if (emojiObject != null)
        {
            return;
        }

        // Don't immediately jump after spawning emoji.
        jumperTickTimer = 0.0f;

        if (Random.Range(0.0f, 1.0f) > customerManager.satisfaction)
        {
            SpawnEmoji(EmojiType.Angry);
        }
    }

    public void SpawnEmoji(EmojiType type)
    {
        emojiObject = Instantiate(emojiPrefab, transform.position, Quaternion.identity);

        emoji = emojiObject.GetComponent<Emoji>();

        if (type == EmojiType.Happy)
        {
            emoji.image.sprite = customerHappySprite;
        }
        else if (type == EmojiType.Angry)
        {
            emoji.image.sprite = customerAngrySprite;
        }

        emojiObject.GetComponent<Billboard>().transformToOffsetFrom = Instantiate(transformToOffsetFrom.gameObject, transform.position, Quaternion.identity).transform;
    }
}
