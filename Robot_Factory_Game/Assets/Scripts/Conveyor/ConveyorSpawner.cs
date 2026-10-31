using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gera itens na entrada da esteira quando a célula está livre.
/// </summary>
public class ConveyorSpawner : MonoBehaviour
{
    // As opções seguem os eixos locais do GameObject Conveyor.
    private enum FlowDirection
    {
        Right,
        Down,
        Left,
        Up
    }

    [SerializeField] private CollectibleItem collectiblePrefab;
    [SerializeField] private List<ItemData> itemOptions = new List<ItemData>();
    [SerializeField, Min(0f)] private float spawnInterval = 2f;
    [SerializeField, Min(0f)] private float movementSpeed = 2f;
    [SerializeField, Tooltip("Direção do fluxo em relação aos eixos locais do objeto Conveyor.")]
    private FlowDirection flowDirection = FlowDirection.Right;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool generationEnabled = true;

    private float timeUntilNextSpawn;
    private int nextItemIndex;

    private void Awake()
    {
        if (collectiblePrefab == null || spawnPoint == null || itemOptions == null || itemOptions.Count == 0)
        {
            Debug.LogError("ConveyorSpawner precisa de Prefab, ponto de geração e ao menos um ItemData.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!generationEnabled)
            return;

        if (timeUntilNextSpawn > 0f)
        {
            timeUntilNextSpawn -= Time.deltaTime;
            return;
        }

        if (IsSpawnPointOccupied())
            return;

        // O intervalo é reiniciado apenas quando um item é gerado.
        if (SpawnNextItem())
            timeUntilNextSpawn = spawnInterval;
    }

    /// <summary>
    /// Cria o próximo tipo válido e inicia seu movimento no sentido local da esteira.
    /// </summary>
    private bool SpawnNextItem()
    {
        ItemData itemData = GetNextItemData();
        if (itemData == null)
            return false;

        CollectibleItem item = Instantiate(collectiblePrefab, spawnPoint.position, Quaternion.identity);
        if (!item.Initialize(itemData))
        {
            Destroy(item.gameObject);
            return false;
        }

        Rigidbody2D itemBody = item.GetComponent<Rigidbody2D>();
        if (itemBody == null)
            itemBody = item.gameObject.AddComponent<Rigidbody2D>();

        itemBody.bodyType = RigidbodyType2D.Kinematic;
        itemBody.gravityScale = 0f;
        itemBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        itemBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        ConveyorItem conveyorItem = item.gameObject.AddComponent<ConveyorItem>();
        conveyorItem.Initialize(item, itemBody, GetMovementDirection(), movementSpeed);
        return true;
    }

    /// <summary>
    /// Converte a opção do Inspector em uma direção baseada nos eixos locais da esteira.
    /// </summary>
    private Vector2 GetMovementDirection()
    {
        switch (flowDirection)
        {
            case FlowDirection.Down:
                return -transform.up;
            case FlowDirection.Left:
                return -transform.right;
            case FlowDirection.Up:
                return transform.up;
            default:
                return transform.right;
        }
    }

    /// <summary>
    /// Impede a geração enquanto houver um colisor ocupando a célula inicial.
    /// </summary>
    private bool IsSpawnPointOccupied()
    {
        Vector2 checkSize = new Vector2(0.9f, 0.9f);
        return Physics2D.OverlapBoxAll(spawnPoint.position, checkSize, 0f).Length > 0;
    }

    private ItemData GetNextItemData()
    {
        if (itemOptions == null || itemOptions.Count == 0)
            return null;

        for (int i = 0; i < itemOptions.Count; i++)
        {
            ItemData itemData = itemOptions[nextItemIndex];
            nextItemIndex = (nextItemIndex + 1) % itemOptions.Count;

            if (itemData != null && itemData.ItemSprite != null)
                return itemData;
        }

        return null;
    }
}
