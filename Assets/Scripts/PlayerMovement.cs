using UnityEngine;
using UnityEngine.InputSystem;

// Movimento do ninja (base: passos 9, 11, 12, 14 e 15 do tutorial)
// + dash com stamina e suporte a teclado e controle de Xbox (Input System).
public class PlayerMovement : MonoBehaviour
{
    // Velocidade pública: ajustável pelo Inspector (passo 11)
    public float speed = 5f;

    [Header("Dash")]
    public float dashSpeed = 13f;
    public float dashDuracao = 0.18f;
    public float dashCusto = 35f;
    public float staminaMax = 100f;
    public float staminaRegen = 22f;
    public AudioClip somDash;
    public AudioClip somAmpulheta;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private SpriteRenderer sr;
    private Vector2 movement;
    private Vector2 direcaoDash = Vector2.right;
    private Vector2 empurrao;
    private float stamina;
    private float dashTimer;
    private float empurraoTimer;

    private InputAction moveAction;
    private InputAction dashAction;

    // Durante o dash o ninja fica invulnerável
    public bool EmDash => dashTimer > 0f;

    void Awake()
    {
        moveAction = new InputAction("Mover", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Keyboard>/leftShift");
        dashAction.AddBinding("<Gamepad>/buttonSouth");   // botão A do Xbox
        dashAction.AddBinding("<Gamepad>/rightTrigger");
    }

    void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
    }

    void OnDestroy()
    {
        moveAction.Dispose();
        dashAction.Dispose();
    }

    // Start roda uma única vez, antes do primeiro Update (passo 10)
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        sr = GetComponent<SpriteRenderer>();
        stamina = staminaMax;
    }

    // Update roda a cada frame: bom lugar para ler a entrada
    void Update()
    {
        if (!GameManager.Jogando)
        {
            movement = Vector2.zero;
            return;
        }

        movement = moveAction.ReadValue<Vector2>();

        // Sem isso a diagonal fica ~41% mais rápida (passo 15).
        // Só normaliza acima de 1 para manter o analógico do controle proporcional.
        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        if (movement.sqrMagnitude > 0.01f)
        {
            direcaoDash = movement.normalized;
            if (Mathf.Abs(movement.x) > 0.1f)
                sr.flipX = movement.x < 0f;
        }

        if (dashAction.WasPressedThisFrame() && !EmDash && stamina >= dashCusto)
        {
            stamina -= dashCusto;
            dashTimer = dashDuracao;
            GameManager.Som(somDash);
        }

        if (!EmDash)
            stamina = Mathf.Min(staminaMax, stamina + staminaRegen * Time.deltaTime);
        GameManager.AtualizarStamina(stamina / staminaMax);
    }

    // FixedUpdate roda no bloco de física: use para mexer no Rigidbody
    void FixedUpdate()
    {
        if (empurraoTimer > 0f)
        {
            empurraoTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = empurrao;
        }
        else if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = direcaoDash * dashSpeed;
        }
        else
        {
            rb.linearVelocity = movement * speed;
        }
    }

    // Chamado pelo PlayerHealth ao levar dano
    public void Empurrar(Vector2 velocidade, float duracao)
    {
        empurrao = velocidade;
        empurraoTimer = duracao;
        dashTimer = 0f;
    }

    // Chamado quando o jogador entra num collider marcado como "Is Trigger" (passo 12)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coletavel"))
        {
            audioSource.Play();
            Destroy(other.gameObject);
            GameManager.Coletar();
        }
        else if (other.CompareTag("Ampulheta"))
        {
            GameManager.Som(somAmpulheta);
            Destroy(other.gameObject);
            GameManager.GanharTempo();
        }
    }
}
