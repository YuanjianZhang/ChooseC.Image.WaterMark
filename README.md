## ChooseC.Image.WaterMark

用于给图像添加相框，可以将照片信息【焦距、光圈、快门速度、ISO...】绘制添加到相框中里。

### 项目结构

----

```batch project.tree

功能代码：ChooseC.Image.WaterMark.Core

控制台：ChooseC.Image.WaterMark.ConsoleApp
```

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

> + exportsize
>   + width
>   + height
>
>**width、height都为0，原尺寸输出，不进行缩放**
>
>**只有width 或 heiht,根据原图宽高比进行缩放**
>
>[System.Drawing.Size](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.size)

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
