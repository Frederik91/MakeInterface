using System;
using System.Collections.Generic;
using System.Text;

namespace MakeInterface.Generator.Attributes;

public class GenerateInterfaceAttribute : Attribute
{
    public string[]? Exclude { get; set; }
}
