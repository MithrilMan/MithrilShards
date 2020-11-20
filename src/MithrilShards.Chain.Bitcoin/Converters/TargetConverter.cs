using System;
using System.ComponentModel;
using System.Globalization;
using MithrilShards.Chain.Bitcoin.DataTypes;

namespace MithrilShards.Chain.Bitcoin.Converters
{
   public class TargetConverter : TypeConverter
   {
      public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
      {
         if (sourceType == typeof(string)) return true;

         return base.CanConvertFrom(context, sourceType);
      }

      public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
      {
         if (destinationType == typeof(string)) return true;

         return base.CanConvertTo(context, destinationType);
      }

      public override object? ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
      {
         if (destinationType != typeof(string)) return base.ConvertTo(context, culture, value, destinationType);

         return value?.ToString();
      }

      public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
      {
         if (!(value is string)) return base.ConvertFrom(context, culture, value);

         if (((string)value).Trim().Length == 0) return null;

         return new Target((string)value);
      }
   }
}
