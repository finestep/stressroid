using UnityEngine;
using UnityEditor;

using System;
using System.Collections.Generic;

[CustomEditor(typeof(AsteroidTilemap))]
[CanEditMultipleObjects]
public class AsteroidTilemapEditor : Editor {

    [SerializeField]
    int selectedType = 0;


    [SerializeField]
    bool editing;

    void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var t = ((AsteroidTilemap)target);
        base.OnInspectorGUI();

        if (t.tiles == null)
            GUILayout.Label("tiles = null");
        else
        {
            var count = 0;
            foreach (var x in t.tiles)
                if (x > 0) count++;
            GUILayout.Label(count + " tiles");
        }
        if (t.boxes == null)
            GUILayout.Label("boxes = null");
        else
        {
            var count = 0;
            foreach (var b in t.boxes)
                if (b!=null) count++;
            GUILayout.Label(count+ " boxes");
        }
        if (GUILayout.Button("Update"))
        {
            t.UpdateMesh();
            t.InitCollider();
        }
    }

    public void OnSceneGUI()
    {

        var t = ((AsteroidTilemap)target);

        var offset = HandleUtility.WorldToGUIPoint( new Vector2(
                t.gameObject.transform.position.x - 1,
                t.gameObject.transform.position.y + t.height * t.tileSize)
            );
        
        var typepos = new Rect(offset, new Vector2(60f, 18f));
        offset.y += 22f;
        var togglepos = new Rect(offset, new Vector2(60f, 15f));

        Handles.BeginGUI();

        if(GUI.Button(typepos, "type " +selectedType))
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("0"), false, () => { this.selectedType = 0; });
            menu.AddItem(new GUIContent("1"), false, () => { this.selectedType = 1; });

            menu.ShowAsContext();
        }

        editing = GUI.Toggle(togglepos, editing,"edit");


        Event current = Event.current;

        if (editing )
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (current.type == EventType.MouseDrag || current.type == EventType.MouseUp)
            {
                Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Vector2 worldPos = new Vector2(worldRay.origin.x, worldRay.origin.y);

                var gridPos = t.gameObject.GetComponent<Grid>().WorldToCell(worldPos);


                t.SetTile(gridPos.x, gridPos.y, (byte)selectedType);

                current.Use();

            } else if (Event.current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlID);
        }

        Handles.EndGUI();

        if(editing)
        {
            var lines = new List<Vector3>();

            var tpos = t.transform.position;
            tpos.z = 1;

            for (float ix = 0; ix < (t.width+1) * t.tileSize; ix += t.tileSize)
            {
                lines.Add(tpos + new Vector3(ix, 0));
                lines.Add(tpos + new Vector3(ix, t.height * t.tileSize));
            }
            for (float iy = 0; iy < (t.height+1) * t.tileSize; iy += t.tileSize)
            {
                lines.Add(tpos + new Vector3(0, iy));
                lines.Add(tpos + new Vector3(t.width * t.tileSize, iy));
            }

            Handles.DrawLines(lines.ToArray());
        }
   
    }
}
