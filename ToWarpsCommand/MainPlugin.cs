using System;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Xna.Framework;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI.Hooks;
using System.IO;
using Mono.Data.Sqlite;

namespace TZDevelops
{
    [ApiVersion(2, 1)]
    public class ToWarpsCommand : TerrariaPlugin
	{
		public override string Author => "Zoom L1 | Colag";
        public override string Name => "ToWarpsCommand";
        public ToWarpsCommand(Main game) : base(game) {}

        Database manager;

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("twc.admin", new CommandDelegate(AddWarpToList), "tpadd"));
            Commands.ChatCommands.Add(new Command("twc.admin", new CommandDelegate(RemoveWarpFromList), "tpdel"));
            PlayerHooks.PlayerCommand += this.OnCommand;
            string arg = Path.Combine(TShock.SavePath, "TpaAdd.sqlite");
            manager = new Database(new SqliteConnection(string.Format("uri=file://{0},Version=3", arg)));
        }

        public void OnCommand(PlayerCommandEventArgs args)
        {
			if (args.CommandPrefix == "/")
			{
				ToWarps w = manager.GetWarp(args.CommandName);
				if (w == null)
				{
					return;
				}
				Warp warp = TShock.Warps.Find(w.WarpName);
				if (warp == null)
				{
					args.Player.SendErrorMessage("Oops. The administration did not create such a warp.");
					args.Handled = true;
					return;
				}
				args.Player.Teleport((float)(warp.Position.X * 16), (float)(warp.Position.Y * 16), 1);
				args.Handled = true;
				return;
			}
        }
        /*
            /tpadd <warp> <cmd>
            /tpdel<cmd>
        */
        void AddWarpToList(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage("You entered the command incorrectly. Please try /tpadd <WarpName> <Command>");
                return;
            }
            Warp w = TShock.Warps.Find(args.Parameters[0]);
            if (w == null)
            {
                args.Player.SendErrorMessage("You entered a warp {0}, but there is no such warp!", args.Parameters[0]);
                return;
            }
            manager.Add(w.Name, args.Parameters[1]);
            args.Player.SendSuccessMessage("Done! Now the command /{0} can be teleported to the warp {1}!", w.Name, args.Parameters[1]);
        }

        void RemoveWarpFromList(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("You entered the command incorrectly. Please try /tpdel <command>");
                return;
            }
            string cmd = args.Parameters[0];
            ToWarps w = manager.GetWarp(cmd);
            if (w == null)
            {
                args.Player.SendSuccessMessage("Now, there is no such command.");
                return;
            }
            manager.Remove(w.Command);
            args.Player.SendSuccessMessage("Bye bye command!");
        }

        public class ToWarps
        {
            public ToWarps(string wpne, string cmd)
            {
                this.WarpName = wpne;
                this.Command = cmd;
            }

            public string WarpName { get; set; }
            public string Command { get; set; }
        }

        public class Database
        {
            private IDbConnection database;
            public Database(IDbConnection db)
            {
                this.database = db;
                SqlTable table = new SqlTable("ToWarps", new SqlColumn[]
                {
                    new SqlColumn("WarpName", MySqlDbType.String),
                    new SqlColumn("Command", MySqlDbType.String)
                });
                IQueryBuilder provider;
                if (db.GetSqlType() != SqlType.Sqlite)
                {
                    IQueryBuilder queryBuilder = new MysqlQueryCreator();
                    provider = queryBuilder;
                }
                else
                {
                    IQueryBuilder queryBuilder = new SqliteQueryCreator();
                    provider = queryBuilder;
                }
                new SqlTableCreator(db, provider).EnsureTableStructure(table);
            }

            public void Add(string warpname, string command)
            {
                try
                {
                    this.database.Query("INSERT INTO ToWarps (WarpName, Command) VALUES (@0, @1);", warpname, command);
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }

            public void Remove(string command)
            {
                ToWarps w = GetWarp(command);
                if (w == null)
                {
                    return;
                }
                try
                {
                    this.database.Query("DELETE FROM ToWarps WHERE Command=@0;", command);
                }
                catch (Exception ex)
                {
                    TShock.Log.Error(ex.ToString());
                }
            }
			
            public ToWarps GetWarp(string command)
            {
                using (QueryResult reader = this.database.QueryReader("SELECT * FROM ToWarps WHERE Command=@0", command))
                {
                    while (reader != null && reader.Read())
                    {
                        ToWarps wrp = new ToWarps(reader.Get<string>("WarpName"), reader.Get<string>("Command"));
						return wrp;
                    }
                }
				return null;
            }
        }
	}
}