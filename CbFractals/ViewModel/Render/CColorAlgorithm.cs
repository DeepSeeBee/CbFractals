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
        public CColorAlgorithmEnumParameter(CParameters aParentParameters, CNameEnum aNameEnum) : base(aParentParameters, aNameEnum)
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


        private bool DebugOutputDone;

        // Diese Variable auf false setzen, um die funktion laufen zu lassen.
        private bool ThrowNotImplemented = true;

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

            // Hier mal ein ganz einfaches beispiel. Es generiert nur einen grünton in abhingigkeit des parameters d.
            var aRed = 0; // Variable deklarieren [01] und wert zuweisen: 0 = Kein Rot anteil
            var aGreen = 255d * d; // Da die variable 'd' von 0...1 geht und 255 maximaler Grünanteil ist, gibt diese multiplikation den maximalen grünanteil.
            var aBlue = 0; // Kein Blauanteil

            // Wir erzeugen eine Farbe, die aus rot grün und blau anteil besteht.
            // Dazu rufen wir die (Statische [02]) Methode der Klasse [03] 'Color' auf.
            // Diese Funktion gibt ein Objekt [04] vom Typ 'Color' zurück.
            var aColor = Color.FromRgb((byte)aRed, (byte)aGreen, (byte)aBlue);


            // Damit ist die funktion im wesentlichen abgearbeitet.
            // eigentlich könntne wir jetzt 'return aColor' schreiben,
            // um die farbe zurückzugeben.
            //
            // Was jetzt kommt ist ein bisschen was für demonstrationszwecke.

            // EINMAL pro durchlauf geben wir eine Debug-Nachricht aus:
            if (this.DebugOutputDone) // If-Bedingung [05]
            {
                // Für diesen Durchlauf nichts mehr ausgeben, sonst dauert das render ewig.
            }
            else if (d > 0.5) // Ausgabe nur, wenn ein gewisser grünanteil vorhanden ist.
            {



                // Wir geben eine Debug ausgabe aus: [06]
                // Debuggausgabe ist nur verfügbar, wenn das programm gestartet ist
                // Und der Debugger [07] angefügt ist (Immer gegeben, wenn programm im Visual Studio gestartet wird)
                // Wir bauen einen String (Text) [08] zusammen, indem wir einzelne Strings anfügen.
                // Dazu benutzen wir ein Objekt 'StringBuilder'.[09]
                var aStringBuilder = new StringBuilder();            // Objekt erstellen [10] und in der Variable 'StringBuilder' ablegen.
                                                                     // 'new' Allokiert [11] immer einen kleinen happen speicher, das von dem Objekt verwendet wird um seine aufgabe zu erledigen.
                                                                     // Die Aufgabe des Objekts 'StringBuilder' ist, einen Text (String) zusammenzubauen.
                aStringBuilder.Append("Farbanteil grün: ");          // Wir fügen den text an.
                aStringBuilder.Append(aColor.G.ToString());          // Das Byte [12] 'G' konveritern wir in einen string und fügen es an.
                aStringBuilder.Append(" (d=");                       // Wir fügen noch den Eingabeparameter d an.
                aStringBuilder.Append(d.ToString());                 // Wir fügen den Wert des parameters d an.
                aStringBuilder.Append(")");                          // Wir fügen eine schliessende Klammer an.
                var aDebugNachricht1 = aStringBuilder.ToString();    // Der StringBuilder baut den string zusammen,
                                                                        // wobei er eine optimierte Speicherallokierung implementiert,
                                                                        // die vermutlich effizienter ist, als die einzelnen string anteile.
                var aBeispiel = " a " + " + " + " b";                // So kann man einen text auch zusammenbauen. StringBuilder ist vor allem für längere texte interesannt.
                var aDebugNachricht2 = aDebugNachricht1 + aBeispiel; // Es empfielt sich immer (soweit möglich) eine neue variable zu verwenden. Ist klarer von der programmierung her,
                                                                        // da man sonst immer wissen muss, ob eine Variable nochmal überschriebne wird.
                var aDebguNachricht = aDebugNachricht2;              // Der finale Wert für die Debug Nachricht.

                // So geben wir die dEbugnachricht im ausgabefenster aus: [13]
                // Erscheint im Fenster 'Ausgabe.Debuggen' unter VisualStudio->Ansicht->Ausgabe.
                // (Zeilennumbruch nicht vergessen!) ;)
                System.Diagnostics.Debug.Print(aDebguNachricht + Environment.NewLine);

                // Wir merken uns, dass wir die Debug ausgabe gemacht haben. sonst kommt pro pixel eine ausgabe 
                // und das rendern dauert ewig. [14]
                this.DebugOutputDone = true;
            }
            { // Demonstration: Verwenden einer 'NotImplementedException' um Fehlerbehandlung zu aktivieren, wenn eine funktion nicht (fertig) programmiert ist.
                // Variable auf 'False' setzen, um die funktion laufen zu lassen:
                if (this.ThrowNotImplemented)
                {
                    // Wenn die Variable 'true' ist:
                    // Wir werfen eine Ausnahme, also einen fehler, der besagt, dass die funktion (nicht fertig) programmiert (implementiert) ist.
                    // Fehler kann man behandeln, wenn man das nicht tut, stürtzt das programm ab. [15]                    
                    throw new NotImplementedException();
                }
                else
                {
                    // Wenn die Variable 'False' ist:
                    // Farbe zurückgeben
                    return aColor;
                }
            }

            // Hier was zu lesen:
            //
            // [01] Variable deklarieren      : https://de.wikibooks.org/wiki/Arbeiten_mit_.NET:_C-Sharp/_Grundlagen/_Bestandteile/_Variable
            // [02] Statische Methoden        : http://www.gailer-net.de/tutorials/java/Notes/chap25/ch25_13.html
            // [03] Klassen (Allgemein)       : https://de.wikipedia.org/wiki/Klasse_(Objektorientierung)
            // [03] Klasse* 'Color'           : https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.color?view=net-5.0
            // [04] Unterschie Klasse<>Objekt : https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.color?view=net-5.0
            // [05] If-Bedingung              : https://docs.microsoft.com/de-de/dotnet/csharp/language-reference/keywords/if-else
            // [06] DebugAusgabe              : https://www.fambach.net/debug-ausgaben-im-quellcode/
            // [07] Debugger                  : https://de.wikipedia.org/wiki/Debugger
            // [08] String                    : https://www.das-grosse-computer-abc.de/CSharp/Grundlagen/Zeichenketten
            // [09] Stringbuilder             : https://docs.microsoft.com/en-us/dotnet/api/system.text.stringbuilder?view=net-5.0  
            // [10] New Operator              : https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/new-operator
            // [11] Allokieren                : https://de.wikipedia.org/wiki/Allokation_(Informatik)
            // [12] ByteDatentyp              : https://docs.microsoft.com/en-us/dotnet/api/system.byte?view=net-5.0
            // [13] Klasse 'Debug'            : https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.debug?view=net-5.0
            // [14] MemberVariablen           : https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/members
            // [14] Scopes                    : https://www.geeksforgeeks.org/scope-of-variables-in-c-sharp/
            // [15] Try/Catch Blöcke          : https://docs.microsoft.com/en-us/dotnet/standard/exceptions/how-to-use-the-try-catch-block-to-catch-exceptions

            // *: Struct ist ganz ähnlich wie eine Klasse in c#. Unterschied ist im wesentlichen die art der speicherverwaltung und sich daraus ergebnde konsequenzen.

        }
    }
}
