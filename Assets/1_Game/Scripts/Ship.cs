using System;
using DragonWater.Scripting;
using Meta.XR.Guides.Editor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public enum ShipType
{
    FoodSmall,
    FoodMedium,
    FoodBig,
    Pirates,
    Tool
}

public enum BehavioralState
{
    Normal,
    Pirate,
    Control,
    Avoid,
    Destroyed,
    InPort,
    Stop,
    CollectedByKeeper
}

public class Ship : MonoBehaviour
{
    public ShipType type = ShipType.FoodSmall;
    public BehavioralState behavioralState = BehavioralState.Normal;

    public float minimumSpeed = 0.6f;
    public float maximumSpeed = 0.8f;

    public LighthouseLight lighthouseLight;
    public ShipSpawner shipSpawner;
    public ShipEyes eyes;
    public GameObject floater;

    public Collider collisionCollider;

    public bool hasBeenDestroyed = false;

    public UnityAction<Ship> onDestroyed;

    private float speed;
    private float direction;
    private float rangeFactor;
    private float piratesInControlCounter;
    private float destroyedCounter;
    private float collisionRotationCounter;
    private float scaleDownCounter;
    [SerializeField] private float scaleDownSpeed = 0.05f;

    private bool isInPort = false;

    private int avoidDirection = 1;

    private Quaternion rotationBeforeCollision;
    private Quaternion targetRotationAfterCollision;

    private const int visibilityRange = 110;
    private const float startDirectionWiggle = 15.0f;

    private const float collisionRotationTime = 0.5f;
    private const float destroyTime = 3.5f;
    private const float destroyTimeInPort = 6.5f;
    private const float scaleDownTime = 0.25f;
    private const float decelerationSpeed = 0.17f;
    private const float sinkFactor = 0.26f;

    // These were originally in Player and depended on appropriate curves
    private float playerRange = 10.0f;
    private const float playerPiratesInControl = 1.0f;
    private const float playerTurnSpeed = 30.0f;
    private const float playerAdditionalShipSpeed = 1.0f;

    // These were originally in LevelController and depended on appropriate curves
    private const float levelControllerShipsSpeed = 1.0f;

    private void Awake()
    {
        // SetStartDirection();
        rangeFactor = ShipTypeToRangeFactor(type);
    }

    private void Update()
    {
        if (shipSpawner != null)
        {
            playerRange = shipSpawner.shipRange;
        }
        
        if (IsOutOfRoom())
        {
            Destroy(gameObject);
            return;
        }

        switch (behavioralState)
        {
            case BehavioralState.Normal:
                {
                    if (InPortStateChange() || DestroyedStateChange() || AvoidStateChange() || ControlStateChange() || PirateStateChange())
                    {
                        break;
                    }

                    break;
                }

            case BehavioralState.Pirate:
                {
                    if (DestroyedStateChange() || AvoidStateChange() || ControlStateChange())
                    {
                        break;
                    }

                    break;
                }

            case BehavioralState.Control:
                {
                    if (InPortStateChange() || DestroyedStateChange())
                    {
                        break;
                    }

                    if (ControlStateEnded())
                    {
                        if (NormalStateChange())
                        {
                            break;
                        }
                    }

                    break;
                }

            case BehavioralState.Avoid:
                {
                    if (InPortStateChange() || DestroyedStateChange())
                    {
                        break;
                    }

                    if (AvoidStateEnded())
                    {
                        if (PirateStateChange())
                        {
                            break;
                        }

                        if (NormalStateChange())
                        {
                            break;
                        }
                    }

                    break;
                }

            case BehavioralState.Destroyed:
                {
                    break;
                }

            case BehavioralState.InPort:
                {
                    if (DestroyedStateChange())
                    {
                        break;
                    }

                    break;
                }

            case BehavioralState.Stop:
                {
                    if (ControlStateChange())
                    {
                        maximumSpeed = levelControllerShipsSpeed;
                        break;
                    }

                    if (InPortStateChange() || DestroyedStateChange())
                    {
                        break;
                    }
                    break;
                }

            case BehavioralState.CollectedByKeeper:
                {
                    CollectedByKeeperBehavior();
                    return;
                }
        }

        switch (behavioralState)
        {
            case BehavioralState.Normal:
                {
                    NormalBehavior();
                    UpdatePosition();
                    UpdateRotation();
                    break;
                }

            case BehavioralState.Pirate:
                {
                    PirateBehavior();
                    UpdatePosition();
                    UpdateRotation();
                    break;
                }

            case BehavioralState.Control:
                {
                    ControlBehavior();
                    UpdatePosition();
                    UpdateRotation();
                    break;
                }

            case BehavioralState.Avoid:
                {
                    AvoidBehavior();
                    UpdatePosition();
                    UpdateRotation();
                    break;
                }

            case BehavioralState.Destroyed:
                {
                    DestroyedBehavior();
                    break;
                }

            case BehavioralState.InPort:
                {
                    InPortBehavior();
                    UpdatePosition();
                    UpdateRotation();
                    break;
                }

            case BehavioralState.Stop:
                {
                    break;
                }
        }
    }

