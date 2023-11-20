using Cameca.CustomAnalysis.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.GibbExcess;

public static class ExtensionUtils
{
    public static Dictionary<string, byte> GetIonTypes(IIonData ionData)
    {
        Dictionary<string, byte> ionTypes = new();

        byte count = 1;
        foreach (var ionType in ionData.Ions)
            ionTypes.Add(ionType.Name, count++);

        return ionTypes;
    }
}
