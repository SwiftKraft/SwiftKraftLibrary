// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Plugins.NativeLauncher
{
    using System.Collections.Generic;
    using System.Text;

    public class NlProcessStartupInfo
    {
        public string ExecutablePath { get; private set; }
        public List<string> Arguments { get; private set; }
        public Dictionary<string, string> EnvironmentVariables { get; private set; }
        public bool RaiseOnExit { get; set; }

        public NlProcessStartupInfo(string executablePath, string arguments)
        {
            ExecutablePath = executablePath;
            Arguments = ConvertToArgumentList(arguments);
            EnvironmentVariables = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(ExecutablePath);
            foreach (var arg in Arguments)
            {
                sb.Append(' ');
                sb.Append(arg);
            }

            sb.Append(" ENV: ");
            foreach (var kvp in EnvironmentVariables)
            {
                sb.Append($"{kvp.Key}={kvp.Value} ");
            }

            return sb.ToString();
        }

        public string[] EnvironmentVariablesToArray()
        {
            var envVars = new List<string>();
            foreach (var kvp in EnvironmentVariables)
            {
                envVars.Add($"{kvp.Key}={kvp.Value}");
            }

            return envVars.ToArray();
        }

        // The following code is a copy of the code from System.Diagnostics.Process.Unix.cs
        // https://github.com/dotnet/runtime/blob/598d5f729a0d114a5909487e618eb842c6b45d58/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L859
        private static List<string> ConvertToArgumentList(string arguments)
        {
            var argumentList = new List<string>();
            for (var i = 0; i < arguments?.Length; i++)
            {
                while (i < arguments.Length && (arguments[i] == ' ' || arguments[i] == '\t'))
                {
                    i++;
                }

                if (i == arguments.Length)
                {
                    break;
                }

                argumentList.Add(GetNextArgument(arguments, ref i));
            }

            return argumentList;
        }

        private static string GetNextArgument(string arguments, ref int i)
        {
            var currentArgument = new StringBuilder(256);
            var inQuotes = false;

            while (i < arguments.Length)
            {
                // From the current position, iterate through contiguous backslashes.
                int backslashCount = 0;
                while (i < arguments.Length && arguments[i] == '\\')
                {
                    i++;
                    backslashCount++;
                }

                if (backslashCount > 0)
                {
                    if (i >= arguments.Length || arguments[i] != '"')
                    {
                        // Backslashes not followed by a double quote:
                        // they should all be treated as literal backslashes.
                        currentArgument.Append('\\', backslashCount);
                    }
                    else
                    {
                        // Backslashes followed by a double quote:
                        // - Output a literal slash for each complete pair of slashes
                        // - If one remains, use it to make the subsequent quote a literal.
                        currentArgument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 != 0)
                        {
                            currentArgument.Append('"');
                            i++;
                        }
                    }

                    continue;
                }

                var c = arguments[i];

                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                if (c == '"')
                {
                    if (inQuotes && i < arguments.Length - 1 && arguments[i + 1] == '"')
                    {
                        // Two consecutive double quotes inside an inQuotes region should result in a literal double quote
                        // (the parser is left in the inQuotes region).
                        // This behavior is not part of the spec of code:ParseArgumentsIntoList, but is compatible with CRT
                        // and .NET Framework.
                        currentArgument.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    i++;
                    continue;
                }

                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, it should be added to the results and then reset for the next one.
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    break;
                }

                // Nothing special; add the character to the current argument.
                currentArgument.Append(c);
                i++;
            }

            return currentArgument.ToString();
        }
    }
}
