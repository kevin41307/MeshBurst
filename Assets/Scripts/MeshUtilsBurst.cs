using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public static class MeshUtilsBurst
{

    public static void CreateEmptyMeshNativeArrays(int quadCount, out NativeArray<Vector3> vertices, out NativeArray<Vector2> uvs, out NativeArray<int> triangles, out NativeArray<Quaternion> cachedQuaternionEulerNativeArray)
    {
        vertices = new NativeArray<Vector3>(4 * quadCount, Allocator.Persistent);
        uvs = new NativeArray<Vector2>(4 * quadCount, Allocator.Persistent);
        triangles = new NativeArray<int>(6 * quadCount, Allocator.Persistent);
        cachedQuaternionEulerNativeArray = new NativeArray<Quaternion>(360, Allocator.Persistent);
    }
    public static void CreateEmptyMeshArrays(int quadCount, out Vector3[] vertices_mod, out Vector2[] uvs_mod, out int[] triangles_mod)
    {
        vertices_mod = new Vector3[4 * quadCount];
        uvs_mod = new Vector2[4 * quadCount];
        triangles_mod = new int[6 * quadCount];
    }
    public static NiceMeshJob Create(NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<int> triangles, NativeArray<Vector3> pos, float rot, NativeArray<Vector3> quadSize, NativeArray<Vector2> gridUV00, NativeArray<Vector2> gridUV11, NativeArray<Quaternion> cachedQuaternionEulerNativeArray)
    {
        NiceMeshJob niceMeshJob = new NiceMeshJob
        {
            vertices = vertices,
            uvs = uvs,
            triangles = triangles,
            pos = pos,
            rot = rot,
            baseSize = quadSize,
            uv00 = gridUV00,
            uv11 = gridUV11,
            cachedQuaternionEulerNativeArray = cachedQuaternionEulerNativeArray
        };
        return niceMeshJob;
    }
    public static void CopyToModifiedArray(in NiceMeshJob niceMeshJob, Vector3[] vertices_mod, Vector2[] uvs_mod, int[] triangles_mod)
    {
        niceMeshJob.vertices.CopyTo(vertices_mod);
        niceMeshJob.uvs.CopyTo(uvs_mod);
        niceMeshJob.triangles.CopyTo(triangles_mod);
    }

    public static void ApplyToMesh(Mesh mesh, Vector3[] vertices_mod, Vector2[] uvs_mod, int[] triangles_mod)
    {
        mesh.vertices = vertices_mod;
        mesh.uv = uvs_mod;
        mesh.triangles = triangles_mod;
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct NiceMeshJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> vertices;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uvs;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> triangles;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> pos;
        public float rot;
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> baseSize;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uv00;
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> uv11;


        [NativeDisableParallelForRestriction]
        public NativeArray<Quaternion> cachedQuaternionEulerNativeArray;
        private bool cached;
        public NiceMeshJob(NativeArray<Vector3> vertices, NativeArray<Vector2> uvs, NativeArray<int> triangles, NativeArray<Vector3> nPos, float rot, NativeArray<Vector3> nQuadSize, NativeArray<Vector2> nGridUV00, NativeArray<Vector2> nGridUV11, NativeArray<Quaternion> cachedQuaternionEulerNativeArray)
        {
            this.vertices = vertices;
            this.uvs = uvs;
            this.triangles = triangles;
            this.pos = nPos;
            this.rot = rot;
            this.baseSize = nQuadSize;
            this.uv00 = nGridUV00;
            this.uv11 = nGridUV11;
            this.cachedQuaternionEulerNativeArray = cachedQuaternionEulerNativeArray;
            cached = false;
        }


        public void Execute(int i)
        {
            int vIndex = i * 4;
            int vIndex0 = vIndex;
            int vIndex1 = vIndex + 1;
            int vIndex2 = vIndex + 2;
            int vIndex3 = vIndex + 3;
            baseSize[i] *= .5f;

            bool skewed = baseSize[i].x != baseSize[i].y;
            skewed = false;
            if (skewed)
            {
                /*
			    vertices[vIndex0] = pos + GetQuaternionEulerXZ(rot) * new Vector3(-baseSize.x, baseSize.y);
			    vertices[vIndex1] = pos + GetQuaternionEulerXZ(rot) * new Vector3(-baseSize.x, -baseSize.y);
			    vertices[vIndex2] = pos + GetQuaternionEulerXZ(rot) * new Vector3(baseSize.x, -baseSize.y);
			    vertices[vIndex3] = pos + GetQuaternionEulerXZ(rot) * baseSize;
                */
            }
            else
            {
                vertices[vIndex0] = pos[i] + GetQuaternionEulerXZNativeArray(rot - 270) * baseSize[i];
                vertices[vIndex1] = pos[i] + GetQuaternionEulerXZNativeArray(rot - 180) * baseSize[i];
                vertices[vIndex2] = pos[i] + GetQuaternionEulerXZNativeArray(rot - 90) * baseSize[i];
                vertices[vIndex3] = pos[i] + GetQuaternionEulerXZNativeArray(rot - 0) * baseSize[i];
            }

            //Relocate UVs
            uvs[vIndex0] = new Vector2(uv00[i].x, uv11[i].y);
            uvs[vIndex1] = new Vector2(uv00[i].x, uv00[i].y);
            uvs[vIndex2] = new Vector2(uv11[i].x, uv00[i].y);
            uvs[vIndex3] = new Vector2(uv11[i].x, uv11[i].y);

            //Create triangles
            int tIndex = i * 6;

            triangles[tIndex + 0] = vIndex0;
            triangles[tIndex + 1] = vIndex3;
            triangles[tIndex + 2] = vIndex1;

            triangles[tIndex + 3] = vIndex1;
            triangles[tIndex + 4] = vIndex3;
            triangles[tIndex + 5] = vIndex2;
        }


        private void CacheQuaternionEulerXZNativeArray()
        {
            if (cached == true) return;
            cached = true;
            for (int i = 0; i < 360; i++)
            {
                cachedQuaternionEulerNativeArray[i] = Quaternion.Euler(0, -i, 0);
            }
        }
        public Quaternion GetQuaternionEulerXZNativeArray(float rotFloat)
        {
            int rot = Mathf.RoundToInt(rotFloat);
            rot = rot % 360;
            if (rot < 0) rot += 360;
            //if (rot >= 360) rot -= 360;
            if (cached == false) CacheQuaternionEulerXZNativeArray();
            return cachedQuaternionEulerNativeArray[rot];
        }
    }
}
