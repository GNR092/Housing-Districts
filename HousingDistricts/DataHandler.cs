﻿using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TShockAPI;
using System.IO.Streams;
using Microsoft.Xna.Framework;

namespace HousingDistricts
{
	public delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
	public class GetDataHandlerArgs : EventArgs
	{
		public TSPlayer Player { get; private set; }
		public MemoryStream Data { get; private set; }

		public Player TPlayer
		{
			get { return Player.TPlayer; }
		}

		public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
		{
			Player = player;
			Data = data;
		}
	}
	public static class GetDataHandlers
	{
		static readonly string EditHouse = "house.edit";
		static readonly string TPHouse = "house.rod";
		private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

		public static void InitGetDataHandler()
		{
			GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
			{
				{PacketTypes.Tile, HandleTile},
				{PacketTypes.TileSendSquare, HandleSendTileSquare},
				{PacketTypes.LiquidSet, HandleLiquidSet},
				{PacketTypes.Teleport, HandleTeleport},
				{PacketTypes.PaintTile, HandlePaintTile},
				{PacketTypes.PaintWall, HandlePaintWall},
				{PacketTypes.PlaceObject, HandlePlaceObject},
				{PacketTypes.MassWireOperation, HandleMassWire}
			};
		}

		public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
		{
			GetDataHandlerDelegate handler;
			if (GetDataHandlerDelegates.TryGetValue(type, out handler))
			{
				try
				{
					return handler(new GetDataHandlerArgs(player, data));
				}
				catch (Exception ex)
				{
					TShock.Log.Error(ex.ToString());
				}
			}
			return false;
		}

