using UnityEngine;

public class Port : MonoBehaviour
{
    public enum PortType
    {
        Food,
        Wood,
    }

    public PortType portType;

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }

        if (other.attachedRigidbody.TryGetComponent(out Ship ship))
        {
            if (portType == PortType.Food)
            {
                if (ship.type == ShipType.FoodSmall || ship.type == ShipType.FoodMedium || ship.type == ShipType.FoodBig)
                {
                    ship.StopAtPort();
                }
            }
            else if (portType == PortType.Wood)
            {
                if (ship.type == ShipType.WoodSmall || ship.type == ShipType.WoodMedium || ship.type == ShipType.WoodBig)
                {
                    ship.StopAtPort();
                }
            }
        }
    }
}
