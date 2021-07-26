using CbFractals.Gui.Wpf;
using CbFractals.Tools;
using CbFractals.ViewModel.Mandelbrot;
using CbFractals.ViewModel.PropertySystem;
using CbFractals.ViewModel.Render;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;

namespace CbFractals.ViewModel.MandelModel
{
    using CVec2Int = Tuple<int, int>;
    public class CFullMandelModel
    {
        internal static void CalcMemoryCosts(int aMaxIterations, TimeSpan aTimeSpan, int aFps, double aMinZoom, double aMaxZoom, int aDx, int aDy)
        {
            var aGetBitCount = new Func<int, int>(m =>
            {
                var r = m;
                var b = 0;
                while (r > 0)
                {
                    r -= (1 << b);
                    ++b;
                }
                return b;
            });
            var aBitCount = aGetBitCount(aMaxIterations);
            var aFrames = (int)aFps * aTimeSpan.TotalSeconds;
            var aZoomRange = aMaxZoom / aMinZoom;
            var aPixels1 = aDx * aDy;
            var aPixels2 = aPixels1 * aZoomRange;
            var aBits = aPixels2 * aBitCount;
            var aTb = aBits / 8d / 1024d / 1024d / 1024d;
            System.Diagnostics.Debug.Write("Model needs " + Math.Round(aTb, 3).ToString() + " GB memory" + Environment.NewLine);
        }

        internal static void CalcMemoryCosts()
        {
            var a9Bits = 511;
            var a10Bit = 1023;
            var a11Bit = 2047;
            CalcMemoryCosts(a11Bit, TimeSpan.FromMinutes(1), 40, 1, 5000, 1920, 1080);
        }



    }

    internal abstract class CModelIo
    {
        internal CModelIo(Stream aStream)
        {
            this.Stream = aStream;
        }
        internal readonly Guid FormatId = new Guid("fab2b2bb-08d4-449e-bb69-e230d9c3a700");
        internal readonly Stream Stream;
    }

    internal sealed class CModelWriter : CModelIo
    {
        #region ctor
        internal CModelWriter(CVec2Int aSizePxl, Stream aStream) : base(aStream)
        {
            this.SizePxl = aSizePxl;
            this.StreamWriter = new BinaryWriter(aStream);
            this.ColorFktSink = new CActionSink<double>(this.WriteColorFkt);
        }
        internal void Dispose()
        {
            this.Close();
            this.Stream.Dispose();
        }
        #endregion
        internal readonly BinaryWriter StreamWriter;
        internal bool HeaderWritten;
        internal readonly CSink<double> ColorFktSink;
        internal readonly byte BitCount = 16;
        internal readonly CVec2Int SizePxl;
        private void WriteHeader()
        {
            if (this.HeaderWritten)
            {
                throw new InvalidOperationException();
            }
            else
            {
                this.Stream.Write(this.FormatId.ToByteArray(), 0, 16);
                this.StreamWriter.Write((Int32)this.SizePxl.Item1);
                this.StreamWriter.Write((Int32)this.SizePxl.Item2);
                this.StreamWriter.Write((byte)this.BitCount);
                this.HeaderWritten = true;
            }
        }
        internal void Flush()
        {
            if(!this.HeaderWritten)
            {
                this.WriteHeader();
            }
        }
        
        internal void WriteColorFkt(double aColorFkt)
        {
            if (!this.HeaderWritten)
            {
                this.WriteHeader();
            }
            var aUInt16 = Convert.ToUInt16(aColorFkt * UInt16.MaxValue);
            this.StreamWriter.Write(aUInt16);
        }

        private void Close()
        {
            this.Stream.SetLength(this.Stream.Position);
            this.Stream.Close();
        }
    }
 
    internal sealed class CModelReader : CModelIo
    {
        #region ctor
        internal CModelReader(Stream aStream) : base(aStream)
        {
            this.BinaryReader = new BinaryReader(aStream);
        }
        internal void Dispose()
        {
            this.Stream.Dispose();
        }
        #endregion
        private readonly BinaryReader BinaryReader;
        internal byte? BitCount;
        internal CVec2Int SizePxl;

