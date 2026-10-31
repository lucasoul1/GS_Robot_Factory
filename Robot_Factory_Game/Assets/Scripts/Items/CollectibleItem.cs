using UnityEngine;

/// <summary>
/// Representa um item do cenário que pode ser coletado pelo robô.
/// </summary>
public class CollectibleItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData itemData;

    private SpriteRenderer itemRenderer;
    private Collider2D itemCollider;
    private bool isCollected;

    public ItemData Data => itemData;
    public bool IsCollected => isCollected;

    private void Awake()
    {
        itemRenderer = GetComponent<SpriteRenderer>();
        itemCollider = GetComponent<Collider2D>();

        if (itemRenderer != null && itemData != null)
            itemRenderer.sprite = itemData.ItemSprite;
    }

    /// <summary>
    /// Solicita ao componente de transporte do robô que tente coletar este item.
    /// </summary>
    public void Interact(GameObject interactor)
    {
        if (isCollected || interactor == null)
            return;

        RobotCarry carry = interactor.GetComponent<RobotCarry>();
        if (carry != null)
            carry.TryCollect(this);
    }

    /// <summary>
    /// Oculta este objeto no cenário depois que o transporte aceita a coleta.
    /// </summary>
    public bool TryCollect()
    {
        if (isCollected || itemData == null || itemData.ItemSprite == null || itemRenderer == null || itemCollider == null)
            return false;

        isCollected = true;
        itemRenderer.enabled = false;
        itemCollider.enabled = false;
        return true;
    }

    /// <summary>
    /// Define os dados do item quando ele é criado por uma esteira.
    /// </summary>
    public bool Initialize(ItemData data)
    {
        if (data == null || data.ItemSprite == null || isCollected)
            return false;

        itemData = data;
        if (itemRenderer == null)
            itemRenderer = GetComponent<SpriteRenderer>();
        if (itemCollider == null)
            itemCollider = GetComponent<Collider2D>();

        if (itemRenderer == null || itemCollider == null)
            return false;

        itemRenderer.sprite = data.ItemSprite;
        itemRenderer.enabled = true;
        itemCollider.enabled = true;
        return true;
    }
}
