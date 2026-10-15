using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 1인칭에서 내 몸 중 팔만 보이게 한다. 모델이 메시 하나로 통째라 렌더러로는 나눌 수 없어서,
// 시작할 때 팔 뼈에 붙은 삼각형만 골라 메시를 하나 더 만든다. 뼈는 원본과 같이 쓰니
// 애니메이션과 ArmAim이 그대로 따라온다.
// 원본 몸은 PlayerController가 그림자 전용으로 바꾼다. 그 뒤에 만들어야 팔까지
// 그림자 전용이 되지 않아서 늦게 실행한다.
[DefaultExecutionOrder(50)]
public class FirstPersonArms : MonoBehaviour
{
    [Tooltip("비워두면 자식에서 찾는다. 모델 Import Settings에서 Read/Write가 켜져 있어야 한다.")]
    [SerializeField] private SkinnedMeshRenderer body;
    [Tooltip("뼈 이름에 이 중 하나가 들어 있으면 팔로 친다. 어깨(Shoulder)는 목 근처까지 보여서 뺀다.")]
    [SerializeField] private string[] armBoneKeywords = { "Arm", "Hand" };

    private void Start()
    {
        if (body == null)
            body = GetComponentInChildren<SkinnedMeshRenderer>();
        if (body == null || !body.sharedMesh.isReadable)
        {
            Debug.LogWarning("[FirstPersonArms] 몸 메시가 없거나 Read/Write가 꺼져 있다.");
            return;
        }

        Mesh arms = BuildArmsMesh(body.sharedMesh, FindArmBones(body.bones));

        GameObject armsObject = new GameObject("FirstPersonArms");
        armsObject.transform.SetParent(body.transform.parent, false);
        armsObject.transform.localPosition = body.transform.localPosition;
        armsObject.transform.localRotation = body.transform.localRotation;
        armsObject.transform.localScale = body.transform.localScale;

        SkinnedMeshRenderer armsRenderer = armsObject.AddComponent<SkinnedMeshRenderer>();
        armsRenderer.sharedMesh = arms;
        armsRenderer.bones = body.bones;
        armsRenderer.rootBone = body.rootBone;
        armsRenderer.sharedMaterials = body.sharedMaterials;
        armsRenderer.localBounds = body.localBounds;
        // 그림자는 원본 몸이 이미 드리운다.
        armsRenderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private bool[] FindArmBones(Transform[] bones)
    {
        bool[] isArm = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
        {
            if (bones[i] == null)
                continue;
            foreach (string keyword in armBoneKeywords)
                if (bones[i].name.Contains(keyword))
                    isArm[i] = true;
        }
        return isArm;
    }

    // 세 꼭짓점이 모두 팔 뼈에 가장 크게 붙어 있는 삼각형만 남긴다.
    // 하나라도 몸통 쪽이면 몸통까지 늘어난 조각이 보일 수 있어서 뺀다.
    private Mesh BuildArmsMesh(Mesh source, bool[] isArm)
    {
        Mesh mesh = Instantiate(source);
        mesh.name = source.name + " (Arms)";

        // weight0이 가장 큰 가중치다.
        BoneWeight[] weights = source.boneWeights;
        bool[] armVertex = new bool[weights.Length];
        for (int v = 0; v < weights.Length; v++)
            armVertex[v] = isArm[weights[v].boneIndex0];

        for (int sub = 0; sub < source.subMeshCount; sub++)
        {
            int[] triangles = source.GetTriangles(sub);
            List<int> kept = new List<int>();
            for (int t = 0; t < triangles.Length; t += 3)
            {
                if (armVertex[triangles[t]] && armVertex[triangles[t + 1]] && armVertex[triangles[t + 2]])
                {
                    kept.Add(triangles[t]);
                    kept.Add(triangles[t + 1]);
                    kept.Add(triangles[t + 2]);
                }
            }
            mesh.SetTriangles(kept, sub);
        }
        return mesh;
    }
}
