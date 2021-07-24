﻿using CbFractals.Tools;
using CbFractals.ViewModel.SceneManager; // TODO-abstrahieren?
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CbFractals.ViewModel.PropertySystem
{
    public enum CValueNodeGuiEnum
    {
        Null,
        ParameterRef,
        TextBox,
        ValueSources,
        Enum,
    }

    public abstract class CValueNode : CViewModel
    {
        internal CValueNode(CProgressionManager aParentProgressionManager, CNameEnum aName)
        {
            this.ParentProgressionManager = aParentProgressionManager;
            this.Name = aName;
        }
        internal readonly CProgressionManager ParentProgressionManager;

        #region Name
        internal const CNameEnum Name_Max = CNameEnum.Max;
        internal const CNameEnum Name_Min = CNameEnum.Min;
        internal const CNameEnum Name_Constant = CNameEnum.Constant;
        internal const CNameEnum Name_MappedProgression = CNameEnum.MappedProgression;
        internal readonly CNameEnum Name;
        public string VmName => this.Name.ToString().TryTrimStart("Parameter_"); // TODO-Hack
        #endregion
        #region Value
        //internal abstract void SetTypelessValue(object v);
        internal abstract object GetTypelessValue();
        internal double GetDoubleValue() => Convert.ToDouble(this.GetTypelessValue());
        //internal object TypelessValue { get => this.GetTypelessValue(); set => this.SetTypelessValue(this.ConvertToTypedValue(value)); }
        //internal abstract object ConvertToTypedValue(object o);
        //internal virtual double GetDoubleValue() => Convert.ToDouble(this.TypelessValue);
        internal void OnValueChanged()
        {
            this.ParentProgressionManager.OnChangeValue(this);
        }
        #endregion
        #region Buid
        internal virtual void Build()
        {
        }
        #endregion
        #region ValueSources
        internal virtual IEnumerable<CValueNode> StoredNodes => this.SubValueNodes.Where(n => n.Supported && n.DataEditable);
        internal virtual IEnumerable<CValueNode> ValueSources => Array.Empty<CValueNode>();
        public IEnumerable<CValueNode> VmValueSources => this.ValueSources;
        #endregion
        #region ValueNode
        private CValueNode ValueSourceM;
        internal virtual CValueNode ValueSource
        {
            get => this.ValueSourceM;
            set
            {
                this.ValueSourceM = value;
                this.OnPropertyChanged(nameof(this.VmValueSource));
            }
        }
        public CValueNode VmValueSource
        {
            get => this.ValueSource;
            set => this.ValueSource = value;
        }
        #endregion
        #region ValueNodeGui
        internal virtual CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.Null;
        public CValueNodeGuiEnum VmValueNodeGuiEnum => this.Supported ? this.ValueNodeGuiEnum : CValueNodeGuiEnum.Null;
        #endregion
        internal bool Supported = true;
        internal bool IsVisible => this.Supported;
        public bool VmIsVisible => this.IsVisible;
        #region Storage
        internal virtual bool IsData => false;
        internal virtual string StoredData { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
        #endregion
        internal virtual IEnumerable<CValueNode> SubValueNodes => Array.Empty<CValueNode>();

        internal virtual void SetEditable(bool v)
        {
            this.SetValueSourceEditable(v);
            this.SetDataEditable(v);
            foreach (var aSubValueNode in this.SubValueNodes)
                aSubValueNode.SetEditable(v);
        }
        private bool ValueSourceEditable = true;
        public bool VmValueSourceEditable => this.ValueSourceEditable;
        internal void SetValueSourceEditable(bool b)
        {
            this.ValueSourceEditable = b;
            foreach (var aSubNode in this.SubValueNodes)
                aSubNode.SetValueSourceEditable(b);
        }
        private bool DataEditable = true;
        public bool VmDataEditable => this.DataEditable;
        internal void SetDataEditable(bool b)
        {
            this.DataEditable = b;
            foreach (var aSubValueNode in this.SubValueNodes)
                aSubValueNode.SetDataEditable(b);
        }
        internal void SetSelectable(bool b)
        {
            this.Selectable = b;
        }
        internal bool Selectable = true;
        public bool VmSelectable => this.Selectable;

    }



    internal sealed class CParameterRef : CValueNode
    {
        #region ctor
        internal CParameterRef(CProgression aProgression, CNameEnum aName) : base(aProgression.ParentProgressionManager, aName)
        {
            this.ParentProgression = aProgression;
        }
        #endregion
        #region Progression
        internal readonly CProgression ParentProgression;
        #endregion
        #region Parameters
        internal IEnumerable<CParameter> Parameters => this.ParentProgressionManager.Parameters.Parameters;
        public IEnumerable<CParameter> VmParameters => this.Parameters;
        #endregion
        #region Parameter
        private CParameter ParameterM;
        internal CParameter Parameter
        {
            get => CLazyLoad.Get(ref this.ParameterM, () => this.ParentProgressionManager.Parameters[CParameterEnum.DoubleZero]);
            set
            {
                this.ParameterM = value;
                this.OnPropertyChanged(nameof(this.VmParameter));
            }
        }
        public CParameter VmParameter
        {
            get => this.Parameter;
            set => this.Parameter = value;
        }
        #endregion
        internal override CValueNodeGuiEnum ValueNodeGuiEnum => CValueNodeGuiEnum.ParameterRef;
        internal override object GetTypelessValue() => this.Parameter.GetTypelessValue();
    }
}
