using System.Collections;

namespace KamuraPrime.States
{
    public class RandomSelectorNode : CompositeNodeBt
    {
        private IEnumerator currentRunningRoutine;
        private NodeBT currentActiveNode;

        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
            if (currentRunningRoutine != null)
            {
                Blackboard.Runner.StopCoroutine(currentRunningRoutine);
                currentRunningRoutine = null;
            }

            if (currentActiveNode != null)
            {
                currentActiveNode.Stop();
                currentActiveNode = null;
            }

            foreach (NodeBT child in children)
            {
                child?.Stop();
            }
        }

        protected override void OnSuccess()
        {
        }

        protected override IEnumerator OnUpdate()
        {
            while (State == BTNodeState.Running)
            {
                var index = UnityEngine.Random.Range(0, children.Count);
                var subnode = children[index];
                currentActiveNode = subnode;

                Blackboard.CurrentNode = subnode;

                currentRunningRoutine = Blackboard.Runner.StartCoroutine(subnode.UpdateBT());

                yield return MyWaitForUpdate.Get();

                while (subnode.State == BTNodeState.Running)
                {
                    if (State != BTNodeState.Running)
                        yield break;

                    yield return MyWaitForUpdate.Get();
                }

                currentRunningRoutine = null;

                if (subnode.State == BTNodeState.Pause)
                {
                    Pause();
                    yield break;
                }

                yield return MyWaitForUpdate.Get();
            }
            Success();
        }
    }
}