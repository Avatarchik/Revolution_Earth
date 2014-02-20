﻿using UnityEngine;
using System.Collections;

public class SoldierAnimations : MonoBehaviour {

	public Transform aimPivot;
	public Transform aimTarget;
	
	public float jumpAnimStretch = 5f;
	public float jumpLandCrouchAmount = 1.6f;
	
	private SoldierController soldier;
	private CharacterMotor motor;
	private float lastNonRelaxedTime;
	private float aimAngleY = 0.0f;
	
	private bool aim;
	private bool fire;
	private bool walk;
	private bool crouch;
	private Vector3 moveDir;
	private bool reloading;
	private int currentWeapon;
	private bool inAir;
	
	private float groundedWeight = 1f;
	private float crouchWeight = 0f;
	private float relaxedWeight = 1f;
	private float aimWeight = 0f;
	private float fireWeight = 0f;
	
	void OnEnable()
	{
		soldier = gameObject.GetComponent<SoldierController>();
		motor = gameObject.GetComponent<CharacterMotor>();
		GameObject go = GameObject.Find("SoldierTarget") as GameObject;
		
		if (aimTarget == null)
			aimTarget = go.transform;
		
		SetAnimationProperties();
	}
	
	void Update()
	{
		CheckSoldierState();
		
		if (crouch)
			crouchWeight = CrossFadeUp(crouchWeight, 0.4f);
		else if (inAir && jumpLandCrouchAmount > 0)
			crouchWeight = CrossFadeUp(crouchWeight, 1 / jumpLandCrouchAmount);
		else
			crouchWeight = CrossFadeDown(crouchWeight, 0.45f);
		var uprightWeight = 1 - crouchWeight;
		
		if (fire) {
			aimWeight = CrossFadeUp(aimWeight, 0.2f);
			fireWeight = CrossFadeUp(fireWeight, 0.2f);
		}
		else if (aim) {
			aimWeight = CrossFadeUp(aimWeight, 0.3f);
			fireWeight = CrossFadeDown(fireWeight, 0.3f);
		}
		else {
			aimWeight = CrossFadeDown(aimWeight, 0.5f);
			fireWeight = CrossFadeDown(fireWeight, 0.5f);
		}
		var nonAimWeight = (1 - aimWeight);
		var aimButNotFireWeight = aimWeight - fireWeight;
		
		if (inAir)
			groundedWeight = CrossFadeDown(groundedWeight, 0.1f);
		else
			groundedWeight = CrossFadeUp(groundedWeight, 0.2f);
		
		// Method that computes the idle timer to control IDLE and RELAXEDWALK animations
		if (aim || fire || crouch || !walk || (moveDir != Vector3.zero && moveDir.normalized.z < 0.8 ))
			lastNonRelaxedTime = Time.time;
		
		if (Time.time > lastNonRelaxedTime + 2)
			relaxedWeight = CrossFadeUp(relaxedWeight, 1.0f);
		else
			relaxedWeight = CrossFadeDown(relaxedWeight, 0.3f);
		float nonRelaxedWeight = 1 - relaxedWeight;
		
		animation["NormalGroup"].weight  = uprightWeight * nonAimWeight * groundedWeight * nonRelaxedWeight;
		animation["RelaxedGroup"].weight = uprightWeight * nonAimWeight * groundedWeight * relaxedWeight;
		animation["CrouchGroup"].weight  = crouchWeight  * nonAimWeight * groundedWeight;
		
		animation["NormalAimGroup"].weight = uprightWeight * aimButNotFireWeight * groundedWeight;
		animation["CrouchAimGroup"].weight = crouchWeight  * aimButNotFireWeight * groundedWeight;
		
		animation["NormalFireGroup"].weight = uprightWeight * fireWeight * groundedWeight;
		animation["CrouchFireGroup"].weight = crouchWeight  * fireWeight * groundedWeight;
		
		float runningJump = Mathf.Clamp01(Vector3.Dot(motor.movement.velocity, transform.forward) / 2.0f);
		animation["StandingJump"].weight = (1 - groundedWeight) * (1 - runningJump);
		animation["RunJump"].weight = (1 - groundedWeight) * runningJump;
		if (inAir) {
			//var normalizedTime = Mathf.Lerp(0.15, 0.65, Mathf.InverseLerp(jumpAnimStretch, -jumpAnimStretch, motor.movement.velocity.y));
			float normalizedTime = Mathf.InverseLerp(jumpAnimStretch, -jumpAnimStretch, motor.movement.velocity.y);
			animation["StandingJump"].normalizedTime = normalizedTime;
			animation["RunJump"].normalizedTime = normalizedTime;
		}
		
		//Debug.Log("motor.movement.velocity.y="+motor.movement.velocity.y+" - "+animation["StandingJump"].normalizedTime);
		
		float locomotionWeight = 1;
		locomotionWeight *= 1 - animation["Crouch"].weight;
		locomotionWeight *= 1 - animation["CrouchAim"].weight;
		locomotionWeight *= 1 - animation["CrouchFire"].weight;
		
		animation["LocomotionSystem"].weight = locomotionWeight;
		
		// Aiming up/down
		Vector3 aimDir = (aimTarget.position - aimPivot.position).normalized;
		var targetAngle = Mathf.Asin(aimDir.y) * Mathf.Rad2Deg;
		aimAngleY = Mathf.Lerp(aimAngleY, targetAngle, Time.deltaTime * 8);
				
		// Use additive animations for aiming when aiming and firing
		animation["StandingAimUp"].weight = uprightWeight * aimWeight;
		animation["StandingAimDown"].weight = uprightWeight * aimWeight;
		animation["CrouchAimUp"].weight = crouchWeight * aimWeight;
		animation["CrouchAimDown"].weight = crouchWeight * aimWeight;
		
		// Set time of animations according to current vertical aiming angle
		animation["StandingAimUp"].time = Mathf.Clamp01(aimAngleY / 90);
		animation["StandingAimDown"].time = Mathf.Clamp01(-aimAngleY / 90);
		animation["CrouchAimUp"].time = Mathf.Clamp01(aimAngleY / 90);
		animation["CrouchAimDown"].time = Mathf.Clamp01(-aimAngleY / 90);
		
		
		if(reloading)
		{
			animation.CrossFade("Reload" + soldier.currentWeaponName, 0.1f);
		}
		
		if(currentWeapon > 0 && fire)
		{
			animation.CrossFade("FireM203");
		}
	}
	
