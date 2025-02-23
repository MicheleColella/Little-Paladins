using UnityEngine;
using Cinemachine;

[ExecuteAlways]
public class CinemachineClampExtension : CinemachineExtension
{
    [Header("Limiti sull'asse Y")]
    public float minY = -10f;
    public float maxY = 10f;

    private string minYField;
    private string maxYField;

    new void OnEnable()
    {
        minYField = minY.ToString();
        maxYField = maxY.ToString();
    }


    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage,
        ref CameraState state,
        float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body)
        {
            Vector3 pos = state.FinalPosition;
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            // Applica la correzione alla posizione
            state.PositionCorrection += pos - state.FinalPosition;
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying)
            return;
        
        if (!DebugManager.DebugState)
            return;

        GUILayout.BeginArea(new Rect(350, 65, 220, 120));
        
        // Titolo del pannello
        GUILayout.Label("Cinemachine Limit Debug", GUILayout.ExpandWidth(true));
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("minY:", GUILayout.Width(50));
        minYField = GUILayout.TextField(minYField, GUILayout.Width(100));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("maxY:", GUILayout.Width(50));
        maxYField = GUILayout.TextField(maxYField, GUILayout.Width(100));
        GUILayout.EndHorizontal();

        if (float.TryParse(minYField, out float newMin) &&
            float.TryParse(maxYField, out float newMax))
        {
            minY = newMin;
            maxY = newMax;
        }
        
        GUILayout.EndArea();
    }

}
