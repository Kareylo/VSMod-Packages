using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Packages.Models;
using Packages.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Item = Packages.Models.Item;

namespace Packages;

public class PackagesModSystem : ModSystem
{
        private ICoreServerAPI api;
        private string DataDirectory;
        private string PackageFile;

        private Dictionary<string, Package> Packages { get; set; }

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            DataDirectory = this.api.GetOrCreateDataPath("Packages");
            PackageFile = Path.Combine(DataDirectory, "packages.json");

            CmdInit(null);
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            
            api.Permissions.RegisterPrivilege(Privileges.User, "");
            api.Permissions.RegisterPrivilege(Privileges.Admin, "");

            api.ChatCommands.Create("package")
                .WithDescription(Lang.Get("packages:pkg-desc"))
                .RequiresPrivilege(Privileges.User)
                .RequiresPlayer()

                // Command /package give <package> [Player]
                .BeginSubCommand("give")
                .WithDescription(Lang.Get("packages:pkg-give"))
                .RequiresPrivilege(Privileges.User)
                .RequiresPlayer()
                .WithArgs(
                    parsers.Word(Lang.Get("packages:pkg-give-args-package")), 
                    parsers.OptionalEntities(Lang.Get("packages:pkg-give-args-player")))
                .HandleWith(CmdGive)
                .EndSubCommand()

                // Command /package list
                .BeginSubCommand("list")
                .WithDescription(Lang.Get("packages:pkg-list"))
                .RequiresPrivilege(Privileges.User)
                .RequiresPlayer()
                .HandleWith(CmdList)
                .EndSubCommand()

                // Command /package refresh
                .BeginSubCommand("refresh")
                .WithDescription(Lang.Get("packages:pkg-refresh"))
                .RequiresPrivilege(Privileges.Admin)
                .RequiresPlayer()
                .HandleWith(CmdInit)
                .EndSubCommand()

                // Comand /package items
                .BeginSubCommand("items")
                .WithDescription(Lang.Get("packages:pkg-items"))
                .RequiresPrivilege(Privileges.Admin)
                .RequiresPlayer()
                .HandleWith(CmdItems)
                .EndSubCommand()
                .Validate();
        }

        /// <summary>
        /// Logic of /package refresh and init of the packages
        /// </summary>
        private TextCommandResult CmdInit(TextCommandCallingArgs args)
        {
            try
            {
                Packages = new Dictionary<string, Package>();
                
                if (File.Exists(PackageFile))
                {
                    
                    List<Package> packages = Serializer.Deserialize<List<Package>>(File.ReadAllText(PackageFile));
                    Packages = packages.ToDictionary(package => package.Name.ToUpper(), package => package);
                }
                else
                {
                    // Creation of default Package file
                    List<Package> packages = new List<Package>()
                    {
                        new Package()
                        {
                            Name = "Example Package",
                            Description = "Example Package giving 32 Cattails",
                            Items = new List<Item>()
                            {
                                new Item()
                                {
                                    Name = "cattailtops",
                                    Quantity = 32
                                }
                            }
                        }
                    };
                    File.WriteAllText(PackageFile, Serializer.Serialize(packages));
                }
                return TextCommandResult.Error(Lang.Get("packages:pkg-init-success"));
            }
            catch (Exception e)
            {
                api.Logger.Error(e);
                return TextCommandResult.Error(Lang.Get("packages:pkg-init-error"));
            }
        }

        /// <summary>
        /// Logic of /package give command
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private TextCommandResult CmdGive(TextCommandCallingArgs args)
        {
            try
            {
                string package = args.Parsers[0].GetValue().ToString();
                IPlayer player = args.Parsers[1].IsMissing
                    ? args.Caller.Player
                    : api.Server.Players.FirstOrDefault(x => x.PlayerName.Equals(args.Parsers[1].GetValue().ToString(),
                        StringComparison.InvariantCultureIgnoreCase));

                if (string.IsNullOrWhiteSpace(package) || !Packages.ContainsKey(package.ToUpper()))
                    return TextCommandResult.Error(Lang.Get("packages:no-package"));

                if (player == null)
                    return TextCommandResult.Error(Lang.Get("packages:no-player"));

                Package curPackage = Packages.Get(package.ToUpper());

                foreach (Item item in curPackage.Items)
                {
                    Vintagestory.API.Common.Item i = api.World.GetItem(new AssetLocation(item.Name));
                    if (i == null)
                        continue;

                    ItemStack itemStack = new ItemStack(i, item.Quantity);
                    player.Entity.TryGiveItemStack(itemStack.Clone());
                }

                return TextCommandResult.Success(string.Format(Lang.Get("packages:pkg-give-success", player.PlayerName)));
            }
            catch (Exception e)
            {
                api.Logger.Error(e);
                return TextCommandResult.Error(Lang.Get("packages:pkg-give-error"));
            }
        }

        /// <summary>
        /// Create a file with all items registered in server
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private TextCommandResult CmdItems(TextCommandCallingArgs args)
        {
            try
            {
                string filePath = Path.Combine(DataDirectory, "items.txt");
                if (File.Exists(filePath))
                    File.Delete(filePath);

                // Getting all items registered in Server
                IEnumerable<string> items = this.api.World.Items
                    .Where(x => x.Code != null)
                    .Select(x => x.Code.GetName());

                // Writing all items in file
                File.WriteAllLines(filePath, items);
                return TextCommandResult.Success(string.Format(Lang.Get("packages:pkg-items-success"), filePath));
            }
            catch (Exception e)
            {
                api.Logger.Error(e);
                return TextCommandResult.Error(Lang.Get("packages:pkg-items-error"));
            }
            
        }

        /// <summary>
        /// List all package of packages.json file
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private TextCommandResult CmdList(TextCommandCallingArgs args)
        {
            return TextCommandResult.Success(string.Join("\n", Packages));
        }
}