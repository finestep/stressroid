using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using Util;

[CustomEditor(typeof(DisplacementState))]
[CanEditMultipleObjects]
public class DisplacementStateEditor : Editor
{
    public int updateCount = 2;
    public override void OnInspectorGUI()
    {
        bool updateTex = false;


        serializedObject.Update();

        var t = ((DisplacementState)target);
        //base.OnInspectorGUI();

        var typeProp = serializedObject.FindProperty("type");
        if (EditorGUILayout.PropertyField(typeProp))
        {
            updateTex = true;
        }
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("iterations"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("deltatime"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("density"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lameComp"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lameRigid"));


        serializedObject.ApplyModifiedProperties();

        updateCount = EditorGUILayout.IntField("Updates",updateCount);

        if (GUILayout.RepeatButton("Update Displacement State"))
        {
            //var time = Stopwatch.StartNew();
            for(int i=0;i<updateCount;i++)
               t.UpdateDisplacement();
            //UnityEngine.Debug.Log("took " + time.Elapsed + " s");
            updateTex = true;
        }

        if (GUILayout.Button("Update Texture") || updateTex)
        {
            t.UpdateTex();
        }



        if (GUILayout.Button("Poke"))
        {



            for(int i=0;i<t.grid.size;i++)
            {
                t.grid.uX[i] = 0f;
                t.grid.uY[i] = 0f;
                t.grid.uX_prev[i] = 0f;
                t.grid.uY_prev[i] = 0f;
            }

            /*
                for (int iy = -6; iy <= 6; iy++)
                {

                    t.grid.uX[1, 18 + iy] = 10f;
                    //t.grid.uX_prev[0, 18 + iy] = 30f;
                }
                */
            
            for (int ix = -4; ix <= 4; ix++)
                for (int iy = -4; iy <= 4; iy++)
                {
                    if (ix == 0 && iy == 0) continue;
                    float a = Mathf.Atan2(iy, ix);
                    float d = Mathf.Sqrt(ix * ix + iy * iy);

                    float w = 10f;


                    float A = d/w;

                    t.grid.uX[9+ix,21+iy] = Mathf.Cos(a) * A;
                    t.grid.uY[9+ix,21+iy] = Mathf.Sin(a) * A;
                    t.grid.uX_prev[8 + ix, 21 + iy] = -Mathf.Cos(a) * A * 100f;
                    t.grid.uY_prev[8 + ix, 21 + iy] = -Mathf.Sin(a) * A * 100f;

                }
            
    
        }

        GUILayout.Label("Width:\t" + t.width + " \t Height: " + t.height);

        GUILayout.Label("X:\t" + t.stats.minX + " \t " + t.stats.maxX);

        GUILayout.Label("Y:\t" + t.stats.minY + " \t " + t.stats.maxY);

        GUILayout.Label("Pr. Stress:\t" + t.stats.minStress + " \t " + t.stats.maxStress);

        GUILayout.Label("Shear:\t" + t.stats.minShear + " \t " + t.stats.maxShear);

        GUILayout.Label("Pressure:\t" + t.stats.minPressure + " \t " + t.stats.maxPressure);
    }
}
