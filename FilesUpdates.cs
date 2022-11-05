using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using System.IO;
using System.Xml.Linq;
using System.Net;

namespace MichomeFirmwareManager
{
    public static class FilesUpdates
    {
        /// <summary>
        /// Возвращет все доступные ПО
        /// </summary>
        /// <returns>Первое имя, второе путь</returns>
        public static Dictionary<string, string> GetLocalFirmwares()
        {
            Dictionary<string, string> tmp = new Dictionary<string, string>();

            foreach (var item in Directory.GetFiles("firmwares/", "*.mfir"))
            {
                tmp.Add(Path.GetFileNameWithoutExtension(item), item);
            }
            return tmp;
        }

        /// <summary>
        /// Возвращет все доступные ПО
        /// </summary>
        /// <returns>Первое имя, второе путь</returns>
        public static Dictionary<string, string> GetGatewayFirmwares(string ipGateway)
        {
            Dictionary<string, string> tmp = new Dictionary<string, string>();
            string firs = "";
            try
            {
                firs = michomeframework.Gateway.SetData(ipGateway, "michome/firmwares/get.php?type=modules");
            }
            catch
            {
                return tmp;
            }

            foreach (var item in firs.Split(';'))
            {
                if(item != "")
                tmp.Add(Path.GetFileNameWithoutExtension(item), "http://"+ ipGateway + "/michome/firmwares/" + item);
            }
            return tmp;
        }
    }

    public class FileUpdate
    {
        public FileUpdate(string file)
        {
            File = file;
            if (file.Contains("http://"))
            {
                Directory.CreateDirectory("FromGateway");
                new WebClient().DownloadFile(file, "FromGateway\\" + Path.GetFileName(file));
                File = "FromGateway\\" + Path.GetFileName(file);
            }
          
            var text = new StreamReader(ZipFile.Read(File)["metadata.xml"].OpenReader()).ReadToEnd();
            Meta = new MetaFile(text);
        }

        public string File
        {
            get;
            set;
        }

        public MetaFile Meta
        {
            get;
        }

        public long SizeFirmware
        {
            get { return ZipFile.Read(File)[Meta.Firmware].UncompressedSize; }
        }

        public void ExtractFirmware()
        {
            if (System.IO.File.Exists(Meta.Firmware))
                System.IO.File.Delete(Meta.Firmware);
            ZipFile.Read(File)[Meta.Firmware].Extract();
        }
    }

    public class MetaFile
    {
        public MetaFile(string text)
        {
            var xdoc = XDocument.Parse(text).Element("Manifest");
            Name = xdoc.Element("Name").Value;
            ModuleType = xdoc.Element("ModuleType").Value;
            ModuleID = xdoc.Element("ModuleID").Value;
            FirmwareVersion = xdoc.Element("FirmwareVersion").Value;
            FirmwareMichome = xdoc.Element("FirmwareMichome").Value;
            Date = xdoc.Element("Date").Value;
            Firmware = xdoc.Element("Firmware").Value;
        }
        public string Name
        {
            get;
            set;
        }

        public string ModuleType
        {
            get;
            set;
        }

        public string ModuleID
        {
            get;
            set;
        }

        public string FirmwareVersion
        {
            get;
            set;
        }

        public string FirmwareMichome
        {
            get;
            set;
        }

        public string Date
        {
            get;
            set;
        }

        public string Firmware
        {
            get;
            set;
        }
    }
}
