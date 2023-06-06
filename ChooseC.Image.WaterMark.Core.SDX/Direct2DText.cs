using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

using d2 = SharpDX.Direct2D1;
using d3d = SharpDX.Direct3D11;
using dxgi = SharpDX.DXGI;
using wic = SharpDX.WIC;
using dw = SharpDX.DirectWrite;

namespace ChooseC.Image.WaterMark.Core.SDX
{
    /// <summary>
    /// 绘制表情符号和文字
    /// ref:https://gist.github.com/ksasao/b4f5f7bef56e1cacddee4fd10204fec4
    /// </summary>
    public class Direct2DText : IDisposable
    {
        // initialize the D3D device which will allow to render to image any graphics - 3D or 2D
        d3d.Device defaultDevice;
        d3d.Device1 d3dDevice;
        dxgi.Device dxgiDevice;
        Device d2dDevice;
        wic.ImagingFactory2 imagingFactory = new wic.ImagingFactory2(); // initialize the WIC factory

        // initialize the DeviceContext - it will be the D2D render target and will allow all rendering operations
        DeviceContext d2dContext;
        dw.Factory dwFactory;

        // specify a pixel format that is supported by both D2D and WIC
        PixelFormat d2PixelFormat = new d2.PixelFormat(dxgi.Format.R8G8B8A8_UNorm, d2.AlphaMode.Premultiplied);
        // if in D2D was specified an R-G-B-A format - use the same for wic
        Guid wicPixelFormat = wic.PixelFormat.Format32bppPRGBA;
        TextFormat textFormat;
        SolidColorBrush textBrush;
        BitmapProperties1 d2dBitmapProps;

        float dpi = 96;

        public Direct2DText()
        {
            defaultDevice = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware,
                                                          d3d.DeviceCreationFlags.VideoSupport
                                                          | d3d.DeviceCreationFlags.BgraSupport
                                                          | d3d.DeviceCreationFlags.None); // take out the Debug flag for better performance
            d3dDevice = defaultDevice.QueryInterface<d3d.Device1>(); // get a reference to the Direct3D 11.1 device
            dxgiDevice = d3dDevice.QueryInterface<dxgi.Device>(); // get a reference to DXGI device
            d2dDevice = new d2.Device(dxgiDevice); // initialize the D2D device
            imagingFactory = new wic.ImagingFactory2(); // initialize the WIC factory
            d2dContext = new d2.DeviceContext(d2dDevice, d2.DeviceContextOptions.None);
            dwFactory = new dw.Factory();
            d2dBitmapProps = new BitmapProperties1(d2PixelFormat, dpi, dpi, BitmapOptions.Target | BitmapOptions.CannotDraw);
        }

        public void SetFont(string fontName, float fontSize)
        {
            if (textFormat != null)
            {
                textFormat.Dispose();
            }
            textFormat = new TextFormat(dwFactory, fontName, fontSize);
        }

        public void SetColor(System.Drawing.Color color)
        {
            if (textBrush != null)
            {
                textBrush.Dispose();
            }
            textBrush = new SolidColorBrush(d2dContext, new RawColor4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255));
        }
        private int GetTextWidth(string text, int width, int height)
        {
            // measure text width including white spaces
            TextLayout tl0 = new TextLayout(dwFactory, "A", textFormat, width, height);
            TextLayout tl1 = new TextLayout(dwFactory, text + "A", textFormat, width, height);
            int result = (int)(tl1.Metrics.Width - tl0.Metrics.Width);
            tl0.Dispose();
            tl1.Dispose();
            return result > width ? width : result;
        }
        public System.Drawing.Bitmap TextToBitmap(string text, int width, int height)
        {
            int pixelWidth = GetTextWidth(text, width, height);
            int pixelHeight = height;

            var d2dRenderTarget = new Bitmap1(d2dContext, new Size2(pixelWidth, pixelHeight), d2dBitmapProps);
            if (d2dContext.Target != null)
            {
                d2dContext.Target.Dispose();
            }
            d2dContext.Target = d2dRenderTarget; // associate bitmap with the d2d context


            // Draw Text
            TextLayout textLayout = new TextLayout(dwFactory, text, textFormat, pixelWidth, pixelHeight);

            d2dContext.BeginDraw();
            d2dContext.DrawTextLayout(new RawVector2(0, 0), textLayout, textBrush, DrawTextOptions.EnableColorFont);
            d2dContext.EndDraw();

            textLayout.Dispose();


            // Copy to MemoryStream
            var stream = new MemoryStream();
            var encoder = new wic.PngBitmapEncoder(imagingFactory);
            encoder.Initialize(stream);

            var bitmapFrameEncode = new wic.BitmapFrameEncode(encoder);
            bitmapFrameEncode.Initialize();
            bitmapFrameEncode.SetSize(pixelWidth, pixelHeight);
            bitmapFrameEncode.SetPixelFormat(ref wicPixelFormat);

            // this is the trick to write D2D1 bitmap to WIC
            var imageEncoder = new wic.ImageEncoder(imagingFactory, d2dDevice);
            var imageParam = new wic.ImageParameters(d2PixelFormat, dpi, dpi, 0, 0, pixelWidth, pixelHeight);
            imageEncoder.WriteFrame(d2dRenderTarget, bitmapFrameEncode, imageParam);
            //imageEncoder.WriteFrame(d2dRenderTarget, bitmapFrameEncode, ref imageParam);
            bitmapFrameEncode.Commit();
            encoder.Commit();

            imageEncoder.Dispose();
            encoder.Dispose();
            bitmapFrameEncode.Dispose();
            d2dRenderTarget.Dispose();

            // Convert To Bitmap
            byte[] data = stream.ToArray();
            stream.Seek(0, SeekOrigin.Begin);
            var bmp = new System.Drawing.Bitmap(stream);
            stream.Dispose();

            return bmp;
        }
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    d2dContext.Dispose();
                    dwFactory.Dispose();
                    imagingFactory.Dispose();
                    d2dDevice.Dispose();
                    dxgiDevice.Dispose();
                    d3dDevice.Dispose();
                    defaultDevice.Dispose();
                    textBrush.Dispose();
                    textFormat.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
