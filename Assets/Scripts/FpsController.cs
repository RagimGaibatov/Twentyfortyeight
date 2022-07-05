using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsController : MonoBehaviour{
    [SerializeField] private int FPS = 60;

    private void Awake(){
        Application.targetFrameRate = FPS;
    }
}