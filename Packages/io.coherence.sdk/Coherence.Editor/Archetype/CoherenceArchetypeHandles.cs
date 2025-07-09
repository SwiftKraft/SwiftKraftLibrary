// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit {
	using System;
	using System.Linq;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal class CoherenceArchetypeHandles
        {
		internal static void DrawHandles(SerializedObject serializedObject)
            {
			var sync = serializedObject.targetObject as CoherenceSync;
			var archeType = sync.Archetype;
            if (archeType != null)
            {
			    var color = new Color(1, 0.8f, 0.4f, 1);
			    Handles.color = color;
			    Color originalColor = GUI.color;
			    GUI.color = color;

			    for(int i = 0; i < archeType.LODLevels.Count; i++)
                    {
				    int bits = archeType.GetTotalActiveBitsOfLOD(i);
				    bool last = i == archeType.LODLevels.Count - 1;

				    float distance = archeType.LODLevels[i].Distance;
				    float nextDistance = last ? distance * 1.2f : archeType.LODLevels[i + 1].Distance;
				    float halfway = (distance + nextDistance) * .5f;

				    // if there is no next distance - set it to one;
				    halfway = Mathf.Max(halfway, 1f);

				    //Handles.DrawWireDisc(archeType.transform.position, archeType.transform.up, distance);
				    EditorGUI.BeginChangeCheck();
				    float newDistance = Handles.RadiusHandle(sync.transform.rotation, sync.transform.position, distance, false);
				    if(EditorGUI.EndChangeCheck())
                    {
					    Undo.RecordObject(sync, "Changed Distance of LOD");
					    archeType.SetLodLevelDistance(newDistance, i);
				    }

				    Vector3 labelPosition = sync.transform.position + sync.transform.right * halfway + sync.transform.up * .5f;

				    string distanceText = last ? $"> {distance.ToString("N2")}" : $"{distance.ToString("N2")} to {nextDistance.ToString("N2")}";
				    Handles.Label(labelPosition, $"{(i == 0 ? "Base" : $"LOD {i}")}\n{distanceText}\n{bits} {(bits == 1 ? "Bit" : "Bits")}");
			    }
			    GUI.color = originalColor;
            }
		}
    }
}
