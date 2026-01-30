using TechTalk.SpecFlow;
using Xunit;
using LLMDeployer.Core.Services;
using LLMDeployer.Core.Models;

namespace LLMDeployer.Specs.StepDefinitions;

[Binding]
public class ModelManagementSteps
{
    private readonly ModelManager _modelManager = new();
    private LlmModel? _currentModel;
    private IEnumerable<LlmModel>? _loadedModels;
    private string? _inferenceResult;

    [Given(@"I have a model file at ""(.*)""")]
    public void GivenIHaveAModelFileAt(string filePath)
    {
        // This step assumes the file exists for testing purposes
        // In production, you'd validate file existence
        Assert.False(string.IsNullOrWhiteSpace(filePath));
    }

    [When(@"I load the model")]
    public async Task WhenILoadTheModel()
    {
        var modelPath = "C:\\models\\llama2.bin";
        _currentModel = await _modelManager.LoadModelAsync(modelPath);
    }

    [Then(@"the model should be loaded and running")]
    public void ThenTheModelShouldBeLoadedAndRunning()
    {
        Assert.NotNull(_currentModel);
        Assert.True(_currentModel.IsRunning);
    }

    [Then(@"the model should have a unique ID")]
    public void ThenTheModelShouldHaveAUniqueId()
    {
        Assert.NotNull(_currentModel);
        Assert.NotEmpty(_currentModel.Id);
    }

    [Given(@"I have loaded (\d+) models")]
    public async Task GivenIHaveLoadedModels(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _modelManager.LoadModelAsync($"C:\\models\\model{i}.bin");
        }
    }

    [When(@"I request the list of loaded models")]
    public async Task WhenIRequestTheListOfLoadedModels()
    {
        _loadedModels = await _modelManager.GetLoadedModelsAsync();
    }

    [Then(@"I should get a list with (\d+) models")]
    public void ThenIShouldGetAListWithModels(int count)
    {
        Assert.NotNull(_loadedModels);
        Assert.Equal(count, _loadedModels.Count());
    }

    [Given(@"I have a loaded model")]
    public async Task GivenIHaveALoadedModel()
    {
        _currentModel = await _modelManager.LoadModelAsync("C:\\models\\test.bin");
    }

    [Given(@"I have a loaded model with ID ""(.*)""")]
    public async Task GivenIHaveALoadedModelWithId(string modelId)
    {
        _currentModel = await _modelManager.LoadModelAsync("C:\\models\\test.bin");
    }

    [When(@"I unload the model")]
    public async Task WhenIUnloadTheModel()
    {
        Assert.NotNull(_currentModel);
        await _modelManager.UnloadModelAsync(_currentModel.Id);
    }

    [Then(@"the model should no longer be in the loaded models list")]
    public async Task ThenTheModelShouldNoLongerBeInTheLoadedModelsList()
    {
        var models = await _modelManager.GetLoadedModelsAsync();
        Assert.DoesNotContain(_currentModel, models);
    }

    [When(@"I run inference with prompt ""(.*)""")]
    public async Task WhenIRunInferenceWithPrompt(string prompt)
    {
        Assert.NotNull(_currentModel);
        _inferenceResult = await _modelManager.InferenceAsync(_currentModel.Id, prompt);
    }

    [Then(@"I should receive a response")]
    public void ThenIShouldReceiveAResponse()
    {
        Assert.NotNull(_inferenceResult);
        Assert.NotEmpty(_inferenceResult);
    }
}
