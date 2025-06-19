using UnityEngine;

public class Port : MonoBehaviour
{
    public DeliveryType portType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }

        if (other.attachedRigidbody.TryGetComponent(out Ship ship))
        {
            if (portType == DeliveryType.Food)
            {
                if (ship.type == ShipType.FoodSmall || ship.type == ShipType.FoodMedium || ship.type == ShipType.FoodBig)
                {
                    ship.StopAtPort();
                }
            }
            else if (portType == DeliveryType.Wood)
            {
                if (ship.type == ShipType.WoodSmall || ship.type == ShipType.WoodMedium || ship.type == ShipType.WoodBig)
                {
                    ship.StopAtPort();
                }
            }
        }
    }
}
