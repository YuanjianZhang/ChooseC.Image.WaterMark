using ChooseC.Image.WaterMark.Core.Enum;
using ChooseC.Image.WaterMark.Core.Model;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ChooseC.Image.WaterMark.Core
{
    public class WaterMarkHelper
    {
        public static readonly string SourceFolder = Path.Combine(AppContext.BaseDirectory, "Source");
        public static readonly string FontsFolder = Path.Combine(SourceFolder, "Fonts");
        public static readonly string LogosFolder = Path.Combine(SourceFolder, "Logos");

        /// <summary>
        /// 创建水印图片在底部，布局为上下
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_TopBottom(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            try
            {
                var config = settings.topbottom;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(stream, fields.Select(p => p.Name).ToArray());
                    infoDict.Append(new KeyValuePair<string, string>("sign", settings.sign));
                    //string format
                    var strFormatter = GetStringFormat(direction);

                    System.Drawing.Image topBitmap = null;
                    System.Drawing.Image bottomBitmap = null;
                    #region get top logo
                    var strCamermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.toplogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, strCamermake)?.FullName;
                            if (logofile is not null) topBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            var logoFont = GetFont(config.wordlogofont.fontname,
                                config.wordlogofont.fontstyle,
                                config.wordlogofont.fontsize,
                                config.wordlogofont.fontunit);

                            topBitmap = WaterMarkHelper.CreateWordImage(
                                strCamermake,
                                strFormatter,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                                Size.Empty,
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region origin Bitmap 
                    var originBitmap = FillImage(
                        stream,
                        GetColor(config.fillcolor),
                        settings.exportsize,
                        0);
                    #endregion

                    #region get bottom info
                    //font
                    var font = GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize,
                        config.infofont.fontunit);
                    //info
                    var bottominfo = FormatterInfo(config.bottominfo, infoDict);

                    bottomBitmap = WaterMarkHelper.CreateWordImage(
                        bottominfo,
                        strFormatter,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                        new Size(originBitmap.Width, 0),
                        10,
                        10,
                        false);
                    #endregion

                    #region 计算绘制的图片大小

                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight <= 1 ?
                            (int)Math.Round(originBitmap.Height * config.logmaxheight, 0)
                            :
                            (int)config.logmaxheight;
                        if (topBitmap != null && topBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)topBitmap.Width / topBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            topBitmap = new Bitmap(topBitmap, resize);
                        }
                    }
                    var totalHeight = originBitmap.Height
                        + (topBitmap == null ? 0 : topBitmap.Height)
                        + (bottomBitmap == null ? 0 : bottomBitmap.Height)
                        + settings.bottommargin;
                    var totalWidth = Math.Max(
                        Math.Max(originBitmap.Width, topBitmap == null ? 0 : topBitmap.Height),
                        bottomBitmap == null ? 0 : bottomBitmap.Height);
                    Bitmap exportBitmap = new Bitmap(totalWidth, totalHeight);
                    #endregion

                    #region 绘制
                    Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 
                                              //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置白色
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion
                    var x = (totalWidth - originBitmap.Width) / 2;
                    var y = 0;
                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width, originBitmap.Height);
                    if (topBitmap is not null)
                    {
                        x = (originBitmap.Width - topBitmap.Width) / 2;
                        y = originBitmap.Height
                            + settings.bottommargin;
                        graphics.DrawImage(topBitmap, x, y, topBitmap.Width, topBitmap.Height);
                    }
                    if (bottomBitmap is not null)
                    {
                        x = (originBitmap.Width - bottomBitmap.Width) / 2;
                        y = originBitmap.Height
                            + (topBitmap == null ? 0 : topBitmap.Height)
                            + settings.bottommargin;
                        graphics.DrawImage(bottomBitmap, x, y, bottomBitmap.Width, bottomBitmap.Height);
                    }

                    graphics.Flush();
                    graphics.Dispose();

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );

                    exportBitmap.Dispose();
                    topBitmap.Dispose();
                    bottomBitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>
        /// 创建水印图片在底部，布局为左右
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_Bottom(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            try
            {
                var config = settings.bottom;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(stream, fields.Select(p => p.Name).ToArray());
                    infoDict.Append(new KeyValuePair<string, string>("sign", settings.sign));
                    //string format
                    var sf = GetStringFormat(direction);

                    System.Drawing.Image leftBitmap = null,
                        leftinfoBitmap = null,
                        rightBitmap = null,
                        rightinfoBitmap = null;

                    var logoFont = GetFont(
                        config.wordlogofont.fontname,
                        config.wordlogofont.fontstyle,
                        config.wordlogofont.fontsize,
                        config.wordlogofont.fontunit);

                    #region get left logo
                    var camermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.leftlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, camermake)?.FullName;
                            if (logofile is not null) leftBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            leftBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                                Size.Empty,
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region get right logo
                    var lensmake = infoDict[nameof(InfoTag.lensmake)];
                    switch (config.rightlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, lensmake)?.FullName;
                            if (logofile is not null) rightBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            rightBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.infofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                                Size.Empty,
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region    get left  info || get right info
                    //font
                    var font = GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize,
                        config.infofont.fontunit);
                    //info
                    var leftinfo = FormatterInfo(config.leftinfo, infoDict);
                    var rightinfo = FormatterInfo(config.rightinfo, infoDict);


                    leftinfoBitmap = WaterMarkHelper.CreateWordImage(
                        leftinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                        new Size(0, config.bottommaxsize.Height),
                        10,
                        10,
                        leftBitmap != null);

                    rightinfoBitmap = WaterMarkHelper.CreateWordImage(
                        rightinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                        new Size(0, config.bottommaxsize.Height),
                        10,
                        10,
                        rightBitmap != null);
                    #endregion

                    #region origin bitmap 
                    var originBitmap = Bitmap.FromStream(stream);
                    #endregion

                    #region 缩放logo 

                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight <= 1 ?
                            (int)Math.Round(originBitmap.Height * config.logmaxheight, 0)
                            :
                            (int)config.logmaxheight;
                        maxheight = maxheight > config.bottommaxsize.Height ? config.bottommaxsize.Height : maxheight;

                        if (leftBitmap != null && leftBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)leftBitmap.Width / leftBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            leftBitmap = new Bitmap(leftBitmap, resize);
                        }

                        if (rightBitmap != null && rightBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)rightBitmap.Width / rightBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            rightBitmap = new Bitmap(rightBitmap, resize);
                        }
                    }
                    #endregion

                    var maxInfoHeigt = Math.Max(
                            Math.Max(leftBitmap == null ? 0 : leftBitmap.Height, leftinfoBitmap == null ? 0 : leftinfoBitmap.Height),
                            Math.Max(rightBitmap == null ? 0 : rightBitmap.Height, rightinfoBitmap == null ? 0 : rightinfoBitmap.Height)
                            );
                    var totalHeight = originBitmap.Height + maxInfoHeigt;
                    var totalWidth = originBitmap.Width;
                    Bitmap exportBitmap = new Bitmap(totalWidth, totalHeight);

                    #region 绘制
                    Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 

                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    //背景设置白色
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion
                    var fillratio = config.fillratio;
                    var x = (int)Math.Round(originBitmap.Width * fillratio / 2, 0);
                    var y = (int)Math.Round(originBitmap.Height * fillratio / 2, 0);

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width - (x * 2), originBitmap.Height - (y * 2));
                    if (leftBitmap is not null)
                    {
                        x = 0;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftBitmap, x, y, leftBitmap.Width, leftBitmap.Height);
                    }
                    if (leftinfoBitmap is not null)
                    {
                        x = leftBitmap == null ? 0 : leftBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftinfoBitmap, x, y, leftinfoBitmap.Width, leftinfoBitmap.Height);
                    }
                    if (rightBitmap is not null)
                    {
                        x = originBitmap.Width - rightBitmap.Width - (rightinfoBitmap == null ? 0 : rightinfoBitmap.Width);
                        y = originBitmap.Height;
                        graphics.DrawImage(rightBitmap, x, y, rightBitmap.Width, rightBitmap.Height);
                    }
                    if (rightinfoBitmap is not null)
                    {
                        x = originBitmap.Width - rightinfoBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(rightinfoBitmap, x, y, rightinfoBitmap.Width, rightinfoBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );

                    exportBitmap.Dispose();
                    leftBitmap.Dispose();
                    leftinfoBitmap.Dispose();
                    rightBitmap.Dispose();
                    rightinfoBitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// 创建水印图片在底部，布局为包围
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_Surround(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder)
        {
            try
            {
                var config = settings.surround;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //exif info data
                    var fields = typeof(InfoTag).GetFields(BindingFlags.Public | BindingFlags.Static);
                    var infoDict = ImageMetaDataHelper.GetMetaData(stream, fields.Select(p => p.Name).ToArray());
                    infoDict.Append(new KeyValuePair<string, string>("sign", settings.sign));
                    //string format
                    var sf = GetStringFormat(direction);

                    System.Drawing.Image leftBitmap = null,
                        leftinfoBitmap = null,
                        rightBitmap = null,
                        rightinfoBitmap = null;

                    var logoFont = GetFont(
                        config.wordlogofont.fontname,
                        config.wordlogofont.fontstyle,
                        config.wordlogofont.fontsize,
                        config.wordlogofont.fontunit
                        );

                    #region get left logo
                    var camermake = infoDict[nameof(InfoTag.camermake)];
                    switch (config.leftlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, camermake)?.FullName;
                            if (logofile is not null) leftBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            leftBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                                Size.Empty,
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region get right logo
                    var lensmake = infoDict[nameof(InfoTag.lensmake)];
                    switch (config.rightlogo)
                    {
                        case nameof(LogoTypeEnum.imglogo):
                            var logofile = GetLogos(LogosFolder, lensmake)?.FullName;
                            if (logofile is not null) rightBitmap = Bitmap.FromFile(logofile);
                            break;
                        case nameof(LogoTypeEnum.wordlogo):
                            rightBitmap = WaterMarkHelper.CreateWordImage(
                                camermake,
                                sf,
                                logoFont,
                                GetColor(config.fillcolor),
                                GetColor(config.wordlogofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                                Size.Empty,
                                10,
                                10,
                                false);
                            break;
                        case nameof(LogoTypeEnum.none):
                            break;
                        default:
                            break;
                    }
                    #endregion

                    #region get left  info || get right info

                    //font
                    var font = GetFont(
                        config.infofont.fontname,
                        config.infofont.fontstyle,
                        config.infofont.fontsize,
                        config.infofont.fontunit
                        );
                    //info
                    var leftinfo = FormatterInfo(
                        config.leftinfo,
                        infoDict
                        );
                    var rightinfo = FormatterInfo(
                        config.rightinfo,
                        infoDict
                        );
                    //get left  info
                    leftinfoBitmap = WaterMarkHelper.CreateWordImage(
                        leftinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                        new Size(0, config.bottommaxsize.Height),
                        10,
                        10,
                        leftBitmap != null);
                    //get right info
                    rightinfoBitmap = WaterMarkHelper.CreateWordImage(
                        rightinfo,
                        sf,
                        font,
                        GetColor(config.fillcolor),
                        GetColor(config.infofont.fontcolor),
                                GetColor(settings.splitlinecolor),
                        new Size(0, config.bottommaxsize.Height),
                        10,
                        10,
                        rightBitmap != null);

                    #endregion

                    #region origin bitmap 
                    var originBitmap = Bitmap.FromStream(stream);
                    #endregion

                    #region 缩放logo 

                    if (config.logmaxheight > 0)
                    {
                        var maxheight = config.logmaxheight <= 1 ?
                            (int)Math.Round(originBitmap.Height * config.logmaxheight, 0)
                            :
                            (int)config.logmaxheight;
                        maxheight = maxheight > config.bottommaxsize.Height ? config.bottommaxsize.Height : maxheight;

                        if (leftBitmap != null && leftBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)leftBitmap.Width / leftBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            leftBitmap = new Bitmap(leftBitmap, resize);
                        }

                        if (rightBitmap != null && rightBitmap.Height > maxheight)
                        {
                            var logoRatio = (double)rightBitmap.Width / rightBitmap.Height;
                            var resize = new Size(
                                (int)(Math.Round(logoRatio * maxheight, 0)),
                                maxheight);
                            rightBitmap = new Bitmap(rightBitmap, resize);
                        }
                    }
                    #endregion
                    int maxBottomHeight = 0;

                    var maxInfoHeigt = Math.Max(
                            Math.Max(leftBitmap == null ? 0 : leftBitmap.Height, leftinfoBitmap == null ? 0 : leftinfoBitmap.Height),
                            Math.Max(rightBitmap == null ? 0 : rightBitmap.Height, rightinfoBitmap == null ? 0 : rightinfoBitmap.Height)
                            );
                    var totalHeight = originBitmap.Height
                        + maxInfoHeigt
                        + maxBottomHeight;
                    var totalWidth = originBitmap.Width;
                    Bitmap exportBitmap = new Bitmap(totalWidth, totalHeight);

                    #region 绘制
                    Graphics graphics = Graphics.FromImage(exportBitmap);
                    //图像字体质量
                    graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                    graphics.TextContrast = 4;//高对比度,default : 4 

                    //图像质量
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    //背景设置白色
                    graphics.Clear(GetColor(config.fillcolor));
                    #endregion

                    int x = 0, y = 0;
                    if (config.fillratio > 0 && config.fillratio <= 1)
                    {
                        x = (int)Math.Round(originBitmap.Width * config.fillratio / 2, 0);
                        y = (int)Math.Round(originBitmap.Height * config.fillratio / 2, 0);
                    }
                    else
                    {
                        x = (int)config.fillratio;
                        y = (int)config.fillratio;
                    }

                    graphics.DrawImage(originBitmap, x, y, originBitmap.Width, originBitmap.Height);
                    if (leftBitmap is not null)
                    {
                        x = 0;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftBitmap, x, y, leftBitmap.Width, leftBitmap.Height);
                    }
                    if (leftinfoBitmap is not null)
                    {
                        x = leftBitmap == null ? 0 : leftBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(leftinfoBitmap, x, y, leftinfoBitmap.Width, leftinfoBitmap.Height);
                    }
                    if (rightBitmap is not null)
                    {
                        x = originBitmap.Width - rightBitmap.Width - (rightinfoBitmap == null ? 0 : rightinfoBitmap.Width);
                        y = originBitmap.Height;
                        graphics.DrawImage(rightBitmap, x, y, rightBitmap.Width, rightBitmap.Height);
                    }
                    if (rightinfoBitmap is not null)
                    {
                        x = originBitmap.Width - rightinfoBitmap.Width;
                        y = originBitmap.Height;
                        graphics.DrawImage(rightinfoBitmap, x, y, rightinfoBitmap.Width, rightinfoBitmap.Height);
                    }
                    graphics.Flush();
                    graphics.Dispose();

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        exportBitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );

                    exportBitmap.Dispose();
                    leftBitmap.Dispose();
                    leftinfoBitmap.Dispose();
                    rightBitmap.Dispose();
                    rightinfoBitmap.Dispose();
                }
            }
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// 只填充图片
        /// </summary>
        /// <param name="fileinfo"></param>
        /// <param name="settings"></param>
        /// <param name="direction"></param>
        /// <param name="exportfolder"></param>
        public static void CreateWaterMark_OnlyFill(FileInfo fileinfo, Settings settings, DrawDirectionEnum direction, DirectoryInfo exportfolder, Size exportSize)
        {
            try
            {
                var config = settings.onlyfill;
                using (FileStream stream = fileinfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var bitmap = WaterMarkHelper.FillImage(
                        stream,
                        GetColor(config.fillcolor),
                        exportSize,
                        config.fillratio);

                    if (exportfolder is null)
                    {
                        var outPath = Path.Combine(AppContext.BaseDirectory, settings.exportfolder);
                        if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
                        exportfolder = new DirectoryInfo(outPath);
                    }
                    SaveImageFile(
                        bitmap,
                        fileinfo.Name.Replace(fileinfo.Extension, ""),
                        settings.exporttype,
                        exportfolder.FullName,
                        settings.exportquality
                        );
                    bitmap.Dispose();
                }
            }
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// 绘制文字logo
        /// 默认会有间距
        /// </summary>
        /// <param name="word"></param>
        /// <param name="sf"></param>
        /// <param name="font"></param>
        /// <param name="fillcolor">填充颜色</param>
        /// <param name="leftPadding">左右间距</param>
        /// <param name="topPadding">上下间距</param>
        /// <param name="splitLineFlag">是否分割线</param>
        /// <returns></returns>
        public static Bitmap CreateWordImage(string word, StringFormat sf, Font font, Color fillcolor, Color pencolor, Color splitLineColor, Size size, int leftPadding = 10, int topPadding = 10, bool splitLineFlag = false)
        {
            try
            {
                //默认宽度不设限
                var maxWidth = 9000;
                if (size != Size.Empty)
                {
                    //优先宽度
                    maxWidth = size.Width;
                    if (size.Width > 0 && size.Height <= 0) maxWidth = size.Width;
                    if (size.Width <= 0 && size.Height > 0) maxWidth = size.Height;
                }

                if (sf is null) sf = StringFormat.GenericTypographic;

                //计算估测的图片高度和宽度【不换行】
                //var drawInfoSize = ComputerDrawStringSize(word, font, sf, new SizeF(9000F,9000F), out int chartNum, out int lineNum);
                //固定宽度，计算估测的图片高度【换行】
                var drawInfoSize = ComputerDrawStringSize(word, font, maxWidth, sf);
                //图片总宽：绘制的宽度 + 左右间隔宽度
                var totalWidth = (int)Math.Round(
                drawInfoSize.Width + (2 * leftPadding)
                , 0);
                //图片总高：绘制的高度 + 上下间隔高度
                var totalHeight = (int)Math.Round(
                    drawInfoSize.Height + (2 * topPadding)
                    , 0);

                //创建绘制的矩形
                //在矩形中绘制，会自动换行
                var drawRectangle = new RectangleF()
                {
                    //尺寸
                    Size = new Size(totalWidth, totalHeight),
                    //绘制开始点
                    Location = new PointF(leftPadding, topPadding),
                };

                //创建位图对象
                Bitmap bitmap = new Bitmap(totalWidth, totalHeight);
                //创建图形对象
                using Graphics graphics = Graphics.FromImage(bitmap);
                #region 绘制图形质量配置
                //字体质量
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                graphics.TextContrast = 4;//高对比度,default : 4 
                //图形质量
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                #endregion
                #region 绘制方向 通过旋转图片绘制垂直方向的文字，ref:https://stackoverflow.com/questions/8970807
                if (sf.FormatFlags.HasFlag(StringFormatFlags.DirectionVertical))
                {
                    //结合旋转，可以实现从下到上书写，左对齐。
                    //sf.FormatFlags = StringFormatFlags.DirectionVertical|StringFormatFlags.DirectionRightToLeft;
                    //设置绘制点为右下角
                    //graphics.TranslateTransform(drawRectangle.Right, drawRectangle.Bottom);
                    //旋转180
                    //graphics.RotateTransform(180);
                }
                else
                {
                    //水平绘制，左对齐
                }
                #endregion
                //背景设置填充色
                graphics.Clear(fillcolor);
                //绘制画笔颜色
                using SolidBrush pen = new SolidBrush(pencolor);

                //绘制文字
                graphics.DrawString(word, font, pen, drawRectangle, sf);
                //绘制分割线
                if (splitLineFlag)
                {
                    graphics.DrawLine(
                        new Pen(splitLineColor),
                        0,
                        topPadding,
                        0,
                        drawRectangle.Size.Height - topPadding);
                }

                graphics.Flush();

                if (Environment.GetEnvironmentVariable("INFOPIC") is not null)
                {
                    SaveImageFile(bitmap, "info", ImageFormat.Jpeg, Path.Combine(AppContext.BaseDirectory, "infoExport"));
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        /// <summary>
        /// 计算绘制文字的图片高度和宽度【不换行】
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="font"></param>
        /// <param name="stringFormat"></param>
        /// <param name="sizeF"></param>
        /// <param name="chartNum"></param>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        public static SizeF ComputerDrawStringSize(string strContent, Font font, StringFormat stringFormat, SizeF sizeF, out int chartNum, out int lineNum)
        {
            using Graphics graphics = Graphics.FromImage(new Bitmap((int)sizeF.Width, (int)sizeF.Height));

            var drawStringSize = graphics.MeasureString(strContent, font, sizeF, stringFormat, out chartNum, out lineNum);
            //var TextRenderdrawStringSize =TextRenderer.MeasureText(strContent, font);
            //drawStringSize.Width = TextRenderdrawStringSize.Width;
            //drawStringSize.Height = TextRenderdrawStringSize.Height;
            return drawStringSize;
        }
        /// <summary>
        /// 指定最大宽度，计算绘制文字图像高度
        /// </summary>
        /// <param name="strContent"></param>
        /// <param name="font"></param>
        /// <param name="maxWidth"></param>
        /// <param name="stringFormat"></param>
        /// <returns></returns>
        public static SizeF ComputerDrawStringSize(string strContent, Font font, int maxWidth, StringFormat stringFormat = null)
        {
            using Graphics graphics = Graphics.FromImage(new Bitmap(maxWidth, 9000));
            var drawStringSize = graphics.MeasureString(strContent, font, maxWidth, stringFormat ?? StringFormat.GenericDefault);

            return drawStringSize;
        }

        /// <summary>
        /// 填充图片
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fillcolor"></param>
        /// <param name="targetSize"></param>
        /// <param name="fillratio"></param>
        /// <returns></returns>
        public static Bitmap FillImage(Stream stream, Color fillcolor, Size targetSize, double fillratio = 0.1)
        {
            try
            {
                var image = Bitmap.FromStream(stream);
                if (targetSize == Size.Empty || targetSize == image.Size)
                {
                    targetSize = image.Size;
                    Bitmap bitmap = new Bitmap(targetSize.Width, targetSize.Height);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    //图像质量
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置
                    graphics.Clear(fillcolor);

                    //计算坐标 (默认原图缩放10%的空间为空白填充)
                    var x = 0;
                    var y = 0;
                    if (fillratio > 0 && fillratio <= 1)
                    {
                        x = (int)Math.Ceiling(targetSize.Width * fillratio / 2);
                        y = (int)Math.Ceiling(targetSize.Height * fillratio / 2);
                    }
                    else
                    {
                        x = (int)fillratio;
                        y = (int)fillratio;
                    }

                    graphics.DrawImage(image,
                                       x,
                                       y,
                                       (targetSize.Width - (x * 2)),
                                       (targetSize.Height - (y * 2)));
                    graphics.Flush();
                    graphics.Dispose();

                    return bitmap;
                }
                else
                {
                    var originRatio = (double)image.Width / image.Height;
                    if (targetSize.Width > 0 && targetSize.Height <= 0)
                    {
                        targetSize = new Size()
                        {
                            Width = targetSize.Width,
                            Height = (int)Math.Round(targetSize.Width / originRatio, 0)
                        };
                    }
                    else if (targetSize.Width <= 0 && targetSize.Height > 0)
                    {
                        targetSize = new Size()
                        {
                            Width = (int)Math.Round(targetSize.Height * originRatio, 0),
                            Height = targetSize.Height
                        };
                    }
                    Bitmap bitmap = new Bitmap(targetSize.Width, targetSize.Height);
                    using Graphics graphics = Graphics.FromImage(bitmap);
                    //图像质量
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    //背景设置
                    graphics.Clear(fillcolor);

                    //计算绘制的图像大小
                    var scaleSize = ComputerMaxScaleSize(image.Size, targetSize);
                    //计算坐标 (默认原图缩放10%的空间为空白填充)

                    var x = (int)Math.Ceiling(((double)targetSize.Width - scaleSize.Width) / 2);
                    var y = (int)Math.Ceiling((double)(targetSize.Height - scaleSize.Height) / 2);

                    graphics.DrawImage(image,
                                       x,
                                       y,
                                       scaleSize.Width,
                                       scaleSize.Height);
                    graphics.Flush();
                    graphics.Dispose();

                    return bitmap;
                }

            }
            catch
            {

                throw;
            }
        }

        /******************************************Common Method***********************************************/

        #region Common Method
        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="imgname"></param>
        /// <param name="exporttype"></param>
        /// <param name="exportpath"></param>
        public static void SaveImageFile(Bitmap bitmap, string imgname, ImageFormat exporttype, string exportpath, long qualityNum = 100L)
        {
            //文件类型
            var imageFormat = exporttype;
            //名称 原图名称_yyMMddHHmmss.jpeg/bmp
            var filename = $"{imgname}_{DateTime.Now:yyMMddHHmmss}.{exporttype}";

            if (!Directory.Exists(exportpath)) Directory.CreateDirectory(exportpath);

            using FileStream filestream = File.OpenWrite(Path.Combine(exportpath, filename));

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualityNum);

            var encoder = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == imageFormat.Guid);

            bitmap.Save(filestream, encoder, encoderParameters);

            //bitmap.Save(filestream, imageFormat);
        }
        /// <summary>
        /// 格式化信息内容
        /// </summary>
        /// <param name="formatInfo"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static string FormatterInfo(string formatInfo, IReadOnlyDictionary<string, string> dict)
        {
            try
            {
                var formatterStr = formatInfo;
                var matchReg = @"(\{[\d\w\s]+\})";
                foreach (Match match in Regex.Matches(formatInfo, matchReg,
                                                        RegexOptions.None,
                                                        TimeSpan.FromSeconds(1)))
                {
                    var key = match.Value.Replace("{", "").Replace("}", "");
                    formatterStr = formatterStr.Replace(match.Value, dict[key] ?? string.Empty);
                }
                return formatterStr;
            }
            catch (Exception)
            {
                return formatInfo;
            }
        }
        /// <summary>
        /// 在指定文件夹下过滤获取logos
        /// </summary>
        /// <param name="make"></param>
        /// <returns></returns>
        public static FileInfo GetLogos(string folderpath, string make)
        {
            try
            {
                FileInfo fileinfo = null;
                foreach (var item in new DirectoryInfo(folderpath).GetFiles())
                {

                    var flag = make.ToLower().Contains(item.Name.Replace(item.Extension, "").ToLower());
                    if (flag) return item;
                }
                if (fileinfo is null)
                {
                    fileinfo = new DirectoryInfo(folderpath).GetFiles($"{make.ToLower()}*").FirstOrDefault();
                }
                return fileinfo;
            }
            catch (Exception)
            {

                throw;
            }
        }
        /// <summary>
        /// 获取字体
        /// </summary>
        /// <param name="font"></param>
        /// <param name="fontstyle"></param>
        /// <param name="size"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static Font GetFont(string font, FontStyle fontstyle, int size, GraphicsUnit unit)
        {
            var fontFileName = $"{font}-{System.Enum.GetName(typeof(FontStyle), fontstyle)}.*";
            var fontFileInfo = new DirectoryInfo(FontsFolder).GetFiles(fontFileName).FirstOrDefault();
            return FontHelper.GetFont(fontFileInfo.FullName, size, fontstyle, unit);
        }
        /// <summary>
        /// 获取字符绘制格式
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static StringFormat GetStringFormat(DrawDirectionEnum direction)
        {
            StringFormat sf = null;
            if (direction == DrawDirectionEnum.lefttoright)
            {
                sf = new StringFormat(StringFormat.GenericTypographic);
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
            }
            else if (direction == DrawDirectionEnum.toptobottom)
            {
                sf = new StringFormat(StringFormatFlags.DirectionVertical | StringFormatFlags.DirectionRightToLeft);
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
            }
            else
            {
                sf = new StringFormat(StringFormat.GenericTypographic);
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Near;
                sf.FormatFlags = StringFormatFlags.MeasureTrailingSpaces;
            }
            return sf;
        }
        /// <summary>
        /// Hex Convert To Argb Color
        /// </summary>
        /// <param name="Hex"></param>
        /// <returns></returns>
        public static Color GetColor(string Hex)
        {
            var b = Hex.ToCharArray();
            var R = Convert.ToInt32(b[0].ToString() + b[1].ToString(), 16);
            var G = Convert.ToInt32(b[2].ToString() + b[3].ToString(), 16);
            var B = Convert.ToInt32(b[4].ToString() + b[5].ToString(), 16);
            var A = Convert.ToInt32(b[6].ToString() + b[7].ToString(), 16);//透明度
            return Color.FromArgb(A, R, G, B);
        }
        /// <summary>
        /// 计算原图保持宽高比前提下，在指定尺寸下最大的缩放大小
        /// </summary>
        /// <param name="originSize"></param>
        /// <param name="targetSize"></param>
        /// <returns></returns>
        public static Size ComputerMaxScaleSize(Size originSize, Size targetSize, double keepBlank = 0.05)
        {

            try
            {
                var scaleSize = Size.Empty;

                var width = originSize.Width;
                var height = originSize.Height;
                var ratio = (float)width / height;
                if (targetSize == Size.Empty)
                {
                    scaleSize = originSize;
                }
                else if (targetSize.Width > 0 && targetSize.Height <= 0)
                {
                    scaleSize = new Size()
                    {
                        Width = targetSize.Width,
                        Height = (int)Math.Round(targetSize.Width / ratio, 0)
                    };
                }
                else if (targetSize.Width <= 0 && targetSize.Height > 0)
                {
                    scaleSize = new Size()
                    {
                        Width = (int)Math.Round(targetSize.Height * ratio, 0),
                        Height = targetSize.Height
                    };
                }
                else
                {
                    var maxwidth = targetSize.Width;
                    var maxheight = targetSize.Height;
                    var maxratio = (float)maxwidth / maxheight;

                    //目标大小的宽高比与原图相同
                    if (maxratio == ratio)
                    {
                        scaleSize = targetSize;
                    }
                    else
                    {
                        var gcd = GCD(width, height);

                        var minwidth = width / gcd;
                        var minheight = height / gcd;


                        var quotientW = (double)maxwidth / minwidth;
                        var quotientH = (double)maxheight / minheight;

                        var commonQuotient = Math.Min(quotientW, quotientH);

                        var resizeW = minwidth * commonQuotient;
                        var resizeH = minheight * commonQuotient;

                        //避免只有宽或高有留白,保留一定比例像素的留白
                        if (resizeH == maxheight && resizeW < maxwidth)
                        {
                            resizeH = resizeH - (int)Math.Round(resizeH * keepBlank, 0);
                            resizeW = (int)Math.Round(resizeH * ratio, 0);
                        }
                        else if (resizeH < maxheight && resizeW == maxwidth)
                        {
                            resizeW = resizeW - (int)Math.Round(resizeW * keepBlank, 0);
                            resizeH = (int)Math.Round(resizeW / ratio, 0);
                        }

                        scaleSize = new Size((int)resizeW, (int)resizeH);
                    }
                }
                return scaleSize;
            }
            catch (Exception)
            {
                throw;
            }
        }


        /// <summary>
        /// 公约数
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int GCD(int a, int b)
        {
            if (0 != b) while (0 != (a %= b) && 0 != (b %= a)) ;
            return a + b;
        }
        #endregion

    }
}
