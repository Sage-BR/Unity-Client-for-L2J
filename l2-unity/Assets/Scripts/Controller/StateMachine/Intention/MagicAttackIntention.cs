using System.Threading.Tasks;
using UnityEngine;

public class MagicAttackIntention : IntentionBase
{

    public MagicAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        if(arg0.GetType() == typeof(MagicSkillUseDto))
        {
            MagicSkillUseDto useSkill = (MagicSkillUseDto)arg0;
            AnimationCombo anim = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId , useSkill.SkillLvl);
            float distance = VectorUtils.Distance2D(useSkill.AttackerPos, useSkill.TargetPos); 
            //Debug.Log("DISTANCE TO SERVER UNITY  " + distance);
           
            if (useSkill.SkillId != 1177) return;

            if (anim != null)
            {
                Rotate(IncomingPacketActions.Player, useSkill).Observe();
                WaitAndStart(useSkill, anim, distance).Observe();
            }
        }
    }

    private async Task WaitAndStart(MagicSkillUseDto useSkill , AnimationCombo anim , float distance)
    {
        float deadline = Time.realtimeSinceStartup + 0.5f;

        while (IncomingPacketActions.Player != null && IncomingPacketActions.Player.IsTurnsAround())
        {
           // if (await Task.WhenAny(timeout) == timeout) break;
           if (Time.realtimeSinceStartup >= deadline) break;
           await Task.Delay(16);
        }

        await StartUseSkill(useSkill, anim, distance);
    }

    private async Task StartUseSkill(MagicSkillUseDto useSkill , AnimationCombo anim , float distance)
    {
        Entity monster =await  IncomingPacketActions.GameWorld.GetEntityNoLock(useSkill.TargetId);
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SkillCombos.ExecutePlayerCombo(useSkill.TargetId, anim, useSkill.HitTime, distance, IncomingPacketActions.EffectSkills, useSkill.SkillId);
            var footerPosition = IncomingPacketActions.Player.GetPlayerPosition();
            // var bodyPosition = PlayerController.Instance.GetBodyPosition();
            var bodyPosition = IncomingPacketActions.Player.GetCollisionSelf(monster.transform);
            IncomingPacketActions.EffectSkills.ShowEffect(useSkill.SkillId, footerPosition, bodyPosition, useSkill.HitTime , monster);
        });
    }

    private async Task Rotate(PlayerController controller , MagicSkillUseDto useSkill)
    {
        Entity entity = await IncomingPacketActions.GameWorld.GetEntityNoLock(useSkill.TargetId);
        if (controller != null && entity != null) controller.StartRotateFollow(entity);
    }

    public override void Exit() { }
    public override void Update()
    {

    }
}
