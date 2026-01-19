using UnityEngine;

namespace KamuraPrime.YieldInstructions
{
    public class WaitForAnimationFrame : CustomYieldInstruction
    {
        private readonly Animator animator;
        private readonly int targetFrame;
        private readonly int layer;

        private int elapsedFrames = 0;

        public WaitForAnimationFrame(Animator animator, int targetFrame, int layer = 0)
        {
            this.animator = animator;
            this.targetFrame = targetFrame;
            this.layer = layer;
        }

        public override bool keepWaiting
        {
            get
            {
                if (elapsedFrames < 1)
                {
                    elapsedFrames++;
                    return true;
                }

                if (!animator || !animator.enabled) 
                    return false;

                var clipInfo = animator.GetCurrentAnimatorClipInfo(layer);
                var clip = clipInfo[0].clip;
                var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);

                float totalFrames = clip.length * clip.frameRate;
                float currentFrame = (stateInfo.normalizedTime % 1f) * totalFrames;

                return currentFrame < targetFrame;
            }
        }
    }
}