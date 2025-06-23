using System;
using System.Collections;
using Oculus.Interaction;
using UnityEngine;
using Random = UnityEngine.Random;

public class AnimEventReceiver : MonoBehaviour
{
    [SerializeField] private PullSwitch pullSwitchRef;
    [SerializeField] private GameObject lightBulbPrefab;
    [SerializeField] private Transform lightbulbSocket;
    private bool takenLightBulb = false;
    
    public void GiveLightbulb()
    {
        takenLightBulb = false;
        Random.InitState(DateTime.Now.Millisecond);
        GameObject bulb = Instantiate(lightBulbPrefab, lightbulbSocket.transform);
        bulb.GetComponent<AdditionalLightbulbLogic>().onTakeBulbFromPenguin += pullSwitchRef.TakeLightbulbAnim;
        bulb.GetComponent<AdditionalLightbulbLogic>().onTakeBulbFromPenguin += OnTakeBulb;
        Utilities.SilenceRigidbody(bulb.GetComponent<Rigidbody>(), true);
        bulb.transform.localPosition = Vector3.zero;
        GameManager.Instance.storyController.PlayEmergentVoiceline(Random.Range(2, 6));
        StartCoroutine(RemindAboutLightbulb());
    }

    IEnumerator RemindAboutLightbulb()
    {
        yield return new WaitForSeconds(10);
        if (!takenLightBulb) 
        {
            Random.InitState(DateTime.Now.Millisecond);
            GameManager.Instance.storyController.PlayEmergentVoiceline(Random.Range(2, 6));
        }
    }
    
    void OnTakeBulb()
    {
        takenLightBulb = true;
    }
}
