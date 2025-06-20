using TMPro;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    public int ShipsLimit = 5;
    public float ShipsSpeed = 0.65f;
    public int MaxCustomersToLose = 15;
    public int slightlyAngryStartTreshold = 6;
    public int MapFood { get; private set; }
    public float Time { get; private set; } = 45.0f; // TODO: This should be set per level
    public bool IsTutorial { get; private set; }
    public bool HasStarted { get; private set; } = true;

    public bool IsDuringScriptedSequence = true;

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
    }
}
