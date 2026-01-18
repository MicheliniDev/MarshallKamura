using DHUtil.Elite.Animation;
using DHUtil.SerializableDictionary;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KamuraPrime.States;

public class EliteChainShootState : BossStateNode
{
    public enum StepType { Air, Ground, Aim }
    public enum TeleportPositionType { FromStartPos, Absolute, FromPlayer }
    
    [Serializable]
    public class ChainStep
    {
        public string StepName = "Step";
        public StepType Type;
        public Vector3 Position;
        public TeleportPositionType TeleportPositionType;

        public int GroundShootFrame = 12;

        public bool SkipTeleport;
        public bool Flip;
        public bool VerticalFlip;
        public float OverrideRotation;

        public Action OnStepEnter;
        public EliteAirShootState.ShootType AirShootType;
        public EliteAirAttackInfo AirAttackInfo;
    }

    public List<ChainStep> Steps = new List<ChainStep>();
    public bool ReloadAtEnd = true;
    public bool DisableGravity = true;

    public SerializableDictionary<string, EventReference> BossProfile => FmodSfxPool.Instance.SoundProfile.EliteBoss;

    protected override void OnStart()
    {
        boss.IsPairing = false;
        boss.SetVelocity(Vector2.zero);
        boss.Gravity = DisableGravity ? 0f : boss.Profile.Gravity;
        RotationGFXImmediately(0f);
        RotationGFX(rotate: false);
    }

    protected override void OnStop()
    {
        boss.Gravity = boss.Profile.Gravity;
        boss.IsPairing = false;
        boss.SkipTeleport = false;
        RotationGFXImmediately(0f);
        RotationGFX(rotate: false);
    }

    protected override void OnSuccess()
    {
        RotationGFXImmediately(0f);
        RotationGFX(rotate: false);
        boss.IsPairing = false;
        boss.Gravity = boss.Profile.Gravity;
    }

    protected override IEnumerator OnUpdate()
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            ChainStep step = Steps[i];
            step.OnStepEnter?.Invoke();

            Vector3 targetPos = CheckTeleportPos(step);
            targetPos.x = Mathf.Clamp(targetPos.x, 2401f, 2438f);
            targetPos.y = Mathf.Clamp(targetPos.y, -286f, Constants.ARENA_TOP_POS.y);

            if (i == 0 || !step.SkipTeleport)
            {
                boss.Change2TeleportReadyAnimation();
                SetHardWall(true);
                
                yield return MyWaitForUpdate.Get();
                yield return MyWaitForUpdate.Get();
                yield return MyWaitForUpdate.Get();

                yield return CheckAnimationEnd();

                boss.OnTeleport(targetPos);
                RotationGFXImmediately(0f);

                if (step.Type != StepType.Aim)
                    boss.Change2TeleportEndAnimation();

                yield return MyWaitForUpdate.Get();

                if (DisableGravity)
                {
                    boss.Gravity = 0f;
                    boss.SetVelocity(Vector2.zero);
                }
                SetHardWall(false);
                yield return CheckAnimationEnd();
            }
            else
            {
                boss.OnTeleport(targetPos);
                RotationGFXImmediately(0f);
            }

