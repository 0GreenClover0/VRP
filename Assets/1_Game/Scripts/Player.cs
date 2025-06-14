using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }

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

    public void ActivateFlash()
    {
        FlashCounter = flashTime;
    }

    public bool IsFlashActive()
    {
        return FlashCounter > 0.0f;
    }
}
