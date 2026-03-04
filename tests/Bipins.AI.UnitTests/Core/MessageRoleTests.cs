using Bipins.AI.Core.Models;
using Xunit;

namespace Bipins.AI.UnitTests.Core;

public class MessageRoleTests
{
    [Fact]
    public void MessageRole_Values_AreDefined()
    {
        _ = MessageRole.User;
        _ = MessageRole.Assistant;
        _ = MessageRole.System;
        _ = MessageRole.Tool;
    }

    [Fact]
    public void MessageRole_Values_AreDistinct()
    {
        Assert.NotEqual(MessageRole.User, MessageRole.Assistant);
        Assert.NotEqual(MessageRole.User, MessageRole.System);
        Assert.NotEqual(MessageRole.Assistant, MessageRole.System);
        Assert.NotEqual(MessageRole.Tool, MessageRole.User);
    }
}
