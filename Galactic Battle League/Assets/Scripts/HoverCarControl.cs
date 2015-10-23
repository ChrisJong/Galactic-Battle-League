﻿using UnityEngine;
using System.Collections;
using InControl;
using UnityEngine.UI;
using System;
using System.IO;

public struct DamageData {
	public float damage;
	public Vector3 position;
	public int playerNumber;
	public float distance;
}

[RequireComponent(typeof(Rigidbody))]
public class HoverCarControl : MonoBehaviour
{
	Rigidbody m_body;
	private bool initialised = false;
	float m_deadZone = 0.1f;
	
	public float m_hoverForce = 9.0f;
	public float m_hoverHeight = 2.0f;
	public GameObject[] m_hoverPoints;
	
	public float m_forwardAcl = 100.0f;
	public float m_backwardAcl = 25.0f;
	float m_currThrust = 0.0f;
	
	public float m_sideAcl = 25.0f;
	float m_currSideThrust = 0.0f;
	
	public float m_turnStrength = 10f;
	float m_currTurn = 0.0f;
	
	public float m_aimStrength = 10f;
	float m_currAim = 0.0f;
	
	public GameObject m_leftAirBrake;
	public GameObject m_rightAirBrake;
	
	public float maxAbilityCharge;
	private float abilityCharge;
	public float abilityChargeRate;
	public float abilityUseRate;
	public float abilityPower;
	private EnergyCounter energyCounter;
	
	public int playerNumber; 
	public int tankClass;
	
	int m_layerMask;
	
	public GameObject sparkParticle;
	public AudioClip sfxBump;
	
	public AudioSource laserHitSound;
	
	public AudioSource dashSound;
	
	public ParticleSystem damage33;
	public ParticleSystem damage66;
	public ParticleSystem[] hoverParticles;
	public ParticleSystem baseParticle;
	private float particleLength;
	
	//Death/respawn variables
	private UIController uiController;
	private HealthCounter healthCounter;
	public AudioClip sfxDeath;
	Vector3 initialPosition;
	Quaternion initialRotation;
	public GameObject hitParticle;
	public ParticleSystem[] deathParticle;
	private double spawnActiveTimer;
	private float respawnTime = 4.0f;
	
	
	// Pickup variables
	public bool invincible = false;
	public float invincibilityTime;

	private GameObject respawnMessage;
	
	private float tempHoverForce;
	private float timer = 0.0f;
	private bool deathRun = false;
	public float maxHealth = 100;
	private float health;
	
	public bool hasRespawned = true;
	
	public Animator[] spawnAnimators;
	
	//Fire control variables
	public GameObject shot;
	public Transform[] shotSpawn;
	public ParticleSystem[] fireParticle;
	//Int declares which cannon to use
	private int spawnInt;
	public float fireRate;
	public AudioClip sfxFire;
	public AudioClip fireLoopEnd;
	public AudioSource weaponSound;
	public Vector3 tankVelocity;
	private float nextFire;
	public AudioClip sfxHit;
	public float explosionRadius = 4.0F;
	public float explosionPower = 25000.0F;
	private float maxError = 7.0f;
	private float currError = 0.0f;
	private float prevError = 0.0f;
	private float changeError = 0.0f;
	private float fireTime;
	
	private bool holdingTrigger;
	private float rumbleTimeHigh;
	private float rumbleTimeMed;
	private float rumbleTimeLow;

	public float rumbleHigh;
	public float rumbleMed;
	public float rumbleLow;
	
	public AudioClip killCheer = null;
	
	public GameObject laserBeam = null;
	
	private bool abilityActive = false;
	public float movingHoverPitch;
	public AudioSource hoverSound;
	
	private string fileName;
	private StreamWriter trackingFile;
	private float damageSinceLastPrint;
	
	private GameObject crosshairCentre;
	private GameObject[] crosshairs;
	private GameObject crosshairHit;
	private float crosshairHitDisappearTime = 0.0f;
	
	public Vector3 centerOfMass = Vector3.zero;
	public Vector3 inertiaTensor = new Vector3(1, 100, 1);
	
	private CameraController cameraController;
	
