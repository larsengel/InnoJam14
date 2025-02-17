﻿using UnityEngine;
using System.Collections;

public class PlayerScript : MonoBehaviour {

	public int playerNumber;
	public string playerName;
	private GlewGunScript gun;
	public Transform catchObject = null;
    public bool catchFollowing = false;

	public RocketBase rocketBase = null;

	public enum DIRECTION
	{LEFT = -1,	NONE = 0, RIGHT = 1	}
	[SerializeField]
    public DIRECTION movingDirection = DIRECTION.NONE;
    public DIRECTION lookAtDirection = DIRECTION.RIGHT;
    private float axisDiff = 0.7f; // controls maximum isn't value 1 mostly. older controller joysticks have some problems
    private enum PICKUPSTATUS { NONE, DOWNTHISFRAME, WASDOWN };
    private PICKUPSTATUS pickup = PICKUPSTATUS.NONE;

    // Movement
    [SerializeField]
    private float speed;
    private float CatchSlowmo = 0.7f;
    private float JumpSlowmo = 0.6f;
    private float currentDegrees = 0;

    // Jump
    private bool enableJump = false;
    private float jumpTime = 0.8f;
    private float jumpCurrent = 0;
    private float gravityDefault = 0.2f;
    private float gravityCurrent = 0;
    private float jumpSpeed = 5.0f;

    // Damage
    private bool enableDamage = false;
    private float damageTime = 0.3f;
    private float damageCurrent = 0;
    private float damageHeight = 3.0f;
    private float damageSpeed = 6.0f;
    private float damageGravity = 0.1f;

	//public enum AIM
	//{ DOWN = -1, NONE = 0, UP = 1 }
	//[SerializeField]
	//internal AIM gunDirection;

	bool isAxisUsed = false;

	void Start()
	{
		gun = this.gameObject.transform.Find ("GlewGun").gameObject.GetComponent<GlewGunScript>();
        this.gravityDefault = this.jumpTime / 2;
        currentDegrees = 90 + 180 * playerNumber;
	}
	
	void Update()
	{
			if(this.enableDamage == false)
			{
				GetInputs();
				Move();
				Jump();
			}
			Damage();
	}

	private void GetInputs()
	{
        DIRECTION oldMovement = movingDirection;
        if (pickup == PICKUPSTATUS.DOWNTHISFRAME)
            pickup = PICKUPSTATUS.WASDOWN;

        string PlayerCode = "A";
        switch (playerNumber)
        {
            case 1:
                PlayerCode = "A";
                if (Input.GetKeyDown(KeyCode.Joystick1Button0) || Input.GetKeyDown(KeyCode.Q)) // A - Jump
                    StartJump();
				if (Input.GetKeyDown(KeyCode.Joystick1Button3) || Input.GetKeyDown(KeyCode.R)) // Y - Launch
					Launch();
                if (Input.GetKeyDown(KeyCode.Joystick1Button2) || Input.GetKeyDown(KeyCode.E)) // Button X
                    Catch();
                if (Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKeyDown(KeyCode.R))
                    Fire();
                break;
            case 2:
                PlayerCode = "B";
                if (Input.GetKeyDown(KeyCode.Joystick2Button0) || Input.GetKeyDown(KeyCode.I)) // A - Jump
                    StartJump();
				if (Input.GetKeyDown(KeyCode.Joystick2Button3) || Input.GetKeyDown(KeyCode.P)) // Y - Launch
					Launch();
                if (Input.GetKeyDown(KeyCode.Joystick2Button2) || Input.GetKeyDown(KeyCode.O)) // Button X
                    Catch();
                if (Input.GetKeyDown(KeyCode.Joystick2Button1) || Input.GetKeyDown(KeyCode.L))
                    Fire();
		    	break;
        }

		// Movement Controller Axis X + Y + A/D + Left/Right
        float axis = Input.GetAxis("Player"+PlayerCode+"Control");
        if (axis < axisDiff * -1)
            movingDirection = DIRECTION.LEFT;
        else if (axis > axisDiff)
            movingDirection = DIRECTION.RIGHT;
        else
            movingDirection = DIRECTION.NONE;
        if (movingDirection != 0)
            lookAtDirection = movingDirection;

		//gunDirection = (AIM)Input.GetAxis("AimA");

        // Reset LT Pickup'ing
        if (Input.GetAxis("Player" + PlayerCode + "ControlLTRT") > 0.6f && pickup == PICKUPSTATUS.NONE) // LR
        {
            pickup = PICKUPSTATUS.DOWNTHISFRAME;
            Catch();
        }
        if (Input.GetAxis("Player" + PlayerCode + "ControlLTRT") <= 0 && pickup == PICKUPSTATUS.WASDOWN) // LR - Reset Pickup
        {
            pickup = PICKUPSTATUS.NONE;
        }


        // RT - Fire
        if (Input.GetAxis("Player" + PlayerCode + "ControlLTRT") < -0.6f && this.catchFollowing == false) // RT - Shot
		{
			if(!isAxisUsed){
				Fire();
				isAxisUsed = true;
			}
		}
		if (Input.GetAxisRaw("Player" + PlayerCode + "ControlLTRT") == 0){
			isAxisUsed = false;
		}


        // Animator - Update Direction and Speed
        if(oldMovement != movingDirection)
        {
            PlayerAnimation animation = this.GetComponent<PlayerAnimation>();
            animation.UpdateAnimator();
        }

	}

