using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using System;
using Unity.Burst;
using Unity.Mathematics;
using Util;


[BurstCompile]
public struct EvolveStateP : IJobParallelFor
{
    [ReadOnly]
    public DisplacementGrid from;

    [WriteOnly]
    public DisplacementGrid to;

    public float density;                       //1500 - 3000 kg/m^3
    public float lame1; //lambda                //80 GPa
    public float lame2; //µ                     //60-100 GPa

    public float dt;

    public int N;

    public float h;


    public void Execute(int i)
    {


        h = 1f;


        //  for (int iy = 0; iy < state.sy; iy++)
        //      for (int ix = 0; ix < state.sx; ix++)

        int ix = i % from.sx;
        int iy = i / from.sx;

        //if (!state.inBounds(ix, iy)) continue;

        
        float nX = 1.0f, nY = 1.0f;
        if (!from.inBounds(ix, iy))
        {
            //return;
           // nX = Mathf.Abs(from.maskNormal(ix, iy).x);
           // nY = Mathf.Abs(from.maskNormal(ix, iy).y);
        }

        to.uX_prev[ix, iy] = from.uX[ix, iy];
        to.uY_prev[ix, iy] = from.uY[ix, iy];

        to.uX[ix, iy] = 4f / 3f * from.uX[ix, iy] - 1f / 3f * from.uX_prev[ix, iy] + 2f / 3f * dt * stencilX(ix, iy, ref from.uX, ref from.uY) * nX;
        to.uY[ix, iy] = 4f / 3f * from.uY[ix, iy] - 1f / 3f * from.uY_prev[ix, iy] + 2f / 3f * dt * stencilY(ix, iy, ref from.uX, ref from.uY) * nY;

    }


    public int windX(int x, int y)
    {
        float diff = (lame1 + lame2) / h;
        float conv = density * density * from.velX(x, y);

        float peclet = conv / diff;
        if (peclet > 2.0f) return 1;
        if (peclet < -2.0f) return -1;

        return 0;
    }
    public int windY(int x, int y)
    {
        float diff = (lame1 + lame2) / h;
        float conv = density * density * from.velY(x, y);

        float peclet = conv / diff;
        if (peclet > 2.0f) return 1;
        if (peclet < -2.0f) return -1;
        return 0;
    }

    public float stencilX(int x, int y, ref NFA2D uX, ref NFA2D uY)
    {
        float laplace = from.dx2(ref uX, x, y, h) + from.dy2(ref uX, x, y, h);

        int wx = windX(x, y);

        int wy = windY(x, y);


        float div = from.dx2(ref uX, x, y, h) + from.dxy(ref uY, x, y, h);


        return lame1 / density * laplace + (lame1 + lame2) / density * div;
    }

    public float stencilY(int x, int y, ref NFA2D uX, ref NFA2D uY)
    {
        float laplace = from.dx2(ref uY, x, y, h) + from.dy2(ref uY, x, y, h);

        int wx = windX(x, y);

        int wy = windY(x, y);


        float div = from.dy2(ref uY, x, y, h) + from.dxy(ref uX, x, y, h);


        return lame1 / density * laplace + (lame1 + lame2) / density * div;
    }
}

    [BurstCompile]
public struct EvolveState : IJob
{
    [ReadOnly]
    public DisplacementGrid state;

    public NFA2D bufX;
    public NFA2D bufY;
    public NFA2D bufX2;
    public NFA2D bufY2;


    public float dt;

    public int N;

    public float h;
    

    public void Execute()
    {

        bufX.grid.CopyFrom(state.uX.grid);
        bufY.grid.CopyFrom(state.uY.grid);

        h = 0.1f;

        for (int i = 0; i<N; i++)
        {

            for (int iy = 0; iy < state.sy; iy++)
                for (int ix = 0;ix < state.sx; ix++)
                 {
                    //if (!state.inBounds(ix, iy)) continue;
                    var toX = bufX2;
                    var toY = bufY2;
                    var fromX = bufX;
                    var fromY = bufY;
                    if (i%2==1)
                    {
                        toX = bufX;
                        toY = bufY;
                        fromX = bufX2;
                        fromY = bufY2;
                    }

                    float nX = 1.0f, nY = 1.0f;
                    if (!state.inBounds(ix, iy))
                    {
                        nX = Mathf.Abs( state.maskNormal(ix, iy).x );
                        nY = Mathf.Abs( state.maskNormal(ix, iy).y );
                    }

                    toX[ix, iy] = 4f / 3f * state.uX[ix, iy] - 1f / 3f * state.uX_prev[ix, iy] + 2f / 3f * dt * stencilX(ix, iy, ref fromX, ref fromY) * nX * 1.2f;
                    toY[ix, iy] = 4f / 3f * state.uY[ix, iy] - 1f / 3f * state.uY_prev[ix, iy] + 2f / 3f * dt * stencilY(ix, iy, ref fromX, ref fromY) * nY * 1.2f;

                }
        }
    }