		private static bool HandleSendTileSquare(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			short size = args.Data.ReadInt16();
			int tilex = args.Data.ReadInt16();
			int tiley = args.Data.ReadInt16();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(tilex, tiley, size, size);
					return House.HandlerAction((Func<House,bool>)(house =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.Account, house))
							{
								//args.Player.SendTileSquare(tilex, tiley);
								args.Player.SendTileSquareCentered(tilex, tiley);
								return true;
							}
						return false;
					}));
				}
			}
			return false;
		}

		private static bool HandleTeleport(GetDataHandlerArgs args)
		{
			if (HConfigFile.Config.AllowRod || args.Player.Group.HasPermission(TPHouse))
				return false;

			var Start = DateTime.Now;

			var Flags = args.Data.ReadInt8();
			var ID = args.Data.ReadInt16();
			var X = args.Data.ReadSingle();
			var Y = args.Data.ReadSingle();

			if ((Flags & 2) != 2 && (Flags & 1) != 1 && !args.Player.Group.HasPermission(TPHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle((int)(X / 16), (int)(Y / 16), 2, 4);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.CanVisitHouse(args.Player.Account, house))
							{
								args.Player.SendErrorMessage(string.Format("You do not have permission to teleport into house '{0}'.", house.Name));
								args.Player.Teleport(args.TPlayer.position.X, args.TPlayer.position.Y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePaintTile(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			var X = args.Data.ReadInt16();
			var Y = args.Data.ReadInt16();
			var T = args.Data.ReadInt8();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.Account, house))
							{
								args.Player.SendData(PacketTypes.PaintTile, "", X, Y, Main.tile[X, Y].color());
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePaintWall(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			var X = args.Data.ReadInt16();
			var Y = args.Data.ReadInt16();
			var T = args.Data.ReadInt8();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.Account, house))
							{
								args.Player.SendData(PacketTypes.PaintWall, "", X, Y, Main.tile[X, Y].wallColor());
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleTile(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			args.Data.ReadInt8();
			int x = args.Data.ReadInt16();
			int y = args.Data.ReadInt16();


            #region AwaitingTempPoint
            if (args.Player.AwaitingTempPoint > 0)
            {
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
                args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;

                if (args.Player.AwaitingTempPoint == 1)
                {
                    args.Player.SendMessage("¡Se ha configurado la esquina superior izquierda del área de protección!", Color.Yellow);
                    args.Player.SendTileSquareCentered(x, y);
                    args.Player.AwaitingTempPoint = 2;
                    args.Player.SendMessage("Ahora toca el bloque INFERIOR-DERECHA del área que se va a proteger.", Color.Aquamarine);
                    return true;
                }

                if (args.Player.AwaitingTempPoint == 2)
                {
                    args.Player.SendMessage("¡Se ha configurado la esquina inferior derecha del área de protección!", Color.Yellow);
                    args.Player.SendMessage("Ahora use /house add [nombre de la casa] para crearla y estar protegido", Color.Aquamarine);
                    args.Player.SendMessage("Ejemplo /house add casa de " + args.Player.Name, Color.Aquamarine);
                    args.Player.SendTileSquareCentered(x, y);
                    args.Player.AwaitingTempPoint = 0;
                    return true;
                }

            }
            #endregion
			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(x, y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.Account, house))
							{
								args.Player.SendTileSquareCentered(x, y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleMassWire(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			int x1 = args.Data.ReadInt16();
			int y1 = args.Data.ReadInt16();
			int x2 = args.Data.ReadInt16();
			int y2 = args.Data.ReadInt16();

			var player = HTools.GetPlayerByID(args.Player.Index);;

			if (args.Player.AwaitingTempPoint > 0)
			{
				args.Player.TempPoints[0].X = x1;
				args.Player.TempPoints[0].Y = y1;
				args.Player.TempPoints[1].X = x2;
				args.Player.TempPoints[1].Y = y2;

				args.Player.SendMessage("Protection corners have been set!", Color.Yellow);
				args.Player.AwaitingTempPoint = 0;
				return true;
			}
			if (!args.Player.Group.HasPermission(EditHouse))
			{
				Rectangle A = new Rectangle(Math.Min(x1, x2), args.TPlayer.direction != 1 ? y1 : y2, Math.Abs(x2 - x1) + 1, 1);
				Rectangle B = new Rectangle(args.TPlayer.direction != 1 ? x2 : x1, Math.Min(y1, y2), 1, Math.Abs(y2 - y1) + 1);

				//lock (HousingDistricts.HPlayers)
				{
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && (house.HouseArea.Intersects(A) || house.HouseArea.Intersects(B)))
							if (!HTools.OwnsHouse(args.Player.Account, house))
								return true;
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandlePlaceObject(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			int x = args.Data.ReadInt16();
			int y = args.Data.ReadInt16();
			//short tiletype = args.Data.ReadInt16();

			var player = HTools.GetPlayerByID(args.Player.Index);

			if (player.AwaitingHouseName)
			{
				if (HTools.InAreaHouseName(x, y) == null)
					args.Player.SendMessage("Tile is not in any House", Color.Yellow);
				else
					args.Player.SendMessage("House Name: " + HTools.InAreaHouseName(x, y), Color.Yellow);

				//args.Player.SendTileSquare(x, y);
				args.Player.SendTileSquareCentered(x, y);
				player.AwaitingHouseName = false;
				return true;
			}

			if (args.Player.AwaitingTempPoint > 0)
			{
				args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].X = x;
				args.Player.TempPoints[args.Player.AwaitingTempPoint - 1].Y = y;

				if (args.Player.AwaitingTempPoint == 1)
					args.Player.SendMessage("Top-left corner of protection area has been set!", Color.Yellow);

				if (args.Player.AwaitingTempPoint == 2)
					args.Player.SendMessage("Bottom-right corner of protection area has been set!", Color.Yellow);

				//args.Player.SendTileSquare(x, y);
				args.Player.SendTileSquareCentered(x, y);
				args.Player.AwaitingTempPoint = 0;
				return true;
			}
			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(x, y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.Account, house))
							{
								args.Player.SendTileSquareCentered(x, y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		private static bool HandleLiquidSet(GetDataHandlerArgs args)
		{
			var Start = DateTime.Now;

			int X = args.Data.ReadInt16();
			int Y = args.Data.ReadInt16();

			if (!args.Player.Group.HasPermission(EditHouse))
			{
				//lock (HousingDistricts.HPlayers)
				{
					var rect = new Rectangle(X, Y, 1, 1);
					return House.HandlerAction((house) =>
					{
						if (HousingDistricts.Timeout(Start)) return false;
						if (house != null && house.HouseArea.Intersects(rect))
							if (!HTools.OwnsHouse(args.Player.Account, house))
							{
								args.Player.SendTileSquareCentered(X, Y);
								return true;
							}
						return false;
					});
				}
			}
			return false;
		}

		
	}
}
