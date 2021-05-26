using System.Collections;
using System.Collections.Generic;

using System;
using System.Reflection;

using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

using Util;


[BurstCompile]
public struct DisplacementGridToTex : IJob
{

    public enum TYPE
    {
        COL,
        LINE,
        STRESS,
        DISTORT,

        NORMAL,
        BOUNDARY,
    }


    [ReadOnly]
    public DisplacementGrid grid;

    [ReadOnly]
    public TYPE type;

    [WriteOnly]
    public NativeArray<Color32> res;

    public NativeArray<float> stat;

    public float minX, maxX,
                 minY, maxY,
                minStress,maxStress,
                 minShear, maxShear,
                 minPressure,maxPressure;


    public DisplacementGridToTex(DisplacementGrid g, TYPE tp, NativeArray<Color32> r, NativeArray<float> s)
    {
        grid = g;
        type = tp;

        res = r;
        stat = s;

        minX = float.MaxValue;
        maxX = float.MinValue;


        minY = float.MaxValue;
        maxY = float.MinValue;

        minStress = float.MaxValue;
        maxStress = float.MinValue;

        minShear = float.MaxValue;
        maxShear = float.MinValue;


        minPressure = float.MaxValue;
        maxPressure = float.MinValue;

    }

    public void Execute()
    {
        if (type == TYPE.BOUNDARY)
        {
            for (int iy = 0; iy < grid.sy; iy++)
                for (int ix = 0; ix < grid.sx; ix++)
                {
                    float r = 0.0f, g = 0.0f, b = 0.0f;
                    float a = 1.0f;
                    if (grid.inBounds(ix, iy))
                        b = 0.75f;
                    else
                        a = 0.0f;

                    Color col = new Color(r, g, b, a);
                    for (int jy = 0; jy < 10; jy++)
                        for (int jx = 0; jx < 10; jx++)
                            res[ix * 10 + jx + (iy * 10 + jy) * grid.sx * 10] = col;
                }
        }
        else if (type == TYPE.COL)
            ColTex();
        else if (type == TYPE.LINE)
            LineTex();
        else if (type == TYPE.STRESS)
            StressTex();
        else if (type == TYPE.DISTORT)
            DistortionTex();
        else if (type == TYPE.NORMAL)
            NormalTex();



        stat[0] = minX;
        stat[1] = maxX;

        stat[2] = minY;
        stat[3] = maxY;

        stat[4] = minStress;
        stat[5] = maxStress;

        stat[6] = minShear;
        stat[7] = maxShear;

        stat[8] = minPressure;
        stat[9] = maxPressure;
    }

