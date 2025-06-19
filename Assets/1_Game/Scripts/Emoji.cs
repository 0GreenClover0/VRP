using UnityEngine;
using UnityEngine.UI;

public class Emoji : Appearable
{
    public Image image;
    private Transform billboardTransformToOffsetFrom;
    private float lifeTimer = 2.0f;

    private const float movingUpSpeed = 1.0f;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        Appear();
    }

    private void Start()
    {
        billboardTransformToOffsetFrom = GetComponent<Billboard>().transformToOffsetFrom;
    }

    private void Update()
    {
        AppearOrDisappearTick();

        MoveUpTick();

        LifeTimerTick();
    }

    private void MoveUpTick()
    {
        Vector3 position = billboardTransformToOffsetFrom.transform.position;
        position.y += Time.deltaTime * movingUpSpeed;
        billboardTransformToOffsetFrom.position = position;
    }

    private void LifeTimerTick()
    {
        lifeTimer -= Time.deltaTime;

        if (lifeTimer < 0.0f)
        {
            Disappear();
        }
    }

    private void OnDestroy()
    {
        if (billboardTransformToOffsetFrom != null)
        {
            Destroy(billboardTransformToOffsetFrom.gameObject);
        }
    }
}
