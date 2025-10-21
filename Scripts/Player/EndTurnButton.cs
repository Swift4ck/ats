using Mirror;
using Spine.Unity;
using TSGame;
using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation buttonSkeleton; // было skeletonAnimation
    [SerializeField] private string idleAnim = "Idle_knop";
    [SerializeField] private string hoverAnim = "Idle_neactiv";
    [SerializeField] private string pressedAnim = "click";

    private bool canInteract = false; // было isInteractable

    private void Start()
    {
        // Скрываем кнопку по умолчанию
        gameObject.SetActive(false);
    }

    public void ShowButton(bool value)
    {
        canInteract = value;
        gameObject.SetActive(value);

        if (value && buttonSkeleton != null)
            buttonSkeleton.state.SetAnimation(0, idleAnim, true);
    }

    private void OnMouseEnter()
    {
        if (canInteract && buttonSkeleton != null)
            buttonSkeleton.state.SetAnimation(0, hoverAnim, true);
    }

    private void OnMouseExit()
    {
        if (canInteract && buttonSkeleton != null)
            buttonSkeleton.state.SetAnimation(0, idleAnim, true);
    }

    private void OnMouseDown()
    {
        if (!canInteract) return;

        if (buttonSkeleton != null)
            buttonSkeleton.state.SetAnimation(0, pressedAnim, false);

        // Вызываем передачу хода через TurnManager
        if (NetworkClient.localPlayer != null)
        {
            var player = NetworkClient.localPlayer.GetComponent<PlayerCore>();
            if (player != null)
            {
                player.CmdEndTurn(); // Command → вызывает TurnManager.EndTurn()
            }
        }
    }
}
