using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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
    [HideInInspector] public float satisfaction = 0.0f;

    private List<Customer> customersFood = new();
    private List<Customer> customersWood = new();
    private float customerSpawnTimer = 0.0f;

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

    private void Update()
    {
        customerSpawnTimer += Time.deltaTime;

        if (customerSpawnTimer > customerSpawnInterval)
        {
            customerSpawnTimer = 0.0f;
            SpawnCustomer(DeliveryType.Food);
            SpawnCustomer(DeliveryType.Wood);
        }

        int allCustomersCount = customersFood.Count + customersWood.Count;
        satisfaction = 1.0f - ((float)allCustomersCount / (float)LevelController.Instance.MaxCustomersToLose);
    }

    private void SpawnCustomer(DeliveryType type)
    {
        GameObject customerObject = null;

        switch (type)
        {
            case DeliveryType.Food:
                {
                    customerObject = Instantiate(customerPrefabFood, Utilities.Convert2DTo3D(Random.insideUnitCircle * 1.0f) + customerFoodSpawnLocation.position, Quaternion.identity);
                    break;
                }
            case DeliveryType.Wood:
                {
                    customerObject = Instantiate(customerPrefabWood, Utilities.Convert2DTo3D(Random.insideUnitCircle * 2.0f) + customerWoodSpawnLocation.position, Quaternion.identity);
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
        customer.Disappear();
        customer.rigidbody.isKinematic = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(customerFoodSpawnLocation.position, spawnRadius);
        Gizmos.color = new Color(0.58f, 0.2f, 0.66f, 0.75f);
        Gizmos.DrawSphere(customerWoodSpawnLocation.position, spawnRadius);
    }
}
