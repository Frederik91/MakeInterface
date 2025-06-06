using System;
using System.Collections.Generic;
using System.Text;

namespace MakeInterface;

public class GenerateInterfaceAttribute : Attribute
{
    public string[]? Exclude { get; set; }
}
