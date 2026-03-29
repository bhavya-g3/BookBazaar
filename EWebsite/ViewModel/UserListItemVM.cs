namespace EWebsite.ViewModel
{
    public class UserListItemVM
    {
        public string Id { get; set; } = default!;

        public string? Email { get; set; }

        public string? UserName { get; set; }

        public string RolesDisplay { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        public bool IsProtected { get; set; }
    }
}