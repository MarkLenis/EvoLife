using UnityEngine;

namespace EvoLife.Presentation
{
    /// <summary>
    /// Runtime irregular discs for biome ground. Logical <c>BiomeZone</c> circles stay unchanged.
    /// </summary>
    public static class PresentationGroundMesh
    {
        public static Mesh CreateIrregularDisc(
            float radius,
            int segments,
            float irregularity,
            int seed,
            float heightNoise = 0f)
        {
            radius = Mathf.Max(0.25f, radius);
            segments = Mathf.Clamp(segments, 8, 64);
            irregularity = Mathf.Clamp(irregularity, 0f, 0.65f);
            heightNoise = Mathf.Max(0f, heightNoise);

            var vertexCount = segments + 1;
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var triangles = new int[segments * 3];

            var phaseA = (seed * 0.37f) % (Mathf.PI * 2f);
            var phaseB = (seed * 0.61f) % (Mathf.PI * 2f);
            var phaseC = (seed * 0.83f) % (Mathf.PI * 2f);
            var noiseOffset = seed * 0.17f;

            vertices[0] = new Vector3(0f, SampleHeight(0f, 0f, heightNoise, noiseOffset) * 0.35f, 0f);
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (var i = 0; i < segments; i++)
            {
                var t = i / (float)segments * Mathf.PI * 2f;
                var wobble =
                    Mathf.Sin(t * 2f + phaseA) * 0.45f +
                    Mathf.Sin(t * 3f + phaseB) * 0.32f +
                    Mathf.Sin(t * 5f + phaseC) * 0.18f +
                    Mathf.Sin(t * 8f + phaseA * 1.7f) * 0.08f;
                var r = radius * (1f + wobble * irregularity);
                var x = Mathf.Cos(t) * r;
                var z = Mathf.Sin(t) * r;
                vertices[i + 1] = new Vector3(x, SampleHeight(x, z, heightNoise, noiseOffset), z);
                uvs[i + 1] = new Vector2(x / (radius * 2f) + 0.5f, z / (radius * 2f) + 0.5f);

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2 <= segments ? i + 2 : 1;
            }

            var mesh = new Mesh
            {
                name = "EvoLifeIrregularDisc",
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static float RimRadiusVariance(Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount < 4)
            {
                return 0f;
            }

            var vertices = mesh.vertices;
            var min = float.MaxValue;
            var max = 0f;
            for (var i = 1; i < vertices.Length; i++)
            {
                var radius = new Vector2(vertices[i].x, vertices[i].z).magnitude;
                min = Mathf.Min(min, radius);
                max = Mathf.Max(max, radius);
            }

            return max - min;
        }

        static float SampleHeight(float x, float z, float heightNoise, float offset)
        {
            if (heightNoise <= 0.0001f)
            {
                return 0f;
            }

            var n = Mathf.PerlinNoise(x * 0.045f + offset, z * 0.045f + offset * 1.3f);
            return (n - 0.35f) * heightNoise;
        }
    }
}
