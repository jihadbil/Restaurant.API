using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Restaurant.Models;
            

public class ApplicationUser:IdentityUser
{
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>
    /// معرف المطعم التابع له المستخدم
    /// </summary>
    public int? RestaurantId { get; set; }

    /// <summary>
    /// معلومات المطعم التابع له المستخدم
    /// </summary>
    public RestaurantInfo? Restaurant { get; set; }
}
