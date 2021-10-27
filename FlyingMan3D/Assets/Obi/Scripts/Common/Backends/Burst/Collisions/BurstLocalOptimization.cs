#if (OBI_BURST && OBI_MATHEMATICS && OBI_COLLECTIONS)
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using System.Runtime.CompilerServices;

namespace Obi
{ 
    public static class BurstLocalOptimization
    {

        /**
         * point in the surface of a signed distance field.
         */
        public struct SurfacePoint
        {
            public float4 bary;
            public float4 point;
            public float4 normal;
        }

        public interface IDistanceFunction
        {
            void Evaluate(float4 point, ref SurfacePoint projectedPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetCartesianConvexAttrib(int simplexStart,
                                                     int simplexSize,
                                                     NativeArray<int> simplices,
                                                     NativeArray<float4> positions,
                                                     NativeArray<float4> radii,
                                                     float4 convexBary,
                                                     out float4 convexPoint, out float convexThickness)
        {
            convexPoint = float4.zero; convexThickness = 0;
            for (int j = 0; j < simplexSize; ++j)
            {
                int particle = simplices[simplexStart + j];
                convexPoint += positions[particle] * convexBary[j];
                convexThickness += radii[particle].x * convexBary[j];
            }
        }

        public static SurfacePoint Optimize<T>(ref T function,
                                               NativeArray<float4> positions,
                                               NativeArray<float4> radii,
                                               NativeArray<int> simplices,
                                               int simplexStart,
                                               int simplexSize,
                                               ref float4 convexBary,
                                               out float4 convexPoint,
                                               int maxIterations = 16,
                                               float tolerance = 0.004f) where T : struct, IDistanceFunction
        {
            var pointInFunction = new SurfacePoint();

            // get cartesian coordinates of the initial guess:
            GetCartesianConvexAttrib(simplexStart, simplexSize, simplices, positions, radii, convexBary, out convexPoint, out float convexThickness);

            // for a 0-simplex (point), perform a single evaluation:
            if (simplexSize == 1 || maxIterations < 1)
                function.Evaluate(convexPoint, ref pointInFunction);

            // for a 1-simplex (edge), perform golden ratio search:
            else if (simplexSize == 2)
                GoldenSearch(ref function, positions, radii, simplices, simplexStart, ref convexBary, ref pointInFunction, maxIterations, tolerance * 10);

            // for higher-order simplices, use general Frank-Wolfe convex optimization:
            else
                FrankWolfe(ref function, simplexStart, simplexSize, positions, radii, simplices, ref convexPoint, ref convexThickness, ref convexBary, ref pointInFunction, maxIterations, tolerance);
            
            return pointInFunction;
        }

        // Frank-Wolfe convex optimization algorithm. Returns closest point to a simplex in a signed distance function.
        private static void FrankWolfe<T>(ref T function,
                                          int simplexStart,
                                          int simplexSize,
                                          NativeArray<float4> positions,
                                          NativeArray<float4> radii,
                                          NativeArray<int> simplices,
                                          ref float4 convexPoint,
                                          ref float convexThickness,
                                          ref float4 convexBary,
                                          ref SurfacePoint pointInFunction,
                                          int maxIterations,
                                          float tolerance) where T : struct, IDistanceFunction
        {
            for (int i = 0; i < maxIterations; ++i)
            {
                // sample target function:
                function.Evaluate(convexPoint, ref pointInFunction);

                // find descent direction:
                int descent = 0;
                float gap = float.MinValue;
                for (int j = 0; j < simplexSize; ++j)
                {
                    int particle = simplices[simplexStart + j];
                    float4 candidate = positions[particle] - convexPoint;

                    // here, we adjust the candidate by projecting it to the engrosed simplex's surface:
                    candidate -= pointInFunction.normal * (radii[particle].x - convexThickness);

                    float corr = math.dot(-pointInFunction.normal, candidate);
                    if (corr > gap)
                    {
                        descent = j;
                        gap = corr;
                    }
                }

                // if the duality gap is below tolerance threshold, stop iterating.
                if (gap < tolerance)
                    break;

                // update the barycentric coords using 2/(i+2) as  the step factor
                float step = 0.3f * 2.0f / (i + 2);
                convexBary *= 1 - step;
                convexBary[descent] += step;

                // get cartesian coordinates of current solution:
                GetCartesianConvexAttrib(simplexStart, simplexSize, simplices, positions, radii, convexBary, out convexPoint, out convexThickness);
            }   
        }

        private static void GoldenSearch<T>(ref T function,
                                         NativeArray<float4> positions,
                                         NativeArray<float4> radii,
                                         NativeArray<int> simplices,
                                         int simplexStartA,
                                         ref float4 convexBary,
                                         ref SurfacePoint pointInFunction,
                                         int maxIterations,
                                         float tolerance) where T : struct, IDistanceFunction
        {
            var pointInFunctionD = new SurfacePoint();

            float4 a = positions[simplices[simplexStartA]];
            float4 b = positions[simplices[simplexStartA + 1]];

            float gr = (math.sqrt(5.0f) + 1) / 2.0f;
            float u = 0; float v = 1;

            float mid = (v + u) * 0.5f;
            float c = v - (v - u) / gr;
            float d = u + (v - u) / gr;
            float4 convexPointC = a * c + b * (1 - c);
            float4 convexPointD = a * d + b * (1 - d);

            function.Evaluate(convexPointC, ref pointInFunction);
            function.Evaluate(convexPointD, ref pointInFunctionD);

            for (int i = 0; i < maxIterations; ++i)
            {
                // if the gap is below tolerance threshold, stop iterating.
                if (math.abs(v - u) < tolerance * (math.abs(c) + math.abs(d)))
                    break;

                float distanceC = math.distance(convexPointC, pointInFunction.point);
                float distanceD = math.distance(convexPointD, pointInFunctionD.point);

                if (distanceC < distanceD)
                    v = d;
                else
                    u = c;

                mid = (v + u) * 0.5f;
                c = v - (v - u) / gr;
                d = u + (v - u) / gr;
                convexPointC = a * c + b * (1 - c);
                convexPointD = a * d + b * (1 - d);

                function.Evaluate(convexPointC, ref pointInFunction);
                function.Evaluate(convexPointD, ref pointInFunctionD);
            }

            convexBary.x = mid;
            convexBary.y = (1 - mid);
            function.Evaluate(convexBary.x * a + convexBary.y * b, ref pointInFunction);
        }


    }

}
#endif