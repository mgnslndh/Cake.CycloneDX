using Cake.Frosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Build.Tasks;

[TaskName("All")]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(TestTask))]
[IsDependentOn(typeof(PackTask))]
[IsDependentOn(typeof(DogfoodTask))]
public class AllTask : FrostingTask
{
}