using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Restaurant.Models;
/// <summary>
/// جدول الفئات في المطعم، يحتوي على معلومات عن كل فئة مثل اسمها والعلاقة بين الفئات ومحطات الطباعة. يتيح هذا الجدول تنظيم الطلبات
/// حسب الفئات المختلفة في المطعم وتحديد أي الفئات يجب أن تطبع على أي محطة طباعة بناءً على إعدادات النظام واحتياجات المطعم.
/// </summary>
public class Category
{
    /// <summary>
    /// معرف التصنيف
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// اسم التصنيف
    /// </summary>
    [Required]
    public string Name { get; set; }


    /// <summary>
    /// العلاقة مع جدو لالربط بين التصنيفات و المحطات
    /// </summary>
    public ICollection<CategoryPrintStation> CategoryPrintStations { get; set; }=new List<CategoryPrintStation>();

    /// <summary>
    /// /// العلاقة مع جدول المنتجات
    /// </summary>
    public ICollection<Product> Products { get; set; } = new List<Product>();

}
