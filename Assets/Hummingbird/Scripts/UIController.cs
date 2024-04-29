using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///Controla una interfaz de usuario muy simple. No hace nada por sí solo.
/// </summary>
public class UIController : MonoBehaviour
{
    [Tooltip("The nectar progress bar for the player")]
    public Slider playerNectarBar;

    [Tooltip("The nectar progress bar for the opponent")]
    public Slider opponentNectarBar;

    [Tooltip("The timer text")]
    public TextMeshProUGUI timerText;

    [Tooltip("The banner text")]
    public TextMeshProUGUI bannerText;

    [Tooltip("The button")]
    public Button button;

    [Tooltip("The button text")]
    public TextMeshProUGUI buttonText;

    /// <summary>
    /// Delega para hacer clic en un botón
    /// </summary>
    public delegate void ButtonClick();

    /// <summary>
    ///Llamado cuando se hace clic en el botón
    /// </summary>
    public ButtonClick OnButtonClicked;

    /// <summary>
    /// Responde a los clics en los botones
    /// </summary>
    public void ButtonClicked()
    {
        if (OnButtonClicked != null) OnButtonClicked();
    }

    /// <summary>
    /// Muestra el botón
    /// </summary>
    /// <param name="text">La cadena de texto en el botón</param>
    public void ShowButton(string text)
    {
        buttonText.text = text;
        button.gameObject.SetActive(true);
    }

    /// <summary>
    /// Oculta el botón
    /// </summary>
    public void HideButton()
    {
        button.gameObject.SetActive(false);
    }

    /// <summary>
    /// Muestra el texto del banner
    /// </summary>
    /// <param name="text">La cadena de texto a mostrar</param>
    public void ShowBanner(string text)
    {
        bannerText.text = text;
        bannerText.gameObject.SetActive(true);
    }

    /// <summary>
    ///  Oculta el texto del bannerv
    /// </summary>
    public void HideBanner()
    {
        bannerText.gameObject.SetActive(false);
    }

    /// <summary>
    ///  Establece el temporizador, si el tiempo restante es negativo, oculta el texto
    /// </summary>
    /// <param name="timeRemaining">El tiempo restante en </param>
    public void SetTimer(float timeRemaining)
    {
        if (timeRemaining > 0f)
            timerText.text = timeRemaining.ToString("00");
        else
            timerText.text = "";
    }

    /// <summary>
    ///  Establece la cantidad de néctar del jugador.
    /// </summary>
    /// <param name="nectarAmount">Una cantidad entre 0 y 1</param>
    public void SetPlayerNectar(float nectarAmount)
    {
        playerNectarBar.value = nectarAmount;
    }

    /// <summary>
    /// Establece la cantidad de néctar del oponente.
    /// </summary>
    /// <param name="nectarAmount">Una cantidad entre 0 y 1</param>
    public void SetOpponentNectar(float nectarAmount)
    {
        opponentNectarBar.value = nectarAmount;
    }
}
