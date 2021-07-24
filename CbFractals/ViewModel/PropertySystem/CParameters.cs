using CbFractals.Tools;
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
        Parameter_FramePos,

        [CGuid("1381b37a-bd4a-40ab-865c-fcdda4a1416c")]
        Parameter_JuliaRealPart,

        [CGuid("dc82d396-9bff-430c-9ef2-469b0f77ee7d")]
        Parameter_JuliaImaginaryPart,

        [CGuid("84894455-06c7-40b6-812c-b63eb537d2e6")]
        Parameter_JuliaMoveX,

        [CGuid("ef4e5902-e225-44f1-bc65-a7d42139c275")]
        Parameter_JuliaMoveY,

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
        [CDataType(typeof(double))]
        [CName(CNameEnum.Parameter_CenterX)]
        CenterX,

        [CName(CNameEnum.Parameter_CenterY)]
        [CDataType(typeof(double))]
        CenterY,

        [CName(CNameEnum.Parameter_DoubleZero)]
        [CDataType(typeof(double))]
        DoubleZero,

        [CName(CNameEnum.Parameter_FrameCount)]
        [CDataType(typeof(Int64))]
        FrameCount,

        [CName(CNameEnum.Parameter_FramePos)]
        [CDataType(typeof(Int64))]
        FrameIndex,

        [CName(CNameEnum.Parameter_JuliaRealPart)]
        [CDataType(typeof(double))]
        JuliaRealPart,

        [CName(CNameEnum.Parameter_JuliaImaginaryPart)]
        [CDataType(typeof(double))]
        JuliaImaginaryPart,

        [CName(CNameEnum.Parameter_JuliaMoveX)]
        [CDataType(typeof(double))]
        JuliaMoveX,

        [CName(CNameEnum.Parameter_JuliaMoveY)]
        [CDataType(typeof(double))]
        JuliaMoveY,

        [CName(CNameEnum.Parameter_Zoom)]
        [CDataType(typeof(double))]
        Zoom,

        [CName(CNameEnum.Parameter_Iterations)]
        [CDataType(typeof(Int64))]
        Iterations,

        [CName(CNameEnum.Parameter_DarkenThresholdLo)]
        [CDataType(typeof(double))]
        DarkenThresholdLo,

        [CName(CNameEnum.Parameter_DarkenThresholdHi)]
        [CDataType(typeof(double))]
        DarkenThresholdHi,

        [CName(CNameEnum.Parameter_ColorPeriod)]
        [CDataType(typeof(double))]
        ColorPeriod,

        [CName(CNameEnum.Parameter_ColorOffset)]
        [CDataType(typeof(double))]
        ColorOffset,
        // Rotation

        [CName(CNameEnum.Parameter_PixelAlgorithm1)]
        [CDataType(typeof(CPixelAlgorithmEnum))]
        PixelAlgorithm1,

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
                               select CDataTypeAttribute.GetByEnum((CParameterEnum)aIdx).NewParameter(this, (CParameterEnum)aIdx)).ToArray();
            this.Parameters = aParameters;
        }
        internal override void Build()
        {
            base.Build();
            foreach (var aParameter in this.Parameters)
            {
                aParameter.Build();
            }

            var aFrameCount = 900;
            this[CParameterEnum.DoubleZero].SetConst<double>(0);
            this[CParameterEnum.DoubleZero].SetEditable(false);
            this[CParameterEnum.FrameCount].SetConst<Int64>(aFrameCount, 1, Int64.MaxValue);
            this[CParameterEnum.FrameCount].Min.SetEditable(false);
            this[CParameterEnum.FrameCount].Max.SetEditable(false);
            this[CParameterEnum.FrameCount].SetValueSourceEditable(false);
            this[CParameterEnum.FrameIndex].SetConst<Int64>(aFrameCount / 2, 0, Int64.MaxValue);
            this[CParameterEnum.FrameIndex].Min.SetEditable(false);
            this[CParameterEnum.FrameIndex].Max.SetEditable(false);
            this[CParameterEnum.JuliaRealPart].As<CParameter<double>>().Min.Value = -4.3d;
            this[CParameterEnum.JuliaRealPart].As<CParameter<double>>().Max.Value = 3;
            this[CParameterEnum.JuliaRealPart].SetConst<double>(0.446d); // -0.68
            this[CParameterEnum.JuliaRealPart].SetMappedProgression(0.43d, 0.47d);
            this[CParameterEnum.JuliaImaginaryPart].As<CParameter<double>>().Min.Value = 0.3d;
            this[CParameterEnum.JuliaImaginaryPart].As<CParameter<double>>().Max.Value = 0.1d;
            this[CParameterEnum.JuliaImaginaryPart].SetConst<double>(0.24d);
            this[CParameterEnum.JuliaImaginaryPart].SetMappedProgression(0.23d, 0.26d);
            this[CParameterEnum.Zoom].SetConst<double>(0d, 1, 1000);
            this[CParameterEnum.Zoom].SetProgression(1d, 1000d);
            this[CParameterEnum.Iterations].SetConst<Int64>(300, 1, Int64.MaxValue);
            this[CParameterEnum.Iterations].SetMappedProgression(300, 10);
            this[CParameterEnum.DarkenThresholdLo].SetConst<double>(0.3);
            this[CParameterEnum.DarkenThresholdHi].SetConst<double>(0.2);
            this[CParameterEnum.ColorPeriod].SetConst<double>(1d);
            this[CParameterEnum.ColorOffset].SetConst<double>(1.5d);
            this[CParameterEnum.PixelAlgorithm1].SetConst(CPixelAlgorithmEnum.MandelbrotJuliaSingle);
            this[CParameterEnum.PixelAlgorithm1].MappedProgression.SetSelectable(false);
            this[CParameterEnum.CenterX].SetConst<double>(0.5d);
            this[CParameterEnum.CenterY].SetConst<double>(0.5d);


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
        #region Parameters
        internal readonly CParameter[] Parameters;
        private IEnumerable<CParameter> VmParametersM;
        public IEnumerable<CParameter> VmParameters => CLazyLoad.Get(ref this.VmParametersM, () => this.Parameters.OrderBy(p => p.Name));

        public CParameter this[CParameterEnum aParameterEnum]
        {
            get => this.Parameters[(int)aParameterEnum];
        }
        public T Get<T>(CParameterEnum aParameterEnum) where T : CParameter
            => (T)this[aParameterEnum];
        #endregion
        #region Parameter
        private CParameter ParameterM;
        internal CParameter Parameter
        {
            get => this.ParameterM;
            set
            {
                this.ParameterM = value;
                this.ValueSource = value;
                this.OnPropertyChanged(nameof(this.VmParameter));
            }
        }
        public CParameter VmParameter
        {
            get => this.Parameter;
            set => this.Parameter = value;
        }
        #endregion
        #region Value
        internal override object GetTypelessValue() => this.Parameter.GetTypelessValue();
        #endregion
        #region ParameterSnapshot
        internal CParameterSnapshot NewParameterSnapshot()
            => new CParameterSnapshot(this);
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
