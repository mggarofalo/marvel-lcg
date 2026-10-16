using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Selects a deterministic legal declaration for an unambiguous payment.</summary>
internal sealed class PaidResourceDeclarationSearch(
    string generated, int cost, string required)
{
    private readonly char[] selected = new char[cost];
    private string? declaredPayment;

    internal string Find()
    {
        Search(start: 0, chosen: 0);
        return declaredPayment
            ?? throw new RulesNotImplementedException(
                "the generated resources have no legal declared payment");
    }

    private bool Search(int start, int chosen)
    {
        if (chosen == selected.Length)
        {
            var declared = selected.ToArray();
            return DeclareWild(index: 0);
        }

        int left = selected.Length - chosen;
        for (int index = start; index <= generated.Length - left; index++)
        {
            selected[chosen] = generated[index];
            if (Search(index + 1, chosen + 1))
            {
                return true;
            }
        }
        return false;
    }

    private bool DeclareWild(int index)
    {
        var declared = selected.ToArray();
        return DeclareWild(declared, index);
    }

    private bool DeclareWild(char[] declared, int index)
    {
        while (index < declared.Length && declared[index] != Resources.Wild) index++;
        if (index < declared.Length)
        {
            foreach (char declaration in Resources.Types)
            {
                declared[index] = declaration;
                if (DeclareWild(declared, index + 1)) return true;
            }
            declared[index] = Resources.Wild;
            return false;
        }
        if (!SatisfiesRequired(declared)) return false;
        declaredPayment = new string(declared);
        return true;
    }

    private bool SatisfiesRequired(char[] declared)
    {
        var pool = declared.ToList();
        foreach (char requiredType in required)
        {
            int found = pool.IndexOf(requiredType);
            if (found < 0) return false;
            pool.RemoveAt(found);
        }
        return true;
    }
}
