using UnityEngine;

// Troca os quadros do sprite em loop (animação de passos).
public class AnimacaoSprite : MonoBehaviour
{
    public Sprite[] quadros;
    public float fps = 8f;
    public bool soQuandoMove = false;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private float tempo;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        bool andando = !soQuandoMove || (rb != null && rb.linearVelocity.sqrMagnitude > 0.05f);
        if (!andando)
        {
            sr.sprite = quadros[0];
            return;
        }
        tempo += Time.deltaTime;
        sr.sprite = quadros[(int)(tempo * fps) % quadros.Length];
    }
}
