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
        enum Type
        {
            Char,
            Extent
        }

        Type ValueType { get; set; }
        char? ValueChar { get; set; }
        Extent? ValueExtent { get; set; }

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
        public List<ExtentValue> Extents { get; set; }

        public Extent(Operations o)
        {
            Operation = o;
            Extents = new List<ExtentValue>();
        }
    }

    public static string Translate(string fullRegex)
    {
        static Extent FindExtents(string regexPart)
        {
            while (regexPart.Contains("^^"))
            {
                regexPart = regexPart.Replace("^^", "^");
            }

            Operations expectedOperation = Operations.Concatenation;
            if (regexPart[0] == '(' && regexPart[^2] == ')' && regexPart[^1] == '^' && regexPart.Length >= 4)
            {
                expectedOperation = Operations.Repetition;
                regexPart = regexPart.Substring(1, regexPart.Length - 3);
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
                    bool searchingForEnd = false;
                    for (int i = 0; i < regexPart.Length; i++)
                    {
                        switch (regexPart[i])
                        {
                            case '(':
                                searchingForEnd = true;
                                currentPart += regexPart[i];
                                if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                {
                                    throw new InvalidDataException($"Unexpected '^' after '(' at {i + 1}");
                                }

                                break;
                            case ')':
                                if (!searchingForEnd)
                                {
                                    throw new InvalidDataException($"Unexpected ')' at {i}");
                                }

                                currentPart += regexPart[i];
                                if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                {
                                    currentPart += regexPart[i + 1];
                                    i += 1;
                                }
                                searchingForEnd = false;
                                extent.Extents.Add(new(FindExtents(currentPart)));
                                currentPart = "";
                                break;
                            default:
                                if (searchingForEnd)
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
                                            Extents = [new(regexPart[i])]
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

                    if (searchingForEnd)
                    {
                        throw new InvalidDataException("Missing ')'");
                    }

                    break;
                }
                case Operations.Union:
                {
                    string currentPart = "";
                    bool searchingForEnd = false;
                    for (int i = 0; i < regexPart.Length; i++)
                    {
                        switch (regexPart[i])
                        {
                            case '(':
                                searchingForEnd = true;
                                extent.Extents.Add(new(FindExtents(currentPart)));
                                currentPart = "";
                                currentPart += regexPart[i];
                                if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                {
                                    throw new InvalidDataException($"Unexpected '^' after '(' at {i + 1}");
                                }
                                break;
                            case ')':
                                if (!searchingForEnd)
                                {
                                    throw new InvalidDataException($"Unexpected ')' at {i}");
                                }
                                currentPart += regexPart[i];
                                if (i + 1 < regexPart.Length && regexPart[i + 1] == '^')
                                {
                                    currentPart += regexPart[i + 1];
                                    i += 1;
                                }
                                searchingForEnd = false;
                                extent.Extents.Add(new(FindExtents(currentPart)));
                                currentPart = "";
                                break;
                            case '|':
                                if (!searchingForEnd)
                                {
                                    extent.Extents.Add(new(FindExtents(currentPart)));
                                    currentPart = "";
                                }
                                break;
                            default:
                                currentPart += regexPart[i];
                                break;
                        }
                    }

                    if (searchingForEnd)
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

        //TODO: Implement translation from intermediate to table of states, and return table and mermaid graph strings
        FindExtents(fullRegex);
        return "Garbage";
    }
}