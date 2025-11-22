Vehicle Select Logic:

Game.Prefabs.PersonalCarSelectData.cs

Service Vehicle Select Logic:

Game.Prefabs.HealthcareVehicleSelectData.cs, etc.


## Speed Limit Findings

- CarLane.m_DefaultSpeedLimit is base speed limit, usage currently unknown
- CarLane.m_SpeedLimit is actual speed limit and is taken from prefab for new roads, but not existing roads
- CarLane.m_SpeedLimit is changed by "Speed Bumps"-policy, and reverted back to m_DefaultSpeedLimit when policy is removed
- CarLane.m_AccessRestrictions looks interesting

TODO: Scooter01 as Private Vehicle for Probability Packs
