using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class HalloweenArtBuilder
{
    const string Art = "Assets/_Project/Art/Halloween";
    static Transform stage;
    static Material purple, dark, orange, mint, pink, floor, wall;
    static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }

    [MenuItem("WhackAMole/Arte/Aplicar ambientação Halloween")]
    public static void Build()
    {
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        EnsurePipeline();
        purple = Mat("Cabinet - painted plum", Hex("#54316F"), Texture("Enamel", 0), .34f);
        dark = Mat("Midnight metal", Hex("#202136"), null, .5f);
        orange = Mat("Pumpkin neon", Hex("#FF801F"), null, .4f, 2.2f);
        mint = Mat("Ectoplasm neon", Hex("#89FFD2"), null, .3f, 1.8f);
        pink = Mat("Violet neon", Hex("#BA63FF"), null, .3f, 2f);
        floor = Mat("Arcade carpet", Color.white, Texture("Carpet", 1), .05f);
        wall = Mat("Acoustic wall", Color.white, Texture("Wall", 2), .1f);
        ConvertMaterial("Assets/MASH Virtual/Stylized Hammer/Material/Hammer.mat");
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Little_GhostLP(FREE)/Material" }))
            ConvertMaterial(AssetDatabase.GUIDToAssetPath(guid));
        foreach (string scene in new[] { "Game", "MainMenu", "GameOver" })
        {
            var opened = EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            var old = GameObject.Find("Halloween Arcade • Art");
            if (old) Object.DestroyImmediate(old);
            var camera = Camera.main;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#0D0B1C");
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#8474AB");
            RenderSettings.ambientEquatorColor = Hex("#504265");
            RenderSettings.ambientGroundColor = Hex("#252138");
            RenderSettings.fog = true;
            RenderSettings.fogColor = Hex("#151126");
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .009f;
            var root = new GameObject("Halloween Arcade • Art");
            stage = root.transform;
            if (scene == "Game") DressGame(camera);
            else DressMenu(scene, camera);
            EditorSceneManager.MarkSceneDirty(opened);
            EditorSceneManager.SaveScene(opened);
            ValidateArt(scene);
            Capture();
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        Debug.Log("HALLOWEEN_ART_BUILD_OK: all three build scenes saved; gameplay scripts unchanged.");
    }

    static void EnsurePipeline()
    {
        string path = Art + "/Halloween URP.asset";
        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (!pipeline)
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, Art + "/Halloween Renderer.asset");
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.supportsHDR = true;
            pipeline.supportsCameraDepthTexture = true;
            AssetDatabase.CreateAsset(pipeline, path);
        }
        GraphicsSettings.defaultRenderPipeline = pipeline;
        int quality = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
        QualitySettings.SetQualityLevel(quality, false);
    }

    public static void Capture()
    {
        var camera = Camera.main;
        var rt = new RenderTexture(1600, 1000, 24);
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var previous = canvas.renderMode;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        Canvas.ForceUpdateCanvases();
        camera.targetTexture = rt; camera.Render();
        var old = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
        Directory.CreateDirectory("ArtPreviews"); File.WriteAllBytes("ArtPreviews/" + camera.gameObject.scene.name + ".png", tex.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = old; canvas.renderMode = previous;
        Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
    }

    static void ValidateArt(string scene)
    {
        if (stage.GetComponentsInChildren<Collider>(true).Length != 0)
            throw new System.InvalidOperationException("Decoration must not intercept gameplay raycasts.");
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
                if (!m || !m.shader || !m.shader.isSupported)
                    throw new System.InvalidOperationException("Unsupported material: " + r.name);
        if (scene == "Game")
        {
            var randomizer = Object.FindFirstObjectByType<MoleRandomizer>();
            var list = new SerializedObject(randomizer).FindProperty("moles");
            if (list.arraySize != 5) throw new System.InvalidOperationException("Expected the five original targets.");
            for (int i = 0; i < list.arraySize; i++)
            {
                var target = (GameObject)list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!target || !target.GetComponent<GhostTarget>() || target.GetComponentsInChildren<Collider>(true).Length == 0)
                    throw new System.InvalidOperationException("Target reference or collider missing.");
            }
            if (!Object.FindFirstObjectByType<HammerInput>() || !Object.FindFirstObjectByType<HitManager>() || !Object.FindFirstObjectByType<GameManager>())
                throw new System.InvalidOperationException("Original gameplay component missing.");
        }
        Debug.Log("HALLOWEEN_VALIDATED: " + scene + " / shaders, decoration and original gameplay references OK");
    }

    static Material Mat(string name, Color color, Texture2D tex = null, float smooth = .3f, float glow = 0)
    {
        string path = Art + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", color); m.SetTexture("_BaseMap", tex);
        m.SetFloat("_Smoothness", smooth);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * glow); }
        EditorUtility.SetDirty(m); return m;
    }

    // Tileable original textures: lacquer flecks, woven arcade carpet and acoustic panels.
    static Texture2D Texture(string name, int kind)
    {
        const int n = 512;
        var t = new Texture2D(n, n, TextureFormat.RGB24, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float noise = ((x * 73856093u ^ y * 19349663u) % 101) / 100f;
            Color c;
            if (kind == 0) c = Color.white * (.80f + noise * .18f);
            else if (kind == 2)
            {
                c = Hex("#252038") * (.85f + noise * .22f);
                if (x % 128 < 3 || y % 64 < 3) c *= .5f;
            }
            else
            {
                c = Hex("#19142D") * (.75f + noise * .45f);
                int a = x % 128 - 64, b = y % 128 - 64;
                float diamond = Mathf.Abs(a) + Mathf.Abs(b);
                if (diamond > 33 && diamond < 36) c = Hex("#69437F");
                if ((a * a + b * b < 12) || (Mathf.Abs(a - 42) < 2 && Mathf.Abs(b + 28) < 9))
                    c = ((x / 128 + y / 128) % 2 == 0) ? Hex("#C07A36") : Hex("#3B988F");
            }
            t.SetPixel(x, y, c);
        }
        t.Apply(); string path = Art + "/" + name + ".png";
        File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat; importer.maxTextureSize = 512; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void ConvertMaterial(string path)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m || m.shader.name == "Universal Render Pipeline/Lit") return;
        var tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
        var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
        var normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
        m.shader = Shader.Find("Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .22f);
        if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        EditorUtility.SetDirty(m);
    }

    static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(stage, false);
        go.transform.localPosition = pos; go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.layer = 2; // Decorative geometry never intercepts the existing hit raycast.
        go.GetComponent<Renderer>().sharedMaterial = mat; return go;
    }

    static void Sign(string text, Vector3 pos, float size, Color color, Vector2 bounds)
    {
        var go = new GameObject("Sign • " + text); go.transform.SetParent(stage, false);
        go.transform.localPosition = pos; go.transform.localRotation = Quaternion.identity;
        var label = go.AddComponent<TextMeshPro>(); label.text = text;
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.fontSize = size; label.color = color; label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true; label.fontSizeMin = size * .5f; label.fontSizeMax = size;
        label.fontStyle = FontStyles.Bold; label.rectTransform.sizeDelta = bounds;
        label.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    static void Lamp(string name, Vector3 pos, Color color, float intensity, float range)
    {
        var go = new GameObject(name); go.transform.SetParent(stage, false); go.transform.localPosition = pos;
        var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = color;
        light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
    }

    static void Room(float width, float depth, float height)
    {
        var f = Box("Patterned arcade carpet", new Vector3(0, -.18f, 2), new Vector3(width, .3f, depth), floor);
        // Primitive cube UVs repeat the carpet across the large floor.
        floor.mainTextureScale = new Vector2(10, 10);
        wall.mainTextureScale = new Vector2(8, 4);
        Box("Back acoustic wall", new Vector3(0, height / 2, depth / 2), new Vector3(width, height, .35f), wall);
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Side acoustic wall", new Vector3(s * width / 2, height / 2, 2), new Vector3(.3f, height, depth), wall);
            Box("Floor neon strip", new Vector3(s * width / 2 - s * .2f, .35f, 2), new Vector3(.12f, .12f, depth), pink);
        }
        Box("Back wall amber rail", new Vector3(0, height * .60f, depth / 2 - .25f), new Vector3(width, .12f, .12f), orange);
        Box("Back wall mint rail", new Vector3(0, height * .60f + .35f, depth / 2 - .25f), new Vector3(width, .06f, .12f), mint);
        if (stage.gameObject.scene.name == "Game")
            Sign("A R C A D E   /   A F T E R   D A R K", new Vector3(0, height * .86f, depth / 2 - .3f), 11, Hex("#DDBBFF"), new Vector2(width * .8f, 3));
        for (int i = -2; i <= 2; i++) Cabinet(new Vector3(i * width * .18f, 0, depth / 2 - 3), i);
        Lamp("Violet room wash", new Vector3(-width / 3, height * .65f, 2), Hex("#AD66FF"), 65, width);
        Lamp("Mint room wash", new Vector3(width / 3, height * .65f, 5), Hex("#73FFCB"), 55, width);
    }

    static void Cabinet(Vector3 p, int index)
    {
        Material glow = index % 2 == 0 ? pink : mint;
        Box("Arcade cabinet / base", p + new Vector3(0, 2.1f, 0), new Vector3(3.1f, 4.2f, 2.6f), dark);
        Box("Arcade cabinet / head", p + new Vector3(0, 5.1f, .4f), new Vector3(3.1f, 3, 1.8f), purple);
        Box("CRT luminous bezel", p + new Vector3(0, 5.1f, -.55f), new Vector3(2.65f, 2.1f, .12f), glow);
        Box("CRT glass", p + new Vector3(0, 5.1f, -.64f), new Vector3(2.38f, 1.85f, .08f), dark);
        Box("Control deck", p + new Vector3(0, 3.6f, -.7f), new Vector3(3.1f, .25f, 1.5f), purple);
        Box("Arcade marquee", p + new Vector3(0, 6.85f, -.4f), new Vector3(3.1f, .55f, .2f), glow);
        Box("Coin slot", p + new Vector3(.6f, 1.8f, -1.34f), new Vector3(.35f, .65f, .05f), orange);
        for (int j = 0; j < 3; j++) Box("Screen pixel", p + new Vector3((j - 1) * .52f, 5 + (j % 2) * .4f, -.71f), new Vector3(.28f, .28f, .03f), glow);
        Box("Joystick", p + new Vector3(-.6f, 3.95f, -1), new Vector3(.12f, .5f, .12f), orange);
        for (int j = 0; j < 2; j++) Box("Arcade button", p + new Vector3(.45f + j * .45f, 3.78f, -1), new Vector3(.25f, .1f, .25f), glow);
    }

    static void DressGame(Camera camera)
    {
        var machine = GameObject.Find("Maquina");
        if (!machine) throw new System.InvalidOperationException("Expected existing Maquina in Game scene.");
        var renderers = machine.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) { bounds.Encapsulate(r.bounds); DressMachineMesh(r); }
        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        stage.position = new Vector3(bounds.center.x, bounds.min.y - .05f, bounds.center.z);
        stage.rotation = Quaternion.LookRotation(forward);
        float span = Mathf.Max(bounds.size.x, bounds.size.z);
        Room(span * 3.5f, span * 3.6f, Mathf.Max(20, bounds.size.y * 1.2f));
        Lamp("Pumpkin key light", new Vector3(-4, bounds.size.y + 3, -8), Hex("#FFBD79"), 110, span * 2);
        Lamp("Ectoplasm rim light", new Vector3(6, bounds.size.y, 3), Hex("#9BFFE4"), 75, span * 1.6f);
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Directional) { light.color = Hex("#C7BDDF"); light.intensity = 1.1f; }
        // Marquee sits above the original mesh, leaving its holes and collision surfaces intact.
        float y = 20;
        Box("Halloween marquee casing", new Vector3(0, y, 3.05f), new Vector3(9.3f, 3, .22f), dark);
        Box("Marquee top neon", new Vector3(0, y + 1.45f, 2.9f), new Vector3(9.3f, .10f, .12f), orange);
        Box("Marquee bottom neon", new Vector3(0, y - 1.45f, 2.9f), new Vector3(9.3f, .10f, .12f), pink);
        Sign("GHOST BONK!", new Vector3(0, y + .2f, 2.87f), 11, Hex("#FFB65F"), new Vector2(9, 2));
        Sign("H A U N T E D   A R C A D E", new Vector3(0, y - .9f, 2.85f), 3.4f, Hex("#A6FFDE"), new Vector2(9, .7f));
        Sign("BOO!", new Vector3(0, 16, 3.05f), 16, Hex("#A6FFDE"), new Vector2(7, 3));
        for (int s = -1; s <= 1; s += 2)
        {
            Box("Backboard neon edge", new Vector3(s * 4.7f, 17, 3), new Vector3(.10f, 9, .13f), orange);
            var rail = Box("Playfield neon edge", new Vector3(s * 4.8f, 11.8f, -1), new Vector3(.11f, .1f, 9), pink);
            rail.transform.localRotation = Quaternion.Euler(-20, 0, 0);
        }
        foreach (var ghost in Object.FindObjectsByType<GhostTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var visual = ghost.GetComponentInChildren<Renderer>(); if (!visual) continue;
            Vector3 p = stage.InverseTransformPoint(visual.bounds.center); p.y = visual.bounds.min.y - stage.position.y + .4f;
            var portal = new GameObject("Ectoplasm portal rim"); portal.transform.SetParent(stage, false);
            var line = portal.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
            line.positionCount = 64; line.widthMultiplier = .075f; line.sharedMaterial = mint;
            line.shadowCastingMode = ShadowCastingMode.Off;
            for (int k = 0; k < 64; k++) { float a = k * Mathf.PI * 2 / 64; line.SetPosition(k, p + new Vector3(Mathf.Cos(a) * 1.03f, 0, Mathf.Sin(a) * 1.03f)); }
        }
        camera.transform.position = stage.TransformPoint(new Vector3(0, 22.8f, -27));
        camera.transform.LookAt(stage.TransformPoint(new Vector3(0, 12.4f, 0)));
        AddHammer();
        var pumpkin = GameObject.CreatePrimitive(PrimitiveType.Sphere); pumpkin.name = "Jack-o-lantern cabinet emblem";
        pumpkin.transform.SetParent(stage, false); pumpkin.transform.localPosition = new Vector3(0, 5.3f, -5.3f);
        pumpkin.transform.localScale = new Vector3(3.6f, 3, .7f); pumpkin.layer = 2;
        Object.DestroyImmediate(pumpkin.GetComponent<Collider>());
        pumpkin.GetComponent<Renderer>().sharedMaterial = Mat("Pumpkin enamel", Hex("#E78329"), null, .25f);
        Box("Pumpkin stem", new Vector3(0, 6.9f, -5.3f), new Vector3(.35f, .55f, .4f), mint);
        for (int s = -1; s <= 1; s += 2)
        {
            var eye = Box("Pumpkin eye", new Vector3(s * .65f, 5.65f, -5.65f), new Vector3(.52f, .52f, .07f), dark);
            eye.transform.localRotation = Quaternion.Euler(0, 0, 45);
        }
        Sign("w", new Vector3(0, 4.8f, -5.72f), 12, Hex("#1C1427"), new Vector2(2, 1));
        PostProcessing();
        StyleUI("Game");
    }

    static void DressMachineMesh(Renderer renderer)
    {
        var filter = renderer.GetComponent<MeshFilter>();
        if (!filter) { renderer.sharedMaterial = purple; return; }
        string path = Art + "/Machine visual mesh.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh)
        {
            mesh = Object.Instantiate(filter.sharedMesh); mesh.name = "Halloween machine surface groups";
            var vertices = mesh.vertices; var triangles = mesh.triangles;
            var shell = new System.Collections.Generic.List<int>();
            var deck = new System.Collections.Generic.List<int>();
            var uv = new Vector2[vertices.Length];
            for (int v = 0; v < vertices.Length; v++) uv[v] = new Vector2(vertices[v].x, vertices[v].z) * .25f;
            mesh.uv = uv;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a).normalized;
                var group = Mathf.Abs(normal.y) > .4f && (a.y + b.y + c.y) / 3 > 8 ? deck : shell;
                group.Add(triangles[i]); group.Add(triangles[i + 1]); group.Add(triangles[i + 2]);
            }
            mesh.subMeshCount = 2; mesh.SetTriangles(shell, 0); mesh.SetTriangles(deck, 1);
            AssetDatabase.CreateAsset(mesh, path);
        }
        filter.sharedMesh = mesh; renderer.sharedMaterials = new[] { purple, dark };
    }

    static void AddHammer()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MASH Virtual/Stylized Hammer/Prefab/Hammer.prefab");
        var prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        prop.name = "Wooden hammer • display prop";
        prop.transform.SetParent(stage, false); prop.transform.localPosition = new Vector3(-4.2f, 12.3f, -1.5f);
        prop.transform.localRotation = Quaternion.Euler(10, 20, -30);
        var r = prop.GetComponentInChildren<Renderer>();
        float length = Mathf.Max(r.bounds.size.x, r.bounds.size.y, r.bounds.size.z);
        prop.transform.localScale *= 3.4f / Mathf.Max(.001f, length);
        foreach (var c in prop.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        foreach (var t in prop.GetComponentsInChildren<Transform>()) t.gameObject.layer = 2;
    }

    static void PostProcessing()
    {
        string path = Art + "/Halloween Volume.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, path);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(.32f); bloom.threshold.Override(1.15f); bloom.scatter.Override(.65f);
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.24f); vignette.smoothness.Override(.5f);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        }
        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) v.enabled = false;
        var volume = new GameObject("Halloween color & bloom"); volume.transform.SetParent(stage, false);
        var vol = volume.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 10; vol.sharedProfile = profile;
    }

    static void DressMenu(string scene, Camera camera)
    {
        stage.position = Vector3.zero; stage.rotation = Quaternion.identity;
        Room(40, 42, 20);
        camera.transform.position = new Vector3(0, 9, -18);
        camera.transform.LookAt(new Vector3(0, 7, 12));
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) l.intensity = .65f;
        StyleUI(scene); PostProcessing();
    }

    static TextMeshProUGUI UILabel(Transform parent, string name, string text, Vector2 pos, Vector2 size, float font)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = font;
        t.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        t.alignment = TextAlignmentOptions.Center; t.color = Hex("#FFEBD5"); t.raycastTarget = false;
        Place(t.rectTransform, pos, size); return t;
    }
    static void Place(RectTransform r, Vector2 pos, Vector2 size)
    { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = size; r.localScale = Vector3.one; }

    static void StyleUI(string scene)
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas) return;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var previous = canvas.transform.Find("Halloween UI Decoration"); if (previous) Object.DestroyImmediate(previous.gameObject);
        var decoration = new GameObject("Halloween UI Decoration", typeof(RectTransform)); decoration.transform.SetParent(canvas.transform, false);
        var d = (RectTransform)decoration.transform; d.anchorMin = Vector2.zero; d.anchorMax = Vector2.one; d.sizeDelta = Vector2.zero; d.SetAsFirstSibling();
        if (scene == "Game")
        {
            var timer = GameObject.Find("TimerText").GetComponent<TextMeshProUGUI>();
            Place(timer.rectTransform, new Vector2(0, 455), new Vector2(250, 90)); timer.fontSize = 44; timer.color = Hex("#A6FFDE"); timer.raycastTarget = false;
            UILabel(d, "Game identity", "GHOST BONK!", new Vector2(-670, 455), new Vector2(470, 70), 36).color = Hex("#FFB65F");
            UILabel(d, "Controls hint", "CLIQUE NOS FANTASMAS   /   CUIDADO COM OS ERROS", new Vector2(0, -485), new Vector2(1500, 50), 23);
            return;
        }
        var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Array.Sort(buttons, (a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
        foreach (var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (!text.GetComponentInParent<Button>()) text.gameObject.SetActive(false);
        UILabel(d, "Edition", "N I G H T   S H I F T   A R C A D E", new Vector2(0, 330), new Vector2(1400, 80), 26).color = Hex("#A6FFDE");
        UILabel(d, "Title", scene == "MainMenu" ? "GHOST BONK!" : "FIM DE JOGO", new Vector2(0, 205), new Vector2(1500, 180), 110).color = Hex("#FFB65F");
        UILabel(d, "Subtitle", scene == "MainMenu" ? "Um fliperama de arrepiar." : "Os fantasmas venceram esta rodada.", new Vector2(0, 82), new Vector2(1400, 80), 30);
        int i = 0;
        foreach (var button in buttons)
        {
            if (!button.gameObject.activeInHierarchy) continue;
            Place((RectTransform)button.transform, new Vector2(0, -70 - i * 108), new Vector2(530, 82));
            var image = button.GetComponent<Image>(); if (image) { image.sprite = null; image.color = Hex(i == 0 ? "#E37827" : "#322344"); }
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Hex("#FFE1AB"); colors.pressedColor = Hex("#B18ACB"); button.colors = colors;
            foreach (var text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                var value = text.text.Trim().ToLowerInvariant();
                string method = button.onClick.GetPersistentEventCount() > 0 ? button.onClick.GetPersistentMethodName(0) : "";
                text.text = method == "PlayAgain" || value.Contains("again") || value.Contains("novamente") ? "JOGAR NOVAMENTE" : method == "OpenOptions" || value.Contains("options") || value.Contains("opções") ? "OPÇÕES" : method == "ExitGame" || method == "QuitGame" || value.Contains("exit") || value.Contains("sair") ? "SAIR" : "JOGAR";
                text.color = Hex("#FFF2DD"); text.fontSize = 30; text.fontStyle = FontStyles.Bold;
                Place(text.rectTransform, Vector2.zero, new Vector2(510, 72));
            }
            i++;
        }
        UILabel(d, "Footer", "HALLOWEEN EDITION   •   FANTASMAS À SOLTA", new Vector2(0, -460), new Vector2(1400, 60), 21).color = Hex("#AD91C1");
    }
    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
        var report = new StringBuilder();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            report.AppendLine($"RENDER {r.name} position={r.transform.position} bounds={r.bounds} materials={string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m ? m.name : "null"))}");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.parent == null) report.AppendLine($"ROOT {t.name} {t.position} {t.eulerAngles}");
        File.WriteAllText("ArtInspection.txt", report.ToString());
    }
}
