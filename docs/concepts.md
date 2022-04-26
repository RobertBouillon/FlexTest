# Concepts






# Goals
- Test assertions should avoid raising exceptions, if possible. Exceptions are expensive and can slow down unit tests.
- Implicit Parallelization - tests should be independent and parallelizable. Any dependencies should be built into the tests
- Test classes are contained within the assemblies and classes they test, excluded by preprocessor directive. This gives tests access to private and internal members.


# Use-Cases

## Debug test with output (incomplete)
As an engineer, I want to view the log / console output of a test as it runs, so I can:
1. Monitor progress and better debug the test as it runs.
2. Better identify the nature of a bug if the test fails





# Test Types

## Unit Tests
Targets an independent unit of code - typically a single function.

A unit test's dependencies are typically a data set specific to the arguments used to invoke the function.

## Functional Tests
Targets a specific feature / requirement.

Functional tests typically involve multiple components and have dependencies.

External dependencies for functional tests can be mocked out.

## Integration Tests
Functional tests with external dependencies in which the dependencies are not mocked.

## Load Tests
Tests which are run under load in a load-testing environment which is both production-like and production-sized, designed to simulate the load that would exist in a production environment.

## Stress Tests
Extended load tests designed to identify bugs which might only appear after extended periods of operation.

## Performance Tests
Functional tests with mocked dependencies are timed to identify performance problems resulting from code changes.


# Patterns

## Static Test Methods

## Test Classes

## Test Preprocessor

