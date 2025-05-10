using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Reticle : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // Asigna tu XRRayInteractor en el Inspector
    public GameObject reticle; // Asigna el objeto del reticle en el Inspector

    private void Start()
    {
        if (reticle != null)
            reticle.SetActive(false); // Ocultar el reticle al inicio
    }

    private void Update()
    {
        if (rayInteractor != null && reticle != null)
        {
            // Verifica si el rayo XR está apuntando a un TeleportationAnchor
            if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                if (hit.collider.GetComponent<TeleportationAnchor>() != null)
                {
                    reticle.SetActive(true); // Activar el reticle
                    reticle.transform.position = hit.point; // Mover el reticle a la posición del rayo
                }
                else
                {
                    reticle.SetActive(false); // Ocultar el reticle si no apunta a un TeleportationAnchor
                }
            }
            else
            {
                reticle.SetActive(false); // Ocultar si no hay colisión con nada
            }
        }
    }
}
