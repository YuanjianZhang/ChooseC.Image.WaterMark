using ChooseC.Image.WaterMark.Core.Model;
using MetadataExtractor.Formats.Exif;

namespace ChooseC.Image.WaterMark.Core
{
    public class ImageMetaDataHelper
    {
        public static IReadOnlyDictionary<string, string> GetMetaData(string path, params string[] tags)
        {
            using var filestream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return GetMetaData(filestream, tags);
        }
        public static IReadOnlyDictionary<string, string> GetMetaData(Stream stream, params string[] tags)
        {
            try
            {
                var dict = new Dictionary<string, string>();
                var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(stream);
                foreach (var tag in tags)
                {
                    switch (tag)
                    {
                        case nameof(InfoTag.camermake):
                            dict.Add(tag, GetDescription_IFD0(directories, InfoTag.camermake));
                            break;
                        case nameof(InfoTag.camermodel):
                            dict.Add(tag, GetDescription_IFD0(directories, InfoTag.camermodel));
                            break;
                        case nameof(InfoTag.focallenth):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.focallenth));
                            break;
                        case nameof(InfoTag.aperture):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.aperture));
                            break;
                        case nameof(InfoTag.exposure):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.exposure));
                            break;
                        case nameof(InfoTag.iso):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.iso));
                            break;
                        case nameof(InfoTag.origintime):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.origintime));
                            break;
                        case nameof(InfoTag.createtime):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.createtime));
                            break;
                        case nameof(InfoTag.location):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.location));
                            break;
                        case nameof(InfoTag.lensmake):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.lensmake));
                            break;
                        case nameof(InfoTag.lensmodel):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.lensmodel));
                            break;
                        case nameof(InfoTag.lensspec):
                            dict.Add(tag, GetDescription_SubIFD(directories, InfoTag.lensspec));
                            break;
                        default:
                            dict.Add(tag, string.Empty);
                            break;
                    }
                }
                return dict;
            }
            catch (Exception)
            {

                throw;
            }
        }
        public static string GetMetaData(string path, string tag)
        {
            using var filestream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return GetMetaData(filestream, tag);
        }
        public static string GetMetaData(Stream stream, string tag)
        {
            try
            {
                var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(stream);
                switch (tag)
                {
                    case nameof(InfoTag.camermake):
                        return GetDescription_IFD0(directories, InfoTag.camermake);
                    case nameof(InfoTag.camermodel):
                        return GetDescription_IFD0(directories, InfoTag.camermodel);
                    case nameof(InfoTag.focallenth):
                        return GetDescription_SubIFD(directories, InfoTag.focallenth);
                    case nameof(InfoTag.aperture):
                        return GetDescription_SubIFD(directories, InfoTag.aperture);
                    case nameof(InfoTag.exposure):
                        return GetDescription_SubIFD(directories, InfoTag.exposure);
                    case nameof(InfoTag.iso):
                        return GetDescription_SubIFD(directories, InfoTag.iso);
                    case nameof(InfoTag.origintime):
                        return GetDescription_SubIFD(directories, InfoTag.origintime);
                    case nameof(InfoTag.createtime):
                        return GetDescription_SubIFD(directories, InfoTag.createtime);
                    case nameof(InfoTag.location):
                        return GetDescription_SubIFD(directories, InfoTag.location);
                    case nameof(InfoTag.lensmake):
                        return GetDescription_SubIFD(directories, InfoTag.lensmake);
                    case nameof(InfoTag.lensmodel):
                        return GetDescription_SubIFD(directories, InfoTag.lensmodel);
                    default:
                        return string.Empty;
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        /// <summary>
        /// Exif SubIFD
        /// </summary>
        /// <param name="list"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        protected static string GetDescription_SubIFD(IEnumerable<MetadataExtractor.Directory> list, int ID)
        {
            var subIfdDirectory = list.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            return subIfdDirectory?.GetDescription(ID);
        }
        /// <summary>
        /// Exif IFD0 
        /// </summary>
        /// <param name="list"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        protected static string GetDescription_IFD0(IEnumerable<MetadataExtractor.Directory> list, int ID)
        {
            var IfdDirectory = list.OfType<ExifIfd0Directory>().FirstOrDefault();
            return IfdDirectory?.GetDescription(ID);
        }
    }
}
