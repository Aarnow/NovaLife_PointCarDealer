using Life.Network;
using Life.UI;
using SQLite;
using System.Threading.Tasks;
using ModKit.Helper;
using ModKit.Helper.PointHelper;
using mk = ModKit.Helper.TextFormattingHelper;
using System.Collections.Generic;
using System.Linq;
using Life;
using Newtonsoft.Json;
using UnityEngine;
using PointCarDealer.Entities;
using ModKit.Utils;
using System;

namespace PointCarDealer.Points
{
    public class CarDealer : ModKit.ORM.ModEntity<CarDealer>, PatternData
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string TypeName { get; set; }
        public string PatternName { get; set; }

        //Declare your other properties here
        public string SpawnPosition {  get; set; }
        [Ignore] public Vector3 LSpawnPosition { get; set; }
        public bool IsBizPoint { get; set; }
        public string Vehicles { get; set; }
        [Ignore] public List<int> LVehicles { get; set; }
        public string BizAllowed { get; set; }
        [Ignore] public List<int> LBizAllowed { get; set; }

        [Ignore] public ModKit.ModKit Context { get; set; }

        public CarDealer() { }
        public CarDealer(bool isCreated)
        {
            TypeName = nameof(CarDealer);
        }

        /// <summary>
        /// Applies the properties retrieved from the database during the generation of a point in the game using this model.
        /// </summary>
        /// <param name="patternId">The identifier of the pattern in the database.</param>
        public async Task SetProperties(int patternId)
        {
            var result = await Query(patternId);

            Id = patternId;
            TypeName = nameof(CarDealer);
            PatternName = result.PatternName;

            //Add your other properties here
            IsBizPoint = result.IsBizPoint;
            Vehicles = result.Vehicles;
            LVehicles = ListConverter.ReadJson(Vehicles);
            BizAllowed = result.BizAllowed;
            LBizAllowed = ListConverter.ReadJson(BizAllowed);
            SpawnPosition = result.SpawnPosition;
            LSpawnPosition = Vector3Converter.ReadJson(SpawnPosition);
        }

        /// <summary>
        /// Contains the action to perform when a player interacts with the point.
        /// </summary>
        /// <param name="player">The player interacting with the point.</param>
        public void OnPlayerTrigger(Player player)
        {
            if (LBizAllowed.Count == 0 || (player.HasBiz() && LBizAllowed.Contains(player.biz.Id)) || (player.IsAdmin && player.serviceAdmin)) PointCarDealerPanel(player);
            else player.Notify("PointCarDealer", "Vous n'avez pas la permission d'accéder à cette boutique.", NotificationManager.Type.Info);
        }

