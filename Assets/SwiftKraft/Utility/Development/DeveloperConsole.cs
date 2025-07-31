using SwiftKraft.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Color = UnityEngine.Color;

public class DeveloperConsole : MonoBehaviour
{
    #region Statics

    public static bool Enabled = true;

    public static Dictionary<string, Command> Commands
    {
        get
        {
            _commands ??= CommandAttribute.GetCommands();
            return _commands;
        }
    }
    private static Dictionary<string, Command> _commands = null;

    private readonly static List<string> executedCommands = new();

    public static DeveloperConsole Instance { get; private set; }

    public static bool Active
    {
        get => Instance != null && Instance.group.activeSelf;
        set
        {
            if (Instance != null)
            {
                Instance.group.SetActive(value);
                if (value)
                {
                    Instance.input.ReleaseSelection();
                    Instance.input.Select();
                    Instance.input.ActivateInputField();
                    Instance.UpdateConsole();
                }

                Instance.eventSystem.gameObject.SetActive(EventSystem.current == null && value);
            }
            else
                Debug.LogWarning("Failed to activate developer console, no instance found in scene.");
        }
    }

    #endregion

    #region References

    [Header("References")]
    [SerializeField]
    GameObject group;

    [SerializeField]
    TMP_InputField input;
    [SerializeField]
    TMP_Text logs;
    [SerializeField]
    EventSystem eventSystem;
    [SerializeField]
    RectTransform panel;

    [Header("Keybinds")]
    [SerializeField]
    KeyCode activationKey = KeyCode.F4;

    #endregion

