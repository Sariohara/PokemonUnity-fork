﻿//Original Scripts by IIColour (IIColour_Spectrum)

using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace PokemonUnity.Legacy
{
	public class NPCHandler : MonoBehaviour
	{
		public int pokemonID = 0;

		public enum NPCBehaviour
		{
			Idle,
			Walk,
			Patrol
		}

		public NPCBehaviour npcBehaviour;

		public Texture2D sprite;
		public string npcSpriteName;
		private UnityEngine.Sprite[] spriteSheet;
		private UnityEngine.Sprite[] lightSheet;

		public WalkCommand[] patrol = new WalkCommand[1];
		public Vector2 walkRange;
		private Vector3 initialPosition;

		public bool animated = false;
		public bool trainerSurfing = false;

		public bool busy = false;
		private bool overrideBusy = false;

		private SpriteRenderer pawnSprite;
		private SpriteRenderer pawnReflectionSprite;
		private SpriteRenderer pawnLightSprite;
		private SpriteRenderer pawnLightReflectionSprite;

		private Light npcLight;

		public Transform hitBox;

		public int direction = 2;

		public MapCollider currentMap;
		public MapCollider destinationMap;

		private GameObject exclaim;

		private int frame;
		private int frames;
		public int framesPerSec;
		private float secPerFrame;
		public bool animPause = true;

		private int modulo;


		void Awake()
		{
			pawnSprite = transform.Find("Pawn").GetComponent<SpriteRenderer>();
		
			pawnReflectionSprite = transform.Find("PawnReflection").GetComponent<SpriteRenderer>();

			modulo = 2;

			if (pokemonID != 0)
			{
				pawnLightSprite = pawnSprite.transform.Find("PawnLight").GetComponent<SpriteRenderer>();
				pawnLightReflectionSprite = pawnReflectionSprite.transform.Find("PawnLightReflection").GetComponent<SpriteRenderer>();
				npcLight = transform.Find("Point light").GetComponent<Light>();

				modulo = 2;
			}

			hitBox = transform.Find("NPC_Object");

			if (hitBox == null)
			{
				hitBox = transform.Find("NPC_Transparent");
			}
		
			if (pokemonID == 0)
			{
				spriteSheet = Resources.LoadAll<UnityEngine.Sprite>("OverworldNPCSprites/" + npcSpriteName);
			}
			else
			{
				//spriteSheet = Pokemon.GetNewSpriteFromID(pokemonID, false, false);

				npcLight.intensity = PokemonDatabase.getPokemon(pokemonID).getLuminance();
				npcLight.color = PokemonDatabase.getPokemon(pokemonID).getLightColor();
				//lightSheet = Pokemon.GetNewSpriteFromID(pokemonID, false, true);
			}

			exclaim = transform.Find("Exclaim").gameObject;
		}

		void Start()
		{
			initialPosition = hitBox.position;

			hitBox.localPosition = new Vector3(0, 0, 0);

			exclaim.SetActive(false);

			if (animated) animPause = false;
		
			StartCoroutine("animateSprite");


			//Check current map
			RaycastHit[] hitRays = Physics.RaycastAll(transform.position + Vector3.up, Vector3.down);
			int closestIndex = -1;
			float closestDistance = float.PositiveInfinity;
			if (hitRays.Length > 0)
			{
				for (int i = 0; i < hitRays.Length; i++)
				{
					if (hitRays[i].collider.gameObject.GetComponent<MapCollider>() != null)
					{
						if (hitRays[i].distance < closestDistance)
						{
							closestDistance = hitRays[i].distance;
							closestIndex = i;
						}
					}
				}
			}
			if (closestIndex != -1)
			{
				currentMap = hitRays[closestIndex].collider.gameObject.GetComponent<MapCollider>();
			}
			else
			{
				Debug.Log("no map found for: " + gameObject.name);
			}


			if (npcBehaviour == NPCBehaviour.Walk)
			{
				StartCoroutine("walkAtRandom");
			}
			else if (npcBehaviour == NPCBehaviour.Patrol)
			{
				StartCoroutine("patrolAround");
			}
		}

	
		//Jump animation
		public IEnumerator jump(float speed, AudioClip jumpClip, bool hasLandClip)
		{
			if (speed == 0)
				speed = 1;
		
			float increment = 0f;
			float parabola = 0;
			float height = 2.1f;
			Vector3 startPosition = pawnSprite.transform.position;
		
			if (jumpClip != null)
				SfxHandler.Play(jumpClip);

			while (increment < 1)
			{
				increment += (1 * speed*2) * Time.deltaTime; //TODO make walkspeed modulable
				if (increment > 1)
				{
					increment = 1;
				}
				parabola = -height * (increment * increment) + (height * increment);
				pawnSprite.transform.position = new Vector3(pawnSprite.transform.position.x, startPosition.y + parabola, pawnSprite.transform.position.z);
				yield return null;
			}
			pawnSprite.transform.position = new Vector3(pawnSprite.transform.position.x, startPosition.y, pawnSprite.transform.position.z);

			if (hasLandClip)
			{
				SfxHandler.Play(PlayerMovement.player.landClip);
			}
		}

		//Better exclaimation not yet implemented
		public IEnumerator exclaimAnimation()
		{
			float increment = -1f;
			float speed = 0.15f;

			exclaim.SetActive(true);

			while (increment < 0.3f)
			{
				increment += (1 / speed) * Time.deltaTime;
				if (increment > 0.3f)
				{
					increment = 0.3f;
				}
				exclaim.transform.localScale = new Vector3(1, 1.3f + (-1.3f * increment * increment), 1);
				yield return null;
			}

			exclaim.transform.localScale = new Vector3(1, 1, 1);

			yield return new WaitForSeconds(1.2f);
			exclaim.SetActive(false);
		}

		private IEnumerator animateSprite()
		{
			if (pokemonID == 0)
			{
				frame = 0;
				frames = 4;
				if (framesPerSec == 0)
				{
					framesPerSec = 7;
				}
				secPerFrame = 1f / (float) framesPerSec;
				while (true)
				{
					for (int i = 0; i < 4; i++)
					{
						while (PlayerMovement.player.busyWith != null && PlayerMovement.player.busyWith != this.gameObject &&
							   !overrideBusy)
						{
							yield return null;
						}
						if (animPause && frame % modulo != 0)
						{
							frame -= 1;
						}
						pawnSprite.sprite = spriteSheet[direction%4 * frames + frame];
						pawnReflectionSprite.sprite = pawnSprite.sprite;
						yield return new WaitForSeconds(secPerFrame / 4f);
					}
					if (!animPause)
					{
						frame += 1;
						if (frame >= frames)
						{
							frame = 0;
						}
					}
				}
			}
			else
			{
				frame = 0;
				while (true)
				{
					for (int i = 0; i < 6; i++)
					{
						pawnSprite.sprite = spriteSheet[direction * modulo + frame];
						pawnLightSprite.sprite = lightSheet[direction * modulo + frame];

						pawnReflectionSprite.sprite = pawnSprite.sprite;
						pawnLightReflectionSprite.sprite = pawnLightSprite.sprite;
						if (i > 2)
						{
							pawnSprite.transform.localPosition = new Vector3(0, 0.17f, -0.36f);
							pawnLightSprite.transform.localPosition = new Vector3(0, 0.171f, -0.36f);
						}
						else
						{
							pawnSprite.transform.localPosition = new Vector3(0, 0.2f, -0.305f);
							pawnLightSprite.transform.localPosition = new Vector3(0, 0.201f, -0.305f);
						}
						yield return new WaitForSeconds(0.055f);
					}
					frame = (frame == 0) ? 1 : 0;
				}
			}
		}

		private void LateUpdate()
		{
			float scale;

			Transform cam = PlayerMovement.player.transform.Find("Camera") != null
				? PlayerMovement.player.transform.Find("Camera")
				: GameObject.Find("Camera").transform;

			Vector3 position = cam.position - PlayerMovement.player.getCamOrigin();

			if (transform.position.z > position.z)
			{
				scale = 0.0334f * (Math.Abs(transform.position.z - position.z))+0.9f;
				if (transform.position.z > position.z + 3)
				{
					scale = 1;
				}
			}
			else
			{
				scale = 0.9f;
			}
		
			//scale = 0.0334f * (transform.position.z - PlayerMovement.player.transform.position.z)+0.9f;
		
			pawnSprite.transform.localScale = new Vector3(scale,scale,scale);
		
			Camera camera = PlayerMovement.player.transform.Find("Camera") != null
				? PlayerMovement.player.transform.Find("Camera").GetComponent<Camera>()
				: GameObject.Find("Camera").GetComponent<Camera>();
		
			pawnSprite.transform.LookAt(camera.transform);

			//if (PlayerMovement.player.transform.Find("Camera") == null)
			pawnSprite.transform.localRotation = Quaternion.Euler(camera.transform.rotation.x-50, 180, 0);
		
			pawnSprite.transform.Rotate( new Vector3(0, 180, 0), Space.Self );
		
			//pawnSprite.transform.rotation = PlayerMovement.player.transform.Find("Pawn").transform.rotation;
			
		}

		public void setFrameStill()
		{
			if (frame % modulo != 0)
			{
				frame -= 1;
			}
			pawnSprite.sprite = spriteSheet[direction * frames + frame];
			pawnReflectionSprite.sprite = pawnSprite.sprite;
		}

		public void setDirection(int newDirection)
		{
			direction = newDirection;
			pawnSprite.sprite = spriteSheet[direction * frames + frame];
			pawnReflectionSprite.sprite = pawnSprite.sprite;
		}

		private IEnumerator walkAtRandom()
		{
			float waitTime;
			int newDirection;
			int walkDistance;
			while (true)
			{
				while (!busy)
				{
					waitTime = Random.Range(-0.8f, 1.6f);
					waitTime = 1.1f + (waitTime * waitTime * waitTime);

					newDirection = Random.Range(0, 4);
					walkDistance = Random.Range(0, 5);
					if (walkDistance > 1)
					{
						//make movements of 1 more likely than others. 
						walkDistance -= 1;
					}

					while (busy)
					{
						yield return null;
					}
					direction = newDirection;
					yield return null; //2 frame delay to prevent taking a step before initialising battle.
					yield return null;

					//walk
					for (int i = 0; i < walkDistance; i++)
					{
						bool atEdge = false;
						if (newDirection == 0)
						{
							if (hitBox.position.z >= (initialPosition.z + walkRange.y))
							{
								atEdge = true;
							}
						}
						else if (newDirection == 1)
						{
							if (hitBox.position.x >= (initialPosition.x + walkRange.x))
							{
								atEdge = true;
							}
						}
						else if (newDirection == 2)
						{
							if (hitBox.position.z <= (initialPosition.z - walkRange.y))
							{
								atEdge = true;
							}
						}
						else if (newDirection == 3)
						{
							if (hitBox.position.x <= (initialPosition.x - walkRange.x))
							{
								atEdge = true;
							}
						}

						if (!atEdge)
						{
							Vector3 movement = getForwardsVector();
							if (movement != new Vector3(0, 0, 0))
							{
								yield return StartCoroutine(move(movement));
							}
						}
					}

					yield return new WaitForSeconds(waitTime);
				}
				yield return null;
			}
		}

		private IEnumerator patrolAround()
		{
			while (true)
			{
				for (int i = 0; i < patrol.Length; i++)
				{
					while (busy)
					{
						yield return null;
					}
					direction = patrol[i].direction;
					yield return null; //2 frame delay to prevent taking a step before initialising battle.
					yield return null;

					for (int i2 = 0; i2 < patrol[i].steps; i2++)
					{
						Vector3 movement = getForwardsVector();
						while (movement == new Vector3(0, 0, 0))
						{
							movement = getForwardsVector();
							yield return new WaitForSeconds(0.1f);
						}

						while (busy)
						{
							yield return null;
						}

						yield return StartCoroutine(move(movement));
					}

					if (patrol[i].endWait > 0)
					{
						yield return new WaitForSeconds(patrol[i].endWait);
					}

					i = patrol.Length;
				}
				transform.position = initialPosition;
				yield return null;
			}
		}

		public Vector3 getForwardsVector()
		{
			return getForwardsVector(false);
		}

		public Vector3 getForwardsVector(bool noClip)
		{
			Vector3 forwardsVector = new Vector3(0, 0, 0);
			if (direction == 0)
			{
				forwardsVector = new Vector3(0, 0, 1f);
			}
			else if (direction == 1)
			{
				forwardsVector = new Vector3(1f, 0, 0);
			}
			else if (direction == 2)
			{
				forwardsVector = new Vector3(0, 0, -1f);
			}
			else if (direction == 3)
			{
				forwardsVector = new Vector3(-1f, 0, 0);
			}

			Vector3 movement = forwardsVector;

			//Check destination map																	//0.5f to adjust for stair height
			//cast a ray directly downwards from the position directly in front of the npc			//1f to check in line with player's head
			RaycastHit[] mapHitColliders = Physics.RaycastAll(transform.position + movement + new Vector3(0, 1.5f, 0),
				Vector3.down);
			RaycastHit mapHit = new RaycastHit();
			//cycle through each of the collisions
			if (mapHitColliders.Length > 0)
			{
				for (int i = 0; i < mapHitColliders.Length; i++)
				{
					//if a collision's gameObject has a mapCollider, it is a map. set it to be the destination map.
					if (mapHitColliders[i].collider.gameObject.GetComponent<MapCollider>() != null)
					{
						mapHit = mapHitColliders[i];
						destinationMap = mapHit.collider.gameObject.GetComponent<MapCollider>();
						i = mapHitColliders.Length;
					}
				}
			}

			//check for a bridge at the destination
			RaycastHit bridgeHit =
				MapCollider.getBridgeHitOfPosition(transform.position + movement + new Vector3(0, 1.5f, 0));
			if (bridgeHit.collider != null)
			{
				//modify the forwards vector to align to the bridge.
				movement -= new Vector3(0, (transform.position.y - bridgeHit.point.y), 0);
			}
			//if no bridge at destination
			else if (mapHit.collider != null)
			{
				//modify the forwards vector to align to the mapHit.
				movement -= new Vector3(0, (transform.position.y - mapHit.point.y), 0);
			}


			float currentSlope = Mathf.Abs(MapCollider.getSlopeOfPosition(transform.position, direction));
			float destinationSlope =
				Mathf.Abs(MapCollider.getSlopeOfPosition(transform.position + forwardsVector, direction));
			float yDistance = Mathf.Abs((transform.position.y + movement.y) - transform.position.y);
			yDistance = Mathf.Round(yDistance * 100f) / 100f;

			//if either slope is greater than 1 it is too steep.
			if (currentSlope <= 1 && destinationSlope <= 1)
			{
				//if yDistance is greater than both slopes there is a vertical wall between them
				if (yDistance <= currentSlope || yDistance <= destinationSlope)
				{
					//check destination tileTag for impassibles unless NoClipping
					if (!noClip)
					{
						int destinationTileTag = destinationMap.getTileTag(transform.position + movement);
						if (destinationTileTag == 1)
						{
							return Vector3.zero;
						}
						else
						{
							if (trainerSurfing)
							{
								//if a surf trainer, normal tiles are impassible
								if (destinationTileTag != 2)
								{
									return Vector3.zero;
								}
							}
							else
							{
								//if not a surf trainer, surf tiles are impassible
								if (destinationTileTag == 2)
								{
									return Vector3.zero;
								}
							}
						}
					}

					bool destinationPassable = true;
					if (!noClip)
					{
						//check destination for objects/player/follower
						Collider[] hitColliders = Physics.OverlapSphere(transform.position + movement, 0.4f);
						if (hitColliders.Length > 0)
						{
							for (int i = 0; i < hitColliders.Length; i++)
							{
								if (hitColliders[i].name == "Player_Transparent" ||
									hitColliders[i].name == "Follower_Transparent" ||
									hitColliders[i].name.ToLowerInvariant().Contains("_object"))
								{
									destinationPassable = false;
								}
							}
						}
					}

					if (destinationPassable)
					{
						return movement;
					}
				}
			}
			return Vector3.zero;
		}

		public IEnumerator move(Vector3 movement)
		{
			yield return StartCoroutine(move(movement, 1));
		}

		public IEnumerator move(Vector3 movement, float speedMod)
		{
			float increment = 0f;

			if (speedMod <= 0)
			{
				speedMod = 1f;
			}
			float speed = PlayerMovement.player.walkSpeed / speedMod;
			framesPerSec = Mathf.RoundToInt(7f * speedMod);

			Vector3 startPosition = transform.position;
			Vector3 destinationPosition = startPosition + movement;

			animPause = false;
			while (increment < 1f)
			{
				//increment increases slowly to 1 over the frames
				if (PlayerMovement.player.busyWith == null || PlayerMovement.player.busyWith == this.gameObject ||
					overrideBusy)
				{
					increment += (1f / speed) * Time.deltaTime;
						//speed is determined by how many squares are crossed in one second
					if (increment > 1)
					{
						increment = 1;
					}
					transform.position = startPosition + (movement * increment);
					hitBox.position = destinationPosition;
				}
				yield return null;
			}
			if (!animated)
				animPause = true;
		}

		public void setOverrideBusy(bool set)
		{
			overrideBusy = set;
		}
	}

	[System.Serializable]
	public class WalkCommand
	{
		public int direction;
		public int steps;
		public float endWait;
	}
}