	void Start()
	{
		if (energyCounter == null) {
			energyCounter = GameObject.Find ("EnergyMeter" + playerNumber).GetComponent<EnergyCounter>();
		}
		if (uiController == null) {
			uiController = GameObject.Find("GameUIRoot").GetComponent<UIController>();
		}
		if (healthCounter == null) {
			healthCounter = GameObject.Find("HealthMeter" + playerNumber).GetComponent<HealthCounter>();
		}
		if (respawnMessage == null) {
			respawnMessage = GameObject.Find("P" + playerNumber + "_RESPAWN_MSG1");
		}
		crosshairCentre = GameObject.Find("P" + playerNumber + "_Crosshair");
		crosshairHit = GameObject.Find("P" + playerNumber + "_CrosshairHitMarker");
		crosshairHit.GetComponent<Image> ().enabled = false;
		crosshairs = new GameObject[4];
		crosshairs[0] = GameObject.Find("P" + playerNumber + "_CrosshairTL");
		crosshairs[1] = GameObject.Find("P" + playerNumber + "_CrosshairTR");
		crosshairs[2] = GameObject.Find("P" + playerNumber + "_CrosshairBL");
		crosshairs[3] = GameObject.Find("P" + playerNumber + "_CrosshairBR");
		
		cameraController = GameObject.Find ("Camera" + playerNumber).GetComponent<CameraController> ();
		crosshairCentre.GetComponent<Image> ().enabled = false;
		foreach(GameObject crosshair in crosshairs)
		{
			crosshair.GetComponent<Image>().enabled = false;
		}
		
		cameraController = transform.parent.GetComponentInChildren<CameraController> ();
		
		Directory.CreateDirectory("tracking");
		fileName = "tracking\\" + DateTime.Now.ToString("ddMMyyyyHHmm") + "damage.txt";
		trackingFile = new StreamWriter(fileName, true);
		trackingFile.Close ();
		damageSinceLastPrint = 0;
		
		foreach (Animator anim in spawnAnimators) 
		{
			anim.enabled = false;
		}
		
		health = maxHealth;
		abilityCharge = maxAbilityCharge;
		
		initialPosition = gameObject.transform.position;
		initialRotation = gameObject.transform.rotation;
		
		m_body = GetComponent<Rigidbody>();
		
		m_layerMask = 1 << LayerMask.NameToLayer("Characters");
		m_layerMask = ~m_layerMask;
		
		respawnMessage.SetActive(true);
		
		initialised = true;
		particleLength = hoverParticles[0].startLifetime;
		spawnInt = 0;
		holdingTrigger = false;
		nextFire = 0;
		
		if (centerOfMass != Vector3.zero)
			m_body.centerOfMass = centerOfMass;
		if (inertiaTensor != Vector3.zero) {
			m_turnStrength *= inertiaTensor.y;
			m_aimStrength *= inertiaTensor.x;
			
			inertiaTensor.x *= m_body.inertiaTensor.x;
			inertiaTensor.y *= m_body.inertiaTensor.y;
			inertiaTensor.z *= m_body.inertiaTensor.z;
			m_body.inertiaTensor = inertiaTensor;
		}
	}
	
