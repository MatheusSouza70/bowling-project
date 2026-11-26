using UnityEngine;

public class FollowBall : MonoBehaviour
{
    [SerializeField] Transform alvo;                         // a bola
    [SerializeField] Vector3 offset = new(0f, 1.5f, -3f);    // posição relativa à bola
    [SerializeField] float suavidade = 8f;                   // maior = mais rígida
    [SerializeField] bool olharParaAlvo = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (alvo == null) return;   // bola destruída: câmera fica parada

        Vector3 destino = alvo.position + offset;
        transform.position = Vector3.Lerp(transform.position, destino, suavidade * Time.deltaTime);

        if (olharParaAlvo)
            transform.LookAt(alvo);
    }
}
