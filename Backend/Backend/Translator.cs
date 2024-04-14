using System.Text;
using Microsoft.Extensions.Primitives;

namespace Backend;

public static class Translator
{
    private enum Operations
    {
        Concatenation,
        Union,
        Repetition
    }

    private struct ExtentValue
    {
        public enum Type
        {
            Char,
            Extent
        }

        public Type ValueType { get; set; }
        public char? ValueChar { get; set; }
        public Extent? ValueExtent { get; set; }

        public ExtentValue(char c)
        {
            ValueType = Type.Char;
            ValueChar = c;
        }

        public ExtentValue(Extent e)
        {
            ValueType = Type.Extent;
            ValueExtent = e;
        }
    }

    /// <summary>
    /// Area within the automaton, subdivides the regex into manageable and convertible regions of consecutive same type operations
    /// </summary>
    private struct Extent
    {
        public Operations Operation { get; set; }
        public List<ExtentValue> Extents { get; set; } = new();
        public List<State> States { get; set; } = new();

        public Extent(Operations o)
        {
            Operation = o;
        }
    }

    /// <summary>
    /// Stores states recovered from regex, states are stored via a graph
    /// </summary>
    private struct Automaton
    {
        public List<State> States { get; set; } = new();

        public Automaton()
        {
            States.Add(new("s"));
        }

