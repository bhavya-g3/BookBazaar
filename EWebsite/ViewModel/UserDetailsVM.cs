using System.Collections.Generic;

namespace EWebsite.ViewModel
{
    public class UserDetailsVM
    {
        public string Id { get; set; } = default!;

        public string? Email { get; set; }

        public string? UserName { get; set; }

        public List<string> RoleNames { get; set; } = new();

        public bool IsProtected { get; set; }

        public string? Name { get; set; }

        public string? Phone { get; set; }

        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? PostalCode { get; set; }
    }
}