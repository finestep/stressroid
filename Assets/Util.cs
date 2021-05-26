using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System;
using System.Diagnostics;
using Unity.Collections;


namespace Util
{

    public class Timer
    {
        public Stopwatch t;
        public Timer()
        {
            t =  Stopwatch.StartNew();
        }

        public float µs()
        {
            return (float) (1000L * 1000L) / Stopwatch.Frequency * t.ElapsedTicks;
        }
    }

    public static class RoidUtil
    {
        public static void trackMinMax(float val, ref float low, ref float high)
        {
            if (val < low)
                low = val;
            if (val > high)
                high = val;
        }

        public static void StructToNative<T>(T t, NativeArray<float> to)
            where T : unmanaged
        {
            if (!to.IsCreated)
                throw new ArgumentNullException();
            //           if (to.Length != Enumerable.Count(typeof(T).GetTypeInfo().DeclaredMembers) )
            //               throw new ArgumentException();
            to.ReinterpretStore(0, t);
        }
        public static T NativeToStruct<T>(NativeArray<float> from)
             where T : unmanaged
        {
            if (!from.IsCreated)
                throw new ArgumentNullException();
            //           if (from.Length != Enumerable.Count(typeof(T).GetTypeInfo().DeclaredMembers) )
            //               throw new ArgumentException();
            return from.ReinterpretLoad<T>(0);
        }
        public static Color LerpValueColor(float val, float low, float high, Color bgCol, Color minCol, Color maxCol, bool splitAtZero = true)
        {
            if (Mathf.Abs(low - high) < 0.0005f) return bgCol;

            //if (Mathf.Abs(low) < 0.0005f) low = -high;
            //else if (Mathf.Abs(high) < 0.0005f) high = -low;

            Color col;
            if (val < (splitAtZero ? 0
                            : low + high / 2f)
            )
                col = Color.Lerp(bgCol, minCol, val / low);
            else
                col = Color.Lerp(bgCol, maxCol, val / high);
            return col;
        }
    }

    public struct Tensor2x2
    {

        public float Sx;
        public float Sy;
        public float Txy;

        public Tensor2x2(float sx, float sy, float txy)
        {
            Sx = sx;
            Sy = sy;
            Txy = txy;
        }


    }

    //4x float
    [Serializable]
    public struct StressStats
    {
        public float minShear, maxShear,
                     minPressure, maxPressure;

    }
    //5x float
    [Serializable]
    public struct RelaxStats
    {
        public float minVal, maxVal,
                     minResidue, maxResidue;

        public float residueNorm;
    }
    //9x float
    [Serializable]
    public struct GridStats
    {
        public RelaxStats relax;
        public StressStats stress;

        public static NativeArray<float> getStressArray(NativeArray<float> from)
        {
            return from.GetSubArray(0, 4);
        }
        public static NativeArray<float> getRelaxArray(NativeArray<float> from)
        {
            return from.GetSubArray(4, 5);
        }

    }
    public struct DisplacementStats
    {
        public float minX, maxX;
        public float minY, maxY;
        public float minStress, maxStress;
        public float minShear, maxShear;
        public float minPressure, maxPressure;
    }

    
    [Serializable]
    public struct StressGrid {


        [SerializeField]
        public NativeArray<bool> mask; // w * h

        [SerializeField]
        public NativeArray<float> stress; //(w*2+1) * (h*2+1)  phi(x,y)

        public int width;
        public int height;

        static int tileToCellCount(int t)
        {
            return t * 2 + 1;
        }

        public int sx {
            get {

                return tileToCellCount(width);
            }
        }
        public int sy
        {
            get
            {
                return tileToCellCount(height);
            }
        }

        public int size
        {
            get
            {
                return sx * sy;
            }
        }

        public bool IsCreated {
            get { return stress.IsCreated && mask.IsCreated;
            }
        }
        
        public StressGrid(int w, int h, NativeArray<bool>? m = null, bool temp = false)
        {
            width = w;
            height = h;
            int len = width * height;

            Allocator alloc = Allocator.Persistent;

            if (temp) alloc = Allocator.TempJob;

            if (m == null)
                mask = new NativeArray<bool>(len, alloc);
            else mask = (NativeArray<bool>)m;
            
            stress = new NativeArray<float>(tileToCellCount(width)*tileToCellCount(height), alloc);
            
        }

        public void cleanup()
        {
            stress.Dispose();
            mask.Dispose();
        }

