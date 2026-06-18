using Shatbly.Models;
using System.Collections.Generic;

namespace Shatbly.ViewModels
{
    public class CustomerSettingsVM
    {
        public CustomerChangePasswordVM ChangePassword { get; set; } = new CustomerChangePasswordVM();
        public List<Address> Addresses { get; set; } = new List<Address>();
    }
}
