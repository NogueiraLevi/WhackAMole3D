using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum MusicType { Start, Gameplay, HighScores, GameOver }

/// <summary>
/// ÁUDIO AUTOMÁTICO DO GHOST BONK.
///
/// Basta ter este arquivo no projeto (Assets/Scripts). Ele cria o próprio objeto "GameAudio" ao dar Play,
/// acha os .wav sozinho e:
///  - toca ACERTO / ERRO / ATRASADO ao clicar, observando os fantasmas (não precisa mexer no seu código)
///  - troca a música conforme a tela (início, partida, melhores pontuações, game over)
///
/// Como ele reconhece os fantasmas: objetos com "ghost" ou "fantasma" no NOME (veja "Ghost Keywords").
///  ACERTO   = o fantasma sob o cursor some logo após o clique
///  ATRASADO = o clique pega um lugar onde um fantasma sumiu há poucos instantes
///  ERRO     = o clique não pega nenhum fantasma
///
/// Uso manual (opcional): GameAudio.Instance.PlayHit(); PlayMiss(); PlayLate(); PlayMusic(MusicType.GameOver);
/// </summary>
[DefaultExecutionOrder(-100)] // roda antes dos outros scripts: vê o fantasma ANTES de o seu código reagir ao clique
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [Header("Fontes de áudio (opcional: criadas sozinhas se vazias)")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Efeitos (opcional: achados sozinhos se vazios)")]
    [SerializeField] private AudioClip hit;
    [SerializeField] private AudioClip miss;
    [SerializeField] private AudioClip late;

    [Header("Músicas (opcional: achadas sozinhas se vazias)")]
    [SerializeField] private AudioClip startMusic;
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip highScoresMusic;
    [SerializeField] private AudioClip gameOverMusic;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    [Header("Efeitos automáticos (acerto / erro / atrasado)")]
    [SerializeField] private bool autoSfx = true;
    [Tooltip("Pedaços do NOME dos objetos dos fantasmas")]
    [SerializeField] private string[] ghostKeywords = { "ghost", "fantasma" };
    [Tooltip("Objetos com esses pedaços no nome NUNCA são tratados como fantasma")]
    [SerializeField] private string[] ignoreKeywords =
        { "bonk", "sign", "title", "logo", "arcade", "cabinet", "machine", "hammer", "marreta", "martelo",
          "spawn", "manager", "pool", "holder", "container", "group", "controller", "hole", "buraco",
          "score", "pontu", "timer", "tempo" };
    [Tooltip("Folga em volta do fantasma que conta como clique nele (unidades do mundo)")]
    [SerializeField] private float clickRadius = 0.4f;
    [Tooltip("Tempo (s) esperando o fantasma sumir depois do clique para contar como acerto")]
    [SerializeField] private float hitWindow = 0.35f;
    [Tooltip("Até quantos segundos depois de o fantasma sumir o clique conta como 'atrasado'")]
    [SerializeField] private float lateWindow = 0.6f;
    [Tooltip("Mostra no Console o que o script decidiu (ajuda a ajustar)")]
    [SerializeField] private bool debugLog = false;

    [Header("Música automática por tela")]
    [SerializeField] private bool autoMusic = true;

    // ---------------- estado ----------------
    private MusicType? currentMusic;
    private bool manualMusic;          // vira true se algum script chamar PlayMusic (ex.: ScreenMusic)
    private string[] ghostKeysN, ignoreKeysN;
    private bool usedEditorFallback;

    private class Ghost
    {
        public GameObject go;
        public Renderer[] rends;
        public bool seen, visible;
        public Bounds bounds;
        public float hiddenAt = -999f;
        public int hiddenFrame = -999;
    }

    private readonly Dictionary<GameObject, Ghost> ghosts = new Dictionary<GameObject, Ghost>();
    private readonly List<Ghost> pending = new List<Ghost>();
    private bool pendingActive;
    private float pendingTime, nextScan, nextMusicTick, lastGhostVisible = -999f;

    // ---------------- criação automática ----------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("GameAudio").AddComponent<GameAudio>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false; sfxSource.playOnAwake = false;
        musicSource.volume = musicVolume; sfxSource.volume = sfxVolume;

        if (hit == null) hit = LoadClip("sfx_hit");
        if (miss == null) miss = LoadClip("sfx_miss");
        if (late == null) late = LoadClip("sfx_late");
        if (startMusic == null) startMusic = LoadClip("music_start");
        if (gameplayMusic == null) gameplayMusic = LoadClip("music_gameplay");
        if (highScoresMusic == null) highScoresMusic = LoadClip("music_highscores");
        if (gameOverMusic == null) gameOverMusic = LoadClip("music_gameover");

        if (usedEditorFallback)
            Debug.Log("GameAudio: sons achados pelo Editor. Para funcionarem também no jogo exportado (build), " +
                      "mova os .wav para Assets/Resources/Audio.");

        ghostKeysN = NormAll(ghostKeywords);
        ignoreKeysN = NormAll(ignoreKeywords);
    }

    private AudioClip LoadClip(string name)
    {
        AudioClip c = Resources.Load<AudioClip>("Audio/" + name);
        if (c == null) c = Resources.Load<AudioClip>(name);
#if UNITY_EDITOR
        if (c == null)
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
                {
                    c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    usedEditorFallback = true;
                    break;
                }
            }
        }