        public float this[int x, int y]
        {
            get
            {
                if (x < 0 || y < 0 || x > sx - 1 || y > sy - 1)
                    return 0.0f;

                return getGrid(x, y);
            }
            set
            {
                setGrid(x, y, value);
            }
        }
        public float this[int i]
        {
            get
            {
                if (i<0 || i>sx*sy)
                    throw new IndexOutOfRangeException();

                return stress[i];
            }
            set
            {
                if (i < 0 || i > sx * sy)
                    throw new IndexOutOfRangeException();
                stress[i] = value;
            }
        }

        int index(int x, int y)
        {
            return x + y * sx;
        }

        int maskIndex(int x, int y)
        {
            return x / 2 + (y / 2) * width;
        }

        public bool getMask(int x, int y) //node resolution
        {
            if (x <= 0 || y <= 0 || x >= sx - 1 || y >= sy - 1)
                return false;
            return mask[maskIndex(x, y)];
        }
        public bool getMask(int i)
        {
            if (i < 0 || i > width*height)
                throw new IndexOutOfRangeException();
            return mask[i];
        }
        public void setMask(int x, int y, bool val) //tile resolution
        {
            if (x < 0 || y < 0 || x > width - 1 || y > height - 1)
                throw new IndexOutOfRangeException();
            mask[x + y * width] = val;
        }
        public void setMask(int i, bool val)
        {
            if (i < 0 ||  i > width * height )
                throw new IndexOutOfRangeException();
            mask[i] = val;
        }

        public float getGrid(int x, int y)
        {
            if (x < 0 || y < 0 || x > sx - 1 || y > sy - 1)
                throw new IndexOutOfRangeException();
            return stress[index(x, y)];
        }
        public void setGrid(int x, int y, float val)
        {
            if (x < 0 || y < 0 || x > sx - 1 || y > sy - 1)
                throw new IndexOutOfRangeException();
            stress[index(x, y)] = val;
        }

        public bool inBounds( int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= sx - 1 || y >= sy - 1)
                return false;

            bool xbetween = x % 2 == 0;
            bool ybetween = y % 2 == 0;

            if(xbetween)
            {
                if(ybetween)
                {
                    return getMask(x - 1, y - 1) && getMask(x + 1, y - 1)
                    && getMask(x - 1, y + 1) && getMask(x + 1, y + 1);
                } else
                {
                    return getMask(x - 1, y) && getMask(x + 1, y);
                }
            }
            else
            {
                if (ybetween)
                {
                    return getMask(x, y - 1) && getMask(x, y + 1);
                }
                else
                {
                    return getMask(x, y);
                }
            }


        }

        float bound(int x, int y, int px, int py)
        {
            //clamp
            if (x < 0) x = 0;
            if (x > sx - 1) x = sx - 1;
            if (y < 0) y = 0;
            if (y > sy - 1) y = sy - 1;

            int dx = x - px;
            int dy = y - py;


            if (!inBounds(x,y) )
            {
                if (Math.Abs(dx) == 2) return getGrid(px + dx / 2, y);
                else if (Math.Abs(dy) == 2) return getGrid(x, py + dy / 2);

            }


            return getGrid(x,y);
        }
        float bound(int x, int y)
        {
            return bound(x, y, x, y);
        }

        public float stencil_13(int x, int y) 
        {
            //1 -4 6 -4 1
            float fx4 = bound(x - 2, y, x, y) - 4 * bound(x - 1, y) + 6 * bound(x, y) - 4 * bound(x + 1, y) + bound(x + 2, y, x, y);
            float fy4 =  bound(x, y - 2, x, y) - 4 * bound(x, y - 1) + 6 * bound(x, y) - 4 * bound(x, y + 1) + bound(x, y + 2, x, y);

            //      1
            //     -4
            //1 -4 12 -4 1
            //     -4 
            //      1


            //   1 -2  1
            //  -2  4 -2
            //   1 -2  1
            float fx2y2 = bound(x - 1, y + 1) - 2 * bound(x, y + 1) + bound(x + 1, y + 1)
                      - 2 * bound(x - 1, y) + 4 * bound(x, y) - 2 * bound(x + 1, y)
                          + bound(x - 1, y - 1) - 2 * bound(x, y - 1) + bound(x + 1, y - 1);


            //      1
            //   2 -8  2
            //1 -8 20 -8 1
            //   2 -8  2
            //      1

            return fx4 + 2 * fx2y2 + fy4;
        }


