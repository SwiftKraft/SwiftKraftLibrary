using UnityEngine;
using System.Text;
using System.Globalization;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SwiftKraft.Utils
{
    public class DeveloperComment : MonoBehaviour
    {
        [Header("Comment")]
        [TextArea(1, 100)]
        public string Text;
        public string Author;
        public int CharCountWrap = 20;
        public float MaxDistance = 50f;
        public bool ShowDate = true;
        [Header("Spacing")]
        public float SpacingDistance = 3.5f;
        public float MinSpacing = 0f;
        public float MaxSpacing = 1f;
        [Header("Styling")]
        public bool RichText = false;
        public int FontSize = 14;
        public Color FontColor = Color.white;
        public FontStyle FontStyle = FontStyle.Normal;
        public Font FontOverride;
        public float FadeDistance = 3f;
        [Header("Build Settings")]
        public bool AutoDestroyInBuild = true;

        [SerializeField, HideInInspector]
        long timestamp;
        [SerializeField, HideInInspector]
        string cachedText;

        private void Start()
        {
            if (!Application.isEditor && AutoDestroyInBuild)
                Destroy(gameObject);
        }

        private void Reset() => UpdateTime(true);
        private void OnValidate() => UpdateTime();

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (SceneView.lastActiveSceneView == null)
                return;

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
                builder.Append("\n\n~ ");
                builder.Append(Author);
            }

            if (ShowDate)
            {
                if (!string.IsNullOrWhiteSpace(Author))
                    builder.Append(", Modified ");
                else
                    builder.Append("\n\n Modified ");

                builder.Append(new DateTime(timestamp).ToLocalTime().ToString(CultureInfo.CurrentCulture));
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

                Handles.Label(transform.position + sceneCam.right * Mathf.Lerp(MinSpacing, MaxSpacing, Mathf.InverseLerp(0f, SpacingDistance, sceneCamDist)), builder.ToString(), style);
            }
#endif
        }

        private void UpdateTime(bool force = false)
        {
            if (Application.isPlaying || (!force && Text == cachedText))
                return;

            timestamp = DateTime.UtcNow.Ticks;
            cachedText = Text;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        [MenuItem("Tools/SwiftKraft/Add Comment")]
        [MenuItem("GameObject/Add Comment #l")]
        public static void AddComment()
        {
            Transform sceneCam = SceneView.lastActiveSceneView.camera.transform;

            GameObject go = new("Developer Comment", typeof(DeveloperComment));
            go.transform.position = sceneCam.position + sceneCam.forward * 3f;
            Undo.RegisterCreatedObjectUndo(go, "Create Developer Comment");
            Selection.activeObject = go;
        }
#endif
    }
}
