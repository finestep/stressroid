using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEditor;
using Util;


[ExecuteInEditMode]
public class DisplacementState : MonoBehaviour, ISerializationCallbackReceiver
{

    public DisplacementGridToTex.TYPE type;


    public DisplacementGrid grid1;
    public DisplacementGrid grid2;

    private bool currentGrid;
    public ref DisplacementGrid grid {
        get
        {
            if (currentGrid) return ref grid2;
            else return ref grid1;
        }
    }
    public ref DisplacementGrid nextGrid
    {
        get
        {
            if (!currentGrid) return ref grid2;
            else return ref grid1;
        }
    }

    [SerializeField]
    List<float> uX_serialized;
    [SerializeField]
    List<float> uY_serialized;
    [SerializeField]
    List<float> uX_prev_serialized;
    [SerializeField]
    List<float> uY_prev_serialized;


    public float deltatime;

    public int iterations;

    public float density;
    public float lameComp;
    public float lameRigid;


    Texture2D tex;

    public DisplacementStats stats;

    [SerializeField]
    public
    int width, height;

    public bool hasGrid
    {
        get
        {
            return grid.IsCreated;
        }
    }

    void GetSize()
    {
        var tm = GetComponent<AsteroidTilemap>();
        width = tm.width;
        height = tm.height;
    }
    void Awake()
    {
        GetSize();
    }

    void OnEnable()
    {

        Init();

        UpdateTex();
    }

    void Start()
    {

    }

    void Init()
    {

        //Debug.Log("New grid of size " + width + " " + height);

        grid1 = new DisplacementGrid(width, height,density,lameComp,lameRigid);
        grid2 = new DisplacementGrid(width, height, density, lameComp, lameRigid);

        currentGrid = false;

        tex = new Texture2D(grid.sx * 10, grid.sy * 10,TextureFormat.RGBA32,false);
        tex.filterMode = FilterMode.Point;
        GetComponent<MeshRenderer>().material.mainTexture = tex;

        if (uX_serialized.Count > 0) grid.uX.grid.CopyFrom(uX_serialized.ToArray());
        if (uY_serialized.Count > 0) grid.uY.grid.CopyFrom(uY_serialized.ToArray());
        if (uX_prev_serialized.Count > 0) grid.uX_prev.grid.CopyFrom(uX_prev_serialized.ToArray());
        if (uY_prev_serialized.Count > 0) grid.uY_prev.grid.CopyFrom(uY_prev_serialized.ToArray());

        uX_serialized.Clear();
        uY_serialized.Clear();
        uX_prev_serialized.Clear();
        uY_prev_serialized.Clear();

        UpdateMask();

        //Debug.Log("has grid: " + hasGrid);
    }

    void UpdateMask()
    {
        var tm = GetComponent<AsteroidTilemap>();
        for (int i = 0; i < tm.width * tm.height; i++)
        {
            grid1.mask[i] = tm.tiles[i] > 0;
            grid2.mask[i] = tm.tiles[i] > 0;
        }
    }


    public void Reset()
    {
        cleanup();

        GetSize();


        Init();
    }

    void cleanup()
    {
        if (hasGrid)
        {
            grid.Dispose();
        }

    }


    public void OnBeforeSerialize()
    {
        if (uX_serialized == null)
            uX_serialized = new List<float>();
        else
            uX_serialized.Clear();

        if (uY_serialized == null)
            uY_serialized = new List<float>();
        else
            uY_serialized.Clear();

        if (uX_prev_serialized == null)
            uX_prev_serialized = new List<float>();
        else
            uX_prev_serialized.Clear();

        if (uY_prev_serialized == null)
            uY_prev_serialized = new List<float>();
        else
            uY_prev_serialized.Clear();

        if (hasGrid)
        {
            uX_serialized.AddRange(grid.uX.grid);
            uY_serialized.AddRange(grid.uY.grid);

            uX_prev_serialized.AddRange(grid.uX_prev.grid);
            uY_prev_serialized.AddRange(grid.uY_prev.grid);
        }

        //Debug.Log("Deserialized " + stress_serialized.Count + " cells");

    }

    public void OnAfterDeserialize()
    {
        //Debug.Log("Deserialized " + stress_serialized.Count + " cells");

    }


    public void OnDestroy()
    {
        cleanup();
    }

    public void UpdateTex()
    {
        if (!hasGrid) Init();

        UpdateMask();

        var g = grid;

        var res = tex.GetRawTextureData<Color32>();
        var stat = new NativeArray<float>(10, Allocator.TempJob, NativeArrayOptions.ClearMemory);


        RoidUtil.StructToNative(stats,stat);

        var j = new DisplacementGridToTex(g, type, res, stat);


        var handle = j.Schedule();

        handle.Complete();
        
        tex.Apply();

        stats = RoidUtil.NativeToStruct<DisplacementStats>(stat);

        
        stat.Dispose();
    }

    public void UpdateDisplacement()
    {
        if (!hasGrid) Init();

        UpdateMask();

        var g = grid;

        var t = new Timer();


        var j = new EvolveStateP();

        j.dt = deltatime;

        j.N = iterations;

        j.density = density;
        j.lame1 = lameComp;
        j.lame2 = lameRigid;


        j.from = grid;
        j.to = nextGrid;

        var handle = j.Schedule(g.sx*g.sy,3);

        handle.Complete();

        nextGrid.uX.grid.CopyFrom(j.to.uX.grid);
        nextGrid.uY.grid.CopyFrom(j.to.uY.grid);
        nextGrid.uX_prev.grid.CopyFrom(j.to.uX_prev.grid);
        nextGrid.uY_prev.grid.CopyFrom(j.to.uY_prev.grid);
        currentGrid = !currentGrid;

        //Debug.Log("Displacement update took " + t.µs() + " µs");

        /*

        var bufX = new CheckedNativeFA(width, height,Allocator.TempJob);
        var bufY = new CheckedNativeFA(width, height, Allocator.TempJob);
        var bufX2 = new CheckedNativeFA(width, height, Allocator.TempJob);
        var bufY2 = new CheckedNativeFA(width, height, Allocator.TempJob);
        
        var j = new EvolveState();

        j.dt = deltatime;

        j.N = iterations;

        j.density = density;
        j.lame1 = lameComp;
        j.lame2 = lameRigid;


        j.state = grid;

        j.bufX = bufX;
        j.bufY = bufY;

        j.bufX2 = bufX2;
        j.bufY2 = bufY2;


        var handle = j.Schedule();

        handle.Complete();


        grid.uX_prev.grid.CopyFrom(grid.uX.grid.ToArray());
        grid.uY_prev.grid.CopyFrom(grid.uY.grid.ToArray());

        grid.uX.grid.CopyFrom(bufX.grid.ToArray());
        grid.uY.grid.CopyFrom(bufY.grid.ToArray());


        bufX.Dispose();
        bufY.Dispose();

        bufX2.Dispose();
        bufY2.Dispose();
        */
    }

}
