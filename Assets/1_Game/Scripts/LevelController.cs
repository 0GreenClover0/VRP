using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance { get; private set; }

    public int ShipsLimit { get; private set; } = 5;
    public float ShipsSpeed { get; private set; } = 0.23f;
    public int MapFood { get; private set; }
    public float Time { get; private set; }
    public bool IsTutorial { get; private set; }
    public bool HasStarted { get; private set; } = true;

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
