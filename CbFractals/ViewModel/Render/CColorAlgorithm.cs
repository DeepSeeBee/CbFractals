using CbFractals.ViewModel.PropertySystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace CbFractals.ViewModel.Render
{

    enum CColorAlgorithmEnum
    {
        [CType(typeof(CCbColorAlgorithm1))]
        CharlyBeck1,

        [CType(typeof(CHahnebeck1ColorAlgorithm))]
        HahneBeck1
    }

    internal struct CColorAlgorithmInput
    {
        internal  CColorAlgorithmInput(CParameterSnapshot aParameterSnapshot)
        {
            this.ParameterSnapshot = aParameterSnapshot;
        }
        internal readonly CParameterSnapshot ParameterSnapshot;
    }
    internal sealed class CColorAlgorithmEnumConstant : CEnumConstant<CColorAlgorithmEnum>
    {
        public CColorAlgorithmEnumConstant(CValueNode aParentValueSource, CNameEnum aName) : base(aParentValueSource, aName)
        {
        }
    }

    internal sealed class CColorAlgorithmEnumParameter : CEnumParameter<CColorAlgorithmEnum>
    {
        public CColorAlgorithmEnumParameter(CParameters aParentParameters, CParameterEnum aParameterEnum) : base(aParentParameters, aParameterEnum)
        {
        }
        internal override CConstant NewConstant(CValueNode aParentValueNode, CNameEnum aName)
            => new CColorAlgorithmEnumConstant(aParentValueNode, aName);
    }

    internal abstract class CColorAlgorithm
    {
        internal CColorAlgorithm(CColorAlgorithmInput aColorAlgorithmInput)
        {
            this.ColorAlgorithmInput = aColorAlgorithmInput;
        }
        private readonly CColorAlgorithmInput ColorAlgorithmInput;

        /// <summary>
        /// Ermittelt eine Farbe für einen Pixel.
        /// </summary>
        /// <param name="d">Wertebereich 0..1. 0='Tiefste' Farbe, miminale Mandelbrotiterationen, 1='Höchste Farbe', maximale mandelbrotiterationen.</param>
        /// <returns>Farbe</returns>
        internal abstract Color GetColor(double d);

        /// <summary>
        /// ParameterSnapshot contains values of CParameters for render thread.
        /// </summary>
        internal CParameterSnapshot ParameterSnapshot => this.ColorAlgorithmInput.ParameterSnapshot;

    }

    [CType(typeof(CCbColorAlgorithm1))]
    internal sealed class CCbColorAlgorithm1 : CColorAlgorithm
    {
        public CCbColorAlgorithm1 (CColorAlgorithmInput aColorAlgorithmInput) : base(aColorAlgorithmInput)
        {
        }

        internal float ChannelHue1(double c)
        {
            var m1a = c % 1d;
            var m1 = m1a < 0d
                   ? 1d + m1a
                   : m1a
                   ;
            var max1 = 1.0f / 3d * 2d;
            var max2 = max1 / 2d;
            var h = m1 < max2
                  ? m1 / 2f / max2
                  : m1 < max1
                  ? (max2 - (m1 - max2)) / max2
                  : 0f
                  ;
            var hm = (float)(h % 1d);
            return hm;
        }
        internal Color ColorHue1(double c, double ho)
        {
            var max = 1.0f / 3d;
            var o = (-max / 2d) * ho;
            var r = ChannelHue1(c + max * 0d + o);
            var g = ChannelHue1(c + max * 1d + o);
            var b = ChannelHue1(c + max * 2d + o);
            return Color.FromScRgb(1f, r, g, b);
        }

        internal override Color GetColor(double d)
        {
            var aParametersSnapshot = this.ParameterSnapshot;
            var co = aParametersSnapshot.Get<double>(CParameterEnum.ColorOffset); //1.5f
            var cperiod = aParametersSnapshot.Get<double>(CParameterEnum.ColorPeriod); // 1d;
            var aDarkenThresholdLo = aParametersSnapshot.Get<double>(CParameterEnum.DarkenThresholdLo); // 0.1;
            var aDarkenTheesholdHi = aParametersSnapshot.Get<double>(CParameterEnum.DarkenThresholdHi); // 0.3;
            var aDarken = new Func<Color, double, Color>(delegate (Color c, double d)
            { return Color.FromScRgb(1.0f, (float)(c.ScR * d), (float)(c.ScG * d), (float)(c.ScB * d)); });
            var aGetColor = new Func<double, Color>(itf =>
            {
                var c1 = this.ColorHue1((itf + (1f / 3f * co)) * cperiod, 0d);
                var c2 = itf < aDarkenThresholdLo
                       ? aDarken(c1, (itf / aDarkenThresholdLo))
                       : itf > 1d - aDarkenTheesholdHi
                       ? aDarken(c1, 1d - (((itf - (1d - aDarkenTheesholdHi))) / aDarkenTheesholdHi))
                       : c1;
                return c2;
            });
            return aGetColor(d);
        }
    }


    /// <summary>
    /// Vorlageklasse für weitere Farbalgorithmen.
    /// Kann umbenannt werden: Klassennamen markieren und ctrl und 2 mal r drücken.
    /// Dann werden alle referenzen auf die Klasse auch umbennant.
    /// </summary>
    [CType(typeof(CHahnebeck1ColorAlgorithm))]
    internal sealed class CHahnebeck1ColorAlgorithm : CColorAlgorithm
    {
        public CHahnebeck1ColorAlgorithm(CColorAlgorithmInput aColorAlgorithmInput) : base(aColorAlgorithmInput)
        {
        }

        /// <summary>
        /// Diese Funktion ermittelt eine Farbe für einen Pixel.
        /// </summary>
        /// <param name="d">Wertebereich 0..1. 0='Tiefste' Farbe, miminale Mandelbrotiterationen, 1='Höchste Farbe', maximale mandelbrotiterationen.</param>
        /// <returns>Farbe</returns>
        internal override Color GetColor(double d)
        {
            // Zu erledigen:
            // Eine der Algorithmen 
            // von : https://www.fractalus.com/fractint/dmj-pub.htm
            // oder: https://duckduckgo.com/?q=fractal+algorithm+collection&atb=v250-1&ia=web
            // hier einbauen.
            // Andere Algorithmen in jeweils eine eigene Klasse.
            throw new NotImplementedException();
        }
    }
}
