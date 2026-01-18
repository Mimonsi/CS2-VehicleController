namespace VehicleController.Components
{
    public static class DataMigrationVersion
    {
        // See https://github.com/krzychu124/Traffic/blob/main/Code/Components/LaneConnections/ModifiedLaneConnections.cs#L51
        
        // [mod pre-Beta 0.4.0] Data before this version is lost due to missing versioning
        public static readonly int InitialVersion = 1;
        
        // [mod pre-?] ? 
        public static readonly int FutureUpdate = 2;
    }
}