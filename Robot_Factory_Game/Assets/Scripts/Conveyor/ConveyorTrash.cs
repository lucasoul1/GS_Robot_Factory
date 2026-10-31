using UnityEngine;

/// <summary>
/// Remove coletáveis não coletados quando eles chegam ao fim da esteira.
/// </summary>
public class ConveyorTrash : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        CollectibleItem item = other.GetComponent<CollectibleItem>();
        if (item != null && !item.IsCollected)
            Destroy(item.gameObject);
    }
}
