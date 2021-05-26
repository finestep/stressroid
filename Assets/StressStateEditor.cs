using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StressState))]
[CanEditMultipleObjects]
public class StressStateEditor : Editor 
{

    public override void OnInspectorGUI()
    {
        bool updateTex = false;


        serializedObject.Update();

        var t = ((StressState)target);
        //base.OnInspectorGUI();

        var typeProp = serializedObject.FindProperty("type");
        if( EditorGUILayout.PropertyField(typeProp) )
        {
            updateTex = true;
        }

        var orProp = serializedObject.FindProperty("overRelax");
        EditorGUILayout.PropertyField(orProp);

        serializedObject.ApplyModifiedProperties();

        if (GUILayout.RepeatButton("Update Stress State"))
        {
            t.UpdateStress();
            updateTex = true;
        }

        if (GUILayout.Button("Update Texture") || updateTex)
        {
            t.UpdateTex();
        }



        if (GUILayout.Button("Poke") )
        {

            for (int i = 0; i < 3; i++)
                t.grid.setGrid(21 + i, 0, 50000);
        }

        GUILayout.Label("Width:\t" + t.width + " \t Height: " + t.height);

        GUILayout.Label("Value:\t" + t.stats.relax.minVal + " \t " + t.stats.relax.maxVal);

        GUILayout.Label("Residue:\t" + t.stats.relax.minResidue + " \t " + t.stats.relax.maxResidue);
        GUILayout.Label("Residue norm:\t" + t.stats.relax.residueNorm );

        if (t.type == StressGridToTex.TYPE.STRESS) {

            GUILayout.Label("Shear:\t" + t.stats.stress.minShear + " \t " + t.stats.stress.maxShear);
            GUILayout.Label("Pressure:\t" + t.stats.stress.minPressure + " \t " + t.stats.stress.maxPressure);

        }
    }
}
