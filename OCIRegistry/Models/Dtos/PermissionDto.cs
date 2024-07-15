namespace OCIRegistry.Models.Dtos
{
    public class PermissionDto
    {
        public string? Resource { get; set; }
        public byte Action { get; set; }

        public static PermissionDto FromDatabaseModel(Database.Permission permission)
        {
            return new PermissionDto
            {
                Resource = permission.Resource,
                Action = permission.Action
            };
        }
    }
}
