using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models;

public static class Money
{
    public const decimal Maximum = 9999999999999999.99m;
    public static bool IsValid(decimal value, bool allowZero = true) => value >= (allowZero ? 0 : 0.01m)
        && value <= Maximum && decimal.Round(value, 2) == value;
    public static void Validate(decimal value, bool allowZero = true)
    {
        if (!IsValid(value, allowZero)) throw new ArgumentException("المبلغ غير صالح. استخدم قيمة غير سالبة بخانتين عشريتين وضمن الحد المسموح.");
    }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class MoneyAttribute : ValidationAttribute
{
    public bool AllowZero { get; set; } = true;
    public MoneyAttribute() => ErrorMessage = "المبلغ يجب أن يكون ضمن الحد المسموح وبخانتين عشريتين.";
    public override bool IsValid(object? value) => value is null || value is decimal amount && Money.IsValid(amount, AllowZero);
}
