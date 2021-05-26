using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEditor;
using Util;


[ExecuteInEditMode]
public class StressState : MonoBehaviour, ISerializationCallbackReceiver
{

    public StressGridToTex.TYPE type;

    int relaxCount;
    public float overRelax;

    
    public StressGrid grid;

    [SerializeField]
    List<float> stress_serialized;

    Texture2D tex;

    public GridStats stats;

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

        grid = new StressGrid(width, height);
 

        if(tex ==null) tex = new Texture2D(width * 2 + 1, height * 2 + 1);
        tex.filterMode = FilterMode.Point;
        GetComponent<MeshRenderer>().material.mainTexture = tex;

        if (stress_serialized.Count > 0) grid.stress.CopyFrom(stress_serialized.ToArray());
        stress_serialized.Clear();

        UpdateMask();

        //Debug.Log("has grid: " + hasGrid);
    }

    void UpdateMask()
    {
        var tm = GetComponent<AsteroidTilemap>();
        for (int i = 0; i < tm.width * tm.height; i++)
        {
            ((StressGrid)grid).setMask(i, tm.tiles[i] > 0);
        }
    }


    public void Reset()
    {
        cleanup();

        GetSize();

        overRelax = 1.7f;

        Init();
    }

    void cleanup()
    {
        if (hasGrid) {
            grid.cleanup();
        }

    }

    
    public void OnBeforeSerialize()
    {
        if (stress_serialized == null)
            stress_serialized = new List<float>();
        else
            stress_serialized.Clear();
        if (hasGrid)
        {
            stress_serialized.AddRange(grid.stress);
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

        var g = (StressGrid)grid;

        var res = new NativeArray<Color>(g.sx * g.sy, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var stat = new NativeArray<float>(9, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


        RoidUtil.StructToNative(stats.stress, GridStats.getRelaxArray(stat) );
        RoidUtil.StructToNative(stats.relax, GridStats.getRelaxArray(stat) );

        var j = new StressGridToTex(g, type, res, stat);


        var handle = j.Schedule();

        handle.Complete();


        tex.SetPixels(res.ToArray());
        tex.Apply();

        stats.stress = RoidUtil.NativeToStruct<StressStats>( GridStats.getStressArray(stat) );
        stats.relax = RoidUtil.NativeToStruct<RelaxStats>(GridStats.getRelaxArray(stat));

        res.Dispose();
        stat.Dispose();
    }

    public void UpdateStress()
    {
        if (!hasGrid) Init();

        UpdateMask();

        var g = (StressGrid)grid;

        var t = new StressGrid(g.width, g.height,null, true);

        var relaxStat = new NativeArray<float>(5, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);


        //var j = new RelaxGrid<L2Norm>(g,relaxStat,relaxCount, overRelax);

        //j.Run();

        var j = new RelaxCells(g, t, overRelax);

        var handle = j.Schedule(g.sx*g.sy,32);

        handle.Complete();
       

        for (int i = 0; i < g.size; i++) g[i] = j.to[i];

        //stats.relax = RoidUtil.NativeToStruct<RelaxStats>(relaxStat);

        //j.cleanup();
        relaxStat.Dispose();
        t.cleanup();
    }

}
