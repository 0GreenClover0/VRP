using System;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public enum ShipColor
{
    Green,
    Purple
}

public class ShipLightsController : MonoBehaviour
{
    private Material material;
    private Texture2D currentLightmap;
    private Ship shipComponent;
    
    [SerializedDictionary("Ship State", "Lightmap")]
    public SerializedDictionary<BehavioralState, Texture2D> statesToLightMaps;
    public SerializedDictionary<BehavioralState, Color> statesToLightColors;
    public Renderer shipRenderer;
    public Renderer lightRenderer;
    
    private void OnEnable()
    {
        if (material == null)
        {
            material = shipRenderer.material;
            material.EnableKeyword("_EMISSION");
        }
        
        if (shipComponent == null)
        {
            shipComponent = GetComponent<Ship>();
            shipComponent.onShipStateChanged += ChangeLightmap;
        }
        else
        {
            shipComponent.onShipStateChanged += ChangeLightmap;
        }
        
        ChangeLightmap(BehavioralState.Normal);
    }

    void ChangeLightmap(BehavioralState state)
    {
        currentLightmap = statesToLightMaps[state];
        material.SetTexture("_EmissionMap", currentLightmap);
        lightRenderer.material.color = statesToLightColors[state];
    }

    private void OnDisable()
    {
        if (shipComponent == null)
        {
            shipComponent = GetComponent<Ship>();
            shipComponent.onShipStateChanged -= ChangeLightmap;
        }
        else
        {
            shipComponent.onShipStateChanged -= ChangeLightmap;
        }
    }
}
