using UnityEngine;

public class NavAgentBehaviour : AIPathModified
{
    public bool IsPathQueued { get; private set; }

    public bool IsSearching
    {
        get { return !canSearchAgain; }
    }
    
    /**
     * Don't update movement on Update()
     */
    private void Update()
    {
    }
    
    /**
     * Don't update movement on FixedUpdate()
     */
    private void FixedUpdate()
    {
    }
    
    public void QueuePath(Vector3 position)
    {
        IsPathQueued = true;
        targetPosition = position;
    }

    public override void SearchPath()
    {
        base.SearchPath();
        IsPathQueued = false;
    }

    public void PerformMove()
    {
        MovementUpdate(Time.deltaTime);
    }
}