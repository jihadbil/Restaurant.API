using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Restaurant.Models;
            

public class ApplicationUser:IdentityUser
{
    public ICollection<Order> Orders { get; set; } = new List<Order>();


}
