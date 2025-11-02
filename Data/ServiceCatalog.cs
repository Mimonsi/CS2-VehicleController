using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

namespace VehicleController.Data
{
    public enum ServiceType
    {
        None,
        Healthcare,
        Fire,
        Police,
        Garbage,
        Deathcare,
        Postal,
        Transport,
        RoadMaintenance,
        ParkMaintenance
    }

    public readonly struct ServiceDescriptor
    {
        public ServiceDescriptor(
            ServiceType serviceType,
            IEnumerable<ComponentType> vehicleComponents,
            IEnumerable<ComponentType> buildingComponents,
            IEnumerable<ComponentType>? vehiclePrefabComponents = null)
        {
            ServiceType = serviceType;
            VehicleComponents = vehicleComponents.ToArray();
            BuildingComponents = buildingComponents.ToArray();
            VehiclePrefabComponents = vehiclePrefabComponents?.ToArray() ?? Array.Empty<ComponentType>();
        }

        public ServiceType ServiceType { get; }
        public ComponentType[] VehicleComponents { get; }
        public ComponentType[] BuildingComponents { get; }
        public ComponentType[] VehiclePrefabComponents { get; }

        public bool MatchesBuilding(EntityManager entityManager, Entity entity)
        {
            foreach (var componentType in BuildingComponents)
            {
                if (entityManager.HasComponent(entity, componentType))
                {
                    return true;
                }
            }

            return false;
        }

        public bool MatchesVehicle(EntityManager entityManager, Entity entity)
        {
            foreach (var componentType in VehicleComponents)
            {
                if (entityManager.HasComponent(entity, componentType))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class ServiceCatalog
    {
        public static readonly IReadOnlyList<ServiceDescriptor> Descriptors = new[]
        {
            new ServiceDescriptor(
                ServiceType.Healthcare,
                new[] { ComponentType.ReadOnly<Game.Vehicles.Ambulance>() },
                new[] { ComponentType.ReadOnly<Game.Buildings.Hospital>() },
                new[] { ComponentType.ReadOnly<Game.Vehicles.AmbulanceData>() }),
            new ServiceDescriptor(
                ServiceType.Fire,
                new[] { ComponentType.ReadOnly<Game.Vehicles.FireEngine>() },
                new[] { ComponentType.ReadOnly<Game.Buildings.FireStation>() },
                new[] { ComponentType.ReadOnly<Game.Vehicles.FireEngineData>() }),
            new ServiceDescriptor(
                ServiceType.Police,
                new[] { ComponentType.ReadOnly<Game.Vehicles.PoliceCar>() },
                new[] { ComponentType.ReadOnly<Game.Buildings.PoliceStation>() },
                new[] { ComponentType.ReadOnly<Game.Vehicles.PoliceCarData>() }),
            new ServiceDescriptor(
                ServiceType.Garbage,
                new[] { ComponentType.ReadOnly<Game.Vehicles.GarbageTruck>() },
                new[] { ComponentType.ReadOnly<Game.Buildings.GarbageFacility>() },
                new[] { ComponentType.ReadOnly<Game.Vehicles.GarbageTruckData>() }),
            new ServiceDescriptor(
                ServiceType.Deathcare,
                new[] { ComponentType.ReadOnly<Game.Vehicles.Hearse>() },
                new[] { ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>() },
                new[] { ComponentType.ReadOnly<Game.Vehicles.HearseData>() }),
            new ServiceDescriptor(
                ServiceType.Postal,
                new[] { ComponentType.ReadOnly<Game.Vehicles.PostVan>() },
                new[] { ComponentType.ReadOnly<Game.Buildings.PostFacility>() },
                new[] { ComponentType.ReadOnly<Game.Vehicles.PostVanData>() }),
            new ServiceDescriptor(
                ServiceType.RoadMaintenance,
                new[]
                {
                    ComponentType.ReadOnly<Game.Vehicles.MaintenanceVehicle>(),
                    ComponentType.ReadOnly<Game.Vehicles.RoadMaintenanceVehicle>()
                },
                new[] { ComponentType.ReadOnly<Game.Buildings.MaintenanceDepot>() }),
            new ServiceDescriptor(
                ServiceType.Transport,
                new[]
                {
                    ComponentType.ReadOnly<Game.Vehicles.Taxi>(),
                    ComponentType.ReadOnly<Game.Vehicles.WorkVehicle>()
                },
                new[] { ComponentType.ReadOnly<Game.Buildings.TransportDepot>() })
        };

        public static readonly ComponentType[] VehicleComponentTypes =
            Descriptors
                .SelectMany(descriptor => descriptor.VehicleComponents)
                .Distinct()
                .ToArray();

        public static readonly ComponentType[] BuildingComponentTypes =
            Descriptors
                .SelectMany(descriptor => descriptor.BuildingComponents)
                .Distinct()
                .ToArray();

        public static bool TryGetDescriptor(ServiceType serviceType, out ServiceDescriptor descriptor)
        {
            foreach (var candidate in Descriptors)
            {
                if (candidate.ServiceType == serviceType)
                {
                    descriptor = candidate;
                    return true;
                }
            }

            descriptor = default;
            return false;
        }
    }
}
