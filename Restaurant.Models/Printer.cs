using Restaurant.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;

namespace Restaurant.Models;
/// <summary>
/// جدول الطابعات في المطعم، يحتوي على معلومات عن كل طابعة مثل اسمها، نوعها، والمحطة التي تتبع لها.
/// </summary>
public class Printer
{
    /// <summary>
    /// معرف الطابعة، يستخدم كمعرف فريد لكل طابعة في النظام. يتم توليده تلقائيًا عند إنشاء طابعة جديدة.
    /// </summary>
    [Key]
    public int Id { get; set; }
    /// <summary>
    /// الاسم الذي يظهر للمستخدم عند عرض الطابعة في واجهة المستخدم. يجب أن يكون هذا الاسم فريدًا داخل المحطة التي تتبع لها الطابعة لتجنب الالتباس بين الطابعات المختلفة.
    /// </summary>
    [Required]
    public string Name { get; set; }=null!;

    /// <summary>
    /// الأسم الحقيقي للطابعة كما هو معرف في نظام التشغيل أو الشبكة. يستخدم هذا الاسم عند إرسال الأوامر للطباعة، ويجب أن يتطابق مع الاسم الذي تم تكوينه في إعدادات الطابعة على الكمبيوتر أو الشبكة.
    /// </summary>
    [Required]
    public string PrinterName { get; set; }= null!;
    /// <summary>
    /// نوع الطابعة، يحدد نوع الطابعة مثل طابعة الفواتير، طابعة المطبخ، أو طابعة البار. هذا يساعد النظام في توجيه الأوامر للطباعة إلى الطابعة المناسبة بناءً على نوع الطلب الذي يتم طباعته.  
    /// </summary>
    public PrinterType PrinterType { get; set; } = PrinterType.Receipt;


    //////////////////////////////////المفاتيح الخاريجية////////////////////////////////////////////
    ///

    /// <summary>
    /// المعرف الخاص بمحطة الطباعة التي تتبع لها هذه الطابعة. يشير هذا الحقل إلى المحطة التي تنتمي إليها الطابعة، مما يسمح للنظام بتنظيم الطابعات حسب المحطات المختلفة في المطعم. يجب أن يكون هذا المعرف مرتبطًا بمحطة طباعة موجودة في جدول  لضمان تكامل البيانات.
    /// </summary>
    public int PrintStationId { get; set; }
    public virtual PrintStation PrintStation { get; set; } 




}
