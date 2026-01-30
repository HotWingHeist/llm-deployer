Feature: Model Management
  As a user of LLM Deployer
  I want to load and manage local LLM models
  So that I can run inference on them

  Scenario: Load a model successfully
    Given I have a model file at "C:\models\llama2.bin"
    When I load the model
    Then the model should be loaded and running
    And the model should have a unique ID

  Scenario: List all loaded models
    Given I have loaded 2 models
    When I request the list of loaded models
    Then I should get a list with 2 models

  Scenario: Unload a model
    Given I have a loaded model with ID "test-model-1"
    When I unload the model
    Then the model should no longer be in the loaded models list

  Scenario: Run inference on a model
    Given I have a loaded model
    When I run inference with prompt "What is AI?"
    Then I should receive a response
