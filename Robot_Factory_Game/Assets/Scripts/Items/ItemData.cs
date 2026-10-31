using UnityEngine;

/// <summary>
/// Dados reutilizáveis que identificam e representam visualmente um tipo de item.
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Robot Factory/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemSprite;
    [SerializeField, Min(0)] private int productivityPoints = 10;

    public string ItemName => itemName;
    public Sprite ItemSprite => itemSprite;
    public int ProductivityPoints => productivityPoints;
}