        internal void ReadHeader()
        {
            var aFormatIdBytes = new byte[16];
            this.Stream.Read(aFormatIdBytes, 0, aFormatIdBytes.Length);
            var aFormatId = new Guid(aFormatIdBytes);
            if (aFormatId != this.FormatId)
                throw new FormatException();
            var aSizePxlX = this.BinaryReader.ReadInt32();
            var aSizePxlY = this.BinaryReader.ReadInt32();
            var aBitCount = this.BinaryReader.ReadByte();
            if (aBitCount != 16)
                throw new FormatException();
            this.SizePxl = new CVec2Int(aSizePxlX, aSizePxlY);
            this.BitCount = aBitCount;
        }
        internal double ReadColorFkt()
        {
            var aU16 = this.BinaryReader.ReadUInt16();
            var aDbl = (double)aU16 / (double)UInt16.MaxValue;
            return aDbl;
        }

        internal void CheckSizePxl(CVec2Int aSizePxl)
        {
            if(aSizePxl.Item1 != this.SizePxl.Item1
            || aSizePxl.Item2 != this.SizePxl.Item2)
            {
                throw new FormatException("Expected ImageSize.X=" + this.SizePxl.Item1 + " && ImageSize.Y=" + this.SizePxl.Item2 + ".");
            }
        }
    }

    public enum CModelRenderModeEnum
    {
        [CType(typeof(CReadStrategy))]
        Ignore,

        [CType(typeof(CWriteStrategy))]
        Write,

        [CType(typeof(CWriteStrategy))]
        Read,
    }

    internal sealed class CStrategyInputArgs
    {
        #region ctor
        internal CStrategyInputArgs(CParameterSnapshot aParameterSnapshot, 
                                    Func<CPixelAlgorithmInput> aNewPixelAlgorithmInput,
                                    CVec2Int aSizePxl)
        {
            this.ParametersSnapshot = aParameterSnapshot;
            this.NewPixelAlgorithmInput = aNewPixelAlgorithmInput;
            this.SizePxl = aSizePxl;
        }
        #endregion
        #region ParameterSnapshot
        internal readonly CParameterSnapshot ParametersSnapshot;
        #endregion
        #region PixelAlgorithmInput
        internal readonly Func<CPixelAlgorithmInput> NewPixelAlgorithmInput;
        #endregion
        #region SizePxl
        internal readonly CVec2Int SizePxl;
        #endregion
    }

    internal abstract class CStrategy
    {
        #region ctor
        internal CStrategy (CStrategyInputArgs aStrategyInputArgs)
        {
            this.StrategyInputArgs = aStrategyInputArgs;
        }
        internal virtual void Dispose()
        {
        }
        internal static CStrategy New(CStrategyInputArgs aArgs)
        {
            var aParametersSnapshot = aArgs.ParametersSnapshot;
            var aStrategyEnum = aParametersSnapshot.Get<CModelRenderModeEnum>(CParameterEnum.ModelRenderMode);
            var aStrategy = CTypeAttribute.GetByEnum(aStrategyEnum).DataType.New<CStrategy>(aArgs);
            return aStrategy;
        }
        #endregion
        #region StrategyInputArgs
        private readonly CStrategyInputArgs StrategyInputArgs;
        #endregion
        #region ColorSinkFkt
        internal virtual CSink<double> NewColorFktSink()
            => new CNullSink<double>();
        private CSink<double> ColorFktSinkM;
        internal virtual CSink<double> ColorFktSink => CLazyLoad.Get(ref this.ColorFktSinkM, () => this.NewColorFktSink());
        #endregion
        #region GetPixelFkt
        internal virtual bool GetPixelFktIsDefined => false;
        internal virtual double GetPixelFkt(CVec2Int aPixelPos) { throw new InvalidOperationException(); }
        internal CPixelAlgorithm<double> NewPixelAlgorithmFunc()
        {
            var aParametersSnapshot = this.StrategyInputArgs.ParametersSnapshot;
            var aPixelAlgoEnum = aParametersSnapshot.Get<CPixelAlgorithmEnum>(CParameterEnum.PixelAlgorithm1);
            var aPixelAlgorithmInput = this.StrategyInputArgs.NewPixelAlgorithmInput();
            var aPixelAlgorithm = CTypeAttribute.GetByEnum(aPixelAlgoEnum).DataType.New<CMandelbrotPixelAlgorithm>(aPixelAlgorithmInput);
            return aPixelAlgorithm;
        }
        #endregion
        //#region PixelAlgorithm
        //internal CPixelAlgorithm<double> NewPixelAlgorithm()
        //{
        //    var aParametersSnapshot = this.StrategyInputArgs.ParametersSnapshot;
        //    var aPixelAlgoEnum = aParametersSnapshot.Get<CPixelAlgorithmEnum>(CParameterEnum.PixelAlgorithm1);
        //    var aPixelAlgorithmInput = this.StrategyInputArgs.NewPixelAlgorithmInput();
        //    var aPixelAlgorithm = CTypeAttribute.GetByEnum(aPixelAlgoEnum).DataType.New<CMandelbrotPixelAlgorithm>(aPixelAlgorithmInput);
        //    return aPixelAlgorithm;
        //}
        //#endregion
        #region Stream
        private FileInfo FileInfoM;
        private FileInfo FileInfo => CLazyLoad.Get(ref this.FileInfoM, () => new FileInfo(Path.Combine(new FileInfo(this.GetType().Assembly.Location).Directory.FullName, "mandelmodel", "000.CbFractal.MandelModel.bin")));
        internal CModelWriter NewModelWriter()
        {
            var aFileInfo = this.FileInfo;
            var aStream = File.OpenWrite(aFileInfo.FullName);
            var aSizePxl = this.StrategyInputArgs.SizePxl;
            var aModelWriter = new CModelWriter(aSizePxl, aStream);
            return aModelWriter;
        }
        internal CModelReader NewModelReader()
        {
            var aFileInfo = this.FileInfo;
            var aStream = File.OpenRead(aFileInfo.FullName);
            var aSizePxl = this.StrategyInputArgs.SizePxl;
            var aModelReader = new CModelReader(aStream);
            try
            {
                aModelReader.ReadHeader();
                aModelReader.CheckSizePxl(aSizePxl);
                return aModelReader;
            }
            catch(Exception)
            {
                aModelReader.Dispose();
                throw;
            }            
        }
        #endregion
        #region ColorAlgorithm
        internal CColorAlgorithm NewColorAlgorithm()
        {
            var aParametersSnapshot = this.StrategyInputArgs.ParametersSnapshot;
            var aColorAlgorithmInput = new CColorAlgorithmInput(aParametersSnapshot);
            var aColorAlgorithmEnum = aParametersSnapshot.Get<CColorAlgorithmEnum>(CParameterEnum.ColorAlgorithm);
            var aColorAlgorithm = CTypeAttribute.GetByEnum(aColorAlgorithmEnum).DataType.New<CColorAlgorithm>(aColorAlgorithmInput);
            return aColorAlgorithm;
        }
        #endregion
        #region MaxThreadCount
        internal virtual bool ForceSingleThread => false;
        #endregion
    }

