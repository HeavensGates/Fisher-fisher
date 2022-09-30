﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    [Header("Boat Attributes")]
    [Range(0.5f, 2f)]
    public float speedFrontStrength = 1;
    [Range(0.5f, 2f)]
    public float speedBackStrength = 0.75f;
    [Range(2f, 10f)]
    public int maximumSpeed = 5;
    [Range(0.05f, 0.1f)]
    public float turningStrength = 0.05f;
    [Range(0.8f, 1f)]
    public float turningSlowdown=0.90f;
    private float turningMomentumLeft;
    private float turningMomentumRight;
    [Range(0.5f, 0.99f)]
    public float playerSpeedSlowdown = 0.8f;
    [SerializeField] float[] enginePowerLevel = {0,0.05f,0.10f,0.15f,0.20f };
    private float playerSpeed;

    [Header("Accelaration Boost Attributes")]
    //Accelaration boost
    private float maximumAccelerationBoost = 5f;
    [Range(0.5f, 10f)]
    public float easyBoostStrength=3, mediumBoostStrength=6, hardBoostStrength=9;
    [Range(0.005f, 0.05f)]
    public float accelarationBoostRate = 0.05f;
    [Range(1f, 5f)]
    public float boostTime = 5;
    private float boostTimeRemaining;
    private float currentAccelarationBoost = 0;
    private bool isAccelarationBoosting = false;

    private Rigidbody rBody;

    [SerializeField] AudioSource boatEngine;
    [SerializeField] float maxSpeedPitch=1.7f;
    [SerializeField] float minSpeedPitch=-2f;
    [SerializeField] float normalSpeedPitch = 0.2f;
    [SerializeField] float boostSpeedPitchEasy = 2f;
    [SerializeField] float boostSpeedPitchMedium = 2.35f;
    [SerializeField] float boostSpeedPitchHard = 2.70f;
    [SerializeField] float pitchIncreaseRate = 0.03f;

    [SerializeField] GameObject speedLines = null;
    [SerializeField] GameObject smokePatrciles = null;

    void OnEnable()
    {
        EventManager.onSpeedBoost += enableAccelarationBoost;
    }
    void OnDisable()
    {
        EventManager.onSpeedBoost -= enableAccelarationBoost;
    }
    private void enableAccelarationBoost(int boost)
    {
        if(boost==1)
        {
            //If easy skill check
            maximumAccelerationBoost = easyBoostStrength;
            boatEngine.pitch = boostSpeedPitchEasy;
        }
        else if(boost==2)
        {
            //If medium skill check
            maximumAccelerationBoost = mediumBoostStrength;
            boatEngine.pitch = boostSpeedPitchMedium;
        }
        else if(boost==3)
        {
            //If hard skill check
            maximumAccelerationBoost = hardBoostStrength;
            boatEngine.pitch = boostSpeedPitchHard;
        }
        else
        {
            //If no boost strength is given
            maximumAccelerationBoost = 5;
        }

        isAccelarationBoosting = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        boostTimeRemaining = boostTime;
        rBody = GetComponent<Rigidbody>();
        boatEngine = GetComponent<AudioSource>();
    }
    void FixedUpdate()
    {
        //Boosting
        if (isAccelarationBoosting)
        {
            accelarationBoost();
        }
        speedLines.SetActive(isAccelarationBoosting);
        smokePatrciles.SetActive(isAccelarationBoosting);
        playerMovement();
    }
    private void playerMovement()
    {
        //Turning
        if (Input.GetAxis("Horizontal") < 0)
        {
            //Store old momentum
            turningMomentumLeft = -turningStrength * playerSpeed;

            //Rotate "Left"
            transform.Rotate(-turningStrength * playerSpeed * Vector3.up);
            boatEngine.pitch += pitchIncreaseRate;
        }
        else
        {
            //Set to 0 so no extra momentum when reduced enough
            if (turningMomentumLeft > 0)
            {
                turningMomentumLeft = 0;
            }
            //Extra momentum for turning
            if (turningMomentumLeft < 0)
            {
                turningMomentumLeft *= turningSlowdown;
            }
            transform.Rotate(turningMomentumLeft * Vector3.up);
        }

        //Turning
        if (Input.GetAxis("Horizontal") > 0)
        {
            //Store old momentum
            turningMomentumRight = turningStrength * playerSpeed;

            //Rotate "Right"
            transform.Rotate(turningStrength * playerSpeed * Vector3.up);
        }
        else
        {
            //Set to 0 so no extra momentum when reduced enough
            if (turningMomentumRight < 0)
            {
                turningMomentumRight = 0;
            }
            //Extra momentum for turning
            if (turningMomentumRight > 0)
            {
                turningMomentumRight *= turningSlowdown;
            }
            transform.Rotate(turningMomentumRight * Vector3.up);
        }

        //Forward/Backwards
        if (Input.GetAxis("Vertical")>0 && playerSpeed <= maximumSpeed+currentAccelarationBoost)
        {
            playerSpeed += speedFrontStrength+enginePowerLevel[StaticValues.EnginePower] + currentAccelarationBoost;
            boatEngine.pitch += pitchIncreaseRate;
        }
        if (Input.GetAxis("Vertical") < 0 && playerSpeed >= -maximumSpeed + currentAccelarationBoost) //Speed up is weaker in reverse
        {
            playerSpeed -= speedBackStrength - enginePowerLevel[StaticValues.EnginePower] + currentAccelarationBoost;
            boatEngine.pitch -= pitchIncreaseRate;
        }
        //if no input, lower the pitch to normal
        if (Input.GetAxis("Vertical") == 0)
        {
            if (boatEngine.pitch > normalSpeedPitch)
            {
                boatEngine.pitch -= pitchIncreaseRate;
            }
            else if (boatEngine.pitch < normalSpeedPitch)
            {
                boatEngine.pitch += pitchIncreaseRate;
            }
        }
        if (!isAccelarationBoosting)
        {
            if (boatEngine.pitch > maxSpeedPitch)
            {
                boatEngine.pitch = maxSpeedPitch;
            }
            if (boatEngine.pitch < minSpeedPitch)
            {
                boatEngine.pitch = minSpeedPitch;
            }
        }
        //Apply the speed to the player
        rBody.AddRelativeForce(Vector3.forward * playerSpeed);

        //Apply a slowdown to the player speed
        playerSpeed *= playerSpeedSlowdown;
    }

    private void accelarationBoost()
    {
        //Reduce time remaining on boost
        boostTimeRemaining -= Time.deltaTime;

        //Speed up until hitting maximum boost
        if (currentAccelarationBoost<maximumAccelerationBoost)
        {
            //Apply boost to current accelaration boost
            currentAccelarationBoost += accelarationBoostRate;
        }
        else if (boostTimeRemaining <= 0)
        {
            //Reset and end boost
            boostTimeRemaining = boostTime;
            currentAccelarationBoost = 0;
            isAccelarationBoosting = false;
            return;
        }
    }
}
