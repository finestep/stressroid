using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AsteroidTilemap : MonoBehaviour {

    public float tileSize = 0.1f;
    

    public int width = 50;
    public int height = 50;

    private bool hasMesh = false;

    public byte[] tiles;

    public BoxCollider2D[] boxes;

    public bool debugtex;

    Mesh mesh;

    public void Reset()
    {
        InitTiles();

        if (mesh != GetComponent<MeshFilter>().sharedMesh)
        {
            mesh = GetComponent<MeshFilter>().mesh;
            if (mesh == null || mesh.name == "None")
                mesh = new Mesh();
            hasMesh = true;

            mesh.MarkDynamic();
        }

        UpdateMesh();
        InitCollider();
    }

	// Use this for initialization
	void Awake () {
        
        Reset();
    }

    bool needsToInit()
    {
        return tiles == null || boxes == null;
    }

    int index(int x, int y)
    {
        return x + y * (int)width;
    }

    byte getTile(int x, int y)
    {

        if (x < 0 || x >= width || y < 0 || y >= height) return 0;

        return tiles[x+y*width];

    }

    void InitTiles()
    {


        DestroyImmediate(gameObject.GetComponent<Grid>());
        Grid g = gameObject.AddComponent<Grid>();
        g.cellSize = new Vector2(tileSize, tileSize);

        foreach (BoxCollider2D b in gameObject.GetComponents<BoxCollider2D>())
            DestroyImmediate(b);

        tiles = new byte[height*width];
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                tiles[index(j,i)] = 1;

        boxes = new BoxCollider2D[height*width];

        gameObject.GetComponent<Rigidbody2D>().mass = 0.0f;
    }

    public void SetTile(int x, int y, byte type)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        var oldType = tiles[index(x,y)];
        if (type == oldType) return;
        tiles[index(x,y)] = type;
        UpdateMesh();
        UpdateCollider(x, y, type, oldType);
    }

    void verticesAt(List<Vector3> vertices, int ix, int iy)
    {

        float py = iy * tileSize;
        float px = ix * tileSize;

        vertices.Add(new Vector3(px, py));

        vertices.Add(new Vector3(px + tileSize, py));


        vertices.Add(new Vector3(px, py + tileSize));


        vertices.Add(new Vector3(px + tileSize, py + tileSize));

    }
    void uvsAt(List<Vector2> uvs, int px, int py)
    {

        float halfx = 0.25f / width;
        float halfy = 0.25f / height;

        uvs.Add(new Vector2( (float)    (px * 2) / (width*2 + 1 ) + halfx,
                             (float)    (py * 2) / (height * 2 + 1) + halfy));
        uvs.Add(new Vector2((float)     (px * 2 + 2) / (width * 2 + 1) + halfx,
                             (float)    (py * 2) / (height * 2 + 1) + halfy));
        uvs.Add(new Vector2((float)     (px * 2) / (width * 2 + 1) + halfx,
                             (float)    (py * 2 + 2) / (height * 2 + 1) + halfy));
        uvs.Add(new Vector2((float)     (px * 2 + 2) / (width * 2 + 1) + halfx,
                            (float)     (py * 2 + 2) / (height * 2 + 1) + halfy));
    }

    public void UpdateMesh()
    {
        if (needsToInit()) InitTiles();
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();

        int n = 0;

        int tileCount = 0;
        
        for (int iy = 0; iy < height; iy++)
            for (int ix = 0; ix < width; ix++)
            {
                if (getTile(ix, iy) > 0)
                {
                    tileCount++;

                    verticesAt(vertices, ix, iy);

                    if (debugtex)
                            uvsAt(uvs, ix, iy);
                    else {
                            Color col = new Color(0.4f, 0.1f, 0.2f, 1.0f);
                            colors.Add(col); colors.Add(col); colors.Add(col); colors.Add(col);
                     }

                    triangles.Add(n + 2); triangles.Add(n + 1); triangles.Add(n);


                    triangles.Add(n + 1); triangles.Add(n + 2); triangles.Add(n + 3);

                    n += 4;

                }
            }

        mesh.Clear();

        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        if (debugtex)
        {
            mesh.SetUVs(0, uvs.ToArray());
            GetComponent<MeshRenderer>().sharedMaterial.SetShaderPassEnabled("VertexColor",true);
            GetComponent<MeshRenderer>().sharedMaterial.SetShaderPassEnabled("DebugTex", false);
        }
        else
        {
            mesh.colors = colors.ToArray();
            GetComponent<MeshRenderer>().sharedMaterial.SetShaderPassEnabled("VertexColor", false);
            GetComponent<MeshRenderer>().sharedMaterial.SetShaderPassEnabled("DebugTex", true);
        }
        mesh.MarkModified();
        
    }

    public void InitCollider()
    {
        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
            {
                if (getTile(j, i) > 0) {
                    float py = i * tileSize;
                    float px = j * tileSize;

                    BoxCollider2D box;
                    if (boxes[index(j, i)] == null)
                    {
                        box = gameObject.AddComponent<BoxCollider2D>();
                        boxes[index(j, i)] = box;
                    }
                    else
                    {
                        box = boxes[index(j, i)];
                    }

                    box.usedByComposite = true;

                    box.hideFlags = HideFlags.HideInInspector;

                    box.offset = new Vector2(px + tileSize / 2, py + tileSize / 2);
                    box.size = new Vector2(tileSize, tileSize);


                    gameObject.GetComponent<Rigidbody2D>().mass += 0.1f;
                }
                else
                {
                    DestroyImmediate(boxes[index(j, i)]);
                    boxes[index(j, i)] = null;
                }
            } 


        gameObject.GetComponent<CompositeCollider2D>().GenerateGeometry();

    }

    void UpdateCollider(int x, int y, byte type, byte oldType)
    {
        if (type == 0)
        {
            DestroyImmediate(boxes[index(x, y)]);
            boxes[index(x, y)] = null;
            gameObject.GetComponent<Rigidbody2D>().mass -= 0.1f;


        } else if (type>0)
        {
            float py = y * tileSize;
            float px = x * tileSize;

            var box = gameObject.AddComponent<BoxCollider2D>();
            boxes[index(x, y)] = box;

            box.usedByComposite = true;

            box.hideFlags = HideFlags.HideInInspector;

            box.offset = new Vector2(px + tileSize / 2, py + tileSize / 2);
            box.size = new Vector2(tileSize, tileSize);


            gameObject.GetComponent<Rigidbody2D>().mass += 0.1f;
        }


        gameObject.GetComponent<CompositeCollider2D>().GenerateGeometry();
    }


    void Update () {

	}

    void OnDestroy()
    {
        if (hasMesh)
            Destroy(GetComponent<MeshFilter>().mesh);
    }
}
