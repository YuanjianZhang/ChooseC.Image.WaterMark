## ChooseC.Image.WaterMark

用于给图像添加相框，可以将照片信息【焦距、光圈、快门速度、ISO...】绘制添加到相框中里。  
想法来源于[semi-utils](https://github.com/leslievan/semi-utils)，想着用Csharp实现一个简单点，自己用的小程序，正好练练手。

### 项目结构

>功能代码：ChooseC.Image.WaterMark.Core  
控制台：ChooseC.Image.WaterMark.ConsoleApp 

### 命令说明

```shell
>WaterMark.exe R -h
Description:
  执行子命令
Usage:
  WaterMark run [options]

Options:
  --path <path>                                  文件/文件夹路径 [支持相对路径和绝对路径]
  --layout <bottom|onlyfill|surround|topbottom>  水印布局 [default: bottom，默认底部布局，可以不指定]
  --type <type>                                  图片类型 [default: jpg|png，只支持jpg或png格式图片]
  --ef, --export-folder <export-folder>          导出文件夹路径 [默认以程序执行路径为父目录，导出至Export文件夹中]
  --direction <lefttoright|toptobottom>          水印绘制方向 [default: lefttoright]
  --size <size>                                  大小 [default: {Width=0, Height=0}]
```
  
**`--path`**：必须要有值，路径为文件路径时，只处理单文件，指定为目录时，处理文件夹下所有文件。

### 运行实例

```batch
@REM 使用默认底部布局，处理当前程序执行路径下demo文件夹中所有图片
WaterMark.exe R --path "demo"
@REM 上下布局
WaterMark.exe R --path "demo\demo.jpg" --layout topbottom
@REM 底部布局
WaterMark.exe R --path "demo\demo.jpg" --layout bottom
@REM 底部环绕布局
WaterMark.exe R --path "demo\demo.jpg" --layout surround
@REM 只填充
WaterMark.exe R --path "demo\demo.jpg" --layout onlyfill
```

#### 上下排版 topbottom

![上下排版](demo/demo_230606223945.Png)

#### 底部布局 bottom

![左右排版](demo/demo_230606224019.Png)

### 常见问题

### 功能

----

#### 照片水印框布局配置

+ [x] 底部布局
  + [x] 上下排版
  + [x] 左右排版
+ [x] 底部环绕
  + [x] 左右排版
+ [x] 空白填充

#### 水印信息配置

可使用的EXIF信息：

|tag|EXIF|
|:--|:--|
|camermake  |相机厂商     |
|camermodel |相机型号     |
|focallenth |焦距        |
|aperture   |光圈        |
|exposure   |快门/曝光    |
|iso        |ISO        |
|origintime |源拍摄时间   |
|createtime |文件创建时间 |
|location   |地点        |
|lensmake   |镜头厂商     |
|lensmodel  |镜头型号     |
|lensspec   |镜头规格     |

#### 导出配置

导出类型：

+ [x] Bmp
+ [x] Png
+ [x] Jpeg【默认】

导出尺寸：

+ [x] 指定宽
+ [x] 指定高
+ [x] 指定宽高
+ [x] 原图

### 配置说明

配置文件：

+ [appsettings.develop.json](ChooseC.Image.WaterMark.ConsoleApp/appsettings.develop.json)
+ [appsettings.json](ChooseC.Image.WaterMark.ConsoleApp/appsettings.json)

**启用`appsettings.develop.json`需要配置环境变量`ENV=develop`**

**`Settings`是配置根节点名称**

----

#### Settings

#### exporttype

导出图片类型

>[System.Drawing.Imaging.ImageFormat](https://learn.microsoft.com/zh-cn/dotnet/api/system.drawing.imaging.imageformat)

#### exportquality

导出图片质量

#### exportfolder

导出文件夹名称，默认父目录为程序运行目录

#### exportsize

图片导出尺寸，保持原图宽高比缩放，相差部份使用空白填充

尺寸为实际输出图片中除去水印相框的尺寸【OnlyFill为实际输出图片尺寸，其它为图像内容尺寸】

>`(width > 0 && height == 0 ) || (width == 0 && height >0)`：根据原图的宽高比计算缩放后的图片，与导出尺寸有相差的部份，使用fillcolor填充。【处理后的图像，尺寸只能确保宽或高为指定值。】
>`(width == 0&&height == 0)`：不缩放图片
>`(width > 0 && height > 0)`：根据导出尺寸，在保持原图宽高比的前提下，计算原图的最大缩放比例，与导出尺寸有相差的部份，使用fillcolor填充。【处理后的图像，尺寸与配置相同】
>
> > + exportsize
> > + width
> > + height
> >[System.Drawing.Size](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.size)

#### sign

个性签名

----

### Layout

#### topbottom

上下排版

#### toplogo 上方logo

上下排版中上方logo部份配置

|value|remark|
|:--|:--|
|none|不绘制|
|imglogo|图标logo|
|wordlogo|文字logo|

#### bottominfo 下方信息

使用`{tag}`进行信息格式串编写,eg:`{lensmodel} | {focallenth} {aperture} {exposure} {iso}`

**[tag列表](#水印信息配置)**

#### fillcolor：填充颜色

8位16进制数，最后两位代表透明度

>eg：
白色：`#FFFFFFFF`
黑色：`#000000FF`

#### fillratio：填充比率

背景填充空白占比
$1>fillratio>0$：相对原图百分比占比
$fillratio>1$：占比像素值

#### logmaxheight

logo最大高度

#### bottommaxsize

底部信息最大尺寸

上下布局中，限制`bottominfo`高度，logo高度通过`logmaxheight`配置

#### wordlogofont 或 infofont

logo字体配置 或 水印信息字体配置

#### fontname

字体名称

已有字体：

+ NotoSerif
+ SmileySans
+ SourceHanSansCN
+ SourceHanSerifSC

可以自行添加

**字体资源路径为`{AppContext.BaseDirectory}\Source\Fonts`,`AppContext.BaseDirectory`为当前程序运行时的路径**

**字体文件命名格式为`{fontname}-{fontstyle}.ttf`**

+ 不一定是`ttf`，也可以是`otf`
+ `fontstyle`必须为`System.Drawing.FontStyle`枚举名称

#### fontcolor：字体颜色

8位16进制数，最后两位代表透明度

>eg：
白色：`#FFFFFFFF`
黑色：`#000000FF`

#### fontstyle：字体样式

|value|
|:--|
|Regular|
|Bold|
|Italic|
|Underline|
|Strikeout|

#### fontsize

字体大小，72、48、32...

#### fontunit

字体绘制单位

----

### Layout：

#### bottom 或 surround

底部布局或底部环绕布局

#### leftlogo

左边logo

#### leftinfo

左边水印信息

#### rightlogo

右边logo

#### rightinfo

右边信息

#### fillcolor

填充颜色

#### fillratio

填充比率

#### logmaxheight

logo最大高度

#### bottommaxsize

底部信息最大尺寸

左右布局中，限制底部总高度，`logomaxheight`优先级低于此项。

#### wordlogofont 或 infofont

logo字体或水印信息字体

#### fontname

字体名称

#### fontcolor

字体颜色

#### fontstyle

字体样式

#### fontsize

字体大小

#### fontunit

字体绘制单位

----

### Layout：

#### onlyfill

包围填充布局

#### fillcolor

填充颜色

#### fillratio

填充相对原图占比
