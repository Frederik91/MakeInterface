using MakeInterface.Contracts.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeInterface.Demo.Models;

[GenerateInterface]
public class Class2 : IClass2
{
    public IClass1? Class1 { get; }
}
