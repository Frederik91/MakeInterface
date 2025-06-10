using System;
using System.Collections.Generic;
using System.Text;

namespace MakeInterface;

internal class GenerateInterfaceAttribute : Attribute
{
    public string[]? Exclude { get; set; }
}
