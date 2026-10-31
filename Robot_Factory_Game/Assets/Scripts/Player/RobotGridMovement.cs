using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Move o robô uma célula por vez e impede a entrada em células bloqueadas.
/// </summary>
public class RobotGridMovement : MonoBehaviour
{
    private enum Direction { None, Up, Down, Left, Right }

    [Header("Movement")]
    [SerializeField] private Grid grid;
    [SerializeField, Min(0.1f)] private float movementSpeed = 4f;
    [SerializeField, Min(0f)] private float initialRepeatDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float repeatInterval = 0.12f;
    [SerializeField] private Direction startingDirection = Direction.Down;

    private Vector3Int currentCell;
    private Vector3 targetPosition;
    private Direction facingDirection;
    private Direction heldDirection;
    private float repeatTimer;
    private bool isMoving;

    /// <summary>
    /// Célula à frente do robô, calculada pela posição lógica e pela orientação atual.
    /// </summary>
    public Vector3Int CellInFront => currentCell + GetCellOffset(facingDirection);

    /// <summary>
    /// Informa se a interpolação entre células ainda está em andamento.
    /// </summary>
    public bool IsMoving => isMoving;

    /// <summary>
    /// Direção atual no formato usado pelo Animator: baixo 0, cima 1, esquerda 2, direita 3.
    /// </summary>
    public int AnimatorDirectionIndex => facingDirection switch
    {
        Direction.Down => 0,
        Direction.Up => 1,
        Direction.Left => 2,
        Direction.Right => 3,
        _ => 0
    };

    /// <summary>
    /// Interrompe o controle e realinha o robô à célula mais próxima.
    /// </summary>
    public void StopMovement()
    {
        currentCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(currentCell);
        targetPosition = transform.position;
        isMoving = false;
        heldDirection = Direction.None;
        repeatTimer = 0f;
    }

    private void Awake()
    {
        if (grid == null)
        {
            Debug.LogError("RobotGridMovement precisa de uma referência ao Grid.", this);
            enabled = false;
            return;
        }

        // Centraliza o robô na célula mais próxima ao iniciar.
        currentCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(currentCell);
        SetFacing(startingDirection);
    }

    private void Update()
    {
        Direction input = ReadInput();

        if (isMoving)
        {
            TrackInputWhileMoving(input);
            MoveToTarget();
            return;
        }

        HandleInput(input);
    }

    /// <summary>
    /// Lê setas/WASD. Qualquer combinação de duas direções é descartada.
    /// </summary>
    private static Direction ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Direction.None;

        bool up = keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed;
        bool down = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed;
        bool left = keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed;
        bool right = keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;

        int pressedDirections = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);
        if (pressedDirections != 1) return Direction.None;

        if (up) return Direction.Up;
        if (down) return Direction.Down;
        if (left) return Direction.Left;
        return Direction.Right;
    }

    /// <summary>
    /// Uma nova direção primeiro orienta o robô; se já estiver orientado, inicia um passo.
    /// </summary>
    private void HandleInput(Direction input)
    {
        if (input == Direction.None)
        {
            heldDirection = Direction.None;
            repeatTimer = 0f;
            return;
        }

        if (input != heldDirection)
        {
            heldDirection = input;
            repeatTimer = initialRepeatDelay;

            if (input != facingDirection)
            {
                SetFacing(input);
                return;
            }

            TryStartStep(input);
            return;
        }

        repeatTimer -= Time.deltaTime;
        if (repeatTimer > 0f) return;

        if (input != facingDirection)
        {
            SetFacing(input);
            repeatTimer = initialRepeatDelay;
            return;
        }

        TryStartStep(input);
        repeatTimer = repeatInterval;
    }

    /// <summary>
    /// Mantém o temporizador coerente enquanto o passo atual termina.
    /// </summary>
    private void TrackInputWhileMoving(Direction input)
    {
        if (input == Direction.None)
        {
            heldDirection = Direction.None;
            repeatTimer = 0f;
        }
        else if (input != heldDirection)
        {
            heldDirection = input;
            repeatTimer = initialRepeatDelay;
        }
        else
        {
            repeatTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Só atualiza a célula lógica e começa a andar se o destino estiver livre.
    /// </summary>
    private void TryStartStep(Direction direction)
    {
        Vector3Int destinationCell = currentCell + GetCellOffset(direction);
        if (IsCellBlocked(destinationCell)) return;

        currentCell = destinationCell;
        targetPosition = grid.GetCellCenterWorld(currentCell);
        isMoving = true;
    }

    /// <summary>
    /// Consulta colisores 2D no espaço da célula de destino.
    /// </summary>
    private bool IsCellBlocked(Vector3Int cell)
    {
        Vector3 center = grid.GetCellCenterWorld(cell);
        Vector2 checkSize = new Vector2(grid.cellSize.x * 0.9f, grid.cellSize.y * 0.9f);
        return Physics2D.OverlapBox(center, checkSize, 0f) != null;
    }

    /// <summary>
    /// Interpola até o centro exato da célula de destino.
    /// </summary>
    private void MoveToTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            movementSpeed * Time.deltaTime);

        if ((transform.position - targetPosition).sqrMagnitude < 0.000001f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    /// <summary>
    /// Atualiza a rotação visual sem mudar a posição do robô.
    /// </summary>
    private void SetFacing(Direction direction)
    {
        facingDirection = direction;

        float angle = direction switch
        {
            Direction.Up => 0f,
            Direction.Down => 180f,
            Direction.Left => 90f,
            Direction.Right => -90f,
            _ => 0f
        };

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private static Vector3Int GetCellOffset(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Vector3Int.up,
            Direction.Down => Vector3Int.down,
            Direction.Left => Vector3Int.left,
            Direction.Right => Vector3Int.right,
            _ => Vector3Int.zero
        };
    }
}
