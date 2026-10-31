using UnityEngine;

/// <summary>
/// Guarda e exibe um único item carregado pelo robô.
/// </summary>
public class RobotCarry : MonoBehaviour
{
    [SerializeField] private SpriteRenderer carriedItemRenderer;

    private CollectibleItem carriedItem;

    public bool IsCarryingItem => carriedItem != null;
    public ItemData CarriedItemData => carriedItem != null ? carriedItem.Data : null;

    private void Awake()
    {
        if (carriedItemRenderer != null)
        {
            carriedItemRenderer.sprite = null;
            carriedItemRenderer.enabled = false;
        }
    }

    /// <summary>
    /// Aceita o item somente quando o robô está vazio e o visual está configurado.
    /// </summary>
    public bool TryCollect(CollectibleItem item)
    {
        if (IsCarryingItem || item == null || item.Data == null || carriedItemRenderer == null)
            return false;

        if (!item.TryCollect())
            return false;

        carriedItem = item;
        carriedItemRenderer.sprite = item.Data.ItemSprite;
        carriedItemRenderer.enabled = true;
        return true;
    }

    /// <summary>
    /// Processa apenas o tipo esperado e libera o robô para carregar outro item.
    /// </summary>
    public bool TryProcessItem(ItemData expectedItemData)
    {
        if (carriedItem == null || expectedItemData == null || carriedItem.Data != expectedItemData)
            return false;

        return ClearCarriedItem();
    }

    /// <summary>
    /// Descarta o item transportado e libera o robô, sem permitir soltar no chão.
    /// </summary>
    public bool TryDiscardItem()
    {
        return ClearCarriedItem();
    }

    /// <summary>
    /// Remove a referência, o visual e o objeto original do item carregado.
    /// </summary>
    private bool ClearCarriedItem()
    {
        if (carriedItem == null)
            return false;

        CollectibleItem removedItem = carriedItem;
        carriedItem = null;
        if (carriedItemRenderer != null)
        {
            carriedItemRenderer.sprite = null;
            carriedItemRenderer.enabled = false;
        }

        Destroy(removedItem.gameObject);
        return true;
    }
}
