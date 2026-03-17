using System.Runtime.InteropServices;

namespace Vext.Shared
{
    /// <summary>
    /// Represents the type of a Vext value.
    /// </summary>
    public enum VextType : byte
    {
        /// <summary>
        /// Represents an integer type.
        /// </summary>
        Int,
        /// <summary>
        /// Represents a floating-point type.
        /// </summary>
        Float,
        /// <summary>
        /// Represents a boolean type.
        /// </summary>
        Bool,
        /// <summary>
        /// Represents a string type.
        /// </summary>
        String,
        /// <summary>
        /// Represents a null type.
        /// </summary>
        Null
    }

    /// <summary>
    /// Represents a value in the Vext virtual machine.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct VextValue
    {
        /// <summary>Represents the type of the value.</summary>
        [FieldOffset(0)] public VextType Type;
        /// <summary>Represents the integer value.</summary>
        [FieldOffset(8)] public long AsInt;
        /// <summary>Represents the floating-point value.</summary>
        [FieldOffset(8)] public double AsFloat;
        /// <summary>Represents the boolean value.</summary>
        [FieldOffset(8)] public bool AsBool;
        /// <summary>Represents the string value.</summary>
        [FieldOffset(16)] public string AsString;

        /// <summary>Creates a VextValue of type Int.</summary>
        public static VextValue FromInt(long n) => new VextValue { Type = VextType.Int, AsInt = n };
        /// <summary>Creates a VextValue of type Float.</summary>
        public static VextValue FromFloat(double n) => new VextValue { Type = VextType.Float, AsFloat = n };
        /// <summary>Creates a VextValue of type Bool.</summary>
        public static VextValue FromBool(bool b) => new VextValue { Type = VextType.Bool, AsBool = b };
        /// <summary>Creates a VextValue of type String.</summary>
        public static VextValue FromString(string s) => new VextValue { Type = VextType.String, AsString = s };
        /// <summary>Creates a VextValue of type Null.</summary>
        public static VextValue Null() => new VextValue { Type = VextType.Null };

        /// <summary>Returns true if this value is a numeric type (Int or Float).</summary>
        public readonly bool IsNumeric => Type == VextType.Int || Type == VextType.Float;

        /// <summary>Returns the numeric value as a double, regardless of whether it is Int or Float.</summary>
        public readonly double ToDouble() => Type == VextType.Int ? (double)AsInt : AsFloat;

        /// <summary>Returns a string representation of the VextValue.</summary>
        public override readonly string ToString()
        {
            return Type switch
            {
                VextType.Int => AsInt.ToString(),
                VextType.Float => AsFloat.ToString(),
                VextType.Bool => AsBool.ToString(),
                VextType.String => AsString ?? "null",
                VextType.Null => "null",
                _ => "unknown"
            };
        }
    }
}
