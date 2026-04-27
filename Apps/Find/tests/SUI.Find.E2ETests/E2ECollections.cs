namespace SUI.Find.E2ETests;

[CollectionDefinition("E2E-V1")]
public class E2EV1Collection : ICollectionFixture<FunctionTestFixture> { }

[CollectionDefinition("E2E-V2")]
public class E2EV2Collection : ICollectionFixture<FunctionTestFixture> { }

[CollectionDefinition("E2E")]
public class FunctionTestCollectionFixture : ICollectionFixture<FunctionTestFixture> { }
