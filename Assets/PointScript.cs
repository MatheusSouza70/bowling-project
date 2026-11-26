using UnityEngine;

public class PointScript : MonoBehaviour
{
    [SerializeField] PinScript[] pinos;

    public int Total { get; private set; }
    public int TotalJogador { get; private set; }
    public int TotalIA { get; private set; }
    public int UltimaRodada { get; private set; }

    public void Resetar()
    {
        Total = 0;
        TotalJogador = 0;
        TotalIA = 0;
        UltimaRodada = 0;
        foreach (var pino in pinos)
            if (pino != null) pino.Resetar();
    }

    public void RestaurarPinos()
    {
        foreach (var pino in pinos)
            if (pino != null) pino.Resetar();
    }

    public int VerificarERemover(bool turnoIA = false)
    {
        int derrubados = 0;

        foreach (var pino in pinos)
        {
            if (pino == null || !pino.gameObject.activeSelf) continue;

            if (pino.Caiu)
            {
                derrubados++;
                pino.Contado = true;
                pino.Remover();
            }
            else
            {
                var rb = pino.GetComponent<Rigidbody>();
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        UltimaRodada = derrubados;

        if (turnoIA)
            TotalIA += derrubados;
        else
            TotalJogador += derrubados;

        Total = TotalJogador;
        return derrubados;
    }
}