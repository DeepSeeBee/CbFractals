using CbFractals.Tools;
using System;
using System.Collections.Generic;
using System.Text;

namespace CbFractals.ViewModel.PropertySystem
{
    public abstract class CParameter : CValueNode
    {
        internal CParameter(CParameters aParameters, CParameterEnum aParameterEnum) : base(aParameters.ParentProgressionManager, aParameterEnum.GetNameEnum())
        {
            this.ParentParameters = aParameters;
            this.ParameterEnum = aParameterEnum;
            this.MappedProgression = new CMappedProgression(this, Name_MappedProgression);
        }
        internal readonly CParameters ParentParameters;
        internal readonly CParameterEnum ParameterEnum;
        #region Constant
        private CConstant ConstantM;
        internal CConstant Constant => CLazyLoad.Get(ref this.ConstantM, () => this.NewConstant(this, Name_Constant));
        public CConstant VmConstant => this.Constant;

        private CConstant MinM;
        internal CConstant Min => CLazyLoad.Get(ref this.MinM, () => this.NewConstant(this, Name_Min));
        public CConstant VmMin => this.Min;
        private CConstant MaxM;
        internal CConstant Max => CLazyLoad.Get(ref this.MaxM, () => this.NewConstant(this, Name_Max));
        public CConstant VmMax => this.Max;

        internal abstract CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName);
        #endregion        
        #region LinearProgression
        internal readonly CMappedProgression MappedProgression;
        #endregion
        #region Progressions
        internal IEnumerable<CProgression> Progressions
        {
            get
            {
                yield return this.Constant;
                if (this.MappedProgression.Selectable)
                    yield return this.MappedProgression;
            }
        }
        public IEnumerable<CProgression> VmProgressions => this.Progressions;
        internal override IEnumerable<CValueNode> ValueSources => this.Progressions;
        internal override IEnumerable<CValueNode> SubValueNodes
        {
            get
            {
                foreach (var aNode in base.SubValueNodes)
                    yield return aNode;
                foreach (var aProgression in this.Progressions)
                    yield return aProgression;
                yield return this.Min;
                yield return this.Max;
            }
        }
        #endregion
        #region Progression
        private CProgression ProgressionM;
        internal CProgression Progression
        {
            get => CLazyLoad.Get(ref this.ProgressionM, () => this.Constant);
            set
            {
                this.ProgressionM = value;
                this.ParentProgressionManager.OnChangeRenderFrameOnDemand();
                this.OnPropertyChanged(nameof(this.VmProgression));
            }
        }
        internal override CValueNode ValueSource { get => this.Progression; set { this.Progression = (CProgression)value; base.ValueSource = value; } }
        public CProgression VmProgression
        {
            get => this.Progression;
            set => this.Progression = value;
        }

        internal abstract object ConverTo(object aValue);
        internal override object GetTypelessValue() => this.ConverTo(this.Progression.GetTypelessValue());
        #endregion
        #region Build
        internal override void Build()
        {
            base.Build();

            this.Progression = this.Constant;
            this.Min.SetDefaultMin();
            this.Max.SetDefaultMax();

            foreach (var aProgression in this.Progressions)
            {
                aProgression.Build();
            }
        }
        internal T As<T>() where T : CParameter => (T)this;

        internal void SetConst<T>(T v)
        {
            this.Constant.As<CConstant<T>>().Value = v;
        }
        internal void SetConst<T>(T aValue, T aMin, T aMax)
        {
            this.SetConst(aValue);
            this.Min.As<CConstant<T>>().Value = aMin;
            this.Max.As<CConstant<T>>().Value = aMax;
        }

        internal void SetMappedProgression(double aMappedMin, double aMappedMax)
        {
            var aMappedProgression = this.MappedProgression;
            aMappedProgression.MappedMin.Value = aMappedMin;
            aMappedProgression.MappedMax.Value = aMappedMax;
        }

        internal void SetProgression(double aFrom, double aTo)
        {
            this.As<CParameter<double>>().Min.Value = aFrom;
            this.As<CParameter<double>>().Max.Value = aTo;
        }
        #endregion


    }

    public abstract class CParameter<T> : CParameter
    {
        internal CParameter(CParameters aParentParameters, CParameterEnum aParameterEnum)
        : base(aParentParameters, aParameterEnum)
        {
        }
        internal new CConstant<T> Min => (CConstant<T>)base.Min;
        internal new CConstant<T> Max => (CConstant<T>)base.Max;
    }

    public abstract class CEnumParameter<T> : CParameter
    {
        internal CEnumParameter(CParameters aParentParameters, CParameterEnum aParameterEnum)
        : base(aParentParameters, aParameterEnum)
        {
        }
        internal override void Build()
        {
            base.Build();
            this.Min.Supported = false;
            this.Max.Supported = false;
        }
        internal override object ConverTo(object aValue) => aValue is T ? (T)aValue : Enum.Parse(typeof(T), aValue.ToString());
    }

    public sealed class CInt64Parameter : CParameter<Int64>
    {
        public CInt64Parameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName) => new CInt64Constant(aParentValueNode, aName);
        internal override object ConverTo(object aValue) => Convert.ToInt64(aValue);
    }
    public sealed class CDoubleParameter : CParameter<double>
    {
        public CDoubleParameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {

        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CDoubleConstant(aParentValueNode, aName);
        internal override object GetTypelessValue() => this.GetMappedValue();
        internal double GetMappedValue()
            => ((double)base.GetTypelessValue()).Map(0d, 1d, this.Min.Value, this.Max.Value);
        internal override object ConverTo(object aValue) => Convert.ToDouble(aValue);
        internal override void Build()
        {
            base.Build();

            this.MappedProgression.MappedMin.Value = 0d;
            this.MappedProgression.MappedMax.Value = 1d;
        }
    }

    internal sealed class CParameterClassRegistry : Dictionary<Type, Type>
    {
        private CParameterClassRegistry()
        {
            this.Add(typeof(Int64), typeof(CInt64Parameter));
            this.Add(typeof(double), typeof(CDoubleParameter));
            this.Add(typeof(CPixelAlgorithmEnum), typeof(CPixelAlgorithmEnumParameter));
        }
        internal static readonly CParameterClassRegistry Singleton = new CParameterClassRegistry();
    }

}
