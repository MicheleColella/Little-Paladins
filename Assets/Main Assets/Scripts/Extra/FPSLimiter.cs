using UnityEngine;

public class FPSLimiter : MonoBehaviour
{
    [Header("Impostazioni FPS")]
    [Tooltip("Se abilitato, il limite FPS verrà impostato al refresh rate dello schermo.")]
    public bool useRefreshRate = false;

    [Tooltip("Limite FPS da utilizzare se 'useRefreshRate' è false.")]
    public int fpsLimit = 60;

    void Awake()
    {
        // Se l'opzione useRefreshRate è attiva, imposta il limite FPS al refresh rate corrente dello schermo
        if (useRefreshRate)
        {
            fpsLimit = Screen.currentResolution.refreshRate;
        }

        // Applica il limite FPS
        Application.targetFrameRate = fpsLimit;
    }
}
