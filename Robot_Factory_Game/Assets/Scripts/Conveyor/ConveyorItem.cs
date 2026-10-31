using UnityEngine;

/// <summary>
/// Move um item coletável na direção local da esteira até ele ser coletado ou descartado.
/// </summary>
public class ConveyorItem : MonoBehaviour
{
    private CollectibleItem collectibleItem;
    private Rigidbody2D itemBody;
    private Vector2 movementDirection;
    private float movementSpeed;

    /// <summary>
    /// Recebe as referências e a direção configuradas pela esteira ao gerar o item.
    /// </summary>
    public void Initialize(
        CollectibleItem item,
        Rigidbody2D body,
        Vector2 direction,
        float speed)
    {
        collectibleItem = item;
        itemBody = body;
        movementDirection = direction.normalized;
        movementSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (collectibleItem == null || collectibleItem.IsCollected)
        {
            if (itemBody != null)
                itemBody.simulated = false;

            enabled = false;
            return;
        }

        itemBody.MovePosition(itemBody.position + movementDirection * movementSpeed * Time.fixedDeltaTime);
    }
}
