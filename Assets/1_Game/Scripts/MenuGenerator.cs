using UnityEngine;

public class MenuGenerator : MonoBehaviour
{
    [SerializeField] private GameObject lightCone;
    [SerializeField] private GeneratorPower generatorPower;

    private float timer = 0.0f;

    private bool startTimer = false;
    private float blinkTime = 0.1f;
    private int blinkCount = 0;

    private void Update()
    {
        if (generatorPower == null)
            return;

        if (blinkCount >= 5)
        {
            return;
        }

        if (generatorPower.currentGeneratorPower > 0.0f)
            {
                startTimer = true;
            }

        if (startTimer)
        {
            timer += Time.deltaTime;

            if (timer > blinkTime)
            {
                lightCone.SetActive(!lightCone.activeSelf);
                timer = 0.0f;
                blinkCount += 1;
            }
        }
    }
}
