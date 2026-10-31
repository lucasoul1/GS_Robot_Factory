using UnityEngine;

/// <summary>
/// Contrato simples para objetos que podem receber uma interação do jogador.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Executa a ação própria do objeto interativo.
    /// </summary>
    void Interact(GameObject interactor);
}
