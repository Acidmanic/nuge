using System;

namespace nuge.Nuget
{
    public class Version
    {
        public string Value { get; private set; }

        public bool IsRange { get; private set; }

        public string Minimum { get; private set; }

        public string Maximum { get; private set; }

        public static implicit operator Version(string originalValue)
        {
            var start = originalValue.StartsWith("(") || originalValue.StartsWith("[");
            var end = originalValue.EndsWith(")") || originalValue.EndsWith("]");

            if (start && end)
            {
                var value = originalValue.Substring(1, originalValue.Length - 2);

                var segments = value.Split(",", StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 1)
                {
                    return MakeVersion(value);
                }

                if (segments.Length == 2)
                {
                    return new Version
                    {
                        Maximum = segments[1],
                        Minimum = segments[0],
                        IsRange = true,
                        Value = originalValue
                    };
                }
                throw new Exception("Invalid version string");
            }

            if (start || end)
            {
                throw new Exception("Invalid version string");
            }

            var version =  MakeVersion(originalValue);

            if (string.IsNullOrEmpty(version.ToString()))
            {
                Console.WriteLine();
            }

            return version;
        }


        public static implicit operator string(Version version)
        {
            return ToString(version);
        }
        
        private static Version MakeVersion(string value)
        {
            return new Version
            {
                Maximum = value,
                Minimum = value,
                Value = value,
                IsRange = false
            };
        }

        public override string ToString()
        {
            return ToString(this);
        }


        private static string ToString(Version version)
        {
            if (version.IsRange)
            {
                return version.Minimum;
            }
            else
            {
                return version.Value;
            }         
        }
    }
}