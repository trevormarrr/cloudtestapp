namespace CloudTestApp.Models
{
    public class Contact
    {
        public int Id { get; set; }             // PK
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
