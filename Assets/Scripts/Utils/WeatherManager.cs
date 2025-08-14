using UnityEngine;

public enum WeatherState { Cold, Mild, Hot }

public class WeatherManager : MonoBehaviour
{
    public WeatherState current = WeatherState.Mild;

    [Tooltip("How much faster/slower effects run vs. base duration")]
    public float coldMultiplier = 0.7f;
    public float hotMultiplier  = 1.3f;

    public float GetSpeedMultiplier()
    {
        switch (current)
        {
            case WeatherState.Cold: return coldMultiplier;
            case WeatherState.Hot:  return hotMultiplier;
            default:                return 1f;
        }
    }
}