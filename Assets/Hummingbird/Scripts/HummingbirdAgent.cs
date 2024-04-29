using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
///  Un agente de aprendizaje automático de colibrí
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving")]
    public float moveForce = 2f;

    [Tooltip("Speed to pitch up or down")]
    public float pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis")]
    public float yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak")]
    public Transform beakTip;

    [Tooltip("The agent's camera")]
    public Camera agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode")]
    public bool trainingMode;

    // El cuerpo rígido del agente
    new private Rigidbody rigidbody;

    //  El área de la flor en la que se encuentra el agente.
    private FlowerArea flowerArea;

    // La flor más cercana al agente.
    private Flower nearestFlower;

    // Permite cambios de tono más suaves
    private float smoothPitchChange = 0f;

    //  Permite cambios de guiñada más suaves
    private float smoothYawChange = 0f;

    // Ángulo máximo que el pájaro puede lanzar hacia arriba o hacia abajo
    private const float MaxPitchAngle = 80f;

    // Distancia máxima desde la punta del pico para aceptar la colisión del néctar
    private const float BeakTipRadius = 0.008f;

    //Si el agente está congelado (intencionalmente no vuela)
    private bool frozen = false;

    /// <summary>
    /// La cantidad de néctar que el agente ha obtenido en este episodio.
    /// </summary>
    public float NectarObtained { get; private set; }

    /// <summary>
    ///  Inicializa el agente
    /// </summary>
    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        //Si no es modo de entrenamiento, no hay paso máximo, juega para siempre
        
        if (!trainingMode) MaxStep = 0;
    }

    /// <summary>
    /// Reiniciar el agente cuando comienza un episodio
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (trainingMode)
        {
            // Solo se reinician las flores en el entrenamiento cuando hay un agente por área

            flowerArea.ResetFlowers();
        }

        //Restablecer el néctar obtenido
        NectarObtained = 0f;

        // Poner a cero las velocidades para que el movimiento se detenga antes de que comience un nuevo episodio
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // Por defecto aparece frente a una flor
        bool inFrontOfFlower = true;
        if (trainingMode)
        {
            // Aparece frente a la flor el 50% del tiempo durante el entrenamiento

            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        // Move the agent to a new random position
        MoveToSafeRandomPosition(inFrontOfFlower);

        // Recalculate the nearest flower now that the agent has moved
        UpdateNearestFlower();
    }

    /// <summary>
    /// Se llama cuando se recibe una acción de la entrada del jugador o de la red neuronal
    /// 
    /// vectorAction[i] represents:
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: move vector y (+1 = up, -1 = down)
    /// Index 2: move vector z (+1 = forward, -1 = backward)
    /// Index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="vectorAction">Las acciones a realizar</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        //No tomes medidas si está congelado
        if (frozen) return;

        // Calcular vector de movimiento
        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);

        // Agrega fuerza en la dirección del vector de movimiento
        rigidbody.AddForce(move * moveForce);

        // Obtiene la rotación actual
        Vector3 rotationVector = transform.rotation.eulerAngles;

        // Calcular la rotación de cabeceo y guiñada
        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        //Calcular cambios de rotación suaves
        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        // Calcular nuevo cabeceo y guiñada basándose en valores suavizados
        // Sujeta el paso para evitar que se voltee
        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        //  Aplicar la nueva rotación
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Recopilar observaciones de vectores del medio ambiente.
    /// </summary>
    /// <param name="sensor">el vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //  Si la flor más cercana es nula, observa una matriz vacía y regresa temprano
        if (nearestFlower == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        // Observa la rotación local del agente (4 observaciones)
        sensor.AddObservation(transform.localRotation.normalized);

        // Obtener un vector desde la punta del pico hasta la flor más cercana
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;

        //Observa un vector normalizado que apunta a la flor más cercana (3 observaciones)
        sensor.AddObservation(toFlower.normalized);

        //Observa un producto escalar que indica si la punta del pico está delante de la flor (1 observación)
        // (+1 significa que la punta del pico está directamente delante de la flor, -1 significa directamente detrás)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observa un producto escalar que indica si el pico apunta hacia la flor (1 observación)
        // (+1 significa que el pico apunta directamente a la flor, -1 significa directamente hacia afuera)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observa la distancia relativa desde la punta del pico hasta la flor (1 observación)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        // 10 observaciones en total
    }

    /// <summary>
    /// Cuando el tipo de comportamiento se establece en "Sólo heurístico" en los parámetros de comportamiento del agente,
    /// se llamará esta función. Sus valores de retorno se introducirán en
    /// <see cref="OnActionReceived(float[])"/> en lugar de utilizar la red neuronal
    /// </summary>
    /// <param name="actionsOut">Y genera la matriz de acciones</param>
    public override void Heuristic(float[] actionsOut)
    {
        // Crea marcadores de posición para todos los movimientos/giros
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // Convierte las entradas del teclado en movimiento y giro
        // Todos los valores deben estar entre -1 y +1

        // Hacia adelante hacia atrás
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // Izquierda derecha
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        // Arriba abajo
        if (Input.GetKey(KeyCode.Q)) up = transform.up;
        else if (Input.GetKey(KeyCode.E)) up = -transform.up;

        // inclinación arriba/abajo
        //float h = pitch * Input.GetAxis("Mouse X");
        //float v = yaw * Input.GetAxis("Mouse Y");
        //transform.Rotate(v, h, 0);
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        // Voltee izquierda derecha
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        //Combina los vectores de movimiento y normaliza
        Vector3 combined = (forward + left + up).normalized;

        // Agrega los 3 valores de movimiento, cabeceo y guiñada a la matriz 
        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }

    /// <summary>
    ///  Evitar que el agente se mueva y realice acciones.
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = true;
        rigidbody.Sleep();
    }

    /// <summary>
    ///Reanudar el movimiento y las acciones del agente.
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");
        frozen = false;
        rigidbody.WakeUp();
    }

    /// <summary>
    /// Mueve el agente a una posición aleatoria segura (es decir, no choca con nada)
    /// Si está frente a una flor, apunte también el pico hacia la flor.
    /// </summary>
    /// <param name="inFrontOfFlower">Si elegir un lugar frente a una flor</param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100; // Prevenir un bucle infinito
        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        // Bucle hasta encontrar una posición segura o nos quedamos sin intentos
        while (!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                // Elige una flor al azar
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                // Colóquelo de 10 a 20 cm delante de la flor
                
                float distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);
                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                //Apunta el pico a la flor (la cabeza del pájaro es el centro de la transformación)
                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                // Elige una altura aleatoria desde el suelo
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                // Elige un radio aleatorio desde el centro del área.
                float radius = UnityEngine.Random.Range(2f, 7f);

                // Elige una dirección aleatoria rotada alrededor del eje y
                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180f, 180f), 0f);

                // combina altura, radio y dirección para elegir una posición potencial
                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                //Elige y establece el tono inicial y la guiñada aleatorios
                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            // Comprueba si el agente chocará con algo
            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            // Se ha encontrado una posición segura si no hay colisionadores superpuestos
            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        //Establece la posición y la rotación.

        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
    }

    /// <summary>
    /// Actualiza la flor más cercana al agente.
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar)
            {
                // No hay ninguna flor más cercana actualmente y esta flor tiene néctar, así que configúrala en esta flor
                nearestFlower = flower;
            }
            else if (flower.HasNectar)
            {
                // Calcula la distancia a esta flor y la distancia a la flor más cercana actual
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                //Si la flor más cercana actual está vacía O esta flor está más cerca, actualiza la flor más cercana
                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }

    /// <summary>
    /// Se llama cuando el colisionador del agente ingresa a un colisionador desencadenante
    /// </summary>
    /// <param name="other">El colisionador activador</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Se llama cuando el colisionador del agente permanece en un colisionador activador.
    /// </summary>
    /// <param name="other">El colisionador activador</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Maneja cuando el colisionador del agente entra o permanece en un colisionador activador
    /// </summary>
    /// <param name="collider">El colisionador activador</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        // Comprobar si el agente está chocando con el néctar
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            // Comprobar si el punto de colisión más cercano está cerca de la punta del pico
            // Nota: una colisión con cualquier cosa que no sea la punta del pico no debe contar
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius)
            {
                // Busca la flor de este colisionador de néctar
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                // Intenta tomar néctar .01
                // Nota: esto es por intervalo de tiempo fijo, lo que significa que ocurre cada 0,02 segundos, o 50 veces por segundo
                float nectarReceived = flower.Feed(.01f);

                // Realizar un seguimiento del néctar obtenido
                NectarObtained += nectarReceived;

                if (trainingMode)
                {
                    // Calcular recompensa por conseguir néctar
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                // Si la flor está vacía, actualiza la flor más cercana
                if (!flower.HasNectar)
                {
                    UpdateNearestFlower();
                }
            }
        }
    }

    /// <summary>
    /// Llamado cuando el agente choca con algo sólido
    /// </summary>
    /// <param name="collision">La información de colisión</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary"))
        {
            // Choca con el límite del área, otorga una recompensa negativa
            AddReward(-.5f);
        }
    }

    /// <summary>
    /// Llama a cada cuadro
    /// </summary>
    private void Update()
    {
        // Dibuja una línea desde la punta del pico hasta la flor más cercana.
        if (nearestFlower != null)
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
    }

    /// <summary>
    /// Llamado cada 0,02 segundos
    /// </summary>
    private void FixedUpdate()
    {
        // Evita el escenario en el que el oponente roba el néctar de la flor más cercana y no lo actualiza.
        if (nearestFlower != null && !nearestFlower.HasNectar)
            UpdateNearestFlower();
    }
}
