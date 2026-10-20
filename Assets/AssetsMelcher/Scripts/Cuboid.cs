namespace Melcher
{
    using UnityEngine;

    /// <summary>
    ///     A 3D analog to <see cref="Rect"/>
    /// </summary>
    // TODO: Can we replace Cuboid with Bounds? -Ryan
    [System.Serializable]
    public struct Cuboid
    {
        // =====================================================================
        // Constructors
        // =====================================================================

        public Cuboid(Vector3 size)
        {
            min = Vector3.zero;
            max = size;
        }

        public Cuboid(Vector3 position, Vector3 size)
        {
            min = position;
            max = min + size;
        }

        // =====================================================================
        // Fields
        // =====================================================================

        public Vector3 min;
        public Vector3 max;

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>
        ///     Size along the x-axis.
        /// </summary>
        public float width
        {
            get => max.x - min.x;
            set
            {
                max.x = min.x + value;
            }
        }

        /// <summary>
        ///     Size along the y-axis.
        /// </summary>
        public float height
        {
            get => max.y - min.y;
            set
            {
                max.y = min.y + value;
            }
        }

        /// <summary>
        ///     Size along the z-axis.
        /// </summary>
        public float depth
        {
            get => max.z - min.z;
            set
            {
                max.z = min.z + value;
            }
        }

        /// <summary>
        ///     The minimum extents of the cuboid.
        /// </summary>
        public Vector3 position
        {
            get => min;
            set
            {
                Vector3 sizeToPreserve = size;
                min = value;
                max = min + sizeToPreserve;
            }
        }

        public Vector3 center => Vector3.Lerp(min, max, 0.5f);

        public Vector3 size
        {
            get => max - min;
            set => max = min + value;
        }

        public float xMin
        {
            get => min.x;
            set => min.x = value;
        }
        public float xMax
        {
            get => max.x;
            set => max.x = value;
        }

        public float yMin
        {
            get => min.y;
            set => min.y = value;
        }
        public float yMax
        {
            get => max.y;
            set => max.y = value;
        }

        public float zMin
        {
            get => min.z;
            set => min.z = value;
        }
        public float zMax
        {
            get => max.z;
            set => max.z = value;
        }

        // =====================================================================
        // Methods
        // =====================================================================

        /// <summary>
        ///     Grow the cuboid to encapsulate the other cuboid.
        /// </summary>
        public Cuboid Encapsulate(Cuboid toEncapsulate)
        {
            min = Vector3.Min(min, toEncapsulate.min);
            max = Vector3.Max(max, toEncapsulate.max);
            return this;
        }

        /// <summary>
        ///     Transforms cuboid from local space to world space.
        /// </summary>
        public Cuboid Transform(Transform transform)
        {
            return Transform(transform, this);
        }

        /// <summary>
        ///     Transforms cuboid from world space to local space.
        /// </summary>
        public Cuboid InverseTransform(Transform transform)
        {
            return InverseTransform(transform, this);
        }

        public override string ToString()
        {
            return $"Cuboid: min={min.x},{min.y},{min.z} max={max.x},{max.y},{max.z} size={size.x},{size.y},{size.z}";
        }

        // =====================================================================
        // Static functions
        // =====================================================================

        // TODO: These could all be made into member functions. Doing so would
        //       make the API more consistent with Unity's Rect type which
        //       Cuboid is a 3D analog to.

        /// <summary>
        /// Map a point that is in normalized unit cube space (i.e. each
        /// channel is between 0.0 - 1.0) to its corresponding position in the
        /// cuboid's local space.
        /// </summary>
        public static Vector3 NormalizedToPoint(Cuboid cuboid, Vector3 normalized)
        {
            return new Vector3(
                Mathf.Lerp(cuboid.xMin, cuboid.xMax, normalized.x),
                Mathf.Lerp(cuboid.yMin, cuboid.yMax, normalized.y),
                Mathf.Lerp(cuboid.zMin, cuboid.zMax, normalized.z)
            );
        }

        /// <summary>
        /// Map a point that is within the cuboid's local space to its
        /// corresponding normalized unit cube space point (i.e. each channel
        /// is between 0.0 - 1.0).
        ///
        /// This function does not clamp the output value.
        /// </summary>
        public static Vector3 PointToNormalized(Cuboid cuboid, Vector3 point)
        {
            return new Vector3(
                Mathf.InverseLerp(cuboid.xMin, cuboid.xMax, point.x),
                Mathf.InverseLerp(cuboid.yMin, cuboid.yMax, point.y),
                Mathf.InverseLerp(cuboid.zMin, cuboid.zMax, point.z)
            );
        }

        /// <summary>
        /// Return <see langword="true"/> if <paramref name="a"/> overlaps
        /// <paramref name="b"/>. Otherwise, return <see langword="false"/>.
        /// </summary>
        // TODO: Rename to `Intersects` to match Unity's Bounds API.
        public static bool Overlaps(Cuboid a, Cuboid b)
        {
            return
                (a.xMin < b.xMax && a.xMax > b.xMin) &&
                (a.yMin < b.yMax && a.yMax > b.yMin) &&
                (a.zMin < b.zMax && a.zMax > b.zMin);
        }

        /// <summary>
        /// Clamp <paramref name="value"/> so it is confined within <paramref
        /// name="clampTo"/>.
        /// </summary>
        public static Cuboid Clamp(Cuboid value, Cuboid clampTo)
        {
            value.xMin = Mathf.Clamp(value.xMin, clampTo.xMin, clampTo.xMax);
            value.xMax = Mathf.Clamp(value.xMax, clampTo.xMin, clampTo.xMax);

            value.yMin = Mathf.Clamp(value.yMin, clampTo.yMin, clampTo.yMax);
            value.yMax = Mathf.Clamp(value.yMax, clampTo.yMin, clampTo.yMax);

            value.zMin = Mathf.Clamp(value.zMin, clampTo.zMin, clampTo.zMax);
            value.zMax = Mathf.Clamp(value.zMax, clampTo.zMin, clampTo.zMax);

            return value;
        }

        /// <summary>
        ///     Transforms cuboid from local space to world space.
        /// </summary>
        /// <param name="transform">
        ///     The transform the cuboid is currently relative/local to.
        /// </param>
        /// <param name="cuboid">
        ///     The cuboid to transform.
        /// </param>
        public static Cuboid Transform(Transform transform, Cuboid cuboid)
        {
            Vector3 transformedMin = transform.TransformPoint(cuboid.min);
            Vector3 transformedMax = transform.TransformPoint(cuboid.max);

            return
                new ()
                {
                    min = Vector3.Min(transformedMin, transformedMax),
                    max = Vector3.Max(transformedMin, transformedMax)
                };
        }

        /// <summary>
        ///     Transforms cuboid from world space to local space.
        /// </summary>
        /// <param name="transform">
        ///     The transform whose local space will be used as the output
        ///     space.
        /// </param>
        /// <param name="cuboid">
        ///     The cuboid to transform.
        /// </param>
        public static Cuboid InverseTransform(Transform transform, Cuboid cuboid)
        {
            Vector3 transformedMin = transform.InverseTransformPoint(cuboid.min);
            Vector3 transformedMax = transform.InverseTransformPoint(cuboid.max);

            return
                new ()
                {
                    min = Vector3.Min(transformedMin, transformedMax),
                    max = Vector3.Max(transformedMin, transformedMax)
                };
        }

        // =====================================================================
        // Static properties
        // =====================================================================

        public static Cuboid zero => new Cuboid();

        [MelcherStaticFieldEditorTreatment.ResetToTypeDefault]
        private static int s_DrawCount = 0;

        public static MelcherDeferred.Details.DeferredAction StartDrawScope()
        {
            s_DrawCount += 1;
            return MelcherDeferred.Defer(() => s_DrawCount -= 1);
        }

        /// <summary>
        ///     Render debug lines of the cuboid's edges.
        /// </summary>
        /// <param name="color">
        ///     The colour to draw the debug lines.
        /// </param>
        /// <param name="duration">
        ///     Number of seconds the lines will persist for.
        /// </param>
        /// <param name="drawX">
        ///     Whether to render an X shape in the middle of the cuboid.
        /// </param>
        /// <param name="offset">
        ///     An optional offset to shift the drawn cuboid ever so slightly
        ///     to help it avoid being hidden by another cuboid drawn in the
        ///     exact same position.
        /// </param>
        public void Draw(Color color, float duration, float dashedSegmentSize = 0.0f, bool drawX = false, int offset = 0)
        {
            Draw(color, color, duration, dashedSegmentSize, drawX, offset);
        }

        public void Draw(Color color1, Color color2, float duration, float step = 0.0f, bool drawX = false, int offset = 0)
        {
            if (s_DrawCount <= 0)
            {
                return;
            }

            Vector3 smudge = 0.001f * offset * Vector3.one;

            // Draw the outer frame of the cuboid.
            foreach (var line in s_UnitCubeLinePairs)
            {
                Vector3 start = NormalizedToPoint(this, s_UnitCubeVertices[line.vertexIndex1]) + smudge;
                Vector3 end = NormalizedToPoint(this, s_UnitCubeVertices[line.vertexIndex2]) + smudge;

                DrawMultiColorLine(start, end, color1, color2, duration, step);
            }

            if (drawX)
            {
                // Draw lines connecting opposite corners.
                foreach (var line in s_UnitCubeLinePairsX)
                {
                    Vector3 start = NormalizedToPoint(this, s_UnitCubeVertices[line.vertexIndex1]) + smudge;
                    Vector3 end = NormalizedToPoint(this, s_UnitCubeVertices[line.vertexIndex2]) + smudge;

                    DrawMultiColorLine(start, end, color1, color2, duration, step);
                }
            }

            static void DrawMultiColorLine(Vector3 start, Vector3 end, Color color1, Color color2, float duration, float step)
            {
                if (step < 0.001f)
                {
                    Debug.DrawLine(start, end, color1, duration);
                    return;
                }

                bool even = true;
                while (!MelcherCommon.IsAlmostEqual(start, end, 0.001f))
                {
                    Vector3 nextStart = Vector3.MoveTowards(start, end, step);

                    Debug.DrawLine(start, nextStart, even ? color1 : color2, duration);

                    even = !even;

                    start = nextStart;
                }
            }
        }

        /// <summary>
        ///     The vertices of a unit-cube where the min extent is (0,0,0) and
        ///     the max extent is (1,1,1).
        /// </summary>
        private static Vector3[] s_UnitCubeVertices =
        {
            new Vector3(0.0f, 0.0f, 0.0f),  // 0
            new Vector3(1.0f, 0.0f, 0.0f),  // 1
            new Vector3(1.0f, 1.0f, 0.0f),  // 2
            new Vector3(0.0f, 1.0f, 0.0f),  // 3

            new Vector3(0.0f, 0.0f, 1.0f),  // 4
            new Vector3(1.0f, 0.0f, 1.0f),  // 5
            new Vector3(1.0f, 1.0f, 1.0f),  // 6
            new Vector3(0.0f, 1.0f, 1.0f),  // 7
        };

        /// <summary>
        ///     Line segments represented as indices into <see
        ///     cref="s_UnitCubeLinePairs"/> that can be used to draw the outer
        ///     box edges of a cuboid.
        /// </summary>
        private static (int vertexIndex1, int vertexIndex2)[] s_UnitCubeLinePairs =
        {
            // Draw bottom square.
            (0 ,1),
            (1, 2),
            (2, 3),
            (3, 0),

            // Vertical lines from bottom square to top square.
            (0, 4),
            (1, 5),
            (2, 6),
            (3, 7),

            // Draw top square.
            (4, 5),
            (5, 6),
            (6, 7),
            (7, 4)
        };

        /// <summary>
        ///     Line segments represented as indices into <see
        ///     cref="s_UnitCubeVertices"/> that can be used to draw an inner
        ///     X shape inside a cuboid.
        /// </summary>
        private static (int vertexIndex1, int vertexIndex2)[] s_UnitCubeLinePairsX =
        {
            (0, 6),
            (1, 7),
            (2, 4),
            (3, 5)
        };

    }   // class Cuboid

}   // namespace Melcher

/// ===========================================================================
/// Initial author:        Ryan Scopic (ryanscopic@hotmail.com)
/// Initial creation date: 2022-04-30
/// ===========================================================================
