﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MakeInterface.Contracts.Attributes;
public class GenerateInterfaceAttribute : Attribute
{
    public List<string>? ExcludedMembers { get; set; }
}
