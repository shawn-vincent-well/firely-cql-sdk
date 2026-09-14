#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
/*
 * Copyright (c) 2023, NCQA and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/FirelyTeam/firely-cql-sdk/main/LICENSE
 */

using Hl7.Cql.Abstractions;
using Hl7.Cql.Primitives;

namespace Hl7.Cql.Operators
{
    internal partial class CqlOperators
    {
        #region Children

        public IEnumerable<object> Children(object o) => throw new NotSupportedException();

        #endregion

        // See: https://cql.hl7.org/09-b-cqlreference.html#convert
        #region Convert

        public static string? ConversionFunctionName(Type from, Type to)
        {
            if (TypeToConvertToken.TryGetValue(from, out var fromToken))
            {
                if (TypeToConvertToken.TryGetValue(to, out var toToken))
                {
                    var methodName = $"Convert{fromToken}To{toToken}";
                    return methodName;
                }
                else return null;
            }
            else return null;

        }
        protected static readonly IDictionary<Type, string> TypeToConvertToken = new Dictionary<Type, string>
        {
            { typeof(bool?), "Boolean" },
            { typeof(bool), "Boolean" },
            { typeof(int?), "Integer" },
            { typeof(int), "Integer" },
            { typeof(long), "Long" },
            { typeof(long?), "Long" },
            { typeof(decimal), "Decimal" },
            { typeof(decimal?), "Decimal" },
            { typeof(string), "String" },
            { typeof(CqlQuantity), "Quantity" },
            { typeof(CqlRatio), "Ratio" },
            { typeof(CqlDate), "Date" },
            { typeof(CqlDateTime), "DateTime" },
            { typeof(CqlTime), "Time" },
            { typeof(CqlCode), "Code" },
            { typeof(CqlConcept), "Concept" },
            { typeof(IEnumerable<CqlCode>), "ListOfCodes" },
        };

        public int? ConvertBooleanToInteger(bool? b) => b == null ? null : (b.Value ? 1 : 0);
        public long? ConvertBooleanToLong(bool? b) => b == null ? null : (b.Value ? 1 : 0);
        public decimal? ConvertBooleanToDecimal(bool? b) => b == null ? null : (b.Value ? 1m : 0m);
        public string? ConvertBooleanToString(bool? b) => b?.ToString()?.ToLower(CultureInfo.InvariantCulture);


        public bool? ConvertIntegerToBoolean(int? i) => i == null ? null : (i.Value == 1 ? true : false);
        public long? ConvertIntegerToLong(int? i) => i;
        public decimal? ConvertIntegerToDecimal(int? i) => i;
        public CqlQuantity? ConvertIntegerToQuantity(int? i) => i == null ? null : new CqlQuantity(i, "1");
        public string? ConvertIntegerToString(int? i) => i?.ToString(CultureInfo.InvariantCulture);

        public bool? ConvertLongToBoolean(long? i) => i == null ? null : (i.Value == 1 ? true : false);
        public int? ConvertLongToInteger(long? i) => (int?)i;
        public decimal? ConvertLongToDecimal(long? i) => i;
        public CqlQuantity? ConvertLongToQuantity(long? i) => i == null ? null : new CqlQuantity(i, "1");
        public string? ConvertLongToString(long? i) => i?.ToString(CultureInfo.InvariantCulture);

        public bool? ConvertDecimalToBoolean(decimal? d) => d == null ? null : (d.Value == 1m ? true : false);
        public CqlQuantity? ConvertDecimalToQuantity(decimal? d) => d == null ? null : new CqlQuantity(d, "1");
        public string? ConvertDecimalToString(decimal? d) => d?.ToString(CultureInfo.InvariantCulture);

        public string? ConvertQuantityToString(CqlQuantity? q) => q?.ToString();

        public string? ConvertRatioToString(CqlRatio? r) => r?.ToString();

        public bool? ConvertStringToBoolean(string? s) => ConvertStringToBooleanImpl(s);

