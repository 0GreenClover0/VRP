using System.Collections.Generic;
using UnityEngine;

public class Customer : Appearable
{
    enum BehaviorState
    {
        Normal,
        SlightlyAngry, // In SlightlyAngry state all customers jump randomly, the more angry they are the more they jump
        Angry, // In Angry state all customers jump in unison
    }

    public enum PenguinSoundType
    {
        NeutralOrAngry, // To depict slightly angry state
        Happy,
        Neutral,
        Angry,
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
    [SerializeField] private Animator animator;
    
    [Space]
    [Header("Penguin sounds")]
    [SerializeField] private List<AudioClip> happyPenguins;
    [SerializeField] private List<AudioClip> neutralPenguins;
    [SerializeField] private List<AudioClip> angryPenguins;
    [SerializeField] private AudioClip riotPenguin;
    [SerializeField] private AudioSource riotAudioSource;
    [SerializeField] private AudioSource happyAudioSource;

    [HideInInspector] public CustomerManager customerManager;
    [HideInInspector] public new Rigidbody rigidbody;

    private AudioSource audioSource;
    private BehaviorState behaviorState;
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
        audioSource = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        base.Update();

        StateCheck();

        if (behaviorState == BehaviorState.SlightlyAngry)
        {
            JumpTick();
        }

        // if (behaviorState == BehaviorState.Angry && (customerManager.areReadyToAngryJump || customerManager.areAngryJumping))
        // {
        //     if (customerManager.areReadyToAngryJump)
        //     {
        //         customerManager.areAngryJumping = true;
        //         customerManager.areReadyToAngryJump = false;
        //     }

        //     Jump();
        // }

        if (behaviorState == BehaviorState.SlightlyAngry || behaviorState == BehaviorState.Angry)
        {
            EmojiTick();
        }
    }

    private void StateCheck()
    {
        if (customerManager.satisfaction <= 0.0f)
        {
            behaviorState = BehaviorState.Angry;
        }
        else if (customerManager.satisfaction < customerManager.slightlyAngryStartSatisfaction)
        {
            behaviorState = BehaviorState.SlightlyAngry;
        }
        else
        {
            behaviorState = BehaviorState.Normal;
        }
    }

    public bool IsGrounded()
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

        jumperTickTimer = Random.Range(-2.0f, 0.0f);

        Jump();
    }

    public void Jump(bool skipSatisfactionCheck = false)
    {
        // if (IsGrounded())
        // {
        //     if (skipSatisfactionCheck || Random.Range(0.0f, 1.0f) > customerManager.satisfaction)
        //     {
        //         rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //     }
        // }
        
        if (skipSatisfactionCheck || Random.Range(0.0f, 1.0f) > customerManager.satisfaction)
        {
            if (skipSatisfactionCheck)
            {
                PlayRiotSound();
            }
            else
            {
                PlayPenguinSound(PenguinSoundType.NeutralOrAngry);
            }
            
            if (animator != null)
            {
                animator.SetTrigger("Jump");   
            }
        }
    }

    public void PlayPenguinSound(PenguinSoundType type)
    {
        switch (type)
        {
            case PenguinSoundType.Happy:
                happyAudioSource.clip = happyPenguins[Random.Range(0, happyPenguins.Count - 1)];
                happyAudioSource.pitch = Random.Range(0.85f, 1.15f);
                happyAudioSource.Play();
                break;
            
            case PenguinSoundType.NeutralOrAngry:
                bool chooseNeutral = Random.Range(0, 1) == 1;
                audioSource.clip = chooseNeutral ? neutralPenguins[Random.Range(0, neutralPenguins.Count - 1)]
                                                 : angryPenguins[Random.Range(0, angryPenguins.Count - 1)];
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.Play();
                break;
            
            case PenguinSoundType.Neutral:
                audioSource.clip = neutralPenguins[Random.Range(0, neutralPenguins.Count - 1)];
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.Play();
                break;
            
            case PenguinSoundType.Angry:
                audioSource.clip = angryPenguins[Random.Range(0, angryPenguins.Count - 1)];
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.Play();
                break;
        }
    }
    
    void PlayRiotSound()
    {
        riotAudioSource.clip = riotPenguin;
        riotAudioSource.pitch = Random.Range(0.85f, 1.15f);
        riotAudioSource.Play();
    }
    
    private void EmojiTick()
    {
        emojiTickTimer += Time.deltaTime;

        if (emojiTickTimer < emojiTickInterval)
        {
            return;
        }

        emojiTickTimer = Random.Range(-2.0f, 0.0f);

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
