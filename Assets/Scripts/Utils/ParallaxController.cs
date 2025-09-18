using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    Transform cam;
    Vector3 camStartPos;
    Vector3 groupStartPos;
    public Vector3 cameraFix;

    GameObject[] backgrounds;
    Material[] mat;
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

        int n = transform.childCount;
        mat = new Material[n];
        backSpeed = new float[n];
        backgrounds = new GameObject[n];

        for (int i = 0; i < n; i++)
        {
            backgrounds[i] = transform.GetChild(i).gameObject;
            var r = backgrounds[i].GetComponent<Renderer>();
            mat[i] = r.material;

            if (i == 0)
            {
                if (mat[i].HasProperty("_BaseMap")) _texProp = "_BaseMap"; else _texProp = "_MainTex";
            }

            // חשוב: לא רפיט ב-Y
            var tex = mat[i].GetTexture(_texProp);
            if (tex != null)
            {
#if UNITY_2017_1_OR_NEWER
                tex.wrapModeU = TextureWrapMode.Repeat; // X ממשיך להיות אינסופי
                tex.wrapModeV = TextureWrapMode.Clamp;  // Y ננעל — ללא רפיט אנכי
#else
                tex.wrapMode = TextureWrapMode.Clamp;   // (לגרסאות ישנות—ינעל גם X)
#endif
            }
        }

        BackSpeedCalculate(n);
    }

    void BackSpeedCalculate(int n)
    {
        farthestBack = 0f;
        for (int i = 0; i < n; i++)
        {
            float depth = backgrounds[i].transform.position.z - cam.position.z;
            if (depth > farthestBack) farthestBack = depth;
        }
        for (int i = 0; i < n; i++)
        {
            float depth = backgrounds[i].transform.position.z - cam.position.z;
            backSpeed[i] = 1f - depth / Mathf.Max(0.0001f, farthestBack);
        }
    }

    void LateUpdate()
    {
        // הקבוצה זזה עם המצלמה גם ב-Y (כל התמונה עולה/יורדת כיחידה אחת)
        transform.position = new Vector3(
            cam.position.x + cameraFix.x,
            cam.position.y + cameraFix.y,
            groupStartPos.z + cameraFix.z
        );

        // offset ב-UV רק ב-X (Y=0 כדי שלא תהיה גלילה אנכית של הטקסטורה)
        float dx = cam.position.x - camStartPos.x;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            float speed = backSpeed[i] * parallaxSpeed;
            mat[i].SetTextureOffset(_texProp, new Vector2(dx * speed, 0f));
        }
    }
}