        internal static bool? ConvertStringToBooleanImpl(string? s)
        {
            if (s == null) return null;
            
            // Optimize for common cases first, avoid allocating a lowercase string
            switch (s.Length)
            {
                case 1:
                    switch (s[0])
                    {
                        case 't':
                        case 'T':
                        case 'y':
                        case 'Y':
                        case '1':
                            return true;
                        case 'f':
                        case 'F':
                        case 'n':
                        case 'N':
                        case '0':
                            return false;
                        default:
                            return null;
                    }
                case 2:
                    if (s.Equals("no", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return null;
                case 3:
                    if (s.Equals("yes", StringComparison.OrdinalIgnoreCase))
                        return true;
                    return null;
                case 4:
                    if (s.Equals("true", StringComparison.OrdinalIgnoreCase))
                        return true;
                    return null;
                case 5:
                    if (s.Equals("false", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return null;
                default:
                    return null;
            }
        }

        public int? ConvertStringToInteger(string? s)
        {
            if (s == null)
                return null;
            else if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return value;
            else return null;
        }
        public long? ConvertStringToLong(string? s)
        {
            if (s == null)
                return null;
            else if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value))
                return value;
            else return null;
        }
        public decimal? ConvertStringToDecimal(string? s)
        {
            if (s == null)
                return null;
            else if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                return value;
            else return null;
        }
        public CqlQuantity? ConvertStringToQuantity(string? s)
        {
            if (s == null)
                return null;
            else if (CqlQuantity.TryParse(s, out CqlQuantity? value))
                return value;
            else return null;
        }
        public CqlDate? ConvertStringToDate(string? s)
        {
            if (s == null)
                return null;
            else if (CqlDate.TryParse(s, out CqlDate? value))
                return value;
            else return null;
        }
        public CqlDateTime? ConvertStringToDateTime(string? s)
        {
            if (s == null)
                return null;
            else if (CqlDateTime.TryParse(s, out CqlDateTime? value))
                return value;
            else return null;
        }
        public CqlTime? ConvertStringToTime(string? s)
        {
            if (s == null)
                return null;
            else if (CqlTime.TryParse(s, out CqlTime? value))
                return value;
            else return null;
        }


        public string? ConvertDateToString(CqlDate? d) => d?.ToString();
        public CqlDateTime? ConvertDateToDateTime(CqlDate? d) => d == null ? null : new CqlDateTime(d!);


        public string? ConvertDateTimeToString(CqlDateTime? d) => d?.ToString();
        public CqlDate? ConvertDateTimeToDate(CqlDateTime? d) => d == null ? null : new CqlDate(d!);


        public string? ConvertTimeToString(CqlTime? t) => t?.ToString();

        public CqlConcept? ConvertCodeToConcept(CqlCode? c) => c == null ? null : new CqlConcept([c], c.display);

        public IEnumerable<CqlCode>? ConvertConceptToListOfCodes(CqlConcept? c) => c?.codes;


        public CqlConcept? ConvertConceptToListOfCodes(IEnumerable<CqlCode>? c) => c == null ? null : new CqlConcept([.. c], null);

        #endregion

        #region Descendents

        public IEnumerable<object?>? Descendents(object? argument) => argument == null ? null : throw new NotSupportedException();

        #endregion

        #region CanConvertQuantity

        public bool? CanConvertQuantity(CqlQuantity? argument, string? unit)
        {
            if (argument == null || argument.value == null || unit == null) return null;
            try
            {
                return UnitConverter.ChangeUnits(argument, unit) is not null;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        #endregion

        #region ConvertQuantity

        public CqlQuantity? ConvertQuantity(CqlQuantity? argument, string? unit)
        {
            if (argument == null || argument.value == null || unit == null)
                return null;

            var newQuantity = UnitConverter.ChangeUnits(argument, unit);
            return newQuantity;
        }

        #endregion

        // ConvertsToX(argument Any) Boolean: whether the VALUE converts, which for a String means
        // that the corresponding ToX parses it. These were type checks -- "is the argument a
        // String?" -- so ConvertsToInteger('4.2') and ConvertsToTime('25:00:00') answered true. They
        // also compared o.GetType() against the resolver's Nullable<T> types, which a boxed value
        // never equals, so every value-typed argument answered false: ConvertsToString(42) was
        // false. Pattern matching sees through both.

        #region ConvertsToBoolean
        public bool? ConvertsToBoolean(object? o) => o switch
        {
            null => null,
            bool => true,
            int i => i is 0 or 1,
            long l => l is 0 or 1,
            decimal d => d == 0m || d == 1m,
            string s => ConvertStringToBoolean(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToDate
        public bool? ConvertsToDate(object? o) => o switch
        {
            null => null,
            CqlDate => true,
            CqlDateTime => true,
            string s => ConvertStringToDate(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToDateTime
        public bool? ConvertsToDateTime(object? o) => o switch
        {
            null => null,
            CqlDateTime => true,
            CqlDate => true,
            string s => ConvertStringToDateTime(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToDecimal
        public bool? ConvertsToDecimal(object? o) => o switch
        {
            null => null,
            decimal => true,
            bool => true,
            int => true,
            long => true,
            string s => ConvertStringToDecimal(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToLong
        public bool? ConvertsToLong(object? o) => o switch
        {
            null => null,
            long => true,
            bool => true,
            int => true,
            string s => ConvertStringToLong(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToInteger
        public bool? ConvertsToInteger(object? o) => o switch
        {
            null => null,
            int => true,
            bool => true,
            long l => l >= int.MinValue && l <= int.MaxValue,
            string s => ConvertStringToInteger(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToQuantity
        public bool? ConvertsToQuantity(object? o) => o switch
        {
            null => null,
            CqlQuantity => true,
            int => true,
            long => true,
            decimal => true,
            CqlRatio => true,
            string s => ConvertStringToQuantity(s) is not null,
            _ => false,
        };
        #endregion

        #region ConvertsToString
        public bool? ConvertsToString(object? o) => o switch
        {
            null => null,
            string => true,
            bool => true,
            int => true,
            long => true,
            decimal => true,
            CqlQuantity => true,
            CqlRatio => true,
            CqlDate => true,
            CqlDateTime => true,
            CqlTime => true,
            _ => false,
        };
        #endregion

        #region ConvertsToTime
        public bool? ConvertsToTime(object? o) => o switch
        {
            null => null,
            CqlTime => true,
            string s => ConvertStringToTime(s) is not null,
            _ => false,
        };
        #endregion

        #region Quantity

        public CqlQuantity? Quantity(decimal? value, string? unit)
        {
            if (value == null || unit == null)
                return null;
            else return new CqlQuantity(value, unit);
        }

        #endregion

        #region ToList

        public IEnumerable<T> ToList<T>(T item)
        {
            yield return item;
        }

        #endregion

    }
}