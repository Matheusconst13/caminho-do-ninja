using UnityEngine;

// Gira e/ou flutua um objeto (shurikens e ampulhetas).
public class Girar : MonoBehaviour
{
    public float grausPorSegundo = 180f;
    public float flutuar = 0f;

    private Vector3 inicio;

    void Start()
    {
        inicio = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, grausPorSegundo * Time.deltaTime);
        if (flutuar > 0f)
            transform.position = inicio + Vector3.up * Mathf.Sin(Time.time * 3f) * flutuar;
    }
}
