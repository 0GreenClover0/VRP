using TMPro;
using UnityEngine;

public class GameOverScreen : Appearable
{
    [SerializeField] private TMP_Text pointsText;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        Appear();
    }

    protected override void Update()
    {
        base.Update();
    }

    public override void Appear()
    {
        base.Appear();

        gameObject.SetActive(true);

        pointsText.text = Player.Instance.Points.ToString();
    }
}
