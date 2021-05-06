using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayHandler : MonoBehaviour
{
    private float prevTime = 0.0f;
    public float timeLimit = 0.0f;
    private float timePassed = 0.0f;
    private float complete = 0.0f; // from 0.0f to 1.0f
    bool alive = false;

    System.Action<float> updateAction = null;
    System.Action endAction = null;

    public void SetLimit(float value)
    {
        timeLimit = value;
    }

    public void StartTimer( System.Action _begin = null, 
                            System.Action <float> _update = null, 
                            System.Action _end = null)
    {
        timePassed = 0.0f;
        alive = true;
        prevTime = Time.time;
        updateAction = _update;
        endAction = _end;
        if (_begin != null)
        {
            _begin.Invoke();
        }
    }

    void Update()
    {
        if (alive)
        {
            float curTime = Time.time;
            float difTime = curTime - prevTime;
            prevTime = curTime;
            timePassed += difTime;
            complete = timePassed / timeLimit;
            complete = complete > 1.0f ? 1.0f : complete;

            if (updateAction != null)
            {
                updateAction.Invoke(complete);
            }

            if (timePassed >= timeLimit)
            {
                alive = false;
                if (endAction != null)
                {
                    endAction.Invoke();
                }
            }
        }
    }

    public bool Alive()
    {
        return alive;
    }

    public bool Finished()
    {
        return timePassed >= timeLimit;
    }
}
