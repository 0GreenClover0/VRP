using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private GameObject gainedPointsPrefab = null;

    public static Player Instance { get; private set; }

    public int Points { get; private set; }

    public int Food { get; private set; }

    public float FlashCounter { get; private set; }

    private const float flashTime = 8.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Player instance already existed. Destroying the old one.");
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    private void Update()
    {
        if (FlashCounter > 0.0f)
        {
            FlashCounter -= Time.deltaTime;
        }
    }

    public void AddPoints(int points, Vector3? position = null)
    {
        Points += points;

        if (position != null)
        {
            GameObject pointsObject = Instantiate(gainedPointsPrefab, position.Value, Quaternion.identity);
            pointsObject.GetComponent<GainPoints>().SetText("+" + points.ToString());
        }
    }

    public void ActivateFlash()
    {
        FlashCounter = flashTime;
    }

    public bool IsFlashActive()
    {
        return FlashCounter > 0.0f;
    }
}