	void OnDrawGizmos()
	{
		//  Hover Force
		RaycastHit hit;
		for (int i = 0; i < m_hoverPoints.Length; i++)
		{
			var hoverPoint = m_hoverPoints [i];
			if (Physics.Raycast(hoverPoint.transform.position, 
			                    -Vector3.up, out hit,
			                    m_hoverHeight, 
			                    m_layerMask))
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawLine(hoverPoint.transform.position, hit.point);
				Gizmos.DrawSphere(hit.point, 0.5f);
			} 
			else
			{
				Gizmos.color = Color.red;
				Gizmos.DrawLine(hoverPoint.transform.position, 
				                hoverPoint.transform.position - Vector3.up * m_hoverHeight);
			}
		}
	}
	
	void Update()
	{
		if (crosshairHitDisappearTime < Time.time)
			crosshairHit.GetComponent<Image> ().enabled = false;
		
		hoverSound.pitch = 1;
		var inputDevice = (InputManager.Devices.Count + 1 > playerNumber) ? InputManager.Devices[playerNumber - 1] : null;
		healthCounter.health = health;
		healthCounter.maxHealth = maxHealth;
		energyCounter.energy = abilityCharge;
		energyCounter.maxEnergy = maxAbilityCharge;
		
		//camera
		if (inputDevice != null) {
			
		}
		
		if (hasRespawned && inputDevice != null) 
		{
			if (inputDevice.RightTrigger.IsPressed) 
			{
				foreach (Animator anim in spawnAnimators)
				{
					anim.enabled = true;
					anim.Play(0, -1, 0f);
				}
				
				cameraController.ChangeMode();
				crosshairCentre.GetComponent<Image> ().enabled = true;
				foreach(GameObject crosshair in crosshairs)
				{
					crosshair.GetComponent<Image>().enabled = true;
				}
				respawnMessage.SetActive(false);
				spawnActiveTimer = Time.time + 1.0f;
				hasRespawned = false;
			}
		}
		else if (!deathRun && inputDevice != null && spawnActiveTimer < Time.time) 
		{
			
			foreach(ParticleSystem particle in hoverParticles)
				particle.startLifetime = particleLength;
			
			// Main Thrust
			m_currThrust = 0.0f;
			float aclAxis = inputDevice.Direction.Y;
			if (aclAxis > m_deadZone)
			{
				foreach(ParticleSystem particle in hoverParticles)
					particle.startLifetime = particleLength*2;
				m_currThrust = aclAxis * m_forwardAcl;
				hoverSound.pitch = movingHoverPitch;
			}
			else if (aclAxis < -m_deadZone)
			{
				foreach(ParticleSystem particle in hoverParticles)
					particle.startLifetime = particleLength*0.5f;
				m_currThrust = aclAxis * m_backwardAcl;
				hoverSound.pitch = movingHoverPitch;
			}
			
			// Side Thrust
			m_currSideThrust = 0.0f;
			float aclSideAxis = inputDevice.Direction.X;
			if (aclSideAxis > m_deadZone)
			{
				m_currSideThrust = aclSideAxis * m_sideAcl;
				hoverSound.pitch = movingHoverPitch;
			}
			else if (aclSideAxis < -m_deadZone)
			{
				m_currSideThrust = aclSideAxis * m_sideAcl;
				hoverSound.pitch = movingHoverPitch;
			}
			
			// Turning
			m_currTurn = 0.0f;
			float turnAxis = inputDevice.RightStickX * inputDevice.RightStickX * inputDevice.RightStickX;
			if (Mathf.Abs (turnAxis) > m_deadZone)
				m_currTurn = turnAxis;
			
			// up/down aiming
			m_currAim = 0.0f;
			float aimAxis = inputDevice.RightStickY * inputDevice.RightStickY * inputDevice.RightStickY;
			if (Mathf.Abs(aimAxis) > m_deadZone)
				m_currAim = aimAxis;
			
			
			// Firing
			if (inputDevice.RightTrigger.IsPressed && Time.time > nextFire) 
			{
				if (fireTime + 0.6 > Time.time)
					holdingTrigger = true;
				nextFire = Time.time + fireRate;
				if (tankClass == 1)
				{
					Rumble(0.15f, 0);
					cameraController.RunQuake(currError/700.0f);
				}
				else
				{
					Rumble (0.3f, 2);
					cameraController.RunQuake(0.015f);
					cameraController.RunShockwave(shotSpawn[spawnInt].position);
				}
				gameObject.GetComponent<Rigidbody>().AddExplosionForce(explosionPower, shotSpawn[spawnInt].position, explosionRadius);
				tankVelocity = GetComponent<Rigidbody>().velocity;
				fireParticle[spawnInt].Play();
				createShot (tankVelocity);
				if (holdingTrigger == false)
					AudioSource.PlayClipAtPoint (sfxFire, shotSpawn[spawnInt].position, 1);
				else
					if (!weaponSound.isPlaying)
						weaponSound.Play ();
				spawnInt++;
				if (spawnInt >= shotSpawn.Length)
					spawnInt = 0;
				fireTime = Time.time;
			}
			
			
			//Firing cancellation
			if (!inputDevice.RightTrigger.IsPressed && weaponSound && Time.time > nextFire && tankClass == 1)
			{
				if (weaponSound.isPlaying)
				{
					holdingTrigger = false;
					weaponSound.Stop();
					AudioSource.PlayClipAtPoint(fireLoopEnd, shotSpawn[spawnInt].position, 1);
				}
			}
			
			if ((!abilityActive || abilityCharge <= 0f) && weaponSound && tankClass == 2)
			{
				if (weaponSound.isPlaying)
				{
					holdingTrigger = false;
					weaponSound.Stop();
					AudioSource.PlayClipAtPoint(fireLoopEnd, shotSpawn[spawnInt].position, 1);
				}
			}
			
			//Ability
			if (inputDevice.LeftTrigger.IsPressed)
				abilityActive = true;
			else
				abilityActive = false;
			
			if (inputDevice.Action4.WasPressed) {
				cameraController.ChangeMode();
			}
		}
		else if(deathRun && weaponSound && Time.time > nextFire)
		{
			if (weaponSound.isPlaying)
			{
				holdingTrigger = false;
				weaponSound.Stop();
				AudioSource.PlayClipAtPoint(fireLoopEnd, shotSpawn[spawnInt].position, 1);
			}
		}
		
		if (currError > 0 && !inputDevice.RightTrigger.IsPressed) 
			currError -= 4.0f * Time.deltaTime;
		
		if (currError < 0)
			currError = 0;
		
		if (crosshairs[0]) 
		{
			changeError = currError - prevError;
			prevError = currError;
			
			crosshairs[0].transform.position = new Vector3(crosshairs[0].transform.position.x - changeError, crosshairs[0].transform.position.y + (changeError/2));
			crosshairs[1].transform.position = new Vector3(crosshairs[1].transform.position.x + changeError, crosshairs[1].transform.position.y + (changeError/2));
			crosshairs[2].transform.position = new Vector3(crosshairs[2].transform.position.x - changeError, crosshairs[2].transform.position.y - (changeError/2));
			crosshairs[3].transform.position = new Vector3(crosshairs[3].transform.position.x + changeError, crosshairs[3].transform.position.y - (changeError/2));
			
			
			foreach(GameObject crosshair in crosshairs)
			{
				crosshair.GetComponent<RectTransform>().localScale = new Vector3 (0.75f + (currError / 10.0f), 0.75f + (currError / 20.0f), 0.5f);
				crosshair.GetComponent<Image>().color = new Color(1, (255-(currError*15))/255, (255-(currError*30))/255);
			}
		}

		if (invincible && invincibilityTime < Time.time)
			invincible = false;
	}
	
	void FixedUpdate()
	{
		
		//  Hover Force
		RaycastHit hit;
		for (int i = 0; i < m_hoverPoints.Length; i++)
		{
			var hoverPoint = m_hoverPoints [i];
			if (Physics.Raycast(hoverPoint.transform.position, 
			                    -Vector3.up, out hit,
			                    m_hoverHeight,
			                    m_layerMask))
				m_body.AddForceAtPosition(Vector3.up 
				                          * m_hoverForce
				                          * (1.0f - (hit.distance / m_hoverHeight)), 
				                          hoverPoint.transform.position);
			else
			{
				if (transform.position.y > hoverPoint.transform.position.y)
					m_body.AddForceAtPosition(
						hoverPoint.transform.up * m_hoverForce,
						hoverPoint.transform.position);
				else
					m_body.AddForceAtPosition(
						hoverPoint.transform.up * -m_hoverForce,
						hoverPoint.transform.position);
			}
		}
		
		// Forward
		if (Mathf.Abs(m_currThrust) > 0)
			m_body.AddForce(transform.forward * m_currThrust);
		
		
		// Sideways
		if (Mathf.Abs(m_currSideThrust) > 0)
			m_body.AddForce(transform.right * m_currSideThrust);
		
		// Turn
		if (m_currTurn > 0)
		{
			m_body.AddRelativeTorque(Vector3.up * m_currTurn * m_turnStrength);
		} else if (m_currTurn < 0)
		{
			m_body.AddRelativeTorque(Vector3.up * m_currTurn * m_turnStrength);
		}
		
		// up down aiming
		
		if (m_currAim > 0) {
			m_body.AddTorque (-transform.right * m_currAim * m_aimStrength);
		} else if (m_currAim < 0) {
			m_body.AddTorque (-transform.right * m_currAim * m_aimStrength);
		}
		
		
		// ability stuff
		if (abilityActive && abilityCharge > 0f) {
			abilityCharge -= abilityUseRate * Time.deltaTime;
			
			if (tankClass == 1) {
				//Boost ability
				m_body.AddForce (transform.forward * m_currThrust * abilityPower);
				m_body.AddForce (transform.right * m_currSideThrust * abilityPower);
				if (!dashSound.isPlaying)
					dashSound.Play();
				cameraController.RunQuake (0.01f);
				foreach (ParticleSystem particle in hoverParticles) {
					particle.startLifetime = particleLength * 4;
				}
				cameraController.RunDash ();
			} else {
				//Laser ability
				var layermask = 1 << 12;
				layermask = ~layermask;
				bool hitWall = false;
				cameraController.RunQuake (0.003f);
				cameraController.RunLaser ();
				if (Physics.Raycast (shotSpawn [0].position, shotSpawn [0].forward, out hit, Mathf.Infinity, layermask)) {
					Debug.DrawLine (shotSpawn [0].position, hit.point, Color.cyan);
					
					//draw laser
					
					laserBeam.transform.localScale = new Vector3 (laserBeam.transform.localScale.x, hit.distance / 2, laserBeam.transform.localScale.z);
					laserBeam.transform.position = (shotSpawn [0].position + hit.point) / 2;
					
					if (!laserBeam.activeInHierarchy) {
						laserBeam.SetActive(true);
					}
					
					//sound
					if (!weaponSound.isPlaying)
						weaponSound.Play ();
					
					RaycastHit[] hits;
					hits = Physics.RaycastAll (shotSpawn [0].position, shotSpawn [0].forward, hit.distance);
					for (int i = 0; i < hits.Length; i++) {
						if (hits [i].collider.tag == "Player") {
							DamageData damageData;
							damageData.damage = abilityPower * Time.deltaTime;
							if (hits [i].collider.GetComponent<HoverCarControl> ().invincible)
								damageData.damage = 0;

							damageData.position = hits [i].point;
							damageData.playerNumber = playerNumber;
							damageData.distance = 0;
							ShowHitMarker( hits[i].collider.gameObject.GetComponent<HoverCarControl>().playerNumber);
							hits [i].collider.gameObject.SendMessage ("Damage", damageData);
							Rumble (0.05f, 1);
							//AudioSource.PlayClipAtPoint(laserHitSound, transform.position, laserHitVolume);
						}
					}
				} else {
					laserBeam.transform.localScale = new Vector3 (laserBeam.transform.localScale.x, 500, laserBeam.transform.localScale.z);
					laserBeam.transform.position = (shotSpawn [0].position + shotSpawn [0].forward * 500);
				}
			}
		} 
		else 
		{
			cameraController.StopLaser ();
			if (laserBeam != null) {
				if (laserBeam.activeInHierarchy) {
					laserBeam.SetActive(false);
				}
			}
		}
		
		if (!abilityActive && abilityCharge <= maxAbilityCharge) {
			abilityCharge += abilityChargeRate * Time.deltaTime;
		}
		if (abilityCharge > maxAbilityCharge) {
			abilityCharge = maxAbilityCharge;
		}
		
		// Rumble
		var inputDevice = (InputManager.Devices.Count + 1 > playerNumber) ? InputManager.Devices[playerNumber - 1] : null;
		if (inputDevice != null && Time.time < rumbleTimeHigh) {
			inputDevice.Vibrate (rumbleHigh, rumbleHigh);
		} else if (inputDevice != null && Time.time < rumbleTimeMed) {
			inputDevice.Vibrate (rumbleMed, rumbleMed);
		} else if (inputDevice != null && Time.time < rumbleTimeLow) {
		inputDevice.Vibrate (rumbleLow, rumbleLow);
		}else if (inputDevice != null) {
			inputDevice.Vibrate(0.0f, 0.0f);
		}
		
		if (gameObject.transform.position.y <= -100) 
		{
			Death ();
			Respawn ();
		}
		
		if (health <= 0) 
		{
			if (!deathRun)
				Death ();
			timer += Time.deltaTime;
			if(timer > respawnTime)
			{
				Respawn();
			}
			else if(timer>respawnTime - 1.5)
			{
				cameraController.RunDeath();
			}
		}
	}
	
	void OnTriggerEnter (Collider other)
	{
		if (deathRun == true)
			return;
		if (other.tag == "Shot")
		{
			ShotController shotControllerCopy = other.gameObject.GetComponent<ShotController>();
			if (shotControllerCopy.playerNumber != playerNumber)
			{
				AudioSource.PlayClipAtPoint(sfxHit, gameObject.transform.position, 0.25f);
				other.SendMessage("Hit", playerNumber);
				//Vector3 explosionPos = other.gameObject.transform.position;
				//Collider[] colliders = Physics.OverlapSphere(explosionPos, explosionRadius);
				
				//Destroy other shot and smoke trail properly
				ParticleSystem smoke = other.GetComponentInChildren<ParticleSystem> ();
				smoke.enableEmission = false;
				smoke.transform.parent=null;
				Destroy(smoke, 3);
				Destroy (other.gameObject);
				
				DamageData damageData;
				damageData.damage = shotControllerCopy.damage;
				if (invincible)
					damageData.damage = 0;
				damageData.position = other.transform.position;
				damageData.playerNumber = shotControllerCopy.playerNumber;
				damageData.distance = Vector3.Distance(shotControllerCopy.startPoint, other.transform.position);
				
				Damage(damageData);
			}
		}
		if (other.tag == "Pickup") 
		{
			ProcessPickup(other);
		}
	}
	
	void OnCollisionEnter(Collision collision)
	{
		if (sfxBump) 
		{
			AudioSource.PlayClipAtPoint(sfxBump, this.transform.position, 2);
		}
		
		foreach (ContactPoint contact in collision.contacts) 
		{
			if (contact.otherCollider.tag != "Shot" + playerNumber)
			{
				ParticleEmitter sparks = ((GameObject)Instantiate(sparkParticle, contact.point, shotSpawn[0].rotation)).GetComponent<ParticleEmitter>();
				sparks.Emit();
				Rumble(0.1f, 1);
			}
		}
	}
	
	void createShot(Vector3 tankVelocity)
	{
		Quaternion shotAngle = shotSpawn[spawnInt].rotation;
		if (tankClass == 1) 
		{
			Vector2 error = UnityEngine.Random.insideUnitCircle * currError;
			Quaternion errorRotation = Quaternion.Euler(error.x/2, error.y, 0);
			shotAngle = shotAngle * errorRotation;
			if (currError < maxError)
				currError += 0.5f;
		}
		GameObject zBullet = (GameObject)Instantiate (shot, shotSpawn[spawnInt].position, shotAngle);
		ShotController sController = zBullet.GetComponent<ShotController> ();
		sController.SetVelocity ();
		sController.SetTank (this);
	}
	
	public void ShowHitMarker (int otherPlayerNumber)
	{
		if (otherPlayerNumber == 1)
			crosshairHit.GetComponent<Image>().color = new Color(1,0,0);
		if (otherPlayerNumber == 2)
			crosshairHit.GetComponent<Image>().color = new Color(0.133f ,0.3125f,1);
		if (otherPlayerNumber == 3)
			crosshairHit.GetComponent<Image>().color = new Color(0.309f,1,0.309f);
		if (otherPlayerNumber == 4)
			crosshairHit.GetComponent<Image>().color = new Color(1,1,0.176f);
		crosshairHit.GetComponent<Image> ().enabled = true;
		crosshairHitDisappearTime = Time.time + 0.3f;
	}
	
	void Death()
	{
		cameraController.RunSignalJammed();
		abilityCharge = 0f;
		tempHoverForce = m_hoverForce;
		m_hoverForce = 0.0f;
		
		m_currThrust = 0.0f;
		m_currSideThrust = 0.0f;
		m_currTurn = 0.0f;
		
		AudioSource.PlayClipAtPoint(sfxDeath, gameObject.transform.position, 1);
		if (killCheer)
			AudioSource.PlayClipAtPoint(killCheer, gameObject.transform.position, 1f);
		timer = 0.0f;
		deathRun = true;
		Vector3 point = gameObject.transform.position;
		point.y += 4;
		
		foreach (ParticleSystem deathExplosion in deathParticle)
			deathExplosion.Play();
		
		foreach (ParticleSystem particle in hoverParticles) 
		{
			particle.Stop();
		}
		baseParticle.Stop ();
		
		currError = 0;
		if (crosshairs[0]) 
		{
			changeError = currError - prevError;
			prevError = currError;
			
			crosshairs[0].transform.position = new Vector3(crosshairs[0].transform.position.x - changeError, crosshairs[0].transform.position.y + (changeError/2));
			crosshairs[1].transform.position = new Vector3(crosshairs[1].transform.position.x + changeError, crosshairs[1].transform.position.y + (changeError/2));
			crosshairs[2].transform.position = new Vector3(crosshairs[2].transform.position.x - changeError, crosshairs[2].transform.position.y - (changeError/2));
			crosshairs[3].transform.position = new Vector3(crosshairs[3].transform.position.x + changeError, crosshairs[3].transform.position.y - (changeError/2));
			
			
			crosshairCentre.GetComponent<Image> ().enabled = false;
			foreach(GameObject crosshair in crosshairs)
			{
				crosshair.GetComponent<RectTransform>().localScale = new Vector3 (0.75f + (currError / 10.0f), 0.75f + (currError / 20.0f), 0.5f);
				crosshair.GetComponent<Image>().color = new Color(1, (255-(currError*15))/255, (255-(currError*30))/255);
				crosshair.GetComponent<Image>().enabled = false;
			}
		}
	}
	
	void Respawn()
	{
		if (damage33) 
		{
			damage33.Stop ();
			damage33.Clear ();
		}
		if (damage66) 
		{
			damage66.Stop ();
		}
		foreach (ParticleSystem deathExplosion in deathParticle) 
		{
			deathExplosion.Stop ();
		}
		
		gameObject.transform.position = initialPosition;
		gameObject.transform.rotation = initialRotation;
		m_body.velocity = Vector3.zero;
		m_body.angularVelocity = Vector3.zero;
		m_hoverForce = tempHoverForce;
		foreach (ParticleSystem particle in hoverParticles) 
		{
			if (particle)
				particle.Play();
		}
		if (baseParticle)
			baseParticle.Play ();
		health = maxHealth;
		deathRun = false;
		hasRespawned = true;
		
		respawnMessage.SetActive(true);
		abilityCharge = maxAbilityCharge;
		abilityActive = false;
		invincible = false;
		cameraController.StopSignalJammed();
		cameraController.RunRespawn();
		cameraController.StopLowHealth ();
	}
	
	void Rumble(float duration, int intensity = 2) {
		if (rumbleTimeHigh < Time.time + duration && intensity == 2){
			rumbleTimeHigh = Time.time + duration;
		}else if (rumbleTimeMed < Time.time + duration && intensity == 1){
			rumbleTimeMed = Time.time + duration;
		}else if (rumbleTimeLow < Time.time + duration && intensity == 0){
			rumbleTimeLow = Time.time + duration;
		}
	}
	
	void Damage(DamageData damageData) {
		if (deathRun)
			return;
		else {
			ParticleSystem hitExplosion = ((GameObject)Instantiate (hitParticle, damageData.position, shotSpawn [0].rotation)).GetComponent<ParticleSystem> ();
			
			//hitExplosion.startLifetime = (float)shotControllerCopy.damage/10.0f;
			if (damageData.damage >= 10)
			{
				hitExplosion.startSize = 6;
				Rumble (0.3f,2);
				cameraController.RunQuake(0.015f);
			}
			else if (damageData.damage >=2)
			{
				hitExplosion.startSize = 3;
				Rumble (0.15f,1);
				cameraController.RunQuake(0.008f);
			}
			else 
			{
				hitExplosion.startSize = 1;
				Destroy(hitExplosion.transform.GetChild(0).gameObject);
				Rumble (0.05f,0);
				cameraController.RunQuake(0.005f);
				if(!laserHitSound.isPlaying)
					laserHitSound.Play();
			}
			
			hitExplosion.Play ();
			//foreach (Collider hit in colliders) 
			//{
			// if (hit && hit.GetComponent<Rigidbody>())
			// hit.GetComponent<Rigidbody>().AddExplosionForce(explosionPower, explosionPos, explosionRadius);
			
			//}
			health -= damageData.damage;
			
			if (damageData.damage >= 10)
				Rumble (0.3f, 2);
			else if (damageData.damage >= 2)
				Rumble (0.15f,1);
			else if (damageData.damage > 0)
				Rumble (0.05f,0);
			
			float healthRatio = health/maxHealth;
			if (healthRatio < .33f)
				if (damage66)
				{
				damage66.emissionRate = (int)(1/healthRatio*20);
				damage66.maxParticles = (int)(1/healthRatio*200);
					damage66.Play ();
				}
			if (healthRatio < .5f && healthRatio > .33f)
			{
				//linear transparency
				//50% health is 0 transparency
				//33% health is 100% transparency
				//y = -5.882x + 2.941
				float transparency =-5.882f* (healthRatio) + 2.941f;
				cameraController.RunLowHealth(transparency);
			}
			if (healthRatio < .66f)
			{
				if (damage33)
					damage33.Play ();
			}
			if (health <= 0) {
				health = 0;
				uiController.PlayerKill (damageData.playerNumber, (int)damageData.damage, playerNumber, tankClass);
			}
			damageSinceLastPrint += damageData.damage;
			if (damageSinceLastPrint >= 1)
			{
				trackingFile = new StreamWriter(fileName, true);
				trackingFile.WriteLine(playerNumber.ToString() + "\t" + damageData.playerNumber.ToString() + "\t" + damageSinceLastPrint.ToString() + "\t" + damageData.distance.ToString());
				trackingFile.Close ();
				uiController.DamageCaused(damageData.playerNumber, (int)damageSinceLastPrint);
				damageSinceLastPrint = 0;
			}
			if (damageData.damage > 0)
				cameraController.RunGlitch();
		}
	}
	
	void ProcessPickup(Collider pickup)
	{
		string pickupType = pickup.name;
		cameraController.RunPickup ();
		if (pickupType == "PickUps_HP(Clone)")
		{
			ProcessHealthPickup (20f);
			uiController.PickupTaken(playerNumber, "REPAIRED");
			pickup.GetComponent<PickupController>().podium.pickupTaken();
			ParticleSystem smoke = pickup.GetComponentInChildren<ParticleSystem> ();
			smoke.enableEmission = true;
			smoke.Play();
			smoke.transform.parent=null;
			Destroy(smoke, 3);
			Destroy(pickup.gameObject);
		}
	}
	
	void ProcessHealthPickup(float healthValue)
	{
		health += healthValue;
		if (health >= maxHealth)
			health = maxHealth;
		Rumble (0.15f,0);
		
		float healthRatio = health / maxHealth;
		if (healthRatio > .33f) 
		{
			if (damage66)
				damage66.Stop ();
		}
		else 
		{
			if (damage66)
			{
				damage66.emissionRate = (int)(1/healthRatio*20);
				damage66.maxParticles = (int)(1/healthRatio*200);
				damage66.Play();
			}
		}
		if (healthRatio > 0.5f)
			cameraController.StopLowHealth ();
		if (healthRatio < .5f && healthRatio > .33f)
		{
			//linear transparency
			//50% health is 0 transparency
			//33% health is 100% transparency
			//y = -5.882x + 2.941
			float transparency =-5.882f* (healthRatio) + 2.941f;
			cameraController.RunLowHealth(transparency);
		}
		if (healthRatio > .66f)
		{
			if (damage33)
				damage33.Stop ();
		}
	}
	
	void OnEnable()
	{
		if (initialised) {
			gameObject.transform.position = initialPosition;
			gameObject.transform.rotation = initialRotation;
			health = maxHealth;
		}
	}
	void OnDisable()
	{
		var inputDevice = (InputManager.Devices.Count + 1 > playerNumber) ? InputManager.Devices[playerNumber - 1] : null;
		rumbleTimeHigh = 0.0f;
		rumbleTimeMed = 0.0f;
		rumbleTimeLow = 0.0f;
		if (inputDevice != null) {
			inputDevice.Vibrate(0.0f, 0.0f);
		}
	}
}
