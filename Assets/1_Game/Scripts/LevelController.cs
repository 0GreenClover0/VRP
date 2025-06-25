using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    [SerializeField] private AnimationCurve shipsLimitCurve;
    [SerializeField] private AnimationCurve shipsSpeedCurve;

    [HideInInspector] public int ShipsLimit = 5;
    [HideInInspector] public float ShipsSpeed = 0.5f;
    public int MaxCustomersToLose = 15;
    public int slightlyAngryStartTreshold = 6;
    public int MapFood { get; private set; }
    public float TimeDeprecated { get; private set; } = 45.0f;
    public bool IsTutorial { get; private set; }
    public bool HasStarted { get; private set; } = true;

    public float TimeStarted { get; private set; } = 0.0f;

    public bool IsDuringScriptedSequence = true;

    public bool GameFinished = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("LevelController instance already existed. Destroying the old one.");
            Destroy(Instance.gameObject);
            Instance = this;
        }

        GameFinished = false;
    }

    private void Update()
    {
        if (IsDuringScriptedSequence)
        {
            return;
        }

        if (TimeStarted <= 0.0f)
        {
            TimeStarted = Time.time;
        }

        ShipsLimit = Mathf.FloorToInt(shipsLimitCurve.Evaluate(Time.time - TimeStarted));
        ShipsSpeed = shipsSpeedCurve.Evaluate(Time.time - TimeStarted);
    }
}
