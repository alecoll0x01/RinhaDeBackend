var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RinhaDeBackend>("rinhadebackend");

builder.Build().Run();
