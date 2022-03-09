﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Clock : MonoBehaviour
{
    public float timeRemaining = 120;
    private TMPro.TextMeshProUGUI clockText;

    // Start is called before the first frame update
    void Start()
    {
        clockText = GetComponent<TMPro.TextMeshProUGUI>();
    }

    void Update()
    {
        if (timeRemaining > 0.1)
        {
            timeRemaining -= Time.deltaTime;
            int mins = (int) Math.Floor(timeRemaining / 60f);
            int secs = ((int) Math.Floor(timeRemaining)) % 60;

            if (secs < 10)
                clockText.text = mins.ToString() + ":0" + secs.ToString();
            else
                clockText.text = mins.ToString() + ":" + secs.ToString();
        }
        else
        {
            clockText.text = "0:00";
        }
    }
}
