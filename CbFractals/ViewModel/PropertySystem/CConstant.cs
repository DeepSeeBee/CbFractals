using CbFractals.Tools;
using CbFractals.ViewModel.Mandelbrot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbFractals.ViewModel.PropertySystem
{
    public abstract class CConstant : CProgression
    {
        #region ctor
        internal CConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal virtual void Increment() => throw new NotImplementedException();
        #endregion
        //internal CConstant<T> As<T>()=>(CConstant<T>)this;
        internal T As<T>() where T : CConstant => (T)this;
        internal override bool IsData => true;
        internal virtual void SetDefaultMin() { }
        internal virtual void SetDefaultMax() { }
    }

    public abstract class CConstant<T> : CConstant
    {
        #region ctor
        internal CConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        // internal override object ValueAsObject { get => this.Value; set => this.Value = (T)(object)value; }
        private T ValueM;
        internal T Value
        {
            get => this.ValueM;
            set
            {
                if (!object.Equals(this.ValueM, value))
                {
                    this.ValueM = value;
                    this.OnValueChanged();
                    this.OnPropertyChanged(nameof(this.VmValue));
                    this.OnPropertyChanged(nameof(this.VmValueText));

                }
            }
        }
        public string VmValueText
        {
            get => this.ToString(this.Value);
            set
            {
                this.Value = this.Parse(value);
            }
        }
        public T VmValue { get => this.Value; set => this.Value = value; }
        internal override object GetTypelessValue() => this.Value;
        internal abstract T Parse(string s);
        internal virtual string ToString(T aValue) => aValue.ToString();
        #endregion
        #region Gui
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.TextBox;
        internal override string StoredData
        {
            get => this.VmValueText;
            set
            {
                try
                {
                    this.VmValueText = value;
                }
                catch (Exception)
                {
                    // TODO-may happen when switching os-language
                }
            }
        }
        #endregion
    }
    internal sealed class CDoubleConstant : CConstant<double>
    {
        #region ctor
        internal CDoubleConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override double Parse(string s) => double.Parse(s);
        #endregion
        #region Gui
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.TextBox;
        #endregion
        internal override void SetDefaultMin() => this.Value = 0d;
        internal override void SetDefaultMax() => this.Value = 1d;
    }
    internal sealed class CInt64Constant : CConstant<Int64>
    {
        #region ctor
        internal CInt64Constant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override Int64 Parse(string s) => Int64.Parse(s);
        internal override void Increment()
        {
            this.Value = this.Value + 1;
        }
        #endregion
        #region Gui

        #endregion
    }

    internal abstract class CEnumConstant<TEnum> : CConstant<TEnum>
    {
        #region ctor
        internal CEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
        #endregion
        #region Value
        internal override TEnum Parse(string s) => (TEnum)Enum.Parse(typeof(TEnum), s);
        public IEnumerable<TEnum> VmValues => Enum.GetValues(typeof(TEnum)).Cast<TEnum>().OrderBy(e => e.ToString());
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.Enum;
        #endregion

    }

    public sealed class CDataTypeAttribute : Attribute
    {
        public CDataTypeAttribute(Type aConstantType)
        {
            this.DataType = aConstantType;
        }
        internal readonly Type DataType;

        internal static CDataTypeAttribute GetByEnum<TEnum>(TEnum e)
            => typeof(TEnum).GetField(e.ToString()).GetCustomAttributes(typeof(CDataTypeAttribute), false).Cast<CDataTypeAttribute>().Single();
        internal CParameter NewParameter(params object[] aArgs)
            => CParameterClassRegistry.Singleton[this.DataType].New<CParameter>(aArgs);

    }

    internal enum CPixelAlgorithmEnum
    {
        [CDataType(typeof(CClassicMandelbrotSetPixelAlgorithm))]
        MandelbrotClassicSet,

        [CDataType(typeof(CSingleJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaSingle,

        [CDataType(typeof(CMultiJuliaMandelbrotSetPixelAlgorithm))]
        MandelbrotJuliaMulti,
    }
    internal sealed class CPixelAlgorithmEnumConstant : CEnumConstant<CPixelAlgorithmEnum>
    {
        public CPixelAlgorithmEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
    }
    internal sealed class CPixelAlgorithmEnumParameter : CEnumParameter<CPixelAlgorithmEnum>
    {
        public CPixelAlgorithmEnumParameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CPixelAlgorithmEnumConstant(aParentValueNode, aName);
    }

}
