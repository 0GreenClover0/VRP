using System;
using Oculus.Interaction;
using UnityEngine;

public class AnimEventReceiver : MonoBehaviour
{
    [SerializeField] private PullSwitch pullSwitchRef;
    [SerializeField] private GameObject lightBulbPrefab;
    [SerializeField] private Transform lightbulbSocket;
    
    public void GiveLightbulb()
    {
        GameObject bulb = Instantiate(lightBulbPrefab, lightbulbSocket.transform);
        bulb.GetComponent<AdditionalLightbulbLogic>().onTakeBulbFromPenguin += pullSwitchRef.TakeLightbulbAnim;
        Utilities.SilenceRigidbody(bulb.GetComponent<Rigidbody>(), true);
        bulb.transform.localPosition = Vector3.zero;
    }
}
