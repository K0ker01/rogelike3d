﻿using UnityEngine;
using System.Collections;

public class PlayerInputController : MonoBehaviour
{

    public PlayerInput Current;
    public Vector2 RightStickMultiplier = new Vector2(3, -1.5f);

    // Use this for initialization
    void Start()
    {
        Current = new PlayerInput();
    }

    void Update()
    {

        // Retrieve our current WASD or Arrow Key input
        // Using GetAxisRaw removes any kind of gravity or filtering being applied to the input
        // Ensuring that we are getting either -1, 0 or 1
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        Vector2 mouseInput = new Vector2(0, 0);

        // Проверка наличия оси "Mouse X" и "Mouse Y"
        if (IsAxisConfigured("Mouse X"))
        {
            mouseInput.x = Input.GetAxis("Mouse X");
        }

        if (IsAxisConfigured("Mouse Y"))
        {
            mouseInput.y = Input.GetAxis("Mouse Y");
        }

        // Проверка наличия оси "AimHorizontal" и "AimVertical"
        Vector2 rightStickInput = new Vector2(0, 0);
        if (IsAxisConfigured("AimHorizontal") && IsAxisConfigured("AimVertical"))
        {
            rightStickInput = new Vector2(
                Input.GetAxisRaw("AimHorizontal"),
                Input.GetAxisRaw("AimVertical")
            );
        }

        // pass rightStick values in place of mouse when non-zero
        mouseInput.x = rightStickInput.x != 0 ? rightStickInput.x * RightStickMultiplier.x : mouseInput.x;
        mouseInput.y = rightStickInput.y != 0 ? rightStickInput.y * RightStickMultiplier.y : mouseInput.y;

        bool rollingInput = Input.GetButtonDown("rolling over");

        Current = new PlayerInput()
        {
            MoveInput = moveInput,
            MouseInput = mouseInput,
            RollInput = rollingInput
        };
    }

    // Метод для проверки существования оси
    bool IsAxisConfigured(string axisName)
    {
        try
        {
            float testValue = Input.GetAxis(axisName);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public struct PlayerInput
{
    public Vector3 MoveInput;
    public Vector2 MouseInput;
    public bool RollInput;
}