	float CrossFadeUp (float weight, float fadeTime) {
		return Mathf.Clamp01(weight + Time.deltaTime / fadeTime);
	}
	
	float CrossFadeDown (float weight, float fadeTime) {
		return Mathf.Clamp01(weight - Time.deltaTime / fadeTime);
	}
	
	public void CheckSoldierState()
	{
		aim = soldier.aim;
		fire = soldier.fire;
		walk = soldier.walk;
		crouch = soldier.crouch;
		reloading = soldier.reloading;
		currentWeapon = soldier.currentWeapon;
		moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		
		inAir = !GetComponent<CharacterController>().isGrounded;
	}
		
	//Method that initializes animations properties
	public void SetAnimationProperties()
	{
		animation.AddClip(animation["StandingReloadM4"].clip, "ReloadM4");
    	animation["ReloadM4"].AddMixingTransform(transform.Find("Pelvis/Spine1/Spine2"));
    	animation["ReloadM4"].wrapMode = WrapMode.Clamp;
    	animation["ReloadM4"].layer = 3;
    	animation["ReloadM4"].time = 0;
    	animation["ReloadM4"].speed = 1.0f;
    	
    	animation.AddClip(animation["StandingReloadRPG1"].clip, "ReloadM203");
    	animation["ReloadM203"].AddMixingTransform(transform.Find("Pelvis/Spine1/Spine2"));
    	animation["ReloadM203"].wrapMode = WrapMode.Clamp;
    	animation["ReloadM203"].layer = 3;
    	animation["ReloadM203"].time = 0;
    	animation["ReloadM203"].speed = 1.0f;
    	
    	animation.AddClip(animation["StandingFireRPG"].clip, "FireM203");
    	animation["FireM203"].AddMixingTransform(transform.Find("Pelvis/Spine1/Spine2"));
    	animation["FireM203"].wrapMode = WrapMode.Clamp;
    	animation["FireM203"].layer = 3;
    	animation["FireM203"].time = 0;
    	animation["FireM203"].speed = 1.0f;
    	
		animation["StandingJump"].layer = 2;
		animation["StandingJump"].weight = 0;
		animation["StandingJump"].speed = 0;
		animation["StandingJump"].enabled = true;
		animation["RunJump"].layer = 2;
		animation["RunJump"].weight = 0;
		animation["RunJump"].speed = 0;
		animation["RunJump"].enabled = true;
		animation.SyncLayer(2);
    	
    	SetupAdditiveAiming("StandingAimUp");
    	SetupAdditiveAiming("StandingAimDown");
    	SetupAdditiveAiming("CrouchAimUp");
    	SetupAdditiveAiming("CrouchAimDown");
	}
	
	public void SetupAdditiveAiming (string anim)
	{		
		animation[anim].blendMode = AnimationBlendMode.Additive;
    	animation[anim].enabled = true;
    	animation[anim].weight = 1;
    	animation[anim].layer = 4;
    	animation[anim].time = 0;
    	animation[anim].speed = 0;
	}
}	