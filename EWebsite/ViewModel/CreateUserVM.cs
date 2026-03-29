namespace EWebsite.ViewModel
{
    public class CreateUserVM
    {
        public string? Email { get; set; }

        public string? UserName { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }

        public bool MakeAdmin { get; set; } = false;
    }
}