using CbFractals.Tools;
using CbFractals.ViewModel.Render;
using CbFractals.ViewModel.SceneManager; // TODO-abstrahieren?
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CbFractals.ViewModel.PropertySystem
{


    public enum CNameEnum
    {
        [CGuid("0e3ae2ee-4e6f-4104-ac7c-9a766b38579d")]
        Constant,

        [CGuid("6947d618-1b32-4fc1-9167-72df4ebab00d")]
        Min,

        [CGuid("9589a0e3-c935-4ba5-9ecb-7b70a690aa8d")]
        Max,

        [CGuid("07c49d2f-7824-49a1-9d4f-a7ad5fc8792c")]
        MappedProgression,

        [CGuid("8602b4fc-5a3f-4aab-b743-04dd05e14684")]
        ParameterRef_ControlMin,

        [CGuid("9aae2525-786e-45f0-baad-15f5b86edee7")]
        ParameterRef_ControlMax,

        [CGuid("e566b2d6-eaff-469e-8cfe-05f5c605c63f")]
        ParameterRef_ControlValue,

        [CGuid("cb3a35ff-bf99-4a05-b729-3a314a3e8a5a")]
        ParameterRef_MappedMin,

        [CGuid("36711a15-cbc5-4b36-95ec-671b211b4480")]
        ParameterRef_MappedMax,

        [CGuid("31c6217e-417b-40e6-ae25-793cefb3b5db")]
        Parameters,

        [CGuid("a36f7633-a6fa-45ee-83a5-c0cf674cd802")]
        Parameter_CenterX,

        [CGuid("ad54e80d-ab69-49b1-bcd2-43dcd6af71a2")]
        Parameter_CenterY,

        [CGuid("a6fc4052-386e-4f30-99ab-278cf8af3021")]
        Parameter_DoubleZero,

        [CGuid("a19d056f-c94d-4941-85ff-5a6d5aaa50c6")]
        Parameter_FrameCount,

        [CGuid("6e2b59d2-8e62-42a7-808f-ecab1639de80")]
        Parameter_FrameIndex,

        [CGuid("1381b37a-bd4a-40ab-865c-fcdda4a1416c")]
        Parameter_JuliaPartReal,

        [CGuid("dc82d396-9bff-430c-9ef2-469b0f77ee7d")]
        Parameter_JuliaPartImaginary,

        //[CGuid("84894455-06c7-40b6-812c-b63eb537d2e6")]
        //Parameter_JuliaMoveX,

        //[CGuid("ef4e5902-e225-44f1-bc65-a7d42139c275")]
        //Parameter_JuliaMoveY,

        [CGuid("13d6f48d-89d7-41ca-898c-0f7c68eb52b8")]
        Parameter_Zoom,

        [CGuid("380d6d30-8083-4ae6-b291-d644f06a1a3f")]
        Parameter_Iterations,

        [CGuid("ea4a1691-74d5-4a58-b7dd-6219d055d971")]
        Parameter_DarkenThresholdLo,

        [CGuid("e85a4dd0-f632-49f5-b755-669881996745")]
        Parameter_DarkenThresholdHi,

        [CGuid("7dae15e2-f6b5-4c3f-bc12-9a8b3cfffb05")]
        Parameter_ColorPeriod,

        [CGuid("b53519c7-6479-454b-986d-5a6783cfd919")]
        Parameter_ColorOffset,

        [CGuid("5797cdbe-ff8d-4bbb-8f2a-7f470682a6b7")]
        Parameter_PixelAlgorithm1,

        [CGuid("1fa7d763-1f5b-4c6e-9df9-e7f9b3ed1ec9")]
        Parameter_BeatIndex,

        [CGuid("89ebec47-6a1f-4a24-b3c1-520feb1c9e5e")]
        Parameter_BeatIndex_Max,

        [CGuid("1517129f-4ec4-49b7-ae6e-2ec16bafdf13")]
        Parameter_BeatCount,

        [CGuid("25767fd8-ac17-4110-a30b-de8ac73134a1")]
        Parameter_SecondIndex,

        [CGuid("14e8d7dd-1123-4fd0-8861-02f8cea74db5")]
        Parameter_SecondCount,

        [CGuid("fe6f4de2-761a-4a4d-86ff-2cceac8f81ec")]
        CurrentValue,

        [CGuid("0305e5a1-dcb9-4acc-ad82-202d72efc8b6")]
        ParameterRef,

        [CGuid("cd672e18-57f2-4766-bcb3-17bc8af6df18")]
        Parameter_BeatsPerMinute,

        [CGuid("6f4461d9-a7fc-4259-84db-ea370b0f255b")]
        Parameter_FramesPerSecond,

        [CGuid("6b3f5fa1-7014-48f5-af7c-bd03690cd793")]
        InputParameters,

        [CGuid("3a9760cd-a341-4e07-aaa4-47efb0aa9f4b")]
        FuncProgression,

        [CGuid("c9b036b8-afc3-4bf2-9b56-ab9ae8106425")]
        Func_SecondsToBeatCount,

        [CGuid("7c16033f-8b48-4631-9f44-67cf1db08bf2")]
        Func_SecondsToBeatCount_In_Seconds,

        [CGuid("1405ed4d-f4d3-44d3-b4aa-fcf285503fb5")]
        Func_SecondsToFrameCount,

        [CGuid("f0e2139e-6483-40a0-926c-0e749b571207")]
        Func_SecondsToFrameCount_In_Seconds,

        [CGuid("f055b084-182f-4806-b29c-9bcebc67a50c")]
        Parameter_ColorAlgorithm,
    }

    public sealed class CNameAttribute : Attribute
    {
        public CNameAttribute(CNameEnum aName)
        {
            this.NameEnum = aName;
        }
        internal readonly CNameEnum NameEnum;
    }

    public enum CParameterEnum
    {
        [CType(typeof(double))]
        [CName(CNameEnum.Parameter_CenterX)]
        CenterX,

        [CName(CNameEnum.Parameter_CenterY)]
        [CType(typeof(double))]
        CenterY,

        [CName(CNameEnum.Parameter_DoubleZero)]
        [CType(typeof(double))]
        DoubleZero,

        [CName(CNameEnum.Parameter_FrameCount)]
        [CType(typeof(double))]
        FrameCount,

        [CName(CNameEnum.Parameter_FrameIndex)]
        [CType(typeof(double))]
        FrameIndex,

        [CName(CNameEnum.Parameter_JuliaPartReal)]
        [CType(typeof(double))]
        JuliaPartReal,

        [CName(CNameEnum.Parameter_JuliaPartImaginary)]
        [CType(typeof(double))]
        JuliaPartImaginary,

        //[CName(CNameEnum.Parameter_JuliaMoveX)]
        //[CDataType(typeof(double))]
        //JuliaMoveX, // TODO_OBSOLETE

        //[CName(CNameEnum.Parameter_JuliaMoveY)]
        //[CDataType(typeof(double))]
        //JuliaMoveY, // TODO_OBSOLETE

        [CName(CNameEnum.Parameter_Zoom)]
        [CType(typeof(double))]
        Zoom,

        [CName(CNameEnum.Parameter_Iterations)]
        [CType(typeof(double))]
        Iterations,

        [CName(CNameEnum.Parameter_DarkenThresholdLo)]
        [CType(typeof(double))]
        DarkenThresholdLo,

        [CName(CNameEnum.Parameter_DarkenThresholdHi)]
        [CType(typeof(double))]
        DarkenThresholdHi,

        [CName(CNameEnum.Parameter_ColorPeriod)]
        [CType(typeof(double))]
        ColorPeriod,

        [CName(CNameEnum.Parameter_ColorOffset)]
        [CType(typeof(double))]
        ColorOffset,
        // Rotation

        [CName(CNameEnum.Parameter_PixelAlgorithm1)]
        [CType(typeof(CPixelAlgorithmEnum))]
        PixelAlgorithm1,

        [CName(CNameEnum.Parameter_BeatIndex)]
        [CType(typeof(double))]
        BeatIndex,

        [CName(CNameEnum.Parameter_BeatIndex_Max)]
        [CType(typeof(double))]
        BeatIndex_Max,

        [CName(CNameEnum.Parameter_BeatsPerMinute)]
        [CType(typeof(double))]
        BeatsPerMinute,

        [CName(CNameEnum.Parameter_BeatCount)]
        [CType(typeof(double))]
        BeatCount,

        [CName(CNameEnum.Parameter_SecondIndex)]
        [CType(typeof(double))]
        SecondIndex,

        [CName(CNameEnum.Parameter_SecondCount)]
        [CType(typeof(double))]
        SecondCount,

        [CName(CNameEnum.Parameter_FramesPerSecond)]
        [CType(typeof(double))]
        FramesPerSecond,

        [CName(CNameEnum.Parameter_ColorAlgorithm)]
        [CType(typeof(CColorAlgorithmEnum))]
        ColorAlgorithm,

        _Count
    }
    // Oscillator : Mit amplitudenmodulation und frequenzmodulation "2 schritte vor 1 zurück" realisieren.
    //  RingelOsci
    //  RingelOsciFm
    //  RingelOsciAm1
    //  RingelOsciAm2
    //  RingelOsciInverter
    //  Pulse
    //  RandomPulse

    // Palleten

    public sealed class CParameters : CValueNode
    {
        #region ctor
        internal CParameters(CProgressionManager aParentProgressionManager) : base(aParentProgressionManager, CNameEnum.Parameters)
        {
            var aParameterCount = (int)CParameterEnum._Count;
            var aParameters = (from aIdx in Enumerable.Range(0, aParameterCount)
                               select CTypeAttribute.GetByEnum((CParameterEnum)aIdx).NewParameter(this, (CParameterEnum)aIdx)).ToArray();
            this.Parameters = aParameters;
        }
        internal override void Build()
        {
            base.Build();
            foreach (var aParameter in this.Parameters)
            {
                aParameter.Build();
            }

            //var aFrameCount = 900;
            var aSecondCount = 10d;
            this[CParameterEnum.SecondCount].SetConst(aSecondCount, 0, double.MaxValue);
            this[CParameterEnum.SecondCount].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.SecondIndex].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.SecondIndex].SetConst(aSecondCount / 2, 0, double.MaxValue);
            this[CParameterEnum.SecondIndex].Min.SetEditable(false);
            this[CParameterEnum.SecondIndex].Max.SetEditable(false);

            this[CParameterEnum.DoubleZero].SetConst<double>(0);
            this[CParameterEnum.DoubleZero].SetEditable(false);
            //this[CParameterEnum.FrameCount].SetConst<Int64>(aFrameCount, 1, Int64.MaxValue);
            this[CParameterEnum.FrameCount].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.FrameCount].As<CDoubleParameter>().SetFuncProgression(CNameEnum.Func_SecondsToFrameCount, true, CParameterEnum.SecondCount);
            this[CParameterEnum.FrameCount].Min.SetEditable(false);
            this[CParameterEnum.FrameCount].Max.SetEditable(false);
            this[CParameterEnum.FrameCount].SetValueSourceEditable(false);
            //this[CParameterEnum.FrameIndex].SetConst<Int64>(aFrameCount / 2, 0, Int64.MaxValue);
            this[CParameterEnum.FrameIndex].As<CDoubleParameter>().MapToRange = false;
            this.SetFrameIndexBySecondIndex();
            this[CParameterEnum.FrameIndex].Min.SetEditable(false);
            this[CParameterEnum.FrameIndex].Max.SetEditable(false);
            this[CParameterEnum.FrameIndex].SetEditable(false);
            this[CParameterEnum.JuliaPartReal].As<CNumericParameter<double>>().Min.Value = -4.3d;
            this[CParameterEnum.JuliaPartReal].As<CNumericParameter<double>>().MaxConstant.Value = 3;
            this[CParameterEnum.JuliaPartReal].SetConst<double>(0.446d); // -0.68
            this[CParameterEnum.JuliaPartReal].SetMappedProgression(0.43d, 0.47d);
            this[CParameterEnum.JuliaPartImaginary].As<CNumericParameter<double>>().Min.Value = 0.3d;
            this[CParameterEnum.JuliaPartImaginary].As<CNumericParameter<double>>().MaxConstant.Value = 0.1d;
            this[CParameterEnum.JuliaPartImaginary].SetConst<double>(0.24d);
            this[CParameterEnum.JuliaPartImaginary].SetMappedProgression(0.23d, 0.26d);
            this[CParameterEnum.Zoom].SetConst<double>(1d, 1, 2.5d);
            //this[CParameterEnum.Zoom].SetMappedProgression(0, 1);
            this[CParameterEnum.Iterations].SetConst<double>(0.3d, 10, 1000);
            this[CParameterEnum.Iterations].SetMappedProgression(0, 1);
            this[CParameterEnum.DarkenThresholdLo].SetConst<double>(0, 0.2, 0.1);
            //this[CParameterEnum.DarkenThresholdLo].SetMappedProgression(0.3, 0);
            this[CParameterEnum.DarkenThresholdHi].SetConst<double>(0.2);
            this[CParameterEnum.DarkenThresholdHi].SetMappedProgression(0.2, 0);
            this[CParameterEnum.ColorPeriod].SetConst<double>(0d, 1d, 3d);
            this[CParameterEnum.ColorOffset].SetConst<double>(0d, 1.5d, 1.6d);
            this[CParameterEnum.PixelAlgorithm1].SetConst(CPixelAlgorithmEnum.MandelbrotJuliaSingle);
            this[CParameterEnum.PixelAlgorithm1].MappedProgression.SetSelectable(false);
            this[CParameterEnum.CenterX].SetConst<double>(0.5d);
            this[CParameterEnum.CenterY].SetConst<double>(0.5d);
            this[CParameterEnum.BeatCount].SetConst<double>(1, 1, double.MaxValue);
            this[CParameterEnum.BeatCount].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.BeatCount].As<CDoubleParameter>().SetFuncProgression(CNameEnum.Func_SecondsToBeatCount, true, CParameterEnum.SecondCount);
            this[CParameterEnum.BeatCount].SetEditable(false);

            this[CParameterEnum.BeatIndex_Max].SetConst(0, 0, double.MaxValue);
            this[CParameterEnum.BeatIndex_Max].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.BeatIndex_Max].SetParameterRef(CParameterEnum.BeatCount, true); // .SetConst<double>(0, 0, 127);
            this[CParameterEnum.BeatIndex_Max].SetEditable(false);
            //this[CParameterEnum.BeatIndex].SetConst<double>(0, 0, 127);
            this[CParameterEnum.BeatIndex].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.BeatIndex].SetMax(CParameterEnum.BeatIndex_Max);
            this[CParameterEnum.BeatIndex].As<CDoubleParameter>().SetFuncProgression(CNameEnum.Func_SecondsToBeatCount, true, CParameterEnum.SecondIndex);
            this[CParameterEnum.BeatIndex].SetEditable(false);
            this[CParameterEnum.BeatsPerMinute].SetConst<double>(128, 1, double.MaxValue);
            this[CParameterEnum.BeatsPerMinute].SetEditable(false);
            this[CParameterEnum.BeatsPerMinute].Constant.SetEditable(true);
            this[CParameterEnum.BeatsPerMinute].As<CDoubleParameter>().MapToRange = false;
            this[CParameterEnum.FramesPerSecond].SetConst(30, 1, double.MaxValue);
            this[CParameterEnum.FramesPerSecond].SetEditable(false);
            this[CParameterEnum.FramesPerSecond].As<CDoubleParameter>().MapToRange = false;

            

            //var aJuliaConstReal = this[CParameterEnum.JuliaConstRealPart];
            //aJuliaConstReal.SetConst<double>(-0.7d);
            //aJuliaConstReal.SetConst<double>(-0.68);
            //aJuliaConstReal.MappedProgression.MappedMin.Value = -0.677d;
            //aJuliaConstReal.MappedProgression.MappedMax.Value = -1.5d;//  - 0.88d;
            //aJuliaConstReal.Progression = aJuliaConstReal.MappedProgression;

            //var aJuliaConstImaginaryPart = this[CParameterEnum.JuliaConstImaginaryPart];
            ////aJuliaConstImaginaryPart.SetConst(-0.27015d);
            //aJuliaConstImaginaryPart.SetConst<double>(0.3d);
            ////aJuliaConstImaginaryPart.MappedProgression.MappedMin.Value = 0.3d;
            ////aJuliaConstImaginaryPart.MappedProgression.MappedMax.Value = 0.1d;
            //aJuliaConstImaginaryPart.Progression = aJuliaConstImaginaryPart.MappedProgression;


            //this[CParameterEnum.JuliaMoveX].SetConst<double>(-0.07d);
            //this[CParameterEnum.JuliaMoveY].SetConst<double>(0.305d);
            //            this[CParameterEnum.Iterations].SetConst<Int64>(300);
            //this[CParameterEnum.DarkenThresholdLo].SetConst<double>(0.1);
            //this[CParameterEnum.DarkenThresholdHi].SetConst<double>(0.2);
            //this[CParameterEnum.ColorPeriod].SetConst<double>(1d);
            //this[CParameterEnum.ColorOffset].SetConst<double>(1.5d);
            //this[CParameterEnum.PixelAlgorithm1].SetConst(CPixelAlgorithmEnum.MandelbrotJuliaSingle);
            //this[CParameterEnum.CenterX].SetConst<double>(0.5d);
            //this[CParameterEnum.CenterY].SetConst<double>(0.5d);
        }
        #endregion
        #region Render
        /// <summary>
        /// Legt fest, dass der FrameIndex anhand der Sekunden berechnet wird.
        /// Diese Einstellung wird während dem Editieren verwendet.
        /// </summary>
        internal void SetFrameIndexBySecondIndex()
            => this[CParameterEnum.FrameIndex].As<CDoubleParameter>().SetFuncProgression(CNameEnum.Func_SecondsToFrameCount, true, CParameterEnum.SecondIndex);

        /// <summary>
        /// Legt fest, dass der FrameIndex anhand der Konstante verwendet wird.
        /// Diese Einstellung wird während dem Rendern verwendet.
        /// </summary>
        internal void SetFrameIndexByConst(double aFrameIndex)
            => this[CParameterEnum.FrameIndex].SetConst<double>(aFrameIndex, true);


        #endregion

        #region Parameters
        internal readonly CParameter[] Parameters;
        public CParameter this[CParameterEnum aParameterEnum]
        {
            get => this.Parameters[(int)aParameterEnum];
        }
        public T Get<T>(CParameterEnum aParameterEnum) where T : CParameter
            => (T)this[aParameterEnum];
        #endregion
        #region Parameter        
        internal CParameter Parameter
        {
            get => (CParameter) this.ValueSource;
            set => this.ValueSource = value;
        }
        #endregion
        #region Value
        internal override object GetTypelessValue() => this.Parameter.GetTypelessValue();
        #endregion
        #region ParameterSnapshot
        internal CParameterSnapshot NewParameterSnapshot()
            => new CParameterSnapshot(this);
        #endregion
        #region ShowAll
        private bool ShowAllM;
        internal bool ShowAll 
        { 
            get => this.ShowAllM; 
            set
            {
                this.ShowAllM = value;
                this.OnPropertyChanged(nameof(this.VmShowAll));
                this.OnPropertyChanged(nameof(this.VmValueSources));
            }
        }
        public bool VmShowAll { get => this.ShowAll; set => this.ShowAll = value; }
        #endregion
        #region VmValueSources
        internal override IEnumerable<CValueNode> ValueSources => this.Parameters;
        public override IEnumerable<CValueNode> VmValueSources => this.ShowAll ? base.VmValueSources : base.VmValueSources.Where(vs => vs.ValueSourceEditable);
        #endregion
        internal override IEnumerable<CValueNode> StoredNodes => this.Parameters;
        private DirectoryInfo DirectoryInfo => new FileInfo(this.GetType().Assembly.Location).Directory;
        //private FileInfo DefaultFileInfo=>new FileInfo(Environment.GetFolderPath(Path.Combine(Environment.SpecialFolder.MyDocuments), )
        private FileInfo DefaultFileInfo => new FileInfo(System.IO.Path.Combine(this.DirectoryInfo.FullName, "DefaultSettings", "Default.CbFractal.Progression.xml"));
        internal void SaveAsDefault()
        {
            CXmlProgressionSnapshot.Save(this, this.DefaultFileInfo);
        }
        internal override IEnumerable<CValueNode> SubValueNodes => this.Parameters;
        internal override bool ValueSourceSetRecalculates => false;

    }

    internal sealed class CParameterSnapshot
    {
        internal CParameterSnapshot(CParameters aParameters)
        {
            this.Values = aParameters.Parameters.Select(aParam => aParam.GetTypelessValue()).ToArray();
        }
        private object[] Values;
        private object this[CParameterEnum aParameterEnum]
        {
            get => this.Values[(int)aParameterEnum];
        }
        internal T Get<T>(CParameterEnum e)
            => (T)this[e];
    }



}
