using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la lógica del juego y controla la interfaz de usuario.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Tooltip("Game ends when an agent collects this much nectar")]
    public float maxNectar = 8f;

    [Tooltip("Game ends after this many seconds have elapsed")]
    public float timerAmount = 60f;

    [Tooltip("The UI Controller")]
    public UIController uiController;

    [Tooltip("The player hummingbird")]
    public HummingbirdAgent player;

    [Tooltip("The ML-Agent opponent hummingbird")]
    public HummingbirdAgent opponent;

    [Tooltip("The flower area")]
    public FlowerArea flowerArea;

    [Tooltip("The main camera for the scene")]
    public Camera mainCamera;

    // Cuando comenzó el cronómetro del juego
    private float gameTimerStartTime;

    /// <summary>
    /// Todos los estados posibles del juego
    /// </summary>
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Gameover
    }

    /// <summary>
    /// El estado actual del juego.
    /// </summary>
    public GameState State { get; private set; } = GameState.Default;

    /// <summary>
    ///Obtiene el tiempo restante del juego
    /// </summary>
    public float TimeRemaining
    {
        get
        {
            if (State == GameState.Playing)
            {
                float timeRemaining = timerAmount - (Time.time - gameTimerStartTime);
                return Mathf.Max(0f, timeRemaining);
            }
            else
            {
                return 0f;
            }
        }
    }

    /// <summary>
    ///Maneja el clic de un botón en diferentes estados
    /// </summary>
    public void ButtonClicked()
    {
        if (State == GameState.Gameover)
        {
            // En el estado Gameover, al hacer clic en el botón se debe ir al menú principal
            MainMenu();
        }
        else if (State == GameState.MainMenu)
        {
            // En el estado MainMenu, al hacer clic en el botón se debe iniciar el juego.
            StartCoroutine(StartGame());
        }
        else
        {
            Debug.LogWarning("Button clicked in unexpected state: " + State.ToString());
        }
    }

    /// <summary>
    /// Llamado cuando comienza el juego.
    /// </summary>
    private void Start()
    {
        //Suscríbete a eventos de clic en botones desde la interfaz de usuario
        uiController.OnButtonClicked += ButtonClicked;

        //Inicia el menú principal
        MainMenu();
    }

    /// <summary>
    /// Llamado a destruir
    /// </summary>
    private void OnDestroy()
    {
        // Cancelar la suscripción a eventos de clic en botones desde la interfaz de usuario
        uiController.OnButtonClicked -= ButtonClicked;
    }

    /// <summary>
    /// Muestra el menú principal
    /// </summary>
    private void MainMenu()
    {
        // Establece el estado en "menú principal"
        State = GameState.MainMenu;

        // Actualiza la interfaz de usuario

        uiController.ShowBanner("");
        uiController.ShowButton("Start");

        // Usa la cámara principal, desactiva las cámaras de los agentes
        mainCamera.gameObject.SetActive(true);
        player.agentCamera.gameObject.SetActive(false);
        opponent.agentCamera.gameObject.SetActive(false); // Nunca vuelvas a encender esto

        // Restablecer las flores
        flowerArea.ResetFlowers();

        // Restablecer los agentes

        player.OnEpisodeBegin();
        opponent.OnEpisodeBegin();

        //Congelar a los agentes
        player.FreezeAgent();
        opponent.FreezeAgent();
    }

    /// <summary>
    /// Comienza el juego con una cuenta atrás.
    /// </summary>
    /// <returns>IEnumerator</returns>
    private IEnumerator StartGame()
    {
        // Establece el estado en "preparando"
        State = GameState.Preparing;

        // Actualiza la UI (ocultala)
        uiController.ShowBanner("");
        uiController.HideButton();

        // Usa la cámara del reproductor, desactiva la cámara principal
        mainCamera.gameObject.SetActive(false);
        player.agentCamera.gameObject.SetActive(true);

        // Mostrar cuenta regresiva
        uiController.ShowBanner("3");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("2");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("1");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("Go!");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("");

        // Establece el estado en "reproduciendo"
        State = GameState.Playing;

        //  Inicia el cronómetro del juego
        gameTimerStartTime = Time.time;

        //Descongelar los agentes
        player.UnfreezeAgent();
        opponent.UnfreezeAgent();
    }

    /// <summary>
    /// Termina el juego
    /// </summary>
    private void EndGame()
    {
        // Establece el estado del juego en "juego terminado"
        State = GameState.Gameover;

        // Congelar a los agentes
        player.FreezeAgent();
        opponent.FreezeAgent();

        // Actualizar el texto del banner dependiendo de ganar/perder
        if (player.NectarObtained >= opponent.NectarObtained )
        {
            uiController.ShowBanner("You win!");
        }
        else
        {
            uiController.ShowBanner("ML-Agent wins!");
        }

        //Actualizar texto del botón
        uiController.ShowButton("Main Menu");
    }

    /// <summary>
    /// Llama a cada cuadro
    /// </summary>
    private void Update()
    {
        if (State == GameState.Playing)
        {
            // Verifica si se acabó el tiempo o si alguno de los agentes obtuvo la cantidad máxima de néctar
            if (TimeRemaining <= 0f ||
                player.NectarObtained >= maxNectar ||
                opponent.NectarObtained >= maxNectar)
            {
                EndGame();
            }

            // Actualiza el temporizador y las barras de progreso de néctar
            uiController.SetTimer(TimeRemaining);
            uiController.SetPlayerNectar(player.NectarObtained / maxNectar);
            uiController.SetOpponentNectar(opponent.NectarObtained / maxNectar);
        }
        else if (State == GameState.Preparing || State == GameState.Gameover)
        {
            // Actualiza el temporizador
            uiController.SetTimer(TimeRemaining);
        }
        else
        {
            // ocultar el temporizador
            uiController.SetTimer(-1f);

            // Actualiza las barras de progreso
            uiController.SetPlayerNectar(0f);
            uiController.SetOpponentNectar(0f);
        }

    }
}
