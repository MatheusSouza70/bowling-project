
using UnityEngine;

public class PinScript : MonoBehaviour
{
    [SerializeField] float anguloMaximo = 30f;
    [SerializeField] float yMinimo = -0.1f;

    //[SerializeField] float tempoConfirmacao = 0.25f;


    public bool Contado { get; set; }

    //public bool Caiu { get; private set; }

    public bool Caiu =>
    Vector3.Angle(transform.forward, Vector3.up) > anguloMaximo
    || transform.position.y < yMinimo;

    //float tempoInclinado;

    Rigidbody rb;
    Vector3 posInicial;
    Quaternion rotInicial;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        posInicial = transform.position;
        rotInicial = transform.rotation;
    }

    public void Remover()
    {
        gameObject.SetActive(false);
    }

    public void Resetar()
    {
        gameObject.SetActive(true);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(posInicial, rotInicial);
        Contado = false;
        //tempoInclinado = 0f;
    }


    // Eixo que aponta para o topo do pino
    float AnguloInclinacao =>
        Vector3.Angle(transform.forward, Vector3.up);

    void Start()
    {
        Debug.Log(
            $"{name} | ângulo inicial: {AnguloInclinacao:F1}°",
            this
        );
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            //Debug.Log(
            //    $"{name} atingido pela BOLA | ângulo: {AnguloInclinacao:F1}°",
            //    this
            //);
        }
        else if (collision.gameObject.CompareTag("Pin"))
        {
            //Debug.Log(
            //    $"{name} atingido por outro pino | ângulo: {AnguloInclinacao:F1}°",
            //    this
            //);
        }
    }
}
