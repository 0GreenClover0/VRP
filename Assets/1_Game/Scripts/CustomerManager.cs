using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [SerializeField] private GameObject customerPrefabFood;
    [SerializeField] private GameObject customerPrefabWood;

    [SerializeField] private Transform customerFoodSpawnLocation;
    [SerializeField] private Transform customerWoodSpawnLocation;

    [SerializeField] private float customerSpawnInterval = 5.0f;

    [SerializeField] private float spawnRadius = 1.2f;

    [HideInInspector] public float slightlyAngryStartSatisfaction = 0.0f;
    [HideInInspector] public float satisfaction = 0.0f;
    [HideInInspector] public bool areAngryJumping = false;

    private List<Customer> customersFood = new();
    private List<Customer> customersWood = new();
    private float customerSpawnTimer = 0.0f;
    private float angryJumperTickTimer = 0.0f;

    private const float angryJumperTickInterval = 1.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("CustomerManager instance already existed. Destroying the old one.");
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    private void Start()
    {
        slightlyAngryStartSatisfaction = 1.0f - (LevelController.Instance.slightlyAngryStartTreshold / (float)LevelController.Instance.MaxCustomersToLose);
    }

    private void Update()
    {
        customerSpawnTimer += Time.deltaTime;

        if (customerSpawnTimer > customerSpawnInterval && customersFood.Count + customersWood.Count < LevelController.Instance.MaxCustomersToLose + 1)
        {
            customerSpawnTimer = 0.0f;
            SpawnCustomer(DeliveryType.Food);
            SpawnCustomer(DeliveryType.Wood);
        }

        int allCustomersCount = customersFood.Count + customersWood.Count;
        satisfaction = 1.0f - Mathf.Clamp(((float)allCustomersCount / (float)LevelController.Instance.MaxCustomersToLose), 0.0f, 1.0f);

        if (allCustomersCount > LevelController.Instance.MaxCustomersToLose)
        {
            GameManager.Instance.GameOver();
        }

        ManageAngryJump();
    }

    private void ManageAngryJump()
    {
        if (satisfaction <= 0.0f && !areAngryJumping)
        {
            areAngryJumping = true;

            foreach (var customer in customersFood)
            {
                if (!customer.IsGrounded())
                {
                    areAngryJumping = false;
                    break;
                }
            }

            if (areAngryJumping)
            {
                foreach (var customer in customersWood)
                {
                    if (!customer.IsGrounded())
                    {
                        areAngryJumping = false;
                        break;
                    }
                }
            }

            if (areAngryJumping)
            {
                foreach (var customer in customersFood)
                {
                    customer.Jump(true);
                }

                foreach (var customer in customersWood)
                {
                    customer.Jump(true);
                }
            }
        }
        else if (satisfaction <= 0.0f && areAngryJumping)
        {
            angryJumperTickTimer += Time.deltaTime;

            if (angryJumperTickTimer < angryJumperTickInterval)
            {
                return;
            }

            angryJumperTickTimer = 0.0f;

            foreach (var customer in customersFood)
            {
                customer.Jump(true);
            }

            foreach (var customer in customersWood)
            {
                customer.Jump(true);
            }
        }
        else
        {
            angryJumperTickTimer = 0.0f;
            areAngryJumping = false;
        }
    }

    private void SpawnCustomer(DeliveryType type)
    {
        GameObject customerObject = null;

        Vector3 initialPosition = Vector3.zero;
        Quaternion initialRotation = Quaternion.identity;

        switch (type)
        {
            case DeliveryType.Food:
                {
                    initialPosition = customerFoodSpawnLocation.position;
                    initialRotation = customerFoodSpawnLocation.rotation;
                    break;
                }
            case DeliveryType.Wood:
                {
                    initialPosition = customerWoodSpawnLocation.position;
                    initialRotation = customerWoodSpawnLocation.rotation;
                    break;
                }
        }

        initialPosition.y += 0.1f;

        Vector3 position = Vector3.zero;

        // Try not to spawn customer into another customer.
        for (int i = 0; i < 100; ++i)
        {
            position = Utilities.Convert2DTo3D(Random.insideUnitCircle * spawnRadius) + initialPosition;

            if (!Physics.CheckSphere(position, 0.25f))
            {
                break;
            }
        }

        switch (type)
        {
            case DeliveryType.Food:
                {
                    customerObject = Instantiate(customerPrefabFood, position, initialRotation);
                    break;
                }
            case DeliveryType.Wood:
                {
                    customerObject = Instantiate(customerPrefabWood, position, initialRotation);
                    break;
                }
            default:
                {
                    Debug.LogError("Unknown delivery type of a customer.");
                    return;
                }
        }

        Customer customer = customerObject.GetComponent<Customer>();
        customer.customerManager = this;

        switch (type)
        {
            case DeliveryType.Food:
                {
                    customersFood.Add(customer);
                    break;
                }
            case DeliveryType.Wood:
                {
                    customersWood.Add(customer);
                    break;
                }
        }
    }

    public void PackageDelivered(ShipType type)
    {
        Customer customer = null;

        if (Ship.IsShipTypeCompatibleWithDeliveryType(type, DeliveryType.Food))
        {
            if (customersFood.Count == 0)
            {
                return;
            }

            customer = customersFood.FirstOrDefault();
            customersFood.RemoveAt(0);
        }
        else if (Ship.IsShipTypeCompatibleWithDeliveryType(type, DeliveryType.Wood))
        {
            if (customersWood.Count == 0)
            {
                return;
            }

            customer = customersWood.FirstOrDefault();
            customersFood.RemoveAt(0);
        }

        customer.SpawnEmoji(Customer.EmojiType.Happy);
        customer.PlayPenguinSound(Customer.PenguinSoundType.Happy);
        customer.Disappear();
        customer.rigidbody.isKinematic = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        if (customerFoodSpawnLocation != null)
        {
            Gizmos.DrawSphere(customerFoodSpawnLocation.position, spawnRadius);
        }

        Gizmos.color = new Color(0.58f, 0.2f, 0.66f, 0.75f);

        if (customerWoodSpawnLocation != null)
        {
            Gizmos.DrawSphere(customerWoodSpawnLocation.position, spawnRadius);
        }
    }
}