#endif
        if (c == null) Debug.LogWarning($"GameAudio: não achei o som '{name}'. Coloque o .wav em Assets/Resources/Audio ou arraste no Inspector.");
        return c;
    }

    private void Update()
    {
        if (autoSfx)
        {
            RefreshAll();
            if (Time.unscaledTime >= nextScan) { Scan(); nextScan = Time.unscaledTime + 0.25f; }
            if (GetClickDown()) OnClick();
            UpdatePending();
        }
        MusicTick();
    }

    // ---------------- Músicas ----------------

    /// <summary>Toca a música da tela (chamada manual desliga a música automática).</summary>
    public void PlayMusic(MusicType type)
    {
        manualMusic = true;
        SwitchMusic(type);
    }

    private void SwitchMusic(MusicType type)
    {
        if (currentMusic == type && musicSource.isPlaying) return;
        AudioClip clip = GetClip(type);
        if (clip == null) return;
        currentMusic = type;
        musicSource.clip = clip;
        musicSource.loop = type != MusicType.GameOver;
        musicSource.Play();
        if (debugLog) Debug.Log($"GameAudio: música -> {type}");
    }

    public void StopMusic() { musicSource.Stop(); currentMusic = null; }
    public void EnableAutoMusic() { manualMusic = false; }

    private AudioClip GetClip(MusicType type)
    {
        switch (type)
        {
            case MusicType.Start:      return startMusic;
            case MusicType.Gameplay:   return gameplayMusic;
            case MusicType.HighScores: return highScoresMusic;
            default:                   return gameOverMusic;
        }
    }

    public void PlayStartMusic()      => PlayMusic(MusicType.Start);
    public void PlayGameplayMusic()   => PlayMusic(MusicType.Gameplay);
    public void PlayHighScoresMusic() => PlayMusic(MusicType.HighScores);
    public void PlayGameOverMusic()   => PlayMusic(MusicType.GameOver);

    // ---------------- Efeitos ----------------

    public void PlayHit()  { PlaySfx(hit);  if (debugLog) Debug.Log("GameAudio: ACERTO"); }
    public void PlayMiss() { PlaySfx(miss); if (debugLog) Debug.Log("GameAudio: ERRO"); }
    public void PlayLate() { PlaySfx(late); if (debugLog) Debug.Log("GameAudio: ATRASADO"); }

    private void PlaySfx(AudioClip clip) { if (clip != null) sfxSource.PlayOneShot(clip); }

    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); musicSource.volume = musicVolume; }
    public void SetSfxVolume(float v)   { sfxVolume = Mathf.Clamp01(v);   sfxSource.volume = sfxVolume; }

    // ---------------- Detecção de fantasmas ----------------

    private static Renderer[] FindAllRenderers()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<Renderer>(true);
#endif
    }

    private static RectTransform[] FindActiveRects()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
#else
        return UnityEngine.Object.FindObjectsOfType<RectTransform>();