    public void SetStartDirection()
    {
        Vector2 shipPosition = new Vector2(transform.position.x, transform.position.z);
        Vector2 dir = (new Vector2(shipSpawner.transform.position.x, shipSpawner.transform.position.z) - shipPosition).normalized;
        // Vector2 dir = (new Vector2(0,0) - shipPosition).normalized;
        float rotateDirection = Mathf.Sign(dir.y);
        direction = Vector2.Angle(new Vector2(1.0f, 0.0f), dir) * rotateDirection;
        direction += Random.Range(-startDirectionWiggle, startDirectionWiggle);

        UpdatePosition();
        UpdateRotation();
    }

    private void UpdatePosition()
    {
        float speedDelta = speed * Time.deltaTime;
        Vector2 speedVector = new Vector2(Mathf.Cos(Mathf.Deg2Rad * direction), Mathf.Sin(Mathf.Deg2Rad * direction)) * speedDelta;

        // TODO: If flash not active

        transform.position += new Vector3(speedVector.x, 0.0f, speedVector.y);
    }

    private void UpdateRotation()
    {
        Vector3 currentRotation = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(currentRotation.x, -direction, currentRotation.z);
    }

    #region Behavior

    private void NormalBehavior()
    {
        speed = maximumSpeed;

        if (piratesInControlCounter > 0.0f)
        {
            piratesInControlCounter -= Time.deltaTime;
        }
    }

    private void PirateBehavior()
    {
        speed = maximumSpeed;

        Vector2 shipPosition = Utilities.Convert3DTo2D(transform.position);

        Ship nearestNonPirateShip = shipSpawner.FindNearestNonPirateShip(shipPosition);

        if (nearestNonPirateShip != null)
        {
            Vector2 targetPosition = Utilities.Convert3DTo2D(nearestNonPirateShip.transform.position);
            FollowPoint(shipPosition, targetPosition);
        }
    }

    private void ControlBehavior()
    {
        speed = maximumSpeed;

        Vector2 shipPosition = Utilities.Convert3DTo2D(transform.position);

        Vector2 targetPosition = lighthouseLight.GetPosition();

        float distanceToLight = Vector2.Distance(targetPosition, shipPosition);

        FollowPoint(shipPosition, targetPosition);

        speed = minimumSpeed + ((maximumSpeed + playerAdditionalShipSpeed - minimumSpeed) * (distanceToLight / (playerRange * rangeFactor)));
    }

    private void AvoidBehavior()
    {
        speed -= decelerationSpeed * Time.deltaTime;
        direction += playerTurnSpeed * avoidDirection * Time.deltaTime;
        speed = Mathf.Max(speed, minimumSpeed);
    }