            if (step.Type != StepType.Aim)
            {
                RotationGFXImmediately(0f);
                boss.Flip(step.Flip ? -1f : 1f);
            }
            yield return PerformAttack(step);
        }

        if (ReloadAtEnd)
        {
            RotationGFXImmediately(0f);
            boss.IsPairing = true;
            boss.Change2ReloadAnimation();
            yield return CheckAnimationEnd();
            boss.IsPairing = false;
        }

        Success();
    }

    private IEnumerator PerformAttack(ChainStep step)
    {
        SetupEffects(step);
        
        if (step.OverrideRotation != 0f)
        {
            float rotationToApply = step.Flip ? -step.OverrideRotation : step.OverrideRotation;
            RotationGFXImmediately(rotationToApply);
            boss.Flip(step.Flip ? -1f : 1f);
        }

        yield return MyWaitForUpdate.Get();

        if (step.Type == StepType.Aim)
        {
            float angle = PlayerAngle();
            while (boss.GetCurrentAnimationFrame() < 2)
            {
                angle = PlayerAngle();
                RotateButGood(angle);
                yield return MyWaitForUpdate.Get();
            }

            yield return CheckAttackFrame(9);
            FireShot(step, 0, angle);
        }
        else
        {
            bool isAir = step.Type == StepType.Air;
            bool isSameTime = isAir && step.AirShootType == EliteAirShootState.ShootType.SameTime;

            int totalShots = isAir ? step.AirAttackInfo.AttackFrameList.Count : 1;

            if (isSameTime)
            {
                int waitFrame = isAir ?
                    step.AirAttackInfo.AttackFrameList[0].AttackFrame :
                    step.GroundShootFrame;

                yield return CheckAttackFrame(waitFrame);

                for (int i = 0; i < totalShots; i++)
                {
                    FireShot(step, i);
                }
            }
            else
            {
                for (int i = 0; i < totalShots; i++)
                {
                    int currentSequenceIndex = i;

                    int dataIndex = i;
                    if (isAir && step.VerticalFlip)
                        dataIndex = totalShots - 1 - i;

                    int waitFrame = isAir ?
                        step.AirAttackInfo.AttackFrameList[currentSequenceIndex].AttackFrame :
                        step.GroundShootFrame;

                    yield return CheckAttackFrame(waitFrame);
                    FireShot(step, dataIndex);
                }
            }
        }
    }

    private void SetupEffects(ChainStep step)
    {
        if (step.Type == StepType.Aim)
        {
            boss.ChangeAnimation(GetAnimationParameter.AimShot);
            return;
        }

        if (step.Type == StepType.Ground)
        {
            boss.ChangeAnimation(GetAnimationParameter.GroundShot);
            boss.PlayBossVoice("GroundShotVoice", boss.transform, BossProfile);
            return;
        }

        boss.Animator.SetBool(GetAnimationParameter.Flip, step.VerticalFlip);

        switch (step.AirShootType)
        {
            case EliteAirShootState.ShootType.Upper:
                boss.ChangeAnimation(GetAnimationParameter.AirShot);
                boss.PlayBossVoice("AirShotVoice", boss.transform, BossProfile);
                break;
            case EliteAirShootState.ShootType.Side:
                boss.ChangeAnimation(GetAnimationParameter.SideShot);
                boss.PlayBossVoice("SlideShotVoice", boss.transform, BossProfile);
                break;
            case EliteAirShootState.ShootType.SameTime:
                boss.ChangeAnimation(GetAnimationParameter.HoleShot);
                FmodSfxPool.Instance.PlaySound("HoleShotVoice", boss.transform, BossProfile);
                break;
        }
    }

    private void FireShot(ChainStep step, int dataIndex, float aimAngle = 0f)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        float speed = 160f;

        switch (step.Type)
        {
            case StepType.Aim:
                spawnPos = boss.AttackPointCounter.transform.position;
                rotation = Quaternion.Euler(0f, 0f, aimAngle + step.OverrideRotation);
                speed = 160f;
                FmodSfxPool.Instance.PlaySound("Shot", boss.transform, BossProfile);
                break;
            case StepType.Air:
                var data = step.AirAttackInfo.AttackFrameList[dataIndex];
                float directionMult = step.Flip ? -1f : 1f;

                Vector3 direction = data.Direction;
                Vector3 offset = data.AttackOffset;

                direction.x *= directionMult;
                offset.x *= directionMult;

                if (step.OverrideRotation != 0f)
                {
                    float radians = step.OverrideRotation * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(radians);
                    float sin = Mathf.Sin(radians);

                    float newOffsetX = offset.x * cos - offset.y * sin;
                    float newOffsetY = offset.x * sin + offset.y * cos;
                    offset = new Vector3(newOffsetX, newOffsetY, offset.z);

                    float newDirX = direction.x * cos - direction.y * sin;
                    float newDirY = direction.x * sin + direction.y * cos;
                    direction = new Vector3(newDirX, newDirY, direction.z);
                }

                offset += boss.Collision2D.GetCenter();

                float angle = Mathf.Atan2(direction.y, direction.x) * 57.29578f;

                spawnPos = offset;
                rotation = Quaternion.Euler(0f, 0f, angle);
                speed = step.AirAttackInfo.Speed;
                FmodSfxPool.Instance.PlaySound("HoleShot", boss.transform, BossProfile);
                break;
            case StepType.Ground:
                float directionX = step.Flip ? -1f : 1f;
                spawnPos = boss.AttackPoint.transform.position;

                float groundAngle = directionX < 0 ? 180f : 0f;
                rotation = Quaternion.Euler(0f, 0f, groundAngle + step.OverrideRotation);

                speed = 160f;
                FmodSfxPool.Instance.PlaySound("HoleShot", boss.transform, BossProfile);
                break;
        }

        var bullet = Singleton<GameManager>.Instance.ObjectPoolingManager.ProjectilePoolingManager.Pop(Constants.BULLET_PROJECTILE_NAME);
        bullet.transform.position = spawnPos;
        bullet.transform.rotation = rotation;

        bullet.Shoot(1, speed, boss);
    }

    private Vector2 CheckTeleportPos(ChainStep step)
    {
        switch (step.TeleportPositionType)
        {
            case TeleportPositionType.Absolute:
                return step.Position;
            case TeleportPositionType.FromPlayer:
                return step.Position + GameManager.Instance.PlayerStateMachine.transform.position;
            case TeleportPositionType.FromStartPos:
                return step.Position + boss.StartPosition;
        }
        return Vector2.zero;
    }

    protected void RotateButGood(float angle)
    {
        float playerX = Singleton<GameManager>.Instance.PlayerStateMachine.transform.position.x;
        float bossX = boss.transform.position.x;
        bool shouldFaceLeft = playerX < bossX;

        float num = angle;

        if (shouldFaceLeft)
        {
            num = 180f - angle;

            if (num > 180f) num -= 360f;
            if (num < -180f) num += 360f;

            boss.Flip(-1f);
        }
        else
        {
            boss.Flip(1f);
        }

        boss.RotationPart.NonRotationHolder.transform.localEulerAngles = new Vector3(0f, 0f, num);
    }
}