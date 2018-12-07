//-----------------------------------------------------------------------
// <copyright file="JsonFormatStrings.cs" company="NJsonSchema">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/rsuter/NJsonSchema/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System;

namespace NJsonSchema
{
    /// <summary>Class containing the constants available as format string. </summary>
    public static class JsonFormatStrings
    {
        /// <summary>Format for a <see cref="System.DateTime"/>. </summary>
        public const string DateTime = "date-time";

        /// <summary>Format for a <see cref="TimeSpan"/>. </summary>
        public const string TimeSpan = "time-span";

        /// <summary>Format for an email. </summary>
        public const string Email = "email";

        /// <summary>Format for an URI. </summary>
        public const string Uri = "uri";

        /// <summary>Format for an GUID. </summary>
        public const string Guid = "guid";

        /// <summary>Format for an UUID (same as GUID). </summary>
        [Obsolete("Now made redundant. Use \"guid\" instead.")]
        public const string Uuid = "uuid";

        /// <summary>Format for an integer. </summary>
        public const string Integer = "int32";

        /// <summary>Format for a long integer. </summary>
        public const string Long = "int64";

        /// <summary>Format for a double number. </summary>
        public const string Double = "double";

        /// <summary>Format for a float number. </summary>
        public const string Float = "float";

        /// <summary>Format for a decimal number. </summary>
        public const string Decimal = "decimal";

        /// <summary>Format for an IP v4 address. </summary>
        public const string IpV4 = "ipv4";

        /// <summary>Format for an IP v6 address. </summary>
        public const string IpV6 = "ipv6";

        /// <summary>Format for binary data encoded with Base64.</summary>
        /// <remarks>Should not be used. Prefer using Byte property of <see cref="JsonFormatStrings"/></remarks>
        [Obsolete("Now made redundant. Use \"byte\" instead.")]
        public const string Base64 = "base64";

        /// <summary>Format for a byte if used with numeric type or for base64 encoded value otherwise.</summary>
        public const string Byte = "byte";

        /// <summary>Format for a hostname (DNS name).</summary>
        public const string Hostname = "hostname";

        /// <summary>Format for a phone number.</summary>
        public const string Phone = "phone";

        /// <summary>Format for a full date per RFC3339 Section 5.6.</summary>
        public const string Date = "date";

        /// <summary>Format for a full time per RFC3339 Section 5.6.</summary>
        public const string Time = "time";
    }
}