    public float stencilX(int x, int y, ref NFA2D uX, ref NFA2D uY)
    {
        float laplace = state.dx2(ref uX, x, y, h) + state.dy2(ref uX, x, y, h);




        float div = state.dx2(ref uX, x, y, h) + state.dxy(ref uY, x, y, h);


        return state.lame2 / state.density * laplace + (state.lame1 + state.lame2) / state.density * div;
    }

    public float stencilY(int x, int y, ref NFA2D uX, ref NFA2D uY)
    {
        float laplace = state.dx2(ref uY, x, y, h) + state.dy2(ref uY, x, y, h);
        


        float div = state.dy2(ref uY, x, y, h) + state.dxy(ref uX, x, y, h);


        return state.lame2 / state.density * laplace + (state.lame1 + state.lame2) / state.density * div;
    }


}

[BurstCompile]
public struct RelaxCells : IJobParallelFor
{
    [ReadOnly]
    public StressGrid from;

    [WriteOnly]
    public StressGrid to;

    public float w;

    public RelaxCells(StressGrid f, StressGrid t, float or) {
        from = f;
        to = t;
        w = or;
        }

    public void Execute(int i)
    {
        int ix = i % to.sx;
        int iy = i / to.sx;

        if (from.getMask(ix,iy))
            to[i] = from[i] - from.stencil_13(ix,iy) / 61f * w;
        else
            to[i] = from[i];
    }

}

public struct RelaxGrid<N> : IJob
    where N : IResidueNorm, new()
{

    public int sx, sy;



    public StressGrid buf1;
    public StressGrid buf2;


    int n;


    int relaxCount;
    float overRelax;

    RelaxStats stats;

    [WriteOnly]
    NativeArray<float> nativeStats;

    N residueNorm;


    public float epsilon;

    public RelaxGrid(StressGrid grid, NativeArray<float> s, int rc = 3, float or = 1.3f, float e = 0.05f)
    {
        
    
        sx = grid.sx;
        sy = grid.sy;
        buf1 = new StressGrid(grid.width, grid.height, null, true);
        buf2 = new StressGrid(grid.width, grid.height, null, true);

        n = 0;

        relaxCount = rc;
        overRelax = or;
        epsilon = e;

        stats = new RelaxStats();
        nativeStats = s;

        for (int iy = 0; iy < sy; iy++)
            for (int ix = 0; ix < sx; ix++)
            {
                //if (!grid.getMask(ix, iy))
                {
                    buf1[ix, iy] = grid[ix, iy];
                    buf2[ix, iy] = grid[ix, iy];

                }
            }
        for (int i = 0; i < grid.width * grid.height; i++)
        {
            buf1.setMask(i, grid.getMask(i));
            buf2.setMask(i, grid.getMask(i));
        }

        residueNorm = new N();

    }

    StressGrid from
    {
        get
        {
            if ((n % 2) == 0) return buf1;
            else return buf2;
        }
    }
    void to(int ix, int iy, float val)
    {

            if ((n % 2) == 0) buf2[ix,iy] = buf2[ix, iy] + val;
            else buf1[ix,iy] = buf1[ix, iy] + val;
    }

    public StressGrid result
    {
        get {
            if ((n % 2) == 0) return buf1;
            else return buf2;
        }
    }

    public void cleanup()
    {
        buf1.cleanup();
        buf2.cleanup();
    }
    void relax(float w = 1.0f)
    {
        Debug.Log("relax, n = " + n);
        if ((n % 2) == 0)
            Debug.Log("from buf1 to buf2");
        else
            Debug.Log("from buf2 to buf1");
           for (int iy = 0; iy < sy; iy++)
            for (int ix = 0; ix < sx; ix++)
            {
                float val = from.getGrid(ix, iy);

                RoidUtil.trackMinMax(val, ref stats.minVal, ref stats.maxVal);

                if (buf1.inBounds(ix, iy))
                {
                    float residue = from.stencil_13(ix, iy);


                    RoidUtil.trackMinMax(residue, ref stats.minResidue, ref stats.maxResidue);


                    residueNorm.Record(residue);

                    if (Mathf.Abs(residue) > epsilon)
                         to(ix, iy, -residue / 61f * w);

                }
            }

    }
    
    void IJob.Execute()
    {
        float prevNorm = float.MaxValue;
        for (int i = 0; i < relaxCount; i++)
        {
            relax(overRelax);

            Debug.Log("residue: " + residueNorm.Value);

            stats.residueNorm = residueNorm.Value;

            if (residueNorm.Value*0.95f > prevNorm)
            {
                break;
            }

            prevNorm = residueNorm.Value;
            residueNorm.Clear();


            n++;
        }


        RoidUtil.StructToNative(stats, nativeStats);
    }


    /*
    int indexLevel(int x, int y, int level = 0)
    {
        if (level < 0 || level > 10) throw new ArgumentOutOfRangeException();
        int step = 1 << level;
        int offset = step / 2;

        return (offset + x * step) + (offset + y * step * width);

    }*/

};


