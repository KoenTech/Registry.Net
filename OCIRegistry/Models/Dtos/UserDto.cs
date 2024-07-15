namespace OCIRegistry.Models.Dtos
{
    public class UserDto
    {
        public ulong Id { get; set; }
        public string Username { get; set; }

        public static UserDto FromDatabaseModel(Database.User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username
            };
        }
    }
}
