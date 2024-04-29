using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// Gestiona una colección de plantas florales y flores adjuntas.
/// </summary>
public class FlowerArea : MonoBehaviour
{
    //El diámetro del área donde se pueden colocar el agente y las flores.
    // usado para observar la distancia relativa entre el agente y la flor
    public const float AreaDiameter = 20f;

    // La lista de todas las plantas de flores en esta área de flores (las plantas de flores tienen varias flores)
    private List<GameObject> flowerPlants;

    // Un diccionario de búsqueda para buscar una flor en un colisionador de néctar
    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    /// <summary>
    /// La lista de todas las flores en el área de flores.
    /// </summary>
    public List<Flower> Flowers { get; private set; }

    /// <summary>
    /// Restablecer las flores y plantas de flores.
    /// </summary>
    public void ResetFlowers()
    {
        // Gira cada planta floral alrededor del eje Y y sutilmente alrededor de X y Z
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);
            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }

        //Restablecer cada flor
        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    /// <summary>
    ///Obtiene la <see cref="Flower"/> a la que pertenece un colisionador de néctar
    /// </summary>
    /// <param name="collider">El colisionador de néctar</param>
    /// <returns>La flor correspondiente</returns>
    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    /// <summary>
    ///Llamado cuando el área se despierta
    /// </summary>
    private void Awake()
    {
        //Inicializar variables
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    /// <summary>
    /// Llamado cuando comienza el juego.
    /// </summary>
    private void Start()
    {
        // Encuentra todas las flores que son hijas de este GameObject/Transform
        FindChildFlowers(transform);
    }

    /// <summary>
    ///Encuentra recursivamente todas las flores y plantas de flores que son hijas de una transformación principal.
    /// </summary>
    /// <param name="parent">El padre comprobueba.</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                // Encontro una planta de flores, agréguela a la lista de plantas de flores.
                flowerPlants.Add(child.gameObject);

                // Busca flores dentro de la planta floral.
                FindChildFlowers(child);
            }
            else
            {
                // No es una planta con flores, busca un componente floral
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    // Encontré una flor, agrégala a la lista de Flores
                    Flowers.Add(flower);

                    // Agrega el colisionador de néctar al diccionario de búsqueda
                    nectarFlowerDictionary.Add(flower.nectarCollider, flower);

                    // Nota: no hay flores que sean hijas de otras flores
                }
                else
                {
                    // Componente de flor no encontrado, así que revisa los niños

                    FindChildFlowers(child);
                }
            }
        }
    }
}
