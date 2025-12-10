using UnityEngine;

/// <summary>
/// NPCの視界（Field of View）をメッシュで可視化するコンポーネント。
/// MeshFilter/MeshRendererを必須とし、Startでメッシュとマテリアルを初期化、LateUpdateで毎フレームメッシュを再構築する。
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FOVVisualizer : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("パラメータの参照元となるNPCのコントローラー")]
    /// <summary>
    /// 視界パラメータの参照先NPCコントローラー。InspectorでD&D。null時は描画を行わない。
    /// </summary>
    public NPCController targetNpc;

    [Tooltip("描画に使うEditorで作成した半透明マテリアル")]
    /// <summary>
    /// メッシュ描画に使用するマテリアル。InspectorでD&D。
    /// </summary>
    public Material fovMaterial;

    /// <summary>
    /// 描画用のメッシュインスタンス。Startで生成してMeshFilterに割り当てる。
    /// </summary>
    private Mesh viewMesh;

    /// <summary>
    /// Unity Start。Awakeの後、最初のフレームの直前に呼ばれる。
    /// メッシュの生成およびMeshFilter／MeshRendererへの割り当てを行う。
    /// </summary>
    void Start()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        GetComponent<MeshFilter>().mesh = viewMesh;
        GetComponent<MeshRenderer>().material = fovMaterial;
    }

    /// <summary>
    /// Unity LateUpdate。各フレームのUpdate処理が完了した後に呼ばれる。
    /// targetNpcから視界パラメータを取得し、扇形のメッシュを生成して視界を可視化する。
    /// targetNpcがnullの場合は処理を行わない。
    /// </summary>
    void LateUpdate()
    {
        if (targetNpc == null) return;

        // 親(NPC)からパラメータを参照して、メッシュ生成に使用
        float radius = targetNpc.viewRadius;
        float angle = targetNpc.viewAngle;
        Vector3 eyeOffset = targetNpc.eyeOffset;
        Vector3 direction = targetNpc.GetDirection();

        int segments = 50;
        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        // メッシュの原点を目のオフセットに設定。親のローカル空間基準で描画するために目位置を基準にする。
        vertices[0] = eyeOffset;

        float currentAngle = -angle / 2;
        float angleStep = angle / segments;

        for (int i = 0; i <= segments; i++)
        {
            // NPCの向き(direction)を基準に扇形の頂点をワールド空間で計算し、
            // 親のローカル空間に変換してメッシュ頂点として設定する。
            Vector3 pointOnArcInWorld = (Quaternion.Euler(0, 0, currentAngle) * direction * radius);
            vertices[i + 1] = (Vector3)eyeOffset + transform.parent.InverseTransformVector(pointOnArcInWorld);

            if (i < segments)
            {
                // 三角形インデックスを設定して扇形を構成
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            currentAngle += angleStep;
        }

        // 古いメッシュデータをクリアして新しい頂点・三角形を適用、法線を再計算する。
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }
}