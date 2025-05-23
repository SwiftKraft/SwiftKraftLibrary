using UnityEngine;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DeveloperComment : MonoBehaviour
{
    public const float ScalingDistance = 3.5f;

    [Header("Comment")]
    [TextArea(1, 100)]
    public string Text;
    public int WordCountWrap = 8;
    public float MaxDistance = 50f;
    [Header("Styling")]
    public bool RichText = false;
    public int FontSize = 14;
    public Color FontColor = Color.white;
    public FontStyle FontStyle = FontStyle.Normal;
    public Font FontOverride;
    public float FadeDistance = 3f;
    [Header("Build Settings")]
    public bool AutoDestroyInBuild = true;

    private void Start()
    {
        if (!Application.isEditor && AutoDestroyInBuild)
            Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR

        Transform sceneCam = SceneView.lastActiveSceneView.camera.transform;

        StringBuilder builder = new();

        string[] sep = Text.Split(' ');

        int counter = 0;
        foreach (string s in sep)
        {
            builder.Append(s);
            builder.Append(' ');
            counter++;

            if (counter >= WordCountWrap)
            {
                builder.Append('\n');
                counter = 0;
            }
        }

        float sceneCamDist = (transform.position - sceneCam.position).magnitude;

        if (sceneCamDist <= MaxDistance + FadeDistance)
        {
            GUIStyle style = new()
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = FontSize,
                richText = RichText,
                fontStyle = FontStyle,
                font = FontOverride,
            };

            Color currentColor = FontColor;

            currentColor.a *= Mathf.InverseLerp(MaxDistance + FadeDistance, MaxDistance, sceneCamDist);

            style.normal.textColor = currentColor;

            Handles.Label(transform.position + sceneCam.right * Mathf.Lerp(0f, 0.5f, Mathf.InverseLerp(0f, ScalingDistance, sceneCamDist)), builder.ToString(), style);
        }
#endif
    }
}