    private void ColTex()
    {

        for (int i = 0; i < grid.size; i++)
        {
            RoidUtil.trackMinMax(grid.uX[i], ref minX, ref maxX);
            RoidUtil.trackMinMax(grid.uY[i], ref minY, ref maxY);
        }



        for (int iy = 0; iy < grid.sy; iy++)
            for (int ix = 0; ix < grid.sx; ix++)
            {


                Color bgCol = new Color(0, 0, 0.25f);

                float valX = grid.uX[ix, iy];
                float valY = grid.uY[ix, iy];

                Color32 colX = RoidUtil.LerpValueColor(valX, minX, maxX, bgCol, Color.red, Color.cyan);
                Color32 colY = RoidUtil.LerpValueColor(valY, minY, maxY, bgCol, Color.magenta, Color.green);

                Color32 col = Color32.Lerp(colX,colY,0.5f);
                if (!grid.inBounds(ix, iy)) col = Color32.Lerp(col,new Color32(0,0,0,255),0.5f);


                for (int jy = 0; jy < 10; jy++)
                    for (int jx = 0; jx < 10; jx++)
                    {
                        var pCol = col;
                        if (jy % 10 <= 1 || jx % 10 <= 1) pCol = Color32.Lerp(pCol, new Color32(0, 0, 0, 255), 0.66f);
                        res[ix * 10 + jx + (iy * 10 + jy) * grid.sx * 10] = pCol;
                    }

            }
    }
    private void LineTex()
    {

        for (int i = 0; i < grid.size; i++)
        {
            RoidUtil.trackMinMax(grid.uX[i], ref minX, ref maxX);
            RoidUtil.trackMinMax(grid.uY[i], ref minY, ref maxY);
        }



        for (int iy = 0; iy < grid.sy; iy++)
            for (int ix = 0; ix < grid.sx; ix++)
            {

                Color32 bgCol = new Color32(0, 0, 63, 255);

                float valX = grid.uX[ix, iy];
                float valY = grid.uY[ix, iy];

                float scaleX = Mathf.Max(Mathf.Abs(minX), Mathf.Abs(maxX));
                float scaleY = Mathf.Max(Mathf.Abs(minY), Mathf.Abs(maxY));

                float scale = Mathf.Max(scaleX, scaleY);

                Color32 col = new Color32(255,0,0,255);
                if (!grid.inBounds(ix, iy))
                {
                    col = Color32.Lerp(col, new Color32(0, 0, 0, 255), 0.66f);
                    bgCol = Color32.Lerp(bgCol, new Color32(0, 0, 0, 255), 0.5f);
                }

                for (int jy = 0; jy < 10; jy++)
                    for (int jx = 0; jx < 10; jx++)
                    {
                        var borCol = bgCol;
                        if (jy % 10 == 0 || jx % 10 == 0) borCol = Color32.Lerp(borCol, new Color32(0, 0, 0, 255), 0.5f);
                        res[ix * 10 + jx + (iy * 10 + jy) * grid.sx * 10] = borCol;
                    }

                if (!float.IsNaN(valX) && !float.IsNaN(valY) && !float.IsInfinity(valX) && !float.IsInfinity(valY))
                {
                    float lx = Mathf.RoundToInt(Mathf.Clamp(valX/scale, -1f, 1f)*5f);
                    float ly = Mathf.RoundToInt(Mathf.Clamp(valY/scale, -1f, 1f) * 5f);

                    for (float i = 0; i < 1f; i+= 0.1f )
                        {

                        int px = ix * 10 + 5;
                            px += Mathf.RoundToInt(lx * i);
                        int py = iy * 10 + 5;
                            py += Mathf.RoundToInt(ly * i);

                        res[px + py * grid.sx * 10] = col;
                    }
                }
            }
    }

    void StressTex()
    {

        Color bgCol = new Color(0, 0, 0.25f);



        for (int iy = 0; iy < grid.sy; iy++)
            for (int ix = 0; ix < grid.sx; ix++)
         {
                Vector2 stress = grid.principalStress(ix, iy);

                RoidUtil.trackMinMax(stress.x, ref minStress, ref maxStress);
                RoidUtil.trackMinMax(stress.y, ref minStress, ref maxStress);
        }


        float scale = Mathf.Max(Mathf.Abs(minStress), Mathf.Abs(maxStress));

        for (int iy = 0; iy < grid.sy; iy++)
            for (int ix = 0; ix < grid.sx; ix++)
            {

                Vector2 stress = grid.principalStress(ix, iy);
                float ang = grid.principalAngle(ix, iy);

                

                Color32 col1 = new Color32(255, 0, 0, 255);
                Color32 col2 = new Color32(0, 255, 0, 255);

                if (!grid.inBounds(ix, iy))
                {
                    bgCol = Color32.Lerp(bgCol, new Color32(0, 0, 0, 255), 0.5f);
                }

                for (int jy = 0; jy < 10; jy++)
                    for (int jx = 0; jx < 10; jx++)
                    {
                        var borCol = bgCol;
                        if (jy % 10 == 0 || jx % 10 == 0) borCol = Color32.Lerp(borCol, new Color32(0, 0, 0, 255), 0.5f);
                        res[ix * 10 + jx + (iy * 10 + jy) * grid.sx * 10] = borCol;
                    }

                if (!float.IsNaN(ang) && !float.IsInfinity(ang))
                {

                    float d1 = Mathf.Clamp(stress.x / scale, -1f, 1f) * 5f;
                    float d2 = Mathf.Clamp(stress.y / scale, -1f, 1f) * 5f;


                    for (float i = 0; i < 1f; i += 0.1f)
                    {


                        float lx = Mathf.Cos(ang + Mathf.PI * 0.5f);
                        float ly = Mathf.Sin(ang + Mathf.PI * 0.5f);

                        int px = ix * 10 + 5;
                        px += Mathf.RoundToInt(lx * i * d1);
                        int py = iy * 10 + 5;
                        py += Mathf.RoundToInt(ly * i * d1);

                        res[px + py * grid.sx * 10] = col1;

                        lx = Mathf.Cos(ang );

                        ly = Mathf.Sin(ang);

                        px = ix * 10 + 5;
                        px += Mathf.RoundToInt(lx * i *  d2);
                        py = iy * 10 + 5;
                        py += Mathf.RoundToInt(ly * i * d2 );


                        res[px + py * grid.sx * 10] = col2;

                    }
                }
            }




    }
    void DistortionTex()
    {

        Color bgCol = new Color(0, 0, 0.25f);

        for (int pass = 0; pass <= 1; pass++)
            for (int iy = 0; iy < grid.sy; iy++)
                for (int ix = 0; ix < grid.sx; ix++)
                {
                    bool xbetween = ix % 2 == 0;
                    bool ybetween = iy % 2 == 0;

                    Color col = Color.black;


                    //between cells on x or y axis, but not both
                    if (xbetween ^ ybetween)
                    {

                        var s = grid.shear(ix, iy);
                        if (pass == 0)
                        {
                            RoidUtil.trackMinMax(s, ref minShear, ref maxShear);
                        }
                        else
                        {

                            col = RoidUtil.LerpValueColor(s, minShear, maxShear, bgCol, Color.cyan, Color.yellow);

                        }

                    }
                    //inside a cell
                    else if (!xbetween && !ybetween)
                    {
                        var p = grid.pressure(ix, iy);
                        if (pass == 0)
                        {
                            RoidUtil.trackMinMax(p, ref minPressure, ref maxPressure);
                        }
                        else
                        {

                            col = RoidUtil.LerpValueColor(p, minPressure, maxPressure, bgCol, Color.magenta, Color.green);

                        }
                    }

                    if (pass == 1)
                    {
                        if (!grid.inBounds(ix, iy)) col *= 0.5f;


                        for (int jy = 0; jy < 10; jy++)
                            for (int jx = 0; jx < 10; jx++)
                            {
                                var pCol = col;
                                if (jy % 10 <= 1 || jx % 10 <= 1) pCol = Color32.Lerp(pCol, new Color32(0, 0, 0, 255), 0.66f);
                                res[ix * 10 + jx + (iy * 10 + jy) * grid.sx * 10] = pCol;
                            }
                    }
                }

    }

