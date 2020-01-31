using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using MithrilShards.Core.DataTypes;

namespace MithrilShards.Chain.Bitcoin.DataTypes
{
   public partial class Target : UInt256
   {
      public static Target operator <<(Target? a, int shiftAmount)
      {
         return a;
      }

      public static Target LeftShift(Target a, int shiftAmount)
      {
         return a << shiftAmount;
      }


      public static Target operator + (Target? a, Target? b)
      {
         // todo
         return a + b;
      }
   }
}
