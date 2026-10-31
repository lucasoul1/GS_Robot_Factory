using System;
using UnityEngine;

/// <summary>
/// Recebe somente o ItemData configurado para esta estação.
/// </summary>
public class ProcessingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData acceptedItemData;

    /// <summary>
    /// Notifica os sistemas interessados somente depois de processar o item aceito.
    /// </summary>
    public event Action<ItemData> ItemProcessed;

    /// <summary>
    /// Confere o item carregado e só o remove quando corresponde ao tipo aceito.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        RobotCarry carry = interactor != null ? interactor.GetComponent<RobotCarry>() : null;
        if (carry == null || !carry.IsCarryingItem)
        {
            Debug.Log("No Item Carried.", this);
            return;
        }

        if (acceptedItemData == null || carry.CarriedItemData != acceptedItemData)
        {
            Debug.Log("Incorrect Item.", this);
            return;
        }

        if (carry.TryProcessItem(acceptedItemData))
        {
            Debug.Log("Item Processed.", this);
            ItemProcessed?.Invoke(acceptedItemData);
            MachineAlert machineAlert = GetComponent<MachineAlert>();
            if (machineAlert != null)
                machineAlert.OnItemProcessed();
        }
    }
}
