using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using michomeframework;
using michomeframework.Settings;
using michomeframework.Modules;
using michomeframework.Modules.StaticController;
using System.Diagnostics;

namespace MichomeFirmwareManager
{
    public partial class Form1 : Form
    {
        Setting set;
        List<string> MODULEaddress = new List<string>();
        TreeNode SelFir;
        TreeNode SelMod;
        public string GatewayIP
        {
            get => toolStripTextBox1.Text;
            set => toolStripTextBox1.Text = value;
        }
        public bool IsDevMode
        {
            get { return включитьToolStripMenuItem.Checked; }
            set { включитьToolStripMenuItem.Checked = value; выключитьToolStripMenuItem.Checked = !value; }
        }
        public string Status
        {
            get => toolStripStatusLabel1.Text;
            set => toolStripStatusLabel1.Text = value;
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void AddModuleList(string moduleID)
        {
            if (!MODULEaddress.Contains(moduleID))
                MODULEaddress.Add(moduleID);
            else return;

            treeView2.Invoke(new Action(() => {
                var node = new TreeNode(moduleID) { Name = moduleID };
                node.ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Перезагрузить модуль", (a,b) => Gateway.SetData(moduleID, "restart")) });
                treeView2.Nodes["root"].Nodes.Add(node);
            }));
            
        }

        private void AddFirmwares()
        {
            treeView1.Nodes.Add(new TreeNode("Локальные файлы") { Name = "local" });
            treeView1.Nodes.Add(new TreeNode("Файлы на шлюзе") { Name = "gateway" });
            foreach (var item in FilesUpdates.GetLocalFirmwares())
            {
                var node = new TreeNode(item.Key) { Name = item.Value };
                //node.ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Перезагрузить модуль", (a, b) => Gateway.SetData(moduleID, "restart")) });
                treeView1.Nodes["local"].Nodes.Add(node);
            }
            
            foreach (var item in FilesUpdates.GetGatewayFirmwares(GatewayIP))
            {
                var node = new TreeNode(item.Key) { Name = item.Value };
                //node.ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Перезагрузить модуль", (a, b) => Gateway.SetData(moduleID, "restart")) });
                treeView1.Nodes["gateway"].Nodes.Add(node);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (SettingManager.IsSetting())
            {
                set = SettingManager.Load();
                GatewayIP = set.GetData(Setting.GatewayIP, "");
            }
            else
            {
                set = SettingManager.Create();
                set.SetData(Setting.GatewayIP, "127.0.0.1");
                set.Save();
                GatewayIP = set.GetData(Setting.GatewayIP, "");
            }
            set.AutoSave = true;

            UDPControllers.OnSearch += (o, a) => 
            {
                if (o.ToString().Split('_').Length < 2) return;
                AddModuleList(o.ToString().Split('_')[1]);               
            };
            UDPControllers.StartSearchUDPControllers();

            treeView2.Nodes.Add(new TreeNode(UDPControllers.broadcast.ToString()) { Name="root" });

            AddFirmwares();
        }

        private void применитьНастройкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            set.SetData(Setting.GatewayIP, GatewayIP);
            Application.Restart();
        }

        private void treeView2_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var ip = e.Node.Name;
            if (ip == "root") return;
            SelMod = e.Node;
            var Module = new Module(ip);
            var ModuleInfo = new Module(ip).GetModuleInfo;
            label21.Text = ModuleInfo.Type;
            label19.Text = ModuleInfo.Name;
            label17.Text = ModuleInfo.FirmwareVersion;
            label15.Text = ModuleInfo.MichomeVersion;
            label24.Text = ModuleInfo.FlashSize;
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var path = e.Node.Name;
            if (path == "root" || path == "local" || path == "gateway") return;
            SelFir = e.Node;
            var File = new FileUpdate(path);
            label2.Text = File.Meta.Name;
            label3.Text = File.SizeFirmware.ToString();
            label7.Text = File.Meta.ModuleType;
            label5.Text = File.Meta.ModuleID;
            label9.Text = File.Meta.FirmwareVersion;
            label11.Text = File.Meta.FirmwareMichome;
            label13.Text = File.Meta.Date;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(SelFir == null)
            {
                MessageBox.Show("Пожалуйста, выберите ПО для обновления");
                return;
            }
            if (SelMod == null)
            {
                MessageBox.Show("Пожалуйста, выберите модуль для обновления");
                return;
            }
            var path = SelFir.Name;
            var File = new FileUpdate(path);
            var Module = new Module(SelMod.Name);
            var ModuleInfo = new Module(SelMod.Name).GetModuleInfo;

            if(ModuleInfo.Type != File.Meta.ModuleType && !IsDevMode)
            {
                MessageBox.Show("Данное ПО не предназначенно для данного типа модуля");
                return;
            }

            if (ModuleInfo.Name != File.Meta.ModuleID && !IsDevMode)
            {
                if(MessageBox.Show("Данное ПО, возможно, не совместимо с данным модулем. Вы хотите продолжить?", "", MessageBoxButtons.YesNo) == DialogResult.No)
                    return;
            }

            if (!System.IO.File.Exists("espota.py"))
            {
                Status = "Ошибка. Отсутствует espota.py";
                MessageBox.Show("Ошибка. Отсутствует espota.py");
                return;
            }

            Status = "Идет распаковка ПО...";
            File.ExtractFirmware();
            Status = "ПО успешно распакованно...";

            StartUpdate(File.Meta.Firmware, ModuleInfo.IP);
        }

        private void включитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IsDevMode = true;
        }

        private void выключитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IsDevMode = false;
        }

        void StartUpdate(string path, string module)
        {
            Status = "Идет обновление ПО";
            Enabled = false;
            //python espota.py -i 192.168.1.33 -I 192.168.1.39 -p 8266 -P 10001 -f b5.bin
            Process ota = new Process();
            ota.StartInfo = new ProcessStartInfo("python", "espota.py -i "+module+" -I "+UDPControllers.LocalIPAddress()+" -p 8266 -P 10001 -f " + path);
            ota.StartInfo.RedirectStandardOutput = true;
            ota.StartInfo.RedirectStandardError = true;
            ota.StartInfo.UseShellExecute = false;
            ota.StartInfo.CreateNoWindow = true;
            ota.OutputDataReceived += (a, b) => { Console.WriteLine(b.Data); };
            ota.ErrorDataReceived += (a, b) => { Console.WriteLine(b.Data); };
            ota.EnableRaisingEvents = true;
            ota.Exited += (a, b) => { Invoke(new Action(() => { Enabled = true; Status = "Обновление ПО завершено"; System.IO.File.Delete(path); })); };
            ota.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (SelMod == null)
                return;
            Process.Start("http://"+SelMod.Name+"/configurator");
        }
    }
}