    private void DestroyedBehavior()
    {
        if (destroyedCounter > 0.0f)
        {
            SinkVisuals();
            destroyedCounter -= Time.deltaTime;
            
            if (destroyedCounter < 0.0f)
            {
                collisionCollider.enabled = false;
            }
        }
        else
        {
            ScaleDown(onlySink: true);
            if (transform.position.y <= -10.00f || transform.GetChild(0).localScale.magnitude < 0.01f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void InPortBehavior()
    {
        speed -= decelerationSpeed * Time.deltaTime;
        speed = Mathf.Max(speed, 0.0f);
    }

    private void CollectedByKeeperBehavior()
    {
        if (scaleDownCounter > 0.0f)
        {
            ScaleDown();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region State Change

    private bool NormalStateChange()
    {
        return ChangeState(BehavioralState.Normal);
    }

    private bool PirateStateChange()
    {
        if (type == ShipType.Pirates && piratesInControlCounter <= 0.0f)
        {
            return ChangeState(BehavioralState.Pirate);
        }

        return false;
    }

    private bool ControlStateChange()
    {
        if (lighthouseLight == null || !lighthouseLight.enabled)
        {
            return false;
        }

        Vector2 shipPosition = Utilities.Convert3DTo2D(transform.position);
        Vector2 targetPosition = lighthouseLight.GetPosition();

        float distanceToLight = Vector2.Distance(shipPosition, targetPosition);

        if (distanceToLight >= playerRange * rangeFactor)
        {
            return false;
        }

        Ship nearestShip = shipSpawner.FindNearestShip(lighthouseLight.GetPosition());

        if (nearestShip != this)
        {
            return false;
        }

        lighthouseLight.controlledShip = this;

        // TODO: Change light color, intensity

        return ChangeState(BehavioralState.Control);
    }

    private bool AvoidStateChange()
    {
        if (eyes.seeObstacle)
        {
            avoidDirection = (UnityEngine.Random.Range(0, 1) % 2 * 2) - 1;
            return ChangeState(BehavioralState.Avoid);
        }

        return false;
    }

    private bool DestroyedStateChange()
    {
        if (hasBeenDestroyed)
        {
            return ChangeState(BehavioralState.Destroyed);
        }

        return false;
    }

    private bool InPortStateChange()
    {
        if (isInPort)
        {
            // TODO: Light color and intensity change.
            return ChangeState(BehavioralState.InPort);
        }

        return false;
    }

    private bool ControlStateEnded()
    {
        bool result = false;

        if (lighthouseLight != null && lighthouseLight.enabled)
        {
            if (lighthouseLight.controlledShip != this)
            {
                result = true;
            }

            Vector2 shipPosition = Utilities.Convert3DTo2D(transform.position);
            Vector2 targetPosition = lighthouseLight.GetPosition();

            float distanceToLight = Vector2.Distance(shipPosition, targetPosition);

            if (distanceToLight >= playerRange * rangeFactor)
            {
                result = true;
            }
        }
        else
        {
            result = true;
        }

        if (result)
        {
            if (type == ShipType.Pirates)
            {
                // TODO: Change light color and intensity
                piratesInControlCounter = playerPiratesInControl;
            }
            else
            {
                // TODO: Change light color and intensity
            }
        }

        return result;
    }

    private bool AvoidStateEnded()
    {
        return !eyes.seeObstacle;
    }


    private bool ChangeState(BehavioralState newState)
    {
        behavioralState = newState;
        return true;
    }

    #endregion

    private void FollowPoint(Vector2 shipPosition, Vector2 targetPosition)
    {
        Vector2 shipDirection = new Vector2(Mathf.Cos(Mathf.Deg2Rad * direction), Mathf.Sin(Mathf.Deg2Rad * direction)).normalized;
        Vector2 targetDirection = (targetPosition - shipPosition).normalized;

        float rotateDistance = Vector2.Angle(shipDirection, targetDirection);

        if (rotateDistance <= visibilityRange)
        {
            float rotateDirection = Mathf.Sign(shipDirection.x * targetDirection.y - shipDirection.y * targetDirection.x);

            direction += rotateDirection * playerTurnSpeed * Time.deltaTime;
        }
    }

    private void ScaleDown(bool onlySink = false)
    {
        scaleDownCounter -= Time.deltaTime * scaleDownSpeed;
        Vector3 scale = transform.GetChild(0).localScale * (scaleDownCounter / scaleDownTime);
        GetComponent<Rigidbody>().freezeRotation = true;
        
        if (!onlySink)
        {
            transform.GetChild(0).localScale = scale;
        }
        else
        {
            GetComponent<FloatingBody>().BuoyancyForce -= Time.deltaTime * 350.0f;   
        }
        
        // TODO: Dim out light
    }

    private bool IsOutOfRoom()
    {
        return Mathf.Abs(transform.position.x) > 50 || Mathf.Abs(transform.position.z) > 50;
    }

    private float ShipTypeToRangeFactor(ShipType type)
    {
        return type switch
        {
            ShipType.FoodSmall => 0.55f,
            ShipType.FoodMedium => 0.75f,
            ShipType.FoodBig => 1.0f,
            ShipType.Pirates => 0.55f,
            ShipType.Tool => 0.55f,
            _ => 1.0f
        };
    }

    private void SinkVisuals()
    {
        FloatingBody f = GetComponent<FloatingBody>();
        f.BuoyancyForce = 1000.0f;
        Vector3 centerOfMass = f.CenterOfMass;
        centerOfMass.x = -0.7f;
        f.CenterOfMass = centerOfMass;
    }
    
    public void DestroyShip()
    {
        if (hasBeenDestroyed)
        {
            return;
        }
        
        SinkVisuals();
        
        hasBeenDestroyed = true;
        destroyedCounter = isInPort ? destroyTimeInPort : destroyTime;
        scaleDownCounter = scaleDownTime;
    }

    public void GetCollectedByKeeper()
    {
        behavioralState = BehavioralState.CollectedByKeeper;
        scaleDownCounter = scaleDownTime;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out Ship _) || other.gameObject.TryGetComponent(out IceBound _))
        {
            DestroyShip();
        }
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.TryGetComponent(out Ship _) || other.TryGetComponent(out IceBound _))
    //     {
    //         DestroyShip();
    //     }
    // }

    private void OnDestroy()
    {
        onDestroyed.Invoke(this);
    }
}
