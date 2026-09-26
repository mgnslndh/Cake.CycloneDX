using Cake.Frosting;

return new CakeHost()
    .UseContext<Frosting.ScenarioContext>()
    .Run(args);
