using UnityEngine;
using System.Collections.Generic;

public class ParallaxController : MonoBehaviour
{
    Transform cam;
    Vector3 camStartPos;
    Vector3 groupStartPos;
    public Vector3 cameraFix;

    Renderer[] renderers;          // כל הרנדררים בכל הצאצאים
    Material[] mats;
    float[] backSpeed;
    float farthestBack;

    [Range(0.005f, 0.2f)]
    public float parallaxSpeed = 0.03f;

    string _texProp = "_MainTex";

    void Start()
    {
        cam = Camera.main.transform;
        camStartPos = cam.position;
        groupStartPos = transform.position;

        BuildLists();
        BackSpeedCalculate();
    }

    void BuildLists()
    {
        renderers = GetComponentsInChildren<Renderer>(true); // גם אם מושבת
        var matsList = new List<Material>();
        backSpeed = new float[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            var m = r.material; // אינסטנס פר רנדרר
            matsList.Add(m);

            if (i == 0)
            {
                if (m.HasProperty("_BaseMap")) _texProp = "_BaseMap"; else _texProp = "_MainTex";
            }

            var tex = m.GetTexture(_texProp);
            if (tex != null)
            {
#if UNITY_2017_1_OR_NEWER
                tex.wrapModeU = TextureWrapMode.Repeat;
                tex.wrapModeV = TextureWrapMode.Clamp;
#else
                tex.wrapMode = TextureWrapMode.Clamp;
#endif
            }
        }
        mats = matsList.ToArray();
    }

    void BackSpeedCalculate()
    {
        farthestBack = 0f;
        for (int i = 0; i < renderers.Length; i++)
        {
            float depth = renderers[i].transform.position.z - cam.position.z;
            if (depth > farthestBack) farthestBack = depth;
        }
        for (int i = 0; i < renderers.Length; i++)
        {
            float depth = renderers[i].transform.position.z - cam.position.z;
            backSpeed[i] = 1f - depth / Mathf.Max(0.0001f, farthestBack);
        }
    }

    public void Rebuild() // אם הוספת/הסרת שכבות בזמן ריצה
    {
        BuildLists();
        BackSpeedCalculate();
    }

    void LateUpdate()
    {
        transform.position = new Vector3(
            cam.position.x + cameraFix.x,
            cam.position.y + cameraFix.y,
            groupStartPos.z + cameraFix.z
        );

        float dx = cam.position.x - camStartPos.x;

        for (int i = 0; i < mats.Length; i++)
        {
            float speed = backSpeed[i] * parallaxSpeed;
            mats[i].SetTextureOffset(_texProp, new Vector2(dx * speed, 0f));
        }
    }
}
