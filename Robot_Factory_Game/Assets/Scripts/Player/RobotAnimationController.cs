using UnityEngine;

/// <summary>
/// Atualiza os parâmetros visuais do robô sem controlar seu movimento no Grid.
/// </summary>
public class RobotAnimationController : MonoBehaviour
{
    private static readonly int DirectionParameter = Animator.StringToHash("Direction");
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");

    [SerializeField] private Animator animator;
    [SerializeField] private RobotGridMovement robotMovement;

    private Transform robotRoot;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (robotMovement == null)
            robotMovement = GetComponentInParent<RobotGridMovement>();

        robotRoot = robotMovement != null ? robotMovement.transform : transform.parent;

        if (animator == null || robotMovement == null)
        {
            Debug.LogError("RobotAnimationController precisa de Animator e RobotGridMovement.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (animator == null || robotMovement == null)
            return;

        animator.SetInteger(DirectionParameter, robotMovement.AnimatorDirectionIndex);
        animator.SetBool(IsMovingParameter, robotMovement.IsMoving);
    }

    private void LateUpdate()
    {
        if (robotRoot == null)
            return;

        // O objeto raiz mantém a orientação usada pelo transporte, enquanto o sprite fica na vertical.
        transform.localRotation = Quaternion.Inverse(robotRoot.localRotation);
    }
}
