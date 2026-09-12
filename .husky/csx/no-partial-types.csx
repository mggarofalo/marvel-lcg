var failed = false;
string root = RepositoryRoot();
foreach (string path in ApplicationSourceFiles(root))
{
    string source = File.ReadAllText(Path.Combine(root, path));
    var declarations = new List<(string Name, int Line)>(PartialDeclarations(source));
    foreach (var declaration in declarations)
    {
        if (IsRequiredGeneratedType(path, source, declarations, declaration.Name))
        {
            continue;
        }

        Console.Error.WriteLine(
            $"{path}:{declaration.Line}: partial types are not allowed in application source");
        failed = true;
    }
}

return failed ? 1 : 0;

static bool IsRequiredGeneratedType(
    string path,
    string source,
    IReadOnlyList<(string Name, int Line)> declarations,
    string type)
{
    // Godot's C# source generator requires Node scripts to be partial. System.Text.Json's
    // source generator likewise requires its context declaration to be partial. Exact
    // file, namespace, type and declaration count keep those requirements from becoming
    // a general exception for application code.
    var required = path switch
    {
        "src/Marvel.Godot/Main.cs" => (Namespace: "Marvel.Godot", Type: "Main"),
        "src/Marvel.Godot/CardControl.cs" => (Namespace: "Marvel.Godot", Type: "CardControl"),
        "src/Marvel.Godot/DecisionPanel.cs" => (Namespace: "Marvel.Godot", Type: "DecisionPanel"),
        "src/Marvel.Server/EngineJson.cs" => (Namespace: "Marvel.Server", Type: "EngineJsonContext"),
        _ => default,
    };
    return required.Type is not null
        && declarations.Count == 1
        && type == required.Type
        && HasSoleFileScopedNamespace(source, required.Namespace);
}

static bool HasSoleFileScopedNamespace(string source, string expected)
{
    string[] parts = expected.Split('.');
    var tokens = Tokens(source);
    var matches = false;
    var namespaces = 0;
    for (int index = 0; index < tokens.Count; index++)
    {
        if (tokens[index].Text != "namespace") continue;
        namespaces++;
        int cursor = index + 1;
        var actual = new List<string>();
        while (cursor < tokens.Count && IsIdentifier(tokens[cursor].Text))
        {
            actual.Add(tokens[cursor++].Text);
            if (cursor >= tokens.Count || tokens[cursor].Text != ".") break;
            cursor++;
        }
        if (SameParts(actual, parts)
            && cursor < tokens.Count
            && tokens[cursor].Text == ";")
        {
            matches = true;
        }
    }
    return namespaces == 1 && matches;
}

static bool SameParts(IReadOnlyList<string> actual, IReadOnlyList<string> expected)
{
    if (actual.Count != expected.Count) return false;
    for (int index = 0; index < actual.Count; index++)
    {
        if (actual[index] != expected[index]) return false;
    }
    return true;
}

static IEnumerable<(string Name, int Line)> PartialDeclarations(string source)
{
    var tokens = Tokens(source);
    for (int index = 0; index < tokens.Count; index++)
    {
        if (tokens[index].Text != "partial" || index + 2 >= tokens.Count)
        {
            continue;
        }

        int kind = index + 1;
        while (kind < tokens.Count && IsDeclarationModifier(tokens[kind].Text))
        {
            kind++;
        }
        if (kind >= tokens.Count) continue;
        if (tokens[kind].Text == "record" && kind + 1 < tokens.Count
            && tokens[kind + 1].Text is "class" or "struct")
        {
            kind++;
        }

        if (tokens[kind].Text is not ("class" or "record" or "struct" or "interface")
            || kind + 1 >= tokens.Count)
        {
            continue;
        }

        string name = tokens[kind + 1].Text;
        if (IsIdentifier(name))
        {
            yield return (name, tokens[index].Line);
        }
    }
}

static bool IsDeclarationModifier(string text) => text is
    "abstract" or "file" or "internal" or "new" or "private" or "protected"
    or "public" or "readonly" or "ref" or "sealed" or "static" or "unsafe";

