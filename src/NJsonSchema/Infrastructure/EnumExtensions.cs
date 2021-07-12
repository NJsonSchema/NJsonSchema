// intentionally root name space to light up faster helpers

using System.Runtime.CompilerServices;

namespace NJsonSchema
{
    internal static class EnumExtensions
    {
        // for older frameworks
        private const MethodImplOptions OptionAggressiveInlining = (MethodImplOptions) 256;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsNull(this JsonObjectType type) => (type & JsonObjectType.Null) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsNumber(this JsonObjectType type) => (type & JsonObjectType.Number) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsObject(this JsonObjectType type) => (type & JsonObjectType.Object) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsArray(this JsonObjectType type) => (type & JsonObjectType.Array) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsInteger(this JsonObjectType type) => (type & JsonObjectType.Integer) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsString(this JsonObjectType type) => (type & JsonObjectType.String) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsBoolean(this JsonObjectType type) => (type & JsonObjectType.Boolean) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsFile(this JsonObjectType type) => (type & JsonObjectType.File) != 0;

        [MethodImpl(OptionAggressiveInlining)]
        public static bool IsNone(this JsonObjectType type) => type == JsonObjectType.None;
    }
}