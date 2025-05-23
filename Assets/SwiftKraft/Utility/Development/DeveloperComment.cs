using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;
using SwiftKraft.Utils;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class DeveloperComment : MonoBehaviour
{
    public const float ScalingDistance = 3.5f;

    [Header("Comment")]
    [TextArea(1, 100)]
    public string Text;
    public string Author;
    public int CharCountWrap = 20;
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

        if (string.IsNullOrWhiteSpace(Text))
            return;

        StringBuilder builder = new();

        string[] sep = Text.Split(' ');

        int counter = 0;
        for (int i = 0; i < sep.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(sep[i]))
                continue;

            builder.Append(sep[i]);
            builder.Append(' ');
            counter += sep[i].Length(RichText);

            if (sep[i].Contains('\n'))
                counter = 0;
            else if (counter >= CharCountWrap && i < sep.Length - 1)
            {
                builder.Append('\n');
                counter = 0;
            }
        }

        if (!string.IsNullOrWhiteSpace(Author))
        {
            builder.Append("\n\n- ");
            builder.Append(Author);
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
                font = FontOverride
            };

            Color currentColor = FontColor;

            currentColor.a *= Mathf.InverseLerp(MaxDistance + FadeDistance, MaxDistance, sceneCamDist);

            style.normal.textColor = currentColor;

            Handles.Label(transform.position + sceneCam.right * Mathf.Lerp(0f, 0.5f, Mathf.InverseLerp(0f, ScalingDistance, sceneCamDist)), builder.ToString(), style);
        }
#endif
    }
}
