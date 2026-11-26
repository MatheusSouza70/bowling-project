using UnityEngine;
// 1. IMPORTANTE: Adicione essa linha no topo do script
using UnityEngine.InputSystem;

public class CamsScript : MonoBehaviour
{
    [SerializeField] private Camera[] listaDeCameras;
    private int indiceCameraAtual = 0;

    void Start()
    {
        // Procura qual câmera da lista começou ativa na cena
        for (int i = 0; i < listaDeCameras.Length; i++)
        {
            if (listaDeCameras[i] != null && listaDeCameras[i].gameObject.activeSelf)
            {
                indiceCameraAtual = i;
                break; // Encontrou a câmera inicial ativa, pode parar o loop
            }
        }

        AtualizarCameras();
    }

    void Update()
    {
        // 2. Esta é a nova forma de checar se uma tecla foi pressionada
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            AlternarProximaCamera();
        }
    }

    public void AlternarProximaCamera()
    {
        if (listaDeCameras.Length == 0) return;
        indiceCameraAtual = (indiceCameraAtual + 1) % listaDeCameras.Length;
        AtualizarCameras();
    }

    private void AtualizarCameras()
    {
        for (int i = 0; i < listaDeCameras.Length; i++)
        {
            if (listaDeCameras[i] == null) continue;

            bool ativa = i == indiceCameraAtual;
            listaDeCameras[i].enabled = ativa;
            listaDeCameras[i].tag = ativa ? "MainCamera" : "Untagged";

            var listener = listaDeCameras[i].GetComponent<AudioListener>();
            if (listener != null) listener.enabled = ativa;
        }
    }

    public void IrParaCamera(int indice)
    {
        if (indice < 0 || indice >= listaDeCameras.Length) return;
        indiceCameraAtual = indice;
        AtualizarCameras();
    }

}
