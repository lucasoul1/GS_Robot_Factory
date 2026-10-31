using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Procura um elemento interativo na célula à frente do robô quando E é pressionado.
/// </summary>
public class RobotInteraction : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private RobotGridMovement robotMovement;

    private void Awake()
    {
        if (robotMovement == null)
            robotMovement = GetComponent<RobotGridMovement>();

        if (grid == null || robotMovement == null)
        {
            Debug.LogError("RobotInteraction precisa das referências ao Grid e ao RobotGridMovement.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame || robotMovement.IsMoving)
            return;

        InteractWithCellInFront();
    }

    /// <summary>
    /// Consulta os colliders da célula à frente e chama o primeiro elemento interativo encontrado.
    /// </summary>
    private void InteractWithCellInFront()
    {
        Vector3 targetCenter = grid.GetCellCenterWorld(robotMovement.CellInFront);
        Vector2 checkSize = new Vector2(grid.cellSize.x * 0.9f, grid.cellSize.y * 0.9f);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(targetCenter, checkSize, 0f);

        foreach (Collider2D targetCollider in colliders)
        {
            if (targetCollider == null)
                continue;

            IInteractable interactable = targetCollider.GetComponent<IInteractable>();
            if (interactable == null)
                continue;

            interactable.Interact(gameObject);
            return;
        }
    }
}
