using System;
using System.Collections.Generic;
using System.Text;

namespace Restaurant.Models;
/// <summary>
/// الجدول الذي يربط بين التصنيفات ومحطات الطباعة في المطعم، حيث يمكن أن تكون هناك التصنيفات متعددة مرتبطة بمحطة طباعة واحدة،
/// والعكس صحيح. هذا يسمح للنظام بتحديد أي التصنيفات يجب أن تطبع على أي محطة طباعة بناءً على إعدادات النظام واحتياجات المطعم.
/// </summary>
public class CategoryPrintStation
{
    /// <summary>
    /// معرف التصنيفات
    /// </summary>
    public int CategoryId { get; set; }

    public Category Category { get; set; }
    /// <summary>
    /// معرف المحطة
    /// </summary>
    public int PrintStationId { get; set; }
public PrintStation PrintStation { get; set; }
}
