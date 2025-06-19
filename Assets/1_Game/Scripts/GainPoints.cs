using TMPro;
using UnityEngine;

public class GainPoints : Appearable
{
    [SerializeField] TMP_Text pointsText = null;

    private float lifeTimer = 2.0f;
    private float opacityTimer = 0.0f;
    private bool shouldDisappear = false;

    private const float movingUpSpeed = 1.0f;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        Appear();
    }

    private void Update()
    {
        AppearOrDisappearTick();

        LifeTimerTick();

        MoveUpTick();
    }

    private void LifeTimerTick()
    {
        lifeTimer -= Time.deltaTime;

        if (lifeTimer < 0.0f)
        {
            pointsText.CrossFadeAlpha(0.0f, 0.5f, false);
        }
    }

    private void MoveUpTick()
    {
        Vector3 position = transform.position;
        position.y += Time.deltaTime * movingUpSpeed;
        transform.position = position;
    }

    private void OpacityDisappearTick()
    {
        if (!shouldDisappear)
        {
            return;
        }

        opacityTimer += Time.deltaTime;

        
    }

    public void SetText(string text)
    {
        pointsText.text = text;
    }
}
