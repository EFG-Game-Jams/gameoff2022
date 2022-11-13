using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HUD : MonoBehaviour
{
    private Label speedLabel;
    private Label timerLabel;

    private void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement; // Basically the DOM equivalent in Unity
        speedLabel = root.Q<Label>("label-speed"); // Get the C# object representing the element (Label, Button, etc)
        timerLabel = root.Q<Label>("label-timer");

        speedLabel.text = "THIS IS FROM SCRIPT";
        timerLabel.text = "THIS IS FROM SCRIPT";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
