using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Windows;
using Prism.Mvvm;
using static Cameca.CustomAnalysis.GibbExcess.MachineModelDetails;

namespace Cameca.CustomAnalysis.GibbExcess;

public class GibbExcessProperties : BindableBase
{
    [Display(AutoGenerateField = false)]
    public List<string> IonNames { get; set; } = new();

    private string ionOfInterest = "";
    [Display(Name = "Ion of Interest", Description = "The ion in which the gibbsian excess calculation will be performed on")]
    public string IonOfInterest
    {
        get => ionOfInterest;
        set
        {
            if (IonNames.Contains(value))
                SetProperty(ref ionOfInterest, value);
            else
            {
                MessageBox.Show("Pick a valid ion in this analysis");
                SetProperty(ref ionOfInterest, "");
            }
        }
    }

    private double selectionStart;
    [Display(Name = "Start of Selection (nm)")]
    public double SelectionStart
    {
        get => selectionStart;
        set => SetProperty(ref selectionStart, value);
    }

    private double selectionEnd;
    [Display(Name = "End of selection (nm)")]
    public double SelectionEnd
    {
        get => selectionEnd;
        set => SetProperty(ref selectionEnd, value);
    }

    private MachineType machineModel = MachineType.other;
    [Display(Name = "Machine Model")]
    public MachineType MachineModel
    {
        get => machineModel;
        set
        {
            SetProperty(ref machineModel, value);

            if (value == MachineType.other)
                detectorEfficiency = 0.0;
            else
                detectorEfficiency = MachineTypeEfficiency[value];

            RaisePropertyChanged(nameof(DetectorEfficiency));
        }
    }

    private double? detectorEfficiency = 0;
    [Display(Name = "Detector Efficiency (%)", Description = "Corresponds to the physical machine used to take this measurement")]
    public double? DetectorEfficiency
    {
        get => detectorEfficiency;
        set
        {
            SetProperty(ref detectorEfficiency, value);

            machineModel = MachineType.other;
            RaisePropertyChanged(nameof(MachineModel));
        }
    }

    //public enum Symbols
    //{
    //    H,He,Li,Be,B,C,N,O,F,Ne,Na,Mg,Al,Si,P,S,Cl,Ar,K,Ca,Sc,Ti,V,Cr,Mn,Fe,Co,Ni,Cu,Zn,Ga,Ge,As,Se,Br,Kr,Rb,Sr,Y,Zr,Nb,Mo,Tc,Ru,Rh,Pd,Ag,Cd,In,
    //    Sn,Sb,Te,I,Xe,Cs,Ba,La,Ce,Pr,Nd,Pm,Sm,Eu,Gd,Tb,Dy,Ho,Er,Tm,Yb,Lu,Hf,Ta,W,Re,Os,Ir,Pt,Au,Hg,Tl,Pb,Bi,Po,At,Rn,Fr,Ra,Ac,Th,Pa,U,Np,Pu,Am,Cm,Bk,Cf,Es,
    //    Fm,Md,No,Lw
    //}
}

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
    public enum MachineType 
    {
        [Display(Name = "Leap 5000 S")]
        _5000S,
        [Display(Name = "Invizio 6000")]
        INVIZIO,
        [Display(Name = "Leap 5000 R")]
        _5000R,
        [Display(Name = "Leap 6000 R")]
        _6000R,
        [Display(Name = "Eikos")]
        EIKOS,
        other 
    }
    public static readonly Dictionary<MachineType, double> MachineTypeEfficiency = new()
    {
        { MachineType._5000S, 80 },
        { MachineType.INVIZIO, 62.4 },
        { MachineType._5000R, 52 },
        { MachineType._6000R, 52 },
        { MachineType.EIKOS, 35.75 }
    };
}