    #region Unity

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Active = false;
        ClearLogs();
        _commands ??= CommandAttribute.GetCommands();
    }

    int currentExecutedCommand = -1;

    private void Update()
    {
        if (Input.GetKeyDown(activationKey) && Enabled)
            Active = !Active;

        if (!Active)
            return;

        if (!Enabled)
        {
            Active = false;
            return;
        }

        // Send the command
        if (Input.GetKeyDown(KeyCode.Return))
        {
            string text = input.text.Replace("\n", "");
            if (!Input.GetKey(KeyCode.LeftShift))
                input.text = "";
            input.Select();
            input.ActivateInputField();
            SendLog(">>> " + text, DeveloperColors.User);
            currentExecutedCommand = -1;

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (executedCommands.Count <= 0 || !executedCommands[0].Equals(text))
                    executedCommands.Insert(0, text);

                if (executedCommands.Count > 30)
                    executedCommands.RemoveAt(executedCommands.Count - 1);

                CommandArguments args = ParseCommand(text, out string commandName);
                SendCommand(commandName, args);
            }
            return;
        }

        // Check for arrow keys
        bool upArrowed = Input.GetKeyDown(KeyCode.UpArrow);
        bool arrowed = upArrowed || Input.GetKeyDown(KeyCode.DownArrow);

        if (!arrowed)
            return;

        // Scroll through executed commands
        int amount = -1;
        if (upArrowed)
            amount = -amount;
        currentExecutedCommand = Mathf.Clamp(currentExecutedCommand + amount, 0, executedCommands.Count - 1);
        if (executedCommands.InRange(currentExecutedCommand))
            input.text = executedCommands[currentExecutedCommand];
    }

    #endregion

    #region Core Logic

    public void SendCommand(string commandName, CommandArguments args)
    {
        commandName = commandName.ToLower();

        if (Commands.ContainsKey(commandName))
        {
            SendLog("Executing command: " + commandName, DeveloperColors.User);
            Commands[commandName].MethodInfo.Invoke(null, 
                Commands[commandName].MethodInfo.GetParameters().Count() <= 0 
                ? new object[0] : new object[] { args });
            return;
        }

        SendLog("Command not found.", DeveloperColors.Error);
    }

    public static CommandArguments ParseCommand(string raw, out string commandName)
    {
        if (raw.Length <= 0)
        {
            commandName = "";
            return null;
        }

        List<string> elements = SplitRespectingQuotes(raw);
        commandName = elements[0];
        elements.RemoveAt(0);
        return elements.ToArray();
    }

    /// <summary>
    /// Splits the input string by spaces, but keeps quoted substrings as single tokens.
    /// Quotation marks are removed from the output.
    /// </summary>
    private static List<string> SplitRespectingQuotes(string input)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '\\' && i + 1 < input.Length && input[i + 1] == '"')
            {
                current.Append('"');
                i++; // Skip escaped quote
            }
            else if (c == '"')
                inQuotes = !inQuotes; // Toggle quoted state
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
                current.Append(c);
        }

        if (current.Length > 0)
            result.Add(current.ToString());

        return result;
    }

    #endregion

    #region Definitions

    public readonly struct CommandArguments
    {
        public readonly string[] Arguments;
        public readonly int Count => Arguments.Length;

        public CommandArguments(params string[] args) => Arguments = args;

        public readonly bool TryGet<T>(int index, out T parser) where T : ParserBase, new()
        {
            parser = new();
            if (!Arguments.InRange(index))
                return false;

            return parser.Init(Arguments[index]);
        }

        public static implicit operator CommandArguments(string[] args) => new(args);
    }

    public readonly struct Command
    {
        public readonly string Name;
        public readonly string Description;
        public readonly MethodInfo MethodInfo;

        public Command(string name, string desc, MethodInfo method)
        {
            Name = name;
            Description = desc;
            MethodInfo = method;
        }

        public override string ToString() => string.Join('\n', Name, Description);
    }

    #endregion

    #region Logging

    private static readonly StringBuilder consoleContent = new();

    public static void SendLog(object obj) => SendLog(obj, Color.white);

    public static void SendLog(object obj, DeveloperColors color) => 
        SendLog(obj, color switch
        {
            DeveloperColors.Error => Color.red,
            DeveloperColors.Warning => Color.yellow,
            DeveloperColors.Success => Color.green,
            DeveloperColors.User => Color.cyan,
            _ => Color.white
        });

    public static void SendLog(object obj, Color textColor)
    {
        if (Instance == null)
        {
            Debug.LogWarning("Failed to send log to developer console, no instance found in scene.");
            return;
        }

        consoleContent.Append($"\n<color=#{ColorUtility.ToHtmlStringRGBA(textColor)}>[{DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern)}] ");
        consoleContent.Append(obj.ToString());
        consoleContent.Append("</color>");

        if (Active)
            Instance.UpdateConsole();
    }

    public static void SendRaw(object obj)
    {
        if (Instance == null)
        {
            Debug.LogWarning("Failed to send raw text to developer console, no instance found in scene.");
            return;
        }

        consoleContent.AppendLine();
        consoleContent.Append(obj.ToString());

        if (Active)
            Instance.UpdateConsole();
    }

    public void UpdateConsole() => Instance.logs.SetText(consoleContent.ToString());

    #endregion

    #region Built-in Commands

    [Command("clear", "Clears the console.")]
    public static void ClearLogs()
    {
        if (Instance == null)
        {
            Debug.LogWarning("Failed to clear logs on developer console, no instance found in scene.");
            return;
        }

        consoleContent.Clear();

        if (Active)
            Instance.UpdateConsole();
    }

    const int commandsPerPage = 5;

    [Command("help", "Shows you all of the commands registered and how to use the command panel.")]
    protected static void HelpCommand(CommandArguments args)
    {
        int commandCount = Commands.Count;
        int totalPages = Mathf.CeilToInt(commandCount / (float)commandsPerPage);
        int page = 1;

        if (args.TryGet(0, out IntParser arg1))
        {
            page = arg1.Value;

            if (page > totalPages || page <= 0)
            {
                SendLog("That help page doesn't exist! ", DeveloperColors.Error);
                return;
            }
        }
        else if (args.TryGet(0, out StringParser arg1a))
        {
            string c = arg1a.Value.ToLower().Trim();
            if (Commands.ContainsKey(c))
                SendRaw("Command: " + Commands[c]);
            return;
        }

        string[] commandKeys = Commands.Keys.OrderBy(k => k).ToArray();

        int startIndex = (page - 1) * commandsPerPage;
        int endIndex = Mathf.Min(startIndex + commandsPerPage, commandCount);

        StringBuilder builder = new($"\n<b>Help ({page}/{totalPages})</b>\n<color=grey>--------------------</color>\nUp/Down arrow keys for previously executed commands. \nShift+Enter to send while keeping command input.\n<color=grey>--------------------</color>\n");
        for (int i = startIndex; i < endIndex; i++)
        {
            string cmd = commandKeys[i];
            builder.Append("<color=yellow>></color> <b>");
            builder.Append(cmd);
            builder.Append("</b>");
            if (!string.IsNullOrWhiteSpace(Commands[cmd].Description))
            {
                builder.Append(" - ");
                builder.Append(Commands[cmd].Description);
            }
            builder.Append('\n');
        }
        builder.Append("<color=grey>--------------------</color>\n");
        builder.Append("Type \"help [page]\" to see more.");

        SendRaw(builder);
    }

    [Command("loadscene", "Loads a scene by index. Usage: \"loadscene <int>\"")]
    protected static void LoadSceneCommand(CommandArguments args)
    {
        if (!args.TryGet(0, out IntParser arg1))
        {
            SendLog("Please provide an integer as the scene index.", DeveloperColors.Error);
            return;
        }

        if (arg1.Value < 0 || arg1.Value >= SceneManager.sceneCountInBuildSettings)
        {
            SendLog("Invalid scene index!", DeveloperColors.Error);
            return;
        }

        SendLog("Loading scene index: " + arg1.Value);
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(arg1.Value);
        loadOperation.completed += onComplete;

        void onComplete(AsyncOperation operation)
        {
            SendLog("Scene loaded: " + SceneManager.GetActiveScene().name);
            loadOperation.completed -= onComplete;
        }
    }

    static readonly Vector2Int minSize = new(350, 200);
    static readonly Vector2Int maxSize = new(1920, 1080);

    [Command("resize", "Resizes the console window. Usage: \"resize <noparse><width></noparse> [height]\"")]
    protected static void ResizeConsoleCommand(CommandArguments args)
    {
        if (!args.TryGet(0, out IntParser arg1))
        {
            SendLog("Please input an integer for width! ", DeveloperColors.Error);
            return;
        }

        int width = Mathf.Clamp(arg1.Value, minSize.x, maxSize.x);
        int height = (int)Instance.panel.sizeDelta.y;
        if (args.TryGet(1, out IntParser arg2))
            height = Mathf.Clamp(arg2.Value, minSize.y, maxSize.y);

        Vector2 size = new(width, height);
        Instance.panel.sizeDelta = size;
        SendLog("Applied console window size: " + size, DeveloperColors.Success);
    }

    #endregion
}

