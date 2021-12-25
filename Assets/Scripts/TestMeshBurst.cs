using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class TestMeshBurst : MonoBehaviour
{
    private Mesh mesh;
    int edge = 50;
    int size;
    float cellSize = 2;
    Vector3 pivot = Vector3.zero;

    NativeArray<Vector3> nvertices;
    NativeArray<Vector2> nuvs;
    NativeArray<int> ntriangles;
    NativeArray<Vector3> nPos;
    NativeArray<Vector3> nQuadSize;
    NativeArray<Vector2> nGridUV00;
    NativeArray<Vector2> nGridUV11;
    NativeArray<Quaternion> cachedQuaternionEulerNativeArray;

    Vector3[] vertices_mod;
    Vector2[] uvs_mod;
    int[] triangles_mod;

    JobHandle m_JobHandle;
    MeshUtilsBurst.NiceMeshJob niceMeshJob;
    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        mesh.MarkDynamic();

        size = edge * edge;

        GetComponent<MeshFilter>().mesh = mesh;
        MeshUtilsBurst.CreateEmptyMeshNativeArrays(size, out nvertices, out nuvs, out ntriangles, out cachedQuaternionEulerNativeArray);
        MeshUtilsBurst.CreateEmptyMeshArrays(size, out vertices_mod, out uvs_mod, out triangles_mod);

        nPos = new NativeArray<Vector3>(size, Allocator.Persistent);
        nQuadSize = new NativeArray<Vector3>(size, Allocator.Persistent);
        nGridUV00 = new NativeArray<Vector2>(size, Allocator.Persistent);
        nGridUV11 = new NativeArray<Vector2>(size, Allocator.Persistent);
    }
    private void OnDestroy()
    {
        nvertices.Dispose();
        nuvs.Dispose();
        ntriangles.Dispose();
        cachedQuaternionEulerNativeArray.Dispose();
        nPos.Dispose();
        nQuadSize.Dispose();
        nGridUV00.Dispose();
        nGridUV11.Dispose();
    }
    // Update is called once per frame
    void Update()
    {
        for (int x = 0; x < edge; x++)
        {
            for (int y = 0; y < edge; y++)
            {
                int index = x * edge + y;
                Vector3 quadSize = new Vector3(1, 1) * cellSize;
                Vector2 gridUV00 = Vector2.zero, gridUV11 = Vector2.zero;

                Vector3 meshQuadSize = new Vector3(quadSize.x, 0, quadSize.y);
                Vector3 pos = GetWorldPosition(x, y) + meshQuadSize * .5f;
                nPos[index] = pos;
                nQuadSize[index] = meshQuadSize;
                nGridUV00[index] = gridUV00;
                nGridUV11[index] = gridUV11;

            }
        }


        niceMeshJob = MeshUtilsBurst.Create(nvertices,  nuvs,  ntriangles, nPos, 0, nQuadSize, nGridUV00, nGridUV11, cachedQuaternionEulerNativeArray);
        m_JobHandle = niceMeshJob.Schedule(size, 100);

    }

    private void LateUpdate()
    {
        m_JobHandle.Complete();

        MeshUtilsBurst.CopyToModifiedArray(in niceMeshJob, vertices_mod, uvs_mod, triangles_mod);
        MeshUtilsBurst.ApplyToMesh(mesh, vertices_mod, uvs_mod, triangles_mod);
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + pivot;
    }
}