static List<(string Text, int Line)> Tokens(string source)
{
    var tokens = new List<(string Text, int Line)>();
    int line = 1;
    for (int index = 0; index < source.Length;)
    {
        char current = source[index];
        if (current == '\n') { line++; index++; continue; }
        if (char.IsWhiteSpace(current)) { index++; continue; }
        if (current == '/' && index + 1 < source.Length && source[index + 1] == '/')
        {
            index += 2;
            while (index < source.Length && source[index] != '\n') index++;
            continue;
        }
        if (current == '/' && index + 1 < source.Length && source[index + 1] == '*')
        {
            index += 2;
            while (index + 1 < source.Length
                && !(source[index] == '*' && source[index + 1] == '/'))
            {
                if (source[index] == '\n') line++;
                index++;
            }
            index = Math.Min(source.Length, index + 2);
            continue;
        }
        if (current == '"')
        {
            int delimiter = 1;
            while (index + delimiter < source.Length && source[index + delimiter] == '"')
            {
                delimiter++;
            }
            if (delimiter >= 3)
            {
                index += delimiter;
                while (index < source.Length)
                {
                    if (source[index] == '\n') line++;
                    if (source[index] == '"')
                    {
                        int closing = 1;
                        while (index + closing < source.Length && source[index + closing] == '"')
                        {
                            closing++;
                        }
                        if (closing >= delimiter) { index += delimiter; break; }
                        index += closing;
                        continue;
                    }
                    index++;
                }
                continue;
            }

            bool verbatim = index > 0 && source[index - 1] == '@'
                || index > 1 && source[index - 1] == '$' && source[index - 2] == '@';
            index++;
            while (index < source.Length)
            {
                if (source[index] == '\n') line++;
                if (verbatim && source[index] == '"' && index + 1 < source.Length
                    && source[index + 1] == '"') { index += 2; continue; }
                if (source[index] == '"') { index++; break; }
                if (!verbatim && source[index] == '\\' && index + 1 < source.Length)
                { index += 2; continue; }
                index++;
            }
            continue;
        }
        if (current == '\'')
        {
            index++;
            while (index < source.Length)
            {
                if (source[index] == '\\' && index + 1 < source.Length) { index += 2; continue; }
                if (source[index] == '\'') { index++; break; }
                index++;
            }
            continue;
        }
        if (IsIdentifierStart(current))
        {
            int start = index++;
            while (index < source.Length && IsIdentifierPart(source[index])) index++;
            tokens.Add((source[start..index], line));
            continue;
        }
        if (current is '.' or ';' or '{')
        {
            tokens.Add((current.ToString(), line));
        }
        index++;
    }
    return tokens;
}

static bool IsIdentifier(string text)
{
    if (text.Length == 0 || !IsIdentifierStart(text[0])) return false;
    for (int index = 1; index < text.Length; index++)
    {
        if (!IsIdentifierPart(text[index])) return false;
    }
    return true;
}
static bool IsIdentifierStart(char value) => value == '_' || char.IsLetter(value);
static bool IsIdentifierPart(char value) => value == '_' || char.IsLetterOrDigit(value);

static IEnumerable<string> ApplicationSourceFiles(string root)
{
    foreach (string path in GitOutput(
                 root, "ls-files", "-z", "--cached", "--others", "--exclude-standard")
             .Split('\0', StringSplitOptions.RemoveEmptyEntries))
    {
        if (path.StartsWith("src/", StringComparison.Ordinal)
            && Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase)
            && File.Exists(Path.Combine(root, path)))
        {
            yield return path;
        }
    }
}

static string RepositoryRoot()
{
    string root = GitOutput(Environment.CurrentDirectory, "rev-parse", "--show-toplevel").Trim();
    if (root.Length == 0)
    {
        throw new InvalidOperationException("Git returned an empty repository root");
    }
    return root;
}

static string GitOutput(string workingDirectory, params string[] arguments)
{
    var start = new System.Diagnostics.ProcessStartInfo("git")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        WorkingDirectory = workingDirectory,
    };
    foreach (string argument in arguments)
    {
        start.ArgumentList.Add(argument);
    }

    using var process = System.Diagnostics.Process.Start(start)
        ?? throw new InvalidOperationException("Could not start git");
    var outputTask = process.StandardOutput.ReadToEndAsync();
    using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(1));
    try
    {
        process.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult();
    }
    catch (System.OperationCanceledException)
    {
        process.Kill(true);
        throw new TimeoutException("Git did not finish within one minute");
    }
    string output = outputTask.GetAwaiter().GetResult();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException("Git command failed");
    }
    return output;
}