        public override string ToString()
        {
            List<char> alphabet = new();
            foreach (var state in States)
            {
                foreach (var transition in state.Transitions)
                {
                    if (!alphabet.Contains(transition.Key))
                    {
                        alphabet.Add(transition.Key);
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append('\t');
            foreach (var val in alphabet)
            {
                sb.Append($"'{val}'\t");
            }

            sb.Append('\n');
            foreach (var state in States)
            {
                sb.Append($"{state.Name}\t");
                foreach (var val in alphabet)
                {
                    if (state.Transitions.ContainsKey(val))
                    {
                        var sbb = new StringBuilder();
                        sbb.Append('{');
                        foreach (var linkState in state.Transitions[val])
                        {
                            sbb.Append($"{linkState.Name},");
                        }
                        sbb.Remove(sbb.Length - 1, 1);
                        sbb.Append('}');
                        sb.Append($"{sbb}\t");
                    }
                    else
                    {
                        sb.Append(" \t");
                    }
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }

    private class State
    {
        public string Name { get; set; }
        public Dictionary<char, HashSet<State>> Transitions { get; set; } = new();

        public State(string name)
        {
            Name = name;
        }
    }
    /// <summary>
    /// Gives a table of the automaton and a mermaid graph
    /// </summary>
    /// <param name="fullRegex">Regex to be turned into an automaton</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">User input is faulty</exception>
    /// <exception cref="NotImplementedException">It was assumed impossible to reach</exception>
    public static (string Automaton, string MermaidGraph) Translate(string fullRegex)
    {
        // Subdivides the regex into extents
        static Extent FindExtents(string regexPart)
        {
            while (regexPart.Contains("^^"))
            {
                regexPart = regexPart.Replace("^^", "^");
            }
            // Whether thrimming the regex is possible without disrupting the logic
            bool can_trim(string regex)
            {
                bool can = regex[0] == '(';
                if (!can) return false;
                int indent = 1;
                for (int i = 1; i < regex.Length; i++)
                {
                    if (regex[i] == '(')
                    {
                        indent += 1;
                    }
                    else if (regex[i] == ')')
                    {
                        indent -= 1;
                        if (indent == 0 && !(i == regex.Length - 1 || (i == regex.Length - 2 && regex[i + 1] == '^')))
                        {
                            can = false;
                            break;
                        }
                    }
                }

                return can;
            }
            // Operation determination concatenation is expected until proven otherwise
            Operations expectedOperation = Operations.Concatenation;
            while (regexPart[0] == '(' && regexPart[^2] == ')' && regexPart[^1] == '^' && can_trim(regexPart))
            {
                if (regexPart.Length >= 4)
                {
                    expectedOperation = Operations.Repetition;
                    regexPart = regexPart.Substring(1, regexPart.Length - 3);
                }
                else
                {
                    throw new InvalidDataException($"Empty repetition found in {regexPart}");
                }

            }

            while (regexPart[0] == '(' && regexPart[^1] == ')' && can_trim(regexPart))
            {
                regexPart = regexPart.Substring(1, regexPart.Length - 2);
            }
            // Repetition is more important than any other operation
            if (expectedOperation != Operations.Repetition)
            {
                int indentLevel = 0;
                foreach (var inspected in regexPart)
                {
                    switch (inspected)
                    {
                        case '|':
                            // Union is more important than concatenation
                            if (indentLevel == 0) expectedOperation = Operations.Union;
                            break;
                        case '(':
                            indentLevel += 1;
                            break;
                        case ')':
                            indentLevel -= 1;
                            break;
                        default:
                            break;
                    }
                }
            }

            Extent extent = new Extent(expectedOperation);
            // Subdivision process, recursive
            switch (expectedOperation)
            {
                case Operations.Concatenation:
                    {
                        // Characters interpreted as char extents, anything else - a subextent
                        string currentPart = "";
                        int indentLevel = 0;
                        for (int i = 0; i < regexPart.Length; i++)
                        {
                            switch (regexPart[i])
                            {
                                case '(':
                                    indentLevel += 1;
                                    currentPart += regexPart[i];
                                    if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                    {
                                        throw new InvalidDataException($"Unexpected '^' after '(' at {i + 1}");
                                    }

                                    break;
                                case ')':
                                    if (indentLevel == 0)
                                    {
                                        throw new InvalidDataException($"Unexpected ')' at {i}");
                                    }
                                    else
                                    {
                                        indentLevel -= 1;
                                    }

                                    currentPart += regexPart[i];
                                    if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                    {
                                        currentPart += regexPart[i + 1];
                                        i += 1;
                                    }

                                    if (indentLevel == 0)
                                    {
                                        extent.Extents.Add(new(FindExtents(currentPart)));
                                        currentPart = "";
                                    }
                                    break;
                                default:
                                    if (indentLevel >= 1)
                                    {
                                        currentPart += regexPart[i];
                                    }
                                    else
                                    {
                                        if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                        {
                                            extent.Extents.Add(new(new Extent()
                                            {
                                                Operation = Operations.Repetition,
                                                Extents = [new(regexPart[i])],
                                                States = new()
                                            }));
                                            i += 1;
                                        }
                                        else
                                        {
                                            extent.Extents.Add(new(regexPart[i]));
                                        }
                                    }

                                    break;
                            }
                        }

                        if (indentLevel != 0)
                        {
                            throw new InvalidDataException("Missing ')'");
                        }

                        break;
                    }
                case Operations.Union:
                    {
                        // Everything is subextents, divided by |
                        string currentPart = "";
                        int indentLevel = 0;
                        for (int i = 0; i < regexPart.Length; i++)
                        {
                            switch (regexPart[i])
                            {
                                case '(':
                                    indentLevel += 1;
                                    currentPart += regexPart[i];
                                    if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                    {
                                        throw new InvalidDataException($"Unexpected '^' after '(' at {i + 1}");
                                    }
                                    break;
                                case ')':
                                    if (indentLevel == 0)
                                    {
                                        throw new InvalidDataException($"Unexpected ')' at {i}");
                                    }
                                    else
                                    {
                                        indentLevel -= 1;
                                    }
                                    currentPart += regexPart[i];
                                    if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                    {
                                        currentPart += regexPart[i + 1];
                                        i += 1;
                                    }
                                    break;
                                case '|':
                                    if (indentLevel == 0)
                                    {
                                        if (regexPart[i + 1] == '^')
                                        {
                                            throw new InvalidDataException($"Unexpected '^' after '|' at {i + 1}");
                                        }
                                        extent.Extents.Add(new(FindExtents(currentPart)));
                                        currentPart = "";
                                    }
                                    else
                                    {
                                        currentPart += regexPart[i];
                                    }
                                    break;
                                default:
                                    currentPart += regexPart[i];
                                    break;
                            }
                        }

                        if (indentLevel != 0)
                        {
                            throw new InvalidDataException("Missing ')'");
                        }

                        if (currentPart.Length > 0)
                        {
                            extent.Extents.Add(new(FindExtents(currentPart)));
                        }
                        break;
                    }
                case Operations.Repetition:
                    {
                        // Simply recurse further
                        extent.Extents.Add(new(FindExtents(regexPart)));
                        break;
                    }
            }

            return extent;
        }

        var fullExtent = FindExtents(fullRegex);

        var automaton = new Automaton();

        void EnrichExtent(Extent localExtent)
        {
            // Add all states required to make the automata work
            switch (localExtent.Operation)
            {
                case Operations.Concatenation:
                    // if an extent is a simple character, it is stored for later reference, that also makes a corresponding state
                    for (var index = 0; index < localExtent.Extents.Count; index++)
                    {
                        var extent = localExtent.Extents[index];
                        if (extent.ValueType == ExtentValue.Type.Char)
                        {
                            automaton.States.Add(new($"q{automaton.States.Count}"));
                            localExtent.States.Add(automaton.States[^1]);
                        }
                        else
                        {
                            EnrichExtent(extent.ValueExtent!.Value);
                        }
                    }

                    break;
                case Operations.Union:
                    foreach (var extent in localExtent.Extents)
                    {
                        if (extent.ValueType == ExtentValue.Type.Extent)
                        {
                            EnrichExtent(extent.ValueExtent!.Value);
                        }
                        else
                        {
                            throw new NotImplementedException("Did not implement union char extentvalue expansion");
                        }
                    }
                    break;
                case Operations.Repetition:
                    // if an extent is a simple character, it is stored for later reference, that also makes a corresponding state
                    {
                        var extent = localExtent.Extents[0];
                        if (extent.ValueType == ExtentValue.Type.Char)
                        {
                            automaton.States.Add(new($"q{automaton.States.Count}"));
                            localExtent.States.Add(automaton.States[^1]);
                        }
                        else
                        {
                            EnrichExtent(extent.ValueExtent!.Value);
                        }
                        break;
                    }
            }
        }

        EnrichExtent(fullExtent);

        HashSet<State> MakeAutomaton(Extent localExtent, HashSet<State>? starterStates = null, HashSet<State>? repeaterStates = null)
        {
            // Connecting all the states together
            var exitStates = new HashSet<State>();
            switch (localExtent.Operation)
            {
                case Operations.Concatenation:
                    int directStates = 0;
                    for (var index = 0; index < localExtent.Extents.Count; index++)
                    {
                        var extent = localExtent.Extents[index];
                        if (extent.ValueType == ExtentValue.Type.Char)
                        {
                            // Uses the next direct state, simple chaining
                            foreach (var starterState in starterStates)
                            {
                                if (!starterState.Transitions.ContainsKey(extent.ValueChar!.Value))
                                {
                                    starterState.Transitions.Add(extent.ValueChar.Value, [localExtent.States[directStates]]);
                                }
                                else
                                {
                                    starterState.Transitions[extent.ValueChar.Value].Add(localExtent.States[directStates]);
                                }
                            }
                            starterStates = [localExtent.States[directStates]];
                            if (index == localExtent.Extents.Count - 1)
                            {
                                // In case we are within a repeater, and this is the last charcter, we need to add epsilon transitions to all possible loop start states
                                exitStates.Add(localExtent.States[directStates]);
                                if (repeaterStates != null)
                                {
                                    foreach (var returnState in repeaterStates)
                                    {
                                        if (!localExtent.States[directStates].Transitions.ContainsKey('ɛ'))
                                        {
                                            localExtent.States[directStates].Transitions.Add('ɛ', [returnState]);
                                        }
                                        else
                                        {
                                            localExtent.States[directStates].Transitions['ɛ'].Add(returnState);
                                        }
                                    }
                                }
                            }
                            directStates += 1;
                        }
                        else
                        {
                            // Recurse and use the exits for chaining
                            HashSet<State> foundExits;
                            if (index == localExtent.Extents.Count - 1)
                            {
                                foundExits = MakeAutomaton(extent.ValueExtent!.Value, starterStates, repeaterStates);
                                exitStates.UnionWith(foundExits);
                            }
                            else
                            {
                                foundExits = MakeAutomaton(extent.ValueExtent!.Value, starterStates);
                            }
                            starterStates = foundExits;
                        }
                    }
                    break;
                case Operations.Union:
                    // Recurse deeper, bring out all found exits
                    foreach (var extent in localExtent.Extents)
                    {
                        if (extent.ValueType == ExtentValue.Type.Extent)
                        {
                            exitStates.UnionWith(MakeAutomaton(extent.ValueExtent!.Value, starterStates, repeaterStates));
                        }
                        else
                        {
                            throw new NotImplementedException("Did not implement union char extentvalue expansion");
                        }
                    }
                    break;
                case Operations.Repetition:
                    {
                        var extent = localExtent.Extents[0];
                        if (extent.ValueType == ExtentValue.Type.Char)
                        {
                            // Chain the character the same way as in concatenation, but also make it refer to itself
                            var state = localExtent.States[0];
                            foreach (var starterState in starterStates)
                            {
                                if (!starterState.Transitions.ContainsKey(extent.ValueChar!.Value))
                                {
                                    starterState.Transitions.Add(extent.ValueChar.Value, [state]);
                                }
                                else
                                {
                                    starterState.Transitions[extent.ValueChar.Value].Add(state);
                                }
                            }

                            if (!state.Transitions.ContainsKey(extent.ValueChar!.Value))
                            {
                                state.Transitions.Add(extent.ValueChar.Value, [state]);
                            }
                            else
                            {
                                state.Transitions[extent.ValueChar.Value].Add(state);
                            }

                            if (repeaterStates != null)
                            {
                                foreach (var returnState in repeaterStates)
                                {
                                    if (!state.Transitions.ContainsKey('ɛ'))
                                    {
                                        state.Transitions.Add('ɛ', [returnState]);
                                    }
                                    else
                                    {
                                        state.Transitions['ɛ'].Add(returnState);
                                    }
                                }
                            }

                            exitStates.Add(state);
                            exitStates.UnionWith(starterStates);
                        }
                        else
                        {
                            // Recurse and known starters and previous repeaters as loop entry points
                            if (repeaterStates != null)
                            {
                                var t = new HashSet<State>(repeaterStates);
                                t.UnionWith(starterStates);
                                exitStates.UnionWith(MakeAutomaton(extent.ValueExtent!.Value, starterStates, t));
                            }
                            else
                            {
                                exitStates.UnionWith(MakeAutomaton(extent.ValueExtent!.Value, starterStates, starterStates));
                            }

                            exitStates.UnionWith(starterStates);
                        }
                        break;
                    }
            }
            return exitStates;
        }

        MakeAutomaton(fullExtent, [automaton.States[0]]);

        void FindFinal()
        {
            // Final states either don't have any transitions or only refer to themselves
            // Otherwise inescapable loops also have final states in their start
            static HashSet<State> FindReachableStates(State state)
            {
                HashSet<State> reached = [state];
                var newReached = new HashSet<State>();
                bool changed = false;
                while (true)
                {
                    foreach (var origin in reached)
                    {
                        foreach (var dest in origin.Transitions)
                        {
                            foreach (var target in dest.Value)
                            {
                                if (!reached.Contains(target))
                                {
                                    newReached.Add(target);
                                    changed = true;
                                }
                            }
                        }
                    }
                    if (!changed)
                    {
                        return reached;
                    }
                    else
                    {
                        reached.UnionWith(newReached);
                        newReached.Clear();
                        changed = false;
                    }
                }
            }

            foreach (var state in automaton.States)
            {
                if (state.Transitions.Count == 0 || FindReachableStates(state).Count == 1)
                {
                    state.Name = "*" + state.Name;
                }
                else if (automaton.States.Any(x=>x.Transitions.Any(y=>y.Key == 'ɛ' && y.Value.Contains(state))))
                {
                    bool closedLoop = true;
                    var reachableStates = FindReachableStates(state);
                    foreach (var test in reachableStates)
                    {
                        if (!FindReachableStates(test).SetEquals(reachableStates))
                        {
                            closedLoop = false;
                            break;
                        }
                    }

                    if (closedLoop)
                    {
                        state.Name = "*" + state.Name;
                    }
                }
            }

        }

        FindFinal();

        static string MakeGraph(List<State> states)
        {
            var sb = new StringBuilder();
            sb.Append("flowchart LR\n");
            foreach (var state in states)
            {
                foreach (var pair in state.Transitions)
                {
                    foreach (var dest in pair.Value)
                    {
                        sb.AppendLine($"{state.Name}(({state.Name})) -->|{pair.Key}| {dest.Name}(({dest.Name}))");
                    }
                }
            }
            return sb.ToString();
        }

        return (automaton.ToString(), MakeGraph(automaton.States));
    }
}