    internal sealed class CWriteStrategy : CStrategy
    {
        #region ctor
        public CWriteStrategy(CStrategyInputArgs aStrategyInputArgs) : base(aStrategyInputArgs)
        {
           // this.PixelAlgorithm = this.NewPixelAlgorithm();
            this.ModelWriter = this.NewModelWriter();
        }      
        internal override void Dispose()
        {
            base.Dispose();
            this.ModelWriter.Dispose();
        }
        #endregion
        //#region PixelAlgorithm
        //private readonly CPixelAlgorithm<double> PixelAlgorithm;
        //internal override double GetPixelFkt(CVec2Int aPixelPos)
        //    => this.PixelAlgorithm.GetPixelFkt(aPixelPos.Item1, aPixelPos.Item2);
        //#endregion
        #region ModelWriter
        private readonly CModelWriter ModelWriter;
        internal override CSink<double> ColorFktSink => this.ModelWriter.ColorFktSink;
        #endregion
    }

    internal sealed class CReadStrategy : CStrategy
    {
        public CReadStrategy(CStrategyInputArgs aStrategyInputArgs) : base(aStrategyInputArgs)
        {
            this.ColorAlgorithm = this.NewColorAlgorithm();
            this.ModelReader = this.NewModelReader();
        }

        private readonly CModelReader ModelReader;
        private readonly CColorAlgorithm ColorAlgorithm;

        private readonly CSink<double> ColorFktSinkM;
        internal override CSink<double> ColorFktSink => this.ColorFktSinkM;

        internal override bool GetPixelFktIsDefined => true;
        internal override double GetPixelFkt(CVec2Int aPixelPos)
            => this.ModelReader.ReadColorFkt(); // TODO-HACK

        internal override bool ForceSingleThread => true; // TODO-HACK
    }

    internal sealed class CIgnoreStrategy :CStrategy
    {
        public CIgnoreStrategy(CStrategyInputArgs aStrategyInputArgs) : base(aStrategyInputArgs)
        {
            //this.PixelAlgorithm = this.NewPixelAlgorithm();
        }

        //private readonly CPixelAlgorithm<double> PixelAlgorithm;

    }


}
