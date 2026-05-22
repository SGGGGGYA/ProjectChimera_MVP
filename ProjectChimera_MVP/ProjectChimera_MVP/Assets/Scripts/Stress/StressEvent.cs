using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StressEvent
{
    public int amount;
    public StressTag tag;
    public float gameTime;

    public StressEvent(int amount, StressTag tag)
    {
        this.amount = amount;
        this.tag = tag;
        this.gameTime = Time.time;
    }
}
