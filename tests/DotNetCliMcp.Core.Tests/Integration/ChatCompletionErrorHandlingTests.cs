using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using Xunit;

namespace DotNetCliMcp.Core.Tests.Integration;

/// <summary>
/// Integration tests for chat completion error handling scenarios.
/// These tests simulate the errors that can occur when connecting to LM Studio.
/// </summary>
public class ChatCompletionErrorHandlingTests
{
    private readonly ILogger<ChatCompletionErrorHandlingTests> _logger;

    public ChatCompletionErrorHandlingTests()
    {
        _logger = Substitute.For<ILogger<ChatCompletionErrorHandlingTests>>();
    }

    [Fact]
    public async Task ChatCompletion_WithInvalidEndpoint_ShouldThrowHttpOperationException()
    {
        // Arrange
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: "test-model",
                apiKey: "test-key",
                endpoint: new Uri("http://localhost:9999/v1")) // Invalid port
            .Build();

        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddUserMessage("test message");

        // Act & Assert
        // Semantic Kernel wraps HttpRequestException in HttpOperationException
        await Assert.ThrowsAnyAsync<HttpOperationException>(async () =>
        {
            await chatService.GetChatMessageContentAsync(history);
        });
    }

    [Fact]
    public void ChatHistory_RemoveLastMessage_ShouldHandleEmptyHistory()
    {
        // Arrange
        var history = new ChatHistory();

        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            if (history.Count > 0)
            {
                history.RemoveAt(history.Count - 1);
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void ChatHistory_RemoveLastMessage_ShouldRemoveCorrectMessage()
    {
        // Arrange
        var history = new ChatHistory();
        history.AddSystemMessage("system");
        history.AddUserMessage("user1");
        history.AddAssistantMessage("assistant1");
        history.AddUserMessage("user2");

        var initialCount = history.Count;

        // Act
        history.RemoveAt(history.Count - 1);

        // Assert
        Assert.Equal(initialCount - 1, history.Count);
        Assert.Equal("assistant1", history[history.Count - 1].Content);
    }

    [Fact]
    public void ChatHistory_AddMessagesAfterRemoval_ShouldMaintainConsistency()
    {
        // Arrange
        var history = new ChatHistory();
        history.AddSystemMessage("system");
        history.AddUserMessage("user1");
        history.AddAssistantMessage("assistant1");

        // Act
        history.RemoveAt(history.Count - 1); // Remove assistant message
        history.AddAssistantMessage("assistant2"); // Add new assistant message

        // Assert
        Assert.Equal(3, history.Count);
        Assert.Equal("assistant2", history[history.Count - 1].Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ResponseContent_WithNullOrEmpty_ShouldBeDetectable(string? content)
    {
        // Arrange & Act
        var isValid = !string.IsNullOrEmpty(content);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void EndpointUri_WithV1Path_ShouldBeValid()
    {
        // Arrange & Act
        var endpoint = new Uri("http://127.0.0.1:1234/v1");

        // Assert
        Assert.Equal("http://127.0.0.1:1234/v1", endpoint.ToString());
        Assert.Equal("/v1", endpoint.PathAndQuery);
    }

    [Fact]
    public void EndpointUri_WithoutV1Path_ShouldNotHavePath()
    {
        // Arrange & Act
        var endpoint = new Uri("http://127.0.0.1:1234");

        // Assert
        Assert.Equal("http://127.0.0.1:1234/", endpoint.ToString());
        Assert.Equal("/", endpoint.PathAndQuery);
    }

    [Theory]
    [InlineData("ArgumentOutOfRangeException", true)]
    [InlineData("HttpRequestException", false)]
    [InlineData("InvalidOperationException", false)]
    public void ExceptionTypeCheck_ForKnownErrors_ShouldIdentifyCorrectly(
        string exceptionTypeName, 
        bool shouldMatchArgumentOutOfRange)
    {
        // Arrange
        Exception? exception = exceptionTypeName switch
        {
            "ArgumentOutOfRangeException" => new ArgumentOutOfRangeException("index"),
            "HttpRequestException" => new HttpRequestException("Connection failed"),
            "InvalidOperationException" => new InvalidOperationException("Invalid state"),
            _ => null
        };

        // Act
        var isArgumentOutOfRange = exception is ArgumentOutOfRangeException argEx 
            && argEx.Message.Contains("index");

        // Assert
        Assert.Equal(shouldMatchArgumentOutOfRange, isArgumentOutOfRange);
    }

    [Fact]
    public void ArgumentOutOfRangeException_WithIndexParameter_ShouldMatchExpectedPattern()
    {
        // Arrange
        var exception = new ArgumentOutOfRangeException("index", "Specified argument was out of the range of valid values.");

        // Act
        var isExpectedError = exception is ArgumentOutOfRangeException && 
                             exception.Message.Contains("index");

        // Assert
        Assert.True(isExpectedError);
        Assert.Contains("index", exception.Message);
    }

    [Fact]
    public void MultipleErrors_InSequence_ShouldEachBeHandledIndependently()
    {
        // This test simulates multiple error scenarios in sequence
        // to ensure error handling doesn't break the application state

        // Arrange
        var history = new ChatHistory();
        history.AddSystemMessage("system");

        // Simulate first error scenario
        history.AddUserMessage("query1");
        // Error occurs, remove last message
        if (history.Count > 0)
        {
            history.RemoveAt(history.Count - 1);
        }

        // Simulate second error scenario
        history.AddUserMessage("query2");
        // Error occurs, remove last message
        if (history.Count > 0)
        {
            history.RemoveAt(history.Count - 1);
        }

        // Simulate successful scenario
        history.AddUserMessage("query3");
        history.AddAssistantMessage("response3");

        // Assert
        Assert.Equal(3, history.Count); // system + user + assistant
        Assert.Equal("query3", history[1].Content);
        Assert.Equal("response3", history[2].Content);
    }
}
