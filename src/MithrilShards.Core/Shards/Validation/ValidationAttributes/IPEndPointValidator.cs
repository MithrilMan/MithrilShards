using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace MithrilShards.Core.Shards.Validation.ValidationAttributes;

/// <summary>
/// Ensure the value is a valid IPEndPoint.
/// Null value is considered valid, use <see cref="RequiredAttribute"/> if you don't want to allow null values.
/// </summary>
/// <seealso cref="System.ComponentModel.DataAnnotations.ValidationAttribute" />
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class IPEndPointValidator : ValidationAttribute
{
   protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
   {
      if (value is null) return ValidationResult.Success;

      string instance = value as string ?? string.Empty;

      if (!IPEndPoint.TryParse(instance, out IPEndPoint? _))
      {
         return new ValidationResult($"Not a valid EndPoint ({instance})", new string[] { validationContext.MemberName! });
      }

      return ValidationResult.Success;
   }
}
