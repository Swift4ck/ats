using UnityEngine;
using Spine.Unity;

public class PlayerAvatarController : MonoBehaviour
{
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Названия анимаций для этого аватара")]
    [SerializeField] private string idleAnim = "idle_vb";
    [SerializeField] private string attackAnim = "open_vb";
    [SerializeField] private string damageAnim = "hit_vb";
    [SerializeField] private string deathAnim = "close_vb";
    [SerializeField] private string respawnAnim = "respawn_vb";

    private void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }

    public void PlayIdle()
    {
        skeletonAnimation.state.SetAnimation(0, idleAnim, true);
    }

    public void PlayAttack()
    {
        skeletonAnimation.state.SetAnimation(0, attackAnim, false);
        skeletonAnimation.state.AddAnimation(0, idleAnim, true, 0f);
    }

    public void PlayDamage()
    {
        skeletonAnimation.state.SetAnimation(0, damageAnim, false);
        skeletonAnimation.state.AddAnimation(0, idleAnim, true, 0f);
    }

    public void PlayDeath()
    {
        skeletonAnimation.state.SetAnimation(0, deathAnim, false);
    }

    public void PlayRespawn()
    {
        skeletonAnimation.state.SetAnimation(0, respawnAnim, false);
        skeletonAnimation.state.AddAnimation(0, idleAnim, true, 0f);
    }
}
