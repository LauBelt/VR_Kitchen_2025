using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class ObjetoArrastrable : MonoBehaviour
{
    public string palabraObjeto; // Palabra asociada al objeto

    void OnTriggerEnter(Collider other)
    {
        XRGrabInteractable objetoInteractable = other.GetComponent<XRGrabInteractable>();
        if (objetoInteractable != null && objetoInteractable.gameObject.activeSelf)
        {
            string palabraObjetoDelObjeto = other.GetComponent<ObjetoArrastrable>()?.palabraObjeto; // Intenta obtener la palabra del script del objeto arrastrado
            if (!string.IsNullOrEmpty(palabraObjetoDelObjeto))// Verifica que la palabra no sea nula
            {
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null)
                {
                    // Llama funcion de GameManager para comprobar si el objeto es correcto
                    gameManager.ComprobarObjetoEnCajaDesdeObjeto(palabraObjeto, other.gameObject, palabraObjetoDelObjeto);
                }
            }
        }
    }
}


