using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Cameca.CustomAnalysis.GibbExcess;

public static class MachineModelDetails
{
    /*
    * Machine Model Efficiencies
    * 
    * 5000S:   MCP = 80    Mesh = 100  => 80
    * INVIZIO: MCP = 80    Mesh = 78   => 62.4
    * 5000R:   MCP = 80    Mesh = 65   => 52
    * 6000R:   MCP = 80    Mesh = 65   => 52
    * EIKOS:   MCP = 55    Mesh = 65   => 35.75
    */
    //public enum MachineType
    //{
    //    [Display(Name = "Leap 5000 S")]
    //    _5000S,
    //    [Display(Name = "Invizio 6000")]
    //    INVIZIO,
    //    [Display(Name = "Leap 5000 R")]
    //    _5000R,
    //    [Display(Name = "Leap 6000 R")]
    //    _6000R,
    //    [Display(Name = "Eikos")]
    //    EIKOS,
    //    other
    //}

    //public static readonly Dictionary<MachineType, double> MachineTypeEfficiency = new()
    //{
    //    { MachineType._5000S, 80 },
    //    { MachineType.INVIZIO, 62.4 },
    //    { MachineType._5000R, 52 },
    //    { MachineType._6000R, 52 },
    //    { MachineType.EIKOS, 35.75 }
    //};
    
    public static readonly Dictionary<string, double> MachineTypeEfficiency = new()
    {
        { "Leap 5000 S", 80 },
        { "Invizio 6000", 62.4 },
        { "Leap 5000 R", 52 },
        { "Leap 6000 R", 52 },
        { "Eikos", 35.75 }
    };
}
