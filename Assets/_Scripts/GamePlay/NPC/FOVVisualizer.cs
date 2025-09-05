using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FOVVisualizer : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("パラメータの参照元となるNPCのコントローラー")]
    public NPCController targetNpc;

    [Tooltip("描画に使うEditorで作成した半透明マテリアル")]
    public Material fovMaterial;

    private Mesh viewMesh;

    void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        GetComponent<MeshFilter>().mesh = viewMesh;
        GetComponent<MeshRenderer>().material = fovMaterial;
    }

    void LateUpdate()
    {
        if (targetNpc == null) return;

        //親からパラメータを読み取る
        float radius = targetNpc.viewRadius;
        float angle = targetNpc.viewAngle;
        Vector3 eyeOffset = targetNpc.eyeOffset;
        Vector3 direction = targetNpc.GetDirection();

        int segments = 50;
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        //原点は目のオフセット
        vertices[0] = eyeOffset;

        float currentAngle = -angle / 2;
        float angleStep = angle / segments;

        for (int i = 0; i <= segments; i++)
        {
            //NPCの現在の向き(direction)を基準に扇形の頂点を計算する
            //計算は親のローカル空間で行い子のTransformに自動で変換させる
            Vector3 pointOnArcInWorld = (Quaternion.Euler(0, 0, currentAngle) * direction * radius);
            vertices[i + 1] = (Vector3)eyeOffset + transform.parent.InverseTransformVector(pointOnArcInWorld);

            if (i < segments)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            currentAngle += angleStep;
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }
}