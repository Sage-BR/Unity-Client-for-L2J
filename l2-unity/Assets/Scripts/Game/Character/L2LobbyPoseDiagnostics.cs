using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>One pose snapshot per lobby pawn, after equipment and animation settle.</summary>
public static class L2LobbyPoseDiagnostics
{
    public static IEnumerator Record(GameObject pawn)
    {
        yield return new WaitForSecondsRealtime(1f);
        if (pawn == null) yield break;
        var animator = pawn.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null) yield break;
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        Debug.Log($"[LobbyPose] controller={animator.runtimeAnimatorController.name} " +
            $"state={animator.GetCurrentAnimatorStateInfo(0).shortNameHash} " +
            $"clips={string.Join(",", clips.Select(c => c.clip.name))} death={animator.GetBool("death")} " +
            $"animatorRotation={animator.transform.localEulerAngles}");
        foreach (var renderer in pawn.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMesh == null) continue;
            int foreignBones = renderer.bones.Count(bone => bone != null && !bone.IsChildOf(animator.transform));
            Debug.Log($"[LobbyPose] mesh={renderer.sharedMesh.name} bones={renderer.bones.Length} " +
                $"foreignBones={foreignBones} rootInAnimator={renderer.rootBone != null && renderer.rootBone.IsChildOf(animator.transform)} " +
                $"size={renderer.bounds.size} centerAbovePawn={renderer.bounds.center.y - pawn.transform.position.y:F4}");
        }
    }
}
