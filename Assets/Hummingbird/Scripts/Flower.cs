using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maneja una sola flor con néctar.
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full")]
    public Color fullFlowerColor = new Color(1f, 0f, .3f);

    [Tooltip("The color when the flower is empty")]
    public Color emptyFlowerColor = new Color(.5f, 0f, 1f);

    /// <summary>
    /// El colisionador disparador que representa el néctar.
    /// </summary>
    [HideInInspector]
    public Collider nectarCollider;

    // El colisionador sólido que representa los pétalos de las flores.
    private Collider flowerCollider;

    //El material de la flor
    private Material flowerMaterial;

    /// <summary>
    ///Un vector que apunta directamente desde la flor.
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get
        {
            return nectarCollider.transform.up;
        }
    }

    /// <summary>
    /// La posición central del colisionador de néctar.
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectarCollider.transform.position;
        }
    }

    /// <summary>
    ///  La cantidad de néctar que queda en la flor.
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    ///Si a la flor le queda algo de néctar
    /// </summary>
    public bool HasNectar
    {
        get
        {
            return NectarAmount > 0f;
        }
    }

    /// <summary>
    ///Intenta extraer el néctar de la flor.
    /// </summary>
    /// <param name="amount">La cantidad de néctar a eliminar</param>
    /// <returns>La cantidad real eliminada con éxito</returns>
    public float Feed(float amount)
    {
        // Seguimiento de cuánto néctar se tomó exitosamente (no se puede tomar más del disponible)
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);

        // Resta el néctar
        NectarAmount -= amount;

        if (NectarAmount <= 0)
        {
            // No queda néctar
            NectarAmount = 0;

            // Desactiva los colisionadores de flores y néctar.
            flowerCollider.gameObject.SetActive(false);
            nectarCollider.gameObject.SetActive(false);

            // Cambia el color de la flor para indicar que está vacía
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        // RDevuelve la cantidad de néctar que se tomó
        return nectarTaken;
    }

    /// <summary>
    /// Restablece la flor
    /// </summary>
    public void ResetFlower()
    {
        // Rellena el néctar
        NectarAmount = 1f;

        //Habilitar los colisionadores de flores y néctar.

        flowerCollider.gameObject.SetActive(true);
        nectarCollider.gameObject.SetActive(true);

        // Cambia el color de la flor para indicar que está llena
        flowerMaterial.SetColor("_BaseColor", fullFlowerColor);
    }

    /// <summary>
    /// Llamado cuando la flor se despierta.
    /// </summary>
    private void Awake()
    {
        // Encuentra el renderizador de malla de la flor y obtiene el material principal.
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        // Encuentra colisionadores de flores y néctar
        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}