    private void NormalTex()
    {

        for (int iy = 0; iy < grid.sy; iy++)
            for (int ix = 0; ix < grid.sx; ix++)
            {


                Color32 bgCol = new Color32(0, 0, 63, 255);

                Color32 col = new Color32(255, 0, 0, 255);
                if (!grid.inBounds(ix, iy))
                {
                    bgCol = Color32.Lerp(bgCol, new Color32(0, 0, 0, 255), 0.95f);
                }

                for (int jy = 0; jy < 10; jy++)
                    for (int jx = 0; jx < 10; jx++)
                    {
                        var borCol = bgCol;
                        if (jy % 10 == 0 || jx % 10 == 0) borCol = Color32.Lerp(borCol, new Color32(0, 0, 0, 255), 0.5f);
                        res[ix * 10 + jx + (iy * 10 + jy) * grid.sx * 10] = borCol;
                    }


                if (grid.inBounds(ix, iy)) continue;

                float nx = -grid.maskNormal(ix, iy,1).x;
                float ny = -grid.maskNormal(ix, iy,1).y;

                for (float i = 0; i < 5f; i += 0.1f)
                {

                    int px = ix * 10 + 5;
                    px += -Mathf.RoundToInt(Mathf.Clamp(nx*4f, -4f, 4f));
                    int py = iy * 10 + 5;
                    py += -Mathf.RoundToInt(Mathf.Clamp(ny*4f, -4f, 4f));

                    res[px + py * grid.sx * 10] = col;
                }
            }
    }
}


[BurstCompile]
public struct StressGridToTex : IJob
{

    public enum TYPE
    {
        VAL,
        RESIDUE,
        STRESS,


        BOUNDARY,
    }
    

    [ReadOnly]
    public StressGrid grid;

    [ReadOnly]
    public TYPE type;

    [WriteOnly]
    public NativeArray<Color> res;

    public NativeArray<float> stat;

    public float minVal, maxVal,
                 minShear, maxShear,
                 minPressure, maxPressure,
                 minResidue, maxResidue;