        public Tensor2x2 tensor(int x,int y)
        {
            var fx2 = -2 * bound(x,y) + bound( x - 1 ,y) + bound( x + 1,y);

            var fy2 = -2 * bound(x,y) + bound( x , y - 1) + bound( x, y + 1);

            var fxy = 1 / 4 * (bound( x + 1, y + 1) - bound( x - 1, y + 1) - bound( x + 1, y - 1) + bound( x - 1, y - 1));

            return new Tensor2x2(fy2, fx2, -fxy) ;
        }

        public float shear(int x, int y)
        {
            var t = tensor(x, y);

            return Mathf.Sqrt(  ((t.Sx-t.Sy)/2) * ((t.Sx - t.Sy) / 2) + t.Txy * t.Txy );

        }

        public float pressure(int x, int y)
        {
            var t = tensor(x, y);
            return -0.5f * (t.Sx + t.Sy);
        }

    }


    public struct NFA2D
    {
        [SerializeField]
        public NativeArray<float> grid;


        public int width;
        public int height;

        public NFA2D(int w, int h, Allocator alloc)
        {
            width = w;
            height = h;
            grid = new NativeArray<float>(tileToCellCount(width) * tileToCellCount(height), alloc);
        }


        public void Dispose()
        {
            grid.Dispose();
        }
        static int tileToCellCount(int t)
        {
            return t * 2 + 1;
        }



        public int index(int x, int y)
        {
            return x + y * sx;
        }
        public int sx
        {
            get
            {
                return tileToCellCount(width);
            }
        }
        public int sy
        {
            get
            {
                return tileToCellCount(height);
            }
        }

        public int size
        {
            get
            {
                return sx * sy;
            }
        }

        public bool inBounds(int x, int y)
        {
            return x >= 0 && x < sx
            && y >= 0 && y < sy;
        }
        public float this[int x, int y]
        {
            get
            {
                return grid[index(x, y)];
            }
            set
            {
                grid[index(x, y)] = value;
            }
        }

        public float this[int i]
        {
            get
            {
                return grid[i];
            }
            set
            {
                grid[i] = value;
            }
        }
}

    [Serializable]
    public struct CheckedNativeFA
    {

        [SerializeField]
        public NativeArray<float> grid;


        public int width;
        public int height;

        public CheckedNativeFA(int w, int h, Allocator alloc)
        {
            width = w;
            height = h;
            grid = new NativeArray<float>(tileToCellCount(width) * tileToCellCount(height), alloc);
        }

        public void Dispose()
        {
            grid.Dispose();
        }

        static int tileToCellCount(int t)
        {
            return t * 2 + 1;
        }



        public int index(int x, int y)
        {
            return x + y * sx;
        }
        public int sx
        {
            get
            {

                return tileToCellCount(width);
            }
        }
        public int sy
        {
            get
            {
                return tileToCellCount(height);
            }
        }

        public int size
        {
            get
            {
                return sx * sy;
            }
        }

        public bool inBounds(int x, int y)
        {
            return x >= 0 && x < sx
            && y >= 0 && y < sy;
        }
        public float this[int x, int y]
        {
            get
            {
                if (inBounds(x, y))
                    return grid[index(x, y)];
                else return 0.0f;
            }
            set
            {
                if (inBounds(x, y))
                   grid[index(x, y)] = value;
            }
        }

        public float this[int i]
        {
            get
            {
                if (i >= 0 && i < grid.Length)
                    return grid[i];
                else return 0.0f;
            }
            set
            {
                if (i>=0 && i<grid.Length)
                    grid[i] = value;
            }
        }
    }

    [Serializable]
    public struct DisplacementGrid
    {

        [SerializeField]
        public NativeArray<bool> mask; // w * h

        public NFA2D uX;
        public NFA2D uY;

        public NFA2D uX_prev;
        public NFA2D uY_prev;

        public float density;                       //1500 - 3000 kg/m^3
        public float lame1; //lambda                //80 GPa
        public float lame2; //µ                     //60-100 GPa

        public int width;
        public int height;
        public DisplacementGrid(int w, int h, float rho, float lambda, float mu,NativeArray<bool>? m = null)
        {
            width = w;
            height = h;
            int len = width * height;

            Allocator alloc = Allocator.Persistent;

            density = rho;
            lame1 = lambda;
            lame2 = mu;


            if (m == null)
                mask = new NativeArray<bool>(len, alloc);
            else mask = (NativeArray<bool>)m;

            uX = new NFA2D(width, height, alloc);
            uY = new NFA2D(width, height, alloc);
            uX_prev = new NFA2D(width, height, alloc);
            uY_prev = new NFA2D(width, height, alloc);
        }

        public void Dispose()
        {
            mask.Dispose();
            uX.Dispose();
            uY.Dispose();
            uX_prev.Dispose();
            uY_prev.Dispose();
        }
        static int tileToCellCount(int t)
        {
            return t * 2 + 1;
        }


        public int sx
        {
            get
            {
                return uX.sx;
            }
        }
        public int sy
        {
            get
            {
                return uX.sy;
            }
        }

        public int size
        {
            get
            {
                return sx * sy;
            }
        }
        public bool inBounds(int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= sx - 1 || y >= sy - 1)
                return false;

            bool xbetween = x % 2 == 0;
            bool ybetween = y % 2 == 0;

            if (xbetween)
            {
                if (ybetween)
                {
                    return getMask(x - 1, y - 1) && getMask(x + 1, y - 1)
                    && getMask(x - 1, y + 1) && getMask(x + 1, y + 1);
                }
                else
                {
                    return getMask(x - 1, y) && getMask(x + 1, y);
                }
            }
            else
            {
                if (ybetween)
                {
                    return getMask(x, y - 1) && getMask(x, y + 1);
                }
                else
                {
                    return getMask(x, y);
                }
            }


        }

        public bool IsCreated
        {
            get
            {
                return mask.IsCreated && uX.grid.IsCreated && uY.grid.IsCreated && uX_prev.grid.IsCreated && uY_prev.grid.IsCreated;
            }
        }
        public int maskIndex(int x, int y)
        {
            return x / 2 + (y / 2) * width;
        }

        public bool getMask(int x, int y)
        {
            return mask[maskIndex(x, y)];
        }



        public float velX(int x, int y, float dt = 1)
        {
            return(uX[x,y] - uX_prev[x,y] ) / dt;
        }
        public float velY( int x, int y, float dt = 1)
        {
            return (uY[x,y] - uY_prev[x,y] ) / dt;
        }



        public int windX(int x, int y)
        {
            float diff = (lame1 + lame2) / 0.001f;
            float conv = density * density * velX(x, y);

            float peclet = conv / diff;
            if (peclet > 2.0f) return 1;
            if (peclet < -2.0f) return -1;

            return 0;
        }
        public int windY(int x, int y)
        {
            float diff = (lame1 + lame2) / 0.001f;
            float conv = density * density * velY(x, y);

            float peclet = conv / diff;
            if (peclet > 2.0f) return 1;
            if (peclet < -2.0f) return -1;
            return 0;
        }

        public float masked_get(ref NFA2D u, int x, int offX, int y, int offY)
        {
            if (inBounds(x+offX, y+offY)) return u[x+offX, y+offY];
            else
            {
                int gx = x - 2 * offX;
                int gy = y - 2 * offY;
                if (inBounds(gx, y)) return -u[gx, y];
                else if (inBounds(x, gy)) return -u[x, gy];
                else if (inBounds(gx,gy))  return -u[gx,gy ];
            }

            return 0.0f;
        }


        public Vector2 maskNormal(int x, int y, int r = 3)
        {

            float sumX = 0;
            float sumY = 0;

            for (int iy = -r; iy <= r; iy++)
                for (int ix = -r; ix <= r; ix++)
                {
                    if (inBounds(x + ix, y + iy))
                    {
                    if (ix != 0)
                        sumX += 1/ix;
                    if(iy != 0)
                        sumY += 1/iy;
                    }
                }
            return new Vector2(sumX, sumY).normalized;
        }

        public float dx2(ref NFA2D u, int x, int y, float h = 1.0f)
        {
            return 1f / h / h * ( masked_get(ref u, x, - 1, y, 0) - 2 * u[x, y] + masked_get(ref u, x, + 1, y, 0) );
        }
        public float dy2(ref NFA2D u, int x, int y, float h = 1.0f)
        {
            return 1f / h / h * ( masked_get(ref u, x, 0, y, - 1) - 2 * u[x, y] + masked_get(ref u, x, 0, y, + 1) );
        }

        public float dx(ref NFA2D u, int x, int y, float h = 1.0f)
        {
            int wx = windX(x, y);
            if (wx == 0)
                return 1f / h / 2f * (masked_get(ref u, x, + 1, y,0) - masked_get(ref u, x, - 1, y,0) );
            else
                return 1f / h / 2f * (masked_get(ref u, x, + wx, y, 0) - u[x, y]);
        }
        public float dy(ref NFA2D u, int x, int y, float h = 1.0f)
        {
            int wy = windY(x, y);
            if (wy == 0)
                return 1f / h / 2f * (masked_get(ref u, x,0, y, + 1) - masked_get(ref u, x,0, y, - 1) );
            else
                return 1f / h / 2f * (masked_get(ref u, x,0, y, + wy) - u[x, y]);
        }
        public float dxy(ref NFA2D u, int x, int y, float h = 1.0f)
        {
            int wx = windX(x, y);
            int wy = windY(x, y);
            float val = u[x, y];
            if(wx == 0 && wy == 0)
                return 1f / h / h / 4f * (masked_get(ref u, x, + 1, y, + 1) - masked_get(ref u, x, + 1, y, - 1) - masked_get(ref u, x, - 1, y, + 1) + masked_get(ref u, x, - 1, y, - 1) );
            else if(wx != 0 && wy==0)
                return 1f / h / h / 4f * (masked_get(ref u, x, + wx, y, + 1) - masked_get(ref u, x, + wx, y, - 1) - masked_get(ref u, x, - 0, y, + 1) + masked_get(ref u, x, - 0, y, - 1) );
            else if (wx == 0 && wy != 0)
                return 1f / h / h / 4f * (masked_get(ref u, x, + 1, y, + wy) - masked_get(ref u, x, + 1, y, - 0) - masked_get(ref u, x, - 1, y, + wy) + masked_get(ref u, x, - 1, y, - 0) );
            else
                return 1f / h / h / 4f * (masked_get(ref u, x, + wx, y, + wy) - masked_get(ref u, x, + wx, y, - 0) - masked_get(ref u, x, - 0, y, + wy) + masked_get(ref u, x, - 0, y, - 0) );
        }
        

        public Tensor2x2 strain(int x, int y,float h = 1.0f)
        {
            int wx = windX(x, y);
            int wy = windY(x, y);
            return new Tensor2x2(               dx(ref uX, x,y, h),                      dy(ref uY, x, y, h),
                                 0.5f * (dx(ref uY, x, y, h)) + dy(ref uX,x,y,h) );
        }




        Tensor2x2 stress( int x, int y, float h = 1.0f)
        {

            var e = strain(x, y, h);

            float trace = e.Sx + e.Sy;

            return new Tensor2x2(2 * lame2 * e.Sx + lame1 * trace, 2 * lame1 * e.Sy + lame1 * trace,
                                 2 * lame2 * e.Txy + lame1 * trace);
        }

        public Vector2 principalStress(int x, int y, float h = 1.0f)
        {
            var t = stress(x, y, h);

            float norm = (t.Sx + t.Sy) * 0.5f;

            return new Vector2(norm - shear(x, y, h), norm + shear(x, y, h));
        }

        public float principalAngle(int x, int y, float h = 1.0f)
        {
            var t = stress(x, y, h);

            return 0.5f * Mathf.Atan(0.5f * (t.Txy / (t.Sx - t.Sy)) );

        }
       
        public float shear(int x, int y, float h = 1.0f)
        {
            var t = stress(x, y, h);

            return Mathf.Sqrt(((t.Sx - t.Sy) / 2) * ((t.Sx - t.Sy) / 2) + t.Txy * t.Txy);

        }

        public float pressure(int x, int y, float h = 1.0f)
        {
            var t = stress( x, y, h);
            return (dx(ref uX,x,y)+dy(ref uY,x,y) )*10000000000f-0.5f * (t.Sx + t.Sy);
        }

    }

    public interface IResidueNorm
    {
        void Clear();
        void Record(float r);
        float Value { get; }

    }

    public struct L1Norm : IResidueNorm
    {
        float sum;

        public void Clear()
        {
            sum = 0f;
        }

        public void Record(float r)
        {
            sum += Mathf.Abs(r);
        }


        public float Value => sum;

    }
    public struct L2Norm : IResidueNorm
    {
        float sum;


        public void Clear()
        {
            sum = 0f;
        }

        public void Record(float r)
        {
            sum += r * r;
        }

        public float Value => Mathf.Sqrt(sum);

    }
}