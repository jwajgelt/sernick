namespace sernickTest.CodeGeneration.InstructionSelection;

using Moq;
using sernick.CodeGeneration.InstructionSelection;

public static class CodeTreePatternRuleHelper
{
    public static CodeTreeNodePatternRule AsRule(this CodeTreePattern pattern) =>
        new(pattern, new Mock<CodeTreeNodePatternRule.GenerateInstructionsDelegate>().Object);
}