        #region CUSTOM
        public async void PointCarDealerPanel(Player player)
        {
            List<PointCarDealer_Vehicle> vehicles = await PointCarDealer_Vehicle.QueryAll();
            vehicles = vehicles.Where(i => LVehicles.Contains(i.Id)).ToList();

            Panel panel = Context.PanelHelper.Create($"{PatternName}", UIPanel.PanelType.TabPrice, player, () => PointCarDealerPanel(player));

            foreach (var vehicle in vehicles)
            {
                panel.AddTabLine($"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}", $"{vehicle.Price}€", VehicleUtils.GetIconId(vehicle.ModelId), _ => { });
            }

            if (vehicles.Count > 0)
            {
                panel.NextButton("Acheter", () =>
                {
                    if (vehicles[panel.selectedTab].IsBuyable)
                    {
                        //code
                    }
                    else
                    {
                        player.Notify("PointCarDealer", "Ce véhicule n'est pas achetable", NotificationManager.Type.Info);
                        panel.Refresh();
                    }
                });
                panel.NextButton("Vendre", () =>
                {
                    if (vehicles[panel.selectedTab].IsResellable)
                    {
                        //code
                    }
                    else
                    {
                        player.Notify("PointShop", "Cette objet n'est pas vendable", NotificationManager.Type.Info);
                        panel.Refresh();
                    }
                });
            }


            if (LBizAllowed.Count > 0)
            {
                panel.NextButton("Historique", async () =>
                {
                    if (player.HasBiz() || (player.IsAdmin && player.serviceAdmin && player.HasBiz()))
                    {
                        var permissions = await PermissionUtils.GetPlayerPermission(player);
                        if (player.biz.OwnerId == player.character.Id || (permissions.hasRemoveMoneyPermission && permissions.hasAddMoneyPermission)) PointCarDealerLogsPanel(player);
                        else player.Notify("PointCarDealer", "Vous ne disposez pas des droits sur le compte bancaire d'entreprise", Life.NotificationManager.Type.Warning);
                    }
                    else player.Notify("PointCarDealer", "Vous devez être propriétaire ou avoir les droits sur le compte en banque de votre société", Life.NotificationManager.Type.Warning);
                });
            }
            if (player.IsAdmin && player.serviceAdmin) panel.NextButton("Admin", () => PointCarDealerAdminPanel(player));
            panel.CloseButton();

            panel.Display();
        }
        public async void PointCarDealerLogsPanel(Player player)
        {
            List<PointCarDealer_Logs> logs = await PointCarDealer_Logs.QueryAll();
            logs = logs.Where(l => l.ShopId == Id && l.BizId == player.character.BizId).ToList();
            logs.Reverse();

            Panel panel = Context.PanelHelper.Create($"{PatternName} - Historique", UIPanel.PanelType.TabPrice, player, () => PointCarDealerLogsPanel(player));

            foreach (var log in logs)
            {
                panel.AddTabLine($"{mk.Color($"{(log.IsPurchase ? "ACHAT" : "VENTE")}", (log.IsPurchase ? mk.Colors.Success : mk.Colors.Orange))} par {mk.Color(log.CharacterFullName, mk.Colors.Info)}<br>" +
                    $"{mk.Size($"{VehicleUtils.GetModelNameByModelId(log.ModelId)}", 14)}",
                    $"{DateUtils.ConvertNumericalDateToString(log.CreatedAt)}<br>{mk.Align($"{mk.Color($"{(log.IsPurchase ? "-" : "+")} {log.Price}€", mk.Colors.Verbose)}", mk.Aligns.Center)}",
                    VehicleUtils.GetIconId(log.ModelId), _ => { });
            }

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        public async void PointCarDealerAdminPanel(Player player)
        {
            List<PointCarDealer_Vehicle> vehicles = await PointCarDealer_Vehicle.QueryAll();
            vehicles = vehicles.Where(i => LVehicles.Contains(i.Id)).ToList();

            Panel panel = Context.PanelHelper.Create($"{PatternName} - Modifier la boutique", UIPanel.PanelType.TabPrice, player, () => PointCarDealerAdminPanel(player));

            foreach (var vehicle in vehicles)
            {
                panel.AddTabLine($"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}", $"{vehicle.Price}€", VehicleUtils.GetIconId(vehicle.ModelId), _ => PointShopAdminItemPanel(player, vehicle));
            }

            panel.NextButton("Ajouter", () => PointShopAddItemPanel(player));
            if (vehicles.Count > 0) panel.AddButton("Modifier", _ => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        public void PointShopAddItemPanel(Player player)
        {
            Panel panel = Context.PanelHelper.Create($"{PatternName} - Ajouter un article", UIPanel.PanelType.Input, player, () => PointShopAddItemPanel(player));

            panel.TextLines.Add("Renseigner l'ID du modèle de véhicule");
            panel.inputPlaceholder = "exemple: 5";

            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (int.TryParse(panel.inputText, out int modelId))
                {
                    if (Nova.v.GetVehicleByModelId(modelId) != null)
                    {
                        PointCarDealer_Vehicle newVehicle = new PointCarDealer_Vehicle();
                        newVehicle.ModelId = modelId;
                        newVehicle.Price = 1;
                        newVehicle.IsBuyable = true;
                        newVehicle.IsResellable = true;
                        if (await newVehicle.Save())
                        {
                            LVehicles.Add(newVehicle.Id);
                            Vehicles = ListConverter.WriteJson(LVehicles);
                            await Save();
                            player.Notify("PointCarDealer", $"Article enregistré", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("PointCarDealer", $"Nous n'avons pas pu enregistrer cette article", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("PointCarDealer", $"Aucun objet ne correspond à l'ID {modelId}", NotificationManager.Type.Warning);
                        return false;
                    }
                }
                else
                {
                    player.Notify("PointCarDealer", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        public void PointShopAdminItemPanel(Player player, PointCarDealer_Vehicle vehicle)
        {
            var currentVehicle = Nova.v.GetVehicleByModelId(vehicle.ModelId);
            Panel panel = Context.PanelHelper.Create($"{PatternName} - Modifier un article", UIPanel.PanelType.TabPrice, player, () => PointShopAdminItemPanel(player, vehicle));

            panel.AddTabLine($"{mk.Color("Modèle:", mk.Colors.Info)} {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}", "", IconUtils.Others.None.Id, _ =>
            {
                player.Notify("PointShop", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Warning);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Prix:", mk.Colors.Info)} {vehicle.Price}€", "", IconUtils.Others.None.Id, _ =>
            {
                PointShopItemPricePanel(player, vehicle);
            });
            panel.AddTabLine($"{mk.Color("Achetable:", mk.Colors.Info)} {(vehicle.IsBuyable ? "Oui" : "Non")}", "", IconUtils.Others.None.Id, async _ =>
            {
                vehicle.IsBuyable = !vehicle.IsBuyable;
                if (await vehicle.Save()) player.Notify("PointCarDealer", "Modification enregistrée", NotificationManager.Type.Success);
                else player.Notify("PointCarDealer", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Vendable:", mk.Colors.Info)} {(vehicle.IsResellable ? "Oui" : "Non")}", "", IconUtils.Others.None.Id, async _ =>
            {
                vehicle.IsResellable = !vehicle.IsResellable;
                if (await vehicle.Save()) player.Notify("PointCarDealer", "Modification enregistrée", NotificationManager.Type.Success);
                else player.Notify("PointCarDealer", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                panel.Refresh();
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.PreviousButtonWithAction("Supprimer", async () =>
            {
                LVehicles.Remove(vehicle.Id);
                Vehicles = JsonConvert.SerializeObject(LVehicles);
                if (await Save())
                {
                    player.Notify("PointShop", "Suppression confirmée", NotificationManager.Type.Success);
                    return true;
                }
                else
                {
                    player.Notify("PointShop", "Nous n'avons pas pu enregistrer cette suppression", NotificationManager.Type.Error);
                    return false;
                }

            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        public void PointShopItemPricePanel(Player player, PointCarDealer_Vehicle vehicle)
        {
            Panel panel = Context.PanelHelper.Create($"{PatternName} - Modifier le prix", UIPanel.PanelType.Input, player, () => PointShopItemPricePanel(player, vehicle));

            panel.TextLines.Add("Définir le prix");
            panel.inputPlaceholder = "exemple: 1.50";

            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                string inputText = panel.inputText.Replace(",", ".");
                //DEBUG FORMAT INCORRECT AVEC POINT OU VIRGULE
                if (double.TryParse(inputText, out double price))
                {
                    vehicle.Price = Math.Round(price, 2);
                    if (await vehicle.Save())
                    {
                        player.Notify("PointCarDealer", "Modification enregistrée", NotificationManager.Type.Success);
                        return true;
                    }
                    else
                    {
                        player.Notify("PointCarDealer", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                        return true;
                    }
                }
                else
                {
                    player.Notify("PointCarDealer", "Format incorrect", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion

        /// <summary>
        /// Triggers the function to begin creating a new model.
        /// </summary>
        /// <param name="player">The player initiating the creation of the new model.</param>
        public void SetPatternData(Player player)
        {
            //Set the function to be called when a player clicks on the “create new model” button
            SetName(player);
        }
        /// <summary>
        /// Displays all properties of the pattern specified as parameter.
        /// The user can select one of the properties to make modifications.
        /// </summary>
        /// <param name="player">The player requesting to edit the pattern.</param>
        /// <param name="patternId">The ID of the pattern to be edited.</param>
        public async void EditPattern(Player player, int patternId)
        {
            CarDealer pattern = new CarDealer(false);
            pattern.Context = Context;
            await pattern.SetProperties(patternId);

            Panel panel = Context.PanelHelper.Create($"Modifier un {pattern.TypeName}", UIPanel.PanelType.Tab, player, () => EditPattern(player, patternId));


            panel.AddTabLine($"{mk.Color("Nom:", mk.Colors.Info)} {pattern.PatternName}", _ => {
                pattern.SetName(player, true);
            });
            panel.AddTabLine($"{mk.Color("Sociétés autorisées:", mk.Colors.Info)} {pattern.LBizAllowed.Count}", _ => {
                pattern.SetBizAllowed(player, true);
            });
            panel.AddTabLine($"{mk.Color("Utiliser l'argent des sociétés:", mk.Colors.Info)} {(pattern.IsBizPoint ? "Oui" : "Non")}", async _ => {
                pattern.IsBizPoint = !pattern.IsBizPoint;
                if (await pattern.Save())
                {
                    player.Notify("CarDealer", "Modification enregistrée", NotificationManager.Type.Success);
                    panel.Refresh();
                }
                else
                {
                    player.Notify("CarDealer", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                    panel.Refresh();
                }
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Allows the player to set a name for the pattern, either during creation or modification.
        /// </summary>
        /// <param name="player">The player interacting with the panel.</param>
        /// <param name="inEdition">A flag indicating if the pattern is being edited.</param>
        public void SetName(Player player, bool isEditing = false)
        {
            Panel panel = Context.PanelHelper.Create($"{(!isEditing ? "Créer" : "Modifier")} un modèle de {TypeName}", UIPanel.PanelType.Input, player, () => SetName(player));

            panel.TextLines.Add("Donner un nom à votre boutique");
            panel.inputPlaceholder = "3 caractères minimum";

            if (!isEditing)
            {
                panel.NextButton("Suivant", () =>
                {
                    if (panel.inputText.Length >= 3)
                    {
                        PatternName = panel.inputText;
                        LVehicles = new List<int>();
                        Vehicles = JsonConvert.SerializeObject(LVehicles);
                        LBizAllowed = new List<int>();
                        SetBizAllowed(player, isEditing);
                    }
                    else
                    {
                        player.Notify("Attention", "Vous devez donner un titre à votre boutique (3 caractères minimum)", Life.NotificationManager.Type.Warning);
                        panel.Refresh();
                    }
                });
            }
            else
            {
                panel.PreviousButtonWithAction("Confirmer", async () =>
                {
                    if (panel.inputText.Length >= 3)
                    {
                        PatternName = panel.inputText;
                        if (await Save()) return true;
                        else
                        {
                            player.Notify("Erreur", "échec lors de la sauvegarde de vos changements", Life.NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("Attention", "Vous devez donner un titre à votre boutique (3 caractères minimum)", Life.NotificationManager.Type.Warning);
                        return false;
                    }
                });
            }
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        public void SetBizAllowed(Player player, bool isEditing = false)
        {
            Panel panel = Context.PanelHelper.Create($"{(!isEditing ? "Créer" : "Modifier")} un modèle de {TypeName}", UIPanel.PanelType.TabPrice, player, () => SetBizAllowed(player));

            foreach (var biz in Nova.biz.bizs)
            {
                bool isAllowed = LBizAllowed.Contains(biz.Id);
                panel.AddTabLine($"{mk.Color($"{biz.BizName}", isAllowed ? mk.Colors.Success : mk.Colors.Error)}", _ => {
                    if (isAllowed) LBizAllowed.Remove(biz.Id);
                    else LBizAllowed.Add(biz.Id);
                    SetBizAllowed(player, isEditing);
                });
            }

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            if (!isEditing)
            {
                panel.NextButton("Sauvegarder", async () =>
                {
                    CarDealer newCarDealer = new CarDealer();

                    newCarDealer.TypeName = nameof(CarDealer);
                    newCarDealer.PatternName = PatternName;
                    newCarDealer.BizAllowed = ListConverter.WriteJson(LBizAllowed);
                    newCarDealer.LVehicles = new List<int>();
                    newCarDealer.Vehicles = ListConverter.WriteJson(LVehicles);
                    newCarDealer.LSpawnPosition = player.setup.transform.position;
                    newCarDealer.SpawnPosition = Vector3Converter.WriteJson(LSpawnPosition);

                    //function to call for the following property
                    // If you want to generate your point
                    if (await newCarDealer.Save())
                    {
                        player.Notify("PointShop", "Modifications enregistrées", NotificationManager.Type.Success);
                        ConfirmGeneratePoint(player, newCarDealer);
                    }
                    else
                    {
                        player.Notify("PointShop", "Nous n'avons pas pu enregistrer vos modifications", NotificationManager.Type.Error);
                        panel.Refresh();
                    }
                });
            }
            else
            {
                panel.PreviousButtonWithAction("Sauvegarder", async () =>
                {
                    BizAllowed = ListConverter.WriteJson(LBizAllowed);
                    //function to call for the following property
                    // If you want to generate your point
                    if (await Save())
                    {
                        player.Notify("PointShop", "Modifications enregistrées", NotificationManager.Type.Success);
                        return true;
                    }
                    else
                    {
                        player.Notify("PointShop", "Nous n'avons pas pu enregistrer vos modifications", NotificationManager.Type.Error);
                        return false;
                    }

                });
            }

            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        #region REPLACE YOUR CLASS/TYPE AS PARAMETER
        /// <summary>
        /// Displays a panel allowing the player to select a pattern from a list of patterns.
        /// </summary>
        /// <param name="player">The player selecting the pattern.</param>
        /// <param name="patterns">The list of patterns to choose from.</param>
        /// <param name="configuring">A flag indicating if the player is configuring.</param>
        public void SelectPattern(Player player, List<CarDealer> patterns, bool configuring)
        {
            Panel panel = Context.PanelHelper.Create("Choisir un modèle", UIPanel.PanelType.Tab, player, () => SelectPattern(player, patterns, configuring));

            foreach (var pattern in patterns)
            {
                panel.AddTabLine($"{pattern.PatternName}", _ => { });
            }
            if (patterns.Count == 0) panel.AddTabLine($"Vous n'avez aucun modèle de {TypeName}", _ => { });

            if (!configuring && patterns.Count != 0)
            {
                panel.CloseButtonWithAction("Confirmer", async () =>
                {
                    if (await Context.PointHelper.CreateNPoint(player, patterns[panel.selectedTab])) return true;
                    else return false;
                });
            }
            else
            {
                panel.NextButton("Modifier", () => {
                    EditPattern(player, patterns[panel.selectedTab].Id);
                });
                panel.NextButton("Supprimer", () => {
                    ConfirmDeletePattern(player, patterns[panel.selectedTab]);
                });
            }

            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPointsSettingPanel(player);
            });
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Confirms the generation of a point with a previously saved pattern.
        /// </summary>
        /// <param name="player">The player confirming the point generation.</param>
        /// <param name="pattern">The pattern to generate the point from.</param>
        public void ConfirmGeneratePoint(Player player, CarDealer pattern)
        {
            Panel panel = Context.PanelHelper.Create($"Modèle \"{pattern.PatternName}\" enregistré !", UIPanel.PanelType.Text, player, () =>
            ConfirmGeneratePoint(player, pattern));

            panel.TextLines.Add($"Voulez-vous générer un point sur votre position avec ce modèle \"{PatternName}\"");

            panel.CloseButtonWithAction("Générer", async () =>
            {
                if (await Context.PointHelper.CreateNPoint(player, pattern)) return true;
                else return false;
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion

        #region DO NOT EDIT
        /// <summary>
        /// Base panel allowing the user to choose between creating a pattern from scratch
        /// or generating a point from an existing pattern.
        /// </summary>
        /// <param name="player">The player initiating the creation or generation.</param>
        public void CreateOrGenerate(Player player)
        {
            Panel panel = Context.PanelHelper.Create($"Créer ou générer un {TypeName}", UIPanel.PanelType.Text, player, () => CreateOrGenerate(player));

            panel.TextLines.Add(mk.Pos($"{mk.Align($"{mk.Color("Générer", mk.Colors.Info)} utiliser un modèle existant. Les données sont partagés entre les points utilisant un même modèle.", mk.Aligns.Left)}", 5));
            panel.TextLines.Add("");
            panel.TextLines.Add($"{mk.Align($"{mk.Color("Créer:", mk.Colors.Info)} définir un nouveau modèle de A à Z.", mk.Aligns.Left)}");

            panel.NextButton("Créer", () =>
            {
                SetPatternData(player);
            });
            panel.NextButton("Générer", async () =>
            {
                await GetPatternData(player, false);
            });
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPointsPanel(player);
            });
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Retrieves all patterns before redirecting to a panel allowing the user various actions (CRUD).
        /// </summary>
        /// <param name="player">The player initiating the retrieval of pattern data.</param>
        /// <param name="configuring">A flag indicating if the user is configuring.</param>
        public async Task GetPatternData(Player player, bool configuring)
        {
            var patterns = await QueryAll();
            SelectPattern(player, patterns, configuring);
        }

        /// <summary>
        /// Confirms the deletion of the specified pattern.
        /// </summary>
        /// <param name="player">The player confirming the deletion.</param>
        /// <param name="patternData">The pattern data to be deleted.</param>
        public async void ConfirmDeletePattern(Player player, PatternData patternData)
        {
            var pattern = await Query(patternData.Id);

            Panel panel = Context.PanelHelper.Create($"Supprimer un modèle de {pattern.TypeName}", UIPanel.PanelType.Text, player, () =>
            ConfirmDeletePattern(player, patternData));

            panel.TextLines.Add($"Cette suppression entrainera également celle des points.");
            panel.TextLines.Add($"Êtes-vous sûr de vouloir supprimer le modèle \"{pattern.PatternName}\" ?");

            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (await Context.PointHelper.DeleteNPointsByPattern(player, pattern))
                {
                    if (await pattern.Delete())
                    {
                        return true;
                    }
                    else
                    {
                        player.Notify("Erreur", $"Nous n'avons pas pu supprimer le modèle \"{PatternName}\"", Life.NotificationManager.Type.Error, 6);
                        return false;
                    }
                }
                else
                {
                    player.Notify("Erreur", "Certains points n'ont pas pu être supprimés.", Life.NotificationManager.Type.Error, 6);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Retrieves all NPoints before redirecting to a panel allowing various actions by the user.
        /// </summary>
        /// <param name="player">The player retrieving the NPoints.</param>
        public async Task GetNPoints(Player player)
        {
            var points = await NPoint.Query(e => e.TypeName == nameof(CarDealer));
            SelectNPoint(player, points);
        }

        /// <summary>
        /// Lists the points using this pattern.
        /// </summary>
        /// <param name="player">The player selecting the points.</param>
        /// <param name="points">The list of points to choose from.</param>
        public async void SelectNPoint(Player player, List<NPoint> points)
        {
            var patterns = await QueryAll();
            Panel panel = Context.PanelHelper.Create($"Points de type {nameof(CarDealer)}", UIPanel.PanelType.Tab, player, () => SelectNPoint(player, points));

            if (points.Count > 0)
            {
                foreach (var point in points)
                {
                    var currentPattern = patterns.FirstOrDefault(p => p.Id == point.PatternId);
                    panel.AddTabLine($"point n° {point.Id}: {(currentPattern != default ? currentPattern.PatternName : "???")}", _ => { });
                }

                panel.NextButton("Voir", () =>
                {
                    DisplayNPoint(player, points[panel.selectedTab]);
                });
                panel.NextButton("Supprimer", async () =>
                {
                    await Context.PointHelper.DeleteNPoint(points[panel.selectedTab]);
                    await GetNPoints(player);
                });
            }
            else
            {
                panel.AddTabLine($"Aucun point de ce type", _ => { });
            }
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.AdminPointsSettingPanel(player);
            });
            panel.CloseButton();

            panel.Display();
        }

        /// <summary>
        /// Displays the information of a point and allows the user to modify it.
        /// </summary>
        /// <param name="player">The player viewing the point information.</param>
        /// <param name="point">The point to display information for.</param>
        public async void DisplayNPoint(Player player, NPoint point)
        {
            var pattern = await Query(p => p.Id == point.PatternId);
            Panel panel = Context.PanelHelper.Create($"Point n° {point.Id}", UIPanel.PanelType.Tab, player, () => DisplayNPoint(player, point));

            panel.AddTabLine($"Type: {point.TypeName}", _ => { });
            panel.AddTabLine($"Modèle: {(pattern[0] != null ? pattern[0].PatternName : "???")}", _ => { });
            panel.AddTabLine($"", _ => { });
            panel.AddTabLine($"Position: {point.Position}", _ => { });


            panel.AddButton("TP", ui =>
            {
                Context.PointHelper.PlayerSetPositionToNPoint(player, point);
            });
            panel.AddButton("Définir pos.", async ui =>
            {
                await Context.PointHelper.SetNPointPosition(player, point);
                panel.Refresh();
            });
            panel.PreviousButton();
            panel.CloseButton();

            panel.Display();
        }
        #endregion
    }
}