#endif
    }

    private GameObject GhostRoot(Transform t)
    {
        GameObject found = null;
        for (Transform c = t; c != null; c = c.parent)
        {
            string n = Norm(c.name);
            bool isGhost = HasAny(n, ghostKeysN) && !HasAny(n, ignoreKeysN);
            if (isGhost) found = c.gameObject;
            else if (found != null) break;
        }
        return found;
    }

    /// <summary>Procura fantasmas novos na cena e guarda seus renderers.</summary>
    private void Scan()
    {
        var groups = new Dictionary<GameObject, List<Renderer>>();
        foreach (Renderer r in FindAllRenderers())
        {
            if (r == null) continue;
            GameObject root = GhostRoot(r.transform);
            if (root == null) continue;
            List<Renderer> list;
            if (!groups.TryGetValue(root, out list)) { list = new List<Renderer>(); groups[root] = list; }
            list.Add(r);
        }
        foreach (var kv in groups)
        {
            Ghost g;
            if (!ghosts.TryGetValue(kv.Key, out g)) { g = new Ghost { go = kv.Key }; ghosts[kv.Key] = g; }
            g.rends = kv.Value.ToArray();
        }
    }

    private void Evaluate(Ghost g)
    {
        bool vis = false, has = false;
        Bounds b = default(Bounds);
        if (g.go != null && g.rends != null)
        {
            foreach (Renderer r in g.rends)
            {
                if (r != null && r.enabled && r.gameObject.activeInHierarchy)
                {
                    vis = true;
                    if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
                }
            }
        }
        if (vis) { g.bounds = b; g.seen = true; g.visible = true; lastGhostVisible = Time.time; }
        else if (g.visible) { g.visible = false; g.hiddenAt = Time.time; g.hiddenFrame = Time.frameCount; }
    }

    private void RefreshAll()
    {
        List<GameObject> dead = null;
        foreach (var kv in ghosts)
        {
            Evaluate(kv.Value);
            if (kv.Value.go == null && Time.time - kv.Value.hiddenAt > lateWindow + 1f)
            {
                if (dead == null) dead = new List<GameObject>();
                dead.Add(kv.Key);
            }
        }
        if (dead != null) foreach (GameObject k in dead) ghosts.Remove(k);
    }

    private void OnClick()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Scan(); // pega fantasmas que acabaram de aparecer
        foreach (var g in ghosts.Values) Evaluate(g);

        Ray ray = cam.ScreenPointToRay(GetMousePosition());
        pending.Clear();
        bool hitNow = false;
        bool lateHit = false;

        foreach (Ghost g in ghosts.Values)
        {
            if (!g.seen) continue;
            Bounds b = g.bounds;
            b.Expand(clickRadius * 2f);
            if (!b.IntersectRay(ray)) continue;

            if (g.visible) pending.Add(g);
            else if (Time.frameCount - g.hiddenFrame <= 1) hitNow = true;      // sumiu neste mesmo clique
            else if (Time.time - g.hiddenAt <= lateWindow) lateHit = true;     // já tinha sumido
        }

        pendingActive = false;
        if (hitNow) PlayHit();
        else if (pending.Count > 0) { pendingActive = true; pendingTime = Time.time; }
        else if (lateHit) PlayLate();
        else PlayMiss();
    }

    private void UpdatePending()
    {
        if (!pendingActive) return;
        foreach (Ghost g in pending)
        {
            Evaluate(g);
            if (!g.visible) { pendingActive = false; PlayHit(); return; }
        }
        // O fantasma continuou ali: assume acerto com animação mais lenta
        if (Time.time - pendingTime >= hitWindow) { pendingActive = false; PlayHit(); }
    }

    // ---------------- Música automática ----------------

    private static readonly string[] GameOverKeys = { "gameover", "fimdejogo", "fimdepartida", "derrota", "voceperdeu", "perdeu", "youlose" };
    private static readonly string[] HighKeys = { "highscore", "hiscore", "melhorespontuac", "melhorespontos", "ranking", "leaderboard" };
    private static readonly string[] StartKeys = { "startscreen", "telainicial", "titlescreen", "telatitulo", "mainmenu", "menuprincipal", "menuinicial", "startmenu", "inicio" };

    private void MusicTick()
    {
        if (!autoMusic || manualMusic || Time.unscaledTime < nextMusicTick) return;
        nextMusicTick = Time.unscaledTime + 0.5f;

        MusicType? screen = DetectScreen();
        if (screen.HasValue) { SwitchMusic(screen.Value); return; }

        if (Time.time - lastGhostVisible < 3f) SwitchMusic(MusicType.Gameplay);
        else if (currentMusic == null) SwitchMusic(MusicType.Start);
    }

    private MusicType? DetectScreen()
    {
        bool go = false, hs = false, st = false;
        foreach (RectTransform rt in FindActiveRects())
        {
            if (rt == null) continue;
            string n = Norm(rt.name);
            if (HasAny(n, GameOverKeys)) go = true;
            else if (rt.childCount > 0)
            {
                if (HasAny(n, HighKeys)) hs = true;
                else if (HasAny(n, StartKeys)) st = true;
            }
        }
        if (go) return MusicType.GameOver;
        if (hs) return MusicType.HighScores;
        if (st) return MusicType.Start;

        string scene = Norm(SceneManager.GetActiveScene().name);
        if (HasAny(scene, GameOverKeys)) return MusicType.GameOver;
        if (HasAny(scene, HighKeys)) return MusicType.HighScores;
        if (scene.Contains("menu") || scene.Contains("start") || scene.Contains("title") || scene.Contains("inicio")) return MusicType.Start;
        if (scene.Contains("game") || scene.Contains("jogo") || scene.Contains("level") || scene.Contains("partida")) return MusicType.Gameplay;
        return null;
    }

    // ---------------- utilidades ----------------

    private static string Norm(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s.ToLowerInvariant())
        {
            if (char.IsLetter(c))
            {
                // tira acentos mais comuns
                switch (c)
                {
                    case 'á': case 'à': case 'ã': case 'â': sb.Append('a'); break;
                    case 'é': case 'ê': sb.Append('e'); break;
                    case 'í': sb.Append('i'); break;
                    case 'ó': case 'ô': case 'õ': sb.Append('o'); break;
                    case 'ú': sb.Append('u'); break;
                    case 'ç': sb.Append('c'); break;
                    default: sb.Append(c); break;
                }
            }
        }
        return sb.ToString();
    }

    private static string[] NormAll(string[] keys)
    {
        var list = new List<string>();
        if (keys != null) foreach (string k in keys) { string n = Norm(k); if (n.Length > 0) list.Add(n); }
        return list.ToArray();
    }

    private static bool HasAny(string s, string[] keys)
    {
        if (keys == null) return false;
        foreach (string k in keys) if (k.Length > 0 && s.Contains(k)) return true;
        return false;
    }

    private Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? (Vector3)Mouse.current.position.ReadValue() : Vector3.zero;
#else
        return Input.mousePosition;
#endif
    }

    private bool GetClickDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