	private void Move()
	{
        // Player Movement
        float tmpSpeed = speed;
        if (this.catchFollowing == true) // slow down if catch item
            tmpSpeed *= CatchSlowmo;
        if (this.enableJump == true) // slow down if Jump
            tmpSpeed *= JumpSlowmo;

        this.currentDegrees = Utility.DoAroundMovement(this.transform, this.currentDegrees, movingDirection, tmpSpeed, !this.enableJump);

        if (this.catchObject != null && catchFollowing == true)
        {
            // Item Movement
            float deg = Utility.DoAroundMovement(this.catchObject, this.catchObject.GetComponent<Item>().currentDegrees, movingDirection, tmpSpeed, false);
            this.catchObject.GetComponent<Item>().currentDegrees = deg;
        }
    }

    private void Catch()
    {

        // Aufnehmen
		if (this.catchObject != null && catchFollowing == false && this.catchObject.GetComponent<Item>().isLocked == false)	
        {
            catchFollowing = true;
            this.catchObject.transform.Translate(Vector3.down * 1.2f);
            Item item = this.catchObject.GetComponent<Item>();
			item.isLocked = true;
            item.currentDegrees = this.currentDegrees;
            if(this.catchObject.GetComponent<ItemMovement>() != null)
            {
                Component.Destroy(this.catchObject.GetComponent<ItemMovement>());
            }
        }
        // Ablegen
        else if (this.catchObject != null && catchFollowing == true)
        {
			if(this.rocketBase != null && this.rocketBase.GetComponent<RocketBase>().getModuleCnt() < 12)
			{
                rocketBase.placeItem(this.catchObject.gameObject);
                catchFollowing = false;
                this.catchObject = null;
			}
			else
			{
                this.ThroughItemAway();
			}
        }

    }

    public void ThroughItemAway()
    {

        float x1 = this.catchObject.transform.position.x;
        float y1 = this.catchObject.transform.position.y;
        float x2 = GameMaster.Earth.transform.position.x;
        float y2 = GameMaster.Earth.transform.position.y;
        float dist = Vector2.Distance(new Vector2(x1, y1), new Vector2(x2, y2));
        dist -= 3.5f;
        dist *= -1;

        this.catchObject.transform.Translate(Vector3.down * dist);
        this.catchObject.GetComponent<Item>().isLocked = false;
        catchFollowing = false;
        this.catchObject = null;
    }

    private void Fire()
    {
        gun.Fire(lookAtDirection, this.currentDegrees, (float)this.lookAtDirection*10);
    }

    private void StartJump()
    {
        if (this.enableJump == false)
        {
			if(playerNumber == 2)
				this.transform.Find("Spaceman_Blue").GetComponent<Animator>().SetTrigger("Jump");
			if(playerNumber == 1)
				this.transform.Find("Spaceman_Red").GetComponent<Animator>().SetTrigger("Jump");
			GameObject.Find ("ScriptContainer/Jump").GetComponent<AudioSource> ().Play ();
            this.enableJump = true;
            this.gravityCurrent = this.gravityDefault;
        }
    }

    private void Jump()
    {
        if(this.enableJump == true)
        {
            this.jumpCurrent += Time.deltaTime;
            this.gravityCurrent -= Time.deltaTime;
            if(this.jumpCurrent <= this.jumpTime)
            {
                float speedJump = this.jumpSpeed * Time.deltaTime * (this.gravityCurrent / this.gravityDefault);

                this.transform.Translate(Vector3.up * speedJump);
                if (this.catchObject != null && this.catchFollowing == true)
                    this.catchObject.transform.Translate(Vector3.down * speedJump);
            }
            else
            {
                this.jumpCurrent = 0;
                this.enableJump = false;
            }
        }

    }

	private void Launch()
	{
		if(this.rocketBase != null && this.rocketBase.GetComponent<RocketBase>().isStartable())
		{
			this.rocketBase.GetComponent<RocketBase>().startCountdown();
		}
	}

    public void EnableDamage()
    {
        this.enableDamage = true;
        this.gravityCurrent = this.damageGravity;
        this.enableJump = false;
        
		if(playerNumber == 2)
			this.transform.Find("Spaceman_Blue").GetComponent<Animator>().SetTrigger ("Hitted");
		if(playerNumber == 1)
			this.transform.Find("Spaceman_Red").GetComponent<Animator>().SetTrigger ("Hitted");
		GameObject.Find("ScriptContainer/Hit").GetComponent<AudioSource>().Play();

        if(this.catchFollowing == true) {
            this.ThroughItemAway();
        }

    }

    public void Damage()
    {
        if(this.enableDamage)
        {
            this.damageCurrent += Time.deltaTime;
            this.gravityCurrent -= Time.deltaTime;

            if(this.damageCurrent <= this.damageTime)
            {
                // Jump top
                Vector3 damageJump = new Vector3(0, 1, 0) * this.damageHeight * Time.deltaTime * (this.gravityCurrent / this.damageGravity);
                // Jump backwords
                damageJump += new Vector3((float)lookAtDirection*-1, 0, 0) * this.damageSpeed * Time.deltaTime;
                this.transform.Translate(damageJump);
            }
            else
            {
                this.enableDamage = false;
                this.damageCurrent = 0;

                // move player by degrees
                float addDegrees = 15 * (float)lookAtDirection*-1;
                this.currentDegrees += addDegrees;
                // rotate player
                Utility.DoTurn(this.transform, addDegrees);

            }
            
        }
    }

    public void LookOtherWay()
    {
        this.lookAtDirection = (DIRECTION)((float)this.lookAtDirection*-1);
        this.movingDirection = (DIRECTION)((float)this.lookAtDirection);
    }


}
