using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FOVRenderer : MonoBehaviour
{
    // Inspectorから見えない内部変数
    private float viewRadius;
    private float viewAngle;
    private int segments = 50;
    private Color viewColor = new Color(1f, 0.5f, 0f, 0.3f);

    private MeshFilter viewMeshFilter;
    private Mesh viewMesh;

    void Awake()
    {
        viewMeshFilter = GetComponent<MeshFilter>();
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

        Material viewMaterial = new Material(Shader.Find("Sprites/Default"));
        viewMaterial.color = viewColor;
        GetComponent<MeshRenderer>().material = viewMaterial;
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    // 親から設定を受け取るためのメソッド
    public void SetViewParameters(float angle, float radius)
    {
        viewAngle = angle;
        viewRadius = radius;
    }

    // 扇形のメッシュを描画するメインの処理
    void DrawFieldOfView()
    {
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;

        float currentAngle = -viewAngle / 2;
        float angleStep = viewAngle / segments;

        for (int i = 0; i <= segments; i++)
        {
            // 常にローカル座標の右方向(Vector3.right)を基準にベクトルを計算
            Vector3 point = Quaternion.Euler(0, 0, currentAngle) * Vector3.right * viewRadius;
            vertices[i + 1] = point;

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