    public StressGridToTex(StressGrid g, TYPE tp, NativeArray<Color> r,NativeArray<float> s)
    {
        grid = g;
        type = tp;

        res = r;
        stat = s;

        minShear = float.MaxValue;
        maxShear = float.MinValue;

        minPressure = float.MaxValue;
        maxPressure = float.MinValue;

        minVal = stat[4];
        maxVal = stat[5];

        minResidue = stat[6];
        maxResidue = stat[7];
    }

    public void Execute()
    {
        if (type == TYPE.BOUNDARY)
        {
            for (int iy = 0; iy < grid.sy; iy++)
                for (int ix = 0; ix < grid.sx; ix++)
                {
                    float r = 0.0f, g = 0.0f, b = 0.0f;
                    float a = 1.0f;
                    if (grid.inBounds(ix, iy))
                        b = 0.75f;
                    else
                        a = 0.0f;

                    Color col = new Color(r, g, b, a);
                    res[ix + iy * grid.sx] = col;
                }
        }
        else if (type == TYPE.VAL)
            ValueTex();
        else if (type == TYPE.STRESS)
            StressTex();
        else if (type == TYPE.RESIDUE)
            ResidueTex();


         stat[0] = minShear;
         stat[1] = maxShear;

         stat[2] = minPressure;
         stat[3] = maxPressure;


        stat[4] = minVal;
        stat[5] = maxVal;

        stat[6] = minResidue;
        stat[7] = maxResidue;
    }



    private void ValueTex()
    {
        
        for (int i = 0; i < grid.size; i++)
        {
            RoidUtil.trackMinMax(grid[i], ref minVal, ref maxVal);
        }
        

        Color bgCol = new Color(0,0, 0.25f);

        for (int iy = 0; iy < grid.sy; iy++)
            for (int ix = 0; ix < grid.sx; ix++)
            {
                float val = grid[ix, iy];

                Color col = RoidUtil.LerpValueColor(val,minVal, maxVal, bgCol,Color.red,Color.green);
                if (!grid.inBounds(ix, iy)) col *= 0.5f;


                res[ix + iy * grid.sx] = col;
            }
    }

    private void StressTex()
    {

        Color bgCol = new Color(0,0, 0.25f);

        for (int pass = 0; pass <= 1; pass++)
            for (int iy = 0; iy < grid.sy; iy++)
                for (int ix = 0; ix < grid.sx; ix++)
                {
                    bool xbetween = ix % 2 == 0;
                    bool ybetween = iy % 2 == 0;

                    Color col = Color.black;


                    //between cells on x or y axis, but not both
                    if (xbetween ^ ybetween)
                    {

                        var s = grid.shear(ix, iy);
                        if (pass == 0)
                        {
                            RoidUtil.trackMinMax(s, ref minShear, ref maxShear);
                        }
                        else
                        {

                        col = RoidUtil.LerpValueColor(s, minShear, maxShear, bgCol, Color.cyan, Color.yellow,false);

                        }

                    }
                    //inside a cell
                    else if (!xbetween && !ybetween )
                        {
                            var p = grid.pressure(ix, iy);
                            if (pass == 0)
                            {
                                RoidUtil.trackMinMax(p, ref minPressure, ref maxPressure);
                            }
                            else
                            {

                            col = RoidUtil.LerpValueColor(p, minPressure, maxPressure, bgCol, Color.magenta, Color.green);

                            }       
                     }

                    if (pass == 1)
                    {
                        if (!grid.inBounds(ix, iy)) col *= 0.5f;
                        res[ix + iy * grid.sx] = col;
                    }
                }

    }


    private void ResidueTex()
    {
        Color bgCol = new Color(0,0, 0.5f);

        for (int pass = 0; pass <= 1; pass++)
        //int pass = 1;
            for (int iy = 0; iy < grid.sy; iy++)
                for (int ix = 0; ix < grid.sx; ix++)
                {
                    float r = grid.stencil_13(ix, iy);
                    if(pass==0 && grid.inBounds(ix,iy) )
                    {
                        RoidUtil.trackMinMax(r, ref minResidue, ref maxResidue);
                    }
                    else
                    {
                        Color col;
                        if (grid.inBounds(ix, iy))
                            col = RoidUtil.LerpValueColor(r, minResidue, maxResidue, bgCol, Color.red, Color.green);
                        else col = bgCol * 0.5f;
                        res[ix + iy * grid.sx] = col;
                    }
                }
    }
}