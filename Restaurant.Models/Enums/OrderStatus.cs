using System;
using System.Collections.Generic;
using System.Text;

namespace Restaurant.Models.Enums;

public enum OrderStatus
{
    
    Preparing = 0,      // قيد التحضير
    Ready = 1,          // جاهزة
    Delivered = 2,      // تم التسليم
    Cancelled = 3       // ملغاة


}
