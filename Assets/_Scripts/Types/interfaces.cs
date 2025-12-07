using UnityEngine;

namespace core.Types
{
    public interface IDamagable
    {
        public int Hp { get; }
    }

    public interface IEntity
    {
        void SendPosition(Vector3 position);
        void SendRotation(Vector3 rotation);
    }

    public interface IFuel
    {
        Coroutine FuelVehicle(RCC_CarControllerV4 controller);
    }
}