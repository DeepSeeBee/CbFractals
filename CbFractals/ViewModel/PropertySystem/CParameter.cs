using CbFractals.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private CConstant MaxConstantM;
        internal CConstant MaxConstant => CLazyLoad.Get(ref this.MaxConstantM, () => this.NewConstant(this, Name_Max));
        private CParameter MaxParameterNullable { get; set; }
        internal CValueNode Max => this.MaxParameterNullable is object ? (CValueNode)this.MaxParameterNullable : (CValueNode)this.MaxConstant;
        public CValueNode VmMax => this.Max;

        internal abstract CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName);
        #endregion        
        #region LinearProgression
        internal readonly CMappedProgression MappedProgression;
        #endregion
        #region ParameterRefProgression
        private CParameterRefProgression ParameterRefProgressionM;
        internal CParameterRefProgression ParameterRefProgression => CLazyLoad.Get(ref this.ParameterRefProgressionM, () => new CParameterRefProgression(this, CNameEnum.ParameterRef));
        #endregion
        #region FuncProgression
        private CFuncProgression FuncProgressionM;
        internal CFuncProgression FuncProgression => CLazyLoad.Get(ref this.FuncProgressionM, () => new CFuncProgression(this, CNameEnum.FuncProgression));
        #endregion
        #region Progressions
        private IEnumerable<CProgression> ProgressionsPrivate
        {
            get
            {
                yield return this.Constant;
                yield return this.MappedProgression;
                yield return this.ParameterRefProgression;
                yield return this.FuncProgression;
            }
        }
        internal IEnumerable<CProgression> Progressions => this.ProgressionsPrivate.Where(p => p.Selectable);
        public IEnumerable<CProgression> VmProgressions => this.Progressions.OrderBy(p=>p.VmName);
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
                yield return this.MaxConstant;
                if (this.MaxParameterNullable is object)
                    yield return this.MaxParameterNullable;
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
                this.OnValueChanged();
                //this.ParentProgressionManager.OnChangeRenderFrameOnDemand();
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
            this.MaxConstant.SetDefaultMax();
            this.CurrentValue.SetEditable(false);

            foreach (var aProgression in this.Progressions)
            {
                aProgression.Build();
            }
        }
        internal T As<T>() where T : CParameter => (T)this;

        internal void SetConst<T>(T v, bool aSelect = false)
        {
            this.Constant.As<CConstant<T>>().Value = v;
            if(aSelect)
            {
                this.Progression = this.Constant;
            }
        }
        internal void SetConst<T>(T aValue, T aMin, T aMax)
        {
            this.SetConst(aValue);
            this.Min.As<CConstant<T>>().Value = aMin;
            this.MaxConstant.As<CConstant<T>>().Value = aMax;
        }

        internal void SetParameterRef(CParameter aParameter, bool aSelect = false)
        {
            this.ParameterRefProgression.ParameterRef.Parameter = aParameter;
            if(aSelect)
            {
                this.Progression = this.ParameterRefProgression;
            }
        }

        internal void SetParameterRef(CParameterEnum e, bool aSelect = false)
            => this.SetParameterRef(this.ParentProgressionManager.Parameters[e], aSelect);

        internal void SetMappedProgression(double aMappedMin, double aMappedMax)
        {
            var aMappedProgression = this.MappedProgression;
            aMappedProgression.MappedMin.Value = aMappedMin;
            aMappedProgression.MappedMax.Value = aMappedMax;
        }

        //internal void SetProgression(double aFrom, double aTo)
        //{
        //    this.As<CParameter<double>>().Min.Value = aFrom;
        //    this.As<CParameter<double>>().MaxConstant.Value = aTo;
        //}
        #endregion
        #region ActualValue
        private CConstant CurrentValueM;
        private CConstant CurrentValue => CLazyLoad.Get(ref this.CurrentValueM, () => this.NewConstant(this, CNameEnum.CurrentValue));
        public CConstant VmCurrentValue => this.CurrentValue;
        internal void UpdateCurrentValue()
            => this.CurrentValue.SetTypelessValue(this.GetTypelessValue());
        internal void SetMax(CParameter p)
            => this.MaxParameterNullable = p;
        internal void SetMax(CParameterEnum p)
            => this.SetMax(this.ParentProgressionManager.Parameters[p]);
        #endregion
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ParameterCurrentValue;
        internal override void SetEditable(bool v)
        {
            base.SetEditable(v);
            this.ParameterRefProgression.SetEditable(v);
        }

        internal void SetConst()
        {
            this.Progression = this.Constant;
        }
    }

    public abstract class CParameter<T> : CParameter
    {
        internal CParameter(CParameters aParentParameters, CParameterEnum aParameterEnum)
        : base(aParentParameters, aParameterEnum)
        {
        }
        internal new CConstant<T> Min => (CConstant<T>)base.Min;
        internal new CConstant<T> MaxConstant => (CConstant<T>)base.MaxConstant;
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
        internal override object GetTypelessValue() => this.MapToRange ? this.GetMappedValue() : base.GetTypelessValue();

        internal bool MapToRange = true;
        internal double GetMappedValue()
            => ((double)base.GetTypelessValue()).Map(0d, 1d, this.Min.Value, this.Max.GetDoubleValue());
        internal override object ConverTo(object aValue) => Convert.ToDouble(aValue);
        internal override void Build()
        {
            base.Build();

            this.MappedProgression.MappedMin.Value = 0d;
            this.MappedProgression.MappedMax.Value = 1d;
        }

        internal void SetFuncProgression(CNameEnum aFuncNameEnum, bool aSelect = false, params CParameterEnum[] aParameters)
        {
            var aFunc = this.FuncProgression.Funcs.Find(aFuncNameEnum);
            this.FuncProgression.Func = aFunc;
            if(aSelect)
            {
                this.Progression = this.FuncProgression;
            }
            foreach(var i in Enumerable.Range(0, aParameters.Length))
            {
                aFunc.InputParameters.InputParameters[i].ParameterRef.SetValueSource(aParameters[i]);
            }            
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
