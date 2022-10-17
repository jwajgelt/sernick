using sernick.Grammar;

namespace sernickTest.Grammar;

public class TestGrammar
{
    [Fact]
    public void Grammar_categories_priorities_are_distinct()
    {
        var grammar = new sernick.Grammar.Grammar();
        var priorities = grammar.generateGrammar().ConvertAll((grammarEntry) => grammarEntry.Category.Priority);
        // hash set size == list size => no two list elements are equal
        Assert.True(priorities.Count == priorities.ToHashSet().Count);
    }


}