#region Enums

public enum DeveloperColors
{
    Error,
    Warning,
    Success,
    User
}

#endregion

#region Attribute

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class CommandAttribute : Attribute
{
    public readonly string CommandName;
    public readonly string CommandDescription;

    public CommandAttribute(string commandName, string commandDescription = "")
    {
        CommandName = commandName.ToLower().Trim();
        CommandDescription = commandDescription;
    }

    public static Dictionary<string, DeveloperConsole.Command> GetCommands() => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(m => new { Method = m, Attribute = m.GetCustomAttribute<CommandAttribute>() })
                .Where(x => x.Attribute != null))
            .ToDictionary(x => x.Attribute.CommandName, x => new DeveloperConsole.Command(x.Attribute.CommandName, x.Attribute.CommandDescription, x.Method));
}

#endregion

#region Parsers

public abstract class ParserBase
{
    protected string Argument { get; private set; }

    public bool Init(string arg)
    {
        Argument = arg;
        return Parse();
    }

    /// <summary>
    /// Set the value after parsing.
    /// </summary>
    protected abstract bool Parse();
}

public abstract class ParserBase<T> : ParserBase
{
    public T Value { get; protected set; }
}

public class IntParser : ParserBase<int>
{
    protected override bool Parse()
    {
        if (int.TryParse(Argument, out int i))
        {
            Value = i;
            return true;
        }
        return false;
    }
}

public class FloatParser : ParserBase<float>
{
    protected override bool Parse()
    {
        if (float.TryParse(Argument, out float i))
        {
            Value = i;
            return true;
        }

        return false;
    }
}

public class BoolParser : ParserBase<bool>
{
    protected override bool Parse()
    {
        if (bool.TryParse(Argument, out bool i))
        {
            Value = i;
            return true;
        }

        return false;
    }
}

public class StringParser : ParserBase<string>
{
    protected override bool Parse()
    {
        Value = Argument;
        return true;
    }
}

#endregion