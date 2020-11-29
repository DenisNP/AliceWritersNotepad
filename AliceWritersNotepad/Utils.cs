using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AliceWritersNotepad
{
    public static class Utils
    {
        public static readonly JsonSerializerSettings ConverterSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };
        
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }
        
        public static string Join(this IEnumerable<string> s, string separator)
        {
            return string.Join(separator, s);
        }

        public static string GetNumericPhrase(int num, string one, string few, string many)
        {
            num = num < 0 ? 0 : num;
            string postfix;

            if (num < 10)
            {
                if (num == 1) postfix = one;
                else if (num > 1 && num < 5) postfix = few;
                else postfix = many;
            }
            else if (num <= 20)
            {
                postfix = many;
            }
            else if (num <= 99)
            {
                var lastOne = num - ((int)Math.Floor((double)num / 10)) * 10;
                postfix = GetNumericPhrase(lastOne, one, few, many);
            }
            else
            {
                var lastTwo = num - ((int)Math.Floor((double)num / 100)) * 100;
                postfix = GetNumericPhrase(lastTwo, one, few, many);
            }
            return postfix;
        }

        public static string ToPhrase(this int num, string one, string few, string many)
        {
            return num + " " + GetNumericPhrase(num, one, few, many);
        }
    }
}