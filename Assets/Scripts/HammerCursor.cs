using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Faz a marreta 3D substituir o cursor do mouse.
/// - Esconde o cursor do sistema
/// - A CABEÇA da marreta (Hit Point) fica exatamente embaixo do cursor
/// - Anima a batida ao clicar e detecta fantasmas acertados com "ajuda de mira"
///
/// Como usar:
/// 1. Crie um GameObject vazio (ex: "HammerRig") e coloque este script nele.
/// 2. Arraste o modelo da marreta para dentro do HammerRig (filho "hammerModel").
/// 3. Crie um objeto vazio filho da marreta, posicionado no centro da cabeça (o lado que bate),
///    e arraste para o campo "Hit Point".
/// 4. Configure câmera, superfície da mesa e layer dos fantasmas.
/// </summary>
public class HammerCursor : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Camera gameCamera;           // se vazio usa Camera.main
    [SerializeField] private Transform hammerModel;       // filho que gira na batida
    [Tooltip("Objeto vazio no centro da CABEÇA da marreta. Esse ponto fica exatamente sob o cursor.")]
    [SerializeField] private Transform hitPoint;

    [Header("Posição")]
    [Tooltip("Altura (Y no mundo) do topo da mesa. Ignorado se Table Surface estiver preenchido.")]
    [SerializeField] private float tableY = 1.0f;
    [Tooltip("Opcional: superfície da mesa (funciona com mesa inclinada). O eixo Y (verde) deve apontar para fora da mesa.")]
    [SerializeField] private Transform tableSurface;
    [Tooltip("Distância da cabeça da marreta acima da mesa quando parada")]
    [SerializeField] private float hoverHeight = 0.3f;
    [SerializeField] private float followSpeed = 40f;     // maior = mais colada ao mouse
    [Tooltip("Limita a área (só vale sem Table Surface)")]
    [SerializeField] private bool clampToArea = false;
    [SerializeField] private Vector2 areaCenter = Vector2.zero; // X,Z
    [SerializeField] private Vector2 areaSize = new Vector2(3f, 3f);

    [Header("Batida")]
    [SerializeField] private float swingAngle = 70f;      // ângulo da batida
    [SerializeField] private float swingDuration = 0.15f; // tempo total ida+volta
    [Range(0.1f, 0.6f)]
    [SerializeField] private float hitMoment = 0.3f;      // quando (0-1) o acerto é checado. Menor = mais responsivo
    [SerializeField] private Vector3 swingAxis = Vector3.right; // eixo local da rotação

    [Header("Acerto (facilitar)")]
    [SerializeField] private LayerMask ghostLayer;        // layer dos fantasmas
    [Tooltip("Espessura do 'raio' disparado da câmera pelo cursor. Maior = mais fácil acertar")]
    [SerializeField] private float aimAssistRadius = 0.35f;
    [Tooltip("Raio extra em volta do ponto na mesa (pega fantasmas saindo do buraco)")]
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private float maxAimDistance = 50f;
    [SerializeField] private bool lockCursor = false;     // true = trava o cursor no centro

    [Header("Colisão com paredes")]
    [Tooltip("Layer das paredes/laterais que a marreta não pode atravessar")]
    [SerializeField] private LayerMask wallLayer;
    [Tooltip("Tamanho da 'bolha' em volta da cabeça da marreta usada na colisão")]
    [SerializeField] private float hammerRadius = 0.2f;
    [Tooltip("Folga mínima entre a marreta e a parede")]
    [SerializeField] private float wallSkin = 0.01f;

    /// <summary>Disparado quando a marreta acerta um fantasma (use para pontuar, som, partículas).</summary>
    public event System.Action<Collider> OnGhostHit;
    public event System.Action OnMiss;

    private Plane tablePlane;
    private Vector3 targetPos;
    private Vector3 hitLocalOffset;   // posição da cabeça relativa ao rig
    private Quaternion restRotation;
    private bool swinging;
    private float swingTime;
    private bool hitChecked;

    private readonly List<Collider> candidates = new List<Collider>();

    private void Awake()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (hammerModel == null) hammerModel = transform;

        restRotation = hammerModel.localRotation;

        // Guarda onde a cabeça está em relação ao rig (para alinhar com o cursor)
        hitLocalOffset = hitPoint != null ? transform.InverseTransformPoint(hitPoint.position) : Vector3.zero;

        targetPos = transform.position;

        Cursor.visible = false;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.Confined;

        // A marreta não pode bloquear raycasts do jogo
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        FollowMouse();

        if (GetClickDown() && !swinging)
            StartSwing();

        UpdateSwing();
    }

    // ---------------- Movimento ----------------

    private Vector3 SurfaceNormal => tableSurface != null ? tableSurface.up : Vector3.up;
    private Vector3 SurfaceOrigin => tableSurface != null ? tableSurface.position : new Vector3(0f, tableY, 0f);

    private void FollowMouse()
    {
        Ray ray = gameCamera.ScreenPointToRay(GetMousePosition());
        Vector3 normal = SurfaceNormal;

        // O plano é elevado até a altura da cabeça da marreta. Assim, o ponto onde o raio do
        // cursor cruza o plano é exatamente onde a cabeça aparece na tela (sem desalinhamento).
        tablePlane = new Plane(normal, SurfaceOrigin + normal * hoverHeight);

        if (tablePlane.Raycast(ray, out float enter))
        {
            Vector3 p = ray.GetPoint(enter);

            if (clampToArea && tableSurface == null)
            {
                p.x = Mathf.Clamp(p.x, areaCenter.x - areaSize.x * 0.5f, areaCenter.x + areaSize.x * 0.5f);
                p.z = Mathf.Clamp(p.z, areaCenter.y - areaSize.y * 0.5f, areaCenter.y + areaSize.y * 0.5f);
            }

            // Posiciona o rig de modo que a CABEÇA fique em 'p'
            targetPos = p - transform.TransformVector(hitLocalOffset);
        }

        Vector3 desiredRig = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));

        // Colisão: a CABEÇA da marreta não pode atravessar as paredes
        if (wallLayer.value != 0)
        {
            Vector3 offset = transform.TransformVector(hitLocalOffset);
            Vector3 headFrom = transform.position + offset;
            Vector3 headTo = desiredRig + offset;
            desiredRig = ResolveWalls(headFrom, headTo) - offset;
        }

        transform.position = desiredRig;
    }

    /// <summary>
    /// Move a cabeça de 'from' até 'to' parando nas paredes e deslizando ao longo delas.
    /// </summary>
    private Vector3 ResolveWalls(Vector3 from, Vector3 to)
    {
        Vector3 tableNormal = SurfaceNormal;
        Vector3 pos = from;
        Vector3 remaining = to - from;

        for (int i = 0; i < 3; i++)
        {
            float dist = remaining.magnitude;
            if (dist < 0.0001f) break;

            Vector3 dir = remaining / dist;

            if (Physics.SphereCast(pos, hammerRadius, dir, out RaycastHit hit, dist + wallSkin,
                                   wallLayer, QueryTriggerInteraction.Ignore))
            {
                float travel = Mathf.Max(0f, hit.distance - wallSkin);
                pos += dir * travel;

                // Sobra de movimento desliza ao longo da parede (e continua na altura da mesa)
                Vector3 left = remaining - dir * travel;
                left = Vector3.ProjectOnPlane(left, hit.normal);
                remaining = Vector3.ProjectOnPlane(left, tableNormal);
            }
            else
            {
                pos += remaining;
                remaining = Vector3.zero;
                break;
            }
        }

        return PushOutOfWalls(pos);
    }

    /// <summary>Se a cabeça acabou dentro de uma parede (ex: parede começou em cima dela), empurra para fora.</summary>
    private Vector3 PushOutOfWalls(Vector3 pos)
    {
        Collider[] cols = Physics.OverlapSphere(pos, hammerRadius, wallLayer, QueryTriggerInteraction.Ignore);
        foreach (var col in cols)
        {
            // ClosestPoint não funciona em MeshCollider côncavo; use Box/Sphere/Capsule nas paredes
            if (col is MeshCollider mc && !mc.convex) continue;

            Vector3 closest = col.ClosestPoint(pos);
            Vector3 diff = pos - closest;
            float d = diff.magnitude;
            if (d < 0.0001f) continue;
            if (d < hammerRadius)
                pos += diff / d * (hammerRadius - d);
        }
        return pos;
    }

    // ---------------- Batida ----------------

    private void StartSwing()
    {
        swinging = true;
        hitChecked = false;
        swingTime = 0f;
    }

    private void UpdateSwing()
    {
        if (!swinging) return;

        swingTime += Time.deltaTime;
        float t = Mathf.Clamp01(swingTime / swingDuration);

        // Desce até o impacto (t=0.5) e volta
        float curve = t < 0.5f ? t * 2f : (1f - t) * 2f;
        float angle = Mathf.SmoothStep(0f, swingAngle, curve);
        hammerModel.localRotation = restRotation * Quaternion.AngleAxis(angle, swingAxis);

        if (!hitChecked && t >= hitMoment)
        {
            hitChecked = true;
            CheckHit();
        }

        if (t >= 1f)
        {
            swinging = false;
            hammerModel.localRotation = restRotation;
        }
    }

    // ---------------- Acerto com ajuda de mira ----------------

    private void CheckHit()
    {
        Ray ray = gameCamera.ScreenPointToRay(GetMousePosition());
        candidates.Clear();

        // 1) "Raio grosso" da câmera passando pelo cursor
        RaycastHit[] cast = Physics.SphereCastAll(ray, aimAssistRadius, maxAimDistance, ghostLayer, QueryTriggerInteraction.Collide);
        foreach (var h in cast)
            if (h.collider != null) candidates.Add(h.collider);

        // 2) Esfera na mesa, na posição da cabeça (pega fantasmas saindo do buraco)
        Vector3 head = hitPoint != null ? hitPoint.position : transform.position;
        Vector3 onTable = head - SurfaceNormal * hoverHeight;
        foreach (var c in Physics.OverlapSphere(onTable, hitRadius, ghostLayer, QueryTriggerInteraction.Collide))
            if (!candidates.Contains(c)) candidates.Add(c);

        // Escolhe o fantasma mais próximo do cursor (o raio do mouse)
        Collider best = null;
        float bestDist = float.MaxValue;
        foreach (var c in candidates)
        {
            float d = DistancePointToRay(ray, c.bounds.center);
            if (d < bestDist) { bestDist = d; best = c; }
        }

        if (best != null)
        {
            // Chame aqui a lógica do seu fantasma. Exemplo:
            // best.GetComponentInParent<Ghost>()?.TakeHit();
            Debug.Log($"Acertou: {best.name}");
            OnGhostHit?.Invoke(best);
        }
        else
        {
            OnMiss?.Invoke();
        }
    }

    private static float DistancePointToRay(Ray ray, Vector3 point)
    {
        return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
    }

    // ---------------- Input (suporta os dois sistemas) ----------------

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

    private void OnDrawGizmosSelected()
    {
        Vector3 head = hitPoint != null ? hitPoint.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(head, 0.03f);                       // onde a cabeça fica
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(head - SurfaceNormal * hoverHeight, hitRadius); // área de acerto na mesa
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(head, hammerRadius);                            // bolha de colisão com paredes

        if (clampToArea && tableSurface == null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(areaCenter.x, tableY, areaCenter.y),
                                new Vector3(areaSize.x, 0.01f, areaSize.y));
        }
    }
}
