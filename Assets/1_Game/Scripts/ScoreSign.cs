using UnityEngine;
using TMPro;

public class ScoreSign : MonoBehaviour
{
    public TMP_Text pointsText;

    private void Update()
    {
        string pointsString = Player.Instance.Points.ToString();
        if (pointsText.text != pointsString)
        {
            pointsText.text = pointsString;
        }
    }
}
