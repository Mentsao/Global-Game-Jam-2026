using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SquiglyText : MonoBehaviour
{
    public enum Mode { Global, Selected }

    [Header("Mode Settings")]
    public Mode mode = Mode.Global;
    public List<TextMeshProUGUI> selectedTexts = new();

    [Header("Animation Settings")]
    public int frames = 4;
    public float frameRate = 8f;
    public float jitter = 0.5f;
    public bool autoRefresh = true;

    private float timer;
    private int currentFrame;

    private Dictionary<TextMeshProUGUI, Vector3[][]> textFrames = new();
    private Dictionary<TextMeshProUGUI, float> lastAlpha = new();

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        textFrames.Clear();
        lastAlpha.Clear();

        if (mode == Mode.Global)
        {
            var allTexts = FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var tmp in allTexts)
                TryRegister(tmp);
        }
        else
        {
            foreach (var tmp in selectedTexts)
                TryRegister(tmp);
        }
    }

    void Update()
    {
        if (autoRefresh && mode == Mode.Global && Time.frameCount % 120 == 0)
        {
            var allTexts = FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var tmp in allTexts)
                TryRegister(tmp);
        }

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames;
            AnimateAll();
        }
    }

    void TryRegister(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        float alpha = tmp.alpha;

        if (!lastAlpha.ContainsKey(tmp))
            lastAlpha[tmp] = alpha;

        if (!tmp.gameObject.activeInHierarchy || alpha <= 0.001f)
            return;

        if (!textFrames.ContainsKey(tmp))
        {
            textFrames[tmp] = GenerateFrames(tmp);
        }
    }

    Vector3[][] GenerateFrames(TextMeshProUGUI tmp)
    {
        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        if (textInfo.meshInfo.Length == 0)
            return new Vector3[0][];

        Vector3[] baseVerts = textInfo.meshInfo[0].vertices;
        if (baseVerts == null || baseVerts.Length == 0)
            return new Vector3[0][];

        Vector3[][] framesArray = new Vector3[frames][];

        for (int f = 0; f < frames; f++)
        {
            Vector3[] vertsCopy = new Vector3[baseVerts.Length];

            for (int i = 0; i < baseVerts.Length; i++)
            {
                vertsCopy[i] = baseVerts[i] + new Vector3(
                    Random.Range(-jitter, jitter),
                    Random.Range(-jitter, jitter),
                    0f
                );
            }

            framesArray[f] = vertsCopy;
        }

        return framesArray;
    }

    void AnimateAll()
    {
        foreach (var entry in textFrames)
        {
            TextMeshProUGUI tmp = entry.Key;
            if (tmp == null) continue;

            float currentAlpha = tmp.alpha;
            float prevAlpha = lastAlpha[tmp];

            if (prevAlpha <= 0.001f && currentAlpha > 0.001f)
            {
                textFrames[tmp] = GenerateFrames(tmp);
            }

            lastAlpha[tmp] = currentAlpha;

            if (!tmp.gameObject.activeInHierarchy || currentAlpha <= 0.001f)
                continue;

            tmp.ForceMeshUpdate();
            TMP_TextInfo textInfo = tmp.textInfo;

            Vector3[][] framesArray = entry.Value;
            if (framesArray == null || framesArray.Length == 0)
                continue;

            Vector3[] frameVerts = framesArray[currentFrame];
            if (frameVerts.Length != textInfo.meshInfo[0].vertices.Length)
            {
                textFrames[tmp] = GenerateFrames(tmp);
                continue;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                var meshInfo = textInfo.meshInfo[i];
                var verts = meshInfo.vertices;

                for (int v = 0; v < verts.Length; v++)
                    verts[v] = frameVerts[v];

                meshInfo.mesh.vertices = verts;
                tmp.UpdateGeometry(meshInfo.mesh, i);
            }
        }
    }


    public void AddText(TextMeshProUGUI tmp)
    {
        TryRegister(tmp);
    }

    public void RemoveText(TextMeshProUGUI tmp)
    {
        if (textFrames.ContainsKey(tmp))
            textFrames.Remove(tmp);

        if (lastAlpha.ContainsKey(tmp))
            lastAlpha.Remove(tmp);
    }

    public void RefreshAll()
    {
        Initialize();
    }
}
