var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.SmartShift_Api>("api");

builder.Build().Run();
