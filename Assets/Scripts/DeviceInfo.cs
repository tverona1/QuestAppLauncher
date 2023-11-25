using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DeviceInfo : MonoBehaviour
{
    public DateTime ExpDat;

    public bool isTracking;

    public int ExpDate,
      ExpMonth,
      ExpYear;

    public UnityEvent onAllowed,
        onBanned;

    private void Start()
    {
        ExpDat = new DateTime(ExpYear, ExpMonth, ExpDate);
        int res = DateTime.Compare(ExpDat, DateTime.UtcNow);

        if (isTracking)
        {
            if (res >= 0)
            {
                onAllowed.Invoke();
            }
            else
            {
                onBanned.Invoke();
            }
        }
    }
}
