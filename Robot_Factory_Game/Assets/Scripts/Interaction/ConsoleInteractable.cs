using UnityEngine;

/// <summary>
/// Objeto provisório que confirma a interação escrevendo uma mensagem no Console.
/// </summary>
public class ConsoleInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string message = "Interação de teste realizada.";

    /// <summary>
    /// Registra no Console que o robô interagiu com este objeto.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        Debug.Log(message, this);
    }
}
