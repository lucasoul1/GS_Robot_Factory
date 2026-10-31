using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Soma produtividade somente quando uma estação confirma o processamento correto.
/// </summary>
public class ProductivityManager : MonoBehaviour
{
    [SerializeField, Min(1)] private int productivityGoal = 100;

    private readonly List<ProcessingStation> connectedStations = new List<ProcessingStation>();
    private int currentProductivity;

    public int CurrentProductivity => currentProductivity;
    public int ProductivityGoal => productivityGoal;

    /// <summary>
    /// Permite que o HUD acompanhe a pontuação sem controlar a regra de produtividade.
    /// </summary>
    public event Action<int, int> ProductivityChanged;

    private void OnEnable()
    {
        ProcessingStation[] stations = FindObjectsByType<ProcessingStation>(FindObjectsSortMode.None);
        foreach (ProcessingStation station in stations)
        {
            station.ItemProcessed += AddProductivity;
            connectedStations.Add(station);
        }
    }

    private void OnDisable()
    {
        foreach (ProcessingStation station in connectedStations)
        {
            if (station != null)
                station.ItemProcessed -= AddProductivity;
        }

        connectedStations.Clear();
    }

    /// <summary>
    /// Adiciona o valor configurado no ItemData processado pela estação.
    /// </summary>
    private void AddProductivity(ItemData itemData)
    {
        if (itemData == null)
            return;

        currentProductivity += Mathf.Max(0, itemData.ProductivityPoints);
        ProductivityChanged?.Invoke(currentProductivity, productivityGoal);
        Debug.Log($"Productivity: {currentProductivity} / {productivityGoal}.", this);
    }
}
