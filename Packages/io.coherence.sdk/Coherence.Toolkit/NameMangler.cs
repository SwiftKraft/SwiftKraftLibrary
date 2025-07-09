// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System.Text.RegularExpressions;
    using System.Text;

    static class NameMangler
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names
        //   Identifiers must start with a letter or underscore (_).
        //   Identifiers can contain Unicode letter characters, decimal digit characters, Unicode connecting characters, Unicode combining characters, or Unicode formatting characters.
        // https://ecma-international.org/wp-content/uploads/ECMA-334_7th_edition_december_2023.pdf
        //   §6.4.3 (identifiers)
        // https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions?redirectedfrom=MSDN#unicode-category-or-unicode-block-p
        // TL;DR
        //   First character: unicode categories Lu, Ll, Lt, Lm and Lo. Also underscore.
        //   Subsequent characters: unicode categories Lu, Ll, Lt, Lm, Lo, Nl, Mn, Mc, Nd, Pc and Cf.
        private static readonly Regex invalidOnCSharpIdentifierFirstCharacter = new(@"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}_]");
        private static readonly Regex invalidOnCSharpIdentifierSubsequentCharacters = new(@"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]");
        static readonly Regex invalidOnSchemaIdentifierRegex = new("[^a-zA-Z0-9_]");

        public static string MangleSchemaIdentifier(string s)
        {
            return invalidOnSchemaIdentifierRegex.Replace(s, CharsToInts);
        }

        public static string MangleCSharpIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            var firstCharacter = s[..1];
            var result = invalidOnCSharpIdentifierFirstCharacter.Replace(firstCharacter, CharsToInts);

            var subsequentCharacters = s[1..];
            result += invalidOnCSharpIdentifierSubsequentCharacters.Replace(subsequentCharacters, CharsToInts);
            return result;
        }

        private static string CharsToInts(Match match)
        {
            var sb = new StringBuilder();

            sb.Append('_');

            foreach(var c in match.Value)
            {
                sb.Append((int)c);
            }

            sb.Append('_');

            return sb.ToString();
        }
    }
}
