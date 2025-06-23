using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements.Experimental;

public enum AppearingState
{
    None,
    Appearing,
    Disappearing,
}

public class Appearable : MonoBehaviour
{
    protected float appearDuration = 2.0f;

    private AppearingState state;
    private float appearingTimer = 0.0f;

    protected virtual void Update()
    {
        AppearOrDisappearTick();
    }

    protected void AppearOrDisappearTick()
    {
        switch (state)
        {
            case AppearingState.Appearing:
                {
                    appearingTimer += Time.deltaTime;

                    float value = appearingTimer / appearDuration;

                    if (value > 1.0f)
                    {
                        transform.localScale = Vector3.one;
                        ChangeAppearingState(AppearingState.None);
                        break;
                    }

                    float newSize = Easing.OutElastic(value);
                    Vector3 newScale = Vector3.one * newSize;

                    transform.localScale = newScale;
                    break;
                }
            case AppearingState.Disappearing:
                {
                    appearingTimer -= Time.deltaTime;

                    float value = appearingTimer / appearDuration;

                    if (value < 0.0f)
                    {
                        transform.localScale = Vector3.zero;
                        ChangeAppearingState(AppearingState.None);
                        Destroy(gameObject);
                        break;
                    }

                    float newSize = Easing.OutElastic(value);
                    Vector3 newScale = Vector3.one * newSize;

                    transform.localScale = newScale;
                    break;
                }
        }
    }

    public virtual void Appear()
    {
        Mathf.Clamp(appearingTimer, 0.0f, appearDuration);
        state = AppearingState.Appearing;
    }

    public virtual void Disappear()
    {
        Mathf.Clamp(appearingTimer, 0.0f, appearDuration);
        state = AppearingState.Disappearing;
    }

    private void ChangeAppearingState(AppearingState newState)
    {
        state = newState;
    }
}
