using UnityEngine;

/// <summary>
/// Descarta qualquer item carregado quando o robô interage com a lixeira.
/// </summary>
public class TrashBin : MonoBehaviour, IInteractable
{
    /// <summary>
    /// Solicita ao componente de transporte a remoção do item atual.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        RobotCarry carry = interactor != null ? interactor.GetComponent<RobotCarry>() : null;
        if (carry == null)
            return;

        carry.TryDiscardItem();
    }
}
