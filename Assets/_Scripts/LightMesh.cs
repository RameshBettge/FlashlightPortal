using UnityEngine;

public struct LightMesh
{
    public Vector3 middlePos;

    public Vector3[] vertices;
    public int[] tris;


    public Vector3 maxPos;
    public Vector3 minPos;

    public Mesh GenerateMesh()
    {
        int triIndex = 0;
        tris = new int[(vertices.Length - 1) * 3];
        for (int i = 0; i < vertices.Length - 2; i++, triIndex += 3)
        {
            tris[triIndex] = i + 2;
            tris[triIndex + 1] = 0;
            tris[triIndex + 2] = i + 1;
        }
        //create the last triangle
        tris[triIndex] = 1;
        tris[triIndex + 1] = 0;
        tris[triIndex + 2] = vertices.Length - 1;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;

        return mesh;
    }

    public void SetMinMax()
    {
        maxPos = Vector3.zero;
        minPos = Vector3.zero;

        foreach (Vector3 v in vertices)
        {
            if(v.x > maxPos.x)
            {
                maxPos.x = v.x;
            }
            else if (v.x < minPos.x)
            {
                minPos.x = v.x;
            }

            if (v.y > maxPos.y)
            {
                maxPos.y = v.y;
            }
            else if (v.y < minPos.y)
            {
                minPos.y = v.y;
            }

            if (v.z > maxPos.z)
            {
                maxPos.z = v.z;
            }
            else if (v.z < minPos.z)
            {
                minPos.z = v.z;
            }
        }
    }
}
