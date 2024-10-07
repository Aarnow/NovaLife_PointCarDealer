using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Interfaces;
using Newtonsoft.Json;
using PointCarDealer.Classes;
using System.IO;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace PointCarDealer
{
    public class PointCarDealer : ModKit.ModKit
    {
        public static string ConfigDirectoryPath;
        public static string ConfigPointCarDealerPath;
        public static PointCarDealerConfig _pointCarDealerConfig;

        public PointCarDealer(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InitConfig();
            _pointCarDealerConfig = LoadConfigFile(ConfigPointCarDealerPath);

            /*Orm.RegisterTable<PointShop_Logs>();
            Orm.RegisterTable<PointShop_Item>();*/

            Orm.RegisterTable<Points.CarDealer>();
            PointHelper.AddPattern("CarDealer", new Points.CarDealer(false));
            AAMenu.AAMenu.menu.AddBuilder(PluginInformations, "CarDealer", new Points.CarDealer(false), this);

            InsertMenu();

            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        #region Config
        private void InitConfig()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/PointShop";
                ConfigPointCarDealerPath = Path.Combine(ConfigDirectoryPath, "pointCarDealerConfig.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!File.Exists(ConfigPointCarDealerPath)) InitPointCarDealerConfig();
            }
            catch (IOException ex)
            {
                ModKit.Internal.Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitPointCarDealerConfig()
        {
            PointCarDealerConfig pointCarDealerConfig = new PointCarDealerConfig();
            string json = JsonConvert.SerializeObject(pointCarDealerConfig);
            File.WriteAllText(ConfigPointCarDealerPath, json);
        }

        private PointCarDealerConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                PointCarDealerConfig pointCarDealerConfig = JsonConvert.DeserializeObject<PointCarDealerConfig>(jsonContent);

                return pointCarDealerConfig;
            }
            else return null;
        }
        #endregion

        public void InsertMenu()
        {
            _menu.AddAdminPluginTabLine(PluginInformations, 5, "PointCarDealer", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                PointCarDealerPanel(player);
            });
        }

        public void PointCarDealerPanel(Player player)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("PointCarDealer", UIPanel.PanelType.TabPrice, player, () => PointCarDealerPanel(player));

            //Corps
            panel.AddTabLine($"{mk.Color("Appliquer la configuration", mk.Colors.Info)}", _ =>
            {
                _pointCarDealerConfig = LoadConfigFile(ConfigPointCarDealerPath);
                panel.Refresh();
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPluginPanel(player));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
