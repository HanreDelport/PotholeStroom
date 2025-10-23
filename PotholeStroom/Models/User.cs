using SQLite;

namespace PotholeStroom.Models
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique, NotNull]
        public string Email { get; set; }

        [NotNull]
        public string PasswordHash { get; set; }
    }
}
