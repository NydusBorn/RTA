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

    public static (string Automaton, string MermaidGraph) Translate(string fullRegex)
    {
        static Extent FindExtents(string regexPart)
        {
            while (regexPart.Contains("^^"))
            {
                regexPart = regexPart.Replace("^^", "^");
            }

            Operations expectedOperation = Operations.Concatenation;
            if (regexPart[0] == '(' && regexPart[^2] == ')' && regexPart[^1] == '^' )
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

            while (regexPart[0] == '(' && regexPart[^1] == ')')
            {
                regexPart = regexPart.Substring(1, regexPart.Length - 2);
            }
            if (expectedOperation != Operations.Repetition)
            {
                int indentLevel = 0;
                foreach (var inspected in regexPart)
                {
                    switch (inspected)
                    {
                        case '|':
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
            switch (expectedOperation)
            {
                case Operations.Concatenation:
                    {
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
                        extent.Extents.Add(new(FindExtents(regexPart)));
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException("expectedOperation is out of range");
            }

            return extent;
        }

        var fullExtent = FindExtents(fullRegex);

        var automaton = new Automaton();

        void EnrichExtent(Extent localExtent)
        {
            switch (localExtent.Operation)
            {
                case Operations.Concatenation:
                    foreach (var extent in localExtent.Extents)
                    {
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
                default:
                    throw new ArgumentOutOfRangeException("expectedOperation is out of range");
            }
        }

        EnrichExtent(fullExtent);

        HashSet<State> MakeAutomaton(Extent localExtent, HashSet<State>? starterStates = null, HashSet<State>? repeaterStates = null)
        {
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
                        }
                        else
                        {
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
                default:
                    throw new ArgumentOutOfRangeException("expectedOperation is out of range");
            }
            return exitStates;
        }

        MakeAutomaton(fullExtent, [automaton.States[0]]);

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